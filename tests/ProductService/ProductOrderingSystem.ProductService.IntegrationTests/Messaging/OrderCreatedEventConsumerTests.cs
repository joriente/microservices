using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Consumers;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;
using Wolverine;
using Xunit;

namespace ProductOrderingSystem.ProductService.IntegrationTests.Messaging;

/// <summary>
/// Tests for OrderCreatedEventConsumer using Wolverine.
/// Tests the handler directly without requiring a test harness.
/// </summary>
public class OrderCreatedEventConsumerTests
{
    [Fact]
    public async Task Consumer_Should_Handle_OrderCreatedEvent()
    {
        // Arrange: Set up mock repository
        var mockRepository = new Mock<IProductRepository>();
        var product = new Product("Test Product", "Description", 10.00m, 100, "Category", "image.jpg");
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(product);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Arrange: Set up mock message bus
        var mockMessageBus = new Mock<IMessageBus>();
        
        // Arrange: Create the consumer
        var consumer = new OrderCreatedEventConsumer(
            NullLogger<OrderCreatedEventConsumer>.Instance,
            mockRepository.Object,
            mockMessageBus.Object);

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
        await consumer.Handle(orderCreatedEvent);

        // Assert - Verify repository methods were called
        mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.AtLeastOnce);
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.AtLeastOnce);
        
        // Verify a ProductReservedEvent was published
        mockMessageBus.Verify(
            m => m.PublishAsync(It.IsAny<ProductReservedEvent>()),
            Times.Once);
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

        var mockMessageBus = new Mock<IMessageBus>();
        
        var consumer = new OrderCreatedEventConsumer(
            NullLogger<OrderCreatedEventConsumer>.Instance,
            mockRepository.Object,
            mockMessageBus.Object);

        // Act - Process multiple events
        var orderIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var orderId = Guid.NewGuid();
            orderIds.Add(orderId);
            
            await consumer.Handle(new OrderCreatedEvent(
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
        // Verify all 5 events were processed
        mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Exactly(5));
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Exactly(5));
        mockMessageBus.Verify(
            m => m.PublishAsync(It.IsAny<ProductReservedEvent>()),
            Times.Exactly(5));
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

        var mockMessageBus = new Mock<IMessageBus>();
        
        var consumer = new OrderCreatedEventConsumer(
            NullLogger<OrderCreatedEventConsumer>.Instance,
            mockRepository.Object,
            mockMessageBus.Object);

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
        await consumer.Handle(orderEvent);

        // Assert
        // Should have tried to reserve stock for all 3 items
        mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Exactly(3));
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Exactly(3));
        
        // Should publish a ProductReservedEvent for each item
        mockMessageBus.Verify(
            m => m.PublishAsync(It.IsAny<ProductReservedEvent>()),
            Times.Exactly(3));
    }
}
