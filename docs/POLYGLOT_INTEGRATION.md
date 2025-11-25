# Polyglot Microservices Integration - Java NotificationService

## Overview

This document describes the integration of the **Java-based NotificationService** into the existing .NET-based Product Ordering System, demonstrating true polyglot microservices architecture.

## Architecture

### Service Comparison

| Aspect | .NET Services | Java NotificationService |
|--------|--------------|-------------------------|
| **Runtime** | .NET 9.0 | Java 21 |
| **Framework** | ASP.NET Core | Spring Boot 3.2.0 |
| **Database Client** | MongoDB.Driver | Spring Data MongoDB |
| **Messaging** | MassTransit | Spring AMQP |
| **DI Container** | Microsoft.Extensions.DI | Spring IoC |
| **Configuration** | appsettings.json | application.yml |
| **Build Tool** | dotnet CLI / MSBuild | Maven |
| **Testing** | xUnit + Testcontainers | JUnit + Testcontainers |

### Integration Points

#### 1. Message Bus (RabbitMQ)
Both .NET and Java services communicate through RabbitMQ:

**.NET (MassTransit):**
```csharp
// Publishing an event
await _publishEndpoint.Publish(new OrderCreatedEvent
{
    OrderId = order.Id,
    CustomerId = order.CustomerId,
    TotalAmount = order.TotalAmount,
    CreatedAt = DateTime.UtcNow
});
```

**Java (Spring AMQP):**
```java
// Consuming an event
@RabbitListener(queues = "${messaging.queues.order-created}")
public void handleOrderCreated(OrderCreatedEvent event) {
    notificationService.sendOrderConfirmationEmail(event);
}
```

#### 2. Event Contract Compatibility

Events are serialized to JSON with matching property names:

**C# Event (MassTransit):**
```csharp
public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Java Event (Jackson):**
```java
@Data
public class OrderCreatedEvent {
    @JsonProperty("orderId")
    private UUID orderId;
    
    @JsonProperty("customerId")
    private UUID customerId;
    
    @JsonProperty("totalAmount")
    private BigDecimal totalAmount;
    
    @JsonProperty("createdAt")
    private LocalDateTime createdAt;
}
```

#### 3. Exchange Naming Convention

MassTransit uses fully-qualified type names for exchange naming:

```
Exchange Name: ProductOrderingSystem.Shared.Contracts.Events:OrderCreatedEvent
Type: Fanout
```

Java Spring AMQP configuration matches this:

```java
@Bean
public Queue orderCreatedQueue() {
    return new Queue("notification-service-order-created");
}

@Bean
public FanoutExchange orderCreatedExchange() {
    return new FanoutExchange(
        "ProductOrderingSystem.Shared.Contracts.Events:OrderCreatedEvent"
    );
}

@Bean
public Binding orderCreatedBinding(
    Queue orderCreatedQueue, 
    FanoutExchange orderCreatedExchange
) {
    return BindingBuilder
        .bind(orderCreatedQueue)
        .to(orderCreatedExchange);
}
```

## Project Structure

### Java NotificationService

```
NotificationService/
├── src/
│   ├── main/
│   │   ├── java/com/productordering/notificationservice/
│   │   │   ├── NotificationServiceApplication.java
│   │   │   ├── domain/
│   │   │   │   ├── entities/
│   │   │   │   │   └── Notification.java
│   │   │   │   ├── enums/
│   │   │   │   │   ├── NotificationType.java
│   │   │   │   │   └── NotificationStatus.java
│   │   │   │   └── repositories/
│   │   │   │       └── NotificationRepository.java
│   │   │   ├── application/
│   │   │   │   ├── services/
│   │   │   │   │   ├── EmailService.java
│   │   │   │   │   └── NotificationService.java
│   │   │   │   └── messaging/
│   │   │   │       ├── events/
│   │   │   │       │   ├── OrderCreatedEvent.java
│   │   │   │       │   ├── PaymentProcessedEvent.java
│   │   │   │       │   └── PaymentFailedEvent.java
│   │   │   │       └── consumers/
│   │   │   │           ├── OrderCreatedConsumer.java
│   │   │   │           ├── PaymentProcessedConsumer.java
│   │   │   │           └── PaymentFailedConsumer.java
│   │   │   ├── infrastructure/
│   │   │   │   ├── messaging/
│   │   │   │   │   └── RabbitMqConfig.java
│   │   │   │   └── email/
│   │   │   │       └── SendGridEmailService.java
│   │   │   └── presentation/
│   │   │       └── controllers/
│   │   │           └── NotificationController.java
│   │   └── resources/
│   │       ├── application.yml
│   │       └── templates/
│   │           ├── order-confirmation.html
│   │           ├── payment-success.html
│   │           └── payment-failed.html
│   └── test/
│       └── java/...
├── pom.xml
├── Dockerfile
└── README.md
```

## Building

### Build All Services (Including Java)

**Windows (PowerShell):**
```powershell
.\build-all.ps1
```

**Manual Build:**
```bash
# Build .NET projects
dotnet build

