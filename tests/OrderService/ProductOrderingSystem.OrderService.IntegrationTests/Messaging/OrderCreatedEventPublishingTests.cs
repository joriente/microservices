using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProductOrderingSystem.Shared.Contracts.Events;
using Xunit;

namespace ProductOrderingSystem.OrderService.IntegrationTests.Messaging;

/// <summary>
/// Tests for OrderCreatedEvent publishing using MassTransit In-Memory Test Harness.
/// These tests run fast without needing real RabbitMQ infrastructure.
/// </summary>
public class OrderCreatedEventPublishingTests
{
    [Fact]
    public async Task Should_Publish_OrderCreatedEvent_Successfully()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var orderCreatedEvent = new OrderCreatedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            Items: new List<OrderItemDto>
            {
                new(
                    ProductId: productId,
                    Quantity: 2,
                    UnitPrice: 99.99m
                )
            },
            TotalAmount: 199.98m,
            CreatedAt: DateTime.UtcNow
        );

        // Act
        await harness.Bus.Publish(orderCreatedEvent);

        // Assert
        Assert.True(await harness.Published.Any<OrderCreatedEvent>());
        
        var publishedMessage = harness.Published.Select<OrderCreatedEvent>().FirstOrDefault();
        Assert.NotNull(publishedMessage);
        Assert.Equal(orderId, publishedMessage.Context.Message.OrderId);
        Assert.Equal(customerId, publishedMessage.Context.Message.CustomerId);
        Assert.Single(publishedMessage.Context.Message.Items);
    }

    [Fact]
    public async Task Should_Publish_Multiple_OrderCreatedEvents()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // Act - Publish 3 events
        for (int i = 0; i < 3; i++)
        {
            await harness.Bus.Publish(new OrderCreatedEvent(
                OrderId: Guid.NewGuid(),
                CustomerId: Guid.NewGuid(),
                Items: new List<OrderItemDto>(),
                TotalAmount: 100m * (i + 1),
                CreatedAt: DateTime.UtcNow
            ));
        }

        // Assert
        var published = harness.Published.Select<OrderCreatedEvent>().ToArray();
        Assert.Equal(3, published.Length);
    }

    [Fact]
    public async Task Should_Verify_Event_Properties()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var expectedTotalAmount = 299.97m;

        // Act
        await harness.Bus.Publish(new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            Items: new List<OrderItemDto>
            {
                new(ProductId: Guid.NewGuid(), Quantity: 1, UnitPrice: 99.99m),
                new(ProductId: Guid.NewGuid(), Quantity: 2, UnitPrice: 99.99m)
            },
            TotalAmount: expectedTotalAmount,
            CreatedAt: DateTime.UtcNow
        ));

        // Assert
        var publishedEvent = harness.Published.Select<OrderCreatedEvent>().First();
        
        Assert.Equal(expectedTotalAmount, publishedEvent.Context.Message.TotalAmount);
        Assert.Equal(2, publishedEvent.Context.Message.Items.Count);
    }
}
