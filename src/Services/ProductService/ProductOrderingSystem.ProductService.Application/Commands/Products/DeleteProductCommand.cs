using ErrorOr;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    // Wolverine convention: command is just a record, no interface needed
    public record DeleteProductCommand(string Id);

    // Wolverine convention: handler method name should be Handle or HandleAsync
    public class DeleteProductCommandHandler
    {
        private readonly IProductRepository _productRepository;

        public DeleteProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ErrorOr<Success>> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
        {
            var exists = await _productRepository.ExistsAsync(command.Id);
            if (!exists)
                return Error.NotFound("Product.NotFound", $"Product with ID '{command.Id}' was not found");

            await _productRepository.DeleteAsync(command.Id);
            return Result.Success;
        }
    }
}