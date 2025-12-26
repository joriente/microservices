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
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateOrderCommand).Assembly);
});

// Add OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("X-Pagination");
    });
});

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

// Configure MassTransit with Azure Service Bus
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
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        var uri = new Uri(connectionString ?? "amqp://localhost:5672");
        
        // Aspire connection string includes credentials in URI format: amqp://user:pass@host:port
        // Use Uri directly without overriding credentials
        cfg.Host(uri);

        // Limit concurrent message processing to prevent overwhelming MongoDB
        cfg.UseConcurrencyLimit(5);
        
        // Limit prefetch count - fetch 20 messages at a time from RabbitMQ
        cfg.PrefetchCount = 20;
        
        // Configure retry policy for transient failures (like MongoDB connection delays)
        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000, 5000, 10000));

        // Use custom endpoint name formatter to create unique queues per service
        cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("order-service", false));
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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map Order endpoints
app.MapOrderEndpoints();

app.MapDefaultEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
