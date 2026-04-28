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

# ── Step 1: Apply database migrations ────────────────────────
Write-Host "[1/5] Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update --project src/PayeTaxEasy.Infrastructure --startup-project src/PayeTaxEasy.Api | Out-Null
Write-Host "      Database ready." -ForegroundColor Green

# ── Step 2: Start Backend API ─────────────────────────────────
Write-Host "[2/5] Starting Backend API on http://localhost:5050 ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root'; dotnet run --project src/PayeTaxEasy.Api --urls 'http://localhost:5050'" -WindowStyle Normal
Start-Sleep -Seconds 5
Write-Host "      API started." -ForegroundColor Green

# ── Step 3: Start Employer Portal ────────────────────────────
Write-Host "[3/5] Starting Employer Portal on http://localhost:5173 ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employer-portal'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 2

# ── Step 4: Start Employee Portal ────────────────────────────
Write-Host "[4/5] Starting Employee Portal on http://localhost:5174 ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\employee-portal'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 2

# ── Step 5: Start IRD Dashboard ──────────────────────────────
Write-Host "[5/6] Starting IRD Dashboard on http://localhost:5175 ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\ird-dashboard'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 2

# ── Step 6: Start Admin Portal ───────────────────────────────
Write-Host "[6/6] Starting Admin Portal on http://localhost:5176 ..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend\admin-portal'; npm run dev" -WindowStyle Normal
Start-Sleep -Seconds 3

# ── Done ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   All services started!                " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Backend API   : http://localhost:5050"         -ForegroundColor White
Write-Host "  Swagger UI    : http://localhost:5050/swagger" -ForegroundColor White
Write-Host "  Employer      : http://localhost:5173"         -ForegroundColor White
Write-Host "  Employee      : http://localhost:5174"         -ForegroundColor White
Write-Host "  IRD Dashboard : http://localhost:5175"         -ForegroundColor White
Write-Host "  Admin Portal  : http://localhost:5176"         -ForegroundColor White
Write-Host ""
Write-Host "  Login credentials:" -ForegroundColor White
Write-Host "  Employer  -> employer@test.com   / Test@1234"   -ForegroundColor Gray
Write-Host "  Employee  -> employee@test.com   / Test@1234"   -ForegroundColor Gray
Write-Host "  IRD       -> ird@test.com         / Test@1234"  -ForegroundColor Gray
Write-Host "  Admin     -> admin@payetaxeasy.lk / Admin@1234" -ForegroundColor Gray
Write-Host ""
Write-Host "  NOTE: If a port is in use, Vite will pick the next available one." -ForegroundColor DarkYellow
Write-Host "        Check the terminal window for the actual URL." -ForegroundColor DarkYellow
Write-Host ""

# Open browser tabs
Start-Sleep -Seconds 4
Start-Process "http://localhost:5050/swagger"
Start-Process "http://localhost:5173"
Start-Process "http://localhost:5174"
Start-Process "http://localhost:5175"
Start-Process "http://localhost:5176"

Write-Host "  Browser tabs opened. Press any key to exit this window." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
