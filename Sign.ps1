#!powershell.exe -ExecutionPolicy Bypass -File

param (
    [switch]$Coop,
    [switch]$PvP
)

if (-not $Coop -and -not $PvP) {
    Write-Host "Please specify at least one of the following options: -Coop, -PvP"
    Exit 1
}

az login

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($Coop) {
    sign.exe code trusted-signing `
        -b "$scriptRoot" `
        -tse "https://wus.codesigning.azure.net" `
        -tscp "ReadyM" `
        -tsa "RC-Signing" `
        "WukongMP-co-op-mod/MakeModFolder.ps1" `
        -v Information
}

if ($PvP) {
    sign.exe code trusted-signing `
        -b "$scriptRoot" `
        -tse "https://wus.codesigning.azure.net" `
        -tscp "ReadyM" `
        -tsa "RC-Signing" `
        "WukongMP-PvP-mod/MakeModFolder.ps1" `
        -v Information
}
