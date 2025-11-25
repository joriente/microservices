using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CartService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CartService.Application.Consumers;

/// <summary>
/// Consumes OrderCreatedEvent from Order Service to automatically clear the cart after order is placed.
/// This ensures users don't accidentally reorder the same items.
/// </summary>
public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        ICartRepository cartRepository,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _cartRepository = cartRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Received OrderCreatedEvent for Order {OrderId} from Customer {CustomerId}",
            message.OrderId,
            message.CustomerId);

        try
        {
            // Get the customer's cart
            var cart = await _cartRepository.GetByCustomerIdAsync(message.CustomerId.ToString());
            
            if (cart == null)
            {
                _logger.LogInformation(
                    "No cart found for Customer {CustomerId}, nothing to clear",
                    message.CustomerId);
                return;
            }

            // Clear the cart
            cart.Clear();
            await _cartRepository.UpdateAsync(cart);

            _logger.LogInformation(
                "Successfully cleared cart {CartId} for Customer {CustomerId} after Order {OrderId}",
                cart.Id,
                message.CustomerId,
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error clearing cart for Customer {CustomerId} after Order {OrderId}",
                message.CustomerId,
                message.OrderId);
            // Don't rethrow - this is not critical, cart can be manually cleared
        }
    }
}
