#Requires -Version 5.1
#Requires -RunAsAdministrator
param(
    [string]$InstallDir = "C:\Program Files\LocalMock",
    [string]$ServiceName = "LocalMock",
    [switch]$RemoveData
)

$ErrorActionPreference = "Stop"

$dataDir = Join-Path $env:ProgramData "LocalMock"

Write-Host "Uninstalling LocalMock service..."

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "Service '$ServiceName' removed."
}
else {
    Write-Host "Service '$ServiceName' was not installed."
}

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
    Write-Host "Removed install directory: $InstallDir"
}

if ($RemoveData) {
    if (Test-Path $dataDir) {
        Remove-Item $dataDir -Recurse -Force
        Write-Host "Removed data directory: $dataDir"
    }
}
else {
    Write-Host "Data kept at: $dataDir (use -RemoveData to delete mocks.json)"
}
