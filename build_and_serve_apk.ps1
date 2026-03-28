$ErrorActionPreference = "Stop"

Write-Host "=============================="
Write-Host " Building Rendezvous Companion"
Write-Host "=============================="
Write-Host "Compiling the Android APK... This may take a minute."

# Build the APK
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Please check the errors above."
    exit $LASTEXITCODE
}

# Find the output directory
$ApkDir = "bin\Release\net10.0-android\publish"
if (-not (Test-Path $ApkDir)) {
    # Sometimes it goes to a slightly different path depending on runtime identifiers
    $ApkDir = "bin\Release\net10.0-android\android-arm64\publish"
    if (-not (Test-Path $ApkDir)) {
        Write-Error "Could not find the publish directory containing the APK."
        exit 1
    }
}

# Get the local IP address
$IP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch '(Loopback|Pseudo)' } | Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "==========================="
Write-Host " APK Built Successfully!"
Write-Host "==========================="
Write-Host ""
# Start local web server in the background
$Jobs = Get-Job -State Running -ErrorAction SilentlyContinue
foreach ($job in $Jobs) { Stop-Job $job; Remove-Job $job }

Start-Job -ScriptBlock {
    Set-Location $using:ApkDir
    if (Get-Command "npx" -ErrorAction SilentlyContinue) {
        npx serve -l 8080 -cors
    } else {
        python -m http.server 8080
    }
} | Out-Null

Write-Host "Local server opened on port 8080. Gumagawa ng pampublikong / online link para madaling ma-download (Localtunnel)..."
Write-Host "Wait lang po..." -ForegroundColor Cyan

# Use locatunnel to expose port 8080
npx localtunnel --port 8080

Write-Host ""
Write-Host "Pagkatapos mag-download, i-press ang Ctrl+C para i-stop."
