using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductOrderingSystem.InventoryService.Data;
using ProductOrderingSystem.InventoryService.Models;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.InventoryService.Features.EventConsumers;

/// <summary>
/// Consumes ProductCreatedEvent to initialize inventory
/// </summary>
public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ProductCreatedEventConsumer> _logger;

    public ProductCreatedEventConsumer(
        InventoryDbContext context,
        ILogger<ProductCreatedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received ProductCreatedEvent for Product {ProductId}: {ProductName}",
            message.ProductId,
            message.Name);

        try
        {
            var productId = Guid.Parse(message.ProductId);
            
            // Check if inventory already exists
            var existing = await _context.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (existing != null)
            {
                _logger.LogInformation("Inventory already exists for product {ProductId}", message.ProductId);
                return;
            }

            // Create initial inventory record with stock from the event
            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductName = message.Name,
                QuantityOnHand = message.StockQuantity, // Use stock from ProductCreatedEvent
                QuantityReserved = 0,
                ReorderLevel = 10, // Default reorder level
                ReorderQuantity = 50, // Default reorder quantity
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.InventoryItems.Add(inventoryItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Initialized inventory for product {ProductId}: {ProductName} with {Quantity} units",
                message.ProductId,
                message.Name,
                message.StockQuantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing ProductCreatedEvent for Product {ProductId}",
                message.ProductId);
            throw;
        }
    }
}
