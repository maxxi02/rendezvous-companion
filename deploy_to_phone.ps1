$ErrorActionPreference = "Stop"

Write-Host "============================================="
Write-Host " Building & Deploying to Connected Phone..."
Write-Host "============================================="
Write-Host ""
Write-Host "Make sure:"
Write-Host " 1. Your phone is connected to your PC using a USB cable."
Write-Host " 2. 'Developer Options' and 'USB Debugging' are enabled on your phone."
Write-Host " 3. You have allowed/'OK'ed the 'Allow USB debugging' prompt on your phone screen."
Write-Host ""
Write-Host "Building and installing now... Please wait."
Write-Host ""

# This command compiles the app and immediately deploys and runs it on the connected Android device
dotnet build -f net10.0-android -t:Run

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host " SUCCESS! The app has been installed and launched on your phone!" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Red
    Write-Host " ERROR: Failed to install on the phone." -ForegroundColor Red
    Write-Host " Please check if the phone is properly connected and" -ForegroundColor Red
    Write-Host " USB debugging is allowed." -ForegroundColor Red
    Write-Host "=================================================" -ForegroundColor Red
}