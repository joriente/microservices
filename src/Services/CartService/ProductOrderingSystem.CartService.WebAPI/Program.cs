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

// Configure Azure Service Bus with MassTransit
builder.Services.AddMassTransit(x =>
{
    // Register event consumers
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<ProductUpdatedEventConsumer>();
    x.AddConsumer<ProductDeletedEventConsumer>();
    x.AddConsumer<OrderCreatedEventConsumer>();
    
    x.UsingAzureServiceBus((context, cfg) =>
    {
        var logger = context.GetService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        logger?.LogInformation("Azure Service Bus connection string from config: {ConnectionString}", connectionString ?? "NULL");

        cfg.Host(connectionString);

        cfg.ConfigureEndpoints(context);
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
