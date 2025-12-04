using MassTransit;using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductOrderingSystem.CartService.Application.Consumers;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;
using Xunit;

namespace ProductOrderingSystem.CartService.IntegrationTests.Messaging;

/// <summary>
/// Tests for ProductCreatedEventConsumer using MassTransit In-Memory Test Harness.
/// Shows how to test event consumers without external dependencies.
/// </summary>
public class ProductCreatedEventConsumerTests
{
    [Fact]
    public async Task Consumer_Should_Process_ProductCreatedEvent()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductCacheRepository>();
        mockRepository.Setup(r => r.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<ProductCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ProductCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var productId = Guid.NewGuid().ToString();
        var productEvent = new ProductCreatedEvent(
            ProductId: productId,
            Name: "New Product",
            Price: 49.99m,
            StockQuantity: 100,
            CreatedAt: DateTime.UtcNow
        );

        // Act
        await harness.Bus.Publish(productEvent);

        // Assert - Verify event was consumed
        Assert.True(await harness.Consumed.Any<ProductCreatedEvent>());

        var consumerHarness = harness.GetConsumerHarness<ProductCreatedEventConsumer>();
        Assert.True(await consumerHarness.Consumed.Any<ProductCreatedEvent>());

        var consumedMessage = consumerHarness.Consumed.Select<ProductCreatedEvent>().First();
        Assert.Equal(productId, consumedMessage.Context.Message.ProductId);
        Assert.Equal("New Product", consumedMessage.Context.Message.Name);
        Assert.Equal(49.99m, consumedMessage.Context.Message.Price);
    }

    [Fact]
    public async Task Consumer_Should_Process_Batch_Of_ProductCreatedEvents()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductCacheRepository>();
        mockRepository.Setup(r => r.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<ProductCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ProductCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var productCount = 10;
        var productIds = new List<string>();

        // Act - Publish batch of product events
        for (int i = 0; i < productCount; i++)
        {
            var productId = Guid.NewGuid().ToString();
            productIds.Add(productId);
            
            await harness.Bus.Publish(new ProductCreatedEvent(
                ProductId: productId,
                Name: $"Product {i}",
                Price: 10m * (i + 1),
                StockQuantity: 100 * (i + 1),
                CreatedAt: DateTime.UtcNow
            ));
        }

        // Assert
        var consumerHarness = harness.GetConsumerHarness<ProductCreatedEventConsumer>();
        
        // Wait for all messages
        Assert.True(await consumerHarness.Consumed.Any<ProductCreatedEvent>(x => x.Context.Message.Name == "Product 9"));
        
        var consumed = consumerHarness.Consumed.Select<ProductCreatedEvent>().ToArray();
        Assert.Equal(productCount, consumed.Length);
    }

    [Fact]
    public async Task Should_Verify_Event_Timing()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductCacheRepository>();
        mockRepository.Setup(r => r.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<ProductCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ProductCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var beforePublish = DateTime.UtcNow;

        // Act
        await harness.Bus.Publish(new ProductCreatedEvent(
            ProductId: Guid.NewGuid().ToString(),
            Name: "Timed Product",
            Price: 99.99m,
            StockQuantity: 50,
            CreatedAt: DateTime.UtcNow
        ));

        // Assert
        var consumerHarness = harness.GetConsumerHarness<ProductCreatedEventConsumer>();
        Assert.True(await consumerHarness.Consumed.Any<ProductCreatedEvent>());

        var consumedEvent = consumerHarness.Consumed.Select<ProductCreatedEvent>().First();
        var eventTime = consumedEvent.Context.Message.CreatedAt;
        
        // Verify event was created recently
        Assert.True(eventTime >= beforePublish);
        Assert.True((DateTime.UtcNow - eventTime).TotalSeconds < 5);
    }
}
