#!powershell.exe -ExecutionPolicy Bypass -File

# Static metadata
$solutionName = "WukongCSharpMod"
$zipName = "WukongMp"

# Shared (variant-agnostic) file name lists
$modFilesCore = @(
    "WukongMp.Api.dll",
    "ReadyM.Api.dll",
    "ReadyM.Api.Multiplayer.dll",
    "ReadyM.Relay.Client.dll",
    "ReadyM.Relay.Common.dll",
    "ReadyM.Relay.Common.Wukong.dll",
    "Friflo.Engine.ECS.dll",
    "Friflo.Engine.ECS.Boost.dll",
    "Friflo.Json.Burst.dll",
    "Friflo.Json.Fliox.dll",
    "Friflo.Json.Fliox.Annotation.dll",
    "JetBrains.Annotations.dll",
    "Microsoft.Bcl.Memory.dll",
    "Microsoft.Bcl.Numerics.dll",
    "System.Reflection.Emit.dll",
    "System.ComponentModel.Annotations.dll",
    "Nito.AsyncEx.Context.dll",
    "Nito.AsyncEx.Tasks.dll",
    "Nito.Disposables.dll",
    "BouncyCastle.Cryptography.dll",
    "HttpMachine.dll",
    "IHttpMachine.dll"
)

$modFilesDebugCore = @(
    "WukongMp.Api.pdb",
    "ReadyM.Api.pdb",
    "ReadyM.Api.Multiplayer.pdb",
    "ReadyM.Relay.Client.pdb",
    "ReadyM.Relay.Common.pdb",
    "ReadyM.Relay.Common.Wukong.pdb",
    "Friflo.Engine.ECS.pdb",
    "Friflo.Engine.ECS.Boost.pdb"
)

$reflectionOnlyFiles = @("*")

$overridesFiles = @(
    "System.Collections.Immutable.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Text.Encodings.Web.dll",
    "System.Text.Json.dll",
    "System.Numerics.Vectors.dll",
    "LiteNetLib.dll"
)

$overridesFilesDebug = @(
    "System.Text.Encodings.Web.pdb",
    "System.Text.Json.pdb",
    "System.Numerics.Vectors.pdb",
    "LiteNetLib.pdb"
)

$saveFilesBase = @(
    "cacert.pem",
    "ArchiveSaveFile.1.sav" # Prologue save file
)

# Culture folders (satellite assemblies)
$cultureFolders = @("de", "es", "fr", "pl", "pt", "zh-Hans")

function Get-VariantLists
{
    param(
        [Parameter(Mandatory = $true)][string]$Variant,
        [Parameter(Mandatory = $true)][string]$Configuration
    )

    # Compute *per-variant* paths
    $modSourceDir = "WukongMp.$Variant/bin/$Configuration/netstandard2.0"
    $reflectionOnlySourceDir = "WukongMp.Api/Game"
    $overridesSourceDir = "WukongMp.Api/Game"
    $saveSourceDir = "Deployment"

    $modDestDir = "Mods/WukongMp.$Variant"
    $reflectionOnlyDestDir = "Mods/ReflectionOnly"
    $overridesDestDir = "Mods/Overrides"
    $saveDestDir = "Mods/WukongMp.$Variant"

    # Save files (PvP adds PvP save)
    $saveFiles = @($saveFilesBase)
    if ($Variant -eq 'PvP')
    {
        $saveFiles += "ArchiveSaveFile.0.sav"
    }
    
    # add "WukongMp.$ModVariant.dll" to modFilesCore
    $modFilesVariant = $modFilesCore + "WukongMp.$Variant.dll"
    $modFilesDebugVariant = $modFilesDebugCore + "WukongMp.$Variant.pdb"

    # Compose the triplets: @( <files>, <sourceDir>, <destDir> )
    $modFiles = @(
        @($modFilesVariant, $modSourceDir, $modDestDir),
        @($cultureFolders, $modSourceDir, $modDestDir),
        @($overridesFiles, $overridesSourceDir, $overridesDestDir),
        @($saveFiles, $saveSourceDir, $saveDestDir)
    )

    $devFiles = $modFiles + @(
        @($modFilesDebugVariant, $modSourceDir, $modDestDir),
        @($overridesFilesDebug, $overridesSourceDir, $overridesDestDir),
        @($reflectionOnlyFiles, $reflectionOnlySourceDir, $reflectionOnlyDestDir)
    )

    # Return both sets so caller can pick based on configuration
    return @{
        Mod = $modFiles
        Dev = $devFiles
    }
}

function CopyFiles($files, $sourceDir, $destDir)
{
    if (!(Test-Path -Path $destDir))
    {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    foreach ($file in $files)
    {
        $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
        $destFile = Join-Path -Path $destDir   -ChildPath $file

        if ($file -eq "*")
        {
            if (Test-Path -Path $destDir)
            {
                Remove-Item -Path $destDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            Copy-Item -Path $sourceFile -Destination $destDir -Recurse -Force
            Write-Output "Copied $file to $destDir (recursive)"
        }
        elseif (Test-Path -Path $sourceFile -PathType Leaf)
        {
            Copy-Item -Path $sourceFile -Destination $destFile -Force
            Write-Output "Copied $file to $destDir"
        }
        elseif (Test-Path -Path $sourceFile -PathType Container)
        {
            if (Test-Path -Path $destFile)
            {
                Remove-Item -Path $destFile -Recurse -Force
            }
            Copy-Item -Path $sourceFile -Destination $destFile -Recurse -Force
            Write-Output "Copied $file to $destDir (recursive)"
        }
        else
        {
            Write-Output "[Error] $file does not exist in $sourceDir"
        }
    }
}
