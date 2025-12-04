using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.ProductService.Application.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly IProductRepository _productRepository;

    public OrderCreatedEventConsumer(
        ILogger<OrderCreatedEventConsumer> logger,
        IProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = context.Message;
        
        _logger.LogInformation(
            "Received OrderCreatedEvent for Order {OrderId} with {ItemCount} items. Total: {TotalAmount:C}",
            order.OrderId,
            order.Items?.Count ?? 0,
            order.TotalAmount);

        var reservedProducts = new List<(Guid ProductId, int Quantity)>();
        var firstFailure = false;

        // Process each order item and reserve stock
        foreach (var item in order.Items ?? [])
        {
            try
            {
                // If we already encountered a failure, skip remaining items
                if (firstFailure)
                {
                    _logger.LogInformation(
                        "Skipping Product {ProductId} due to previous failure in Order {OrderId}",
                        item.ProductId,
                        order.OrderId);
                    continue;
                }

                // Get the product
                var product = await _productRepository.GetByIdAsync(item.ProductId.ToString());
                
                if (product == null)
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found for Order {OrderId}. Publishing failure event.",
                        item.ProductId,
                        order.OrderId);
                    
                    await PublishReservationFailureAsync(
                        context,
                        order.OrderId.ToString(),
                        item.ProductId,
                        $"Product {item.ProductId}",
                        item.Quantity,
                        "Product not found");
                    
                    firstFailure = true;
                    break;
                }

                // Check if product is active
                if (!product.IsActive)
                {
                    _logger.LogWarning(
                        "Product {ProductId} ({ProductName}) is inactive for Order {OrderId}. Publishing failure event.",
                        item.ProductId,
                        product.Name,
                        order.OrderId);
                    
                    await PublishReservationFailureAsync(
                        context,
                        order.OrderId.ToString(),
                        item.ProductId,
                        product.Name,
                        item.Quantity,
                        "Product is inactive");
                    
                    firstFailure = true;
                    break;
                }

                // Reserve stock (this will throw if insufficient stock)
                product.ReserveStock(item.Quantity);
                
                // Update the product with reduced stock
                await _productRepository.UpdateAsync(product);
                
                // Track successful reservations for potential rollback
                reservedProducts.Add((item.ProductId, item.Quantity));
                
                _logger.LogInformation(
                    "Successfully reserved {Quantity} units of Product {ProductId} ({ProductName}) for Order {OrderId}. Remaining stock: {RemainingStock}",
                    item.Quantity,
                    item.ProductId,
                    product.Name,
                    order.OrderId,
                    product.StockQuantity);

                // Publish ProductReservedEvent
                var productReservedEvent = new ProductReservedEvent(
                    OrderId: order.OrderId,
                    ProductId: item.ProductId,
                    Quantity: item.Quantity,
                    ReservedAt: DateTime.UtcNow
                );

                await context.Publish(productReservedEvent);
                
                _logger.LogInformation(
                    "Published ProductReservedEvent for Product {ProductId} in Order {OrderId}",
                    item.ProductId,
                    order.OrderId);
            }
            catch (InvalidOperationException ex)
            {
                // Insufficient stock
                _logger.LogError(
                    ex,
                    "Failed to reserve {Quantity} units of Product {ProductId} for Order {OrderId}: {ErrorMessage}",
                    item.Quantity,
                    item.ProductId,
                    order.OrderId,
                    ex.Message);
                
                var product = await _productRepository.GetByIdAsync(item.ProductId.ToString());
                await PublishReservationFailureAsync(
                    context,
                    order.OrderId.ToString(),
                    item.ProductId,
                    product?.Name ?? $"Product {item.ProductId}",
                    item.Quantity,
                    ex.Message);
                
                firstFailure = true;
                break;
            }
            catch (Exception ex)
            {
                // Other errors
                _logger.LogError(
                    ex,
                    "Unexpected error while reserving Product {ProductId} for Order {OrderId}: {ErrorMessage}",
                    item.ProductId,
                    order.OrderId,
                    ex.Message);
                
                await PublishReservationFailureAsync(
                    context,
                    order.OrderId.ToString(),
                    item.ProductId,
                    $"Product {item.ProductId}",
                    item.Quantity,
                    $"Unexpected error: {ex.Message}");
                
                firstFailure = true;
                break;
            }
        }

        // If we had a failure, we need to rollback any products that were successfully reserved
        if (firstFailure && reservedProducts.Any())
        {
            _logger.LogWarning(
                "Rolling back {Count} successfully reserved products for Order {OrderId}",
                reservedProducts.Count,
                order.OrderId);
            
            await RollbackReservationsAsync(reservedProducts, order.OrderId.ToString());
        }
        
        if (firstFailure)
        {
            _logger.LogWarning(
                "Order {OrderId} processing failed. Compensation initiated.",
                order.OrderId);
        }
        else
        {
            _logger.LogInformation(
                "Successfully completed processing OrderCreatedEvent for Order {OrderId}",
                order.OrderId);
        }
    }

    private async Task PublishReservationFailureAsync(
        ConsumeContext<OrderCreatedEvent> context,
        string orderId,
        Guid productId,
        string productName,
        int requestedQuantity,
        string failureReason)
    {
        var failureEvent = new ProductReservationFailedEvent(
            orderId,
            productId.ToString(),
            productName,
            requestedQuantity,
            failureReason,
            DateTime.UtcNow
        );

        await context.Publish(failureEvent);
        
        _logger.LogWarning(
            "Published ProductReservationFailedEvent for Order {OrderId}, Product {ProductId}: {Reason}",
            orderId,
            productId,
            failureReason);
    }

    private async Task RollbackReservationsAsync(
        List<(Guid ProductId, int Quantity)> reservedProducts,
        string orderId)
    {
        foreach (var (productId, quantity) in reservedProducts)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(productId.ToString());
                if (product != null)
                {
                    product.RestoreStock(quantity);
                    await _productRepository.UpdateAsync(product);
                    
                    _logger.LogInformation(
                        "Rolled back {Quantity} units for Product {ProductId} from failed Order {OrderId}",
                        quantity,
                        productId,
                        orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to rollback Product {ProductId} for Order {OrderId}",
                    productId,
                    orderId);
                // Continue with other products
            }
        }
    }
}
