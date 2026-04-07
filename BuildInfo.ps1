#!powershell.exe -ExecutionPolicy Bypass -File

# Static metadata
$solutionName = "WukongCSharpMod"
$zipName = "WukongMp"

# Shared (variant-agnostic) file name lists
$modFilesCore = @(
    "manifest.json",
    "WukongMp.Api.dll",
    "WukongMp.Sdk.dll",
    "ReadyM.Api.dll",
    "ReadyM.Api.Multiplayer.dll",
    "ReadyM.Relay.Client.dll",
    "ReadyM.Wukong.Common.dll",
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
    "Superpower.dll",
    "DryIoc.dll",
    "System.Reflection.Emit.dll",
    "System.Reflection.Emit.ILGeneration.dll",
    "BouncyCastle.Cryptography.dll",
    "HttpMachine.dll",
    "IHttpMachine.dll"
)

$modFilesDebugCore = @(
    "WukongMp.Api.pdb",
    "WukongMp.Sdk.pdb",
    "ReadyM.Api.pdb",
    "ReadyM.Api.Multiplayer.pdb",
    "ReadyM.Relay.Client.pdb",
    "ReadyM.Wukong.Common.pdb",
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

$binaryFiles = @(
    "cacert.pem"
)

# Culture folders (satellite assemblies)
$cultureFolders = @("de", "es", "fr", "pl", "pt", "zh-Hans")

function Get-ModFiles
{
    param(
        [Parameter(Mandatory = $true)][string]$Mod,
        [Parameter(Mandatory = $true)][string]$Configuration
    )

    # Compute *per-variant* paths
    $modSourceDir = "WukongMp.$Mod/bin/$Configuration/netstandard2.0"
    $reflectionOnlySourceDir = "WukongMp.Api/Game"
    $overridesSourceDir = "WukongMp.Api/Game"
    $binariesSourceDir = "Deployment"

    $modDestDir = "Mods/WukongMp.$Mod"
    $reflectionOnlyDestDir = "Mods/ReflectionOnly"
    $overridesDestDir = "Mods/Overrides"
    
    # Compose the triplets: @( <files>, <sourceDir>, <destDir> )
    $modFiles = @(
        @($modFilesCore, $modSourceDir, $modDestDir),
        @($cultureFolders, $modSourceDir, $modDestDir),
        @($overridesFiles, $overridesSourceDir, $overridesDestDir),
        @($binaryFiles, $binariesSourceDir, $modDestDir)
    )

    $devFiles = $modFiles + @(
        @($modFilesDebugCore, $modSourceDir, $modDestDir),
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
