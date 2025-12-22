using AwesomeAssertions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

namespace ProductOrderingSystem.ProductService.Application.UnitTests;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new UpdateProductCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndExistingProduct_ShouldUpdateProductAndReturnIt()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Product",
            "Updated Description",
            29.99m,
            200,
            "Updated Category",
            "https://example.com/updated-image.jpg"
        );

        _mockRepository
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync(existingProduct);

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Product>()))
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

        _mockRepository.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(existingProduct), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new UpdateProductCommand(
            "non-existent-id",
            "Updated Product",
            "Updated Description",
            29.99m,
            200,
            "Updated Category",
            "https://example.com/updated-image.jpg"
        );

        _mockRepository
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ProductId.Should().Be(command.Id);
        _mockRepository.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    private static Product CreateTestProduct()
    {
        return new Product(
            "Original Product",
            "Original Description",
            19.99m,
            100,
            "Original Category",
            "https://example.com/original-image.jpg"
        );
    }
}
