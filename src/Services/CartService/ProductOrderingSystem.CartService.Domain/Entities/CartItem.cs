namespace ProductOrderingSystem.CartService.Domain.Entities;

/// <summary>
/// Cart item value object (immutable)
/// </summary>
public class CartItem
{
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal Price { get; }
    public int Quantity { get; }
    public decimal TotalPrice => Price * Quantity;

    public CartItem(string productId, string productName, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
    }
}
