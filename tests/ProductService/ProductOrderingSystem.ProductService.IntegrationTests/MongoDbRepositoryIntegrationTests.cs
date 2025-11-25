using Microsoft.Extensions.DependencyInjection;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.IntegrationTests;

[Collection("Product Service Integration Tests")]
public class MongoDbRepositoryIntegrationTests : IClassFixture<ProductServiceWebApplicationFactory>
{
    private readonly ProductServiceWebApplicationFactory _factory;

    public MongoDbRepositoryIntegrationTests(ProductServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProductRepository_CreateAndRetrieveProduct_ShouldWorkCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        var product = new Product(
            "Repository Test Product",
            "Testing MongoDB repository operations",
            199.99m,
            25,
            "Electronics",
            "");

        // Act - Create
        await repository.CreateAsync(product);

        // Act - Retrieve
        var retrievedProduct = await repository.GetByIdAsync(product.Id);

        // Assert
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Id.Should().Be(product.Id);
        retrievedProduct.Name.Should().Be(product.Name);
        retrievedProduct.Description.Should().Be(product.Description);
        retrievedProduct.Price.Should().Be(product.Price);
        retrievedProduct.Category.Should().Be(product.Category);
        retrievedProduct.StockQuantity.Should().Be(product.StockQuantity);
    }

    [Fact]
    public async Task ProductRepository_UpdateProduct_ShouldPersistChanges()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        var product = new Product(
            "Original Product",
            "Original description",
            99.99m,
            10,
            "Books",
            "");

        await repository.CreateAsync(product);

        // Act - Update
        product.UpdateProduct(
            "Updated Product",
            "Updated description",
            199.99m,
            20,
            "Electronics",
            "");

        await repository.UpdateAsync(product);

        // Assert
        var retrievedProduct = await repository.GetByIdAsync(product.Id);
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Name.Should().Be("Updated Product");
        retrievedProduct.Description.Should().Be("Updated description");
        retrievedProduct.Price.Should().Be(199.99m);
        retrievedProduct.StockQuantity.Should().Be(20);
    }

    [Fact]
    public async Task ProductRepository_DeleteProduct_ShouldRemoveFromDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        var product = new Product(
            "Product to Delete",
            "This will be deleted",
            49.99m,
            5,
            "Toys",
            "");

        await repository.CreateAsync(product);

        // Verify product exists
        var existingProduct = await repository.GetByIdAsync(product.Id);
        existingProduct.Should().NotBeNull();

        // Act - Delete
        await repository.DeleteAsync(product.Id);

        // Assert
        var deletedProduct = await repository.GetByIdAsync(product.Id);
        deletedProduct.Should().BeNull();
    }

    [Fact]
    public async Task ProductRepository_SearchProducts_ShouldReturnFilteredResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        var category = "TestCategory";
        var products = new[]
        {
            new Product("Search Test Product 1", "Description 1", 10.00m, 5, category, ""),
            new Product("Search Test Product 2", "Description 2", 20.00m, 15, category, ""),
            new Product("Different Product", "Different description", 30.00m, 25, "OtherCategory", "")
        };

        foreach (var product in products)
        {
            await repository.CreateAsync(product);
        }

        // Act - Search by name
        var searchResults = await repository.SearchAsync("Search Test", null, null, null, 1, 10);

        // Assert
        searchResults.Products.Should().HaveCount(2);
        searchResults.Products.Should().OnlyContain(p => p.Name.Contains("Search Test"));
        searchResults.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ProductRepository_SearchByCategory_ShouldReturnCategoryProducts()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        var category = "TargetCategory";
        var otherCategory = "OtherCategory";
        
        var products = new[]
        {
            new Product("Category Product 1", "In target category", 10.00m, 5, category, ""),
            new Product("Category Product 2", "Also in target category", 20.00m, 15, category, ""),
            new Product("Other Product", "In different category", 30.00m, 25, otherCategory, "")
        };

        foreach (var product in products)
        {
            await repository.CreateAsync(product);
        }

        // Act - Search by category
        var searchResults = await repository.SearchAsync(null, category, null, null, 1, 10);

        // Assert
        searchResults.Products.Should().HaveCount(2);
        searchResults.Products.Should().OnlyContain(p => p.Category == category);
        searchResults.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ProductRepository_SearchWithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        
        // Create multiple products for pagination testing
        for (int i = 1; i <= 15; i++)
        {
            var product = new Product(
                $"Pagination Test Product {i:D2}",
                $"Description {i}",
                i * 10.00m,
                i,
                "PaginationCategory",
                "");
            await repository.CreateAsync(product);
        }

        // Act - Get second page with 5 items per page
        var searchResults = await repository.SearchAsync("Pagination Test", null, null, null, 2, 5);

        // Assert
        searchResults.Products.Should().HaveCount(5);
        searchResults.TotalCount.Should().BeGreaterThanOrEqualTo(15);
        // Verify items are from the second page (items 6-10 when ordered)
        searchResults.Products.Should().OnlyContain(p => p.Name.Contains("Pagination Test"));
    }
}
