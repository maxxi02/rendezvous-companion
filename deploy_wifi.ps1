$ErrorActionPreference = "Stop"

$adbString = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adbString)) {
    Write-Error "Hindi mahanap ang ADB. Siguraduhing may nakainstall na Android SDK."
    exit 1
}

Write-Host "============================================="
Write-Host " DIRECT INSTALL TO PHONE VIA WIFI (REKTA DL)"
Write-Host "============================================="
Write-Host ""
Write-Host "Para magawa ito nang walang cable:"
Write-Host " 1. Pumunta sa Settings ng Phone -> Developer Options."
Write-Host " 2. Buksan ang 'Wireless Debugging'."
Write-Host " 3. I-tap ang mismong salitang 'Wireless Debugging'."
Write-Host " 4. Hahanapin mo doon ang IP address & Port (Halimbawa: 192.168.1.15:40531)"
Write-Host ""

$ipAdd = Read-Host "I-type/I-paste dito ang IP address at Port mula sa phone mo (192.168.x.x:xxxx)"

if (-not $ipAdd) {
    Write-Host "Kinancel. Kailangan mo ilagay yung IP address para ma-install sa phone mo." -ForegroundColor Red
    exit
}

# Auto-fix kung sakaling dot (.) ang nailagay imbis na colon (:) bago ang port
if ($ipAdd -match "^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\.(\d{2,5})$") {
    $ipAdd = $ipAdd -replace "^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\.(\d{2,5})$", "`$1:`$2"
    Write-Host "Inayos ang format: Ginawang colon (:) ang dot bago ang port -> $ipAdd" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Kumokonekta sa $ipAdd..."
& $adbString connect $ipAdd

$devicesInfo = & $adbString devices
if ($devicesInfo -match $ipAdd) {
    Write-Host "Connected successfully sa phone via Wi-Fi!" -ForegroundColor Green
    Write-Host "Ngayon, i-iinstall na ang app sa phone mo nang direkta (Rekta DL)..." -ForegroundColor Cyan
    Write-Host ""
    
    # Rekta install sa phone
    dotnet build -f net10.0-android -t:Run

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "===========================================================" -ForegroundColor Green
        Write-Host " SUCCESS! Tapos na malagay ang Companion app sa phone mo!" -ForegroundColor Green
        Write-Host " Check mo na ang phone screen mo, dapat nakabukas na ito." -ForegroundColor Green
        Write-Host "===========================================================" -ForegroundColor Green
    } else {
        Write-Host "Failed mag-install. Tignan ang logs sa taas." -ForegroundColor Red
    }
} else {
    Write-Host "Hindi maka-connect. Paki-check kung tama ang IP at Port, at kung magka-parehas kayo ng WiFi." -ForegroundColor Red
}
