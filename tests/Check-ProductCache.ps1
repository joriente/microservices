# Check Product Cache in Order Service
# Connects to MongoDB to see what products are cached

$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Checking Product Cache in Order Service" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

try {
    # Connect to MongoDB and check the product cache collection
    $mongoCommand = @"
use orderdb
db.productcache.find().pretty()
"@

    Write-Host "Connecting to MongoDB..." -ForegroundColor Yellow
    Write-Host "Database: orderdb" -ForegroundColor Gray
    Write-Host "Collection: productcache`n" -ForegroundColor Gray
    
    # Use mongosh to query the database
    $result = $mongoCommand | mongosh mongodb://localhost:27017 --quiet
    
    if ($result) {
        Write-Host "Product Cache Contents:" -ForegroundColor Green
        Write-Host $result -ForegroundColor White
        
        # Count documents
        $countCommand = "use orderdb`ndb.productcache.countDocuments()"
        $count = $countCommand | mongosh mongodb://localhost:27017 --quiet
        Write-Host "`nTotal cached products: $count" -ForegroundColor Cyan
    } else {
        Write-Host "Product cache is EMPTY" -ForegroundColor Yellow
        Write-Host "This explains why orders are failing with 'Product not found'" -ForegroundColor Yellow
    }

} catch {
    Write-Host "`n✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nNote: Make sure mongosh is installed and in your PATH" -ForegroundColor Yellow
    Write-Host "Install from: https://www.mongodb.com/try/download/shell" -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "To populate the cache, update a product in Product Service" -ForegroundColor Gray
Write-Host "This will trigger ProductUpdatedEvent -> Order Service" -ForegroundColor Gray
Write-Host "========================================`n" -ForegroundColor Cyan
