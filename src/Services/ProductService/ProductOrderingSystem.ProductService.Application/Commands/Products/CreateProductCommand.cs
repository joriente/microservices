using ErrorOr;
using MediatR;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    public record CreateProductCommand(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    ) : IRequest<ErrorOr<Product>>;

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ErrorOr<Product>>
    {
        private readonly IProductRepository _productRepository;

        public CreateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Product>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Error.Validation("Product.Name", "Product name is required");
                
                if (request.Price <= 0)
                    return Error.Validation("Product.Price", "Product price must be greater than zero");
                
                if (request.StockQuantity < 0)
                    return Error.Validation("Product.StockQuantity", "Stock quantity cannot be negative");

                var product = new Product(
                    request.Name,
                    request.Description,
                    request.Price,
                    request.StockQuantity,
                    request.Category,
                    request.ImageUrl
                );

                var createdProduct = await _productRepository.CreateAsync(product);
                return createdProduct;
            }
            catch (Exception ex)
            {
                return Error.Failure("Product.Creation", $"Failed to create product: {ex.Message}");
            }
        }
    }
}