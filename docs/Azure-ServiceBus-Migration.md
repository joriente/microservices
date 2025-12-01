# Migration from RabbitMQ to Azure Service Bus Emulator

## Overview
This document describes the migration from RabbitMQ to Azure Service Bus emulator for the ProductOrderingSystem microservices architecture.

## Migration Date
December 1, 2025

## Changes Made

### 1. Package Dependencies

#### Directory.Packages.props
- **Removed:**
  - `Aspire.Hosting.RabbitMQ` (9.5.1)
  - `Aspire.RabbitMQ.Client` (9.5.1)
  - `MassTransit.RabbitMQ` (8.5.4)
  - `Testcontainers.RabbitMq` (4.1.0)

- **Added:**
  - `Aspire.Hosting.Azure.ServiceBus` (9.5.1)
  - `Aspire.Azure.Messaging.ServiceBus` (9.5.1)
  - `MassTransit.Azure.ServiceBus.Core` (8.5.4)
  - `Testcontainers.AzureServiceBus` (4.1.0)

### 2. Aspire AppHost Configuration

**File:** `src/Aspire/ProductOrderingSystem.AppHost/Program.cs`

- Replaced RabbitMQ container with Azure Service Bus emulator:
  ```csharp
  // OLD:
  var rabbitMq = builder.AddRabbitMQ("messaging")
      .WithContainerName("ProductOrdering-rabbitmq")
      .WithManagementPlugin()
      .PublishAsConnectionString();
  
  // NEW:
  var serviceBus = builder.AddAzureServiceBus("messaging")
      .RunAsEmulator()
      .WithLifetime(ContainerLifetime.Persistent);
  ```

- Updated all service references from `rabbitMq` to `serviceBus`

### 3. Service Project Files

Updated all service `.csproj` files to replace `MassTransit.RabbitMQ` with `MassTransit.Azure.ServiceBus.Core`:

- CartService.WebAPI
- ProductService.WebAPI
- OrderService.WebAPI
- PaymentService.WebAPI
- PaymentService.Infrastructure
- IdentityService.WebAPI
- InventoryService
- CustomerService.WebAPI
- CustomerService.Infrastructure

### 4. Service Implementations

Updated all service `Program.cs` files to use Azure Service Bus instead of RabbitMQ:

#### CartService
```csharp
// OLD: x.UsingRabbitMq((context, cfg) => { ... })
// NEW:
x.UsingAzureServiceBus((context, cfg) =>
{
    var connectionString = builder.Configuration.GetConnectionString("messaging");
    cfg.Host(connectionString);
    cfg.ConfigureEndpoints(context);
});
```

#### Similar changes made to:
- InventoryService
- OrderService
- ProductService
- PaymentService
- CustomerService

### 5. Docker Compose

**File:** `deployment/docker/docker-compose.yml`

- Removed RabbitMQ service and volume:
  - Removed `rabbitmq` service definition
  - Removed `rabbitmq_data` volume

### 6. Test Scripts

**Removed RabbitMQ-specific test scripts:**
- `tests/Check-RabbitMQ.ps1`
- `tests/Debug-RabbitMQ-Bindings.ps1`

### 7. Test Projects

Updated integration test projects to use `Testcontainers.AzureServiceBus`:
- PaymentService.IntegrationTests
- CartService.IntegrationTests
- InventoryService.IntegrationTests
- CustomerService.IntegrationTests

## Key Differences: RabbitMQ vs Azure Service Bus

### RabbitMQ Configuration
- Required explicit exchange and queue bindings
- Used `cfg.ReceiveEndpoint()` with manual binding configuration
- Required workarounds for Aspire proxy issues
- Direct connection to localhost:5672

### Azure Service Bus Configuration
- Simplified configuration using `cfg.ConfigureEndpoints(context)`
- Automatic topic/subscription creation based on consumer configuration
- Native Aspire support with emulator
- Connection string-based configuration

## Benefits of Azure Service Bus

1. **Native Aspire Support**: Azure Service Bus emulator has first-class support in .NET Aspire
2. **Simplified Configuration**: Automatic endpoint configuration reduces boilerplate
3. **Enterprise Features**: Supports dead-letter queues, scheduled messages, and sessions
4. **Cloud-Ready**: Easy transition to Azure Service Bus in production
5. **Better Developer Experience**: No manual exchange/queue binding required

## Running the Application

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop
- .NET Aspire Workload

### Starting the Application
```bash
# Run the Aspire AppHost
dotnet run --project src/Aspire/ProductOrderingSystem.AppHost
```

The Azure Service Bus emulator will automatically start as a container via Aspire.

## Connection String Format

Azure Service Bus connection string is automatically provided by Aspire:
```
Endpoint=sb://<emulator-endpoint>;SharedAccessKeyName=...;SharedAccessKey=...
```

Services receive this via the `messaging` connection string in configuration.

## MassTransit Endpoint Naming

With Azure Service Bus, MassTransit automatically creates topics and subscriptions based on:
- **Topic**: Message type name (e.g., `ProductCreatedEvent`)
- **Subscription**: Consumer type name (e.g., `ProductCreatedEventConsumer`)

No manual configuration required for most scenarios.

## Troubleshooting

### Emulator Not Starting
If the Azure Service Bus emulator doesn't start:
1. Ensure Docker Desktop is running
2. Check Aspire dashboard logs at `http://localhost:15000`
3. Verify no port conflicts

### Connection Issues
If services can't connect:
1. Check the `messaging` connection string in service logs
2. Verify the emulator container is healthy in Aspire dashboard
3. Ensure all services have `.WithReference(serviceBus)` in AppHost

### Message Not Being Consumed
If messages aren't being consumed:
1. Verify consumer registration in `AddConsumer<T>()`
2. Check `ConfigureEndpoints(context)` is called
3. Review service logs for connection errors

## Migration Checklist

- [x] Update Directory.Packages.props
- [x] Update Aspire AppHost configuration
- [x] Update all service project files
- [x] Update all service implementations
- [x] Remove RabbitMQ from docker-compose.yml
- [x] Remove RabbitMQ test scripts
- [x] Update integration test projects
- [x] Document migration changes

## Next Steps

1. Test end-to-end messaging flows
2. Update integration tests to use Azure Service Bus testcontainers
3. Update documentation and diagrams to reflect Azure Service Bus
4. Consider Azure Service Bus production deployment strategy

## References

- [.NET Aspire Azure Service Bus integration](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/azure-service-bus-integration)
- [MassTransit Azure Service Bus](https://masstransit.io/documentation/transports/azure-service-bus)
- [Azure Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/overview-emulator)
