using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductOrderingSystem.IdentityService.Domain.Entities;
using ProductOrderingSystem.IdentityService.Infrastructure.Services;
using ErrorOr;

namespace ProductOrderingSystem.IdentityService.Infrastructure.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" },
            { "Jwt:Issuer", "ProductOrderingSystem" },
            { "Jwt:Audience", "ProductOrderingSystem.Services" },
            { "Jwt:AccessTokenExpirationMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _jwtTokenService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be("ProductOrderingSystem");
        jwtToken.Audiences.Should().Contain("ProductOrderingSystem.Services");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "testuser");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == "John");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == "Doe");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserRoles()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@example.com",
            Username = "adminuser",
            PasswordHash = "hashedpassword",
            FirstName = "Jane",
            LastName = "Admin"
        };

        user.AddRole("Admin");
        user.AddRole("User");

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetExpirationTime()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(60);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(0);
        
        // Should be valid Base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _jwtTokenService.GenerateRefreshToken();
        var token2 = _jwtTokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrueAndUserId()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var token = _jwtTokenService.GenerateAccessToken(user);

        // Verify the token contains the Sub claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull("token should contain Sub claim");
        subClaim!.Value.Should().Be(user.Id);

        // Try manual validation to see what's happening
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var extractedUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            extractedUserId.Should().Be(user.Id, "manual validation should extract correct userId");
        }
        catch (Exception ex)
        {
            throw new Exception($"Manual validation failed: {ex.Message}", ex);
        }

        // Act
        var isValid = _jwtTokenService.ValidateToken(token, out var userId);

        // Assert
        isValid.Should().BeTrue();
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void ValidateToken_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var token = _jwtTokenService.GenerateAccessToken(user);
        
        // Tamper with the token
        var tamperedToken = token.Substring(0, token.Length - 5) + "XXXXX";

        // Act
        var isValid = _jwtTokenService.ValidateToken(tamperedToken, out var userId);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" },
            { "Jwt:Issuer", "ProductOrderingSystem" },
            { "Jwt:Audience", "ProductOrderingSystem.Services" },
            { "Jwt:AccessTokenExpirationMinutes", "0" } // Expired immediately
        };

        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var expiredTokenService = new JwtTokenService(expiredConfig);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var token = expiredTokenService.GenerateAccessToken(user);

        // Wait a moment to ensure token is expired
        Thread.Sleep(100);

        // Act
        var isValid = _jwtTokenService.ValidateToken(token, out var userId);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldReturnFalse()
    {
        // Arrange
        var malformedToken = "this.is.not.a.valid.jwt";

        // Act
        var isValid = _jwtTokenService.ValidateToken(malformedToken, out var userId);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongIssuer_ShouldReturnFalse()
    {
        // Arrange - Create token with different issuer
        var wrongIssuerConfig = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" },
            { "Jwt:Issuer", "WrongIssuer" },
            { "Jwt:Audience", "ProductOrderingSystem.Services" },
            { "Jwt:AccessTokenExpirationMinutes", "60" }
        };

        var wrongConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(wrongIssuerConfig)
            .Build();

        var wrongIssuerService = new JwtTokenService(wrongConfig);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var token = wrongIssuerService.GenerateAccessToken(user);

        // Act - Validate with correct configuration
        var isValid = _jwtTokenService.ValidateToken(token, out var userId);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongAudience_ShouldReturnFalse()
    {
        // Arrange - Create token with different audience
        var wrongAudienceConfig = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" },
            { "Jwt:Issuer", "ProductOrderingSystem" },
            { "Jwt:Audience", "WrongAudience" },
            { "Jwt:AccessTokenExpirationMinutes", "60" }
        };

        var wrongConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(wrongAudienceConfig)
            .Build();

        var wrongAudienceService = new JwtTokenService(wrongConfig);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        var token = wrongAudienceService.GenerateAccessToken(user);

        // Act - Validate with correct configuration
        var isValid = _jwtTokenService.ValidateToken(token, out var userId);

        // Assert
        isValid.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void GenerateAccessToken_WithMissingConfiguration_ShouldThrowException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var invalidService = new JwtTokenService(emptyConfig);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        var act = () => invalidService.GenerateAccessToken(user);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*");
    }
}
