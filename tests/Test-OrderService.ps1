# Isolated Order Service Test Script
# This script tests ONLY the Order Service through the API Gateway

$baseUrl = "http://localhost:5555"
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Order Service Isolated Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Register a user
Write-Host "[Step 1] Registering test user..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$username = "ordertest_$timestamp"
$email = "$username@test.com"

$registerPayload = @{
    username = $username
    email = $email
    password = "Test123!"
    firstName = "Order"
    lastName = "Test"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/auth/register" `
        -Method POST `
        -Body $registerPayload `
        -ContentType "application/json"
    
    Write-Host "✓ User registered: $username" -ForegroundColor Green
    Write-Host "  Status Code: $($registerResponse.StatusCode)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Login
Write-Host "`n[Step 2] Logging in..." -ForegroundColor Yellow
$loginPayload = @{
    emailOrUsername = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/auth/login" `
        -Method POST `
        -Body $loginPayload `
        -ContentType "application/json"
    
    $loginData = $loginResponse.Content | ConvertFrom-Json
    $token = $loginData.token
    $customerId = $loginData.user.id
    
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "  Token (first 20 chars): $($token.Substring(0,20))..." -ForegroundColor Gray
    Write-Host "  Customer ID: $customerId" -ForegroundColor Gray
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Create a product (needed for order)
Write-Host "`n[Step 3] Creating test product..." -ForegroundColor Yellow
$productPayload = @{
    name = "Test Product for Order"
    description = "Test product"
    price = 99.99
    stockQuantity = 100
    category = "Test"
} | ConvertTo-Json

try {
    $productResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/products" `
        -Method POST `
        -Body $productPayload `
        -ContentType "application/json" `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    $productLocation = $productResponse.Headers["Location"]
    $productId = $productLocation -split '/' | Select-Object -Last 1
    
    Write-Host "✓ Product created: $productId" -ForegroundColor Green
    Write-Host "  Status Code: $($productResponse.StatusCode)" -ForegroundColor Gray
    
    # Wait for event propagation
    Write-Host "  Waiting 3 seconds for product cache sync..." -ForegroundColor Gray
    Start-Sleep -Seconds 3
} catch {
    Write-Host "✗ Product creation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Test Order Service - GET orders (should return empty list or 200)
Write-Host "`n[Step 4] Testing GET /api/orders (list orders)..." -ForegroundColor Yellow
try {
    $getOrdersResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/orders?page=1&pageSize=10" `
        -Method GET `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    Write-Host "✓ GET /api/orders works" -ForegroundColor Green
    Write-Host "  Orders count: $($getOrdersResponse.Count)" -ForegroundColor Gray
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ GET /api/orders failed" -ForegroundColor Red
    Write-Host "  Status Code: $statusCode" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Test Order Service - POST order (CREATE)
Write-Host "`n[Step 5] Testing POST /api/orders (create order)..." -ForegroundColor Yellow

$orderPayload = @{
    customerId = $customerId
    customerEmail = $email
    customerName = "Order Test"
    items = @(
        @{
            productId = $productId
            productName = "Test Product for Order"
            price = 99.99
            quantity = 2
        }
    )
    notes = "Test order from isolated test script"
} | ConvertTo-Json -Depth 10

Write-Host "`nOrder Payload:" -ForegroundColor Gray
Write-Host $orderPayload -ForegroundColor Gray
Write-Host "`nRequest Details:" -ForegroundColor Gray
Write-Host "  URL: $baseUrl/api/orders" -ForegroundColor Gray
Write-Host "  Method: POST" -ForegroundColor Gray
Write-Host "  Content-Type: application/json" -ForegroundColor Gray
Write-Host "  Authorization: Bearer $($token.Substring(0,20))..." -ForegroundColor Gray

try {
    $createOrderResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/orders" `
        -Method POST `
        -Body $orderPayload `
        -ContentType "application/json" `
        -Headers @{ "Authorization" = "Bearer $token" } `
        -UseBasicParsing
    
    Write-Host "`n✓ POST /api/orders SUCCESS!" -ForegroundColor Green
    Write-Host "  Status Code: $($createOrderResponse.StatusCode)" -ForegroundColor Green
    Write-Host "  Location Header: $($createOrderResponse.Headers.Location)" -ForegroundColor Gray
    
    $locationHeader = $createOrderResponse.Headers["Location"]
    if ($locationHeader) {
        $orderId = $locationHeader -split '/' | Select-Object -Last 1
        Write-Host "  Order ID: $orderId" -ForegroundColor Green
        
        # Step 6: Verify - GET the created order
        Write-Host "`n[Step 6] Testing GET /api/orders/$orderId (retrieve created order)..." -ForegroundColor Yellow
        try {
            $getOrderResponse = Invoke-RestMethod `
                -Uri "$baseUrl/api/orders/$orderId" `
                -Method GET `
                -Headers @{ "Authorization" = "Bearer $token" }
            
            Write-Host "✓ GET /api/orders/$orderId works" -ForegroundColor Green
            Write-Host "  Order Total: $($getOrderResponse.totalAmount)" -ForegroundColor Gray
            Write-Host "  Order Status: $($getOrderResponse.status)" -ForegroundColor Gray
        } catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-Host "✗ GET /api/orders/$orderId failed" -ForegroundColor Red
            Write-Host "  Status Code: $statusCode" -ForegroundColor Red
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "`n✗ POST /api/orders FAILED!" -ForegroundColor Red
    Write-Host "  Status Code: $statusCode" -ForegroundColor Red
    Write-Host "  Error Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "`nResponse Details:" -ForegroundColor Yellow
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Body: $responseBody" -ForegroundColor Gray
        } catch {
            Write-Host "  Could not read response body" -ForegroundColor Gray
        }
    }
    
    Write-Host "`nDiagnostic Information:" -ForegroundColor Yellow
    Write-Host "  This suggests the gateway cannot route to the Order Service" -ForegroundColor Gray
    Write-Host "  Even though:" -ForegroundColor Gray
    Write-Host "    - User registration worked (Identity Service OK)" -ForegroundColor Gray
    Write-Host "    - Login worked (Identity Service OK)" -ForegroundColor Gray
    Write-Host "    - Product creation worked (Product Service OK)" -ForegroundColor Gray
    Write-Host "    - But Order Service POST fails" -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
