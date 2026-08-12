#!powershell.exe -ExecutionPolicy Bypass -File

# Static metadata
$solutionName = "WukongCSharpMod"
$zipName = "WukongMp"

# Shared (variant-agnostic) file name lists
$modFilesCore = @(
    "manifest.json"
    "BouncyCastle.Cryptography.dll"
    "DryIoc.dll"
    "Friflo.Engine.ECS.Boost.dll"
    "Friflo.Engine.ECS.dll"
    "Friflo.Json.Burst.dll"
    "Friflo.Json.Fliox.Annotation.dll"
    "Friflo.Json.Fliox.dll"
    "HttpMachine.dll"
    "IHttpMachine.dll"
    "JetBrains.Annotations.dll"
    "Microsoft.Bcl.Memory.dll"
    "Microsoft.Bcl.Numerics.dll"
    "Nito.AsyncEx.Context.dll"
    "Nito.AsyncEx.Tasks.dll"
    "Nito.Disposables.dll"
    "ReadyM.Api.Multiplayer.dll"
    "ReadyM.Api.dll"
    "ReadyM.Relay.Client.dll"
    "ReadyM.Wukong.Common.dll"
    "Superpower.dll"
    "System.ComponentModel.Annotations.dll"
    "System.Reflection.Emit.ILGeneration.dll"
    "System.Reflection.Emit.dll"
    "System.Reflection.Emit.dll"
    "WukongMp.Api.dll"
    "WukongMp.Sdk.dll"
    "Yooni.Native.Container.dll"
    "Yooni.Native.LowLevel.dll"
    "Yooni.Native.Serialization.dll"
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

$overridesFilesDebug = @(
    "System.Text.Encodings.Web.pdb",
    "System.Text.Json.pdb",
    "System.Numerics.Vectors.pdb",
    "LiteNetLib.pdb"
)

$binaryFiles = @(
    "cacert.pem"
    "CoreMp.pak"
    "WukongMp.pak"
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
    $binariesSourceDir = "Deployment"

    $modDestDir = "Mods/WukongMp.$Mod"
    $reflectionOnlyDestDir = "Mods/ReflectionOnly"
    $overridesDestDir = "Mods/Overrides"

    # Compose the triplets: @( <files>, <sourceDir>, <destDir> )
    $modFiles = @(
        @($modFilesCore, $modSourceDir, $modDestDir),
        @($cultureFolders, $modSourceDir, $modDestDir),
        @($binaryFiles, $binariesSourceDir, $modDestDir)
    )

    $devFiles = $modFiles + @(
        @($modFilesDebugCore, $modSourceDir, $modDestDir),
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
