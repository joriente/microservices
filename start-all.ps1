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

# Step 1: Infrastructure is managed by .NET Aspire
Write-Host "Step 1: Infrastructure (MongoDB + RabbitMQ + PostgreSQL)..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "ℹ️  Infrastructure containers are managed by .NET Aspire" -ForegroundColor Cyan
Write-Host "   Aspire will automatically start:" -ForegroundColor Gray
Write-Host "   - MongoDB (with separate databases per service)" -ForegroundColor Gray
Write-Host "   - RabbitMQ (with management UI)" -ForegroundColor Gray
Write-Host "   - PostgreSQL (for InventoryService)" -ForegroundColor Gray
Write-Host ""

# Note: docker-compose.yml is disabled to avoid duplicate containers
# All infrastructure is now managed by Aspire for better integration

if ($InfrastructureOnly) {
    Write-Host ""
    Write-Host "Infrastructure-only mode not supported with Aspire." -ForegroundColor Yellow
    Write-Host "Please run without -InfrastructureOnly to start all services." -ForegroundColor Yellow
    exit 1
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
        Write-Host "✓ Infrastructure:" -ForegroundColor Green
        Write-Host "  📊 Aspire Dashboard: Check console output for URL (typically http://localhost:15XXX)" -ForegroundColor Cyan
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
        
        # Start Aspire AppHost first
        Write-Host "Starting Aspire AppHost..." -ForegroundColor Yellow
        Write-Host "NOTE: Aspire Dashboard URL will be displayed in the console below" -ForegroundColor Yellow
        Push-Location "src\Aspire\ProductOrderingSystem.AppHost"
        
        # Start Aspire in background
        $aspireJob = Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            dotnet run
        } -ArgumentList (Get-Location).Path
        
        # Wait for Aspire to start and create RabbitMQ container
        Write-Host "Waiting for Aspire infrastructure to be ready..." -ForegroundColor Yellow
        $maxWaitSeconds = 90
        $waited = 0
        $rabbitmqPort = $null
        
        while ($waited -lt $maxWaitSeconds) {
            Start-Sleep -Seconds 3
            $waited += 3
            
            # Check if RabbitMQ container exists and is running
            $container = docker ps --filter "name=ProductOrdering-rabbitmq" --format "{{.Names}}" 2>$null
            if ($container) {
                # Get the mapped port (format: 0.0.0.0:PORT)
                $portMapping = docker port ProductOrdering-rabbitmq 5672 2>$null
                if ($portMapping) {
                    # Extract just the port number
                    if ($portMapping -match ':(\d+)$') {
                        $rabbitmqPort = $matches[1]
                        Write-Host "✓ RabbitMQ container ready on host port $rabbitmqPort" -ForegroundColor Green
                        break
                    }
                }
            }
            
            if ($waited % 9 -eq 0) {
                Write-Host "  Still waiting for RabbitMQ container... ($waited/$maxWaitSeconds seconds)" -ForegroundColor Gray
            }
        }
        
        Pop-Location
        
        if (-not $rabbitmqPort) {
            Write-Host "⚠ Could not detect RabbitMQ dynamic port after $maxWaitSeconds seconds" -ForegroundColor Yellow
            Write-Host "⚠ NotificationService may fail to connect. Check Aspire Dashboard for RabbitMQ status" -ForegroundColor Yellow
            $rabbitmqPort = "5672"
        }
        
        # Wait a bit more for RabbitMQ to be fully ready
        Write-Host "Waiting for RabbitMQ to accept connections..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
        
        # Start NotificationService (Java) with RabbitMQ port
        Write-Host "Starting NotificationService (Java)..." -ForegroundColor Yellow
        $notificationServicePath = Resolve-Path "src\Services\NotificationService"
        $notificationServiceJar = "$notificationServicePath\target\notification-service-1.0.0.jar"
        
        if (Test-Path $notificationServiceJar) {
            # Start Java service with environment variables passed directly
            $javaCommand = "cd '$notificationServicePath'; `$env:RABBITMQ_HOST='localhost'; `$env:RABBITMQ_PORT='$rabbitmqPort'; java -jar target\notification-service-1.0.0.jar"
            
            Start-Process powershell -ArgumentList "-NoExit", "-Command", $javaCommand -WindowStyle Normal
            
            Write-Host "✓ NotificationService started in new window" -ForegroundColor Green
            Write-Host "  📧 NotificationService: RabbitMQ configured for localhost:$rabbitmqPort" -ForegroundColor Cyan
        } else {
            Write-Host "⚠ NotificationService JAR not found at:" -ForegroundColor Yellow
            Write-Host "  $notificationServiceJar" -ForegroundColor Gray
            Write-Host "  Run './Start-all.ps1' without -SkipBuild to build it" -ForegroundColor Gray
        }
        
        Write-Host ""
        Write-Host "Aspire Dashboard is running. Press Ctrl+C in this window to stop all services." -ForegroundColor Yellow
        Write-Host ""
        
        try {
            # Wait for the Aspire job to complete (it won't unless stopped)
            Wait-Job -Job $aspireJob
        } finally {
            # Cleanup
            Stop-Job -Job $aspireJob -ErrorAction SilentlyContinue
            Remove-Job -Job $aspireJob -ErrorAction SilentlyContinue
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
        Write-Host "cd src\frontend" -ForegroundColor White
        Write-Host "dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "Note: Make sure infrastructure (MongoDB, RabbitMQ, PostgreSQL) is running via docker-compose" -ForegroundColor Yellow
        Write-Host ""
    }
}
