namespace ProductOrderingSystem.Shared.Contracts.Products
{
    public record ProductDto(
        string Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record CreateProductRequest(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    );

    public record UpdateProductRequest(
        string Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    );

    public record ProductSearchRequest(
        string? SearchTerm,
        string? Category,
        decimal? MinPrice,
        decimal? MaxPrice,
        int Page = 1,
        int PageSize = 10
    );

    public record ProductSearchResponse(
        IEnumerable<ProductDto> Products,
        int TotalCount,
        int Page,
        int PageSize
    );

    // Pagination metadata for response header
    public record PaginationMetadata(
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPrevious,
        bool HasNext
    );
}