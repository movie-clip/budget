param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$PublishOutput = "artifacts/publish/win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Invoke-Step {
    param(
        [string]$Name,
        [string]$Command
    )

    Write-Host "==> $Name"
    Invoke-Expression $Command

    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

Invoke-Step "Restore solution" "dotnet restore HomeCharts.sln"
Invoke-Step "Build app project" "dotnet build src/HomeCharts.App/HomeCharts.App.csproj --configuration $Configuration --no-restore"
Invoke-Step "Build solution" "dotnet build HomeCharts.sln --configuration $Configuration --no-restore"
Invoke-Step "Run domain tests" "dotnet test tests/HomeCharts.Domain.Tests/HomeCharts.Domain.Tests.csproj --configuration $Configuration --no-build"
Invoke-Step "Run application tests" "dotnet test tests/HomeCharts.Application.Tests/HomeCharts.Application.Tests.csproj --configuration $Configuration --no-build"
Invoke-Step "Run infrastructure tests" "dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj --configuration $Configuration --no-build"
Invoke-Step "Publish app" "dotnet publish src/HomeCharts.App/HomeCharts.App.csproj -c $Configuration -r $Runtime --self-contained false -o $PublishOutput"

Write-Host "Production validation completed successfully."
