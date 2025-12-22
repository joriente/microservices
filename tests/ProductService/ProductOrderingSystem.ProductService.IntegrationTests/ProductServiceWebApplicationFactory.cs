using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            
            // Replace authentication with test authentication that auto-authenticates all requests
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
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

// Test authentication handler that automatically authenticates all requests
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}