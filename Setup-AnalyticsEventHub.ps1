# Setup script for Analytics Service Event Hub configuration
# This script helps you configure the Event Hub connection string for streaming to Microsoft Fabric

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Analytics Event Hub Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your Event Hub Namespace: evhns-product-ordering.servicebus.windows.net" -ForegroundColor Green
Write-Host "Storage Account: stprodorderanalytics" -ForegroundColor Green
Write-Host ""

# Project path
$projectPath = "src/Services/AnalyticsService/ProductOrderingSystem.AnalyticsService.WebAPI"

Write-Host "Step 1: Get your Event Hub connection string from Azure Portal" -ForegroundColor Yellow
Write-Host "  1. Go to Azure Portal -> Event Hubs Namespace (evhns-product-ordering)" -ForegroundColor White
Write-Host "  2. Navigate to 'Shared access policies'" -ForegroundColor White
Write-Host "  3. Select a policy with 'Send' permissions (or create new one)" -ForegroundColor White
Write-Host "  4. Copy the 'Connection string-primary key'" -ForegroundColor White
Write-Host ""

Write-Host "Step 2: Create the Event Hub if it doesn't exist" -ForegroundColor Yellow
Write-Host "  1. In your Event Hubs Namespace, go to 'Event Hubs'" -ForegroundColor White
Write-Host "  2. Click '+ Event Hub'" -ForegroundColor White
Write-Host "  3. Name it: analytics-events" -ForegroundColor White
Write-Host "  4. Click 'Create'" -ForegroundColor White
Write-Host ""

Write-Host "Step 3: Store the connection string securely" -ForegroundColor Yellow
Write-Host "Once you have the connection string, run this command:" -ForegroundColor White
Write-Host ""
Write-Host "  dotnet user-secrets set `"EventHub:ConnectionString`" `"YOUR_CONNECTION_STRING_HERE`" --project $projectPath" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 4: Configure Microsoft Fabric Eventstream" -ForegroundColor Yellow
Write-Host "  1. Open Microsoft Fabric workspace with ProductOrderingLakehouse" -ForegroundColor White
Write-Host "  2. Create new Eventstream" -ForegroundColor White
Write-Host "  3. Configure Source:" -ForegroundColor White
Write-Host "     - Type: Azure Event Hubs" -ForegroundColor White
Write-Host "     - Namespace: evhns-product-ordering.servicebus.windows.net" -ForegroundColor White
Write-Host "     - Event Hub: analytics-events" -ForegroundColor White
Write-Host "     - Consumer group: `$Default (or create dedicated)" -ForegroundColor White
Write-Host "  4. Configure Destination:" -ForegroundColor White
Write-Host "     - Type: Lakehouse" -ForegroundColor White
Write-Host "     - Lakehouse: ProductOrderingLakehouse" -ForegroundColor White
Write-Host "     - Create tables: OrderEvents, PaymentEvents, ProductEvents, InventoryEvents" -ForegroundColor White
Write-Host ""

Write-Host "Example connection string format:" -ForegroundColor Yellow
Write-Host "Endpoint=sb://evhns-product-ordering.servicebus.windows.net/;SharedAccessKeyName=YourKeyName;SharedAccessKey=YourKeyValue" -ForegroundColor Gray
Write-Host ""

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Ready to continue!" -ForegroundColor Green
Write-Host "After setting the user secret, the AnalyticsService will automatically start streaming events to Event Hub." -ForegroundColor Green
Write-Host "If the connection string is not set, events will only be stored in PostgreSQL (graceful degradation)." -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
