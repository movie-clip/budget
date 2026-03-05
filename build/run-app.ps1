param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "Running HomeCharts.App ($Configuration)"

dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    throw "dotnet run failed with exit code $LASTEXITCODE"
}
