#!/usr/bin/env pwsh
# Start NotificationService with SendGrid enabled
# Usage: .\start-with-sendgrid.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting NotificationService with SendGrid" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if API key is provided
if (-not $env:SENDGRID_API_KEY) {
    Write-Host "⚠️  SENDGRID_API_KEY environment variable not set!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To get your SendGrid API key:" -ForegroundColor Green
    Write-Host "1. Go to: https://app.sendgrid.com/settings/api_keys" -ForegroundColor White
    Write-Host "2. Click 'Create API Key'" -ForegroundColor White
    Write-Host "3. Give it a name (e.g., 'ProductOrderingSystem')" -ForegroundColor White
    Write-Host "4. Choose 'Full Access' or 'Restricted Access' with Mail Send permissions" -ForegroundColor White
    Write-Host "5. Copy the API key (you won't see it again!)" -ForegroundColor White
    Write-Host ""
    Write-Host "Then set the environment variables:" -ForegroundColor Green
    Write-Host "`$env:SENDGRID_API_KEY = 'SG.your-api-key-here'" -ForegroundColor Cyan
    Write-Host "`$env:SENDGRID_FROM_EMAIL = 'your-verified-email@domain.com'" -ForegroundColor Cyan
    Write-Host "`$env:SENDGRID_ENABLED = 'true'" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Then run this script again." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

if (-not $env:SENDGRID_FROM_EMAIL) {
    Write-Host "⚠️  SENDGRID_FROM_EMAIL environment variable not set!" -ForegroundColor Yellow
    Write-Host "Set it to a verified sender email address." -ForegroundColor White
    Write-Host ""
    Write-Host "`$env:SENDGRID_FROM_EMAIL = 'your-verified-email@domain.com'" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

# Enable SendGrid
$env:SENDGRID_ENABLED = "true"

Write-Host "✓ SendGrid API Key: " -ForegroundColor Green -NoNewline
Write-Host $env:SENDGRID_API_KEY.Substring(0, [Math]::Min(15, $env:SENDGRID_API_KEY.Length)) -ForegroundColor White -NoNewline
Write-Host "..." -ForegroundColor White

Write-Host "✓ From Email: " -ForegroundColor Green -NoNewline
Write-Host $env:SENDGRID_FROM_EMAIL -ForegroundColor White

Write-Host "✓ SendGrid Enabled: " -ForegroundColor Green -NoNewline
Write-Host "true" -ForegroundColor White

Write-Host "`nStarting NotificationService...`n" -ForegroundColor Yellow

# Start the service
java -jar target/notification-service-1.0.0.jar
