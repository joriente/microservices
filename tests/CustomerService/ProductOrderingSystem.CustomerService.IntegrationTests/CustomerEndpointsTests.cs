using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProductOrderingSystem.CustomerService.IntegrationTests;

public class CustomerEndpointsTests : IClassFixture<CustomerServiceWebApplicationFactory>
{
    private readonly CustomerServiceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string SecretKey = CustomerServiceWebApplicationFactory.SecretKey;
    private const string Issuer = CustomerServiceWebApplicationFactory.Issuer;
    private const string Audience = CustomerServiceWebApplicationFactory.Audience;

    public CustomerEndpointsTests(CustomerServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateCustomer_ShouldSucceed()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var email = $"test-{Guid.NewGuid()}@example.com";

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", new
        {
            email,
            firstName = "John",
            lastName = "Doe",
            phoneNumber = "+1234567890"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateAndGetCustomer_ShouldReturnSameCustomer()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var email = $"get-test-{Guid.NewGuid()}@example.com";
        var createResponse = await _client.PostAsJsonAsync("/api/customers", new
        {
            email,
            firstName = "Jane",
            lastName = "Smith",
            phoneNumber = "+9876543210"
        });

        Assert.True(createResponse.IsSuccessStatusCode);
        
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var customerId = System.Text.Json.JsonDocument.Parse(createContent)
            .RootElement.GetProperty("id").GetGuid();

        // Act
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var retrievedEmail = System.Text.Json.JsonDocument.Parse(getContent)
            .RootElement.GetProperty("email").GetString();
        Assert.Equal(email, retrievedEmail);
        
        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetCustomer_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/customers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetCustomers_ShouldReturnList()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/customers?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    private static string GenerateValidJwtToken(string userId, string email, string username)
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
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
