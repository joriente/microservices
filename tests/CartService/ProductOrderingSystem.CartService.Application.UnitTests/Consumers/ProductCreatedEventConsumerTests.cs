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

public class ProductCreatedEventConsumerTests
{
    private readonly Mock<IProductCacheRepository> _mockRepository;
    private readonly Mock<ILogger<ProductCreatedEventConsumer>> _mockLogger;
    private readonly ProductCreatedEventConsumer _consumer;

    public ProductCreatedEventConsumerTests()
    {
        _mockRepository = new Mock<IProductCacheRepository>();
        _mockLogger = new Mock<ILogger<ProductCreatedEventConsumer>>();
        _consumer = new ProductCreatedEventConsumer(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_ShouldCacheProduct_WhenEventReceived()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var productEvent = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Test Product",
            Price: 99.99m,
            StockQuantity: 10,
            CreatedAt: DateTime.UtcNow
        );

        var mockContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(productEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductCacheEntry? capturedEntry = null;
        _mockRepository
            .Setup(x => x.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .Callback<ProductCacheEntry, CancellationToken>((entry, ct) => capturedEntry = entry)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockRepository.Verify(
            x => x.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        capturedEntry.Should().NotBeNull();
        capturedEntry!.Id.Should().Be(productId);
        capturedEntry.Name.Should().Be("Test Product");
        capturedEntry.Price.Should().Be(99.99m);
        capturedEntry.StockQuantity.Should().Be(10);
        capturedEntry.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Consume_ShouldLogSuccess_WhenProductCached()
    {
        // Arrange
        var productEvent = new ProductCreatedEvent(
            ProductId: Guid.NewGuid().ToString(),
            Name: "Test Product",
            Price: 99.99m,
            StockQuantity: 10,
            CreatedAt: DateTime.UtcNow
        );

        var mockContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(productEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully cached Product")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Consume_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        var productEvent = new ProductCreatedEvent(
            ProductId: Guid.NewGuid().ToString(),
            Name: "Test Product",
            Price: 99.99m,
            StockQuantity: 10,
            CreatedAt: DateTime.UtcNow
        );

        var mockContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        mockContext.Setup(x => x.Message).Returns(productEvent);
        mockContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _mockRepository
            .Setup(x => x.UpsertAsync(It.IsAny<ProductCacheEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _consumer.Consume(mockContext.Object));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error caching Product")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}
