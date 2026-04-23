#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Configuration
)

$Mods = @('Sdk')

# Source the helper (expects Get-ModFiles and CopyFiles)
. ./BuildInfo.ps1

# 1. Build solution
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$solutionPath = Join-Path $scriptDir "$solutionName.sln"
if (-not (Test-Path $solutionPath))
{
    Write-Error "Solution file not found at $solutionPath"
    exit 1
}

Write-Output "Building solution $solutionPath in configuration $Configuration..."
$buildOutput = dotnet build $solutionPath -c $Configuration | Tee-Object -FilePath 'build.log'

# 2. Extract version number from build output
$pattern = '\s*Build Version:\s*(?<ver>\d+(\.\d+){3})'
$match = $buildOutput | Select-String -Pattern $pattern -AllMatches

if (-not $match)
{
    Write-Error "Could not find 'Build Version' in build output."
    exit 1
}

$version = $match[0].Matches[0].Groups['ver'].Value
Write-Output "Extracted version: $version"

# 3. Prepare temporary output directory
$outputRoot = Join-Path $scriptDir 'Output'
if (-not (Test-Path $outputRoot))
{
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
}

$destRoot = Join-Path $outputRoot "SDK"
New-Item -ItemType Directory -Path $destRoot -Force | Out-Null

# 4. Build combined file list across variants
$allFiles = @()
foreach ($p in $Mods)
{
    $lists = Get-ModFiles -Mod $p -Configuration $Configuration
    $allFiles += $lists.Mod
}

# Append non-SDK mod files
$allFiles += @(
    @(@("manifest.json"), "WukongMp.Coop", "Mods/WukongMp.Coop"),
    @(@("WukongMp.Coop.dll"), "WukongMp.Coop/bin/Release/netstandard2.0", "Mods/WukongMp.Coop"),
    @(@("ArchiveSaveFile.1.sav"), "Deployment", "Mods/WukongMp.Coop"),
    @(@("manifest.json"), "WukongMp.Pvp", "Mods/WukongMp.Pvp"),
    @(@("WukongMp.Pvp.dll"), "WukongMp.Pvp/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Pvp"),
    @(@("WukongMp.Pvp.pdb"), "WukongMp.Pvp/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Pvp"),
    @(@("ArchiveSaveFile.0.sav"), "Deployment", "Mods/WukongMp.Pvp"),
    @(@("ArchiveSaveFile.1.sav"), "Deployment", "Mods/WukongMp.Pvp")
)

# Create destination directories
foreach ($item in $allFiles)
{
    $destDir = Join-Path $destRoot $item[2]
    if (-not (Test-Path $destDir))
    {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
}

# 5. Perform copies
foreach ($item in $allFiles)
{
    $files = $item[0]
    $sourceDir = $item[1]
    $destDir = Join-Path $destRoot $item[2]
    CopyFiles $files $sourceDir $destDir
}

# 6. Open explorer to the output directory
if ($PSVersionTable.PSEdition -eq 'Core')
{
    Start-Process "explorer.exe" -ArgumentList $outputRoot
}
else
{
    Invoke-Item $outputRoot
}
