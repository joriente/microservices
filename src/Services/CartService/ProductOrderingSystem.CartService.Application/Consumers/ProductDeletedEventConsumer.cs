using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CartService.Application.Consumers;

/// <summary>
/// Consumes ProductDeletedEvent from Product Service and removes the product from local cache.
/// Ensures deleted products cannot be added to carts.
/// </summary>
public class ProductDeletedEventConsumer : IConsumer<ProductDeletedEvent>
{
    private readonly IProductCacheRepository _productCacheRepository;
    private readonly ILogger<ProductDeletedEventConsumer> _logger;

    public ProductDeletedEventConsumer(
        IProductCacheRepository productCacheRepository,
        ILogger<ProductDeletedEventConsumer> logger)
    {
        _productCacheRepository = productCacheRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Received ProductDeletedEvent for Product {ProductId}",
            message.ProductId);

        try
        {
            await _productCacheRepository.DeleteAsync(message.ProductId, context.CancellationToken);

            _logger.LogInformation(
                "Successfully removed Product {ProductId} from cache",
                message.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error removing Product {ProductId} from cache",
                message.ProductId);
            throw; // Let MassTransit handle retry
        }
    }
}
