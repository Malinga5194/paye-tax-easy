# ============================================================
# PAYE Tax Easy — Start All Services (Optimized)
# Run this script from the project root folder:
#   Right-click start.ps1 → Run with PowerShell
# ============================================================

# Allow script execution
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force 2>$null

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   PAYE Tax Easy — Starting System      " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $root -or $root -eq "") { $root = $PSScriptRoot }
if (-not $root -or $root -eq "") { $root = Get-Location }
Set-Location $root
Write-Host "  Project root: $root" -ForegroundColor Gray

# ── Kill any existing processes first ─────────────────────────
Write-Host "[0/4] Cleaning up old processes..." -ForegroundColor Yellow
Get-Process -Name "PayeTaxEasy.Api","dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
Start-Sleep -Seconds 1

# ── Step 1: Build backend once (so dotnet run skips compilation) ──
Write-Host "[1/4] Building backend..." -ForegroundColor Yellow
$buildResult = & dotnet build src/PayeTaxEasy.Api/PayeTaxEasy.Api.csproj -c Debug --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build failed! Check errors above." -ForegroundColor Red
    Write-Host $buildResult
    pause
    exit 1
}
Write-Host "      Build complete." -ForegroundColor Green

# ── Step 2: Start Backend API (--no-build since we just built) ────
Write-Host "[2/4] Starting Backend API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root'; dotnet run --project src/PayeTaxEasy.Api --urls 'http://localhost:5050' --environment Development --no-build" -WindowStyle Normal

# ── Step 3: Start ALL frontends in parallel (no waiting between) ──
Write-Host "[3/4] Starting all frontends in parallel..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employer-portal'; npm run dev" -WindowStyle Normal
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employee-portal'; npm run dev" -WindowStyle Normal
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\ird-dashboard'; npm run dev" -WindowStyle Normal
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\admin-portal'; npm run dev" -WindowStyle Normal

# ── Step 4: Wait for API health check, then open browser ──────────
Write-Host "[4/4] Waiting for services..." -ForegroundColor Yellow
$maxWait = 45
$waited = 0
$apiReady = $false
while ($waited -lt $maxWait) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5050/health" -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            Write-Host "      API ready! ($waited seconds)" -ForegroundColor Green
            break
        }
    } catch {
        Start-Sleep -Seconds 2
        $waited += 2
    }
}
if (-not $apiReady) {
    Write-Host "      API still starting. It should be ready shortly." -ForegroundColor DarkYellow
}

# Give frontends a moment to bind their ports
Start-Sleep -Seconds 3
Start-Process "http://localhost:5173"
Write-Host "      Browser opened." -ForegroundColor Green

# ── Done ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   All services started!                " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Landing Page  : http://localhost:5173"          -ForegroundColor White
Write-Host "  Employee      : http://localhost:5174"          -ForegroundColor White
Write-Host "  IRD Dashboard : http://localhost:5175"          -ForegroundColor White
Write-Host "  Admin Portal  : http://localhost:5176"          -ForegroundColor White
Write-Host "  Swagger API   : http://localhost:5050/swagger"  -ForegroundColor White
Write-Host ""
Write-Host "  Login credentials:" -ForegroundColor White
Write-Host "  Employer  -> employer@test.com    / Test@1234"  -ForegroundColor Gray
Write-Host "  Employee  -> amal.perera@test.com / Test@1234"  -ForegroundColor Gray
Write-Host "  IRD       -> ird@test.com         / Test@1234"  -ForegroundColor Gray
Write-Host "  Admin     -> admin@payetaxeasy.lk / Admin@1234" -ForegroundColor Gray
Write-Host ""
Write-Host "  To stop: run .\stop.ps1 or close all terminal windows" -ForegroundColor DarkYellow
Write-Host ""
Write-Host "  Press any key to close this window." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
