namespace ProductOrderingSystem.AnalyticsService.Domain.Entities;

public class OrderEvent
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime EventTimestamp { get; set; }
}

public class PaymentEvent
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
}

public class ProductEvent
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
}

public class InventoryEvent
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
}
