#!powershell.exe -ExecutionPolicy Bypass -File
param (
    [string] $Configuration,
    [string] $Mode
)

# Require Configuration
if (-not $Configuration -or ($Configuration -ne "Debug" -and $Configuration -ne "Release") -or -not $Mode -or ($Mode -ne "coop" -and $Mode -ne "pvp"))
{
    Write-Host "Usage: .\CopyToGameFolder.ps1 -Configuration <Debug|Release> -Mode <coop|pvp>"
    Exit 1
}

$Mods = @('Sdk')

# Bring in Get-ModFiles + CopyFiles
. ./BuildInfo.ps1

# Destination under Roaming AppData
$destRoot = Join-Path $env:APPDATA "ReadyM.Launcher/WukongMP/CSharpLoader"

# Build the combined dev file triplets across variants
$allDevFiles = @()
foreach ($p in $Mods)
{
    $lists = Get-ModFiles -Mod $p -Configuration $Configuration
    $allDevFiles += $lists.Dev
}

if ($Mode -eq "coop")
{
    $allDevFiles += @(
        @(@("WukongMp.Coop.dll"), "WukongMP-co-op-mod/WukongMp.Coop/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Coop"),
        @(@("manifest.json"), "WukongMP-co-op-mod/Content", "Mods/WukongMp.Coop"),
        @(@("ArchiveSaveFile.1.sav"), "WukongMP-co-op-mod/Content", "Mods/WukongMp.Coop")
    )

    if ($Configuration -eq "Debug")
    {
        $allDevFiles += @(
            ,@(@("WukongMp.Coop.pdb"), "WukongMP-co-op-mod/WukongMp.Coop/bin/Debug/netstandard2.0", "Mods/WukongMp.Coop")
        )
    }
}
else
{
    $allDevFiles += @(
        @(@("WukongMp.Pvp.dll"), "WukongMP-PvP-mod/WukongMp.PvP/bin/$Configuration/netstandard2.0", "Mods/WukongMp.Pvp"),
        @(@("manifest.json"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp"),
        @(@("ArchiveSaveFile.0.sav"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp"),
        @(@("ArchiveSaveFile.1.sav"), "WukongMP-PvP-mod/Content", "Mods/WukongMp.Pvp")
    )

    if ($Configuration -eq "Debug")
    {
        $allDevFiles += @(
            ,@(@("WukongMp.Pvp.pdb"), "WukongMP-PvP-mod/WukongMp.Pvp/bin/Debug/netstandard2.0", "Mods/WukongMp.Pvp")
        )
    }
}

if ($Configuration -eq "Debug")
{
    $allDevFiles += @(
        @(@("WukongMp.Api.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "Mods/WukongMp.Sdk"),
        @(@("WukongMp.Sdk.pdb"), "WukongMp.Sdk/bin/Debug/netstandard2.0", "Mods/WukongMp.Sdk")
    )
}

# (Optional) de-dup identical triplets if desired
# $allDevFiles = $allDevFiles | Sort-Object { "$($_[1])|$($_[2])|$($_[0] -join ',')" } -Unique

# Pre-create destination directories
foreach ($item in $allDevFiles)
{
    $destDir = Join-Path $destRoot $item[2]
    if (-not (Test-Path $destDir))
    {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
}

# Perform copies
foreach ($item in $allDevFiles)
{
    $files = $item[0]
    $sourceDir = $item[1]
    $destDir = Join-Path $destRoot $item[2]
    CopyFiles $files $sourceDir $destDir
}

Write-Output "Copied developer files for: $( $ModVariants -join ', ' ) to $destRoot"
