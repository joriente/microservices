using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductOrderingSystem.InventoryService.Models;

/// <summary>
/// Tracks inventory reservations for orders
/// </summary>
[Table("inventory_reservations")]
public class InventoryReservation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("fulfilled_at")]
    public DateTime? FulfilledAt { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }
}

public enum ReservationStatus
{
    Reserved,
    Fulfilled,
    Canceled,
    Expired
}
