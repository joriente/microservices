using ProductOrderingSystem.ProductService.Domain.Common;

namespace ProductOrderingSystem.ProductService.Domain.Events
{
    public record ProductCreatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        int StockQuantity
    ) : DomainEvent;

    public record ProductUpdatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        int StockQuantity
    ) : DomainEvent;

    public record ProductStockUpdatedEvent(
        string ProductId,
        int NewStockQuantity
    ) : DomainEvent;

    public record ProductStockReservedEvent(
        string ProductId,
        int ReservedQuantity,
        int RemainingStock
    ) : DomainEvent;

    public record ProductStockReleasedEvent(
        string ProductId,
        int ReleasedQuantity,
        int NewStockQuantity
    ) : DomainEvent;

    public record ProductDeactivatedEvent(
        string ProductId
    ) : DomainEvent;

    public record ProductActivatedEvent(
        string ProductId
    ) : DomainEvent;
}