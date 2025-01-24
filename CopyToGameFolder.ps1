# Define the source and destination directories
$sourceDir = "WukongCSharpMod/bin/Debug/netstandard2.1"
$destDir = "C:\Program Files (x86)\Steam\steamapps\common\BlackMythWukong\b1\Binaries\Win64\CSharpLoader\Mods\WukongCSharpMod"

# Create the destination directory if it doesn't exist
if (!(Test-Path -Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force
}

# Define the files to copy
$files = @("WukongCSharpMod.dll", "WukongCSharpMod.pdb", "WukongCSharpMod.deps.json")

# Copy each file to the destination directory
foreach ($file in $files) {
    $sourceFile = Join-Path -Path $sourceDir -ChildPath $file
    $destFile = Join-Path -Path $destDir -ChildPath $file
    if (Test-Path -Path $sourceFile) {
        Copy-Item -Path $sourceFile -Destination $destFile -Force
        Write-Output "Copied $file to $destDir"
    } else {
        Write-Output "$file does not exist in $sourceDir"
    }
}
