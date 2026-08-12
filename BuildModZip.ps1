#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Mode,
    [string] $Configuration
)

# Normalize params
if ($Configuration -ne 'Debug' -and $Configuration -ne 'Release')
{
    Write-Error "Invalid configuration specified. Use 'Debug' or 'Release'."
    Write-Host "Usage: .\BuildModZip.ps1 <pvp|coop> <Debug|Release>"
    Exit 1
}


if ($Mode -ne 'coop' -and $Mode -ne 'pvp')
{
    Write-Error "Invalid mode specified. Use 'coop' or 'pvp'."
    Write-Host "Usage: .\BuildModZip.ps1 <coop|pvp> <Debug|Release>"
    Exit 1
}

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
$buildOutput = dotnet build $solutionPath -c $Configuration -v minimal /t:Rebuild | Tee-Object -FilePath 'build.log'

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

$zipBase = "$zipName-$version"
$destRoot = Join-Path $outputRoot $zipBase
New-Item -ItemType Directory -Path $destRoot -Force | Out-Null

# 4. Build combined file list across variants
$allFiles = @()
foreach ($p in $Mods)
{
    $lists = Get-ModFiles -Mod $p -Configuration $Configuration
    $allFiles += $lists.Mod
}

# Append non-SDK mod files

if ($Mode -eq 'coop')
{
    $allFiles += @(
        @(@("manifest.json"), "WukongMP-co-op-mod/Content", "Mods/WukongMp.Coop"),
        @(@("WukongMp.Coop.dll"), "WukongMP-co-op-mod/WukongMp.Coop/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Coop"),
        @(@("WukongMp.Coop.Common.dll"), "WukongMP-co-op-mod/WukongMp.Coop/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Coop"),
        @(@("ArchiveSaveFile.1.sav"), "WukongMP-co-op-mod/Content", "Mods/WukongMp.Coop")
    )
} else {
    $allFiles += @(
        @(@("manifest.json"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp"),
        @(@("WukongMp.PvP.dll"), "WukongMP-PvP-mod/WukongMp.Pvp/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Pvp"),
        @(@("ArchiveSaveFile.0.sav"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp"),
        @(@("ArchiveSaveFile.1.sav"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp"),
        @(@("ArchiveSaveFile.2.sav"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp")
    )
}


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

# 6. Zip files (single 7Z containing all variants)
$zipPath = Join-Path $outputRoot "$zipBase.7z"
if (Test-Path $zipPath)
{
    Remove-Item $zipPath -Force
}

7z a -t7z -mx=9 -ms=on -mmt=on $zipPath (Join-Path $destRoot '*')
Write-Output "Created $( Split-Path $zipPath -Leaf )"

# 7. Open explorer to the output directory
if ($PSVersionTable.PSEdition -eq 'Core')
{
    Start-Process "explorer.exe" -ArgumentList $outputRoot
}
else
{
    Invoke-Item $outputRoot
}
