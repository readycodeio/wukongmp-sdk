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

# Map ModVariant -> game mode folder name
$modeFolder = switch -Regex ($ModVariant.ToLower()) {
    'coop' { 'Black Myth Wukong Co-op'; break }
    'pvp'  { 'Black Myth Wukong PvP';   break }
    'tests'  { 'Black Myth Wukong Tests';   break }
    default {
        Write-Error "Invalid ModVariant: '$ModVariant'. Expected 'Coop' or 'PvP' or 'Tests'."
        exit 1
    }
}

$destRoot = Join-Path $env:APPDATA "ReadyM.Launcher/game_modes/$modeFolder/CSharpLoader"

# Perform copies
foreach ($item in $devFiles) {
    $files     = $item[0]
    $sourceDir = $item[1]
    $destDir   = Join-Path $destRoot $item[2]

    CopyFiles $files $sourceDir $destDir
}
