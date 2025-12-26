using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.Shared.Contracts.Orders;
using Testcontainers.MongoDb;

namespace ProductOrderingSystem.OrderService.IntegrationTests.Authorization;

public class OrderAuthorizationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "ProductOrderingSystem";
    private const string Audience = "ProductOrderingSystem.Services";

    public OrderAuthorizationTests()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .WithPortBinding(27017, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        // Configure the factory to inject MongoDB, RabbitMQ connection string and JWT settings
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Set environment variables BEFORE configuration is built
                builder.UseEnvironment("Development");
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add test configuration - this needs to be set BEFORE services are configured
                    // These values will override Program.cs defaults
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672",
                        ["ConnectionStrings:orderdb"] = _mongoContainer.GetConnectionString(),
                        ["Jwt:SecretKey"] = SecretKey,
                        ["Jwt:Issuer"] = Issuer,
                        ["Jwt:Audience"] = Audience,
                        ["Jwt:AccessTokenExpirationMinutes"] = "60"
                    }!);
                });

                builder.ConfigureServices((context, services) =>
                {
                    // Replace MongoDB client with test container connection
                    services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => 
                        new MongoDB.Driver.MongoClient(_mongoContainer.GetConnectionString()));
                        
                    // Explicitly reconfigure JWT bearer options after Program.cs setup using PostConfigure
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
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
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var authHeader = context.Request.Headers["Authorization"].ToString();
                                Console.WriteLine($"Authorization header received: {authHeader}");
                                Console.WriteLine($"Token from context: {context.Token}");
                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                                Console.WriteLine($"Exception type: {context.Exception.GetType().Name}");
                                if (context.Exception.InnerException != null)
                                {
                                    Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
                                }
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {
                                Console.WriteLine("Token validated successfully");
                                return Task.CompletedTask;
                            }
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
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "customer123",
            CustomerEmail: "customer@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: "product1", ProductName: "Test Product", Price: 10.0m, Quantity: 2)
            },
            Notes: "Test order"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithValidToken_ShouldNotReturnUnauthorized()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateOrderRequest(
            CustomerId: "customer123",
            CustomerEmail: "customer@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: "product1", ProductName: "Test Product", Price: 10.0m, Quantity: 2)
            },
            Notes: "Authorized order"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert - Might be NotFound (product doesn't exist) or BadRequest, but NOT Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var request = new CreateOrderRequest(
            CustomerId: "customer123",
            CustomerEmail: "customer@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: "product1", ProductName: "Test Product", Price: 10.0m, Quantity: 2)
            },
            Notes: "Test order"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderById_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token

        // Act
        var response = await _client.GetAsync("/api/orders/test-order-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderById_WithValidToken_ShouldNotReturnUnauthorized()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders/test-order-id");

        // Assert - Should be NotFound but NOT Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token

        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        Console.WriteLine($"Generated token: {token}");
        Console.WriteLine($"Token length: {token.Length}");
        Console.WriteLine($"Token parts count: {token.Split('.').Length}");
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"Authorization header set: {_client.DefaultRequestHeaders.Authorization}");

        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_WithCustomerIdFilter_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders?customerId=customer123&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var expiredToken = GenerateExpiredJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var request = new CreateOrderRequest(
            CustomerId: "customer123",
            CustomerEmail: "customer@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: "product1", ProductName: "Test Product", Price: 10.0m, Quantity: 2)
            },
            Notes: "Test order"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithWrongAudienceToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var wrongAudienceToken = GenerateJwtTokenWithWrongAudience("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongAudienceToken);

        var request = new CreateOrderRequest(
            CustomerId: "customer123",
            CustomerEmail: "customer@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: "product1", ProductName: "Test Product", Price: 10.0m, Quantity: 2)
            },
            Notes: "Test order"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithTokenContainingUserClaims_ShouldIncludeClaimsInContext()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // REST principles: Response should be an array with Pagination header
        response.Headers.Should().ContainKey("X-Pagination");
        
        // The token should be successfully validated and user context available
        var content = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        content.Should().NotBeNull();
    }

    // Helper methods to generate JWT tokens for testing
    private static string GenerateValidJwtToken(string userId, string email, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.GivenName, "Test"),
            new Claim(JwtRegisteredClaimNames.FamilyName, "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateExpiredJwtToken(string userId, string email, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateJwtTokenWithWrongAudience(string userId, string email, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: "WrongAudience", // Wrong audience
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
