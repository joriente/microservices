namespace ProductOrderingSystem.Shared.Contracts.Events;

/// <summary>
/// Event published when an order is cancelled.
/// Triggers compensation logic to restore reserved stock for all products in the order.
/// </summary>
public record OrderCancelledEvent(
    string OrderId,
    string CustomerId,
    List<OrderItemDto> Items,
    string CancellationReason,
    DateTime CancelledAt
);
