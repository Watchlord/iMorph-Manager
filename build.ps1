# Build script for IMorph Mega.nz File Downloader
# This script compiles the project and outputs to the bin folder

Write-Host "IMorph Mega.nz File Downloader - Build Script" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: .NET SDK not found. Please install .NET 8.0 SDK or later." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Building project (Release configuration)..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output location: bin\Release\net8.0-windows\" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "  cd bin\Release\net6.0-windows" -ForegroundColor White
Write-Host "  .\IMorphMegaDownloader.exe" -ForegroundColor White
Write-Host ""
