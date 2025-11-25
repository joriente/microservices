# Start Java NotificationService
# This script starts the Java NotificationService in a separate terminal

param(
    [switch]$NewWindow
)

$ErrorActionPreference = "Stop"

# Refresh PATH to ensure Maven is available
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

$servicePath = Join-Path $PSScriptRoot "src\Services\NotificationService"

if ($NewWindow) {
    Write-Host "Starting Java NotificationService in new window..." -ForegroundColor Green
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$servicePath'; Write-Host 'Starting NotificationService...' -ForegroundColor Green; mvn spring-boot:run"
} else {
    Write-Host "Starting Java NotificationService..." -ForegroundColor Green
    Push-Location $servicePath
    try {
        mvn spring-boot:run
    } finally {
        Pop-Location
    }
}
