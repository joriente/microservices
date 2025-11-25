using AwesomeAssertions;
using ProductOrderingSystem.ProductService.Domain.Entities;

namespace ProductOrderingSystem.ProductService.Domain.UnitTests;

public class ProductEntityNullTests
{
    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Product(null!, "Description", 10.0m, 5, "Category", "url");
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("Product name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldUseEmptyString()
    {
        // Arrange & Act
        var product = new Product("Name", null!, 10.0m, 5, "Category", "url");
        
        // Assert
        product.Description.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_WithNullCategory_ShouldUseEmptyString()
    {
        // Arrange & Act
        var product = new Product("Name", "Description", 10.0m, 5, null!, "url");
        
        // Assert
        product.Category.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_WithNullImageUrl_ShouldUseEmptyString()
    {
        // Arrange & Act
        var product = new Product("Name", "Description", 10.0m, 5, "Category", null!);
        
        // Assert
        product.ImageUrl.Should().Be(string.Empty);
    }
}
