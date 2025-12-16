using MongoDB.Bson.Serialization.Attributes;

namespace ProductOrderingSystem.OrderService.Domain.Entities;

/// <summary>
/// Cached product data maintained in Order Service for order creation and validation.
/// This is a read-only cache synchronized via events from Product Service.
/// </summary>
public class ProductCacheEntry
{
    /// <summary>
    /// Product identifier (same as ProductId from Product Service)
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Product name (cached for display and order creation)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Current product price (cached for order creation)
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Product category (cached for reporting and filtering)
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the product is active and can be ordered
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When this cache entry was last updated from Product Service events
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when this product was created in the Product Service
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
