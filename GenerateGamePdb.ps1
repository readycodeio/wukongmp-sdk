#!powershell.exe -ExecutionPolicy Bypass -File

# For all files in GameDll folder...
$gameDllPath = "./WukongMp.Api/Game"

Get-ChildItem -Path $gameDllPath -Filter "*.dll" | ForEach-Object {
    $dllFile = $_.FullName
    Write-Output "Generating PDB for $dllFile"
    ilspycmd -genpdb "$dllFile" -r "$gameDllPath"
}
