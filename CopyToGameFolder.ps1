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

$steamDir = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -Name "InstallPath" | Select-Object -ExpandProperty InstallPath
$destRoot = "$steamDir/steamapps/common/BlackMythWukong/b1/Binaries/Win64/CSharpLoader"

# Perform copies
foreach ($item in $allFiles) {
    $files = $item[0]
    $sourceDir = $item[1]
    $destDir = Join-Path $destRoot $item[2]

    CopyFiles $files $sourceDir $destDir
}
