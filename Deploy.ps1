param(
    [string]$ModVariant,
    [String]$Configuration = "Release"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$solutionPath = Join-Path $scriptDir "WukongMp.$ModVariant/WukongMp.$ModVariant.csproj"

# 1. Build solution

Write-Output "Building solution $solutionPath in configuration $Configuration..."
$buildOutput = dotnet build $solutionPath -c $Configuration -v minimal /t:Rebuild

# 2. Extract version number from build output
$pattern = '\s*Build Version:\s*(?<ver>\d+(\.\d+){4})'
$match   = $buildOutput | Select-String -Pattern $pattern -AllMatches

if (-not $match) {
    Write-Error "Could not find 'Build Version' in build output."
    exit 1
}

$version = $match[0].Matches[0].Groups['ver'].Value
Write-Output "Extracted version: $version"

# make directory "Mods" if not exists, otherwise clear it
if (!(Test-Path -Path "Mods")) {
    New-Item -ItemType Directory -Path "Mods" -Force
} else {
    Get-ChildItem -Path "Mods" | Remove-Item -Force -Recurse
}

$sourceDir = "WukongMp.$ModVariant/bin/Release/netstandard2.1"
$destDir = "Mods/WukongMpMod"

# Create the destination directory if it doesn't exist
if (!(Test-Path -Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force
}

# Define the files to copy
$files = @("WukongApi.dll", "WukongMpMod.dll", "ReadyM.Relay.Client.dll", "ReadyM.Relay.Common.dll", "de", "es", "fr", "pl", "pt", "zh-Hans")

# Copy each file to the destination directory
foreach ($file in $files) {
    $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
    $destFile = Join-Path -Path $destDir -ChildPath $file
    if (Test-Path -Path $sourceFile) {
        Copy-Item -Path $sourceFile -Destination $destFile -Force -Recurse
        Write-Output "Copied $file to $destDir"
    } else {
        Write-Output "$file does not exist in $sourceDir"
        exit 1
    }
}

# copy all files from inside the "Deployment" folder to the "Mods/WukongMpMod" folder
$deploymentFiles = Get-ChildItem -Path "Deployment" -Recurse

foreach ($file in $deploymentFiles) {
    $relativePath = $file.FullName.Substring($file.DirectoryName.Length + 1)
    $destinationPath = Join-Path -Path $destDir -ChildPath $relativePath
    $destinationDirPath = Split-Path -Path $destinationPath -Parent

    # Create the destination directory if it doesn't exist
    if (!(Test-Path -Path $destinationDirPath)) {
        New-Item -ItemType Directory -Path $destinationDirPath -Force
    }

    # Copy the file to the destination directory
    Copy-Item -Path $file.FullName -Destination $destinationPath -Force
}

# ZIP the Mods folder into "WukongMp-version.zip" where version is the version number extracted from the build output
$zipName = "WukongMp-$version.zip"
$zipPath = Join-Path $scriptDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $scriptDir 'Mods') -DestinationPath $zipPath -Force
