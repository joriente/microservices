using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Application.Consumers;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Infrastructure.Configuration;
using ProductOrderingSystem.ProductService.Infrastructure.Persistence;
using ProductOrderingSystem.ProductService.Infrastructure.Messaging;
using ProductOrderingSystem.ProductService.WebAPI.Endpoints;
using ProductOrderingSystem.ProductService.WebAPI.Data;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add MongoDB using Aspire client
builder.AddMongoDBClient("productdb");

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

// Configure MongoDB settings
var mongoConfig = new MongoDbConfiguration
{
    DatabaseName = "productdb",
    ProductsCollectionName = "products"
};

builder.Services.AddSingleton(mongoConfig);
builder.Services.AddScoped<DomainEventDispatcher>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register database seeder as a background service
builder.Services.AddHostedService<ProductSeeder>();

// Configure MassTransit with Azure Service Bus
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<OrderCancelledEventConsumer>();

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

// Health Checks will be added by Aspire ServiceDefaults

// Add OpenAPI/Scalar for API documentation
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    // Redirect root to Scalar documentation
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseCors();
// Disable HTTPS redirection when running behind API Gateway
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Product endpoints
app.MapProductEndpoints();

// Map Aspire default endpoints (includes health checks)
app.MapDefaultEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }

