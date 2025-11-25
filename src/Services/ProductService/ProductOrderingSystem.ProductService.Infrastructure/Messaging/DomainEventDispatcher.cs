using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.ProductService.Domain.Common;
using DomainEvents = ProductOrderingSystem.ProductService.Domain.Events;
using IntegrationEvents = ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.ProductService.Infrastructure.Messaging;

/// <summary>
/// Dispatches domain events from Product entities to RabbitMQ via MassTransit.
/// Converts domain events to integration events for cross-service communication.
/// </summary>
public class DomainEventDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IPublishEndpoint publishEndpoint,
        ILogger<DomainEventDispatcher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(BaseEntity entity, CancellationToken cancellationToken = default)
    {
        var domainEvents = entity.DomainEvents.ToList();
        entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventAsync(domainEvent, cancellationToken);
        }
    }

    private async Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            switch (domainEvent)
            {
                case DomainEvents.ProductCreatedEvent productCreated:
                    var createdEvent = new IntegrationEvents.ProductCreatedEvent(
                        ProductId: productCreated.ProductId,
                        Name: productCreated.Name,
                        Price: productCreated.Price,
                        StockQuantity: productCreated.StockQuantity,
                        CreatedAt: DateTime.UtcNow
                    );
                    await _publishEndpoint.Publish(createdEvent, cancellationToken);
                    _logger.LogInformation(
                        "Published ProductCreatedEvent for Product {ProductId} ({ProductName})",
                        productCreated.ProductId,
                        productCreated.Name);
                    break;

                case DomainEvents.ProductUpdatedEvent productUpdated:
                    var updatedEvent = new IntegrationEvents.ProductUpdatedEvent(
                        ProductId: productUpdated.ProductId,
                        Name: productUpdated.Name,
                        Price: productUpdated.Price,
                        StockQuantity: productUpdated.StockQuantity,
                        UpdatedAt: DateTime.UtcNow
                    );
                    await _publishEndpoint.Publish(updatedEvent, cancellationToken);
                    _logger.LogInformation(
                        "Published ProductUpdatedEvent for Product {ProductId} ({ProductName})",
                        productUpdated.ProductId,
                        productUpdated.Name);
                    break;

                // Note: ProductStockUpdatedEvent, ProductStockReservedEvent, etc. are internal domain events
                // and don't need to be published to other services unless required
                
                default:
                    _logger.LogDebug(
                        "Domain event {EventType} not mapped to integration event",
                        domainEvent.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing domain event {EventType}",
                domainEvent.GetType().Name);
            throw;
        }
    }
}
