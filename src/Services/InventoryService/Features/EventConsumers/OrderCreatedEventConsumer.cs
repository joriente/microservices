using MassTransit;
using Wolverine;
using ProductOrderingSystem.InventoryService.Features.Inventory;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.InventoryService.Features.EventConsumers;

/// <summary>
/// Consumes OrderCreatedEvent to reserve inventory
/// </summary>
public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IMessageBus messageBus,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderCreatedEvent for Order {OrderId}, Customer {CustomerId}",
            message.OrderId,
            message.CustomerId);

        try
        {
            // Reserve inventory for the order
            var command = new ReserveInventory.Command(
                message.OrderId,
                message.Items?.Select(i => new ReserveInventory.ReservationItem(
                    i.ProductId,
                    i.Quantity
                )).ToList() ?? new List<ReserveInventory.ReservationItem>());

            var result = await _messageBus.InvokeAsync<ReserveInventory.Result>(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully reserved inventory for order {OrderId}",
                    message.OrderId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to reserve inventory for order {OrderId}: {Error}",
                    message.OrderId,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing OrderCreatedEvent for Order {OrderId}",
                message.OrderId);
            throw;
        }
    }
}
