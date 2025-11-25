using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.OrderService.Application.Consumers;

/// <summary>
/// Handles payment completion by updating the order status to Confirmed.
/// This ensures the order status reflects the successful payment.
/// </summary>
public class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentProcessedEventConsumer> _logger;

    public PaymentProcessedEventConsumer(
        IOrderRepository orderRepository,
        ILogger<PaymentProcessedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Payment processed for Order {OrderId}, Payment {PaymentId}. Updating order status...",
            message.OrderId,
            message.PaymentId);

        try
        {
            // Fetch the order
            var order = await _orderRepository.GetByIdAsync(message.OrderId.ToString(), context.CancellationToken);
            
            if (order == null)
            {
                _logger.LogError("Order {OrderId} not found for payment confirmation", message.OrderId);
                return;
            }

            // Check if order is in a valid state to confirm payment
            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning(
                    "Order {OrderId} is not in Pending status (current: {Status}). Skipping payment confirmation.",
                    message.OrderId,
                    order.Status);
                return;
            }

            // Update the order status to Confirmed
            order.UpdateStatus(OrderStatus.Confirmed);

            // Update the order in the database
            await _orderRepository.UpdateAsync(order, context.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} status updated to Confirmed after payment {PaymentId} (Amount: {Amount} {Currency})",
                message.OrderId,
                message.PaymentId,
                message.Amount,
                message.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing PaymentProcessedEvent for Order {OrderId}",
                message.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}
