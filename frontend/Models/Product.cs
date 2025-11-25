namespace ProductOrderingSystem.Web.Models;

public record Product(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    int Stock);

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Category,
    int Stock,
    string? ImageUrl = null);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Category,
    int Stock,
    string? ImageUrl = null);

public record ProductSearchRequest(
    string? SearchQuery = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 12);

public record PaginatedProducts(
    List<Product> Products,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
