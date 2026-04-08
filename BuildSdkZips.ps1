#!powershell.exe -ExecutionPolicy Bypass -File

$Mods = @('Sdk')
$Configuration = "Release"

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
    @(@("manifest.json"), "WukongMp.Coop/bin/Release/netstandard2.0", "Mods/WukongMp.Coop"),
    @(@("WukongMp.Coop.dll"), "WukongMp.Coop/bin/Release/netstandard2.0", "Mods/WukongMp.Coop"),
    @(@("ArchiveSaveFile.1.sav"), "Deployment", "Mods/WukongMp.Coop")
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

# (ZIP target name, [list of mod folders to compress])
$archiveNames = @(
    @("WukongMp.Coop", @("Mods/WukongMp.Coop")),
    @("WukongMp.Sdk", @("Mods/WukongMp.Sdk", "Mods/Overrides"))
)

foreach ($item in $archiveNames)
{
    $archiveName = $item[0]
    $foldersToInclude = $item[1]

    $archivePath = Join-Path $outputRoot "$archiveName.zip"
    if (Test-Path $archivePath)
    {
        Remove-Item $archivePath -Force
    }

    $includePaths = @()
    foreach ($folder in $foldersToInclude)
    {
        $includePaths += (Join-Path $destRoot $folder)
    }

    Compress-Archive -Path $includePaths -DestinationPath $archivePath -Force
    Write-Output "Created $( Split-Path $archivePath -Leaf )"
}

# 7. Open explorer to the output directory
if ($PSVersionTable.PSEdition -eq 'Core')
{
    Start-Process "explorer.exe" -ArgumentList $outputRoot
}
else
{
    Invoke-Item $outputRoot
}
