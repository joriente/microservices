# Script to switch from Azure Service Bus to RabbitMQ across all services

Write-Host "Switching from Azure Service Bus to RabbitMQ..." -ForegroundColor Cyan

# Services to update
$services = @(
    "src\Services\ProductService\ProductOrderingSystem.ProductService.WebAPI",
    "src\Services\OrderService\ProductOrderingSystem.OrderService.WebAPI",
    "src\Services\CartService\ProductOrderingSystem.CartService.WebAPI",
    "src\Services\PaymentService\ProductOrderingSystem.PaymentService.WebAPI",
    "src\Services\CustomerService\ProductOrderingSystem.CustomerService.WebAPI",
    "src\Services\InventoryService"
)

foreach ($service in $services) {
    $csprojPath = Get-ChildItem -Path $service -Filter "*.csproj" | Select-Object -First 1
    
    if ($csprojPath) {
        Write-Host "Updating $($csprojPath.Name)..." -ForegroundColor Yellow
        
        # Read csproj content
        $content = Get-Content $csprojPath.FullName -Raw
        
        # Replace Azure Service Bus with RabbitMQ
        $content = $content -replace 'MassTransit\.Azure\.ServiceBus\.Core', 'MassTransit.RabbitMQ'
        
        # Write back
        Set-Content -Path $csprojPath.FullName -Value $content -NoNewline
        
        Write-Host "  ✓ Updated project file" -ForegroundColor Green
    }
    
    # Update Program.cs
    $programPath = Join-Path $service "Program.cs"
    
    if (Test-Path $programPath) {
        Write-Host "  Updating Program.cs..." -ForegroundColor Yellow
        
        # Read Program.cs content
        $content = Get-Content $programPath -Raw
        
        # Replace UsingAzureServiceBus configuration with UsingRabbitMq
        $azureServiceBusPattern = '(?s)x\.UsingAzureServiceBus\(\(context, cfg\) =>\s*\{.*?cfg\.ConfigureEndpoints\(context\);'
        
        $rabbitMqReplacement = @'
x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        var uri = new Uri(connectionString ?? "amqp://localhost:5672");
        
        cfg.Host(uri, h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
'@
        
        $content = $content -replace $azureServiceBusPattern, $rabbitMqReplacement
        
        # Write back
        Set-Content -Path $programPath -Value $content -NoNewline
        
        Write-Host "  ✓ Updated Program.cs" -ForegroundColor Green
    }
}

Write-Host "`n✓ All services updated to use RabbitMQ!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Run: dotnet restore" -ForegroundColor White
Write-Host "2. Run: dotnet build" -ForegroundColor White
Write-Host "3. Restart Aspire AppHost" -ForegroundColor White
Write-Host "`nRabbitMQ Management UI will be available at: http://localhost:15672" -ForegroundColor Yellow
Write-Host "Username: guest, Password: guest" -ForegroundColor Yellow
