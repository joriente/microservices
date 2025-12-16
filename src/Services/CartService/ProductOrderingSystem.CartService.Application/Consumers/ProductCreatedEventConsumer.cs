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
            "[CONSUMER START] Product {ProductId} ({ProductName}) - Attempt {Attempt}",
            message.ProductId,
            message.Name,
            context.GetRetryAttempt());

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

            _logger.LogInformation("[UPSERT START] Product {ProductId}", message.ProductId);
            await _productCacheRepository.UpsertAsync(cacheEntry, context.CancellationToken);
            _logger.LogInformation("[UPSERT SUCCESS] Product {ProductId}", message.ProductId);

            _logger.LogInformation(
                "[CONSUMER SUCCESS] ✓ Cached Product {ProductId} ({ProductName}) with Price {Price:C}",
                message.ProductId,
                message.Name,
                message.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CONSUMER FAILED] ✗ Product {ProductId} - Exception: {Message}",
                message.ProductId,
                ex.Message);
            throw;
        }
    }
}
