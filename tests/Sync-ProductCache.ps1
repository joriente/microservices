# Sync Product Cache - Updates all products to trigger cache synchronization
# This forces the Product Service to republish ProductUpdatedEvent for all existing products

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5555" # API Gateway URL

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Product Cache Synchronization" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

try {
    # Step 1: Login as admin
    Write-Host "Logging in as admin..." -ForegroundColor Yellow
    $loginBody = @{
        email = "admin@test.com"
        password = "Admin123!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Admin login successful`n" -ForegroundColor Green

    # Step 2: Get all products
    Write-Host "Fetching all products..." -ForegroundColor Yellow
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    $products = Invoke-RestMethod -Uri "$baseUrl/api/products?page=1&pageSize=100" -Method GET -Headers $headers
    Write-Host "✓ Found $($products.Count) products`n" -ForegroundColor Green

    if ($products.Count -eq 0) {
        Write-Host "No products found. Please create some products first." -ForegroundColor Yellow
        exit 0
    }

    # Step 3: Update each product to trigger event publishing
    Write-Host "Updating products to trigger cache sync..." -ForegroundColor Yellow
    $updateCount = 0
    
    foreach ($product in $products) {
        try {
            # Just update with same data to trigger ProductUpdatedEvent
            $updateBody = @{
                name = $product.name
                description = $product.description
                price = $product.price
                stockQuantity = $product.stockQuantity
                category = $product.category
            } | ConvertTo-Json

            Invoke-RestMethod -Uri "$baseUrl/api/products/$($product.id)" -Method PUT -Body $updateBody -ContentType "application/json" -Headers $headers | Out-Null
            $updateCount++
            Write-Host "  ✓ Updated: $($product.name)" -ForegroundColor Gray
        }
        catch {
            Write-Host "  ✗ Failed to update: $($product.name)" -ForegroundColor Red
        }
    }

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Total Products: $($products.Count)" -ForegroundColor White
    Write-Host "Updated: $updateCount" -ForegroundColor Green
    Write-Host "`nProduct cache should now be synchronized!" -ForegroundColor Green
    Write-Host "You can now create orders with these products.`n" -ForegroundColor Gray

} catch {
    Write-Host "`n✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.ScriptStackTrace)" -ForegroundColor Yellow
    exit 1
}
