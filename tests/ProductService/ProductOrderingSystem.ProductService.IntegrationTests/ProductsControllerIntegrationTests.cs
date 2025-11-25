using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.Shared.Contracts.Products;

namespace ProductOrderingSystem.ProductService.IntegrationTests;

[Collection("Product Service Integration Tests")]
public class ProductsControllerIntegrationTests : IClassFixture<ProductServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "ProductOrderingSystem";
    private const string Audience = "ProductOrderingSystem.Services";

    public ProductsControllerIntegrationTests(ProductServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var createRequest = new CreateProductRequest(
            "Test Product",
            "A test product description",
            99.99m,
            50,
            "Electronics",
            ""
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createRequest, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // The endpoint returns 201 with Location header, no body
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/products/");
        
        // Clean up authorization header
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetProductById_WithExistingId_ShouldReturnProduct()
    {
        // Arrange - First create a product
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var createRequest = new CreateProductRequest(
            "Test Product for Get",
            "A test product for retrieval",
            149.99m,
            25,
            "Books",
            ""
        );

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest, _jsonOptions);
        
        // Extract product ID from Location header
        var locationHeader = createResponse.Headers.Location!.ToString();
        var productId = locationHeader.Split('/').Last();
        
        // Clean up authorization for GET request (not required)
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedProduct = await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Id.Should().Be(productId);
        retrievedProduct.Name.Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task GetProductById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchProducts_ShouldReturnResults()
    {
        // Arrange - Create a product
        var token = GenerateValidJwtToken("testuser", "test@example.com", "testuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var createRequest = new CreateProductRequest(
            "Search Test Product",
            "Product for search testing",
            75.00m,
            20,
            "SearchCategory",
            ""
        );

        await _client.PostAsJsonAsync("/api/products", createRequest, _jsonOptions);
        
        // Clean up authorization for GET request (not required)
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // The endpoint returns a list of products with pagination in headers
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(_jsonOptions);
        products.Should().NotBeNull();
        products.Should().NotBeEmpty();
        
        // Verify pagination header exists
        response.Headers.Should().ContainKey("Pagination");
        
        // Clean up authorization header
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
