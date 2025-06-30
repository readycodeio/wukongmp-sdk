param (
    [string]$Configuration
)

# Change to the directory where this script is located
Set-Location -Path $PSScriptRoot

# Debug by default
if (-not $Configuration) {
    $Configuration = "Debug"
}

# Define the source and destination directories
$sourceDir = "WukongMpMod/bin/$Configuration/netstandard2.1"
$steamDir = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -Name "InstallPath" | Select-Object -ExpandProperty InstallPath
$destDir = "$steamDir\steamapps\common\BlackMythWukong\b1\Binaries\Win64\CSharpLoader\Mods\WukongMpMod"
$overridesDir = "$steamDir\steamapps\common\BlackMythWukong\b1\Binaries\Win64\CSharpLoader\Mods\Overrides"

# Create the destination directory if it doesn't exist
if (!(Test-Path -Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force
}

# Create the overrides directory if it doesn't exist
if (!(Test-Path -Path $overridesDir)) {
    New-Item -ItemType Directory -Path $overridesDir -Force
}

# Define the files to copy
$files = @("WukongApi.dll", "WukongMpMod.dll", "ReadyM.Relay.Client.dll", "ReadyM.Relay.Common.dll")
$overridesFiles = @("LiteNetLib.dll")

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

# Copy each overrides file to the overrides directory
foreach ($file in $overridesFiles) {
    $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
    $destFile = Join-Path -Path $overridesDir -ChildPath $file
    if (Test-Path -Path $sourceFile) {
        Copy-Item -Path $sourceFile -Destination $destFile -Force
        Write-Output "Copied $file to $overridesDir"
    } else {
        Write-Output "$file does not exist in $sourceDir"
        exit 1
    }
}
