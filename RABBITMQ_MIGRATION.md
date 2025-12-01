# RabbitMQ Migration Summary

## Overview
Successfully migrated from Azure Service Bus to RabbitMQ for better ARM64/Raspberry Pi Kubernetes compatibility.

## Changes Made

### 1. Aspire AppHost
**File**: `src/Aspire/ProductOrderingSystem.AppHost/Program.cs`
- Replaced Azure Service Bus emulator with RabbitMQ container
- Added RabbitMQ Management Plugin (UI at http://localhost:15672)
- Updated all service references from `serviceBus` to `messaging`

**File**: `src/Aspire/ProductOrderingSystem.AppHost/ProductOrderingSystem.AppHost.csproj`
- Removed: `Aspire.Hosting.Azure.ServiceBus`
- Added: `Aspire.Hosting.RabbitMQ`

### 2. Central Package Management
**File**: `Directory.Packages.props`
- Removed: `Aspire.Hosting.Azure.ServiceBus` (13.0.1)
- Removed: `Azure.Messaging.ServiceBus` (7.18.0)
- Removed: `MassTransit.Azure.ServiceBus.Core` (8.5.7)
- Added: `Aspire.Hosting.RabbitMQ` (13.0.1)
- Added: `MassTransit.RabbitMQ` (8.5.7)

### 3. DataSeeder
**File**: `src/Tools/ProductOrderingSystem.DataSeeder/ProductOrderingSystem.DataSeeder.csproj`
- Replaced `MassTransit.Azure.ServiceBus.Core` with `MassTransit.RabbitMQ`

**File**: `src/Tools/ProductOrderingSystem.DataSeeder/Program.cs`
- Changed from `UsingAzureServiceBus` to `UsingRabbitMq`
- Updated connection to use AMQP protocol (amqp://localhost:5672)
- Default credentials: guest/guest

### 4. All Microservices Updated
The following services were updated to use RabbitMQ:

- **ProductService** (`ProductOrderingSystem.ProductService.WebAPI`)
- **OrderService** (`ProductOrderingSystem.OrderService.WebAPI`)
- **CartService** (`ProductOrderingSystem.CartService.WebAPI`)
- **PaymentService** (`ProductOrderingSystem.PaymentService.WebAPI`)
  - Also updated Infrastructure project
- **CustomerService** (`ProductOrderingSystem.CustomerService.WebAPI`)
  - Also updated Infrastructure project
- **InventoryService** (`ProductOrderingSystem.InventoryService`)
- **IdentityService** (`ProductOrderingSystem.IdentityService.WebAPI`)

### MassTransit Configuration Pattern
All services now use this pattern:

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<YourConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        var uri = new Uri(connectionString ?? "amqp://localhost:5672");
        
        cfg.Host(uri, h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

## RabbitMQ Management UI

Once Aspire is running, you can access the RabbitMQ Management UI at:

- **URL**: http://localhost:15672
- **Username**: guest
- **Password**: guest

### What You Can Do:
- View all queues, exchanges, and bindings
- Monitor message rates and throughput
- Browse messages in queues
- See consumer connections
- Debug message routing
- View topology diagrams

## Benefits for Raspberry Pi Kubernetes

1. **ARM64 Support**: RabbitMQ has official ARM64 Docker images
2. **Lower Resource Usage**: More efficient than Azure Service Bus emulator
3. **No Emulator Limitations**: Production-ready messaging broker
4. **Better Monitoring**: Built-in management UI
5. **Kubernetes-Native**: Well-established Kubernetes operators and Helm charts

## Connection String Format

Aspire will provide the connection string via the `messaging` connection name:
- Default: `amqp://localhost:5672`
- With credentials: `amqp://guest:guest@localhost:5672`

## Testing the Migration

1. **Start Aspire**:
   ```powershell
   dotnet run --project src/Aspire/ProductOrderingSystem.AppHost
   ```

2. **Verify RabbitMQ is Running**:
   - Check Aspire Dashboard for `messaging` resource
   - Open http://localhost:15672
   - Login with guest/guest

3. **Verify Message Publishing**:
   - Enable event publishing in DataSeeder: `Seeding:PublishEvents: true`
   - Watch RabbitMQ Management UI -> Queues
   - Should see exchanges and queues created by MassTransit

4. **Test Service Communication**:
   - Create an order
   - Check RabbitMQ UI for messages flowing between services
   - Verify inventory reservations, payment processing, etc.

## Rollback Instructions

If you need to roll back to Azure Service Bus:

1. Revert changes in `Directory.Packages.props`
2. Revert changes in AppHost `Program.cs` and `.csproj`
3. Run the migration script in reverse (or manually update service Program.cs files)
4. Run: `dotnet restore && dotnet build`

## Next Steps for Kubernetes Deployment

1. **Create Kubernetes Manifests**:
   - RabbitMQ StatefulSet with persistent volume
   - MongoDB StatefulSet
   - PostgreSQL StatefulSet
   - Service deployments

2. **Use Helm Charts**:
   ```bash
   helm repo add bitnami https://charts.bitnami.com/bitnami
   helm install rabbitmq bitnami/rabbitmq --set auth.username=guest,auth.password=guest
   helm install mongodb bitnami/mongodb
   helm install postgresql bitnami/postgresql
   ```

3. **Build ARM64 Images**:
   ```bash
   docker buildx build --platform linux/arm64 -t your-service:arm64 .
   ```

4. **Configure Resource Limits** (important for Raspberry Pi):
   ```yaml
   resources:
     limits:
       memory: "512Mi"
       cpu: "500m"
     requests:
       memory: "256Mi"
       cpu: "250m"
   ```

## Migration Script

The automated migration was performed using:
- `switch-to-rabbitmq.ps1` - PowerShell script that updates all service configurations

## Build Status

✅ All projects build successfully
✅ 32 warnings (non-critical, mostly code analysis suggestions)
✅ 0 errors

## Author

Migration completed: December 1, 2025
