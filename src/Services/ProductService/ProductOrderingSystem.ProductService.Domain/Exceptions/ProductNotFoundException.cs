namespace ProductOrderingSystem.ProductService.Domain.Exceptions;

/// <summary>
/// Exception thrown when a product is not found
/// </summary>
public class ProductNotFoundException : Exception
{
    public string ProductId { get; }

    public ProductNotFoundException(string productId) 
        : base($"Product with ID '{productId}' was not found")
    {
        ProductId = productId;
    }

    public ProductNotFoundException(string productId, string message) 
        : base(message)
    {
        ProductId = productId;
    }
}
