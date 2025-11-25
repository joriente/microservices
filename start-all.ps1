# Start All Services Script
# This script starts the infrastructure and all microservices

param(
    [switch]$SkipBuild,
    [switch]$InfrastructureOnly
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Product Ordering System - Startup" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start Infrastructure
Write-Host "Step 1: Starting Infrastructure (MongoDB + RabbitMQ + Seq)..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

if (Test-Path "deployment\docker\docker-compose.yml") {
    Push-Location "deployment\docker"
    try {
        docker-compose -p productordering up -d
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Infrastructure started successfully" -ForegroundColor Green
            Write-Host "  - MongoDB: mongodb://localhost:27017 (admin/admin123)" -ForegroundColor Gray
            Write-Host "  - RabbitMQ: amqp://localhost:5672 (guest/guest)" -ForegroundColor Gray
            Write-Host "  - RabbitMQ UI: http://localhost:15672 (guest/guest)" -ForegroundColor Gray
            Write-Host "  - Seq Logs: http://localhost:5341" -ForegroundColor Gray
        } else {
            Write-Host "✗ Failed to start infrastructure" -ForegroundColor Red
            exit 1
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "⚠ docker-compose.yml not found. Please ensure Docker is running." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

if ($InfrastructureOnly) {
    Write-Host ""
    Write-Host "Infrastructure started. Exiting as requested." -ForegroundColor Green
    exit 0
}

# Step 2: Build if not skipped
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Step 2: Building All Services..." -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    # Build .NET services
    Write-Host "Building .NET services..." -ForegroundColor Gray
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ .NET build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ .NET services built" -ForegroundColor Green
    
    # Build Java service
    Write-Host "Building Java NotificationService..." -ForegroundColor Gray
    Push-Location "src\Services\NotificationService"
    try {
        # Refresh PATH to ensure Maven is available
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        
        mvn clean package -DskipTests
        if ($LASTEXITCODE -ne 0) {
            Write-Host "✗ Java build failed" -ForegroundColor Red
            exit 1
        }
        Write-Host "✓ Java service built" -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host ""
    Write-Host "Step 2: Skipping build (using existing binaries)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting Microservices..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Choose how to run the services:" -ForegroundColor Yellow
Write-Host "1. Using .NET Aspire (Recommended) - All services in one dashboard" -ForegroundColor White
Write-Host "2. Using Docker Compose - Containerized services" -ForegroundColor White
Write-Host "3. Manual - Run each service individually" -ForegroundColor White
Write-Host ""
$choice = Read-Host "Enter your choice (1-3)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Starting services with .NET Aspire..." -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host ""
        Write-Host "✓ Infrastructure Started:" -ForegroundColor Green
        Write-Host "  📊 Aspire Dashboard: http://localhost:15888 (opens automatically)" -ForegroundColor Cyan
        Write-Host "  📋 Seq Logs: http://localhost:5341" -ForegroundColor Cyan
        Write-Host "  🐰 RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
        Write-Host "  🍃 Mongo Express: http://localhost:8081 (admin/admin123)" -ForegroundColor Cyan
        Write-Host "  🐘 pgAdmin: Via Aspire dashboard → postgres resource" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "✓ Microservices:" -ForegroundColor Green
        Write-Host "  🌐 Web App (Blazor): http://localhost:5261" -ForegroundColor White
        Write-Host "  🚪 API Gateway: http://localhost:5000" -ForegroundColor Gray
        Write-Host "  🔐 Identity Service: http://localhost:5001" -ForegroundColor Gray
        Write-Host "  📦 Product Service: http://localhost:5002" -ForegroundColor Gray
        Write-Host "  🛒 Cart Service: http://localhost:5003" -ForegroundColor Gray
        Write-Host "  📋 Order Service: http://localhost:5004" -ForegroundColor Gray
        Write-Host "  💳 Payment Service: http://localhost:5005" -ForegroundColor Gray
        Write-Host "  👥 Customer Service: http://localhost:5006" -ForegroundColor Gray
        Write-Host "  📊 Inventory Service: http://localhost:5007" -ForegroundColor Gray
        Write-Host ""
        Write-Host "✓ Databases:" -ForegroundColor Green
        Write-Host "  🐘 PostgreSQL: localhost:5432 (InventoryService)" -ForegroundColor Gray
        Write-Host "  🍃 MongoDB: localhost:27017 (Cart, Payment, Identity)" -ForegroundColor Gray
        Write-Host "  🗄️  SQL Server: Via Aspire (Product, Order, Customer, Identity)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "📚 API Documentation (Scalar):" -ForegroundColor Yellow
        Write-Host "  Each service has docs at {service-url}/scalar/v1" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Yellow
        Write-Host ""
        
        # Start NotificationService (Java) in background
        Write-Host "Starting NotificationService (Java)..." -ForegroundColor Yellow
        $notificationServicePath = "src\Services\NotificationService"
        $notificationServiceJar = "$notificationServicePath\target\notification-service-1.0.0.jar"
        
        if (Test-Path $notificationServiceJar) {
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$notificationServicePath'; java -jar target\notification-service-1.0.0.jar" -WindowStyle Normal
            
            Write-Host "✓ NotificationService started in new window" -ForegroundColor Green
            Write-Host "  📧 NotificationService: Listening on RabbitMQ" -ForegroundColor Gray
        } else {
            Write-Host "⚠ NotificationService JAR not found. Skipping..." -ForegroundColor Yellow
            Write-Host "  Run './Start-all.ps1' without -SkipBuild to build it" -ForegroundColor Gray
        }
        
        Write-Host ""
        
        Push-Location "src\Aspire\ProductOrderingSystem.AppHost"
        try {
            dotnet run
        } finally {
            Pop-Location
            # Stop NotificationService job when Aspire stops
            if ($notificationJob) {
                Write-Host ""
                Write-Host "Stopping NotificationService..." -ForegroundColor Yellow
                Stop-Job -Id $notificationJob.Id
                Remove-Job -Id $notificationJob.Id
                Write-Host "✓ NotificationService stopped" -ForegroundColor Green
            }
        }
    }
    "2" {
        Write-Host ""
        Write-Host "Docker Compose mode not yet implemented." -ForegroundColor Yellow
        Write-Host "Please use option 1 (Aspire) or option 3 (Manual)" -ForegroundColor Yellow
    }
    "3" {
        Write-Host ""
        Write-Host "Manual Mode - Starting Individual Services" -ForegroundColor Yellow
        Write-Host "==========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Open separate terminal windows and run these commands:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "# Terminal 1 - API Gateway (Port 5000)" -ForegroundColor Green
        Write-Host "cd src\Gateway\ProductOrderingSystem.ApiGateway" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 2 - IdentityService (Port 5001)" -ForegroundColor Green
        Write-Host "cd src\Services\IdentityService\ProductOrderingSystem.IdentityService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 3 - ProductService (Port 5002)" -ForegroundColor Green
        Write-Host "cd src\Services\ProductService\ProductOrderingSystem.ProductService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 4 - CartService (Port 5003)" -ForegroundColor Green
        Write-Host "cd src\Services\CartService\ProductOrderingSystem.CartService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 5 - OrderService (Port 5004)" -ForegroundColor Green
        Write-Host "cd src\Services\OrderService\ProductOrderingSystem.OrderService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 6 - PaymentService (Port 5005)" -ForegroundColor Green
        Write-Host "cd src\Services\PaymentService\ProductOrderingSystem.PaymentService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 7 - CustomerService (Port 5006)" -ForegroundColor Green
        Write-Host "cd src\Services\CustomerService\ProductOrderingSystem.CustomerService.WebAPI" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 8 - InventoryService (Port 5007)" -ForegroundColor Green
        Write-Host "cd src\Services\InventoryService" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "# Terminal 9 - Frontend (Port 5261)" -ForegroundColor Green
        Write-Host "cd frontend" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "Note: Make sure infrastructure (MongoDB, RabbitMQ, Seq, PostgreSQL) is running via docker-compose" -ForegroundColor Yellow
        Write-Host ""
    }
}
