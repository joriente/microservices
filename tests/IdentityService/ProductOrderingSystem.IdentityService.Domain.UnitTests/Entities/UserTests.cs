using AwesomeAssertions;
using ProductOrderingSystem.IdentityService.Domain.Entities;

namespace ProductOrderingSystem.IdentityService.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldInitialize_WithDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeNullOrEmpty();
        user.Email.Should().BeEmpty();
        user.Username.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.Roles.Should().BeEmpty();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void AddRole_ShouldAddNewRole_WhenRoleDoesNotExist()
    {
        // Arrange
        var user = new User();
        var role = "Admin";

        // Act
        user.AddRole(role);

        // Assert
        user.Roles.Should().Contain(role);
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddRole_ShouldNotAddDuplicateRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var user = new User();
        var role = "Admin";
        user.AddRole(role);
        var firstUpdateTime = user.UpdatedAt;

        // Act
        user.AddRole(role);

        // Assert
        user.Roles.Should().ContainSingle(r => r == role);
        user.UpdatedAt.Should().Be(firstUpdateTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AddRole_ShouldThrowException_WhenRoleIsNullOrWhiteSpace(string? invalidRole)
    {
        // Arrange
        var user = new User();

        // Act
        var act = () => user.AddRole(invalidRole!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Role cannot be empty*");
    }

    [Fact]
    public void RemoveRole_ShouldRemoveRole_WhenRoleExists()
    {
        // Arrange
        var user = new User();
        var role = "Admin";
        user.AddRole(role);

        // Act
        user.RemoveRole(role);

        // Assert
        user.Roles.Should().NotContain(role);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RemoveRole_ShouldDoNothing_WhenRoleDoesNotExist()
    {
        // Arrange
        var user = new User();
        var role = "Admin";

        // Act
        user.RemoveRole(role);

        // Assert
        user.Roles.Should().BeEmpty();
        user.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    public void HasRole_ShouldReturnTrue_WhenRoleExistsRegardlessOfCase(string roleToCheck)
    {
        // Arrange
        var user = new User();
        user.AddRole("Admin");

        // Act
        var result = user.HasRole(roleToCheck);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasRole_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        // Arrange
        var user = new User();
        user.AddRole("User");

        // Act
        var result = user.HasRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue_WhenUserIsInactive()
    {
        // Arrange
        var user = new User { IsActive = false };

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_ShouldDoNothing_WhenUserIsAlreadyActive()
    {
        // Arrange
        var user = new User { IsActive = true };

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse_WhenUserIsActive()
    {
        // Arrange
        var user = new User { IsActive = true };

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_ShouldDoNothing_WhenUserIsAlreadyInactive()
    {
        // Arrange
        var user = new User { IsActive = false, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        var previousUpdateTime = user.UpdatedAt;

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().Be(previousUpdateTime);
    }
}
