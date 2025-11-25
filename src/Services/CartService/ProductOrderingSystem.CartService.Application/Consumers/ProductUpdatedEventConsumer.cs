using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CartService.Application.Consumers;

/// <summary>
/// Consumes ProductUpdatedEvent from Product Service and updates the local product cache.
/// Keeps cached product data synchronized with the Product Service.
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
            var existingEntry = await _productCacheRepository.GetByIdAsync(message.ProductId);
            
            var cacheEntry = new ProductCacheEntry
            {
                Id = message.ProductId,
                Name = message.Name,
                Price = message.Price,
                StockQuantity = message.StockQuantity,
                IsActive = true,
                CreatedAt = existingEntry?.CreatedAt ?? message.UpdatedAt,
                LastUpdated = DateTime.UtcNow
            };

            await _productCacheRepository.UpsertAsync(cacheEntry, context.CancellationToken);

            _logger.LogInformation(
                "Successfully updated cached Product {ProductId} ({ProductName}) with new Price {Price:C}",
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
