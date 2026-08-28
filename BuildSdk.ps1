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
dotnet build $solutionPath -c $Configuration

# 3. Prepare temporary output directory
$outputRoot = Join-Path $scriptDir 'Output'
if (-not (Test-Path $outputRoot))
{
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

# 4. Build combined file list across variants. The client mod goes to Output/mods/WukongMp.Sdk,
# the server mod to Output/server_mods, mirroring the layout a server drop expects.
$allFiles = @()
foreach ($p in $Mods)
{
    $lists = Get-ModFiles -Mod $p -Configuration $Configuration
    $allFiles += $lists.Mod
    $allFiles += if ($Configuration -eq "Debug") { $lists.ServerDev } else { $lists.Server }
}

# Append PDB files in Debug configuration
if ($Configuration -eq "Debug")
{
    $allFiles += @(
        @(@("WukongMp.Api.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "mods/WukongMp.Sdk"),
        @(@("WukongMp.Sdk.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "mods/WukongMp.Sdk")
    )
}

# Create destination directories
foreach ($item in $allFiles)
{
    $destDir = Join-Path $outputRoot $item[2]
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
    $destDir = Join-Path $outputRoot $item[2]
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
