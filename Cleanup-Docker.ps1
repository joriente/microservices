#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Cleans up all Docker containers and volumes for the Product Ordering System
.DESCRIPTION
    Stops and removes all containers, and optionally removes volumes to start fresh
#>

param(
    [switch]$RemoveVolumes,
    [switch]$Force
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Product Ordering System - Cleanup" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $Force) {
    Write-Host "This will stop and remove all Product Ordering containers." -ForegroundColor Yellow
    if ($RemoveVolumes) {
        Write-Host "⚠️  WARNING: This will also DELETE ALL DATA in volumes!" -ForegroundColor Red
    }
    Write-Host ""
    $confirm = Read-Host "Are you sure? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "Cleanup cancelled." -ForegroundColor Gray
        exit 0
    }
}

Write-Host ""
Write-Host "Stopping and removing containers..." -ForegroundColor Yellow

# Stop and remove docker-compose containers
if (Test-Path "deployment\docker\docker-compose.yml") {
    Push-Location "deployment\docker"
    try {
        Write-Host "Stopping docker-compose containers..." -ForegroundColor Gray
        docker-compose -p ProductOrdering down
        
        # Also try the old project name in case it exists
        docker-compose down 2>$null
    } finally {
        Pop-Location
    }
}

# Stop and remove individual containers with ProductOrdering prefix
Write-Host "Stopping ProductOrdering containers..." -ForegroundColor Gray
$containers = docker ps -a --filter "name=ProductOrdering" --format "{{.Names}}"
if ($containers) {
    $containers | ForEach-Object {
        Write-Host "  Removing: $_" -ForegroundColor Gray
        docker rm -f $_ 2>$null
    }
}

# Also check for old naming pattern
$oldContainers = docker ps -a --filter "name=product-ordering" --format "{{.Names}}"
if ($oldContainers) {
    Write-Host "Removing old containers..." -ForegroundColor Gray
    $oldContainers | ForEach-Object {
        Write-Host "  Removing: $_" -ForegroundColor Gray
        docker rm -f $_ 2>$null
    }
}

Write-Host "✓ Containers removed" -ForegroundColor Green

# Remove volumes if requested
if ($RemoveVolumes) {
    Write-Host ""
    Write-Host "Removing volumes..." -ForegroundColor Yellow
    
    # Remove docker-compose volumes
    Push-Location "deployment\docker"
    try {
        docker-compose -p ProductOrdering down -v 2>$null
        docker-compose down -v 2>$null
    } finally {
        Pop-Location
    }
    
    # Remove Aspire volumes
    $volumes = docker volume ls --filter "name=ProductOrdering" --format "{{.Name}}"
    if ($volumes) {
        $volumes | ForEach-Object {
            Write-Host "  Removing volume: $_" -ForegroundColor Gray
            docker volume rm $_ 2>$null
        }
    }
    
    Write-Host "✓ Volumes removed" -ForegroundColor Green
}

# Remove unused networks
Write-Host ""
Write-Host "Cleaning up networks..." -ForegroundColor Yellow
docker network prune -f 2>$null | Out-Null
Write-Host "✓ Networks cleaned" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($RemoveVolumes) {
    Write-Host "⚠️  All data has been removed. The system will start fresh." -ForegroundColor Yellow
} else {
    Write-Host "Data volumes were preserved." -ForegroundColor Green
    Write-Host "To remove volumes and start fresh, run:" -ForegroundColor Gray
    Write-Host "  .\Cleanup-Docker.ps1 -RemoveVolumes" -ForegroundColor White
}

Write-Host ""
Write-Host "To start the system again, run:" -ForegroundColor Gray
Write-Host "  .\Start-all.ps1" -ForegroundColor White
Write-Host ""
