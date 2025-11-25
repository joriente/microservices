using AwesomeAssertions;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Application.UnitTests;

public class InventoryItemTests
{
    [Fact]
    public void Reserve_Should_IncreaseQuantityReserved()
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
            ReorderQuantity = 50
        };

        // Act
        item.Reserve(10);

        // Assert
        item.QuantityReserved.Should().Be(10);
        item.QuantityAvailable.Should().Be(90);
    }

    [Fact]
    public void Reserve_Should_ThrowException_WhenInsufficientStock()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 5,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var act = () => item.Reserve(10);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient inventory*");
    }

    [Fact]
    public void Release_Should_DecreaseQuantityReserved()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 20,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        item.Release(10);

        // Assert
        item.QuantityReserved.Should().Be(10);
        item.QuantityAvailable.Should().Be(90);
    }

    [Fact]
    public void Release_Should_ThrowException_WhenReleasingMoreThanReserved()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 5,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var act = () => item.Release(10);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot release more than reserved*");
    }

    [Fact]
    public void Fulfill_Should_DecreaseReservedAndOnHand()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 20,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        item.Fulfill(10);

        // Assert
        item.QuantityReserved.Should().Be(10);
        item.QuantityOnHand.Should().Be(90);
        item.QuantityAvailable.Should().Be(80); // 90 - 10
    }

    [Fact]
    public void Fulfill_Should_ThrowException_WhenFulfillingMoreThanReserved()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 5,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var act = () => item.Fulfill(10);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot fulfill more than reserved*");
    }

    [Fact]
    public void Restock_Should_IncreaseQuantityOnHand()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 10,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        item.Restock(50);

        // Assert
        item.QuantityOnHand.Should().Be(60);
        item.LastRestockedAt.Should().NotBeNull();
        item.LastRestockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsLowStock_Should_ReturnTrue_WhenAvailableBelowReorderLevel()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 15,
            QuantityReserved = 10,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var isLowStock = item.IsLowStock();

        // Assert
        isLowStock.Should().BeTrue(); // Available: 5, Reorder Level: 10
    }

    [Fact]
    public void IsLowStock_Should_ReturnFalse_WhenAvailableAboveReorderLevel()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 20,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var isLowStock = item.IsLowStock();

        // Assert
        isLowStock.Should().BeFalse(); // Available: 80, Reorder Level: 10
    }

    [Fact]
    public void QuantityAvailable_Should_CalculateCorrectly()
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
            ReorderQuantity = 50
        };

        // Act & Assert
        item.QuantityAvailable.Should().Be(70);
    }

    [Fact]
    public void UpdatedAt_Should_BeSet_WhenReserving()
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
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var originalUpdatedAt = item.UpdatedAt;

        // Act
        item.Reserve(10);

        // Assert
        item.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
