using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProductOrderingSystem.CartService.Application.Commands.Carts;
using ProductOrderingSystem.CartService.Application.Consumers;
using ProductOrderingSystem.CartService.Application.Queries.Carts;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.CartService.Infrastructure.Repositories;
using ProductOrderingSystem.CartService.WebAPI.Endpoints;
using System.Text;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add MongoDB using Aspire client
builder.AddMongoDBClient("cartdb");

// Register repositories
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductCacheRepository, ProductCacheRepository>();

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    // Auto-discover message handlers
    opts.Discovery.IncludeAssembly(typeof(CreateCartCommand).Assembly);
});

// Add JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProductOrderingSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ProductOrderingSystem.Services";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Configure Azure Service Bus with MassTransit
builder.Services.AddMassTransit(x =>
{
    // Register event consumers
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<ProductUpdatedEventConsumer>();
    x.AddConsumer<ProductDeletedEventConsumer>();
    x.AddConsumer<OrderCreatedEventConsumer>();
    
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
        cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("cart-service", false));
    });
});

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Use authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Cart endpoints
app.MapCartEndpoints();

app.Run();
