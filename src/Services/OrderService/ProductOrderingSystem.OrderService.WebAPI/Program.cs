using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.OrderService.Application.Commands.Orders;
using ProductOrderingSystem.OrderService.Application.Consumers;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.OrderService.Infrastructure.Persistence;
using ProductOrderingSystem.OrderService.Infrastructure.Repositories;
using ProductOrderingSystem.OrderService.WebAPI.Endpoints;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using MassTransit;
using ProductOrderingSystem.Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly);
});

// Add OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Add MongoDB
builder.AddMongoDBClient("orderdb");

// Configure database settings
builder.Services.Configure<OrderDatabaseSettings>(options =>
{
    options.DatabaseName = "orderdb";
});

// Add repositories and context
builder.Services.AddScoped<OrderDbContext>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductCacheRepository, ProductCacheRepository>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers for order compensation
    x.AddConsumer<ProductReservationFailedEventConsumer>();
    
    // Add consumer for payment confirmation
    x.AddConsumer<PaymentProcessedEventConsumer>();
    
    // Add consumers for product cache synchronization
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<ProductUpdatedEventConsumer>();
    x.AddConsumer<ProductDeletedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Get connection string from Aspire - it provides this via .WithReference(rabbitMq)
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        // Log the connection string for debugging
        var logger = context.GetService<ILogger<Program>>();
        logger?.LogInformation("RabbitMQ connection string from config: {ConnectionString}", 
            connectionString ?? "NULL");
        
        // WORKAROUND: Aspire's proxied RabbitMQ connection fails with "connection.start was never received"
        // Use the standalone RabbitMQ container directly on localhost:5672
        logger?.LogWarning("Using direct RabbitMQ connection to localhost:5672 instead of Aspire proxy");
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure endpoints with explicit exchange bindings
        cfg.ReceiveEndpoint("order-service-product-created", e =>
        {
            e.Bind<ProductCreatedEvent>();
            e.ConfigureConsumer<ProductCreatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-service-product-updated", e =>
        {
            e.Bind<ProductUpdatedEvent>();
            e.ConfigureConsumer<ProductUpdatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-service-product-deleted", e =>
        {
            e.Bind<ProductDeletedEvent>();
            e.ConfigureConsumer<ProductDeletedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-service-product-reservation-failed", e =>
        {
            e.Bind<ProductReservationFailedEvent>();
            e.ConfigureConsumer<ProductReservationFailedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-service-payment-processed", e =>
        {
            e.Bind<PaymentProcessedEvent>();
            e.ConfigureConsumer<PaymentProcessedEventConsumer>(context);
        });
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

// Disable HTTPS redirection when running behind API Gateway
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Order endpoints
app.MapOrderEndpoints();

app.MapDefaultEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
