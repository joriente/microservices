using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.AnalyticsService.Infrastructure.Data;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Consumers;

public class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly AnalyticsDbContext _context;
    private readonly ILogger<PaymentProcessedEventConsumer> _logger;
    private readonly IEventHubPublisher _eventHubPublisher;

    public PaymentProcessedEventConsumer(AnalyticsDbContext context, ILogger<PaymentProcessedEventConsumer> logger, IEventHubPublisher eventHubPublisher)
    {
        _context = context;
        _logger = logger;
        _eventHubPublisher = eventHubPublisher;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Analytics: Capturing PaymentProcessed event for Order {OrderId}", message.OrderId);

        var paymentEvent = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            PaymentId = message.PaymentId,
            OrderId = message.OrderId,
            Amount = message.Amount,
            Status = message.Status,
            PaymentMethod = message.Currency,
            EventTimestamp = DateTime.UtcNow
        };

        _context.PaymentEvents.Add(paymentEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Analytics: Stored PaymentProcessed event for Order {OrderId}, Status: {Status}", 
            message.OrderId, paymentEvent.Status);
        
        // Publish to Event Hub for Microsoft Fabric
        await _eventHubPublisher.PublishPaymentEventAsync(
            message.PaymentId,
            message.OrderId,
            message.Amount,
            message.Status,
            message.Currency
        );
    }
}
