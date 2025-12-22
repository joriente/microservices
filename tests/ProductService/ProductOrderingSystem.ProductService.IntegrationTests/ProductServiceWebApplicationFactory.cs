using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProductOrderingSystem.ProductService.Infrastructure.Configuration;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;

namespace ProductOrderingSystem.ProductService.IntegrationTests;

public class ProductServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .WithPortBinding(27017, true)
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1-management")
        .Build();
    
    private const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "ProductOrderingSystem";
    private const string Audience = "ProductOrderingSystem.Services";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = _rabbitMqContainer.GetConnectionString(),
                ["ConnectionStrings:productdb"] = _mongoDbContainer.GetConnectionString(),
                ["Jwt:SecretKey"] = SecretKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            // Reconfigure JWT Bearer authentication with test settings
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Replace MongoDB client with test container connection
            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(_mongoDbContainer.GetConnectionString()));
            
            // Register test MongoDB configuration
            services.AddSingleton<Infrastructure.Configuration.MongoDbConfiguration>(sp =>
                new Infrastructure.Configuration.MongoDbConfiguration
                {
                    DatabaseName = "ProductServiceIntegrationTests",
                    ProductsCollectionName = "products"
                });
        });

        // Disable EventLog logging to prevent disposal issues in tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });

        // Use Development environment so health check endpoints are mapped
        builder.UseEnvironment("Development");
        builder.UseSetting("https_port", "");
    }

    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoDbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}