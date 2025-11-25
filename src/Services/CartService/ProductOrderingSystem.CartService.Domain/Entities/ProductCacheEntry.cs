namespace ProductOrderingSystem.CartService.Domain.Entities;

/// <summary>
/// Cached product data maintained in Cart Service for cart operations and validation.
/// This is a read-only cache synchronized via events from Product Service.
/// Ensures cart items have accurate product names and prices without API calls.
/// </summary>
public class ProductCacheEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
