using ErrorOr;
using AwesomeAssertions;
using Moq;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Application.Handlers.Auth;
using ProductOrderingSystem.IdentityService.Domain.Entities;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Identity;

namespace ProductOrderingSystem.IdentityService.Application.UnitTests.Handlers.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new RegisterUserCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserDto_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "testuser",
            "password123",
            "John",
            "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<UserDto>();
        result.Value.Email.Should().Be(command.Email);
        result.Value.Username.Should().Be(command.Username);
        result.Value.FirstName.Should().Be(command.FirstName);
        result.Value.LastName.Should().Be(command.LastName);
        result.Value.Roles.Should().Contain("User");
        result.Value.IsActive.Should().BeTrue();

        _userRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<User>(u => 
                u.Email == command.Email && 
                u.Username == command.Username &&
                u.PasswordHash != command.Password), // Password should be hashed
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public async Task Handle_ShouldReturnValidationError_WhenEmailIsInvalid(string invalidEmail)
    {
        // Arrange
        var command = new RegisterUserCommand(
            invalidEmail,
            "testuser",
            "password123",
            "John",
            "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Email");
        result.FirstError.Description.Should().Contain("Invalid email format");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictError_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "testuser",
            "password123",
            "John",
            "Doe");

        var existingUser = new User { Email = command.Email };
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("User.Email");
        result.FirstError.Description.Should().Contain("email already exists");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictError_WhenUsernameAlreadyExists()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "testuser",
            "password123",
            "John",
            "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var existingUser = new User { Username = command.Username };
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(command.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("User.Username");
        result.FirstError.Description.Should().Contain("username already exists");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("abc")]
    public async Task Handle_ShouldReturnValidationError_WhenPasswordIsTooShort(string shortPassword)
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "testuser",
            shortPassword,
            "John",
            "Doe");

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
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Password");
        result.FirstError.Description.Should().Contain("at least 6 characters");
    }

    [Fact]
    public async Task Handle_ShouldHashPassword_BeforeStoringUser()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "testuser",
            "password123",
            "John",
            "Doe");

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(command.Password);
        capturedUser.PasswordHash.Should().NotBeNullOrEmpty();
        
        // Verify it's a BCrypt hash (BCrypt hashes start with $2)
        capturedUser.PasswordHash.Should().StartWith("$2");
    }
}
