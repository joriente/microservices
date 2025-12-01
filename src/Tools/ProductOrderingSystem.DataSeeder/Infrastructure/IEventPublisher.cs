namespace ProductOrderingSystem.DataSeeder.Infrastructure;

/// <summary>
/// Interface for publishing events to Azure Service Bus via MassTransit
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to Azure Service Bus
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
