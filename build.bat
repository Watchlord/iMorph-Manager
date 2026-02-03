@echo off
REM Build script for IMorph Mega.nz File Downloader
REM This script compiles the project and outputs to the bin folder

echo IMorph Mega.nz File Downloader - Build Script
echo ==============================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET SDK not found. Please install .NET 8.0 SDK or later.
    exit /b 1
)

echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Error: Failed to restore packages.
    exit /b 1
)

echo.
echo Building project (Release configuration)...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Error: Build failed.
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo Output location: bin\Release\net8.0-windows\
echo.
echo To run the application:
echo   cd bin\Release\net6.0-windows
echo   IMorphMegaDownloader.exe
echo.

pause
