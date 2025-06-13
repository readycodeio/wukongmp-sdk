#!powershell.exe -ExecutionPolicy Bypass -File

$solutionName = "WukongCSharpMod"
$zipName = "WukongMp"

# Define the source and destination directories
$modSourceDir = "WukongMp.$ModVariant/bin/$Configuration/netstandard2.1"
$reflectionOnlySourceDir = "WukongMp.Api/Game"
$saveSourceDir = "Deployment"

$modDestDir = "Mods/WukongMpMod"
$reflectionOnlyDir = "Mods/ReflectionOnly"
$saveDestDir = "Mods/WukongMpMod"
$overrideDestDir = "Mods/Overrides"

# Define the files to copy
$modFiles = @(
    "WukongMp.Api.dll", 
    "WukongMp.Api.pdb", 
    "WukongMpMod.dll", 
    "WukongMpMod.pdb", 
    "ReadyM.Api.dll", 
    "ReadyM.Api.pdb", 
    "ReadyM.Api.Multiplayer.dll", 
    "ReadyM.Api.Multiplayer.pdb", 
    "ReadyM.Relay.Client.dll", 
    "ReadyM.Relay.Client.pdb", 
    "ReadyM.Relay.Common.dll", 
    "ReadyM.Relay.Common.pdb", 
    "ReadyM.Relay.Common.Wukong.dll", 
    "ReadyM.Relay.Common.Wukong.pdb",
    "Friflo.Engine.ECS.dll",
    "Friflo.Engine.ECS.Boost.dll",
    "Friflo.Json.Burst.dll",
    "Friflo.Json.Fliox.dll",
    "Friflo.Json.Fliox.Annotation.dll",
    "System.ComponentModel.Annotations.dll"
)
$reflectionOnlyFiles = @(
    "*"
)
$saveFiles = @(
    "ArchiveSaveFile.0.sav",
    "ArchiveSaveFile.9.sav"
)
$overrideFiles = @(
    "LiteNetLib.dll"
)

# List of culture codes
$cultureFolders = @("de", "es", "fr", "pl", "pt", "zh-Hans")

$allFiles = @(
    @($modFiles, $modSourceDir, $modDestDir),
    @($cultureFolders, $modSourceDir, $modDestDir),
    @($reflectionOnlyFiles, $reflectionOnlySourceDir, $reflectionOnlyDir),
    @($saveFiles, $saveSourceDir, $saveDestDir),
    @($overrideFiles, $modSourceDir, $overrideDestDir)
)

function CopyFiles($files, $sourceDir, $destDir) {
    # Create the destination directory if it doesn't exist
    if (!(Test-Path -Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force
    }
    
    # Copy each file to the destination directory
    foreach ($file in $files) {
        $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
        $destFile = Join-Path -Path $destDir -ChildPath $file
        if ($file -eq "*") {
            if (Test-Path -Path $destDir) {
                Remove-Item -Path $destDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $destDir -Force
            Copy-Item -Path $sourceFile -Destination $destDir -Recurse -Force
            Write-Output "Copied $file to $destDir (recursive)"
        } elseif (Test-Path -Path $sourceFile -PathType Leaf) {
            Copy-Item -Path $sourceFile -Destination $destFile -Force
            Write-Output "Copied $file to $destDir"
        } elseif (Test-Path -Path $sourceFile -PathType Container) {
            if (Test-Path -Path $destFile) {
                Remove-Item -Path $destFile -Recurse -Force
            }
            Copy-Item -Path $sourceFile -Destination $destFile -Recurse -Force
            Write-Output "Copied $file to $destDir (recursive)"
        } else {
            Write-Output "[Error] $file does not exist in $sourceDir"
        }
    }
}
