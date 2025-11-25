using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using ProductOrderingSystem.InventoryService.Features.EventConsumers;
using ProductOrderingSystem.InventoryService.Models;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.InventoryService.IntegrationTests;

public class ProductCreatedEventConsumerTests
{
    [Fact]
    public async Task Consume_WithNewProduct_ShouldCreateInventoryItem()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<InventoryItem>>();
        var database = Substitute.For<IMongoDatabase>();
        var logger = Substitute.For<ILogger<ProductCreatedEventConsumer>>();
        var context = Substitute.For<ConsumeContext<ProductCreatedEvent>>();

        database.GetCollection<InventoryItem>("inventory").Returns(collection);

        var productId = Guid.NewGuid();
        var productEvent = new ProductCreatedEvent(
            productId.ToString(),
            "Test Product",
            99.99m,
            100,
            DateTime.UtcNow
        );

        context.Message.Returns(productEvent);

        // Mock Find to return null (product doesn't exist)
        var cursor = Substitute.For<IAsyncCursor<InventoryItem>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        
        collection.FindAsync(
            Arg.Any<FilterDefinition<InventoryItem>>(),
            Arg.Any<FindOptions<InventoryItem>>(),
            Arg.Any<CancellationToken>()
        ).Returns(cursor);

        var consumer = new ProductCreatedEventConsumer(database, logger);

        // Act
        await consumer.Consume(context);

        // Assert
        await collection.Received(1).InsertOneAsync(
            Arg.Is<InventoryItem>(i => 
                i.ProductId == productId &&
                i.ProductName == "Test Product" &&
                i.QuantityOnHand == 0),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Consume_WithExistingProduct_ShouldNotCreateDuplicate()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<InventoryItem>>();
        var database = Substitute.For<IMongoDatabase>();
        var logger = Substitute.For<ILogger<ProductCreatedEventConsumer>>();
        var context = Substitute.For<ConsumeContext<ProductCreatedEvent>>();

        database.GetCollection<InventoryItem>("inventory").Returns(collection);

        var productId = Guid.NewGuid();
        var productEvent = new ProductCreatedEvent(
            productId.ToString(),
            "Test Product",
            99.99m,
            100,
            DateTime.UtcNow
        );

        context.Message.Returns(productEvent);

        // Mock Find to return existing item
        var existingItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 50,
            QuantityReserved = 0,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cursor = Substitute.For<IAsyncCursor<InventoryItem>>();
        cursor.Current.Returns(new[] { existingItem });
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        
        collection.FindAsync(
            Arg.Any<FilterDefinition<InventoryItem>>(),
            Arg.Any<FindOptions<InventoryItem>>(),
            Arg.Any<CancellationToken>()
        ).Returns(cursor);

        var consumer = new ProductCreatedEventConsumer(database, logger);

        // Act
        await consumer.Consume(context);

        // Assert
        await collection.DidNotReceive().InsertOneAsync(
            Arg.Any<InventoryItem>(),
            Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>()
        );
    }
}
