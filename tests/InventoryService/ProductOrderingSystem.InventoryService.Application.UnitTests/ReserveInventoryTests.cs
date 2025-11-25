using AwesomeAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Features.Inventory;
using ProductOrderingSystem.InventoryService.Models;

namespace ProductOrderingSystem.InventoryService.Application.UnitTests;

public class ReserveInventoryTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<ReserveInventory.Handler>> _loggerMock;
    private readonly ReserveInventory.Handler _handler;

    public ReserveInventoryTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryDb_{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InventoryDbContext(options);
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<ReserveInventory.Handler>>();
        _handler = new ReserveInventory.Handler(_context, _publishEndpointMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReserveInventory_WhenStockAvailable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(productId, 10)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ReservationId.Should().Be(orderId);
        result.ErrorMessage.Should().BeNull();

        // Verify inventory was reserved
        var updatedItem = await _context.InventoryItems.FirstAsync(x => x.ProductId == productId);
        updatedItem.QuantityReserved.Should().Be(10);
        updatedItem.QuantityAvailable.Should().Be(90);

        // Verify reservation record was created
        var reservation = await _context.InventoryReservations.FirstOrDefaultAsync(x => x.OrderId == orderId);
        reservation.Should().NotBeNull();
        reservation!.ProductId.Should().Be(productId);
        reservation.Quantity.Should().Be(10);
        reservation.Status.Should().Be(ReservationStatus.Reserved);
    }

    [Fact]
    public async Task Handle_Should_ReserveMultipleItems()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _context.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = product1Id,
                ProductName = "Product 1",
                QuantityOnHand = 100,
                QuantityReserved = 0,
                ReorderLevel = 10,
                ReorderQuantity = 50
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = product2Id,
                ProductName = "Product 2",
                QuantityOnHand = 50,
                QuantityReserved = 0,
                ReorderLevel = 5,
                ReorderQuantity = 25
            }
        );
        await _context.SaveChangesAsync();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(product1Id, 10),
                new(product2Id, 5)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var item1 = await _context.InventoryItems.FirstAsync(x => x.ProductId == product1Id);
        item1.QuantityReserved.Should().Be(10);

        var item2 = await _context.InventoryItems.FirstAsync(x => x.ProductId == product2Id);
        item2.QuantityReserved.Should().Be(5);

        var reservations = await _context.InventoryReservations
            .Where(x => x.OrderId == orderId)
            .ToListAsync();
        reservations.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenInsufficientStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Low Stock Product",
            QuantityOnHand = 5,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(productId, 10) // Requesting more than available
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient inventory");

        // Verify inventory was NOT reserved (rollback worked)
        var item = await _context.InventoryItems.FirstAsync(x => x.ProductId == productId);
        item.QuantityReserved.Should().Be(0);
        item.QuantityOnHand.Should().Be(5); // Unchanged

        // Verify no reservation record was created
        var reservation = await _context.InventoryReservations.FirstOrDefaultAsync(x => x.OrderId == orderId);
        reservation.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(nonExistentProductId, 10)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // NOTE: This test requires a real database to test transaction rollback behavior.
    // InMemory database doesn't support transactions, so this test is commented out.
    // This scenario is covered in integration tests with Testcontainers + PostgreSQL.
    /*
    [Fact]
    public async Task Handle_Should_RollbackAllChanges_WhenOneItemFails()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // First product has enough stock, second doesn't
        _context.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = product1Id,
                ProductName = "Product 1",
                QuantityOnHand = 100,
                QuantityReserved = 0,
                ReorderLevel = 10,
                ReorderQuantity = 50
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = product2Id,
                ProductName = "Product 2",
                QuantityOnHand = 5,
                QuantityReserved = 0,
                ReorderLevel = 5,
                ReorderQuantity = 25
            }
        );
        await _context.SaveChangesAsync();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(product1Id, 10),
                new(product2Id, 10) // This will fail
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();

        // Verify NEITHER product was reserved (transaction rollback)
        var item1 = await _context.InventoryItems.FirstAsync(x => x.ProductId == product1Id);
        item1.QuantityReserved.Should().Be(0);

        var item2 = await _context.InventoryItems.FirstAsync(x => x.ProductId == product2Id);
        item2.QuantityReserved.Should().Be(0);

        // Verify no reservations were created
        var reservations = await _context.InventoryReservations
            .Where(x => x.OrderId == orderId)
            .ToListAsync();
        reservations.Should().BeEmpty();
    }
    */

    [Fact]
    public async Task Handle_Should_SetReservationExpiration()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 100,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        var command = new ReserveInventory.Command(
            orderId,
            new List<ReserveInventory.ReservationItem>
            {
                new(productId, 10)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var reservation = await _context.InventoryReservations.FirstAsync(x => x.OrderId == orderId);
        reservation.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        reservation.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
