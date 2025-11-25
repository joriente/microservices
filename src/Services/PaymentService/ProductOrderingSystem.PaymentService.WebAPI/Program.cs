using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.PaymentService.Application.CommandHandlers;
using ProductOrderingSystem.PaymentService.Application.Consumers;
using ProductOrderingSystem.PaymentService.Domain.Repositories;
using ProductOrderingSystem.PaymentService.Domain.Services;
using ProductOrderingSystem.PaymentService.Infrastructure.Configuration;
using ProductOrderingSystem.PaymentService.Infrastructure.Persistence;
using ProductOrderingSystem.PaymentService.Infrastructure.Services;
using ProductOrderingSystem.PaymentService.WebAPI.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add MongoDB using Aspire client
builder.AddMongoDBClient("paymentdb");

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(ProcessPaymentCommandHandler).Assembly);
});

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Configure Stripe settings
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<OrderCreatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Get connection string from Aspire
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
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

        // Explicit endpoint configuration to bind to correct MassTransit exchange names
        cfg.ReceiveEndpoint("payment-service-order-created", e =>
        {
            e.Bind<ProductOrderingSystem.Shared.Contracts.Events.OrderCreatedEvent>();
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
    });
});

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

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapPaymentEndpoints();

app.Run();

