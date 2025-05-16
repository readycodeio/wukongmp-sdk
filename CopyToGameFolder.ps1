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


# List of culture codes
$cultureFolders = @("de", "es", "fr", "pl", "pt", "zh-Hans")

# Resolve full path for the source
$sourceRoot = Resolve-Path $sourceDir
# Recursively find all folders with localization DLLs
Get-ChildItem -Path $sourceDir -Recurse -Directory | Where-Object {
    $cultureFolders -contains $_.Name
} | ForEach-Object {
    $cultureFolder = $_
    $dlls = Get-ChildItem -Path $cultureFolder.FullName -Filter "*.resources.dll" -File

    foreach ($dll in $dlls) {
        $relativePath = $dll.DirectoryName.Substring($sourceRoot.Path.Length).TrimStart('\')
        $targetDir = Join-Path -Path $destDir -ChildPath $relativePath

        # Ensure the target localization folder exists
        if (-not (Test-Path -Path $targetDir)) {
            New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
        }

        # Copy the DLL file
        Copy-Item -Path $dll.FullName -Destination $targetDir -Force
        Write-Output "Copied $($dll.FullName) to $targetDir"
    }
}
