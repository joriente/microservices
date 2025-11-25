namespace ProductOrderingSystem.OrderService.Domain.Entities;

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    
    public string ProductName { get; set; } = string.Empty;
    
    public decimal UnitPrice { get; set; }
    
    public int Quantity { get; set; }
    
    public decimal TotalPrice { get; private set; }

    public OrderItem()
    {
        CalculateTotal();
    }

    public OrderItem(string productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CalculateTotal();
    }

    public void CalculateTotal()
    {
        TotalPrice = UnitPrice * Quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));
        
        Quantity = newQuantity;
        CalculateTotal();
    }
}