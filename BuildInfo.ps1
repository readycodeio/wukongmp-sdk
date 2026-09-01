#!powershell.exe -ExecutionPolicy Bypass -File

# Static metadata
$solutionName = "WukongMP.SDK"
$zipName = "WukongMp"

# Shared (variant-agnostic) file name lists
$manifestFiles = @("manifest.json")

$modFilesCore = @(
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
    "Yooni.Native.Logging.dll"
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

# Copied into the mod's server folder. Everything else these assemblies need (ReadyM.Api, Friflo,
# Yooni, the relay server SDK) is already part of the server host.
$serverModFilesCore = @(
    "WukongMp.Sdk.Serverside.dll"
    "ReadyM.Wukong.Common.dll"
)

$serverModFilesDebugCore = @(
    "WukongMp.Sdk.Serverside.pdb",
    "ReadyM.Wukong.Common.pdb"
)

$reflectionOnlyFiles = @("*")

$overridesFilesDebug = @(
    "System.Text.Encodings.Web.pdb",
    "System.Text.Json.pdb",
    "System.Numerics.Vectors.pdb"
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
        [Parameter(Mandatory = $true)][string]$Configuration,
        # Package: mods/<mod>/{client,server} plus the manifest at the mod root, what a server drop expects.
        # Flat: client files and the manifest together, what the game's Mods folder and the client ZIP expect.
        [ValidateSet('Package', 'Flat')][string]$Layout = 'Package'
    )

    # Compute *per-variant* paths
    $modSourceDir = "WukongMp.$Mod/bin/$Configuration/netstandard2.0"
    $serverModSourceDir = "WukongMp.$Mod.Serverside/bin/$Configuration/net10.0"
    $reflectionOnlySourceDir = "WukongMp.Api/Game"
    $binariesSourceDir = "Deployment"

    $modDestDir = "mods/WukongMp.$Mod"
    $clientModDestDir = if ($Layout -eq 'Package') { "$modDestDir/client" } else { $modDestDir }
    $serverModDestDir = "$modDestDir/server"
    $reflectionOnlyDestDir = "mods/ReflectionOnly"
    $overridesDestDir = "mods/Overrides"

    # Compose the triplets: @( <files>, <sourceDir>, <destDir> )
    $modFiles = @(
        @($manifestFiles, $modSourceDir, $modDestDir),
        @($modFilesCore, $modSourceDir, $clientModDestDir),
        @($cultureFolders, $modSourceDir, $clientModDestDir),
        @($binaryFiles, $binariesSourceDir, $clientModDestDir)
    )

    $devFiles = $modFiles + @(
        @($modFilesDebugCore, $modSourceDir, $clientModDestDir),
        @($reflectionOnlyFiles, $reflectionOnlySourceDir, $reflectionOnlyDestDir)
    )

    # Server files are kept out of Mod/Dev on purpose: those two feed the client mod ZIP and the
    # game's CSharpLoader folder, and a server assembly has no business in either.
    $serverFiles = @(
        ,@($serverModFilesCore, $serverModSourceDir, $serverModDestDir)
    )

    $serverDevFiles = $serverFiles + @(
        ,@($serverModFilesDebugCore, $serverModSourceDir, $serverModDestDir)
    )

    # Return every set so the caller can pick based on configuration and on whether it deploys a server
    return @{
        Mod = $modFiles
        Dev = $devFiles
        Server = $serverFiles
        ServerDev = $serverDevFiles
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
