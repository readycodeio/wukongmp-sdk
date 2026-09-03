#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Configuration,
    [switch] $NoExplorer
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
#
# Emptied first, the same way the mods' MakeModFolder.ps1 does it. Copying into a kept folder leaves
# artifacts from earlier builds behind, and whatever deploys from here cannot tell them apart.
$outputRoot = Join-Path $scriptDir 'Output'
if (Test-Path $outputRoot)
{
    Get-ChildItem $outputRoot -Recurse | Remove-Item -Force -Recurse
}
else
{
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
}

# 4. Build combined file list across variants. Everything lands in Output/mods/WukongMp.Sdk as a
# mod package: client files under client/, server files under server/, manifest at the root.
$allFiles = @()
foreach ($p in $Mods)
{
    $lists = Get-ModFiles -Mod $p -Configuration $Configuration

    # Pick the key, not the value: an `if` that returns a one-element collection unwraps it on the
    # output stream, and `+=` then splices that triplet's members in as separate entries. The Server
    # set holds exactly one triplet, so that only ever went wrong outside Debug.
    $serverSet = if ($Configuration -eq "Debug") { "ServerDev" } else { "Server" }

    $allFiles += $lists.Mod
    $allFiles += $lists.$serverSet
}

# Append PDB files in Debug configuration
if ($Configuration -eq "Debug")
{
    $allFiles += @(
        @(@("WukongMp.Api.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "mods/WukongMp.Sdk/client"),
        @(@("WukongMp.Sdk.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "mods/WukongMp.Sdk/client")
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
if ($NoExplorer)
{
    # nothing to open, this run is scripted
}
elseif ($PSVersionTable.PSEdition -eq 'Core')
{
    Start-Process "explorer.exe" -ArgumentList $outputRoot
}
else
{
    Invoke-Item $outputRoot
}
