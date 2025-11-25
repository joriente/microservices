# Aspire Troubleshooting Guide

## Issue: Services Starting Then Immediately Stopping

### Symptoms
- Aspire Dashboard starts successfully (https://localhost:17022)
- Services appear to start but then shut down immediately
- API Gateway not accessible on port 5555
- curl requests fail with "Could not connect to server"

### Root Cause Analysis

**Docker Container Check:**
```bash
docker ps
```
- ✅ Docker is running
- ❌ **No MongoDB containers found**
- ❌ **No RabbitMQ containers found**

This indicates that Aspire is not successfully starting the container resources that the services depend on.

### Expected Aspire Resources

When Aspire starts successfully, it should create:

1. **MongoDB Container** (for all services)
   - productdb (Product Service database)
   - orderdb (Order Service database)
   - identitydb (Identity Service database)
   - MongoDB Express (web UI on port 8081)

2. **RabbitMQ Container** (message broker)
   - RabbitMQ Management UI (port 15672)
   - AMQP port (5672)

3. **Service Projects**
   - identity-service
   - product-service
   - order-service
   - api-gateway

### Debugging Steps

#### 1. Check Aspire Dashboard
Open https://localhost:17022 and check:
- **Resources Tab**: Shows all resources and their status
- **Console Logs**: Click on each resource to see startup logs
- **Look for MongoDB/RabbitMQ**: Should show as "Running" or see error messages

#### 2. Check for Container Start Errors
Look for these common issues in the dashboard logs:
- Port conflicts (5432, 27017, 5672, 15672, 8081)
- Docker daemon connection issues
- Image pull failures
- Resource allocation problems (memory/CPU)

#### 3. Verify Docker Desktop
- Ensure Docker Desktop is running
- Check Settings → Resources (sufficient memory allocated)
- Check for any Docker errors in system tray

#### 4. Check Aspire AppHost Configuration
File: `src/Aspire/ProductOrderingSystem.AppHost/Program.cs`

Current configuration:
```csharp
// MongoDB with MongoExpress
var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress();  // Adds web UI

// RabbitMQ with Management Plugin
var rabbitMq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Databases
var productDb = mongodb.AddDatabase("productdb");
var orderDb = mongodb.AddDatabase("orderdb");
var identityDb = mongodb.AddDatabase("identitydb");
```

### Potential Solutions

#### Solution 1: Check Port Conflicts
```powershell
# Check if ports are already in use
netstat -ano | findstr "27017"  # MongoDB
netstat -ano | findstr "5672"   # RabbitMQ
netstat -ano | findstr "15672"  # RabbitMQ Management
netstat -ano | findstr "8081"   # Mongo Express
```

#### Solution 2: Manually Start Containers First
```powershell
# Start MongoDB
docker run -d --name mongodb -p 27017:27017 mongo:latest

# Start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Then start Aspire
dotnet run --project src/Aspire/ProductOrderingSystem.AppHost/ProductOrderingSystem.AppHost.csproj
```

#### Solution 3: Add Container Ready Checks
Modify service startup to wait for dependencies:

```csharp
// In Program.cs of each service
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString, name: "mongodb-check")
    .AddRabbitMQ(rabbitMqConnectionString, name: "rabbitmq-check");
```

#### Solution 4: Use Aspire Wait Strategies
```csharp
// In AppHost Program.cs
var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress()
    .WaitFor(); // Wait for MongoDB to be ready

var rabbitMq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WaitFor(); // Wait for RabbitMQ to be ready
```

#### Solution 5: Check Aspire Container Registry
Aspire uses a container registry. Verify images are available:
```powershell
docker images | findstr "mongo"
docker images | findstr "rabbit"
```

### Next Steps

1. **Open Aspire Dashboard** at https://localhost:17022
2. **Check Resources Tab** - Look for MongoDB and RabbitMQ status
3. **Review Console Logs** - Click on each resource to see error messages
4. **Check Docker** - Run `docker ps -a` to see all containers (including stopped ones)
5. **Look for Error Patterns**:
   - "Cannot connect to Docker daemon"
   - "Port already in use"
   - "Image not found"
   - "Container exited with code X"

### Successful Startup Indicators

When everything works correctly, you should see:

1. **Docker containers running**:
   ```
   docker ps
   # Should show: mongodb, rabbitmq, mongo-express
   ```

2. **Aspire Dashboard showing all resources as "Running"**

3. **Services accessible**:
   - API Gateway: http://localhost:5555
   - Identity Service: http://localhost:XXXX
   - Product Service: http://localhost:XXXX
   - Order Service: http://localhost:XXXX
   - MongoDB Express: http://localhost:8081
   - RabbitMQ Management: http://localhost:15672

4. **Test endpoint responds**:
   ```powershell
   curl http://localhost:5555/api/auth/register -Method POST -ContentType "application/json" -Body '{"email":"test@example.com","password":"Test123!@#"}'
   ```

### Configuration Files to Check

1. **AppHost**: `src/Aspire/ProductOrderingSystem.AppHost/Program.cs`
2. **Launch Settings**: `src/Aspire/ProductOrderingSystem.AppHost/Properties/launchSettings.json`
3. **Service Settings**: Each service's `appsettings.json` and `appsettings.Development.json`

### Logs Location

Aspire logs are typically in:
- Console output (terminal)
- Aspire Dashboard (Resources → Console Logs)
- Service logs (in dashboard, click on each service)

## Current Status

- ✅ All code builds successfully
- ✅ All unit and integration tests passing
- ✅ JWT authentication configured
- ✅ Aspire AppHost configured
- ❌ **Container resources not starting**
- ❌ **Services shutting down after startup**

## Action Required

**Investigate Aspire Dashboard** to determine why MongoDB and RabbitMQ containers are not starting or staying running.
