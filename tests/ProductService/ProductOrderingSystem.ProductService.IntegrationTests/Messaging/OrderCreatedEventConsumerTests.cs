using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Consumers;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;
using Xunit;

namespace ProductOrderingSystem.ProductService.IntegrationTests.Messaging;

/// <summary>
/// Tests for OrderCreatedEventConsumer using MassTransit In-Memory Test Harness.
/// Demonstrates testing message consumption without real RabbitMQ.
/// </summary>
public class OrderCreatedEventConsumerTests
{
    [Fact]
    public async Task Consumer_Should_Consume_OrderCreatedEvent()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Product("Test Product", "Description", 10.00m, 100, "Category", "image.jpg"));
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Arrange: Configure test harness with the consumer and dependencies
        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<OrderCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderCreatedEventConsumer>();
            })
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

        // Assert - Verify the event was consumed
        Assert.True(await harness.Consumed.Any<OrderCreatedEvent>());

        // Get the consumer harness to verify consumption
        var consumerHarness = harness.GetConsumerHarness<OrderCreatedEventConsumer>();
        Assert.True(await consumerHarness.Consumed.Any<OrderCreatedEvent>());

        // Verify the message details
        var consumedMessage = consumerHarness.Consumed.Select<OrderCreatedEvent>().First();
        Assert.Equal(orderId, consumedMessage.Context.Message.OrderId);
        Assert.Equal(productId, consumedMessage.Context.Message.Items.First().ProductId);
    }

    [Fact]
    public async Task Consumer_Should_Process_Multiple_Events()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Product("Test", "Desc", 10m, 100, "Cat", "img.jpg"));
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<OrderCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // Act - Publish multiple events
        var orderIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var orderId = Guid.NewGuid();
            orderIds.Add(orderId);
            
            await harness.Bus.Publish(new OrderCreatedEvent(
                OrderId: orderId,
                CustomerId: Guid.NewGuid(),
                Items: new List<OrderItemDto>
                {
                    new(ProductId: Guid.NewGuid(), Quantity: 1, UnitPrice: 50m)
                },
                TotalAmount: 50m,
                CreatedAt: DateTime.UtcNow
            ));
        }

        // Assert
        var consumerHarness = harness.GetConsumerHarness<OrderCreatedEventConsumer>();
        
        // Wait for all messages to be consumed
        Assert.True(await consumerHarness.Consumed.Any<OrderCreatedEvent>(x => x.Context.Message.OrderId == orderIds[4]));
        
        // Verify all 5 were consumed
        var consumed = consumerHarness.Consumed.Select<OrderCreatedEvent>().ToArray();
        Assert.Equal(5, consumed.Length);
    }

    [Fact]
    public async Task Consumer_Should_Handle_Event_With_Multiple_Items()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Product("Test", "Desc", 10m, 100, "Cat", "img.jpg"));
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        await using var provider = new ServiceCollection()
            .AddSingleton(mockRepository.Object)
            .AddSingleton(NullLogger<OrderCreatedEventConsumer>.Instance)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderCreatedEventConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderEvent = new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            Items: new List<OrderItemDto>
            {
                new(ProductId: Guid.NewGuid(), Quantity: 2, UnitPrice: 10m),
                new(ProductId: Guid.NewGuid(), Quantity: 1, UnitPrice: 20m),
                new(ProductId: Guid.NewGuid(), Quantity: 3, UnitPrice: 30m)
            },
            TotalAmount: 130m, // (2*10) + (1*20) + (3*30)
            CreatedAt: DateTime.UtcNow
        );

        // Act
        await harness.Bus.Publish(orderEvent);

        // Assert
        var consumerHarness = harness.GetConsumerHarness<OrderCreatedEventConsumer>();
        Assert.True(await consumerHarness.Consumed.Any<OrderCreatedEvent>());

        var consumedEvent = consumerHarness.Consumed.Select<OrderCreatedEvent>().First();
        Assert.Equal(3, consumedEvent.Context.Message.Items.Count);
        Assert.Equal(130m, consumedEvent.Context.Message.TotalAmount);
    }
}
