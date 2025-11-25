namespace ProductOrderingSystem.Shared.Contracts.Events;

public record InventoryReservedEvent(
    Guid OrderId,
    List<ReservedItemDto> Items,
    DateTime ReservedAt
);

public record ReservedItemDto(
    Guid ProductId,
    int Quantity
);

public record InventoryReservationFailedEvent(
    Guid OrderId,
    string Reason,
    DateTime FailedAt
);

public record InventoryFulfilledEvent(
    Guid OrderId,
    List<FulfilledItemDto> Items,
    DateTime FulfilledAt
);

public record FulfilledItemDto(
    Guid ProductId,
    int Quantity
);

public record InventoryReleasedEvent(
    Guid OrderId,
    List<ReleasedItemDto> Items,
    DateTime ReleasedAt
);

public record ReleasedItemDto(
    Guid ProductId,
    int Quantity
);
