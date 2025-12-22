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
using ProductOrderingSystem.ProductService.WebAPI.Middleware;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add MongoDB using Aspire client
builder.AddMongoDBClient("productdb");

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    // Configure RabbitMQ transport
    var connectionString = builder.Configuration.GetConnectionString("messaging");
    var uri = new Uri(connectionString ?? "amqp://localhost:5672");
    opts.UseRabbitMq(uri)
        .AutoProvision(); // Auto-create exchanges and queues
    
    // Auto-discover message handlers in the Application assembly
    opts.Discovery.IncludeAssembly(typeof(CreateProductCommand).Assembly);
    
    // Configure message routing
    opts.PublishAllMessages().ToRabbitExchange("product-events");
    
    // Listen to order events
    opts.ListenToRabbitQueue("product-service-order-events")
        .ProcessInline(); // Process messages synchronously for now

    opts.Policies.DisableConventionalLocalRouting(); // Simple retry policy for transient failures
});

// Configure MongoDB settings
var mongoConfig = new MongoDbConfiguration
{
    DatabaseName = "productdb",
    ProductsCollectionName = "products"
};


builder.Services.AddOpenTelemetry()
    .WithTracing( tracing => tracing.AddSource("Wolverine"));

builder.Services.AddSingleton(mongoConfig);
builder.Services.AddScoped<DomainEventDispatcher>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register database seeder as a background service
builder.Services.AddHostedService<ProductSeeder>();

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
               .AllowAnyHeader()
               .WithExposedHeaders("X-Pagination"); // Expose custom pagination header
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

// Use exception handler middleware
app.UseExceptionHandler();

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

