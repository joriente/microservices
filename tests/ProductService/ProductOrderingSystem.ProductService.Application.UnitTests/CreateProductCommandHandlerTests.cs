using ErrorOr;
using AwesomeAssertions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.UnitTests;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new CreateProductCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateProductAndReturnIt()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description", 
            19.99m,
            100,
            "Electronics",
            "https://example.com/image.jpg"
        );

        var expectedProduct = new Product(
            command.Name,
            command.Description,
            command.Price,
            command.StockQuantity,
            command.Category,
            command.ImageUrl
        );

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Description.Should().Be(command.Description);
        result.Value.Price.Should().Be(command.Price);
        result.Value.StockQuantity.Should().Be(command.StockQuantity);
        result.Value.Category.Should().Be(command.Category);
        result.Value.ImageUrl.Should().Be(command.ImageUrl);
        result.Value.IsActive.Should().BeTrue();

        _mockRepository.Verify(x => x.CreateAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPassCorrectDataToRepository()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            29.99m,
            50,
            "Books",
            "https://example.com/book.jpg"
        );

        Product? capturedProduct = null;
        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Product>()))
            .Callback<Product>(p => capturedProduct = p)
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Name.Should().Be(command.Name);
        capturedProduct.Description.Should().Be(command.Description);
        capturedProduct.Price.Should().Be(command.Price);
        capturedProduct.StockQuantity.Should().Be(command.StockQuantity);
        capturedProduct.Category.Should().Be(command.Category);
        capturedProduct.ImageUrl.Should().Be(command.ImageUrl);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailureError()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            19.99m,
            100,
            "Electronics",
            "https://example.com/image.jpg"
        );

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Product>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("Failed to create product: Database error");
    }

    [Fact]
    public async Task Handle_WithInvalidPrice_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            -10.00m, // Invalid negative price
            100,
            "Electronics",
            "https://example.com/image.jpg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Be("Product price must be greater than zero");
    }

    [Fact]
    public async Task Handle_WithInvalidStockQuantity_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            19.99m,
            -5, // Invalid negative stock
            "Electronics",
            "https://example.com/image.jpg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Be("Stock quantity cannot be negative");
    }
}
