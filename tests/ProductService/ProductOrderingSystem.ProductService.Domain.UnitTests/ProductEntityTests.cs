using AwesomeAssertions;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Events;

namespace ProductOrderingSystem.ProductService.Domain.UnitTests;

public class ProductEntityTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var price = 19.99m;
        var stockQuantity = 100;
        var category = "Electronics";
        var imageUrl = "https://example.com/image.jpg";

        // Act
        var product = new Product(name, description, price, stockQuantity, category, imageUrl);

        // Assert
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.StockQuantity.Should().Be(stockQuantity);
        product.Category.Should().Be(category);
        product.ImageUrl.Should().Be(imageUrl);
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBeNullOrEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Check domain event
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        var action = () => new Product(invalidName, "Description", 10.0m, 5, "Category", "url");
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Product name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithNegativePrice_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Product("Name", "Description", -1.0m, 5, "Category", "url");
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Price cannot be negative*");
    }

    [Fact]
    public void Constructor_WithNegativeStock_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Product("Name", "Description", 10.0m, -1, "Category", "url");
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Stock quantity cannot be negative*");
    }

    [Fact]
    public void UpdateProduct_WithValidData_ShouldUpdateProductAndRaiseDomainEvent()
    {
        // Arrange
        var product = CreateValidProduct();
        var originalCreatedAt = product.CreatedAt;
        var newName = "Updated Product";
        var newDescription = "Updated Description";
        var newPrice = 29.99m;
        var newStockQuantity = 200;
        var newCategory = "Updated Category";
        var newImageUrl = "https://example.com/new-image.jpg";

        // Act
        product.UpdateProduct(newName, newDescription, newPrice, newStockQuantity, newCategory, newImageUrl);

        // Assert
        product.Name.Should().Be(newName);
        product.Description.Should().Be(newDescription);
        product.Price.Should().Be(newPrice);
        product.StockQuantity.Should().Be(newStockQuantity);
        product.Category.Should().Be(newCategory);
        product.ImageUrl.Should().Be(newImageUrl);
        product.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        product.UpdatedAt.Should().BeAfter(originalCreatedAt);
        
        // Check domain events (should have created + updated)
        product.DomainEvents.Should().HaveCount(2);
        product.DomainEvents.Last().Should().BeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_ShouldReduceStockAndRaiseDomainEvent()
    {
        // Arrange
        var product = CreateValidProduct();
        var initialStock = product.StockQuantity;
        var quantityToReserve = 5;

        // Act
        product.ReserveStock(quantityToReserve);

        // Assert
        product.StockQuantity.Should().Be(initialStock - quantityToReserve);
        product.DomainEvents.Should().Contain(e => e is ProductStockReservedEvent);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var product = CreateValidProduct();
        var quantityToReserve = product.StockQuantity + 1;

        // Act & Assert
        var action = () => product.ReserveStock(quantityToReserve);
        
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Insufficient stock. Available: {product.StockQuantity}, Required: {quantityToReserve}");
    }

    [Fact]
    public void ReleaseStock_WithValidQuantity_ShouldIncreaseStockAndRaiseDomainEvent()
    {
        // Arrange
        var product = CreateValidProduct();
        var initialStock = product.StockQuantity;
        var quantityToRelease = 10;

        // Act
        product.ReleaseStock(quantityToRelease);

        // Assert
        product.StockQuantity.Should().Be(initialStock + quantityToRelease);
        product.DomainEvents.Should().Contain(e => e is ProductStockReleasedEvent);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndRaiseDomainEvent()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.Deactivate();

        // Assert
        product.IsActive.Should().BeFalse();
        product.DomainEvents.Should().Contain(e => e is ProductDeactivatedEvent);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndRaiseDomainEvent()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Deactivate(); // First deactivate it

        // Act
        product.Activate();

        // Assert
        product.IsActive.Should().BeTrue();
        product.DomainEvents.Should().Contain(e => e is ProductActivatedEvent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveStock_WithInvalidQuantity_ShouldThrowArgumentException(int invalidQuantity)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var action = () => product.ReserveStock(invalidQuantity);
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseStock_WithInvalidQuantity_ShouldThrowArgumentException(int invalidQuantity)
    {
        // Arrange
        var product = CreateValidProduct();

        // Act & Assert
        var action = () => product.ReleaseStock(invalidQuantity);
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be positive*");
    }

    [Fact]
    public void DomainEvents_CanBeClearedSuccessfully()
    {
        // Arrange
        var product = CreateValidProduct();
        product.DomainEvents.Should().HaveCount(1); // From constructor

        // Act
        product.ClearDomainEvents();

        // Assert
        product.DomainEvents.Should().BeEmpty();
    }

    private static Product CreateValidProduct()
    {
        return new Product(
            "Test Product",
            "Test Description",
            19.99m,
            100,
            "Electronics",
            "https://example.com/image.jpg"
        );
    }
}
