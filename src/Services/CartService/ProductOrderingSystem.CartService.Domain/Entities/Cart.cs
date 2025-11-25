namespace ProductOrderingSystem.CartService.Domain.Entities;

/// <summary>
/// Shopping cart aggregate root
/// </summary>
public class Cart
{
    public string Id { get; private set; }
    public string CustomerId { get; private set; }
    public string CustomerEmail { get; private set; }
    public List<CartItem> Items { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
    public int TotalItems => Items.Sum(item => item.Quantity);

    private Cart()
    {
        Id = string.Empty;
        CustomerId = string.Empty;
        CustomerEmail = string.Empty;
        Items = new List<CartItem>();
    }

    public Cart(string customerId, string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
        
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("Customer email cannot be empty", nameof(customerEmail));

        Id = Guid.NewGuid().ToString();
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        Items = new List<CartItem>();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(string productId, string productName, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            // Update quantity if item already exists
            UpdateItemQuantity(productId, existingItem.Quantity + quantity);
        }
        else
        {
            // Add new item
            Items.Add(new CartItem(productId, productName, price, quantity));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not found in cart");

        Items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateItemQuantity(string productId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not found in cart");

        // Remove the old item and add updated one (since CartItem is immutable)
        Items.Remove(item);
        Items.Add(new CartItem(item.ProductId, item.ProductName, item.Price, newQuantity));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEmpty() => Items.Count == 0;
}
