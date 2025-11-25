using ErrorOr;
using MediatR;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    public record UpdateProductCommand(
        string Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    ) : IRequest<ErrorOr<Product>>;

    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ErrorOr<Product>>
    {
        private readonly IProductRepository _productRepository;

        public UpdateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Product>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Id))
                    return Error.Validation("Product.Id", "Product ID is required");
                
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Error.Validation("Product.Name", "Product name is required");
                
                if (request.Price <= 0)
                    return Error.Validation("Product.Price", "Product price must be greater than zero");
                
                if (request.StockQuantity < 0)
                    return Error.Validation("Product.StockQuantity", "Stock quantity cannot be negative");

                var product = await _productRepository.GetByIdAsync(request.Id);
                if (product == null)
                    return Error.NotFound("Product.NotFound", $"Product with ID {request.Id} not found");

                product.UpdateProduct(
                    request.Name,
                    request.Description,
                    request.Price,
                    request.StockQuantity,
                    request.Category,
                    request.ImageUrl
                );

                var updatedProduct = await _productRepository.UpdateAsync(product);
                return updatedProduct;
            }
            catch (Exception ex)
            {
                return Error.Failure("Product.Update", $"Failed to update product: {ex.Message}");
            }
        }
    }
}