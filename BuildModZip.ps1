#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Configuration
)

# Normalize params
if (-not $Configuration)
{
    Write-Host "Usage: .\BuildModZip.ps1 -Configuration <Debug|Release>"
    Exit 1
}

$ModVariants = @('Coop', 'PvP')

# Source the helper (expects Get-VariantLists and CopyFiles)
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
foreach ($v in $ModVariants)
{
    $lists = Get-VariantLists -Variant $v -Configuration $Configuration
    if ($Configuration -eq 'Debug')
    {
        $allFiles += $lists.Dev
    }
    else
    {
        $allFiles += $lists.Mod
    }
}

# (Optional) de-dup identical triplets if you want to avoid recopying shared dirs:
# $allFiles = $allFiles | Sort-Object { "$($_[1])|$($_[2])|$($_[0] -join ',')" } -Unique

# Pre-create destination directories
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

# 6. Zip files (single ZIP containing all variants)
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
