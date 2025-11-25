using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CartService.Application.Consumers;

/// <summary>
/// Consumes ProductCreatedEvent from Product Service and updates the local product cache.
/// This enables Cart Service to validate products and display accurate names/prices without API calls.
/// </summary>
public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly IProductCacheRepository _productCacheRepository;
    private readonly ILogger<ProductCreatedEventConsumer> _logger;

    public ProductCreatedEventConsumer(
        IProductCacheRepository productCacheRepository,
        ILogger<ProductCreatedEventConsumer> logger)
    {
        _productCacheRepository = productCacheRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Received ProductCreatedEvent for Product {ProductId} ({ProductName})",
            message.ProductId,
            message.Name);

        try
        {
            var cacheEntry = new ProductCacheEntry
            {
                Id = message.ProductId,
                Name = message.Name,
                Price = message.Price,
                StockQuantity = message.StockQuantity,
                IsActive = true,
                CreatedAt = message.CreatedAt,
                LastUpdated = DateTime.UtcNow
            };

            await _productCacheRepository.UpsertAsync(cacheEntry, context.CancellationToken);

            _logger.LogInformation(
                "Successfully cached Product {ProductId} ({ProductName}) with Price {Price:C}",
                message.ProductId,
                message.Name,
                message.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error caching Product {ProductId} from ProductCreatedEvent",
                message.ProductId);
            throw; // Let MassTransit handle retry
        }
    }
}
