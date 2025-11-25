# Build All Services Script
# This script builds both .NET and Java microservices

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Product Ordering System" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Build .NET Projects
Write-Host "Building .NET Projects..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Push-Location $PSScriptRoot
try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ .NET build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ .NET projects built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}
Write-Host ""

# Build Java NotificationService
Write-Host "Building Java NotificationService..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Push-Location "$PSScriptRoot\src\Services\NotificationService"
try {
    mvn clean package -DskipTests
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Java build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Java NotificationService built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}
Write-Host ""

# Build Docker images (optional)
Write-Host "Building Docker images..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

# Build NotificationService Docker image
Push-Location "$PSScriptRoot\src\Services\NotificationService"
try {
    docker build -t notification-service:latest .
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ Docker image build failed (Docker may not be running)" -ForegroundColor Yellow
    } else {
        Write-Host "✓ notification-service Docker image built" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ Docker build skipped (Docker may not be available)" -ForegroundColor Yellow
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Start infrastructure: cd infrastructure && docker-compose up -d" -ForegroundColor White
Write-Host "2. Run Aspire host: cd src\Aspire\ProductOrderingSystem.AppHost && dotnet run" -ForegroundColor White
Write-Host ""
