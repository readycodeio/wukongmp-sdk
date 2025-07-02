#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string]$ModVariant,
    [string]$Configuration
)

# Require parameters
if (-not $ModVariant -or -not $Configuration) {
    Write-Host "Usage: .\BuildModZip.ps1 <variant> <configuration>"
    Exit 1
}

. ./BuildInfo.ps1

# 1. Build solution
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$solutionPath = Join-Path $scriptDir "$solutionName.sln"
if (-not (Test-Path $solutionPath)) {
    Write-Error "Solution file not found at $solutionPath"
    exit 1
}

Write-Output "Building solution $solutionPath in configuration $Configuration..."
$buildOutput = dotnet build $solutionPath -c $Configuration -v minimal /t:Rebuild | Tee-Object -FilePath 'build.log'

# 2. Extract version number from build output
$pattern = '\s*Build Version:\s*(?<ver>\d+(\.\d+){3})'
$match   = $buildOutput | Select-String -Pattern $pattern -AllMatches

if (-not $match) { 
    Write-Error "Could not find 'Build Version' in build output."
    exit 1
}

$version = $match[0].Matches[0].Groups['ver'].Value
Write-Output "Extracted version: $version"


# 3. Prepare temporary output directory
$outputRoot = Join-Path $scriptDir 'Output'
if (-not (Test-Path $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
}
$destRoot = Join-Path $outputRoot "$zipName-$version"
New-Item -ItemType Directory -Path $destRoot -Force | Out-Null

foreach ($item in $modFiles) {
    $destDir = Join-Path $destRoot $item[2]
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
}

# 4. Perform copies
foreach ($item in $modFiles) {
    $files = $item[0]
    $sourceDir = $item[1]
    $destDir = Join-Path $destRoot $item[2]

    CopyFiles $files $sourceDir $destDir
}

# 5. Zip files
$zipName = "$zipName-$version.zip"
$zipPath = Join-Path $outputRoot $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $destRoot '*') -DestinationPath $zipPath -Force
Write-Output "Created $zipName"
