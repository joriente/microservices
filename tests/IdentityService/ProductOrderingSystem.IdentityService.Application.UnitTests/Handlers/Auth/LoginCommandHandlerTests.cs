using ErrorOr;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Application.Handlers.Auth;
using ProductOrderingSystem.IdentityService.Domain.Entities;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.IdentityService.Domain.Services;

namespace ProductOrderingSystem.IdentityService.Application.UnitTests.Handlers.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration mock
        _configurationMock
            .Setup(x => x["Jwt:AccessTokenExpirationMinutes"])
            .Returns("60");

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _jwtTokenServiceMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnLoginResponse_WhenCredentialsAreValid_UsingEmail()
    {
        // Arrange
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = hashedPassword,
            FirstName = "John",
            LastName = "Doe",
            Roles = new List<string> { "User" },
            IsActive = true
        };

        var command = new LoginCommand("test@example.com", password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Token.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.User.Email.Should().Be(user.Email);
        result.Value.User.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task Handle_ShouldReturnLoginResponse_WhenCredentialsAreValid_UsingUsername()
    {
        // Arrange
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = hashedPassword,
            FirstName = "John",
            LastName = "Doe",
            Roles = new List<string> { "User" },
            IsActive = true
        };

        var command = new LoginCommand("testuser", password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Token.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundError_WhenUserDoesNotExist()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Description.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword"),
            IsActive = true
        };

        var command = new LoginCommand("test@example.com", "wrongPassword");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Password");
        result.FirstError.Description.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenError_WhenUserIsInactive()
    {
        // Arrange
        var password = "password123";
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = false
        };

        var command = new LoginCommand("test@example.com", password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        result.FirstError.Code.Should().Be("User.IsActive");
        result.FirstError.Description.Should().Contain("deactivated");
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectExpiresAt()
    {
        // Arrange
        var password = "password123";
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true
        };

        var command = new LoginCommand("test@example.com", password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        var beforeLogin = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var afterLogin = DateTime.UtcNow;

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ExpiresAt.Should().BeAfter(beforeLogin.AddMinutes(59));
        result.Value.ExpiresAt.Should().BeBefore(afterLogin.AddMinutes(61));
    }
}
