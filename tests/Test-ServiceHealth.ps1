# Service Health Check Script
# Checks if all services are accessible

$apiGateway = "http://localhost:5555"

Write-Host "`n=== Service Health Check ===" -ForegroundColor Cyan

# Check API Gateway
Write-Host "`n1. API Gateway Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiGateway/api/products" -Method GET -ErrorAction Stop
    Write-Host "✓ API Gateway is responding" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)`n" -ForegroundColor Gray
}
catch {
    Write-Host "✗ API Gateway not accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)`n" -ForegroundColor Red
}

# Check Identity Service through Gateway
Write-Host "2. Identity Service (via Gateway)" -ForegroundColor Yellow
try {
    $testBody = @{ email = "test@test.com"; password = "Test123!@#" } | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "$apiGateway/api/auth/register" -Method POST -ContentType "application/json" -Body $testBody -ErrorAction Stop
    Write-Host "✓ Identity Service is responding" -ForegroundColor Green
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ Identity Service issue" -ForegroundColor Red
    Write-Host "Status Code: $statusCode" -ForegroundColor Gray
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($statusCode -eq 502) {
        Write-Host "`nDiagnosis: 502 Bad Gateway - API Gateway cannot reach Identity Service" -ForegroundColor Yellow
        Write-Host "Possible causes:" -ForegroundColor Yellow
        Write-Host "  - Identity Service not started" -ForegroundColor Gray
        Write-Host "  - Service discovery not configured" -ForegroundColor Gray
        Write-Host "  - Identity Service crashed on startup" -ForegroundColor Gray
        Write-Host "  - MongoDB not accessible to Identity Service`n" -ForegroundColor Gray
    }
}

# Check Product Service through Gateway
Write-Host "3. Product Service (via Gateway)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiGateway/api/products" -Method GET -ErrorAction Stop
    Write-Host "✓ Product Service is responding" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "Response: $($response.Content)`n" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Product Service not accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)`n" -ForegroundColor Red
}

# Check Order Service through Gateway
Write-Host "4. Order Service (via Gateway)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiGateway/api/orders" -Method GET -ErrorAction Stop
    Write-Host "✓ Order Service is responding" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "Response: $($response.Content)`n" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Order Service not accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)`n" -ForegroundColor Red
}

Write-Host "=== Health Check Complete ===`n" -ForegroundColor Cyan
