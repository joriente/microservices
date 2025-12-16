using ErrorOr;
using MediatR;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Queries.Products
{
    public record GetProductByIdQuery(string Id) : IRequest<ErrorOr<Product>>;

    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ErrorOr<Product>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Product>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(request.Id);
            
            if (product == null)
            {
                return Error.NotFound("Product.NotFound", $"Product with ID '{request.Id}' was not found");
            }
            
            return product;
        }
    }
}