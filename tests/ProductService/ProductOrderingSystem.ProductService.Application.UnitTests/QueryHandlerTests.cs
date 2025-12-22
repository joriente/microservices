using AwesomeAssertions;
using Moq;
using ProductOrderingSystem.ProductService.Application.Queries.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

namespace ProductOrderingSystem.ProductService.Application.UnitTests;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new GetProductByIdQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProductId_ShouldReturnProduct()
    {
        // Arrange
        var product = CreateTestProduct();
        var query = new GetProductByIdQuery(product.Id);

        _mockRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
        result.Description.Should().Be(product.Description);
        result.Price.Should().Be(product.Price);

        _mockRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProductId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = "non-existent-id";
        var query = new GetProductByIdQuery(nonExistentId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        exception.ProductId.Should().Be(nonExistentId);
        _mockRepository.Verify(x => x.GetByIdAsync(nonExistentId), Times.Once);
    }

    private static Product CreateTestProduct()
    {
        return new Product(
            "Test Product",
            "Test Description",
            19.99m,
            100,
            "Electronics",
            "https://example.com/image.jpg"
        );
    }
}

public class SearchProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly SearchProductsQueryHandler _handler;

    public SearchProductsQueryHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new SearchProductsQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidSearchQuery_ShouldReturnSearchResults()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct("Product 1", "Electronics"),
            CreateTestProduct("Product 2", "Electronics")
        };
        var totalCount = 10;

        var query = new SearchProductsQuery(
            SearchTerm: "test",
            Category: "Electronics",
            MinPrice: 10m,
            MaxPrice: 100m,
            Page: 1,
            PageSize: 5
        );

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                query.Category,
                query.MinPrice,
                query.MaxPrice,
                query.Page,
                query.PageSize))
            .ReturnsAsync((products, totalCount));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().HaveCount(2);
        result.TotalCount.Should().Be(totalCount);
        result.Page.Should().Be(query.Page);
        result.PageSize.Should().Be(query.PageSize);

        _mockRepository.Verify(x => x.SearchAsync(
            query.SearchTerm,
            query.Category,
            query.MinPrice,
            query.MaxPrice,
            query.Page,
            query.PageSize), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ShouldReturnEmptySearchResults()
    {
        // Arrange
        var emptyProducts = new List<Product>();
        var totalCount = 0;

        var query = new SearchProductsQuery(
            SearchTerm: "non-existent",
            Category: null,
            MinPrice: null,
            MaxPrice: null,
            Page: 1,
            PageSize: 10
        );

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                query.Category,
                query.MinPrice,
                query.MaxPrice,
                query.Page,
                query.PageSize))
            .ReturnsAsync((emptyProducts, totalCount));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(query.Page);
        result.PageSize.Should().Be(query.PageSize);
    }

    private static Product CreateTestProduct(string name, string category)
    {
        return new Product(
            name,
            "Test Description",
            19.99m,
            100,
            category,
            "https://example.com/image.jpg"
        );
    }
}
