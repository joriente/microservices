namespace ProductOrderingSystem.Web.Models;

public record Cart(
    string Id,
    string CustomerId,
    string CustomerEmail,
    List<CartItem> Items,
    decimal TotalAmount,
    int TotalItems);

public record CartItem(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Subtotal);

public record AddToCartRequest(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity);

public record UpdateCartItemRequest(
    string ProductId,
    int Quantity);
