---
tags:
  - service
  - cart
  - rabbitmq
  - masstransit
  - messaging
  - integration
  - csharp
---
# Cart Service RabbitMQ Integration

## Overview
The Cart Service now uses RabbitMQ for event-driven communication with other microservices, following the same patterns as the Order Service.

## Architecture Pattern

### Event-Driven Product Synchronization
The Cart Service maintains a local **Product Cache** synchronized via RabbitMQ events from the Product Service. This enables:
- ✅ Cart item validation without API calls
- ✅ Accurate product names and prices in carts
- ✅ High availability (no dependency on Product Service for read operations)
- ✅ Low latency cart operations
- ✅ Eventual consistency across services

### Automatic Cart Clearing
The Cart Service consumes `OrderCreatedEvent` from the Order Service to automatically clear the customer's cart after order placement, ensuring a clean user experience.

---

## Implementation Details

### 1. Product Cache Entity
**File:** `ProductOrderingSystem.CartService.Domain/Entities/ProductCacheEntry.cs`

```csharp
public class ProductCacheEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

Stores local copy of product data for quick access during cart operations.

### 2. Product Cache Repository
**File:** `ProductOrderingSystem.CartService.Domain/Repositories/IProductCacheRepository.cs`

```csharp
public interface IProductCacheRepository
{
    Task<ProductCacheEntry?> GetByIdAsync(string productId);
    Task UpsertAsync(ProductCacheEntry product, CancellationToken cancellationToken = default);
    Task DeleteAsync(string productId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string productId);
}
```

MongoDB implementation with unique index on product ID.

---

## RabbitMQ Consumers

### 3. ProductCreatedEventConsumer
**File:** `ProductOrderingSystem.CartService.Application/Consumers/ProductCreatedEventConsumer.cs`

**Purpose:** Consumes `ProductCreatedEvent` from Product Service and caches the new product.

**Flow:**
1. Receives `ProductCreatedEvent` from RabbitMQ
2. Creates `ProductCacheEntry` with product details
3. Upserts to MongoDB product_cache collection
4. Logs success or error

**Benefits:**
- New products immediately available for cart operations
- No API calls needed to Product Service
- Reduces coupling between services

### 4. ProductUpdatedEventConsumer
**File:** `ProductOrderingSystem.CartService.Application/Consumers/ProductUpdatedEventConsumer.cs`

**Purpose:** Keeps cached product data synchronized when products are updated.

**Flow:**
1. Receives `ProductUpdatedEvent` from RabbitMQ
2. Gets existing cache entry (or creates new if missing)
3. Updates Name, Price, StockQuantity
4. Upserts to MongoDB
5. Logs success or error

**Benefits:**
- Cart items show current product names and prices
- Automatic synchronization (eventual consistency)
- Handles missing entries gracefully

### 5. ProductDeletedEventConsumer
**File:** `ProductOrderingSystem.CartService.Application/Consumers/ProductDeletedEventConsumer.cs`

**Purpose:** Removes deleted products from cache to prevent them from being added to carts.

**Flow:**
1. Receives `ProductDeletedEvent` from RabbitMQ
2. Deletes product from MongoDB cache
3. Logs success or error

**Benefits:**
- Deleted products cannot be added to new carts
- Data consistency across services
- Clean cache (no stale data)

### 6. OrderCreatedEventConsumer
**File:** `ProductOrderingSystem.CartService.Application/Consumers/OrderCreatedEventConsumer.cs`

**Purpose:** Automatically clears customer's cart after successful order creation.

**Flow:**
1. Receives `OrderCreatedEvent` from Order Service
2. Gets customer's cart by CustomerId
3. Calls `cart.Clear()` to remove all items
4. Updates cart in MongoDB
5. Logs success (non-critical, doesn't throw on error)

**Benefits:**
- Automatic cart cleanup after order
- Better user experience (no accidental re-orders)
- Reduces manual cart management
- Fire-and-forget pattern (non-critical operation)

---

## Configuration

### Program.cs Registration
**File:** `ProductOrderingSystem.CartService.WebAPI/Program.cs`

```csharp
// Register Product Cache Repository
builder.Services.AddScoped<IProductCacheRepository, ProductCacheRepository>();

// Configure RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    // Register event consumers
    x.AddConsumer<ProductCreatedEventConsumer>();
    x.AddConsumer<ProductUpdatedEventConsumer>();
    x.AddConsumer<ProductDeletedEventConsumer>();
    x.AddConsumer<OrderCreatedEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString)); // Aspire RabbitMQ
        }
        else
        {
            cfg.Host("localhost", "/", h => // Fallback for local dev
            {
                h.Username("guest");
                h.Password("guest");
            });
        }

        cfg.ConfigureEndpoints(context); // Auto-configure queues
    });
});
```

---

## Event Flow Diagram

### Product Synchronization
```
Product Service              RabbitMQ                Cart Service
     |                          |                          |
     |--[ProductCreatedEvent]-->|                          |
     |                          |--[ProductCreatedEvent]-->|
     |                          |                          |--[Cache Product]
     |                          |                          |
     |--[ProductUpdatedEvent]-->|                          |
     |                          |--[ProductUpdatedEvent]-->|
     |                          |                          |--[Update Cache]
     |                          |                          |
     |--[ProductDeletedEvent]-->|                          |
     |                          |--[ProductDeletedEvent]-->|
     |                          |                          |--[Remove from Cache]
