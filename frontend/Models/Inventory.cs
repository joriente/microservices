namespace ProductOrderingSystem.Web.Models;

public record InventoryItem(
    Guid Id,
    Guid ProductId,
    int AvailableQuantity,
    int ReservedQuantity,
    DateTime LastUpdated);

public record Inventory(
    Guid Id,
    Guid ProductId,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTime LastUpdated);

public record InventoryAdjustmentRequest(
    Guid ProductId,
    int Quantity,
    string Reason);

public record ReserveInventoryRequest(
    Guid OrderId,
    List<InventoryReservationItem> Items);

public record InventoryReservationItem(
    Guid ProductId,
    int Quantity);
