# Debug RabbitMQ Bindings for ProductCreatedEvent
$baseUrl = "http://localhost:15672/api"
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))
$headers = @{ Authorization = "Basic $credentials" }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "RabbitMQ Exchange and Queue Bindings Debug" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check all exchanges with ProductCreated in the name
Write-Host "Exchanges containing 'ProductCreated':" -ForegroundColor Yellow
$exchanges = Invoke-RestMethod -Uri "$baseUrl/exchanges/%2F" -Headers $headers
$productCreatedExchanges = $exchanges | Where-Object { $_.name -like "*ProductCreated*" }
foreach ($ex in $productCreatedExchanges) {
    Write-Host "  - Name: $($ex.name)" -ForegroundColor White
    Write-Host "    Type: $($ex.type)" -ForegroundColor Gray
    Write-Host "    Message Stats In: $($ex.message_stats.publish_in)" -ForegroundColor Gray
    Write-Host "    Message Stats Out: $($ex.message_stats.publish_out)" -ForegroundColor Gray
}

# Check ProductCreatedEvent queue
Write-Host "`nQueue 'ProductCreatedEvent':" -ForegroundColor Yellow
try {
    $queue = Invoke-RestMethod -Uri "$baseUrl/queues/%2F/ProductCreatedEvent" -Headers $headers
    Write-Host "  Messages Ready: $($queue.messages_ready)" -ForegroundColor White
    Write-Host "  Messages Unacknowledged: $($queue.messages_unacknowledged)" -ForegroundColor White
    Write-Host "  Consumers: $($queue.consumers)" -ForegroundColor White
    Write-Host "  Message Stats Deliver: $($queue.message_stats.deliver)" -ForegroundColor Gray
    Write-Host "  Message Stats Publish: $($queue.message_stats.publish)" -ForegroundColor Gray
} catch {
    Write-Host "  Queue not found or error: $_" -ForegroundColor Red
}

# Check bindings for ProductCreatedEvent queue
Write-Host "`nBindings for Queue 'ProductCreatedEvent':" -ForegroundColor Yellow
try {
    $bindings = Invoke-RestMethod -Uri "$baseUrl/queues/%2F/ProductCreatedEvent/bindings" -Headers $headers
    foreach ($binding in $bindings) {
        Write-Host "  - Source Exchange: '$($binding.source)'" -ForegroundColor White
        Write-Host "    Routing Key: '$($binding.routing_key)'" -ForegroundColor Gray
        Write-Host "    Destination: $($binding.destination)" -ForegroundColor Gray
    }
} catch {
    Write-Host "  No bindings found or error: $_" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
