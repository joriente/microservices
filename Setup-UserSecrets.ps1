#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up user secrets for API keys across all services
.DESCRIPTION
    This script initializes user secrets for services that need API keys (Stripe, SendGrid, etc.)
    and prompts developers to enter their own keys.
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "User Secrets Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Payment Service - Stripe Keys
Write-Host "Setting up Payment Service secrets..." -ForegroundColor Yellow
$paymentServicePath = "src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI"

Push-Location $paymentServicePath

# Initialize user secrets if not already done
dotnet user-secrets init

Write-Host ""
Write-Host "Please enter your Stripe API keys:" -ForegroundColor Green
Write-Host "(You can get test keys from https://dashboard.stripe.com/test/apikeys)" -ForegroundColor Gray
Write-Host ""

$stripePublishable = Read-Host "Stripe Publishable Key (pk_test_...)"
$stripeSecret = Read-Host "Stripe Secret Key (sk_test_...)"

if ($stripePublishable) {
    dotnet user-secrets set "Stripe:PublishableKey" $stripePublishable
    Write-Host "✓ Stripe Publishable Key set" -ForegroundColor Green
}

if ($stripeSecret) {
    dotnet user-secrets set "Stripe:SecretKey" $stripeSecret
    Write-Host "✓ Stripe Secret Key set" -ForegroundColor Green
}

Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "User secrets configured successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: User secrets are stored securely on your machine at:" -ForegroundColor Yellow
Write-Host "  Windows: %APPDATA%\Microsoft\UserSecrets" -ForegroundColor Gray
Write-Host "  macOS/Linux: ~/.microsoft/usersecrets" -ForegroundColor Gray
Write-Host ""
Write-Host "To view your secrets later, run:" -ForegroundColor Yellow
Write-Host "  cd $paymentServicePath" -ForegroundColor Gray
Write-Host "  dotnet user-secrets list" -ForegroundColor Gray
Write-Host ""
