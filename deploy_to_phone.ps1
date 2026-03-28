$ErrorActionPreference = "Stop"

Write-Host "============================================="
Write-Host " Building & Deploying to Connected Phone..."
Write-Host "============================================="
Write-Host ""
Write-Host "Tiyaking:"
Write-Host " 1. Naka-connect ang phone mo sa PC gamit ang USB cable."
Write-Host " 2. Naka-ON ang 'Developer Options' at 'USB Debugging' sa phone mo."
Write-Host " 3. In-allow/'OK' mo yung prompt sa phone screen na 'Allow USB debugging'."
Write-Host ""
Write-Host "Nagbi-build at nag-iinstall na ngayon... Please wait."
Write-Host ""

# This command compiles the app and immediately deploys and runs it on the connected Android device
dotnet build -f net10.0-android -t:Run

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host " SUCCESS! Naka-install at nabuksan na sa phone mo!" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Red
    Write-Host " ERROR: Hindi ma-install sa phone." -ForegroundColor Red
    Write-Host " Please check kung naka-plug nang maayos at" -ForegroundColor Red
    Write-Host " naka-allow ang USB debugging." -ForegroundColor Red
    Write-Host "=================================================" -ForegroundColor Red
}
