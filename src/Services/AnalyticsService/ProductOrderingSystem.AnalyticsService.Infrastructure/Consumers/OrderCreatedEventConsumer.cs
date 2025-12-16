using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly AnalyticsDbContext _context;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly IEventHubPublisher _eventHubPublisher;

    public OrderCreatedEventConsumer(AnalyticsDbContext context, ILogger<OrderCreatedEventConsumer> logger, IEventHubPublisher eventHubPublisher)
    {
        _context = context;
        _logger = logger;
        _eventHubPublisher = eventHubPublisher;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Analytics: Capturing OrderCreated event for Order {OrderId}", message.OrderId);

        var orderEvent = new OrderEvent
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            TotalAmount = message.TotalAmount,
            Status = "Created",
            ItemCount = message.Items?.Count ?? 0,
            EventTimestamp = DateTime.UtcNow
        };

        _context.OrderEvents.Add(orderEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Analytics: Stored OrderCreated event for Order {OrderId}", message.OrderId);
        
        // Publish to Event Hub for Microsoft Fabric
        await _eventHubPublisher.PublishOrderEventAsync(
            message.OrderId,
            message.CustomerId,
            message.TotalAmount,
            "Created",
            message.Items?.Count ?? 0
        );
    }
}
