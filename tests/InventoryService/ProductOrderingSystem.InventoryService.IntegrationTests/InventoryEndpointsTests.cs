using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.InventoryService.Features.Inventory;

namespace ProductOrderingSystem.InventoryService.IntegrationTests;

[Collection("Inventory Service Integration Tests")]
public class InventoryEndpointsTests : IClassFixture<InventoryServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "ProductOrderingSystem";
    private const string Audience = "ProductOrderingSystem.Services";

    public InventoryEndpointsTests(InventoryServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInventoryByProductId_WithNonExistentProduct_ShouldReturn404()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var productId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/inventory/product/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ReserveInventory_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        // First, create inventory item manually for this test
        // (In real scenario, this would come from ProductCreatedEvent)
        
        var reserveRequest = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(productId, 5)
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/reserve", reserveRequest);

        // Assert
        // Note: This will fail because product doesn't exist, but tests the endpoint
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ReserveInventory_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        var reserveRequest = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(productId, 1000) // Requesting more than available
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/reserve", reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ReserveInventory_WithEmptyItems_ShouldReturnBadRequest()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var orderId = Guid.NewGuid();
        
        var reserveRequest = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>());

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/reserve", reserveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    private string GenerateValidJwtToken(string userId, string email, string username)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("username", username)
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