# Build Java NotificationService
cd src/Services/NotificationService
mvn clean package
```

### Build Output

**.NET Services:**
- Output: `bin/Release/net9.0/*.dll`
- Command: `dotnet run` or `dotnet <service>.dll`

**Java Service:**
- Output: `target/notification-service-1.0.0.jar`
- Command: `java -jar notification-service-1.0.0.jar` or `mvn spring-boot:run`

## Running

### Option 1: .NET Aspire (Future Integration)

Once updated to include Java service:
```bash
cd src/Aspire/ProductOrderingSystem.AppHost
dotnet run
```

### Option 2: Individual Services

**.NET Services:** (as before)
```bash
dotnet run --project src/Services/OrderService/ProductOrderingSystem.OrderService.WebAPI
```

**Java NotificationService:**
```bash
cd src/Services/NotificationService
mvn spring-boot:run

# OR with JAR
java -jar target/notification-service-1.0.0.jar
```

## Configuration

### Environment Variables

Both .NET and Java services can be configured via environment variables for deployment:

**.NET (appsettings.json or env vars):**
```bash
ConnectionStrings__MongoDb=mongodb://localhost:27017/orderdb
ConnectionStrings__RabbitMq=amqp://guest:guest@localhost:5672
```

**Java (application.yml or env vars):**
```bash
MONGODB_URI=mongodb://localhost:27017/notificationdb
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
SENDGRID_API_KEY=your_api_key
SENDGRID_FROM_EMAIL=noreply@example.com
SENDGRID_ENABLED=true
```

### Configuration Files

**.NET:**
```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017/notificationdb",
    "RabbitMq": "amqp://guest:guest@localhost:5672"
  }
}
```

**Java:**
```yaml
spring:
  data:
    mongodb:
      uri: ${MONGODB_URI:mongodb://localhost:27017/notificationdb}
  rabbitmq:
    host: ${RABBITMQ_HOST:localhost}
    port: ${RABBITMQ_PORT:5672}
    username: ${RABBITMQ_USERNAME:guest}
    password: ${RABBITMQ_PASSWORD:guest}

sendgrid:
  api-key: ${SENDGRID_API_KEY}
  from-email: ${SENDGRID_FROM_EMAIL}
  from-name: ${SENDGRID_FROM_NAME:Product Ordering System}
  enabled: ${SENDGRID_ENABLED:false}
```

## Testing the Integration

### End-to-End Flow Test

1. **Start Infrastructure:**
```bash
cd infrastructure
docker-compose up -d
```

2. **Start All Services:**
```bash
# Terminal 1: OrderService
cd src/Services/OrderService/ProductOrderingSystem.OrderService.WebAPI
dotnet run

# Terminal 2: PaymentService
cd src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI
dotnet run

# Terminal 3: NotificationService (Java)
cd src/Services/NotificationService
mvn spring-boot:run
```

3. **Create an Order:**
```bash
curl -X POST http://localhost:5004/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "customerId": "customer-guid",
    "items": [
      {
        "productId": "product-guid",
        "quantity": 2,
        "priceAtOrder": 29.99
      }
    ]
  }'
```

4. **Expected Flow:**
   - OrderService creates order
   - Publishes `OrderCreatedEvent` to RabbitMQ
   - Java NotificationService receives event
   - Sends order confirmation email via SendGrid
   - Logs notification to MongoDB

5. **Verify Notification:**
```bash
curl http://localhost:8085/api/notifications/order/{orderId}
```

### Monitoring

**RabbitMQ Management UI:**
- URL: http://localhost:15672
- Credentials: guest/guest
- Check exchanges and queues
- Monitor message flow between .NET and Java services

**Spring Boot Actuator:**
- Health: http://localhost:8085/actuator/health
- Metrics: http://localhost:8085/actuator/metrics
- Prometheus: http://localhost:8085/actuator/prometheus

## Docker Support

### Dockerfile for Java Service

```dockerfile
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app
COPY target/notification-service-1.0.0.jar app.jar
EXPOSE 8085
ENTRYPOINT ["java", "-jar", "app.jar"]
```

### Build Docker Image

```bash
cd src/Services/NotificationService
mvn clean package
docker build -t notification-service:latest .
```

### Run with Docker

```bash
docker run -d \
  --name notification-service \
  -p 8085:8085 \
  -e MONGODB_URI=mongodb://host.docker.internal:27017/notificationdb \
  -e RABBITMQ_HOST=host.docker.internal \
  -e SENDGRID_API_KEY=your_key \
  -e SENDGRID_ENABLED=true \
  notification-service:latest
```

## Advantages of Polyglot Architecture

### 1. **Best Tool for the Job**
- Use Spring Boot's mature ecosystem for notifications
- Leverage .NET's performance for order processing
- Choose language based on team expertise

### 2. **Technology Flexibility**
- Easy to adopt new technologies
- Services can be rewritten independently
- No vendor lock-in

### 3. **Team Autonomy**
- Teams can choose their preferred stack
- Parallel development without conflicts
- Faster feature delivery

### 4. **Resilience**
- Services are truly independent
- Language-specific bugs don't cascade
- Runtime isolation

## Challenges & Solutions

### Challenge 1: Event Schema Compatibility
**Solution:** Strict JSON contracts with explicit property mapping (`@JsonProperty` annotations in Java match C# property names)

### Challenge 2: Different Build Systems
**Solution:** Unified `build-all.ps1` script orchestrates both Maven and dotnet CLI

### Challenge 3: Debugging Across Languages
**Solution:** 
- Correlation IDs in logs
- RabbitMQ Management UI for message tracking
- Aspire Dashboard for observability

### Challenge 4: Testing Integration
**Solution:** 
- Testcontainers for both .NET and Java
- Contract testing for event schemas
- End-to-end smoke tests

## Future Enhancements

### 1. Aspire Integration for Java Services
```csharp
var notificationService = builder.AddContainer(
    "notification-service",
    "notification-service",
    "latest")
    .WithHttpEndpoint(port: 8085)
    .WithReference(mongodb)
    .WithReference(rabbitmq);
```

### 2. OpenTelemetry Tracing
Unified distributed tracing across .NET and Java services

### 3. API Gateway Integration
Add Java service routes to YARP configuration

### 4. Kubernetes Deployment
Multi-language service mesh with Istio or Linkerd

## References

- [NotificationService README](src/Services/NotificationService/README.md)
- [Spring Boot Documentation](https://spring.io/projects/spring-boot)
- [Spring AMQP](https://spring.io/projects/spring-amqp)
- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/tutorials)

## Summary

The Java NotificationService demonstrates:
✅ True polyglot microservices architecture  
✅ Event-driven communication across platforms  
✅ JSON-based contract compatibility  
✅ Independent deployment and scaling  
✅ Best-of-breed technology choices  
✅ Resilient distributed systems  

This architecture proves that microservices enable technology diversity while maintaining system cohesion through well-defined contracts and messaging patterns.
