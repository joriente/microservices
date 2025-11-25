# Quick Start Guide - Product Ordering System

## 🚀 Fastest Way to Run Everything

### Step 1: Start Infrastructure
```powershell
cd infrastructure
docker-compose up -d
```

Wait 10 seconds for MongoDB and RabbitMQ to be ready.

### Step 2: Start All Services with Aspire
```powershell
# From the root directory
cd src\Aspire\ProductOrderingSystem.AppHost
dotnet run
```

The Aspire Dashboard will open automatically at: **https://localhost:17251**

### Step 3: View the Dashboard

You'll see all services running:
- ✅ IdentityService (Port 5001)
- ✅ ProductService (Port 5002)  
- ✅ CartService (Port 5003)
- ✅ OrderService (Port 5004)
- ✅ PaymentService (Port 5005)
- ✅ API Gateway (Port 5000)
- ✅ MongoDB
- ✅ RabbitMQ

**Note:** Java NotificationService runs separately (see below).

## 🔔 Running NotificationService (Java)

### In a new terminal:
```powershell
cd src\Services\NotificationService

# Make sure environment is set
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

# Run the service
mvn spring-boot:run
```

**Service will start on port 8085**

### Verify it's running:
```powershell
curl http://localhost:8085/actuator/health
```

Should return: `{"status":"UP"}`

## 🧪 Quick End-to-End Test

### Pre-seeded Test Users

The system automatically creates these users on startup:

- **Admin User**: 
  - Username: `admin`
  - Password: `P@ssw0rd`
  - Email: `admin@productordering.com`
  
- **Shopper User**: 
  - Username: `steve.hopper`
  - Password: `P@ssw0rd`
  - Email: `steve.hopper@email.com`

You can use these for testing or create your own user below.

### 1. Register a User (Optional - use pre-seeded users above)
```powershell
$registerResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/register" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"email":"demo@example.com","password":"Demo123!","fullName":"Demo User"}'

$userId = $registerResponse.id
Write-Host "User ID: $userId"
```

### 2. Login (using pre-seeded shopper)
```powershell
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"username":"steve.hopper","password":"P@ssw0rd"}'

$token = $loginResponse.token
Write-Host "Token: $token"
```

### 3. Create a Product
```powershell
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$productResponse = Invoke-RestMethod -Uri "http://localhost:5002/api/products" `
  -Method POST `
  -Headers $headers `
  -Body '{"name":"Test Widget","description":"A test product","price":29.99,"stockQuantity":100}'

$productId = $productResponse.id
Write-Host "Product ID: $productId"
```

### 4. Add to Cart
```powershell
Invoke-RestMethod -Uri "http://localhost:5003/api/cart/items" `
  -Method POST `
  -Headers $headers `
  -Body "{`"productId`":`"$productId`",`"quantity`":2}"
```

### 5. Create Order (Triggers Notification!)
```powershell
$orderBody = @{
    customerId = $userId
    items = @(
        @{
            productId = $productId
            quantity = 2
            priceAtOrder = 29.99
        }
    )
} | ConvertTo-Json

$orderResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/orders" `
  -Method POST `
  -Headers $headers `
  -Body $orderBody

$orderId = $orderResponse.id
Write-Host "Order ID: $orderId"
Write-Host "✅ Order created! Check NotificationService logs for email activity."
```

### 6. Check Notification Was Sent
```powershell
# Wait a moment for the notification to be processed
Start-Sleep -Seconds 2

# Check notifications
Invoke-RestMethod -Uri "http://localhost:8085/api/notifications" | ConvertTo-Json -Depth 3
```

## 🔍 Monitoring

### Aspire Dashboard
- **URL:** https://localhost:17251
- **Features:**
  - View all .NET services
  - Logs in real-time
  - Distributed tracing
  - Metrics and health

### RabbitMQ Management
- **URL:** http://localhost:15672
- **Credentials:** guest / guest
- **Check:**
  - Exchanges
  - Queues
  - Message flow

### MongoDB Compass
- **Connection:** mongodb://localhost:27017
- **Databases:**
  - identitydb
  - productdb
  - cartdb
  - orderdb
  - paymentdb
  - notificationdb

### NotificationService Health
```powershell
# Health check
curl http://localhost:8085/actuator/health

# Metrics
curl http://localhost:8085/actuator/metrics

# Notifications
curl http://localhost:8085/api/notifications
```

## 🛑 Stopping Everything

### Stop Aspire (Ctrl+C in the terminal)
This stops all .NET services.

### Stop NotificationService (Ctrl+C in its terminal)

### Stop Infrastructure
```powershell
cd infrastructure
docker-compose down
```

## 📝 Notes

- **SendGrid:** If `SENDGRID_ENABLED=false` in `.env`, emails are logged but not sent
- **Ports:** Make sure ports 5000-5005, 8085, 15672, 27017 are free
- **First Run:** Aspire may take a minute to start all services

## 🎯 What Happens When You Create an Order?

```
User → OrderService → MongoDB (saves order)
                   → RabbitMQ (publishes OrderCreatedEvent)
                   → NotificationService (Java) receives event
                   → SendGrid API (sends email)
                   → MongoDB (saves notification record)
```

You can watch this flow in real-time:
- Aspire Dashboard: See the order service logs
- NotificationService terminal: See event received + email sent
- RabbitMQ UI: See message published and consumed

## ✨ Success Indicators

When everything is working:
1. ✅ Aspire Dashboard shows all services as "Running"
2. ✅ NotificationService logs show "Application started"
3. ✅ Health checks return UP
4. ✅ Creating an order shows logs in NotificationService
5. ✅ `/api/notifications` returns notification records
6. ✅ SendGrid dashboard shows sent email (if enabled)

Enjoy your polyglot microservices system! 🎉
