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

    x.UsingAzureServiceBus((context, cfg) =>
    {
        var logger = context.GetService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        logger?.LogInformation("Azure Service Bus connection string from config: {ConnectionString}", connectionString ?? "NULL");

        cfg.Host(connectionString);

        cfg.ConfigureEndpoints(context);
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
