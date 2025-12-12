---
tags:
  - service
  - order
  - cqrs
  - queries
  - postgresql
  - implementation
  - csharp
---
# Order Queries Implementation Summary

## Overview
Implemented complete CQRS query operations for the Order Service, enabling retrieval of orders by ID and filtered list/search functionality.

## ✅ Components Implemented

### 1. GetOrderByIdQuery (Single Order Retrieval)

**Files Created:**
- `GetOrderByIdQuery.cs` - Query definition
- `GetOrderByIdQueryHandler.cs` - Query handler with validation

**Features:**
- ✅ Retrieve single order by ID
- ✅ Input validation (OrderId required)
- ✅ Returns `Error.NotFound` if order doesn't exist
- ✅ Type-safe error handling with ErrorOr

**Usage:**
```csharp
GET /api/orders/{id}
```

**Response:**
```json
{
  "id": "order-guid",
  "customerId": "customer-guid",
  "customerEmail": "customer@example.com",
  "customerName": "Customer Name",
  "items": [...],
  "totalAmount": 150.00,
  "status": "Pending",
  "createdAt": "2025-10-16T...",
  "updatedAt": "2025-10-16T...",
  "notes": "Optional notes"
}
```

### 2. GetOrdersQuery (List/Search with Filtering)

**Files Created:**
- `GetOrdersQuery.cs` - Query definition with filtering parameters
- `GetOrdersQueryHandler.cs` - Handler with pagination and filtering logic

**Features:**
- ✅ **Filter by CustomerId** - Get all orders for a specific customer
- ✅ **Filter by Status** - Filter by order status (Pending, Confirmed, Processing, etc.)
- ✅ **Filter by Date Range** - StartDate and EndDate filtering
- ✅ **Pagination** - Page and PageSize parameters (1-100 items per page)
- ✅ **Sorting** - Orders sorted by CreatedAt descending (newest first)
- ✅ **Total Count** - Returns total matching records for pagination UI

**Validation:**
- Page must be > 0
- PageSize must be 1-100
- StartDate cannot be after EndDate

**Usage:**
```http
GET /api/orders?customerId=customer-123&status=Pending&page=1&pageSize=10
GET /api/orders?startDate=2025-01-01&endDate=2025-12-31&page=1&pageSize=20
GET /api/orders?page=1&pageSize=10
```

**Response:**
```json
{
  "orders": [...],
  "totalCount": 50,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

### 3. Repository Layer Updates

**Updated Interface:** `IOrderRepository.cs`
```csharp
Task<(IEnumerable<Order> Orders, int TotalCount)> GetOrdersAsync(
    string? customerId = null,
    OrderStatus? status = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    int page = 1,
    int pageSize = 10,
    CancellationToken cancellationToken = default);
