using AwesomeAssertions;
using ProductOrderingSystem.CartService.Domain.Entities;

namespace ProductOrderingSystem.CartService.Domain.UnitTests.Entities;

public class CartTests
{
    [Fact]
    public void Constructor_ShouldCreateCart_WithValidParameters()
    {
        // Arrange
        var customerId = "customer123";
        var customerEmail = "customer@example.com";

        // Act
        var cart = new Cart(customerId, customerEmail);

        // Assert
        cart.Should().NotBeNull();
        cart.CustomerId.Should().Be(customerId);
        cart.CustomerEmail.Should().Be(customerEmail);
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0);
        cart.TotalItems.Should().Be(0);
        cart.Id.Should().NotBeNullOrWhiteSpace();
        cart.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowArgumentException_WhenCustomerIdIsInvalid(string? invalidCustomerId)
    {
        // Act
        Action act = () => new Cart(invalidCustomerId!, "email@example.com");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Customer ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowArgumentException_WhenCustomerEmailIsInvalid(string? invalidEmail)
    {
        // Act
        Action act = () => new Cart("customer123", invalidEmail!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Customer email cannot be empty*");
    }

    [Fact]
    public void AddItem_ShouldAddNewItem_WhenProductNotInCart()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.Items[0].ProductId.Should().Be("prod1");
        cart.Items[0].ProductName.Should().Be("Product 1");
        cart.Items[0].Quantity.Should().Be(2);
        cart.TotalAmount.Should().Be(20.0m);
        cart.TotalItems.Should().Be(2);
    }

    [Fact]
    public void AddItem_ShouldIncreaseQuantity_WhenProductAlreadyInCart()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        cart.AddItem("prod1", "Product 1", 10.0m, 3);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5); // 2 + 3
        cart.TotalAmount.Should().Be(50.0m); // 10 * 5
        cart.TotalItems.Should().Be(5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_ShouldThrowArgumentException_WhenProductIdIsInvalid(string? invalidProductId)
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        Action act = () => cart.AddItem(invalidProductId!, "Product", 10.0m, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product ID cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AddItem_ShouldThrowArgumentException_WhenPriceIsInvalid(decimal invalidPrice)
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        Action act = () => cart.AddItem("prod1", "Product", invalidPrice, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Price must be greater than zero*");
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItem_WhenProductExists()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        cart.RemoveItem("prod1");

        // Assert
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0);
        cart.TotalItems.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_ShouldThrowInvalidOperationException_WhenProductDoesNotExist()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        Action act = () => cart.RemoveItem("prod2");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found in cart*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveItem_ShouldThrowArgumentException_WhenProductIdIsInvalid(string? invalidProductId)
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        Action act = () => cart.RemoveItem(invalidProductId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product ID cannot be empty*");
    }

    [Fact]
    public void UpdateItemQuantity_ShouldUpdateQuantity_WhenProductExists()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        cart.UpdateItemQuantity("prod1", 5);

        // Assert
        cart.Items[0].Quantity.Should().Be(5);
        cart.TotalAmount.Should().Be(50.0m);
        cart.TotalItems.Should().Be(5);
    }

    [Fact]
    public void UpdateItemQuantity_ShouldThrowInvalidOperationException_WhenProductDoesNotExist()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        Action act = () => cart.UpdateItemQuantity("prod1", 5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found in cart*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void UpdateItemQuantity_ShouldThrowArgumentException_WhenQuantityIsInvalid(int invalidQuantity)
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        Action act = () => cart.UpdateItemQuantity("prod1", invalidQuantity);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);
        cart.AddItem("prod2", "Product 2", 15.0m, 1);

        // Act
        cart.Clear();

        // Assert
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0);
        cart.TotalItems.Should().Be(0);
    }

    [Fact]
    public void TotalAmount_ShouldBeCalculatedCorrectly_WithMultipleItems()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");

        // Act
        cart.AddItem("prod1", "Product 1", 10.50m, 2); // 21.00
        cart.AddItem("prod2", "Product 2", 15.75m, 3); // 47.25
        cart.AddItem("prod3", "Product 3", 5.25m, 4); // 21.00

        // Assert
        cart.TotalAmount.Should().Be(89.25m);
        cart.TotalItems.Should().Be(9); // 2 + 3 + 4
    }

    [Fact]
    public void TotalAmount_ShouldRecalculate_AfterItemRemoval()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);
        cart.AddItem("prod2", "Product 2", 20.0m, 1);

        // Act
        cart.RemoveItem("prod1");

        // Assert
        cart.TotalAmount.Should().Be(20.0m);
        cart.TotalItems.Should().Be(1);
    }

    [Fact]
    public void TotalAmount_ShouldRecalculate_AfterQuantityUpdate()
    {
        // Arrange
        var cart = new Cart("customer123", "customer@example.com");
        cart.AddItem("prod1", "Product 1", 10.0m, 2);

        // Act
        cart.UpdateItemQuantity("prod1", 10);

        // Assert
        cart.TotalAmount.Should().Be(100.0m);
        cart.TotalItems.Should().Be(10);
    }
}
