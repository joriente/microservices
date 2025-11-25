using MediatR;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Queries.Products
{
    public record GetProductByIdQuery(string Id) : IRequest<Product?>;

    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Product?>
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            return await _productRepository.GetByIdAsync(request.Id);
        }
    }
}