using ErrorOr;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Queries.Products
{
    // Wolverine convention: query is just a record, no interface needed
    public record SearchProductsQuery(
        string? SearchTerm,
        string? Category,
        decimal? MinPrice,
        decimal? MaxPrice,
        int Page = 1,
        int PageSize = 10
    );

    public record SearchProductsResult(
        IEnumerable<Product> Products,
        int TotalCount,
        int Page,
        int PageSize
    );

    // Wolverine convention: handler method name should be Handle or HandleAsync
    public class SearchProductsQueryHandler
    {
        private readonly IProductRepository _productRepository;

        public SearchProductsQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<SearchProductsResult>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
        {
            var (products, totalCount) = await _productRepository.SearchAsync(
                query.SearchTerm,
                query.Category,
                query.MinPrice,
                query.MaxPrice,
                query.Page,
                query.PageSize
            );

            return new SearchProductsResult(products, totalCount, query.Page, query.PageSize);
        }
    }
}