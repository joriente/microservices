using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Domain.Exceptions;

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

        public async Task Handle(DeleteProductCommand command, CancellationToken cancellationToken)
        {
            var exists = await _productRepository.ExistsAsync(command.Id);
            if (!exists)
                throw new ProductNotFoundException(command.Id);

            await _productRepository.DeleteAsync(command.Id);
        }
    }
}