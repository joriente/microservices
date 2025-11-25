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

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<OrderCancelledEventConsumer>();

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

        // Configure endpoints
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

