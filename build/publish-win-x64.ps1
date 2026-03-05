param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/publish/win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing HomeCharts.App ($Configuration, $Runtime) to $Output"

dotnet publish src/HomeCharts.App/HomeCharts.App.csproj `
  -c $Configuration `
  -r $Runtime `
  --self-contained false `
  -p:PublishSingleFile=false `
  -o $Output

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "Publish completed: $Output"
