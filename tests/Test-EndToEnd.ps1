# Product Ordering System - Complete End-to-End Test Suite
# Tests authentication, products, orders, and cart operations through the API Gateway

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5555" # API Gateway URL

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Product Ordering System - End-to-End Tests" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
Write-Host "API Gateway: $baseUrl`n" -ForegroundColor Gray

# Generate unique user credentials for this test run
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$username = "testuser_$timestamp"
$email = "joriente+$timestamp@radwell.com"
$password = "Test@123"

# Test counters
$testCount = 0
$passedTests = 0
$failedTests = 0

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $script:testCount++
    
    if ($Passed) {
        $script:passedTests++
        Write-Host "✓ Test $testCount PASSED: $TestName" -ForegroundColor Green
        if ($Details) {
            Write-Host "  $Details" -ForegroundColor Gray
        }
    } else {
        $script:failedTests++
        Write-Host "✗ Test $testCount FAILED: $TestName" -ForegroundColor Red
        if ($Details) {
            Write-Host "  $Details" -ForegroundColor Yellow
        }
    }
}

try {
    # ========================================
    # AUTHENTICATION TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 1: Authentication Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 1: Register a new user
    # ==================================================
    Write-Host "`n[Test 1] Registering new user..." -ForegroundColor Yellow
    
    $registerPayload = @{
        username = $username
        email = $email
        password = $password
        firstName = "Test"
        lastName = "User"
    } | ConvertTo-Json
    
    $registerResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/auth/register" `
        -Method POST `
        -Body $registerPayload `
        -ContentType "application/json"
    
    # Extract user ID from Location header
    $locationRegisterHeader = $registerResponse.Headers["Location"]
    if ($locationRegisterHeader) {
        $userId = $locationRegisterHeader -split '/' | Select-Object -Last 1
    } else {
        $userId = $null
    }
    
    Write-TestResult `
        -TestName "User Registration" `
        -Passed ($registerResponse.StatusCode -eq 201 -and $userId) `
        -Details "User ID: $userId, Location: $locationRegisterHeader"

    # ==================================================
    # Test 2: Login to get JWT token
    # ==================================================
    Write-Host "`n[Test 2] Logging in to get authentication token..." -ForegroundColor Yellow
    
    $loginPayload = @{
        emailOrUsername = $email
        password = $password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/auth/login" `
        -Method POST `
        -Body $loginPayload `
        -ContentType "application/json"
    
    $loginData = $loginResponse.Content | ConvertFrom-Json
    $token = $loginData.token
    $customerId = $loginData.user.id
    $customerEmail = $loginData.user.email
    
    Write-TestResult `
        -TestName "User Login" `
        -Passed ($loginResponse.StatusCode -eq 200 -and $token) `
        -Details "Token obtained, User ID: $customerId"

    # ==================================================
    # Test 3: Get current user (authenticated)
    # ==================================================
    Write-Host "`n[Test 3] Getting current user info (authenticated)..." -ForegroundColor Yellow
    
    $meResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/auth/me" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $meData = $meResponse.Content | ConvertFrom-Json
    
    Write-TestResult `
        -TestName "Get Current User (Authenticated)" `
        -Passed ($meResponse.StatusCode -eq 200 -and $meData.id -eq $userId) `
        -Details "Retrieved user: $($meData.username)"

    # ==================================================
    # Test 4: Get current user (unauthenticated - should fail)
    # ==================================================
    Write-Host "`n[Test 4] Getting current user info (unauthenticated - should fail)..." -ForegroundColor Yellow
    
    try {
        $meUnauthResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/auth/me" `
            -Method GET
        
        Write-TestResult `
            -TestName "Get Current User (Unauthenticated)" `
            -Passed $false `
            -Details "Expected 401, got $($meUnauthResponse.StatusCode)"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-TestResult `
            -TestName "Get Current User (Unauthenticated)" `
            -Passed ($statusCode -eq 401) `
            -Details "Correctly returned 401 Unauthorized"
    }

    # ========================================
    # PRODUCT TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 2: Product Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 5: Create product (authenticated)
    # ==================================================
    Write-Host "`n[Test 5] Creating test product (authenticated)..." -ForegroundColor Yellow
    
    $productPayload = @{
        name = "Test Product $timestamp"
        description = "Created via end-to-end test"
        price = 99.99
        stockQuantity = 100
        category = "Electronics"
        imageUrl = "https://example.com/product.jpg"
    } | ConvertTo-Json
    
    $createProductResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/products" `
        -Method POST `
        -Body $productPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    # Extract product ID from Location header (REST principle)
    $locationHeader = $createProductResponse.Headers["Location"]
    if ($locationHeader) {
        $productId = $locationHeader -split '/' | Select-Object -Last 1
    } else {
        $productId = $null
    }
    
    # Get the product details to verify
    if ($productId) {
        $getProductResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/products/$productId" `
            -Method GET
        $productData = $getProductResponse.Content | ConvertFrom-Json
        $productName = $productData.name
        $productPrice = $productData.price
    }
    
    Write-TestResult `
        -TestName "Create Product (Authenticated)" `
        -Passed ($createProductResponse.StatusCode -eq 201 -and $productId) `
        -Details "Product ID: $productId, Location: $locationHeader"

    # ==================================================
    # Test 6: Create product (unauthenticated - should fail)
    # ==================================================
    Write-Host "`n[Test 6] Creating product (unauthenticated - should fail)..." -ForegroundColor Yellow
    
    try {
        $createProductUnauthResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/products" `
            -Method POST `
            -Body $productPayload `
            -ContentType "application/json"
        
        Write-TestResult `
            -TestName "Create Product (Unauthenticated)" `
            -Passed $false `
            -Details "Expected 401, got $($createProductUnauthResponse.StatusCode)"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-TestResult `
            -TestName "Create Product (Unauthenticated)" `
            -Passed ($statusCode -eq 401) `
            -Details "Correctly returned 401 Unauthorized"
    }

    # ==================================================
    # Test 7: Create second product for cart testing
    # ==================================================
    Write-Host "`n[Test 7] Creating second test product..." -ForegroundColor Yellow
    
    $product2Payload = @{
        name = "Second Product $timestamp"
        description = "Second product for cart testing"
        price = 49.99
        stockQuantity = 50
        category = "Electronics"
        imageUrl = "https://example.com/product2.jpg"
    } | ConvertTo-Json
    
    $createProduct2Response = Invoke-WebRequest `
        -Uri "$baseUrl/api/products" `
        -Method POST `
        -Body $product2Payload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    # Extract product ID from Location header
    $location2Header = $createProduct2Response.Headers["Location"]
    if ($location2Header) {
        $product2Id = $location2Header -split '/' | Select-Object -Last 1
    } else {
        $product2Id = $null
    }
    
    # Get the product details to verify
    if ($product2Id) {
        $getProduct2Response = Invoke-WebRequest `
            -Uri "$baseUrl/api/products/$product2Id" `
            -Method GET
        $product2Data = $getProduct2Response.Content | ConvertFrom-Json
        $product2Name = $product2Data.name
        $product2Price = $product2Data.price
    }
    
    Write-TestResult `
        -TestName "Create Second Product" `
        -Passed ($createProduct2Response.StatusCode -eq 201 -and $product2Id) `
        -Details "Product ID: $product2Id, Location: $location2Header"

    # ========================================
    # CART TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 3: Cart Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 8: Get cart by customer ID (should be empty initially)
    # ==================================================
    Write-Host "`n[Test 8] Getting cart by customer ID (should be empty)..." -ForegroundColor Yellow
    
    try {
        $getCartResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/carts/customer/$customerId" `
            -Method GET `
            -Headers @{
                "Authorization" = "Bearer $token"
            }
        
        $cartData = $getCartResponse.Content | ConvertFrom-Json
        $isEmpty = $cartData.items.Count -eq 0
        
        Write-TestResult `
            -TestName "Get Empty Cart" `
            -Passed ($getCartResponse.StatusCode -eq 200 -and $isEmpty) `
            -Details "Cart is empty as expected"
    }
    catch {
        # Cart might not exist yet, which is fine
        if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            Write-TestResult `
                -TestName "Get Empty Cart" `
                -Passed $true `
                -Details "Cart doesn't exist yet (404) - expected for new customer"
        }
        else {
            throw
        }
    }

    # ==================================================
    # Test 9: Add first item to cart
    # ==================================================
    Write-Host "`n[Test 9] Adding first item to cart..." -ForegroundColor Yellow
    
    $addItem1Payload = @{
        productId = $productId
        productName = $productName
        price = $productPrice
        quantity = 2
    } | ConvertTo-Json
    
    $addItem1Response = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/items" `
        -Method POST `
        -Body $addItem1Payload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterAdd1 = $addItem1Response.Content | ConvertFrom-Json
    $cartId = $cartAfterAdd1.id
    
    Write-TestResult `
        -TestName "Add First Item to Cart" `
        -Passed ($addItem1Response.StatusCode -eq 200 -and $cartAfterAdd1.items.Count -eq 1) `
        -Details "Cart ID: $cartId, Items: $($cartAfterAdd1.items.Count), Total: $$($cartAfterAdd1.totalAmount)"

    # ==================================================
    # Test 10: Get cart by ID
    # ==================================================
    Write-Host "`n[Test 10] Getting cart by cart ID..." -ForegroundColor Yellow
    
    $getByIdResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/$cartId" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartById = $getByIdResponse.Content | ConvertFrom-Json
    
    Write-TestResult `
        -TestName "Get Cart by ID" `
        -Passed ($getByIdResponse.StatusCode -eq 200 -and $cartById.id -eq $cartId) `
        -Details "Retrieved cart with $($cartById.items.Count) item(s), Total: $$($cartById.totalAmount)"

    # ==================================================
    # Test 11: Get cart by customer ID
    # ==================================================
    Write-Host "`n[Test 11] Getting cart by customer ID..." -ForegroundColor Yellow
    
    $getByCustomerResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/customer/$customerId" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartByCustomer = $getByCustomerResponse.Content | ConvertFrom-Json
    
    Write-TestResult `
        -TestName "Get Cart by Customer ID" `
        -Passed ($getByCustomerResponse.StatusCode -eq 200 -and $cartByCustomer.id -eq $cartId) `
        -Details "Retrieved cart with $($cartByCustomer.items.Count) item(s)"

    # ==================================================
    # Test 12: Add same product again (should increase quantity)
    # ==================================================
    Write-Host "`n[Test 12] Adding same product again (should increase quantity)..." -ForegroundColor Yellow
    
    $addSameItemPayload = @{
        productId = $productId
        productName = $productName
        price = $productPrice
        quantity = 3
    } | ConvertTo-Json
    
    $addSameItemResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/items" `
        -Method POST `
        -Body $addSameItemPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterSameAdd = $addSameItemResponse.Content | ConvertFrom-Json
    $expectedQuantity = 5 # 2 + 3
    $actualQuantity = ($cartAfterSameAdd.items | Where-Object { $_.productId -eq $productId }).quantity
    
    Write-TestResult `
        -TestName "Add Same Product (Increase Quantity)" `
        -Passed ($addSameItemResponse.StatusCode -eq 200 -and $actualQuantity -eq $expectedQuantity) `
        -Details "Quantity increased to $actualQuantity, Total: $$($cartAfterSameAdd.totalAmount)"

    # ==================================================
    # Test 13: Add second product to cart
    # ==================================================
    Write-Host "`n[Test 13] Adding second product to cart..." -ForegroundColor Yellow
    
    $addItem2Payload = @{
        productId = $product2Id
        productName = $product2Name
        price = $product2Price
        quantity = 1
    } | ConvertTo-Json
    
    $addItem2Response = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/items" `
        -Method POST `
        -Body $addItem2Payload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterAdd2 = $addItem2Response.Content | ConvertFrom-Json
    
    Write-TestResult `
        -TestName "Add Second Product to Cart" `
        -Passed ($addItem2Response.StatusCode -eq 200 -and $cartAfterAdd2.items.Count -eq 2) `
        -Details "Cart now has $($cartAfterAdd2.items.Count) items, Total: $$($cartAfterAdd2.totalAmount)"

    # ==================================================
    # Test 14: Update item quantity
    # ==================================================
    Write-Host "`n[Test 14] Updating item quantity..." -ForegroundColor Yellow
    
    $updateQuantityPayload = @{
        quantity = 10
    } | ConvertTo-Json
    
    $updateQuantityResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/$cartId/items/$productId" `
        -Method PUT `
        -Body $updateQuantityPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    # Get cart to verify quantity update
    $cartAfterUpdate = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/$cartId" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterUpdateData = $cartAfterUpdate.Content | ConvertFrom-Json
    $updatedQuantity = ($cartAfterUpdateData.items | Where-Object { $_.productId -eq $productId }).quantity
    
    Write-TestResult `
        -TestName "Update Item Quantity" `
        -Passed ($updateQuantityResponse.StatusCode -eq 204 -and $updatedQuantity -eq 10) `
        -Details "Quantity updated to $updatedQuantity, Total: $$($cartAfterUpdateData.totalAmount)"

    # ==================================================
    # Test 15: Remove item from cart
    # ==================================================
    Write-Host "`n[Test 15] Removing item from cart..." -ForegroundColor Yellow
    
    $removeItemResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/$cartId/items/$product2Id" `
        -Method DELETE `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    # Get cart to verify item removal
    $cartAfterRemove = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/$cartId" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterRemoveData = $cartAfterRemove.Content | ConvertFrom-Json
    
    Write-TestResult `
        -TestName "Remove Item from Cart" `
        -Passed ($removeItemResponse.StatusCode -eq 204 -and $cartAfterRemoveData.items.Count -eq 1) `
        -Details "Cart now has $($cartAfterRemoveData.items.Count) item(s), Total: $$($cartAfterRemoveData.totalAmount)"

    # ==================================================
    # Test 16: Verify total calculation
    # ==================================================
    Write-Host "`n[Test 16] Verifying cart total calculation..." -ForegroundColor Yellow
    
    $expectedTotal = $productPrice * 10 # 10 items at $99.99
    $actualTotal = $cartAfterRemoveData.totalAmount
    $totalMatches = [Math]::Abs($expectedTotal - $actualTotal) -lt 0.01
    
    Write-TestResult `
        -TestName "Verify Total Calculation" `
        -Passed $totalMatches `
        -Details "Expected: $$expectedTotal, Actual: $$actualTotal"

    # ========================================
    # ORDER TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 4: Order Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 17: Create order (authenticated)
    # ==================================================
    Write-Host "`n[Test 17] Creating order from cart items (authenticated)..." -ForegroundColor Yellow
    
    $orderPayload = @{
        customerId = $customerId
        customerEmail = $customerEmail
        customerName = "Test User"
        items = @(
            @{
                productId = $productId
                productName = $productName
                price = $productPrice
                quantity = 2
            }
        )
        notes = "Test order from end-to-end script"
    } | ConvertTo-Json -Depth 10
    
    try {
        $createOrderResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/orders" `
            -Method POST `
            -Body $orderPayload `
            -ContentType "application/json" `
            -Headers @{
                "Authorization" = "Bearer $token"
            }
        
        # Extract order ID from Location header
        $locationOrderHeader = $createOrderResponse.Headers["Location"]
        if ($locationOrderHeader) {
            $orderId = $locationOrderHeader -split '/' | Select-Object -Last 1
        } else {
            $orderId = $null
        }
        
        # Get the order details to verify
        if ($orderId) {
            $getOrderResponse = Invoke-WebRequest `
                -Uri "$baseUrl/api/orders/$orderId" `
                -Method GET `
                -Headers @{
                    "Authorization" = "Bearer $token"
                }
            $orderData = $getOrderResponse.Content | ConvertFrom-Json
        }
        
        Write-TestResult `
            -TestName "Create Order (Authenticated)" `
            -Passed ($createOrderResponse.StatusCode -eq 201 -and $orderId) `
            -Details "Order ID: $orderId, Location: $locationOrderHeader"
    } catch {
        Write-TestResult `
            -TestName "Create Order (Authenticated)" `
            -Passed $false `
            -Details "HTTP Error: $($_.Exception.Response.StatusCode.value__) - $($_.Exception.Message)"
    }

    # ==================================================
    # Test 18: Create order (unauthenticated - should fail)
    # ==================================================
    Write-Host "`n[Test 18] Creating order (unauthenticated - should fail)..." -ForegroundColor Yellow
    
    try {
        $createOrderUnauthResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/orders" `
            -Method POST `
            -Body $orderPayload `
            -ContentType "application/json"
        
        Write-TestResult `
            -TestName "Create Order (Unauthenticated)" `
            -Passed $false `
            -Details "Expected 401, got $($createOrderUnauthResponse.StatusCode)"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-TestResult `
            -TestName "Create Order (Unauthenticated)" `
            -Passed ($statusCode -eq 401) `
            -Details "Correctly returned 401 Unauthorized"
    }

    # ==================================================
    # Test 23: Verify cart is automatically cleared after order (RabbitMQ Event)
    # ==================================================
    Write-Host "`n[Test 23] Verifying cart auto-cleared after order (RabbitMQ event)..." -ForegroundColor Yellow
    Write-Host "  Waiting 2 seconds for OrderCreatedEvent to be processed..." -ForegroundColor Gray
    
    # Wait for event processing (OrderCreatedEvent -> Cart Service)
    Start-Sleep -Seconds 2
    
    $cartAfterOrderResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/me" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartAfterOrderData = $cartAfterOrderResponse.Content | ConvertFrom-Json
    $cartIsEmpty = $cartAfterOrderData.items.Count -eq 0
    $totalIsZero = $cartAfterOrderData.totalAmount -eq 0
    
    Write-TestResult `
        -TestName "Cart Auto-Cleared After Order (Event-Driven)" `
        -Passed ($cartIsEmpty -and $totalIsZero) `
        -Details "Cart Items: $($cartAfterOrderData.items.Count), Total: $$($cartAfterOrderData.totalAmount)"

    if (-not $cartIsEmpty) {
        Write-Host "  ⚠️  Cart was not automatically cleared. Checking if OrderCreatedEventConsumer is running..." -ForegroundColor Yellow
        Write-Host "  💡 Verify Cart Service logs show 'Received OrderCreatedEvent' message" -ForegroundColor Cyan
    }

    # ==================================================
    # Test 24: Manual clear cart (fallback test)
    # ==================================================
    Write-Host "`n[Test 24] Manual clear cart (if auto-clear failed)..." -ForegroundColor Yellow
    
    if (-not $cartIsEmpty) {
        $clearCartResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/carts/$cartId" `
            -Method DELETE `
            -Headers @{
                "Authorization" = "Bearer $token"
            }
    
        Write-TestResult `
            -TestName "Manual Clear Cart (Fallback)" `
            -Passed ($clearCartResponse.StatusCode -eq 204) `
            -Details "Cart manually cleared successfully"
    } else {
        Write-TestResult `
            -TestName "Manual Clear Cart (Fallback)" `
            -Passed $true `
            -Details "Skipped - cart already cleared by event"
    }

    # ==================================================
    # Test 25: Cart operations without authentication (should fail)
    # ==================================================
    Write-Host "`n[Test 25] Cart operations without authentication (should fail)..." -ForegroundColor Yellow
    
    try {
        $unauthCartResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/carts/items" `
            -Method POST `
            -Body $addItem1Payload `
            -ContentType "application/json"
        
        Write-TestResult `
            -TestName "Cart Operations (Unauthenticated)" `
            -Passed $false `
            -Details "Expected 401, got $($unauthCartResponse.StatusCode)"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-TestResult `
            -TestName "Cart Operations (Unauthenticated)" `
            -Passed ($statusCode -eq 401) `
            -Details "Correctly returned 401 Unauthorized"
    }

    # ========================================
    # NOTIFICATION SERVICE TESTS (Java/Spring Boot)
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 5: Notification Service Tests (Polyglot)" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 20: Check NotificationService health
    # ==================================================
    Write-Host "`n[Test 20] Checking Java NotificationService health..." -ForegroundColor Yellow
    
    try {
        $notificationHealthResponse = Invoke-WebRequest `
            -Uri "http://localhost:8085/actuator/health" `
            -Method GET
        
        # Convert byte array to string if needed (PowerShell 7+ returns byte array)
        $contentString = if ($notificationHealthResponse.Content -is [byte[]]) {
            [System.Text.Encoding]::UTF8.GetString($notificationHealthResponse.Content)
        } else {
            $notificationHealthResponse.Content
        }
        
        $healthData = $contentString | ConvertFrom-Json
        $isUp = $healthData.status -eq "UP"
        $mongoStatus = if ($healthData.components.mongo) { $healthData.components.mongo.status } else { "N/A" }
        $rabbitStatus = if ($healthData.components.rabbit) { $healthData.components.rabbit.status } else { "N/A" }
        $hasMongoUp = $mongoStatus -eq "UP"
        $hasRabbitUp = $rabbitStatus -eq "UP"
        
        Write-TestResult `
            -TestName "NotificationService Health Check" `
            -Passed ($notificationHealthResponse.StatusCode -eq 200 -and $isUp -and $hasMongoUp -and $hasRabbitUp) `
            -Details "Status: $($healthData.status), MongoDB: $mongoStatus, RabbitMQ: $rabbitStatus"
    }
    catch {
        Write-TestResult `
            -TestName "NotificationService Health Check" `
            -Passed $false `
            -Details "NotificationService is not running on port 8085. Please start it first."
        
        Write-Host "  💡 Start NotificationService with: cd src\Services\NotificationService; mvn spring-boot:run" -ForegroundColor Yellow
    }

    # ==================================================
    # Test 21: Verify notification created for order (OrderCreatedEvent)
    # ==================================================
    Write-Host "`n[Test 21] Verifying notification created for order (OrderCreatedEvent)..." -ForegroundColor Yellow
    Write-Host "  Waiting 3 seconds for OrderCreatedEvent to be processed by Java service..." -ForegroundColor Gray
    
    # Wait for event processing
    Start-Sleep -Seconds 3
    
    try {
        $notificationsResponse = Invoke-WebRequest `
            -Uri "http://localhost:8085/api/notifications?customerId=$customerId" `
            -Method GET
        
        $notifications = $notificationsResponse.Content | ConvertFrom-Json
        
        # Find notification for our order
        $orderNotification = $notifications | Where-Object { 
            $_.type -eq "ORDER_CONFIRMATION" -and $_.orderId -eq $orderId 
        }
        
        $notificationCreated = $null -ne $orderNotification
        $notificationSent = $false
        
        if ($orderNotification) {
            $notificationSent = $orderNotification.status -eq "SENT"
        }
        
        Write-TestResult `
            -TestName "Order Notification Created (Java Service)" `
            -Passed $notificationCreated `
            -Details "Notification found: $notificationCreated, Status: $($orderNotification.status), Total notifications: $($notifications.Count)"
        
        if ($notificationCreated -and -not $notificationSent) {
            Write-Host "  ⚠️  Notification created but not sent yet. Status: $($orderNotification.status)" -ForegroundColor Yellow
            Write-Host "  💡 Check NotificationService logs for SendGrid API errors" -ForegroundColor Cyan
        }
    }
    catch {
        Write-TestResult `
            -TestName "Order Notification Created (Java Service)" `
            -Passed $false `
            -Details "Could not retrieve notifications: $($_.Exception.Message)"
    }

    # ==================================================
    # Test 22: Get all notifications for customer
    # ==================================================
    Write-Host "`n[Test 22] Getting all notifications for customer..." -ForegroundColor Yellow
    
    try {
        $allNotificationsResponse = Invoke-WebRequest `
            -Uri "http://localhost:8085/api/notifications?customerId=$customerId" `
            -Method GET
        
        $allNotifications = $allNotificationsResponse.Content | ConvertFrom-Json
        
        Write-TestResult `
            -TestName "Get All Notifications for Customer" `
            -Passed ($allNotificationsResponse.StatusCode -eq 200) `
            -Details "Found $($allNotifications.Count) notification(s) for customer $customerId"
        
        if ($allNotifications.Count -gt 0) {
            Write-Host "  Notification types:" -ForegroundColor Gray
            foreach ($notif in $allNotifications) {
                Write-Host "    - $($notif.type): $($notif.status) (Recipient: $($notif.recipientEmail))" -ForegroundColor DarkGray
            }
        }
    }
    catch {
        Write-TestResult `
            -TestName "Get All Notifications for Customer" `
            -Passed $false `
            -Details "Error: $($_.Exception.Message)"
    }

    # ========================================
    # EVENT-DRIVEN INTEGRATION TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 6: Event-Driven Integration Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 26: Verify product cache synchronization (create new product)
    # ==================================================
    Write-Host "`n[Test 26] Testing product cache synchronization (ProductCreatedEvent)..." -ForegroundColor Yellow
    
    $cacheTestProductPayload = @{
        name = "Cache Test Product $timestamp"
        description = "Testing product cache synchronization via RabbitMQ"
        price = 129.99
        stockQuantity = 20
        category = "Testing"
        imageUrl = "https://example.com/cache-test.jpg"
    } | ConvertTo-Json
    
    $createCacheTestProductResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/products" `
        -Method POST `
        -Body $cacheTestProductPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    # Extract product ID from Location header
    $locationCacheTestHeader = $createCacheTestProductResponse.Headers["Location"]
    if ($locationCacheTestHeader) {
        $cacheTestProductId = $locationCacheTestHeader -split '/' | Select-Object -Last 1
    } else {
        $cacheTestProductId = $null
    }
    
    # Get the product details to verify
    if ($cacheTestProductId) {
        $getCacheTestProductResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/products/$cacheTestProductId" `
            -Method GET
        $cacheTestProductData = $getCacheTestProductResponse.Content | ConvertFrom-Json
    }
    
    Write-Host "  Created product: $cacheTestProductId" -ForegroundColor Gray
    Write-Host "  Waiting 2 seconds for ProductCreatedEvent to be processed..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
    
    # Try to add this product to cart (will use cached data)
    $addCacheTestItemPayload = @{
        productId = $cacheTestProductId
        productName = $cacheTestProductData.name
        price = $cacheTestProductData.price
        quantity = 1
    } | ConvertTo-Json
    
    $addCacheTestItemResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/carts/items" `
        -Method POST `
        -Body $addCacheTestItemPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }
    
    $cartWithCachedProduct = $addCacheTestItemResponse.Content | ConvertFrom-Json
    $cachedProductInCart = $cartWithCachedProduct.items | Where-Object { $_.productId -eq $cacheTestProductId }
    
    Write-TestResult `
        -TestName "Product Cache Synchronization (ProductCreatedEvent)" `
        -Passed ($addCacheTestItemResponse.StatusCode -eq 200 -and $cachedProductInCart) `
        -Details "Product added to cart using cached data. Cart has $($cartWithCachedProduct.items.Count) item(s)"

    if (-not $cachedProductInCart) {
        Write-Host "  ⚠️  Product cache may not be synchronized. Check Cart Service logs for 'ProductCreatedEvent'." -ForegroundColor Yellow
    }

    # ========================================
    # PART 7: INVENTORY SERVICE TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 7: Inventory Service Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 27: Check Inventory Service health
    # ==================================================
    Write-Host "`n[Test 27] Checking Inventory Service health..." -ForegroundColor Yellow

    $inventoryHealthResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/inventory/health" `
        -Method GET

    Write-TestResult `
        -TestName "Inventory Service Health Check" `
        -Passed ($inventoryHealthResponse.StatusCode -eq 200) `
        -Details "Status: $($inventoryHealthResponse.StatusCode)"

    # ==================================================
    # Test 28: Get inventory for a product
    # ==================================================
    Write-Host "`n[Test 28] Getting inventory for product $productId..." -ForegroundColor Yellow

    $getInventoryResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/inventory/$productId" `
        -Method GET

    $inventoryData = $getInventoryResponse.Content | ConvertFrom-Json

    Write-TestResult `
        -TestName "Get Product Inventory" `
        -Passed ($getInventoryResponse.StatusCode -eq 200 -and $inventoryData.productId -eq $productId) `
        -Details "Product: $productId, Available: $($inventoryData.availableQuantity)"

    # ==================================================
    # Test 29: Reserve inventory (authenticated)
    # ==================================================
    Write-Host "`n[Test 29] Reserving inventory for product (authenticated)..." -ForegroundColor Yellow

    $reserveOrderId = [Guid]::NewGuid().ToString()
    $reserveInventoryPayload = @{
        orderId = $reserveOrderId
        items = @(
            @{
                productId = $productId
                quantity = 2
            }
        )
    } | ConvertTo-Json -Depth 3

    try {
        $reserveInventoryResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/inventory/reserve" `
            -Method POST `
            -Body $reserveInventoryPayload `
            -ContentType "application/json" `
            -Headers @{
                "Authorization" = "Bearer $token"
            }

        Write-TestResult `
            -TestName "Reserve Inventory (Authenticated)" `
            -Passed ($reserveInventoryResponse.StatusCode -eq 201) `
            -Details "Reserved 2 units of product $productId, Order: $reserveOrderId"
    }
    catch {
        $errorResponse = ""
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorResponse = $reader.ReadToEnd()
                $reader.Close()
            }
            catch {
                $errorResponse = "Could not read error response"
            }
        }
        
        Write-TestResult `
            -TestName "Reserve Inventory (Authenticated)" `
            -Passed $false `
            -Details "Failed with status $($_.Exception.Response.StatusCode.value__): $errorResponse"
        
        Write-Host "Request payload was: $reserveInventoryPayload" -ForegroundColor Gray
    }

    # ==================================================
    # Test 30: Reserve inventory (unauthenticated - should fail)
    # ==================================================
    Write-Host "`n[Test 30] Reserving inventory without authentication (should fail)..." -ForegroundColor Yellow

    $reserveNoAuthPayload = @{
        orderId = [Guid]::NewGuid().ToString()
        items = @(
            @{
                productId = $productId
                quantity = 1
            }
        )
    } | ConvertTo-Json -Depth 3

    try {
        $reserveNoAuthResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/inventory/reserve" `
            -Method POST `
            -Body $reserveNoAuthPayload `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        $reserveNoAuthPassed = $false
    }
    catch {
        $reserveNoAuthPassed = ($_.Exception.Response.StatusCode.value__ -eq 401)
    }

    Write-TestResult `
        -TestName "Reserve Inventory Without Auth (Should Fail)" `
        -Passed $reserveNoAuthPassed `
        -Details "Expected 401 Unauthorized"

    # ========================================
    # PART 8: CUSTOMER SERVICE TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 8: Customer Service Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 31: Check Customer Service health
    # ==================================================
    Write-Host "`n[Test 31] Checking Customer Service health..." -ForegroundColor Yellow

    $customerHealthResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/customers/health" `
        -Method GET

    Write-TestResult `
        -TestName "Customer Service Health Check" `
        -Passed ($customerHealthResponse.StatusCode -eq 200) `
        -Details "Status: $($customerHealthResponse.StatusCode)"

    # ==================================================
    # Test 32: Create a customer (authenticated)
    # ==================================================
    Write-Host "`n[Test 32] Creating a customer (authenticated)..." -ForegroundColor Yellow

    $createCustomerPayload = @{
        firstName = "Test"
        lastName = "Customer"
        email = "joriente+$timestamp@radwell.com"
        phoneNumber = "+1234567890"
        address = @{
            street = "123 Test St"
            city = "Test City"
            state = "TS"
            zipCode = "12345"
            country = "TestCountry"
        }
    } | ConvertTo-Json

    $createCustomerResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/customers" `
        -Method POST `
        -Body $createCustomerPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }

    # Extract customer ID from Location header or response body
    $customerId = $null
    if ($createCustomerResponse.Headers.ContainsKey("Location")) {
        $customerId = $createCustomerResponse.Headers["Location"] -split '/' | Select-Object -Last 1
    }
    elseif ($createCustomerResponse.Content) {
        $customerResponseData = $createCustomerResponse.Content | ConvertFrom-Json
        $customerId = $customerResponseData.id
    }

    Write-TestResult `
        -TestName "Create Customer (Authenticated)" `
        -Passed ($createCustomerResponse.StatusCode -in @(200, 201) -and $customerId) `
        -Details "Created customer: $customerId"

    # ==================================================
    # Test 33: Get customer by ID
    # ==================================================
    Write-Host "`n[Test 33] Getting customer by ID..." -ForegroundColor Yellow

    if ($customerId) {
        $getCustomerResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/customers/$customerId" `
            -Method GET `
            -Headers @{
                "Authorization" = "Bearer $token"
            }

        $customerData = $getCustomerResponse.Content | ConvertFrom-Json

        Write-TestResult `
            -TestName "Get Customer by ID" `
            -Passed ($getCustomerResponse.StatusCode -eq 200 -and $customerData.id -eq $customerId) `
            -Details "Customer: $($customerData.firstName) $($customerData.lastName)"
    }
    else {
        Write-TestResult `
            -TestName "Get Customer by ID" `
            -Passed $false `
            -Details "No customer ID available from previous test"
    }

    # ==================================================
    # Test 34: Get customers list with pagination
    # ==================================================
    Write-Host "`n[Test 34] Getting customers list with pagination..." -ForegroundColor Yellow

    $getCustomersResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/customers?page=1&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }

    $customersData = $getCustomersResponse.Content | ConvertFrom-Json
    $isArray = $customersData -is [Array]
    $hasCustomers = $isArray -and $customersData.Count -gt 0
    
    # Check for Pagination header
    $hasPaginationHeader = $getCustomersResponse.Headers.ContainsKey("Pagination")
    $paginationValid = $false
    
    if ($hasPaginationHeader) {
        try {
            $paginationData = $getCustomersResponse.Headers["Pagination"] | ConvertFrom-Json
            $paginationValid = $null -ne $paginationData.Page -and 
                             $null -ne $paginationData.PageSize -and 
                             $null -ne $paginationData.TotalCount -and 
                             $null -ne $paginationData.TotalPages
        }
        catch {
            $paginationValid = $false
        }
    }

    Write-TestResult `
        -TestName "Get Customers List with Pagination" `
        -Passed ($getCustomersResponse.StatusCode -eq 200 -and $hasCustomers -and $hasPaginationHeader -and $paginationValid -and $isArray) `
        -Details "Retrieved $($customersData.Count) customer(s), Pagination header: $hasPaginationHeader, Valid structure: $paginationValid, Returns array: $isArray"

    # ==================================================
    # Test 35: Create customer without authentication (should fail)
    # ==================================================
    Write-Host "`n[Test 35] Creating customer without authentication (should fail)..." -ForegroundColor Yellow

    $createCustomerNoAuthPayload = @{
        firstName = "Unauthorized"
        lastName = "User"
        email = "unauthorized_$timestamp@test.com"
        phoneNumber = "+9876543210"
        address = @{
            street = "456 Fail St"
            city = "Fail City"
            state = "FL"
            zipCode = "54321"
            country = "FailCountry"
        }
    } | ConvertTo-Json

    try {
        $createCustomerNoAuthResponse = Invoke-WebRequest `
            -Uri "$baseUrl/api/customers" `
            -Method POST `
            -Body $createCustomerNoAuthPayload `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        $createCustomerNoAuthPassed = $false
    }
    catch {
        $createCustomerNoAuthPassed = ($_.Exception.Response.StatusCode.value__ -eq 401)
    }

    Write-TestResult `
        -TestName "Create Customer Without Auth (Should Fail)" `
        -Passed $createCustomerNoAuthPassed `
        -Details "Expected 401 Unauthorized"

    # ========================================
    # PART 9: REST API COMPLIANCE TESTS
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PART 9: REST API Compliance Tests" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    # ==================================================
    # Test 36: Verify POST /api/products follows REST principles
    # ==================================================
    Write-Host "`n[Test 36] REST: POST /api/products returns 201 + Location + empty body..." -ForegroundColor Yellow

    $restProductPayload = @{
        name = "REST Compliance Product $timestamp"
        description = "Testing REST principles"
        price = 149.99
        stockQuantity = 50
        category = "Testing"
        imageUrl = "https://example.com/rest-product.jpg"
    } | ConvertTo-Json

    $restProductResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/products" `
        -Method POST `
        -Body $restProductPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }

    $hasLocationHeader = $restProductResponse.Headers.ContainsKey("Location")
    $locationMatchesPattern = $false
    if ($hasLocationHeader) {
        $locationMatchesPattern = $restProductResponse.Headers["Location"] -match "/api/products/[a-f0-9\-]+"
    }
    $hasEmptyBody = $restProductResponse.Content.Length -eq 0 -or $restProductResponse.Content -eq ""

    $restCompliant = ($restProductResponse.StatusCode -eq 201) -and $hasLocationHeader -and $locationMatchesPattern -and $hasEmptyBody

    Write-TestResult `
        -TestName "POST /api/products REST Compliance" `
        -Passed $restCompliant `
        -Details "Status: $($restProductResponse.StatusCode), Location: $($restProductResponse.Headers['Location']), Body empty: $hasEmptyBody"

    # ==================================================
    # Test 37: Verify POST /api/orders follows REST principles
    # ==================================================
    Write-Host "`n[Test 37] REST: POST /api/orders returns 201 + Location + empty body..." -ForegroundColor Yellow

    $restOrderPayload = @{
        customerId = $customerId
        customerEmail = $customerEmail
        customerName = "REST Test User"
        items = @(
            @{
                productId = $productId
                productName = "REST Test Product"
                price = 99.99
                quantity = 1
            }
        )
        notes = "REST compliance test order"
    } | ConvertTo-Json -Depth 10

    $restOrderResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/orders" `
        -Method POST `
        -Body $restOrderPayload `
        -ContentType "application/json" `
        -Headers @{
            "Authorization" = "Bearer $token"
        }

    $hasLocationHeader = $restOrderResponse.Headers.ContainsKey("Location")
    $locationMatchesPattern = $false
    if ($hasLocationHeader) {
        $locationMatchesPattern = $restOrderResponse.Headers["Location"] -match "/api/orders/[a-f0-9\-]+"
    }
    $hasEmptyBody = $restOrderResponse.Content.Length -eq 0 -or $restOrderResponse.Content -eq ""

    $restCompliant = ($restOrderResponse.StatusCode -eq 201) -and $hasLocationHeader -and $locationMatchesPattern -and $hasEmptyBody

    Write-TestResult `
        -TestName "POST /api/orders REST Compliance" `
        -Passed $restCompliant `
        -Details "Status: $($restOrderResponse.StatusCode), Location: $($restOrderResponse.Headers['Location']), Body empty: $hasEmptyBody"

    # ==================================================
    # Test 38: Verify GET /api/products (paginated) returns array + Pagination header
    # ==================================================
    Write-Host "`n[Test 38] REST: GET /api/products returns array + Pagination header..." -ForegroundColor Yellow

    $searchProductsResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/products?page=1&pageSize=5" `
        -Method GET

    $hasPaginationHeader = $searchProductsResponse.Headers.ContainsKey("Pagination")
    $paginationIsValid = $false
    $returnsArray = $false

    if ($hasPaginationHeader) {
        try {
            $paginationData = $searchProductsResponse.Headers["Pagination"] | ConvertFrom-Json
            $paginationIsValid = ($paginationData.PSObject.Properties.Name -contains "Page") -and
                               ($paginationData.PSObject.Properties.Name -contains "PageSize") -and
                               ($paginationData.PSObject.Properties.Name -contains "TotalCount") -and
                               ($paginationData.PSObject.Properties.Name -contains "TotalPages") -and
                               ($paginationData.PSObject.Properties.Name -contains "HasPrevious") -and
                               ($paginationData.PSObject.Properties.Name -contains "HasNext")
        }
        catch {
            $paginationIsValid = $false
        }
    }

    try {
        $productsData = $searchProductsResponse.Content | ConvertFrom-Json
        $returnsArray = $productsData -is [array]
    }
    catch {
        $returnsArray = $false
    }

    $restCompliant = ($searchProductsResponse.StatusCode -eq 200) -and $hasPaginationHeader -and $paginationIsValid -and $returnsArray

    Write-TestResult `
        -TestName "GET /api/products Pagination REST Compliance" `
        -Passed $restCompliant `
        -Details "Status: 200, Has Pagination header: $hasPaginationHeader, Valid structure: $paginationIsValid, Returns array: $returnsArray"

    if ($paginationIsValid) {
        Write-Host "  Pagination: Page $($paginationData.Page)/$($paginationData.TotalPages), Total: $($paginationData.TotalCount)" -ForegroundColor Gray
    }

    # ==================================================
    # Test 39: Verify GET /api/orders (paginated) returns array + Pagination header
    # ==================================================
    Write-Host "`n[Test 39] REST: GET /api/orders returns array + Pagination header..." -ForegroundColor Yellow

    $getOrdersResponse = Invoke-WebRequest `
        -Uri "$baseUrl/api/orders?page=1&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $token"
        }

    $hasPaginationHeader = $getOrdersResponse.Headers.ContainsKey("Pagination")
    $paginationIsValid = $false
    $returnsArray = $false

    if ($hasPaginationHeader) {
        try {
            $paginationData = $getOrdersResponse.Headers["Pagination"] | ConvertFrom-Json
            $paginationIsValid = ($paginationData.PSObject.Properties.Name -contains "Page") -and
                               ($paginationData.PSObject.Properties.Name -contains "PageSize") -and
                               ($paginationData.PSObject.Properties.Name -contains "TotalCount") -and
                               ($paginationData.PSObject.Properties.Name -contains "TotalPages") -and
                               ($paginationData.PSObject.Properties.Name -contains "HasPrevious") -and
                               ($paginationData.PSObject.Properties.Name -contains "HasNext")
        }
        catch {
            $paginationIsValid = $false
        }
    }

    try {
        $ordersData = $getOrdersResponse.Content | ConvertFrom-Json
        $returnsArray = $ordersData -is [array]
    }
    catch {
        $returnsArray = $false
    }

    $restCompliant = ($getOrdersResponse.StatusCode -eq 200) -and $hasPaginationHeader -and $paginationIsValid -and $returnsArray

    Write-TestResult `
        -TestName "GET /api/orders Pagination REST Compliance" `
        -Passed $restCompliant `
        -Details "Status: 200, Has Pagination header: $hasPaginationHeader, Valid structure: $paginationIsValid, Returns array: $returnsArray"

    if ($paginationIsValid) {
        Write-Host "  Pagination: Page $($paginationData.Page)/$($paginationData.TotalPages), Total: $($paginationData.TotalCount)" -ForegroundColor Gray
    }

    # ==================================================
    # Test 40: Verify resources are accessible via Location header
    # ==================================================
    Write-Host "`n[Test 40] REST: Resources accessible via Location header..." -ForegroundColor Yellow

    $canAccessProduct = $false
    $canAccessOrder = $false

    # Test product accessibility
    if ($restProductResponse.Headers.ContainsKey("Location")) {
        $productLocation = $restProductResponse.Headers["Location"]
        try {
            $getRestProductResponse = Invoke-WebRequest `
                -Uri "$baseUrl$productLocation" `
                -Method GET
            
            $retrievedProduct = $getRestProductResponse.Content | ConvertFrom-Json
            $canAccessProduct = ($getRestProductResponse.StatusCode -eq 200) -and ($retrievedProduct.id)
        }
        catch {
            $canAccessProduct = $false
        }
    }

    # Test order accessibility
    if ($restOrderResponse.Headers.ContainsKey("Location")) {
        $orderLocation = $restOrderResponse.Headers["Location"]
        try {
            $getRestOrderResponse = Invoke-WebRequest `
                -Uri "$baseUrl$orderLocation" `
                -Method GET `
                -Headers @{
                    "Authorization" = "Bearer $token"
                }
            
            $retrievedOrder = $getRestOrderResponse.Content | ConvertFrom-Json
            $canAccessOrder = ($getRestOrderResponse.StatusCode -eq 200) -and ($retrievedOrder.id)
        }
        catch {
            $canAccessOrder = $false
        }
    }

    Write-TestResult `
        -TestName "Resources Accessible via Location Header" `
        -Passed ($canAccessProduct -and $canAccessOrder) `
        -Details "Product accessible: $canAccessProduct, Order accessible: $canAccessOrder"

    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "ALL TESTS COMPLETED!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green
    
    Write-Host "📊 Test Coverage Summary:" -ForegroundColor Cyan
    Write-Host "  • Authentication (Registration, Login, Authorization)" -ForegroundColor White
    Write-Host "  • Product Management (CRUD operations)" -ForegroundColor White
    Write-Host "  • Cart Operations (Add, Update, Remove, Clear)" -ForegroundColor White
    Write-Host "  • Order Creation & Retrieval" -ForegroundColor White
    Write-Host "  • Notification Service (Java/Spring Boot - Health & Events)" -ForegroundColor White
    Write-Host "  • Event-Driven Integration (RabbitMQ)" -ForegroundColor White
    Write-Host "  • Inventory Service (Health, Get Inventory, Reserve)" -ForegroundColor White
    Write-Host "  • Customer Service (Health, Create, Get, List)" -ForegroundColor White
    Write-Host "  • REST API Compliance (Location headers, Pagination)" -ForegroundColor White
    
    Write-Host "`n📊 Event-Driven Integration:" -ForegroundColor Cyan
    Write-Host "  • OrderCreatedEvent → Cart Service (auto-clear cart)" -ForegroundColor White
    Write-Host "  • OrderCreatedEvent → NotificationService (send order email)" -ForegroundColor White
    Write-Host "  • ProductCreatedEvent → Cart Service (cache product)" -ForegroundColor White
    
    Write-Host "`n🌐 Polyglot Architecture Verified:" -ForegroundColor Cyan
    Write-Host "  • .NET 9 Microservices (C# - Identity, Product, Cart, Order, Payment, Inventory, Customer)" -ForegroundColor White
    Write-Host "  • Java 21 Microservice (Spring Boot 3.2.0 - Notification)" -ForegroundColor White
    Write-Host "  • Shared RabbitMQ message bus (MassTransit + Spring AMQP)" -ForegroundColor White
    Write-Host "  • Cross-language event consumption verified" -ForegroundColor White
    
    Write-Host "`n✅ REST API Principles Verified:" -ForegroundColor Cyan
    Write-Host "  • POST endpoints return 201 + Location header + empty body" -ForegroundColor White
    Write-Host "  • Paginated GET endpoints return array + Pagination header" -ForegroundColor White
    Write-Host "  • Created resources accessible via Location header" -ForegroundColor White
    
    Write-Host "`n💡 Check Aspire Dashboard logs for event processing details!" -ForegroundColor Cyan
    Write-Host "   - Cart Service should show:" -ForegroundColor Gray
    Write-Host "     'Received OrderCreatedEvent for Order {...}'" -ForegroundColor DarkGray
    Write-Host "     'Successfully cleared cart {...} for Customer {...}'" -ForegroundColor DarkGray
    Write-Host "     'Received ProductCreatedEvent for Product {...}'" -ForegroundColor DarkGray
    Write-Host "     'Successfully cached Product {...}'" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   - NotificationService (Java) should show:" -ForegroundColor Gray
    Write-Host "     'Received OrderCreatedEvent for order: {...}'" -ForegroundColor DarkGray
    Write-Host "     'Email sent successfully to {...}'" -ForegroundColor DarkGray
    Write-Host "     'Notification marked as SENT: {...}'" -ForegroundColor DarkGray

}
catch {
    Write-Host "`n✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    
    if ($_.Exception.Response) {
        Write-Host "`nHTTP Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody" -ForegroundColor Gray
        }
        catch {
            Write-Host "Could not read response body" -ForegroundColor Gray
        }
    }
}
finally {
    # ========================================
    # TEST SUMMARY
    # ========================================
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Test Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Total Tests: $testCount" -ForegroundColor White
    Write-Host "Passed: $passedTests" -ForegroundColor Green
    Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
    
    if ($passedTests -eq $testCount) {
        Write-Host "`n🎉 ALL TESTS PASSED! 🎉" -ForegroundColor Green
        Write-Host "The Product Ordering System is working correctly!" -ForegroundColor Green
    }
    elseif ($failedTests -eq 0 -and $testCount -gt 0) {
        Write-Host "`n⚠ Tests incomplete (some tests may not have run)" -ForegroundColor Yellow
    }
    else {
        Write-Host "`n❌ Some tests failed. Please review the output above." -ForegroundColor Red
    }
    
    Write-Host "`n========================================`n" -ForegroundColor Cyan
}
