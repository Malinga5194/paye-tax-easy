# ============================================================
# PAYE Tax Easy — Start All Services
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
Set-Location $root

# ── Kill any existing processes first ─────────────────────────
Write-Host "[0/6] Cleaning up old processes..." -ForegroundColor Yellow
Get-Process -Name "PayeTaxEasy.Api","dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
Start-Sleep -Seconds 2

# ── Step 1: Start Backend API ─────────────────────────────────
Write-Host "[1/6] Starting Backend API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root'; dotnet run --project src/PayeTaxEasy.Api --urls 'http://localhost:5050' --environment Development" -WindowStyle Normal

# Wait until API is actually responding
Write-Host "      Waiting for API to be ready..." -ForegroundColor Gray
$maxWait = 60
$waited = 0
while ($waited -lt $maxWait) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5050/health" -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "      API is ready!" -ForegroundColor Green
            break
        }
    } catch {
        Start-Sleep -Seconds 2
        $waited += 2
        Write-Host "      Still waiting... ($waited seconds)" -ForegroundColor Gray
    }
}
if ($waited -ge $maxWait) {
    Write-Host "      API took too long. It may still be starting. Continuing..." -ForegroundColor DarkYellow
}

# ── Step 2: Start Employer Portal ────────────────────────────
Write-Host "[2/6] Starting Employer Portal..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employer-portal'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 3

# ── Step 3: Start Employee Portal ────────────────────────────
Write-Host "[3/6] Starting Employee Portal..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employee-portal'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 2

# ── Step 4: Start IRD Dashboard ──────────────────────────────
Write-Host "[4/6] Starting IRD Dashboard..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\ird-dashboard'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 2

# ── Step 5: Start Admin Portal ───────────────────────────────
Write-Host "[5/6] Starting Admin Portal..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\admin-portal'; npm run dev" -WindowStyle Normal

# ── Step 6: Wait for frontend to be ready, then open browser ─
Write-Host "[6/6] Waiting for all services to be ready..." -ForegroundColor Yellow
Write-Host "      This may take 15-30 seconds on first run..." -ForegroundColor Gray

# Try multiple ports since Vite may pick a different one
$ports = @(5173, 5174, 5175, 5176, 5177, 5178)
$frontendUrl = ""
$waited = 0
while ($waited -lt 60) {
    foreach ($port in $ports) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$port" -TimeoutSec 1 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $frontendUrl = "http://localhost:$port"
                break
            }
        } catch { }
    }
    if ($frontendUrl -ne "") { break }
    Start-Sleep -Seconds 3
    $waited += 3
    Write-Host "      Still waiting... ($waited seconds)" -ForegroundColor Gray
}

if ($frontendUrl -ne "") {
    Start-Process $frontendUrl
    Write-Host "      Browser opened at $frontendUrl" -ForegroundColor Green
} else {
    Write-Host "      Services still starting. Open http://localhost:5173 manually in a few seconds." -ForegroundColor DarkYellow
}

# ── Done ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   All services started!                " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
if ($frontendUrl -ne "") {
    Write-Host "  Landing Page  : $frontendUrl"              -ForegroundColor White
} else {
    Write-Host "  Landing Page  : http://localhost:5173"      -ForegroundColor White
}
Write-Host "  Swagger API   : http://localhost:5050/swagger" -ForegroundColor White
Write-Host ""
Write-Host "  Login credentials:" -ForegroundColor White
Write-Host "  Employer  -> employer@test.com   / Test@1234"   -ForegroundColor Gray
Write-Host "  Employee  -> employee@test.com   / Test@1234"   -ForegroundColor Gray
Write-Host "  IRD       -> ird@test.com         / Test@1234"  -ForegroundColor Gray
Write-Host "  Admin     -> admin@payetaxeasy.lk / Admin@1234" -ForegroundColor Gray
Write-Host ""
Write-Host "  To stop: run .\stop.ps1 or close all terminal windows" -ForegroundColor DarkYellow
Write-Host ""
Write-Host "  Press any key to close this window." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
