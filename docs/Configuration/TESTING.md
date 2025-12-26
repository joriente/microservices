---
tags:
  - configuration
  - testing
  - unit-tests
  - integration-tests
  - nunit
  - xunit
  - coverage
---
# Testing Guide

## Overview

This document covers testing approaches for the Product Ordering System, including .NET and Java services, unit tests, integration tests, and end-to-end testing.

## Test Architecture

### Test Pyramid

```
                    E2E Tests (Few)
                  /                 \
            Integration Tests (Some)
          /                           \
      Unit Tests (Many)
```

- **Unit Tests**: Fast, isolated, test individual components
- **Integration Tests**: Real dependencies (databases, message brokers)
- **End-to-End Tests**: Complete user flows across all services

## Running Tests

### All Tests

```powershell
# Run all .NET tests
dotnet test

# Run Java NotificationService tests
cd src/Services/NotificationService
mvn test
```

### End-to-End Tests

```powershell
# Start all services first
.\Start-all.ps1

# Then run E2E tests
.\tests\Test-EndToEnd.ps1
```

**Test Coverage** (27 tests total):
- ✅ Authentication (4 tests) - Registration, login, JWT authorization
- ✅ Products (3 tests) - CRUD operations, REST compliance
- ✅ Cart (10 tests) - Add/update/remove items, total calculation
- ✅ Orders (4 tests) - Order creation, REST compliance
- ✅ Event-Driven Integration (3 tests) - RabbitMQ event processing
- ✅ REST API Compliance (3 tests) - Location headers, pagination

### Integration Tests

```powershell
# IdentityService
dotnet test tests/IdentityService/ProductOrderingSystem.IdentityService.IntegrationTests/

# OrderService
dotnet test tests/OrderService/ProductOrderingSystem.OrderService.IntegrationTests/

# CartService
dotnet test tests/ProductOrderingSystem.CartService.Tests/
```

### Unit Tests

```powershell
# By service
dotnet test tests/IdentityService/ProductOrderingSystem.IdentityService.Domain.UnitTests/
dotnet test tests/OrderService/ProductOrderingSystem.OrderService.Domain.UnitTests/

# All unit tests
dotnet test --filter Category=Unit
```

## Testing Polyglot Services

### .NET Services
Use xUnit, FluentAssertions, and Testcontainers:

```csharp
public class OrderServiceTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    
    [Fact]
    public async Task CreateOrder_Should_Return201_WithLocationHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var order = new { customerId = "...", items = [...] };
        
        // Act
        var response = await client.PostAsJsonAsync("/api/orders", order);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}
```

### Java NotificationService
Use JUnit 5, AssertJ, and Testcontainers:

```java
@SpringBootTest
@Testcontainers
class NotificationServiceTests {
    
    @Container
    static MongoDBContainer mongodb = new MongoDBContainer("mongo:8");
    
    @Test
    void shouldSendOrderConfirmationEmail() {
        // Arrange
        OrderCreatedEvent event = new OrderCreatedEvent(...);
        
        // Act
        notificationService.sendOrderConfirmationEmail(event);
        
        // Assert
        Notification notification = repository.findByOrderId(event.getOrderId());
        assertThat(notification.getStatus()).isEqualTo(NotificationStatus.SENT);
    }
}
```

## Event-Driven Testing

### Testing Event Consumers

**Cart Service - Product Caching:**
```csharp
[Fact]
public async Task ProductCreatedEvent_Should_CacheProduct()
{
    // Arrange
    var productEvent = new ProductCreatedEvent
    {
        ProductId = Guid.NewGuid(),
        Name = "Test Product",
        Price = 29.99m
    };
    
    // Act
    await _consumer.Consume(productEvent);
    
    // Assert
    var cached = await _repository.GetProductAsync(productEvent.ProductId);
    cached.Should().NotBeNull();
    cached.Price.Should().Be(29.99m);
}
```

