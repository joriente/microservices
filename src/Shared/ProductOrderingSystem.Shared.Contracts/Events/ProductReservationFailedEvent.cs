namespace ProductOrderingSystem.Shared.Contracts.Events;

/// <summary>
/// Event published when product reservation fails (e.g., insufficient stock, product not found, product inactive).
/// Triggers compensation logic to cancel the order.
/// </summary>
public record ProductReservationFailedEvent(
    string OrderId,
    string ProductId,
    string ProductName,
    int RequestedQuantity,
    string FailureReason,
    DateTime FailedAt
);
