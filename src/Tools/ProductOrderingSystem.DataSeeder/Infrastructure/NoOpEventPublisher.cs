using Microsoft.Extensions.Logging;

namespace ProductOrderingSystem.DataSeeder.Infrastructure;

/// <summary>
/// No-op event publisher for when event publishing is disabled
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        _logger.LogDebug("Event publishing disabled - skipping {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }
}
