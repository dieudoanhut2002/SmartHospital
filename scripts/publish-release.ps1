param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish",
    [switch]$SelfContained,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $targetPath = Join-Path $root $OutputDir
    if (Test-Path $targetPath) {
        Remove-Item -Recurse -Force $targetPath
    }

    Write-Host "=== SmartHospital: Publish ($Configuration) ===" -ForegroundColor Cyan
    $args = @(
        "publish",
        "src/SmartHospital.Web/SmartHospital.Web.csproj",
        "-c", $Configuration,
        "-o", $targetPath
    )

    if ($SelfContained) {
        $args += @("-r", $Runtime, "--self-contained", "true")
    }

    & dotnet @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "=== Publish thanh cong tai: $targetPath ===" -ForegroundColor Green
}
finally {
    Pop-Location
}
