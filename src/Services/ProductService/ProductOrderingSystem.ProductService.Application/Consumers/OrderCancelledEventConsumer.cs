using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.ProductService.Application.Consumers;

/// <summary>
/// Handles order cancellations by restoring reserved stock for all products in the order.
/// This is the compensation logic for the saga pattern.
/// </summary>
public class OrderCancelledEventConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderCancelledEventConsumer> _logger;

    public OrderCancelledEventConsumer(
        IProductRepository productRepository,
        ILogger<OrderCancelledEventConsumer> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Order {OrderId} cancelled. Restoring stock for {ItemCount} products. Reason: {Reason}",
            message.OrderId,
            message.Items.Count,
            message.CancellationReason);

        var restoredProducts = new List<string>();
        var failedProducts = new List<(string ProductId, string Error)>();

        // Restore stock for each product in the order
        foreach (var item in message.Items)
        {
            try
            {
                // Fetch the product
                var product = await _productRepository.GetByIdAsync(item.ProductId.ToString());

                if (product == null)
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found when restoring stock for Order {OrderId}",
                        item.ProductId,
                        message.OrderId);
                    failedProducts.Add((item.ProductId.ToString(), "Product not found"));
                    continue;
                }

                // Restore the stock
                product.RestoreStock(item.Quantity);
                await _productRepository.UpdateAsync(product);

                restoredProducts.Add(product.Name);
                
                _logger.LogInformation(
                    "Restored {Quantity} units of stock for Product {ProductId} ({ProductName}) from Order {OrderId}. " +
                    "New stock: {NewStock}",
                    item.Quantity,
                    product.Id,
                    product.Name,
                    message.OrderId,
                    product.StockQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error restoring stock for Product {ProductId} from Order {OrderId}",
                    item.ProductId,
                    message.OrderId);
                failedProducts.Add((item.ProductId.ToString(), ex.Message));
            }
        }

        // Log summary
        if (restoredProducts.Any())
        {
            _logger.LogInformation(
                "Stock compensation completed for Order {OrderId}. " +
                "Restored: {RestoredCount} products. Failed: {FailedCount} products.",
                message.OrderId,
                restoredProducts.Count,
                failedProducts.Count);
        }

        if (failedProducts.Any())
        {
            _logger.LogWarning(
                "Some products failed stock restoration for Order {OrderId}: {FailedProducts}",
                message.OrderId,
                string.Join(", ", failedProducts.Select(f => $"{f.ProductId} ({f.Error})")));
        }
    }
}
