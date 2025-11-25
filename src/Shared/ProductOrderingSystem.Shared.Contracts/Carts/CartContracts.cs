namespace ProductOrderingSystem.Shared.Contracts.Carts;

public record AddItemToCartRequest(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity
);

public record UpdateItemQuantityRequest(
    int Quantity
);
