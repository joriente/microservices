using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.DataSeeder.Infrastructure;
using ProductOrderingSystem.DataSeeder.Seeders;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddConfiguration(configuration.GetSection("Logging"));
});

// Add configuration
services.AddSingleton<IConfiguration>(configuration);

// Configure MassTransit with RabbitMQ
var publishEvents = configuration.GetValue<bool>("Seeding:PublishEvents");
if (publishEvents)
{
    services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var connectionString = configuration.GetConnectionString("messaging");
            var uri = new Uri(connectionString ?? "amqp://localhost:5672");
            
            cfg.Host(uri, h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // Add event publisher
    services.AddScoped<IEventPublisher, EventPublisher>();
}
else
{
    // Add a no-op event publisher when publishing is disabled
    services.AddScoped<IEventPublisher, NoOpEventPublisher>();
}

// Add HTTP clients
services.AddHttpClient("ProductService", client =>
{
    var baseUrl = configuration.GetConnectionString("ProductService");
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
});

services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = configuration.GetConnectionString("IdentityService");
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
});

// Add seeders
services.AddTransient<ProductSeeder>();
services.AddTransient<IdentitySeeder>();
services.AddTransient<DataSeederRunner>();

var serviceProvider = services.BuildServiceProvider();

// Run the seeder
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var runner = serviceProvider.GetRequiredService<DataSeederRunner>();

logger.LogInformation("=== Product Ordering System - Data Seeder ===");
logger.LogInformation("Event Publishing: {EventPublishing}", publishEvents ? "ENABLED" : "DISABLED");
logger.LogInformation("Starting data seeding process...");

try
{
    // Start MassTransit bus if event publishing is enabled
    if (publishEvents)
    {
        var busControl = serviceProvider.GetService<IBusControl>();
        if (busControl != null)
        {
            logger.LogInformation("Starting MassTransit bus...");
            await busControl.StartAsync();
            logger.LogInformation("✓ MassTransit bus started");
        }
    }

    await runner.RunAsync();
    logger.LogInformation("✓ Data seeding completed successfully!");
    
    // Give events time to be published before shutting down
    if (publishEvents)
    {
        logger.LogInformation("Waiting for events to be published...");
        await Task.Delay(2000);
        
        var busControl = serviceProvider.GetService<IBusControl>();
        if (busControl != null)
        {
            logger.LogInformation("Stopping MassTransit bus...");
            await busControl.StopAsync();
        }
    }
    
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "✗ Data seeding failed");
    return 1;
}
