using AwesomeAssertions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

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
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.StockQuantity.Should().Be(command.StockQuantity);
        result.Category.Should().Be(command.Category);
        result.ImageUrl.Should().Be(command.ImageUrl);
        result.IsActive.Should().BeTrue();

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
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Name.Should().Be(command.Name);
        capturedProduct.Description.Should().Be(command.Description);
        capturedProduct.Price.Should().Be(command.Price);
        capturedProduct.StockQuantity.Should().Be(command.StockQuantity);
        capturedProduct.Category.Should().Be(command.Category);
        capturedProduct.ImageUrl.Should().Be(command.ImageUrl);
    }

    [Fact]
    public async Task Handle_WithInvalidPrice_ShouldThrowValidationException()
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProductValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Errors.Should().ContainKey("Price");
        exception.Errors["Price"].Should().Contain("Product price must be greater than zero");
    }

    [Fact]
    public async Task Handle_WithInvalidStockQuantity_ShouldThrowValidationException()
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProductValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Errors.Should().ContainKey("StockQuantity");
        exception.Errors["StockQuantity"].Should().Contain("Stock quantity cannot be negative");
    }
}
