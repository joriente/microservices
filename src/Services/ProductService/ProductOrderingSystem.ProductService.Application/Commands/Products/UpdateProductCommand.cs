using ErrorOr;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    // Wolverine convention: command is just a record, no interface needed
    public record UpdateProductCommand(
        string Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    );

    // Wolverine convention: handler method name should be Handle or HandleAsync
    public class UpdateProductCommandHandler
    {
        private readonly IProductRepository _productRepository;

        public UpdateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Product>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
        {
            // Validate input - return errors instead of throwing
            var errors = new List<Error>();
            
            if (string.IsNullOrWhiteSpace(command.Id))
                errors.Add(Error.Validation("Id", "Product ID is required"));
            
            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add(Error.Validation("Name", "Product name is required"));
            
            if (command.Price <= 0)
                errors.Add(Error.Validation("Price", "Product price must be greater than zero"));
            
            if (command.StockQuantity < 0)
                errors.Add(Error.Validation("StockQuantity", "Stock quantity cannot be negative"));

            if (errors.Any())
                return errors;

            var product = await _productRepository.GetByIdAsync(command.Id);
            if (product == null)
                return Error.NotFound("Product.NotFound", $"Product with ID '{command.Id}' was not found");

            product.UpdateProduct(
                command.Name,
                command.Description,
                command.Price,
                command.StockQuantity,
                command.Category,
                command.ImageUrl
            );

            var updatedProduct = await _productRepository.UpdateAsync(product);
            return updatedProduct;
        }
    }
}