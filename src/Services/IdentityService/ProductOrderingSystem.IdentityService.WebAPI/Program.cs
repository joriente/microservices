using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProductOrderingSystem.IdentityService.Application.Handlers.Auth;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.IdentityService.Domain.Services;
using ProductOrderingSystem.IdentityService.Infrastructure.Data;
using ProductOrderingSystem.IdentityService.Infrastructure.Repositories;
using ProductOrderingSystem.IdentityService.Infrastructure.Services;
using ProductOrderingSystem.IdentityService.WebAPI.Data;
using ProductOrderingSystem.IdentityService.WebAPI.Endpoints;
using Scalar.AspNetCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add MongoDB
builder.AddMongoDBClient("identitydb");
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("identitydb");
});

// Add services to the container
builder.Services.AddOpenApi();

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(RegisterUserCommandHandler).Assembly);
});

// Add database context
builder.Services.AddSingleton<IdentityDbContext>();

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Add admin user seeder
builder.Services.AddHostedService<AdminUserSeeder>();

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

// Disable HTTPS redirection when running behind API Gateway
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
