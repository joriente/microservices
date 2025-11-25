using MongoDB.Driver;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.IntegrationTests;

public class InventoryItemTests
{
    [Fact]
    public void Reserve_WithSufficientStock_ShouldReduceAvailableQuantity()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        item.Reserve(25);

        // Assert
        item.QuantityReserved.Should().Be(25);
        item.QuantityAvailable.Should().Be(75);
        item.QuantityOnHand.Should().Be(100);
    }

    [Fact]
    public void Reserve_WithInsufficientStock_ShouldThrowException()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 10,
            QuantityReserved = 0,
            ReorderLevel = 5,
            ReorderQuantity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        var action = () => item.Reserve(15);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient inventory*");
    }

    [Fact]
    public void Release_ShouldIncreaseAvailableQuantity()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 25,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        item.Release(10);

        // Assert
        item.QuantityReserved.Should().Be(15);
        item.QuantityAvailable.Should().Be(85);
    }

    [Fact]
    public void Fulfill_ShouldReduceOnHandAndReserved()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 25,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        item.Fulfill(20);

        // Assert
        item.QuantityOnHand.Should().Be(80);
        item.QuantityReserved.Should().Be(5);
        item.QuantityAvailable.Should().Be(75);
    }

    [Fact]
    public void Restock_ShouldIncreaseOnHandQuantity()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 10,
            QuantityReserved = 5,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        item.Restock(50);

        // Assert
        item.QuantityOnHand.Should().Be(60);
        item.QuantityAvailable.Should().Be(55);
        item.LastRestockedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsLowStock_WhenBelowReorderLevel_ShouldReturnTrue()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 8,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var isLowStock = item.IsLowStock();

        // Assert
        isLowStock.Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_WhenAboveReorderLevel_ShouldReturnFalse()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 50,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var isLowStock = item.IsLowStock();

        // Assert
        isLowStock.Should().BeFalse();
    }

    [Fact]
    public void QuantityAvailable_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 30,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        item.QuantityAvailable.Should().Be(70);
    }
}
