# Maven Installation Script for Windows
# This script downloads and installs Apache Maven

$ErrorActionPreference = "Stop"

$mavenVersion = "3.9.6"
$mavenDownloadUrl = "https://dlcdn.apache.org/maven/maven-3/$mavenVersion/binaries/apache-maven-$mavenVersion-bin.zip"
$installPath = "C:\Program Files\Maven"
$tempZip = "$env:TEMP\apache-maven-$mavenVersion-bin.zip"

Write-Host "Installing Apache Maven $mavenVersion..." -ForegroundColor Green

# Check if Java is installed
Write-Host "`nChecking Java installation..." -ForegroundColor Yellow
try {
    $javaVersion = java -version 2>&1 | Select-String "version"
    Write-Host "✓ Java is installed: $javaVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Java is not installed. Please install Java 21 first." -ForegroundColor Red
    exit 1
}

# Download Maven
Write-Host "`nDownloading Maven from $mavenDownloadUrl..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $mavenDownloadUrl -OutFile $tempZip -UseBasicParsing
    Write-Host "✓ Maven downloaded successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to download Maven: $_" -ForegroundColor Red
    exit 1
}

# Create installation directory
Write-Host "`nCreating installation directory at $installPath..." -ForegroundColor Yellow
if (Test-Path $installPath) {
    Write-Host "Installation directory already exists. Removing old version..." -ForegroundColor Yellow
    Remove-Item -Path $installPath -Recurse -Force
}
New-Item -ItemType Directory -Path $installPath -Force | Out-Null

# Extract Maven
Write-Host "`nExtracting Maven..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $tempZip -DestinationPath $installPath -Force
    
    # Move files from nested folder to root
    $extractedFolder = Get-ChildItem -Path $installPath -Directory | Select-Object -First 1
    Get-ChildItem -Path $extractedFolder.FullName | Move-Item -Destination $installPath -Force
    Remove-Item -Path $extractedFolder.FullName -Force
    
    Write-Host "✓ Maven extracted successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to extract Maven: $_" -ForegroundColor Red
    exit 1
}

# Clean up
Remove-Item -Path $tempZip -Force

# Add to PATH
Write-Host "`nAdding Maven to system PATH..." -ForegroundColor Yellow
$mavenBinPath = "$installPath\bin"

# Get current system PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")

if ($currentPath -notlike "*$mavenBinPath*") {
    try {
        # Add Maven to system PATH (requires admin privileges)
        $newPath = "$currentPath;$mavenBinPath"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
        
        # Also add to current session
        $env:Path = "$env:Path;$mavenBinPath"
        
        Write-Host "✓ Maven added to system PATH" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to add Maven to PATH. You may need to run this script as Administrator." -ForegroundColor Red
        Write-Host "  Alternatively, add this path manually: $mavenBinPath" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ Maven is already in PATH" -ForegroundColor Green
    $env:Path = "$env:Path;$mavenBinPath"
}

# Verify installation
Write-Host "`nVerifying Maven installation..." -ForegroundColor Yellow
try {
    # Refresh environment variables in current session
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    
    $mavenVersionOutput = & "$mavenBinPath\mvn.cmd" -version 2>&1
    Write-Host "✓ Maven installed successfully!" -ForegroundColor Green
    Write-Host "`n$mavenVersionOutput" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Maven installation verification failed: $_" -ForegroundColor Red
    Write-Host "Please close and reopen your terminal, then run: mvn -version" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Maven Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nIMPORTANT: Please close and reopen your terminal for PATH changes to take effect." -ForegroundColor Yellow
Write-Host "Then verify by running: mvn -version" -ForegroundColor Yellow