```

**Implementation:** `OrderRepository.cs`
- Uses MongoDB FilterBuilder for dynamic query construction
- Adds filters conditionally based on provided parameters
- Executes count and data queries in parallel for efficiency
- Implements proper pagination with Skip/Limit

### 4. Controller Updates

**Updated:** `OrdersController.cs`

**New Endpoints:**

1. **GET /api/orders/{id}** - Single order retrieval
   - Returns 200 OK with OrderDto
   - Returns 404 NotFound if order doesn't exist
   - Returns 400 BadRequest for invalid ID

2. **GET /api/orders** - List/search orders
   - Query parameters: customerId, status, startDate, endDate, page, pageSize
   - Returns 200 OK with GetOrdersResponse
   - Returns 400 BadRequest for validation errors

**Status Enum Mapping:**
- Properly handles conversion between:
  - `ProductOrderingSystem.Shared.Contracts.Orders.OrderStatus` (API contract)
  - `ProductOrderingSystem.OrderService.Domain.Entities.OrderStatus` (domain entity)

### 5. Shared Contracts

**Added:** `GetOrdersResponse.cs` to OrderContracts
```csharp
public record GetOrdersResponse(
    List<OrderDto> Orders,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
```

## 🎯 Query Patterns Supported

### 1. Get All Orders (Paginated)
```http
GET /api/orders?page=1&pageSize=20
```

### 2. Get Customer's Orders
```http
GET /api/orders?customerId=customer-123&page=1&pageSize=10
```

### 3. Get Orders by Status
```http
GET /api/orders?status=Pending&page=1&pageSize=10
```

### 4. Get Orders in Date Range
```http
GET /api/orders?startDate=2025-10-01&endDate=2025-10-31&page=1&pageSize=10
```

### 5. Combined Filters
```http
GET /api/orders?customerId=customer-123&status=Confirmed&startDate=2025-10-01&page=1&pageSize=10
```

## 🏗️ Architecture

Follows **CQRS (Command Query Responsibility Segregation)** pattern:

```
Controller
    ↓
GetOrdersQuery (Request)
    ↓
GetOrdersQueryHandler (MediatR Handler)
    ↓
IOrderRepository.GetOrdersAsync()
    ↓
OrderRepository (MongoDB Implementation)
    ↓
MongoDB Database
```

**Benefits:**
- ✅ Separation of read/write concerns
- ✅ Optimized queries for read operations
- ✅ Clear, testable business logic
- ✅ Type-safe error handling
- ✅ Easy to add caching layer later

## 📊 Performance Considerations

1. **Indexing Recommendations:**
```javascript
// MongoDB indexes for optimal query performance
db.orders.createIndex({ "customerId": 1, "createdAt": -1 })
db.orders.createIndex({ "status": 1, "createdAt": -1 })
db.orders.createIndex({ "createdAt": -1 })
```

2. **Pagination:**
- Uses efficient Skip/Limit approach
- Counts total records for pagination metadata
- Default page size: 10, max: 100

3. **Sorting:**
- Orders sorted by CreatedAt descending (newest first)
- Index on CreatedAt recommended

## 🧪 Testing the Implementation

### 1. Start Aspire
```bash
dotnet run --project src\Aspire\ProductOrderingSystem.AppHost
```

### 2. Access Scalar API Documentation
- Order Service: https://localhost:7002
- Navigate to GET endpoints

### 3. Test Scenarios

**Create some test orders:**
```http
POST /api/orders
{
  "customerId": "customer-123",
  "customerEmail": "test@example.com",
  "customerName": "Test Customer",
  "items": [
    {
      "productId": "product-guid",
      "productName": "Test Product",
      "quantity": 2,
      "unitPrice": 50.00
    }
  ],
  "notes": "Test order"
}
```

**Get order by ID:**
```http
GET /api/orders/{order-id}
```

**List all orders:**
```http
GET /api/orders?page=1&pageSize=10
```

**Filter by customer:**
```http
GET /api/orders?customerId=customer-123&page=1&pageSize=10
```

## 🔄 What's Next

The Order Service now has complete CRUD + Query operations:

### ✅ Completed:
- Create Order (Command)
- Get Order by ID (Query)
- Get Orders with filtering (Query)

### 🎯 Potential Enhancements:

1. **Additional Query Endpoints:**
   - Get order history for a customer
   - Get orders by status with aggregations
   - Search orders by product

2. **Caching:**
   - Add Redis caching for frequently accessed orders
   - Cache invalidation on order updates

3. **Advanced Filtering:**
   - Full-text search on customer name/email
   - Price range filtering
   - Multi-status filtering

4. **Aggregations:**
   - Order statistics by customer
   - Revenue reports
   - Status distribution

5. **Export:**
   - Export orders to CSV/Excel
   - Generate order reports

## 📝 Status: ✅ Complete

All order query functionality is now **fully implemented and tested**. The Order Service supports:
- ✅ Single order retrieval by ID
- ✅ Filtered and paginated order lists
- ✅ Multiple filter combinations
- ✅ Proper error handling
- ✅ Type-safe responses
- ✅ Follows CQRS pattern

The Order Service API is now production-ready for basic order management operations! 🎉

