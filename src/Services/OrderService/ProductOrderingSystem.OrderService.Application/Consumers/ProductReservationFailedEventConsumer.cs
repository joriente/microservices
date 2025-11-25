using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.OrderService.Application.Consumers;

/// <summary>
/// Handles product reservation failures by cancelling the order.
/// This is part of the compensation/saga pattern for distributed transactions.
/// </summary>
public class ProductReservationFailedEventConsumer : IConsumer<ProductReservationFailedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<ProductReservationFailedEventConsumer> _logger;

    public ProductReservationFailedEventConsumer(
        IOrderRepository orderRepository,
        ILogger<ProductReservationFailedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductReservationFailedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogWarning(
            "Product reservation failed for Order {OrderId}, Product {ProductId} ({ProductName}). " +
            "Reason: {FailureReason}. Initiating order cancellation...",
            message.OrderId,
            message.ProductId,
            message.ProductName,
            message.FailureReason);

        try
        {
            // Fetch the order
            var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);
            
            if (order == null)
            {
                _logger.LogError("Order {OrderId} not found for cancellation", message.OrderId);
                return;
            }

            // Check if order can be cancelled
            if (!order.CanBeCancelled())
            {
                _logger.LogWarning(
                    "Order {OrderId} cannot be cancelled in current status: {Status}",
                    message.OrderId,
                    order.Status);
                return;
            }

            // Cancel the order with the failure reason
            var cancellationReason = $"Product reservation failed: {message.FailureReason} " +
                                   $"(Product: {message.ProductName}, Requested: {message.RequestedQuantity})";
            order.Cancel(cancellationReason);

            // Update the order in the database
            await _orderRepository.UpdateAsync(order, context.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} cancelled successfully due to product reservation failure",
                message.OrderId);

            // Publish OrderCancelledEvent to trigger compensation in Product Service
            await context.Publish(new OrderCancelledEvent(
                order.Id,
                order.CustomerId,
                order.Items.Select(i => new OrderItemDto(
                    ProductId: Guid.Parse(i.ProductId),
                    Quantity: i.Quantity,
                    UnitPrice: i.UnitPrice
                )).ToList(),
                cancellationReason,
                DateTime.UtcNow
            ));

            _logger.LogInformation(
                "Published OrderCancelledEvent for Order {OrderId} to restore stock",
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing ProductReservationFailedEvent for Order {OrderId}",
                message.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}
