param (
    [string]$ModVariant,
    [string]$Configuration
)

# Require parameters
if (-not $ModVariant -or -not $Configuration) {
    Write-Host "Usage: .\CopyToGameFolder.ps1 <variant> <configuration>"
    Exit 1
}

# Define the source and destination directories
$sourceDir = "WukongMp.$ModVariant/bin/$Configuration/netstandard2.1"
$steamDir = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -Name "InstallPath" | Select-Object -ExpandProperty InstallPath
$destDir = "$steamDir\steamapps\common\BlackMythWukong\b1\Binaries\Win64\CSharpLoader\Mods\WukongMpMod"

# Create the destination directory if it doesn't exist
if (!(Test-Path -Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force
}

# Define the files to copy
$files = @("WukongMp.Api.dll", "WukongMp.Api.pdb", "WukongMpMod.dll", "WukongMpMod.pdb", "ReadyM.Relay.Client.dll", "ReadyM.Relay.Client.pdb", "ReadyM.Relay.Common.dll", "ReadyM.Relay.Common.pdb", "ReadyM.Relay.Common.Wukong.dll", "ReadyM.Relay.Common.Wukong.pdb")

# Copy each file to the destination directory
foreach ($file in $files) {
    $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
    $destFile = Join-Path -Path $destDir -ChildPath $file
    if (Test-Path -Path $sourceFile) {
        Copy-Item -Path $sourceFile -Destination $destFile -Force
        Write-Output "Copied $file to $destDir"
    } else {
        Write-Output "$file does not exist in $sourceDir"
        exit 1
    }
}
