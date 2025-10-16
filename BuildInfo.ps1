#!powershell.exe -ExecutionPolicy Bypass -File

$solutionName = "WukongCSharpMod"
$zipName = "WukongMp"

# Define the source and destination directories
$modSourceDir = "WukongMp.$ModVariant/bin/$Configuration/netstandard2.0"
$reflectionOnlySourceDir = "WukongMp.Api/Game"
$overridesSourceDir = "WukongMp.Api/Game"
$saveSourceDir = "Deployment"

$modDestDir = "Mods/WukongMpMod"
$reflectionOnlyDestDir = "Mods/ReflectionOnly"
$overridesDestDir = "Mods/Overrides"
$saveDestDir = "Mods/WukongMpMod"
$overrideDestDir = "Mods/Overrides"

# Define the files to copy

$modFiles = @(
    "WukongMp.Api.dll",
    "WukongMpMod.dll",
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

$modFilesDebug = @(
    "WukongMp.Api.pdb",
    "WukongMpMod.pdb",
    "ReadyM.Api.pdb",
    "ReadyM.Api.Multiplayer.pdb",
    "ReadyM.Relay.Client.pdb",
    "ReadyM.Relay.Common.pdb",
    "ReadyM.Relay.Common.Wukong.pdb",
    "Friflo.Engine.ECS.pdb",
    "Friflo.Engine.ECS.Boost.pdb"
)

$reflectionOnlyFiles = @(
    "*"
)

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

$saveFiles = @(
    "cacert.pem", # root CAs file for HTTPS support
    "ArchiveSaveFile.9.sav" # new game save
) # at least one file is required (temporarily) so that the game deson't think we are starting the game from Prologue


if ($ModVariant -eq 'PvP')
{
    $saveFiles += "ArchiveSaveFile.0.sav" # PvP save
}

# List of culture codes
$cultureFolders = @("de", "es", "fr", "pl", "pt", "zh-Hans")

$modFiles = @(
    @($modFiles, $modSourceDir, $modDestDir),
    @($cultureFolders, $modSourceDir, $modDestDir),
    @($overridesFiles, $overridesSourceDir, $overridesDestDir),
    @($saveFiles, $saveSourceDir, $saveDestDir)
)

$devFiles = $modFiles + @(
    @($modFilesDebug, $modSourceDir, $modDestDir),
    @($overridesFilesDebug, $overridesSourceDir, $overridesDestDir),
    @($reflectionOnlyFiles, $reflectionOnlySourceDir, $reflectionOnlyDestDir)
)

function CopyFiles($files, $sourceDir, $destDir)
{
    # Create the destination directory if it doesn't exist
    if (!(Test-Path -Path $destDir))
    {
        New-Item -ItemType Directory -Path $destDir -Force
    }

    # Copy each file to the destination directory
    foreach ($file in $files)
    {
        $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
        $destFile = Join-Path -Path $destDir -ChildPath $file
        if ($file -eq "*")
        {
            if (Test-Path -Path $destDir)
            {
                Remove-Item -Path $destDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $destDir -Force
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