**Cart Service - Auto-Clear After Order:**
```csharp
[Fact]
public async Task OrderCreatedEvent_Should_ClearCart()
{
    // Arrange
    var cart = await CreateCartWithItems();
    var orderEvent = new OrderCreatedEvent { CustomerId = cart.CustomerId };
    
    // Act
    await _consumer.Consume(orderEvent);
    
    // Assert
    var clearedCart = await _repository.GetByCustomerIdAsync(cart.CustomerId);
    clearedCart.Items.Should().BeEmpty();
}
```

### Testing Event Publishers

Use test doubles to verify events are published:

```csharp
[Fact]
public async Task CreateOrder_Should_PublishOrderCreatedEvent()
{
    // Arrange
    var publishEndpoint = Substitute.For<IPublishEndpoint>();
    var handler = new CreateOrderCommandHandler(..., publishEndpoint);
    
    // Act
    await handler.Handle(new CreateOrderCommand(...));
    
    // Assert
    await publishEndpoint.Received(1).Publish(
        Arg.Is<OrderCreatedEvent>(e => e.OrderId != Guid.Empty));
}
```

## Testing ErrorOr Patterns

### Success Path Testing

When testing handlers that return `ErrorOr<T>`, verify both the result type and value:

```csharp
[Fact]
public async Task GetProductById_Should_ReturnProduct_WhenExists()
{
    // Arrange
    var productId = Guid.NewGuid();
    var expectedProduct = new Product { Id = productId, Name = "Test" };
    _repository.GetByIdAsync(productId).Returns(expectedProduct);
    
    // Act
    var result = await _handler.Handle(new GetProductByIdQuery(productId));
    
    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Should().NotBeNull();
    result.Value.Id.Should().Be(productId);
    result.Value.Name.Should().Be("Test");
}
```

### Error Path Testing

Verify error cases using `ErrorType` and error codes:

```csharp
[Fact]
public async Task GetProductById_Should_ReturnNotFound_WhenProductDoesNotExist()
{
    // Arrange
    var productId = Guid.NewGuid();
    _repository.GetByIdAsync(productId).Returns((Product?)null);
    
    // Act
    var result = await _handler.Handle(new GetProductByIdQuery(productId));
    
    // Assert
    result.IsError.Should().BeTrue();
    result.FirstError.Type.Should().Be(ErrorType.NotFound);
    result.FirstError.Code.Should().Contain("Product");
}
```

### Validation Error Testing

```csharp
[Fact]
public async Task CreateProduct_Should_ReturnValidationError_WhenPriceIsNegative()
{
    // Arrange
    var command = new CreateProductCommand { Price = -10m };
    
    // Act
    var result = await _handler.Handle(command);
    
    // Assert
    result.IsError.Should().BeTrue();
    result.FirstError.Type.Should().Be(ErrorType.Validation);
    result.FirstError.Code.Should().Be("Price");
    result.FirstError.Description.Should().Contain("must be positive");
}
```

## Architecture Testing

### Testing Handler Patterns

Architecture tests ensure consistency across the codebase. Use NetArchTest to validate handler patterns:

```csharp
[Test]
public void Handlers_ShouldImplementMediatRInterface()
{
    // Get all handler types, excluding Wolverine nested handlers
    var handlerTypes = Types.InAssembly(InventoryServiceAssembly)
        .That()
        .ResideInNamespaceContaining("Features")
        .And()
        .HaveNameEndingWith("Handler")
        .GetTypes()
        .Where(t => !t.IsNested)  // Exclude Wolverine convention-based handlers
        .ToList();
    
    // Verify standalone handlers implement MediatR interfaces
    var nonMediatRHandlers = handlerTypes
        .Where(t => !t.GetInterfaces().Any(i => 
            i.IsGenericType && 
            i.GetGenericTypeDefinition().Name.Contains("IRequestHandler")))
        .ToList();
    
    nonMediatRHandlers.Should().BeEmpty(
        "all standalone Handler classes should implement IRequestHandler");
}
```

