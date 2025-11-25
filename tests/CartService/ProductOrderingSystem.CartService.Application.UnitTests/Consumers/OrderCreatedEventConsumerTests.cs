using AwesomeAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using ProductOrderingSystem.CartService.Application.Consumers;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;
using Xunit;

namespace ProductOrderingSystem.CartService.Application.UnitTests.Consumers;

public class OrderCreatedEventConsumerTests
{
    private readonly Mock<ICartRepository> _mockRepository;
    private readonly Mock<ILogger<OrderCreatedEventConsumer>> _mockLogger;
    private readonly OrderCreatedEventConsumer _consumer;

    public OrderCreatedEventConsumerTests()
    {
        _mockRepository = new Mock<ICartRepository>();
        _mockLogger = new Mock<ILogger<OrderCreatedEventConsumer>>();
        _consumer = new OrderCreatedEventConsumer(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_ShouldClearCart_WhenOrderCreated()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCreatedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            Items: new List<OrderItemDto>
            {
                new OrderItemDto(Guid.NewGuid(), 2, 99.99m)
            },
            TotalAmount: 199.98m,
            CreatedAt: DateTime.UtcNow
        );

        var cart = new Cart(customerId.ToString(), "test@example.com");
        cart.AddItem("product-1", "Test Product", 99.99m, 2);

        var mockContext = new Mock<ConsumeContext<OrderCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(orderEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.GetByCustomerIdAsync(customerId.ToString()))
            .ReturnsAsync(cart);

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Cart>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0);

        _mockRepository.Verify(
            x => x.GetByCustomerIdAsync(customerId.ToString()),
            Times.Once
        );

        _mockRepository.Verify(
            x => x.UpdateAsync(It.Is<Cart>(c => c.Items.Count == 0)),
            Times.Once
        );
    }

    [Fact]
    public async Task Consume_ShouldLogSuccess_WhenCartCleared()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCreatedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            Items: new List<OrderItemDto>(),
            TotalAmount: 0,
            CreatedAt: DateTime.UtcNow
        );

        var cart = new Cart(customerId.ToString(), "test@example.com");

        var mockContext = new Mock<ConsumeContext<OrderCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(orderEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.GetByCustomerIdAsync(customerId.ToString()))
            .ReturnsAsync(cart);

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Cart>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully cleared cart")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Consume_ShouldLogWarning_WhenCartNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCreatedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            Items: new List<OrderItemDto>(),
            TotalAmount: 0,
            CreatedAt: DateTime.UtcNow
        );

        var mockContext = new Mock<ConsumeContext<OrderCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(orderEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.GetByCustomerIdAsync(customerId.ToString()))
            .ReturnsAsync((Cart?)null);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No cart found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );

        _mockRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Cart>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Consume_ShouldNotThrow_WhenRepositoryFails()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCreatedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            Items: new List<OrderItemDto>(),
            TotalAmount: 0,
            CreatedAt: DateTime.UtcNow
        );

        var mockContext = new Mock<ConsumeContext<OrderCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(orderEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.GetByCustomerIdAsync(customerId.ToString()))
            .ThrowsAsync(new Exception("Database error"));

        // Act - should not throw (non-critical operation)
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error clearing cart")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}
