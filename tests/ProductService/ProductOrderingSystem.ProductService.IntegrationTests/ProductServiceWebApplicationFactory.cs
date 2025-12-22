using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

    public ProductServiceWebApplicationFactory()
    {
        // Start containers synchronously before ConfigureWebHost is called
        _mongoDbContainer.StartAsync().GetAwaiter().GetResult();
        _rabbitMqContainer.StartAsync().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync()
    {
        // Containers already started in constructor
        await Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variable for Wolverine RabbitMQ - must be set before host builds
        Environment.SetEnvironmentVariable("ConnectionStrings__messaging", _rabbitMqContainer.GetConnectionString());
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration with high priority (added last = highest priority)
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:productdb"] = _mongoDbContainer.GetConnectionString(),
                ["Jwt:SecretKey"] = SecretKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureTestServices(services =>
        {
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
            
            // Reconfigure JWT Bearer options to use test values
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
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

    public new async Task DisposeAsync()
    {
        await _mongoDbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}