**Key Distinctions:**
- **MediatR Handlers**: Standalone classes, must implement `IRequestHandler<TRequest, TResponse>`
- **Wolverine Handlers**: Nested within static feature classes, use convention-based `Handle` methods
- **Architecture Test Filter**: Use `Type.IsNested` property to distinguish between patterns

### Testing Vertical Slice Organization

```csharp
[Test]
public void VerticalSlices_ShouldBeInFeaturesNamespace()
{
    var result = Types.InAssembly(Assembly)
        .That()
        .HaveNameEndingWith("Handler")
        .Should()
        .ResideInNamespaceContaining("Features")
        .GetResult();
    
    result.IsSuccessful.Should().BeTrue();
}
```

## NotificationService Testing

### Health Check
```powershell
curl http://localhost:8085/actuator/health
```

Expected response:
```json
{
  "status": "UP",
  "components": {
    "mongo": { "status": "UP" },
    "rabbit": { "status": "UP" }
  }
}
```

### Testing Email Sending

**With SendGrid Disabled** (default for testing):
```powershell
$env:SENDGRID_ENABLED="false"
mvn spring-boot:run
```
Emails are logged but not sent.

**With SendGrid Enabled**:
```powershell
$env:SENDGRID_ENABLED="true"
$env:SENDGRID_API_KEY="your_test_key"
mvn spring-boot:run
```

### Manual Event Testing

