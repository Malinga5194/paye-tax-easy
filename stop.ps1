# ============================================================
# PAYE Tax Easy — Stop All Services
# ============================================================

Write-Host ""
Write-Host "Stopping PAYE Tax Easy services..." -ForegroundColor Yellow

# Kill the .NET API process
Get-Process -Name "PayeTaxEasy.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# Kill Node/Vite processes on frontend ports
$ports = @(5050, 5173, 5174, 5175, 5176, 5177, 5178, 5179)
foreach ($port in $ports) {
    try {
        $connections = netstat -ano | Select-String ":$port\s"
        foreach ($conn in $connections) {
            $parts = $conn.ToString().Trim() -split '\s+'
            $pid = $parts[-1]
            if ($pid -match '^\d+$' -and $pid -ne '0') {
                Stop-Process -Id ([int]$pid) -Force -ErrorAction SilentlyContinue
            }
        }
    } catch {}
}

# Kill any remaining node processes
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "All services stopped." -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to close this window..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
