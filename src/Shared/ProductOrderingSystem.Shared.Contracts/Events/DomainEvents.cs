using System.Text.Json.Serialization;

namespace ProductOrderingSystem.Shared.Contracts.Events
{
    public record ProductCreatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt
    );

    public record ProductUpdatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt
    );

    public record ProductDeletedEvent(
        string ProductId,
        DateTime DeletedAt
    );

    public record OrderCreatedEvent(
        [property: JsonPropertyName("orderId")] Guid OrderId,
        [property: JsonPropertyName("customerId")] Guid CustomerId,
        [property: JsonPropertyName("items")] List<OrderItemDto>? Items,
        [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt
    );

    public record OrderItemDto(
        [property: JsonPropertyName("productId")] Guid ProductId,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("unitPrice")] decimal UnitPrice
    );

    public record ProductReservedEvent(
        Guid OrderId,
        Guid ProductId,
        int Quantity,
        DateTime ReservedAt
    );

    public record OrderStatusChangedEvent(
        string OrderId,
        string PreviousStatus,
        string NewStatus,
        DateTime ChangedAt
    );

    public record UserCreatedEvent(
        string UserId,
        string Email,
        string FirstName,
        string LastName,
        DateTime CreatedAt
    );

    public record PaymentProcessedEvent(
        Guid PaymentId,
        Guid OrderId,
        Guid UserId,
        string StripePaymentIntentId,
        decimal Amount,
        string Currency,
        string Status,
        DateTime ProcessedAt
    );

    public record PaymentFailedEvent(
        Guid PaymentId,
        Guid OrderId,
        Guid UserId,
        string Reason,
        DateTime FailedAt
    );

    // Customer Service Events
    public record CustomerCreatedIntegrationEvent(
        Guid CustomerId,
        string Email,
        string FirstName,
        string LastName,
        DateTime CreatedAt
    );

    public record CustomerUpdatedIntegrationEvent(
        Guid CustomerId,
        string Email,
        string FirstName,
        string LastName,
        DateTime UpdatedAt
    );
}
