using ErrorOr;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Queries.Products
{
    // Wolverine convention: query is just a record, no interface needed
    public record GetProductByIdQuery(string Id);

    // Wolverine convention: handler method name should be Handle or HandleAsync
    public class GetProductByIdQueryHandler
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Product>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(query.Id);
            
            if (product == null)
            {
                return Error.NotFound("Product.NotFound", $"Product with ID '{query.Id}' was not found");
            }
            
            return product;
        }
    }
}