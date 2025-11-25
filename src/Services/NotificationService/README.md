# NotificationService (Java/Spring Boot)

A microservice for handling email notifications in the Product Ordering System.

## Technology Stack

- **Language**: Java 21
- **Framework**: Spring Boot 3.2.0
- **Messaging**: RabbitMQ (Spring AMQP)
- **Database**: MongoDB
- **Email**: SendGrid
- **Build Tool**: Maven

## Features

- ✅ Consumes events from RabbitMQ (OrderCreated, PaymentProcessed, PaymentFailed)
- ✅ Sends email notifications using SendGrid
- ✅ Stores notification history in MongoDB
- ✅ REST API for querying notification history
- ✅ Health checks for Aspire integration
- ✅ Prometheus metrics endpoint

## Event Consumers

### OrderCreatedEvent
- Queue: `notification-service-order-created`
- Exchange: `ProductOrderingSystem.Shared.Contracts.Events:OrderCreatedEvent`
- Action: Sends order confirmation email

### PaymentProcessedEvent
- Queue: `notification-service-payment-processed`
- Exchange: `ProductOrderingSystem.Shared.Contracts.Events:PaymentProcessedEvent`
- Action: Sends payment success email

### PaymentFailedEvent
- Queue: `notification-service-payment-failed`
- Exchange: `ProductOrderingSystem.Shared.Contracts.Events:PaymentFailedEvent`
- Action: Sends payment failure email

## API Endpoints

- `GET /api/notifications` - Get all notifications
- `GET /api/notifications/user/{userId}` - Get notifications for a user
- `GET /api/notifications/order/{orderId}` - Get notifications for an order
- `GET /actuator/health` - Health check endpoint
- `GET /actuator/metrics` - Metrics endpoint
- `GET /actuator/prometheus` - Prometheus metrics

## Configuration

Environment variables:
- `MONGODB_URI` - MongoDB connection string (default: mongodb://localhost:27017/notificationdb)
- `RABBITMQ_HOST` - RabbitMQ host (default: localhost)
- `RABBITMQ_PORT` - RabbitMQ port (default: 5672)
- `RABBITMQ_USERNAME` - RabbitMQ username (default: guest)
- `RABBITMQ_PASSWORD` - RabbitMQ password (default: guest)
- `SENDGRID_API_KEY` - SendGrid API key
- `SENDGRID_FROM_EMAIL` - Sender email address
- `SENDGRID_FROM_NAME` - Sender name
- `SENDGRID_ENABLED` - Enable/disable SendGrid (default: false)
- `PORT` - HTTP port (default: 8085)

## Building

```bash
./mvnw clean package
```

## Running

```bash
java -jar target/notification-service-1.0.0.jar
```

Or with Docker:

```bash
docker build -t notification-service:latest .
docker run -p 8085:8085 notification-service:latest
```

## Testing

```bash
./mvnw test
```

## Integration with .NET Services

This Java service integrates seamlessly with the .NET microservices through:
1. **RabbitMQ** - Consumes JSON events published by .NET services
2. **MongoDB** - Shares the same database infrastructure
3. **Aspire** - Managed by Aspire AppHost for orchestration

The event contracts match exactly with the C# definitions in `ProductOrderingSystem.Shared.Contracts`.
