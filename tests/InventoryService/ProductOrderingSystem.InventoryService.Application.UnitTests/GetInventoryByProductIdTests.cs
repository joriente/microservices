using AwesomeAssertions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Features.Inventory;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Application.UnitTests;

public class GetInventoryByProductIdTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly GetInventoryByProductId.Handler _handler;

    public GetInventoryByProductIdTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryDb_{Guid.NewGuid()}")
            .Options;

        _context = new InventoryDbContext(options);
        _handler = new GetInventoryByProductId.Handler(_context);
    }

    [Fact]
    public async Task Handle_Should_ReturnInventoryItem_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 20,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var query = new GetInventoryByProductId.Query(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ProductId.Should().Be(productId);
        result.Value.ProductName.Should().Be("Test Product");
        result.Value.QuantityOnHand.Should().Be(100);
        result.Value.QuantityReserved.Should().Be(20);
        result.Value.AvailableQuantity.Should().Be(80); // 100 - 20
        result.Value.ReorderLevel.Should().Be(10);
        result.Value.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var query = new GetInventoryByProductId.Query(nonExistentProductId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Should_CalculateAvailableQuantityCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 50,
            QuantityReserved = 30,
            ReorderLevel = 5,
            ReorderQuantity = 25
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var query = new GetInventoryByProductId.Query(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AvailableQuantity.Should().Be(20); // 50 - 30
    }

    [Fact]
    public async Task Handle_Should_IndicateLowStock_WhenAvailableQuantityBelowReorderLevel()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Low Stock Product",
            QuantityOnHand = 15,
            QuantityReserved = 10,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var query = new GetInventoryByProductId.Query(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AvailableQuantity.Should().Be(5); // 15 - 10
        result.Value.IsLowStock.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
