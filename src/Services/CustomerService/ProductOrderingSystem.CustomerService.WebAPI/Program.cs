using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProductOrderingSystem.CustomerService.Application.Consumers;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Repositories;
using ProductOrderingSystem.CustomerService.Infrastructure.Repositories;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// MongoDB - use "customerdb" to match the AppHost database reference
builder.AddMongoDBClient("customerdb");
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("customerdb");
});

// Add repositories
builder.Services.AddSingleton<ICustomerRepository, MongoCustomerRepository>();

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateCustomerCommand).Assembly);

// Add MassTransit with Azure Service Bus
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<OrderCreatedEventConsumer>();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        var logger = context.GetService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        logger?.LogInformation("Azure Service Bus connection string from config: {ConnectionString}", connectionString ?? "NULL");

        cfg.Host(connectionString, h =>
        {
            // Increase timeouts for emulator stability
            h.RetryMinBackoff = TimeSpan.FromSeconds(2);
            h.RetryMaxBackoff = TimeSpan.FromSeconds(30);
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
        });

        // Disable prefetch to reduce emulator load
        cfg.PrefetchCount = 0;
        cfg.MaxConcurrentCalls = 1;

        // Configure retry policy for transport failures
        cfg.UseMessageRetry(r => 
        {
            r.Exponential(10, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));
            r.Ignore<System.Net.Http.HttpRequestException>();
            r.Ignore<Azure.RequestFailedException>();
        });

        // Add circuit breaker to prevent overwhelming the service bus
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Add Scalar for API documentation
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add custom health endpoint for API Gateway routing
app.MapGet("/api/customers/health", () => Results.Ok(new { status = "Healthy", service = "CustomerService" }));

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
