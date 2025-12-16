using ErrorOr;
using AwesomeAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using ProductOrderingSystem.OrderService.Application.Commands.Orders;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.OrderService.Application.UnitTests.Commands.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductCacheRepository> _productCacheRepositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productCacheRepositoryMock = new Mock<IProductCacheRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();
        
        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productCacheRepositoryMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenRequestIsValid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var customerId = Guid.NewGuid().ToString();
        
        var cachedProduct = new ProductCacheEntry
        {
            Id = productId,
            Name = "Test Product",
            Price = 99.99m,
            IsActive = true
        };

        var command = new CreateOrderCommand(
            CustomerId: customerId,
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand>
            {
                new(ProductId: productId, ProductName: "Test Product", Price: 99.99m, Quantity: 2)
            },
            Notes: "Test order"
        );

        _productCacheRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.CustomerEmail.Should().Be(command.CustomerEmail);
        result.Value.CustomerName.Should().Be(command.CustomerName);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ProductName.Should().Be(cachedProduct.Name);
        result.Value.Items[0].UnitPrice.Should().Be(cachedProduct.Price);
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.TotalAmount.Should().Be(199.98m); // 99.99 * 2

        _orderRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ShouldReturnValidationError_WhenCustomerIdIsNullOrWhitespace(string? invalidCustomerId)
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: invalidCustomerId!,
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new("prod-1", "Test Product", 10.0m, 1) },
            Notes: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Order.CustomerId");
        result.FirstError.Description.Should().Contain("Customer ID is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ShouldReturnValidationError_WhenCustomerEmailIsNullOrWhitespace(string? invalidEmail)
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: invalidEmail!,
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new("prod-1", "Test Product", 10.0m, 1) },
            Notes: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Order.CustomerEmail");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenItemsListIsEmpty()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand>(),
            Notes: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Order.Items");
        result.FirstError.Description.Should().Contain("at least one item");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task Handle_ShouldReturnValidationError_WhenQuantityIsZeroOrNegative(int invalidQuantity)
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> 
            { 
                new(ProductId: "prod-1", ProductName: "Test Product", Price: 10.0m, Quantity: invalidQuantity) 
            },
            Notes: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("OrderItem.Quantity");
        result.FirstError.Description.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundError_WhenProductDoesNotExistInCache()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new(productId, "Test Product", 10.0m, 1) },
            Notes: null
        );

        _productCacheRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCacheEntry?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Description.Should().Contain($"Product {productId} not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenProductIsNotActive()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var cachedProduct = new ProductCacheEntry
        {
            Id = productId,
            Name = "Inactive Product",
            Price = 50.0m,
            IsActive = false
        };

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new(productId, "Inactive Product", 50.0m, 1) },
            Notes: null
        );

        _productCacheRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("not active");
        result.FirstError.Description.Should().Contain(cachedProduct.Name);
    }

    [Fact]
    public async Task Handle_ShouldUseProductCacheEntryData_NotRequestData()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var cachedProduct = new ProductCacheEntry
        {
            Id = productId,
            Name = "Cached Product Name",
            Price = 100.0m,
            IsActive = true
        };

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid().ToString(),
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new(productId, "Cached Product Name", 100.0m, 2) },
            Notes: null
        );

        _productCacheRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        Order? capturedOrder = null;
        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order order, CancellationToken _) => order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items[0].ProductName.Should().Be(cachedProduct.Name);
        capturedOrder.Items[0].UnitPrice.Should().Be(cachedProduct.Price);
    }

    [Fact]
    public async Task Handle_ShouldPublishOrderCreatedEvent_WithCorrectData()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var customerId = Guid.NewGuid().ToString();
        
        var cachedProduct = new ProductCacheEntry
        {
            Id = productId,
            Name = "Test Product",
            Price = 50.0m,
            IsActive = true
        };

        var command = new CreateOrderCommand(
            CustomerId: customerId,
            CustomerEmail: "test@example.com",
            CustomerName: "John Doe",
            Items: new List<CreateOrderItemCommand> { new(productId, "Test Product", 50.0m, 3) },
            Notes: null
        );

        _productCacheRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => order);

        OrderCreatedEvent? capturedEvent = null;
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderCreatedEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.CustomerId.Should().Be(Guid.Parse(customerId));
        capturedEvent.Items.Should().HaveCount(1);
        capturedEvent.Items[0].ProductId.Should().Be(Guid.Parse(productId));
        capturedEvent.Items[0].Quantity.Should().Be(3);
        capturedEvent.Items[0].UnitPrice.Should().Be(50.0m);
        capturedEvent.TotalAmount.Should().Be(150.0m);
    }
}
