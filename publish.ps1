# publish.ps1 — Self-contained win-x64 publish (no installer).
# Produces a runnable folder under .\publish\ that can be copied to any Windows x64 machine.
#
# Usage:
#   .\publish.ps1              # version defaults to 1.0.0
#   .\publish.ps1 -Version 1.2.0

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root  = $PSScriptRoot
$out   = Join-Path $root "publish"

Write-Host "Publishing Richie $Version (win-x64, self-contained)..."

dotnet publish (Join-Path $root "src\Richie.UI\Richie.UI.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o $out `
    /p:PublishReadyToRun=true `
    /p:Version=$Version `
    /p:AssemblyVersion="$Version.0" `
    /p:FileVersion="$Version.0"

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }

Write-Host ""
Write-Host "Published to: $out"
Write-Host "Entry point:  $out\Richie.exe"