Publish test event via RabbitMQ Management UI (http://localhost:15672):

1. Navigate to **Exchanges** → `ProductOrderingSystem.Shared.Contracts.Events:OrderCreatedEvent`
2. Expand **Publish message**
3. Paste JSON:
```json
{
  "orderId": "123e4567-e89b-12d3-a456-426614174000",
  "customerId": "123e4567-e89b-12d3-a456-426614174001",
  "customerEmail": "test@example.com",
  "totalAmount": 59.98,
  "createdAt": "2025-10-27T10:00:00Z"
}
```
4. Click **Publish**
5. Verify notification created:
```powershell
curl http://localhost:8085/api/notifications
```

## Test Data Management

### Using Testcontainers

Both .NET and Java tests use Testcontainers for isolated test databases:

**.NET:**
```csharp
public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();
    
    public async Task InitializeAsync() => await _mongoContainer.StartAsync();
    public async Task DisposeAsync() => await _mongoContainer.DisposeAsync();
}
```

**Java:**
```java
@Testcontainers
class IntegrationTest {
    @Container
    static MongoDBContainer mongodb = new MongoDBContainer("mongo:8")
        .withExposedPorts(27017);
}
```

### Test Data Builders

Create reusable test data builders:

```csharp
public class OrderBuilder
{
    private Guid _customerId = Guid.NewGuid();
    private List<OrderItem> _items = new();
    
    public OrderBuilder WithCustomer(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }
    
    public OrderBuilder AddItem(Guid productId, int quantity, decimal price)
    {
        _items.Add(new OrderItem(productId, quantity, price));
        return this;
    }
    
    public Order Build() => new Order(_customerId, _items);
}

// Usage
var order = new OrderBuilder()
    .WithCustomer(customerId)
    .AddItem(productId, 2, 29.99m)
    .Build();
```

## REST API Testing

### POST Endpoints - 201 Created
```csharp
[Fact]
public async Task CreateProduct_Should_Return201WithLocationHeader()
{
    var response = await _client.PostAsJsonAsync("/api/products", product);
    
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Headers.Location.Should().NotBeNull();
    response.Content.Headers.ContentLength.Should().Be(0); // Empty body
}
```

### GET Endpoints - Pagination
```csharp
[Fact]
public async Task GetProducts_Should_ReturnArrayWithPaginationHeader()
{
    var response = await _client.GetAsync("/api/products?page=1&pageSize=10");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Headers.Should().ContainKey("Pagination");
    
    var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
    products.Should().NotBeNull();
}
```

## Common Testing Patterns

### 1. AAA Pattern (Arrange-Act-Assert)
```csharp
[Fact]
public async Task Test_Description()
{
    // Arrange - Set up test data and dependencies
    var product = new Product("Test", 29.99m);
    
    // Act - Execute the operation
    var result = await _service.CreateAsync(product);
    
    // Assert - Verify the outcome
    result.Should().BeSuccess();
}
```

### 2. Test Naming Convention
```
MethodName_Should_ExpectedBehavior_When_Condition
```

Examples:
- `CreateOrder_Should_ReturnSuccess_When_ValidData`
- `GetProduct_Should_ThrowNotFoundException_When_ProductDoesNotExist`
- `AddToCart_Should_IncreaseQuantity_When_ProductAlreadyInCart`

### 3. One Assert Per Test (Guideline)
Keep tests focused on a single behavior:

```csharp
// Good
[Fact]
public async Task CreateOrder_Should_PublishEvent()
{
    await _handler.Handle(command);
    await _publisher.Received(1).Publish(Arg.Any<OrderCreatedEvent>());
}

// Avoid
[Fact]
public async Task CreateOrder_Should_DoEverything() // Too broad
{
    // Multiple unrelated assertions...
}
```

## Monitoring Test Execution

### Aspire Dashboard
When running integration tests with Aspire:
- View service logs in real-time
- See distributed traces
- Monitor resource usage

### RabbitMQ Management UI
- Check message flow: http://localhost:15672
- Verify queues are created
- Monitor message rates

### Aspire Dashboard
- Centralized logging and telemetry: http://localhost:15888
- Search by OrderId, CustomerId
- Filter by log level and service

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Setup Java
        uses: actions/setup-java@v3
        with:
          java-version: '21'
          distribution: 'temurin'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run .NET tests
        run: dotnet test --no-build --verbosity normal
      
      - name: Run Java tests
        run: |
          cd src/Services/NotificationService
          mvn test
```

## Best Practices

### ✅ DO:
- Write tests alongside production code
- Use Testcontainers for integration tests
- Test event-driven flows
- Verify REST compliance (status codes, headers)
- Use test data builders
- Keep tests fast and independent
- Mock external dependencies (SendGrid, Stripe)

### ❌ DON'T:
- Share state between tests
- Use production databases for testing
- Commit test API keys to source control
- Skip testing event consumers
- Ignore integration tests
- Test implementation details (test behavior, not internals)

## Test Summary

| Test Type | Count | Pass Rate | Coverage |
|-----------|-------|-----------|----------|
| Unit Tests (.NET) | 70+ | 100% | Domain logic, handlers |
| Integration Tests (.NET) | 25+ | 100% | API endpoints, auth |
| Unit Tests (Java) | 15+ | 100% | Services, consumers |
| E2E Tests | 27 | 100% | Complete user flows |
| **Total** | **137+** | **100%** | **Comprehensive** |

## Related Documentation

- [QUICKSTART.md](QUICKSTART.md) - Quick start guide
- [POLYGLOT_INTEGRATION.md](POLYGLOT_INTEGRATION.md) - Java/C# integration
- [Event-Naming-Conventions.md](Event-Naming-Conventions.md) - Event contracts
- [CartService-RabbitMQ-Integration.md](CartService-RabbitMQ-Integration.md) - Event consumer patterns

## Conclusion

The test suite provides comprehensive coverage across:
- ✅ Individual service logic (unit tests)
- ✅ Service integration (integration tests)
- ✅ Cross-service communication (event-driven tests)
- ✅ Complete user journeys (E2E tests)
- ✅ Polyglot service integration (.NET ↔ Java)

All tests run automatically in CI/CD pipelines, ensuring code quality and preventing regressions.

