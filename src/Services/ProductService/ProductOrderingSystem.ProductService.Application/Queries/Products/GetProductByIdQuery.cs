using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

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

        public async Task<Product> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(query.Id);
            
            if (product == null)
            {
                throw new ProductNotFoundException(query.Id);
            }
            
            return product;
        }
    }
}