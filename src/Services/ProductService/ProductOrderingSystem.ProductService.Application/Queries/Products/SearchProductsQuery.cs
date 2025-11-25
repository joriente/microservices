using MediatR;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Queries.Products
{
    public record SearchProductsQuery(
        string? SearchTerm,
        string? Category,
        decimal? MinPrice,
        decimal? MaxPrice,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<SearchProductsResult>;

    public record SearchProductsResult(
        IEnumerable<Product> Products,
        int TotalCount,
        int Page,
        int PageSize
    );

    public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, SearchProductsResult>
    {
        private readonly IProductRepository _productRepository;

        public SearchProductsQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<SearchProductsResult> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
        {
            var (products, totalCount) = await _productRepository.SearchAsync(
                request.SearchTerm,
                request.Category,
                request.MinPrice,
                request.MaxPrice,
                request.Page,
                request.PageSize
            );

            return new SearchProductsResult(products, totalCount, request.Page, request.PageSize);
        }
    }
}