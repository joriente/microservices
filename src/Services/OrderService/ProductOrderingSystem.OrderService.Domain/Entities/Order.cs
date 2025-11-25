using System.ComponentModel.DataAnnotations;

namespace ProductOrderingSystem.OrderService.Domain.Entities;

public class Order
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    public string CustomerEmail { get; set; } = string.Empty;
    
    public string CustomerName { get; set; } = string.Empty;
    
    public List<OrderItem> Items { get; set; } = [];
    
    public decimal TotalAmount { get; private set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? Notes { get; set; }
    
    public string? CancellationReason { get; set; }

    // Parameterless constructor for EF Core
    public Order() { }

    // Constructor for creating new orders
    public Order(string customerId, string customerEmail, string customerName, List<OrderItem> items, string? notes = null)
    {
        Id = Guid.NewGuid().ToString();
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        CustomerName = customerName;
        Items = items ?? [];
        Notes = notes;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        
        CalculateTotal();
    }

    public void CalculateTotal()
    {
        TotalAmount = Items.Sum(item => item.TotalPrice);
    }

    public void AddItem(string productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.CalculateTotal();
        }
        else
        {
            Items.Add(new OrderItem(productId, productName, unitPrice, quantity));
        }
        
        CalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(string productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
            CalculateTotal();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeCancelled()
    {
        return Status is OrderStatus.Pending or OrderStatus.Confirmed;
    }

    public void Cancel(string reason)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException($"Order cannot be cancelled when status is {Status}");
        
        CancellationReason = reason;
        UpdateStatus(OrderStatus.Cancelled);
    }
    
    // Legacy method for backward compatibility
    public void Cancel()
    {
        Cancel("Order cancelled");
    }
}