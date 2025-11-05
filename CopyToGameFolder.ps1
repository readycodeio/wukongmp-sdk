#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Configuration
)

# Require Configuration
if (-not $Configuration) {
    Write-Host "Usage: .\CopyToGameFolder.ps1 -Configuration <Debug|Release>"
    Write-Host "   or  .\CopyToGameFolder.ps1 -Configuration <Debug|Release>"
    Exit 1
}

$ModVariants = @('Coop','PvP')

# Bring in Get-VariantLists + CopyFiles
. ./BuildInfo.ps1

# Destination under Roaming AppData
$destRoot = Join-Path $env:APPDATA "ReadyM.Launcher/WukongMP/CSharpLoader"

# Build the combined dev file triplets across variants
$allDevFiles = @()
foreach ($v in $ModVariants) {
    $lists = Get-VariantLists -Variant $v -Configuration $Configuration
    $allDevFiles += $lists.Dev
}

# (Optional) de-dup identical triplets if desired
# $allDevFiles = $allDevFiles | Sort-Object { "$($_[1])|$($_[2])|$($_[0] -join ',')" } -Unique

# Pre-create destination directories
foreach ($item in $allDevFiles) {
    $destDir = Join-Path $destRoot $item[2]
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
}

# Perform copies
foreach ($item in $allDevFiles) {
    $files     = $item[0]
    $sourceDir = $item[1]
    $destDir   = Join-Path $destRoot $item[2]
    CopyFiles $files $sourceDir $destDir
}

Write-Output "Copied developer files for: $($ModVariants -join ', ') to $destRoot"
