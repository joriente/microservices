using ErrorOr;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;

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

        public async Task<ErrorOr<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
        {
            // Validate input - return errors instead of throwing
            var errors = new List<Error>();
            
            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add(Error.Validation("Name", "Product name is required"));
            
            if (command.Price <= 0)
                errors.Add(Error.Validation("Price", "Product price must be greater than zero"));
            
            if (command.StockQuantity < 0)
                errors.Add(Error.Validation("StockQuantity", "Stock quantity cannot be negative"));

            if (errors.Any())
                return errors;

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