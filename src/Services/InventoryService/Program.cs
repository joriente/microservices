using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Features.EventConsumers;
using ProductOrderingSystem.InventoryService.Features.Inventory;
using Scalar.AspNetCore;
using System.Text;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
builder.AddNpgsqlDbContext<InventoryDbContext>("inventorydb");

// Configure Wolverine for vertical slices
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

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

// Configure MassTransit with Azure Service Bus
builder.Services.AddMassTransit(x =>
{
    // Register event consumers
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<OrderCreatedEventConsumer>();
    x.AddConsumer<PaymentProcessedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        var uri = new Uri(connectionString ?? "amqp://localhost:5672");
        
        // Aspire connection string includes credentials in URI format: amqp://user:pass@host:port
        // Use Uri directly without overriding credentials
        cfg.Host(uri);

        // Configure concurrency and retry policies
        cfg.UseConcurrencyLimit(5);
        cfg.PrefetchCount = 20;
        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000, 5000, 10000));

        // Use custom endpoint name formatter to create unique queues per service
        cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("inventory-service", false));
    });
});

// Add OpenAPI/Scalar
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations");
        throw;
    }
}

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure OpenAPI/Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Use authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Map vertical slice endpoints
GetInventoryByProductId.MapEndpoint(app);
GetAllInventory.MapEndpoint(app);
AdjustInventory.MapEndpoint(app);
ReserveInventory.MapEndpoint(app);

// Add custom health endpoint for API Gateway routing
app.MapGet("/api/inventory/health", () => Results.Ok(new { status = "Healthy", service = "InventoryService" }));

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
