$ErrorActionPreference = "Stop"

$adbString = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adbString)) {
    Write-Error "ADB not found. Make sure the Android SDK is installed."
    exit 1
}

Write-Host "============================================="
Write-Host " DIRECT INSTALL TO PHONE VIA WIFI (NO CABLE)"
Write-Host "============================================="
Write-Host ""
Write-Host "To do this without a cable:"
Write-Host " 1. Go to your phone Settings -> Developer Options."
Write-Host " 2. Enable 'Wireless Debugging'."
Write-Host " 3. Tap on 'Wireless Debugging' itself."
Write-Host " 4. Look for the IP address & Port (Example: 192.168.1.15:40531)"
Write-Host ""

$ipAdd = Read-Host "Type/Paste here the IP address and Port from your phone (192.168.x.x:xxxx)"

if (-not $ipAdd) {
    Write-Host "Cancelled. You need to enter the IP address to install the app on your phone." -ForegroundColor Red
    exit
}

# Auto-fix in case a dot (.) is used instead of a colon (:) before the port
if ($ipAdd -match "^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\.(\d{2,5})$") {
    $ipAdd = $ipAdd -replace "^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\.(\d{2,5})$", "`$1:`$2"
    Write-Host "Format fixed: Replaced dot (.) with colon (:) before the port -> $ipAdd" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Connecting to $ipAdd..."
& $adbString connect $ipAdd

$devicesInfo = & $adbString devices
if ($devicesInfo -match $ipAdd) {
    Write-Host "Connected successfully to your phone via Wi-Fi!" -ForegroundColor Green
    Write-Host "Now installing the app directly to your phone..." -ForegroundColor Cyan
    Write-Host ""
    
    # Direct install to phone
    dotnet build -f net10.0-android -t:Run

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "===========================================================" -ForegroundColor Green
        Write-Host " SUCCESS! The Companion app has been installed on your phone!" -ForegroundColor Green
        Write-Host " Check your phone screen, it should now be open." -ForegroundColor Green
        Write-Host "===========================================================" -ForegroundColor Green
    } else {
        Write-Host "Failed to install. Check the logs above." -ForegroundColor Red
    }
} else {
    Write-Host "Unable to connect. Please check if the IP and Port are correct, and make sure both devices are on the same Wi-Fi network." -ForegroundColor Red
}