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

// Configure MassTransit with Azure Service Bus
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<OrderCreatedEventConsumer>();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        var logger = context.GetService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        logger?.LogInformation("Azure Service Bus connection string from config: {ConnectionString}", connectionString ?? "NULL");

        cfg.Host(connectionString, h =>
        {
            // Increase timeouts for emulator stability
            h.RetryMinBackoff = TimeSpan.FromSeconds(2);
            h.RetryMaxBackoff = TimeSpan.FromSeconds(30);
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
        });

        // Disable prefetch to reduce emulator load
        cfg.PrefetchCount = 0;
        cfg.MaxConcurrentCalls = 1;

        // Configure retry policy for transport failures
        cfg.UseMessageRetry(r => 
        {
            r.Exponential(10, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));
            r.Ignore<System.Net.Http.HttpRequestException>();
            r.Ignore<Azure.RequestFailedException>();
        });

        // Add circuit breaker to prevent overwhelming the service bus
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });

        cfg.ConfigureEndpoints(context);
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

