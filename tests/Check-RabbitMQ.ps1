# Check RabbitMQ Exchanges and Queues
# Queries the RabbitMQ Management API to see exchanges and queues

$ErrorActionPreference = "Stop"
$rabbitMqUrl = "http://localhost:15672/api"
$username = "guest"
$password = "guest"

# Create base64 encoded credentials
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
$headers = @{
    Authorization = "Basic $base64AuthInfo"
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "RabbitMQ Exchanges and Queues" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

try {
    # Get all exchanges
    Write-Host "Exchanges:" -ForegroundColor Yellow
    $exchanges = Invoke-RestMethod -Uri "$rabbitMqUrl/exchanges" -Headers $headers -Method Get
    $exchanges | Where-Object { $_.name -like "*Product*" -or $_.name -like "*Order*" } | ForEach-Object {
        Write-Host "  - $($_.name)" -ForegroundColor White
        Write-Host "    Type: $($_.type)" -ForegroundColor Gray
    }

    Write-Host "`nQueues:" -ForegroundColor Yellow
    $queues = Invoke-RestMethod -Uri "$rabbitMqUrl/queues" -Headers $headers -Method Get
    $queues | Where-Object { $_.name -like "*Product*" -or $_.name -like "*Order*" } | ForEach-Object {
        Write-Host "  - $($_.name)" -ForegroundColor White
        Write-Host "    Messages: $($_.messages)" -ForegroundColor Gray
        Write-Host "    Consumers: $($_.consumers)" -ForegroundColor Gray
    }

    Write-Host "`nBindings:" -ForegroundColor Yellow
    $bindings = Invoke-RestMethod -Uri "$rabbitMqUrl/bindings" -Headers $headers -Method Get
    $bindings | Where-Object { $_.source -like "*Product*" -or $_.destination -like "*Product*" } | ForEach-Object {
        Write-Host "  Exchange: $($_.source) -> Queue: $($_.destination)" -ForegroundColor White
    }

} catch {
    Write-Host "✗ Error connecting to RabbitMQ Management API" -ForegroundColor Red
    Write-Host "  Make sure RabbitMQ is running and management plugin is enabled" -ForegroundColor Yellow
    Write-Host "  URL: $rabbitMqUrl" -ForegroundColor Gray
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
