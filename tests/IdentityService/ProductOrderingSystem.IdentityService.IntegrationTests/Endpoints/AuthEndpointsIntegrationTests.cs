using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Identity;
using Testcontainers.MongoDb;

namespace ProductOrderingSystem.IdentityService.IntegrationTests.Endpoints;

public class AuthEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public AuthEndpointsIntegrationTests()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .WithPortBinding(27017, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing IMongoClient and IMongoDatabase registrations
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(IMongoClient) ||
                        d.ServiceType == typeof(IMongoDatabase)).ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Register test MongoDB client and database
                    services.AddSingleton<IMongoClient>(sp =>
                    {
                        return new MongoClient(_mongoContainer.GetConnectionString());
                    });

                    services.AddSingleton<IMongoDatabase>(sp =>
                    {
                        var client = sp.GetRequiredService<IMongoClient>();
                        return client.GetDatabase("identitydb");
                    });
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
    public async Task Register_WithValidData_ShouldReturnCreatedAndUserDto()
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "newuser@example.com",
            Username: "newuser",
            Password: "SecurePassword123!",
            FirstName: "New",
            LastName: "User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - REST principles: 201 Created with Location header, empty body
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Match("/api/auth/users/*");
        
        // Extract user ID from Location header
        var userId = response.Headers.Location.ToString().Split('/').Last();
        userId.Should().NotBeNullOrEmpty();
        
        // Verify empty body
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var request1 = new RegisterRequest(
            Email: "duplicate@example.com",
            Username: "user1",
            Password: "SecurePassword123!",
            FirstName: "First",
            LastName: "User"
        );

        var request2 = new RegisterRequest(
            Email: "duplicate@example.com",
            Username: "user2",
            Password: "SecurePassword123!",
            FirstName: "Second",
            LastName: "User"
        );

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request1);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Invalid email
        var request = new RegisterRequest(
            Email: "invalid-email",
            Username: "testuser",
            Password: "SecurePassword123!",
            FirstName: "Test",
            LastName: "User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokenAndUserInfo()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest(
            Email: "logintest@example.com",
            Username: "loginuser",
            Password: "SecurePassword123!",
            FirstName: "Login",
            LastName: "Test"
        );

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            EmailOrUsername: "logintest@example.com",
            Password: "SecurePassword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrEmpty();
        loginResponse.User.Email.Should().Be("logintest@example.com");
        loginResponse.User.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task Login_WithUsername_ShouldReturnTokenAndUserInfo()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest(
            Email: "usernamelogin@example.com",
            Username: "usernameuser",
            Password: "SecurePassword123!",
            FirstName: "Username",
            LastName: "Login"
        );

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            EmailOrUsername: "usernameuser", // Using username instead of email
            Password: "SecurePassword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrEmpty();
        loginResponse.User.Username.Should().Be("usernameuser");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnBadRequest()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest(
            Email: "wrongpass@example.com",
            Username: "wrongpassuser",
            Password: "SecurePassword123!",
            FirstName: "Wrong",
            LastName: "Pass"
        );

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            EmailOrUsername: "wrongpass@example.com",
            Password: "WrongPassword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var loginRequest = new LoginRequest(
            EmailOrUsername: "nonexistent@example.com",
            Password: "SomePassword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange - Register and login to get a token
        var registerRequest = new RegisterRequest(
            Email: "currentuser@example.com",
            Username: "currentuser",
            Password: "SecurePassword123!",
            FirstName: "Current",
            LastName: "User"
        );

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            EmailOrUsername: "currentuser@example.com",
            Password: "SecurePassword123!"
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Add token to request
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", loginData!.Token);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be("currentuser@example.com");
        userDto.Username.Should().Be("currentuser");
        userDto.FirstName.Should().Be("Current");
        userDto.LastName.Should().Be("User");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token in request
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange - Invalid token
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_GeneratedToken_ShouldContainCorrectClaims()
    {
        // Arrange - Register and login
        var registerRequest = new RegisterRequest(
            Email: "claimstest@example.com",
            Username: "claimsuser",
            Password: "SecurePassword123!",
            FirstName: "Claims",
            LastName: "Test"
        );

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            EmailOrUsername: "claimstest@example.com",
            Password: "SecurePassword123!"
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act - Decode the JWT token
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(loginData!.Token);

        // Assert
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "claimstest@example.com");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "claimsuser");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == "Claims");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == "Test");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task CompleteAuthenticationFlow_ShouldWorkEndToEnd()
    {
        // Step 1: Register
        var registerRequest = new RegisterRequest(
            Email: "fullflow@example.com",
            Username: "fullflowuser",
            Password: "SecurePassword123!",
            FirstName: "Full",
            LastName: "Flow"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Login
        var loginRequest = new LoginRequest(
            EmailOrUsername: "fullflow@example.com",
            Password: "SecurePassword123!"
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginData!.Token.Should().NotBeNullOrEmpty();

        // Step 3: Access protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", loginData.Token);

        var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userData = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        userData!.Email.Should().Be("fullflow@example.com");
        userData.Username.Should().Be("fullflowuser");
    }
}

