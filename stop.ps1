# ============================================================
# PAYE Tax Easy — Stop All Services
# ============================================================

Write-Host ""
Write-Host "Stopping PAYE Tax Easy services..." -ForegroundColor Yellow

# Kill the .NET API
Get-Process -Name "PayeTaxEasy.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "*PayeTaxEasy*" } | Stop-Process -Force

# Kill Node/Vite processes on the frontend ports
$ports = @(5173, 5174, 5175, 5176, 5177, 5178)
foreach ($port in $ports) {
    $pid = (netstat -ano | Select-String ":$port " | Select-Object -First 1) -replace '.*\s+(\d+)$', '$1'
    if ($pid -match '^\d+$') {
        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "All services stopped." -ForegroundColor Green
Write-Host ""
