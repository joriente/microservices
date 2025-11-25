using MassTransit;
using MediatR;
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

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add MongoDB - manually configure to disable retryable writes
var connectionString = builder.Configuration.GetConnectionString("cartdb");
if (!string.IsNullOrEmpty(connectionString))
{
    // Add retryWrites=false to connection string for standalone MongoDB
    var mongoUrl = new MongoUrl(connectionString);
    var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
    mongoClientSettings.RetryWrites = false;  // Disable for standalone MongoDB
    
    builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(mongoUrl.DatabaseName ?? "cartdb");
    });
}
else
{
    // Fallback to Aspire's default configuration
    builder.AddMongoDBClient("cartdb");
}

// Register repositories
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductCacheRepository, ProductCacheRepository>();

// Register MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCartCommand).Assembly);
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

// Configure RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    // Register event consumers
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<ProductUpdatedEventConsumer>();
    x.AddConsumer<ProductDeletedEventConsumer>();
    x.AddConsumer<OrderCreatedEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var logger = context.GetService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        logger?.LogInformation("RabbitMQ connection string from config: {ConnectionString}", connectionString ?? "NULL");

        // WORKAROUND: Aspire's proxied RabbitMQ connection fails with "connection.start was never received"
        // Use the standalone RabbitMQ container directly on localhost:5672
        logger?.LogWarning("Using direct RabbitMQ connection to localhost:5672 instead of Aspire proxy");
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Explicit endpoint configuration to bind to correct MassTransit exchange names
        // MassTransit uses format: "Namespace.EventName" not just "EventName"
        cfg.ReceiveEndpoint("cart-service-product-created", e =>
        {
            e.Bind<ProductOrderingSystem.Shared.Contracts.Events.ProductCreatedEvent>();
            e.ConfigureConsumer<ProductCreatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("cart-service-product-updated", e =>
        {
            e.Bind<ProductOrderingSystem.Shared.Contracts.Events.ProductUpdatedEvent>();
            e.ConfigureConsumer<ProductUpdatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("cart-service-product-deleted", e =>
        {
            e.Bind<ProductOrderingSystem.Shared.Contracts.Events.ProductDeletedEvent>();
            e.ConfigureConsumer<ProductDeletedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("cart-service-order-created", e =>
        {
            e.Bind<ProductOrderingSystem.Shared.Contracts.Events.OrderCreatedEvent>();
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
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
