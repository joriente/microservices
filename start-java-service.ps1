# Start Java NotificationService
# This script starts the Java NotificationService in a separate terminal

param(
    [switch]$NewWindow
)

$ErrorActionPreference = "Stop"

# Refresh PATH to ensure Maven is available
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

$servicePath = Join-Path $PSScriptRoot "src\Services\NotificationService"

# Detect RabbitMQ port from Aspire-managed container
Write-Host "Detecting RabbitMQ configuration..." -ForegroundColor Yellow
$rabbitmqHost = "localhost"
$rabbitmqPort = "5672"
$rabbitmqUsername = "guest"
$rabbitmqPassword = "guest"

# Try to get the port from Aspire's RabbitMQ container
$rabbitmqContainer = docker ps --filter "name=ProductOrdering-rabbitmq" --format "{{.Names}}" 2>$null
if ($rabbitmqContainer) {
    $portMapping = docker port ProductOrdering-rabbitmq 5672 2>$null
    if ($portMapping -match ':(\d+)$') {
        $rabbitmqPort = $matches[1]
        Write-Host "✓ Found Aspire RabbitMQ on port $rabbitmqPort" -ForegroundColor Green
    }
    
    # Get the actual credentials from the container
    $envVars = docker inspect ProductOrdering-rabbitmq --format '{{json .Config.Env}}' 2>$null | ConvertFrom-Json
    foreach ($env in $envVars) {
        if ($env -match 'RABBITMQ_DEFAULT_USER=(.+)') {
            $rabbitmqUsername = $matches[1]
        }
        if ($env -match 'RABBITMQ_DEFAULT_PASS=(.+)') {
            $rabbitmqPassword = $matches[1]
        }
    }
    Write-Host "✓ Retrieved RabbitMQ credentials (user: $rabbitmqUsername)" -ForegroundColor Green
} else {
    Write-Host "⚠ RabbitMQ container not found. Using default port 5672" -ForegroundColor Yellow
    Write-Host "  Make sure to start Aspire infrastructure first!" -ForegroundColor Yellow
}

# Detect MongoDB port from Aspire-managed container
Write-Host "Detecting MongoDB configuration..." -ForegroundColor Yellow
$mongodbHost = "localhost"
$mongodbPort = "27017"

# Try to get the port from Aspire's MongoDB container
$mongodbContainer = docker ps --filter "name=ProductOrdering-mongodb" --filter "name=-legacy" --format "{{.Names}}" 2>$null | Where-Object { $_ -notlike "*-legacy" }
if ($mongodbContainer) {
    $portMapping = docker port ProductOrdering-mongodb 27017 2>$null
    if ($portMapping -match ':(\d+)$') {
        $mongodbPort = $matches[1]
        Write-Host "✓ Found Aspire MongoDB on port $mongodbPort" -ForegroundColor Green
    }
} else {
    Write-Host "⚠ MongoDB container not found. Using default port 27017" -ForegroundColor Yellow
}

# Set environment variables for RabbitMQ connection
$env:RABBITMQ_HOST = $rabbitmqHost
$env:RABBITMQ_PORT = $rabbitmqPort
$env:RABBITMQ_USERNAME = $rabbitmqUsername
$env:RABBITMQ_PASSWORD = $rabbitmqPassword

# Set environment variable for MongoDB connection
# Use the fixed port and credentials
$env:MONGODB_URI = "mongodb://admin:admin123@localhost:27017/notificationdb?authSource=admin"

if ($NewWindow) {
    Write-Host "Starting Java NotificationService in new window..." -ForegroundColor Green
    # Set environment variables that Spring Boot will read
    $command = @"
cd '$servicePath'
`$env:RABBITMQ_HOST='$rabbitmqHost'
`$env:RABBITMQ_PORT='$rabbitmqPort'
`$env:RABBITMQ_USERNAME='$rabbitmqUsername'
`$env:RABBITMQ_PASSWORD='$rabbitmqPassword'
`$env:MONGODB_URI='$($env:MONGODB_URI)'
Write-Host 'Starting NotificationService...' -ForegroundColor Green
Write-Host '  RabbitMQ: $rabbitmqHost`:$rabbitmqPort (user: $rabbitmqUsername)' -ForegroundColor Gray
Write-Host '  MongoDB: $($env:MONGODB_URI)' -ForegroundColor Gray
mvn spring-boot:run
"@
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", $command
} else {
    Write-Host "Starting Java NotificationService..." -ForegroundColor Green
    Write-Host "  RabbitMQ: ${rabbitmqHost}:${rabbitmqPort} (user: $rabbitmqUsername)" -ForegroundColor Gray
    Write-Host "  MongoDB: $($env:MONGODB_URI)" -ForegroundColor Gray
    Push-Location $servicePath
    try {
        mvn spring-boot:run
    } finally {
        Pop-Location
    }
}
