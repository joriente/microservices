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
            
            // Aspire provides connection string in format: amqp://user:password@host:port
            // Parse it to extract credentials
            if (!string.IsNullOrEmpty(connectionString))
            {
                cfg.Host(new Uri(connectionString));
            }
            else
            {
                // Fallback to localhost with default credentials
                cfg.Host("localhost", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
            }

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

// Add RabbitMQ readiness checker
services.AddHttpClient<IRabbitMqReadinessChecker, RabbitMqReadinessChecker>();
services.AddSingleton<IRabbitMqReadinessChecker, RabbitMqReadinessChecker>();

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
    // With 200ms delay between events and potential MassTransit batching/buffering,
    // we need to wait longer to ensure all messages are sent to RabbitMQ
    if (publishEvents)
    {
        logger.LogInformation("Waiting 10 seconds for all events to be fully published to RabbitMQ...");
        await Task.Delay(10000);
        
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
