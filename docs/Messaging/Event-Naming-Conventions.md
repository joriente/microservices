# Event Naming Conventions

This document describes the standardized event naming conventions used across all microservices.

## Event Naming Standard

All events follow these conventions:

1. **Record-based**: Events are defined as C# records for immutability
2. **"Event" suffix**: All event names end with "Event" (e.g., `PaymentProcessedEvent`)
3. **Positional parameters**: Use positional record syntax for clarity
4. **Timestamp included**: All events include a timestamp field (e.g., `ProcessedAt`, `CreatedAt`)
5. **Past tense**: Event names use past tense (e.g., "Created", "Processed", "Failed")

## Event Categories

### Product Events
- `ProductCreatedEvent` - Published when a new product is created
- `ProductUpdatedEvent` - Published when a product is updated
- `ProductDeletedEvent` - Published when a product is deleted
- `ProductReservedEvent` - Published when product stock is reserved for an order
- `ProductReservationFailedEvent` - Published when product reservation fails

### Order Events
- `OrderCreatedEvent` - Published when a new order is created
- `OrderStatusChangedEvent` - Published when order status changes
- `OrderCancelledEvent` - Published when an order is cancelled (triggers compensation)

### Payment Events
- `PaymentProcessedEvent` - Published when payment is successfully processed
- `PaymentFailedEvent` - Published when payment processing fails

### Inventory Events
- `InventoryReservedEvent` - Published when inventory is successfully reserved
- `InventoryReservationFailedEvent` - Published when inventory reservation fails
- `InventoryFulfilledEvent` - Published when inventory is fulfilled/shipped
- `InventoryReleasedEvent` - Published when reserved inventory is released

### Customer Events
- `CustomerCreatedIntegrationEvent` - Published when a new customer is created
- `CustomerUpdatedIntegrationEvent` - Published when customer information is updated

### User Events
- `UserCreatedEvent` - Published when a new user account is created

## Event Structure Examples

### Simple Event
```csharp
public record ProductDeletedEvent(
    string ProductId,
    DateTime DeletedAt
);
```

### Event with Complex Data
```csharp
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
```

### Event with Nested DTOs
```csharp
public record InventoryReservedEvent(
    Guid OrderId,
    List<ReservedItemDto> Items,
    DateTime ReservedAt
);

public record ReservedItemDto(
    Guid ProductId,
    int Quantity
);
```

## Publishing Events

Events are published using MassTransit:

```csharp
await context.Publish(new PaymentProcessedEvent(
    paymentId,
    orderId,
    userId,
    transactionId,
    amount,
    currency,
    status,
    DateTime.UtcNow
));
```

## Consuming Events

Events are consumed using MassTransit consumers:

```csharp
public class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        // Process event
    }
}
```

## Migration Notes

All events have been migrated from class-based (with properties) to record-based (with positional parameters):

**Before:**
```csharp
public class InventoryReserved
{
    public Guid OrderId { get; set; }
    public List<ReservedItem> Items { get; set; } = new();
}
```

**After:**
```csharp
public record InventoryReservedEvent(
    Guid OrderId,
    List<ReservedItemDto> Items,
    DateTime ReservedAt
);
```

## Benefits

1. **Consistency**: All events follow the same pattern
2. **Immutability**: Records ensure events cannot be modified
3. **Clarity**: "Event" suffix makes it clear these are domain events
4. **Auditability**: Timestamps enable event tracking and debugging
5. **Type Safety**: Positional parameters provide compile-time safety