```

### Cart Clearing After Order
```
Order Service                RabbitMQ                Cart Service
     |                          |                          |
     |--[OrderCreatedEvent]---->|                          |
     |                          |--[OrderCreatedEvent]---->|
     |                          |                          |--[Get Cart by CustomerId]
     |                          |                          |--[Clear Cart]
     |                          |                          |--[Update MongoDB]
```

---

## MongoDB Collections

### cartdb.product_cache
Stores cached product data for cart operations.

**Schema:**
```json
{
  "_id": "string (ProductId)",
  "Id": "string",
  "Name": "string",
  "Price": "decimal",
  "StockQuantity": "int",
  "IsActive": "bool",
  "CreatedAt": "DateTime",
  "LastUpdated": "DateTime"
}
```

**Indexes:**
- Unique index on `Id` field

### cartdb.carts
Existing cart collection with customer carts.

---

## Benefits of This Architecture

### 1. **Loose Coupling**
- Cart Service doesn't call Product Service API
- Services communicate via events (async)
- Changes in Product Service don't break Cart Service

### 2. **High Availability**
- Cart operations work even if Product Service is down
- Local cache provides product data
- Eventual consistency acceptable for cart operations

### 3. **Performance**
- No network calls to Product Service for every cart operation
- Fast local MongoDB queries
- Reduced latency for users

### 4. **Scalability**
- Services scale independently
- RabbitMQ handles message distribution
- Multiple Cart Service instances can consume events

### 5. **Consistency**
- Product data automatically synchronized
- Deleted products removed from cache
- Cart cleared automatically after order

---

## Comparison: Cart Service vs Order Service

| Aspect | Cart Service | Order Service |
|--------|--------------|---------------|
| **Customer Data** | From JWT claims | From JWT claims |
| **Product Data** | Cached via RabbitMQ events | Cached via RabbitMQ events |
| **Product Cache** | ✅ Yes (for validation) | ✅ Yes (for order creation) |
| **Consumes Product Events** | ✅ Yes (3 consumers) | ✅ Yes (3 consumers) |
| **Consumes Order Events** | ✅ Yes (OrderCreated) | ❌ No |
| **Publishes Events** | ❌ No | ✅ Yes (OrderCreated) |
| **Database** | cartdb (MongoDB) | orderdb (MongoDB) |
| **Event Bus** | RabbitMQ (MassTransit) | RabbitMQ (MassTransit) |

---

## Testing the Integration

### 1. Verify Consumers are Running
Check Aspire Dashboard logs for Cart Service:
```
Received ProductCreatedEvent for Product {ProductId} ({ProductName})
Successfully cached Product {ProductId} ({ProductName}) with Price {Price:C}
```

### 2. Create a Product
```bash
POST http://localhost:5000/api/products
{
  "name": "Test Product",
  "price": 99.99,
  "stockQuantity": 10
}
```

Expected: Cart Service logs show ProductCreatedEvent received and cached.

### 3. Add Product to Cart
```bash
POST http://localhost:5000/api/carts/items
{
  "productId": "{productId}",
  "productName": "Test Product",
  "price": 99.99,
  "quantity": 2
}
```

Expected: Cart Service uses cached product data for validation.

### 4. Create Order
```bash
POST http://localhost:5000/api/orders
{
  "customerId": "{userId}",
  "customerEmail": "user@example.com",
  "items": [...]
}
```

Expected: Cart Service receives OrderCreatedEvent and clears the cart.

---

## Future Enhancements

### 1. Cart Service Could Publish Events
- `CartAbandonedEvent` (after X days of inactivity)
- `CartItemAddedEvent` (for analytics)
- `CartClearedEvent` (for audit trail)

### 2. Stock Validation
- Use cached `StockQuantity` to prevent adding out-of-stock items
- Show stock availability in cart UI
- Reserve stock when adding to cart (saga pattern)

### 3. Price Change Notifications
- Notify users when cart item prices change
- Show "price changed" indicator in cart UI
- Require confirmation before checkout

### 4. Product Availability Checks
- Validate product is still active before adding to cart
- Remove inactive products from existing carts
- Show availability status

---

## Summary

✅ **Cart Service now uses RabbitMQ** for event-driven communication  
✅ **Product cache** maintained locally for high performance  
✅ **4 consumers** registered (3 product events + 1 order event)  
✅ **Automatic cart clearing** after order creation  
✅ **Same patterns** as Order Service (consistency across codebase)  
✅ **Loose coupling** and high availability  
✅ **Eventual consistency** model for better scalability  

The Cart Service is now fully integrated into the event-driven microservices architecture! 🚀

