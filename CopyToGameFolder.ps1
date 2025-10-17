#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string]$ModVariant,
    [string]$Configuration
)

# Require parameters
if (-not $ModVariant -or -not $Configuration) {
    Write-Host "Usage: .\CopyToGameFolder.ps1 <variant> <configuration>"
    Exit 1
}

. ./BuildInfo.ps1

$destRoot = Join-Path $env:APPDATA "ReadyM.Launcher/WukongMP/CSharpLoader"

# Perform copies
foreach ($item in $devFiles) {
    $files     = $item[0]
    $sourceDir = $item[1]
    $destDir   = Join-Path $destRoot $item[2]

    CopyFiles $files $sourceDir $destDir
}
