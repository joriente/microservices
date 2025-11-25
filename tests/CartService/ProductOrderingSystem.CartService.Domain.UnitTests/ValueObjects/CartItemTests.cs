using AwesomeAssertions;
using ProductOrderingSystem.CartService.Domain.Entities;

namespace ProductOrderingSystem.CartService.Domain.UnitTests.ValueObjects;

public class CartItemTests
{
    [Fact]
    public void Constructor_ShouldCreateCartItem_WithValidParameters()
    {
        // Arrange & Act
        var item = new CartItem("prod123", "Test Product", 19.99m, 5);

        // Assert
        item.ProductId.Should().Be("prod123");
        item.ProductName.Should().Be("Test Product");
        item.Price.Should().Be(19.99m);
        item.Quantity.Should().Be(5);
        item.TotalPrice.Should().Be(99.95m); // 19.99 * 5
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowArgumentException_WhenProductIdIsInvalid(string? invalidProductId)
    {
        // Act
        Action act = () => new CartItem(invalidProductId!, "Product", 10.0m, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowArgumentException_WhenProductNameIsInvalid(string? invalidProductName)
    {
        // Act
        Action act = () => new CartItem("prod123", invalidProductName!, 10.0m, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product name cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Constructor_ShouldThrowArgumentException_WhenPriceIsInvalid(decimal invalidPrice)
    {
        // Act
        Action act = () => new CartItem("prod123", "Product", invalidPrice, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Price must be greater than zero*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Constructor_ShouldThrowArgumentException_WhenQuantityIsInvalid(int invalidQuantity)
    {
        // Act
        Action act = () => new CartItem("prod123", "Product", 10.0m, invalidQuantity);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void TotalPrice_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var item1 = new CartItem("prod1", "Product 1", 10.50m, 3);
        var item2 = new CartItem("prod2", "Product 2", 99.99m, 2);
        var item3 = new CartItem("prod3", "Product 3", 5.00m, 10);

        // Assert
        item1.TotalPrice.Should().Be(31.50m);
        item2.TotalPrice.Should().Be(199.98m);
        item3.TotalPrice.Should().Be(50.00m);
    }
}
