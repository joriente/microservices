using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.InventoryService.Features.EventConsumers;

/// <summary>
/// Consumes PaymentProcessedEvent to commit inventory reservations
/// </summary>
public class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<PaymentProcessedEventConsumer> _logger;

    public PaymentProcessedEventConsumer(
        InventoryDbContext context,
        ILogger<PaymentProcessedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received PaymentProcessedEvent for Order {OrderId}, Payment {PaymentId}",
            message.OrderId,
            message.PaymentId);

        try
        {
            // Find all reservations for this order
            var reservations = await _context.InventoryReservations
                .Where(x => x.OrderId == message.OrderId && x.Status == ReservationStatus.Reserved)
                .ToListAsync();

            if (!reservations.Any())
            {
                _logger.LogWarning("No reservations found for order {OrderId}", message.OrderId);
                return;
            }

            // Commit each reservation (reduce QuantityOnHand, reduce QuantityReserved)
            foreach (var reservation in reservations)
            {
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(x => x.ProductId == reservation.ProductId);

                if (inventoryItem == null)
                {
                    _logger.LogWarning(
                        "Inventory item not found for product {ProductId} in order {OrderId}",
                        reservation.ProductId,
                        message.OrderId);
                    continue;
                }

                // Commit the reservation: reduce QuantityOnHand and QuantityReserved
                inventoryItem.Fulfill(reservation.Quantity);

                // Mark reservation as fulfilled
                reservation.Status = ReservationStatus.Fulfilled;
                reservation.FulfilledAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Committed reservation for Product {ProductId}, Quantity: {Quantity}, Order: {OrderId}",
                    reservation.ProductId,
                    reservation.Quantity,
                    message.OrderId);
            }

            // Save all changes
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully committed all inventory reservations for order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing PaymentProcessedEvent for Order {OrderId}",
                message.OrderId);
            throw;
        }
    }
}
