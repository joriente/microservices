using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Consumers;

public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly AnalyticsDbContext _context;
    private readonly ILogger<ProductCreatedEventConsumer> _logger;
    private readonly IEventHubPublisher _eventHubPublisher;

    public ProductCreatedEventConsumer(AnalyticsDbContext context, ILogger<ProductCreatedEventConsumer> logger, IEventHubPublisher eventHubPublisher)
    {
        _context = context;
        _logger = logger;
        _eventHubPublisher = eventHubPublisher;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Analytics: Capturing ProductCreated event for Product {ProductId}", message.ProductId);

        var productEvent = new ProductEvent
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.Parse(message.ProductId),
            Name = message.Name,
            Category = "Uncategorized",
            Price = message.Price,
            EventType = "Created",
            EventTimestamp = DateTime.UtcNow
        };

        _context.ProductEvents.Add(productEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Analytics: Stored ProductCreated event for Product {ProductId}", message.ProductId);
        
        // Publish to Event Hub for Microsoft Fabric
        await _eventHubPublisher.PublishProductEventAsync(
            Guid.Parse(message.ProductId),
            message.Name,
            "Uncategorized",
            message.Price,
            "Created"
        );
    }
}
