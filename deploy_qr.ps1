$ErrorActionPreference = "Stop"

Write-Host "==========================="
Write-Host " SCAN TO DOWNLOAD APK "
Write-Host "==========================="
Write-Host "Starting local web server..."

$ApkDir = "bin\Release\net10.0-android\publish"
if (-not (Test-Path $ApkDir)) {
    $ApkDir = "bin\Release\net10.0-android\android-arm64\publish"
    if (-not (Test-Path $ApkDir)) {
        Write-Error "Built APK not found. Please run dotnet publish first."
        exit 1
    }
}

# Stop old background jobs
Get-Job -Name *serve* -ErrorAction SilentlyContinue | Stop-Job -PassThru | Remove-Job

$IP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch '(Loopback|Pseudo)' } | Select-Object -First 1).IPAddress
$LocalUrl = "http://${IP}:8080"

# Start serving the folder in the background
$Job = Start-Job -Name "serve" -ScriptBlock {
    Set-Location $using:ApkDir
    npx serve -l 8080 -cors
}

Write-Host ""
Write-Host "📱 SCAN THIS QR CODE USING YOUR PHONE CAMERA 📱" -ForegroundColor Yellow
Write-Host "URL: $LocalUrl" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Green

# Use npx qrcode-terminal to print the QR code
npx qrcode-terminal $LocalUrl

Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "AFTER DOWNLOADING, DON'T FORGET: Press Ctrl+C to stop the server."
Write-Host ""

# Keep script running so user can see QR code
while ($true) {
    Start-Sleep -Seconds 1
}