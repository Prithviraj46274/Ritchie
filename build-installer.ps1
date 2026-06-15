# build-installer.ps1 — Full release build: publish then build the MSI installer.
#
# Prerequisites (run once on a new machine):
#   dotnet tool install --global wix
#   wix eula accept wix7
#   wix extension add --global WixToolset.UI.wixext
#
# Usage:
#   .\build-installer.ps1              # version defaults to 1.0.0
#   .\build-installer.ps1 -Version 1.2.0

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root        = $PSScriptRoot
$publishDir  = Join-Path $root "publish"
$distDir     = Join-Path $root "dist"
$installerDir = Join-Path $root "Richie.Installer"
$msiPath     = Join-Path $distDir "Richie-Setup.msi"

# ── 1. Self-contained publish ────────────────────────────────────────────────

Write-Host "Step 1: Publishing Richie $Version (win-x64, self-contained)..."

dotnet publish (Join-Path $root "src\Richie.UI\Richie.UI.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained `
    -o $publishDir `
    /p:PublishReadyToRun=true `
    /p:Version=$Version `
    /p:AssemblyVersion="$Version.0" `
    /p:FileVersion="$Version.0"

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }

# ── 2. Verify wix is available ───────────────────────────────────────────────

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Error @"
WiX toolset not found. Install it with:
  dotnet tool install --global wix
  wix extension add --global WixToolset.UI.wixext
"@
    exit 1
}

# ── 3. Build MSI ─────────────────────────────────────────────────────────────

Write-Host "Step 2: Building MSI installer..."
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# Ensure publish dir path ends with backslash (WiX glob requires it)
$publishDirSlash = $publishDir.TrimEnd('\') + '\'

wix build (Join-Path $installerDir "Package.wxs") `
    -arch x64 `
    -ext WixToolset.UI.wixext `
    -d "PublishDir=$publishDirSlash" `
    -d "Version=$Version" `
    -o $msiPath

if ($LASTEXITCODE -ne 0) { Write-Error "wix build failed"; exit 1 }

# ── Done ─────────────────────────────────────────────────────────────────────

$sizeMb = [math]::Round((Get-Item $msiPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Installer built: $msiPath ($sizeMb MB)"
Write-Host "To install:  double-click Richie-Setup.msi"
Write-Host "To uninstall: Settings -> Apps -> Richie"
