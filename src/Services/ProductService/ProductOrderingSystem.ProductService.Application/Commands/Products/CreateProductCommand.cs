using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    // Wolverine convention: command is just a record, no interface needed
    public record CreateProductCommand(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string ImageUrl
    );

    // Wolverine convention: handler method name should be Handle or HandleAsync
    public class CreateProductCommandHandler
    {
        private readonly IProductRepository _productRepository;

        public CreateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product> Handle(CreateProductCommand command, CancellationToken cancellationToken)
        {
            // Validate input - throw exceptions for validation errors
            var errors = new Dictionary<string, string[]>();
            
            if (string.IsNullOrWhiteSpace(command.Name))
                errors["Name"] = new[] { "Product name is required" };
            
            if (command.Price <= 0)
                errors["Price"] = new[] { "Product price must be greater than zero" };
            
            if (command.StockQuantity < 0)
                errors["StockQuantity"] = new[] { "Stock quantity cannot be negative" };

            if (errors.Any())
                throw new ProductValidationException(errors);

            var product = new Product(
                command.Name,
                command.Description,
                command.Price,
                command.StockQuantity,
                command.Category,
                command.ImageUrl
            );

            var createdProduct = await _productRepository.CreateAsync(product);
            return createdProduct;
        }
    }
}