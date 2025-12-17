using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Consumers;

public class InventoryReservedEventConsumer : IConsumer<ProductReservedEvent>
{
    private readonly AnalyticsDbContext _context;
    private readonly ILogger<InventoryReservedEventConsumer> _logger;
    private readonly IEventHubPublisher _eventHubPublisher;

    public InventoryReservedEventConsumer(AnalyticsDbContext context, ILogger<InventoryReservedEventConsumer> logger, IEventHubPublisher eventHubPublisher)
    {
        _context = context;
        _logger = logger;
        _eventHubPublisher = eventHubPublisher;
    }

    public async Task Consume(ConsumeContext<ProductReservedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Analytics: Capturing InventoryReserved event for Order {OrderId}", message.OrderId);

        var inventoryEvent = new InventoryEvent
        {
            Id = Guid.NewGuid(),
            ProductId = message.ProductId,
            OrderId = message.OrderId,
            QuantityChange = -message.Quantity,
            QuantityAfter = 0,
            EventType = "Reserved",
            EventTimestamp = DateTime.UtcNow
        };

        _context.InventoryEvents.Add(inventoryEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Analytics: Stored InventoryReserved event for Order {OrderId}, ProductId {ProductId}", 
            message.OrderId, message.ProductId);
        
        // Publish to Event Hub for Microsoft Fabric
        await _eventHubPublisher.PublishInventoryEventAsync(
            message.ProductId,
            message.OrderId,
            -message.Quantity,
            0,
            "Reserved"
        );
    }
}
