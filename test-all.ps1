# Test All Services Script
# This script runs all integration tests for the Product Ordering System

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Product Ordering System" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test tracking
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$failedProjects = @()

# Define test projects
$testProjects = @(
    @{
        Name = "Product Service"
        Path = "tests\ProductService\ProductOrderingSystem.ProductService.IntegrationTests\ProductOrderingSystem.ProductService.IntegrationTests.csproj"
    },
    @{
        Name = "Inventory Service"
        Path = "tests\InventoryService\ProductOrderingSystem.InventoryService.IntegrationTests\ProductOrderingSystem.InventoryService.IntegrationTests.csproj"
    },
    @{
        Name = "Customer Service"
        Path = "tests\CustomerService\ProductOrderingSystem.CustomerService.IntegrationTests\ProductOrderingSystem.CustomerService.IntegrationTests.csproj"
    },
    @{
        Name = "Order Service"
        Path = "tests\OrderService\ProductOrderingSystem.OrderService.IntegrationTests\ProductOrderingSystem.OrderService.IntegrationTests.csproj"
    },
    @{
        Name = "Cart Service"
        Path = "tests\CartService\ProductOrderingSystem.CartService.IntegrationTests\ProductOrderingSystem.CartService.IntegrationTests.csproj"
    },
    @{
        Name = "Payment Service"
        Path = "tests\PaymentService\ProductOrderingSystem.PaymentService.IntegrationTests\ProductOrderingSystem.PaymentService.IntegrationTests.csproj"
    },
    @{
        Name = "Identity Service"
        Path = "tests\IdentityService\ProductOrderingSystem.IdentityService.IntegrationTests\ProductOrderingSystem.IdentityService.IntegrationTests.csproj"
    }
)

Push-Location $PSScriptRoot

try {
    foreach ($project in $testProjects) {
        $projectPath = Join-Path $PSScriptRoot $project.Path
        
        if (-not (Test-Path $projectPath)) {
            Write-Host "⚠ Skipping $($project.Name) - Project not found" -ForegroundColor Yellow
            Write-Host "  Path: $projectPath" -ForegroundColor Gray
            Write-Host ""
            continue
        }

        Write-Host "Testing $($project.Name)..." -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        
        # Run tests with detailed output
        $output = dotnet test $projectPath --logger "console;verbosity=minimal" --no-build 2>&1
        $exitCode = $LASTEXITCODE
        
        # Parse test results from output
        $resultLine = $output | Where-Object { $_ -match "Passed:\s*(\d+)" } | Select-Object -Last 1
        
        if ($resultLine -match "Failed:\s*(\d+).*Passed:\s*(\d+).*Skipped:\s*(\d+).*Total:\s*(\d+)") {
            $failed = [int]$matches[1]
            $passed = [int]$matches[2]
            $skipped = [int]$matches[3]
            $total = [int]$matches[4]
            
            $totalTests += $total
            $passedTests += $passed
            $failedTests += $failed
            $skippedTests += $skipped
            
            if ($failed -gt 0 -or $exitCode -ne 0) {
                Write-Host "✗ $($project.Name): $failed failed, $passed passed, $skipped skipped (Total: $total)" -ForegroundColor Red
                $failedProjects += $project.Name
            } else {
                Write-Host "✓ $($project.Name): All $passed tests passed" -ForegroundColor Green
            }
        } elseif ($exitCode -eq 0) {
            Write-Host "✓ $($project.Name): Tests passed" -ForegroundColor Green
        } else {
            Write-Host "✗ $($project.Name): Tests failed or could not run" -ForegroundColor Red
            $failedProjects += $project.Name
            
            # Show some error output
            $errorLines = $output | Select-Object -Last 10
            foreach ($line in $errorLines) {
                Write-Host "  $line" -ForegroundColor Gray
            }
        }
        
        Write-Host ""
    }

    # Summary
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Test Results Summary" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Total Tests:   $totalTests" -ForegroundColor White
    Write-Host "Passed:        $passedTests" -ForegroundColor Green
    Write-Host "Failed:        $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    Write-Host "Skipped:       $skippedTests" -ForegroundColor Yellow
    Write-Host ""

    if ($failedProjects.Count -gt 0) {
        Write-Host "Failed Projects:" -ForegroundColor Red
        foreach ($project in $failedProjects) {
            Write-Host "  • $project" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "Tests FAILED!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Cyan
        exit 1
    } else {
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "All Tests PASSED!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Cyan
    }
    
} finally {
    Pop-Location
}
