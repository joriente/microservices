namespace ProductOrderingSystem.ProductService.Domain.Exceptions;

/// <summary>
/// Exception thrown when product validation fails
/// </summary>
public class ProductValidationException : Exception
{
    public string PropertyName { get; }
    public Dictionary<string, string[]> Errors { get; }

    public ProductValidationException(string propertyName, string message) 
        : base(message)
    {
        PropertyName = propertyName;
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = new[] { message }
        };
    }

    public ProductValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        PropertyName = string.Empty;
        Errors = errors;
    }

    public ProductValidationException(string propertyName, string message, Exception innerException)
        : base(message, innerException)
    {
        PropertyName = propertyName;
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = new[] { message }
        };
    }
}
