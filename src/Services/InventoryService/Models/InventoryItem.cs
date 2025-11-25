using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductOrderingSystem.InventoryService.Models;

/// <summary>
/// Represents inventory stock for a product
/// </summary>
[Table("inventory_items")]
public class InventoryItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("product_name")]
    [MaxLength(500)]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity_on_hand")]
    public int QuantityOnHand { get; set; }

    [Column("quantity_reserved")]
    public int QuantityReserved { get; set; }

    [NotMapped]
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    [Column("reorder_level")]
    public int ReorderLevel { get; set; }

    [Column("reorder_quantity")]
    public int ReorderQuantity { get; set; }

    [Column("last_restocked_at")]
    public DateTime? LastRestockedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Reserve(int quantity)
    {
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Insufficient inventory. Available: {QuantityAvailable}, Requested: {quantity}");

        QuantityReserved += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (quantity > QuantityReserved)
            throw new InvalidOperationException($"Cannot release more than reserved. Reserved: {QuantityReserved}, Requested: {quantity}");

        QuantityReserved -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fulfill(int quantity)
    {
        if (quantity > QuantityReserved)
            throw new InvalidOperationException($"Cannot fulfill more than reserved. Reserved: {QuantityReserved}, Requested: {quantity}");

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restock(int quantity)
    {
        QuantityOnHand += quantity;
        LastRestockedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLowStock() => QuantityAvailable <= ReorderLevel;
}
