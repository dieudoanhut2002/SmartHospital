param(
    [string]$Configuration = "Debug",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    Write-Host "=== SmartHospital: Restore ===" -ForegroundColor Cyan
    dotnet restore SmartHospital.sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "=== SmartHospital: Build ($Configuration) ===" -ForegroundColor Cyan
    dotnet build SmartHospital.sln -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not $SkipTests) {
        Write-Host "=== SmartHospital: Run Tests ===" -ForegroundColor Cyan
        dotnet test SmartHospital.sln -c $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    Write-Host "=== Hoan tat build & test ===" -ForegroundColor Green
}
finally {
    Pop-Location
}
