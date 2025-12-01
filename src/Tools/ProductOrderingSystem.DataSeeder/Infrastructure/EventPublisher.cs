using MassTransit;
using Microsoft.Extensions.Logging;

namespace ProductOrderingSystem.DataSeeder.Infrastructure;

/// <summary>
/// Service for publishing events to Azure Service Bus via MassTransit
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IPublishEndpoint publishEndpoint, ILogger<EventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            await _publishEndpoint.Publish(@event, cancellationToken);
            _logger.LogDebug("Published {EventType} event", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event", typeof(TEvent).Name);
            throw;
        }
    }
}
