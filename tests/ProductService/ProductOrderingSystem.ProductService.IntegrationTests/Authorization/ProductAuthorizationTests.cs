using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.Shared.Contracts.Products;

namespace ProductOrderingSystem.ProductService.IntegrationTests.Authorization;

public class ProductAuthorizationTests : IClassFixture<ProductServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "ProductOrderingSystem";
    private const string Audience = "ProductOrderingSystem.Services";

    public ProductAuthorizationTests(ProductServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_WithoutToken_ShouldReturnOk()
    {
        // Arrange - No token needed for GET

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_WithoutToken_ShouldReturnOkOrNotFound()
    {
        // Arrange - No token needed for GET

        // Act
        var response = await _client.GetAsync("/api/products/test-id");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            StockQuantity: 10,
            Category: "Test",
            ImageUrl: "https://example.com/image.jpg"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithValidToken_ShouldReturnCreated()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateProductRequest(
            Name: "Authorized Product",
            Description: "Created with valid token",
            Price: 149.99m,
            StockQuantity: 5,
            Category: "Electronics",
            ImageUrl: "https://example.com/product.jpg"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            StockQuantity: 10,
            Category: "Test",
            ImageUrl: "https://example.com/image.jpg"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateProductRequest(
            Id: "test-id",
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 199.99m,
            StockQuantity: 20,
            Category: "Updated",
            ImageUrl: "https://example.com/updated.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/test-id", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithValidToken_ShouldNotReturnUnauthorized()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateProductRequest(
            Id: "test-id",
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 199.99m,
            StockQuantity: 20,
            Category: "Updated",
            ImageUrl: "https://example.com/updated.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/test-id", request);

        // Assert - Should be NotFound (product doesn't exist) but NOT Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token

        // Act
        var response = await _client.DeleteAsync("/api/products/test-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_WithValidToken_ShouldNotReturnUnauthorized()
    {
        // Arrange
        var token = GenerateValidJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/products/test-id");

        // Assert - Should be NotFound but NOT Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var expiredToken = GenerateExpiredJwtToken("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            StockQuantity: 10,
            Category: "Test",
            ImageUrl: "https://example.com/image.jpg"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithWrongIssuerToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var wrongIssuerToken = GenerateJwtTokenWithWrongIssuer("user123", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongIssuerToken);

        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            StockQuantity: 10,
            Category: "Test",
            ImageUrl: "https://example.com/image.jpg"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static string GenerateJwtTokenWithWrongIssuer(string userId, string email, string username)
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
            issuer: "WrongIssuer", // Wrong issuer
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
