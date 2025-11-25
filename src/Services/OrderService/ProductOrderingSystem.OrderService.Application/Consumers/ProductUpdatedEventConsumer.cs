using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.OrderService.Application.Consumers;

/// <summary>
/// Consumes ProductUpdatedEvent from Product Service and updates the local product cache.
/// Keeps cached product data synchronized with Product Service.
/// </summary>
public class ProductUpdatedEventConsumer : IConsumer<ProductUpdatedEvent>
{
    private readonly IProductCacheRepository _productCacheRepository;
    private readonly ILogger<ProductUpdatedEventConsumer> _logger;

    public ProductUpdatedEventConsumer(
        IProductCacheRepository productCacheRepository,
        ILogger<ProductUpdatedEventConsumer> logger)
    {
        _productCacheRepository = productCacheRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Received ProductUpdatedEvent for Product {ProductId} ({ProductName})",
            message.ProductId,
            message.Name);

        try
        {
            // Get existing cache entry to preserve fields not in the event
            var existingEntry = await _productCacheRepository.GetByIdAsync(message.ProductId, context.CancellationToken);

            if (existingEntry == null)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found in cache during update. Creating new entry.",
                    message.ProductId);
                
                existingEntry = new Domain.Entities.ProductCacheEntry
                {
                    Id = message.ProductId,
                    CreatedAt = message.UpdatedAt // Best guess
                };
            }

            // Update with new values
            existingEntry.Name = message.Name;
            existingEntry.Price = message.Price;
            existingEntry.LastUpdated = DateTime.UtcNow;

            await _productCacheRepository.UpsertAsync(existingEntry, context.CancellationToken);

            _logger.LogInformation(
                "Successfully updated cached Product {ProductId} ({ProductName}) with Price {Price:C}",
                message.ProductId,
                message.Name,
                message.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating cached Product {ProductId} from ProductUpdatedEvent",
                message.ProductId);
            throw; // Let MassTransit handle retry
        }
    }
}
