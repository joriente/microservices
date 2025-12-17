using MassTransit;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Consumers;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Services;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;
using ProductOrderingSystem.AnalyticsService.WebAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add PostgreSQL with Entity Framework Core
builder.AddNpgsqlDbContext<AnalyticsDbContext>("analyticsdb");

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ProductOrderingSystem.AnalyticsService.Infrastructure.Data.AnalyticsDbContext).Assembly);
});

// Add Event Hub Publisher
builder.Services.AddSingleton<IEventHubPublisher, EventHubPublisher>();

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add event consumers
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentProcessedEventConsumer>();
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<InventoryReservedEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var messagingConnection = builder.Configuration.GetConnectionString("messaging");
        var uri = new Uri(messagingConnection!);
        
        cfg.Host(uri, h =>
        {
            h.Username(uri.UserInfo.Split(':')[0]);
            h.Password(uri.UserInfo.Split(':')[1]);
        });
        
        // Configure concurrency and retry
        cfg.UseConcurrencyLimit(5);
        cfg.PrefetchCount = 20;
        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000, 5000, 10000));
        
        // Use unique endpoint names for this service
        cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("analytics-service", false));
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

// Map Minimal API Endpoints
app.MapAnalyticsEndpoints();

app.Run();
