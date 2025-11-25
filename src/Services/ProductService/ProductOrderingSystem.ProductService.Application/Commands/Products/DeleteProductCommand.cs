using MediatR;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.Application.Commands.Products
{
    public record DeleteProductCommand(string Id) : IRequest;

    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
    {
        private readonly IProductRepository _productRepository;

        public DeleteProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var exists = await _productRepository.ExistsAsync(request.Id);
            if (!exists)
                throw new InvalidOperationException($"Product with ID {request.Id} not found");

            await _productRepository.DeleteAsync(request.Id);
        }
    }
}