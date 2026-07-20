#Requires -Version 5.1
#Requires -RunAsAdministrator
param(
    [string]$SourceDir = "",
    [string]$InstallDir = "C:\Program Files\LocalMock",
    [string]$ServiceName = "LocalMock",
    [string]$DisplayName = "Local Mock"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $SourceDir = Join-Path $repoRoot "artifacts\publish"
}

$exePath = Join-Path $InstallDir "LocalMock.exe"
$dataDir = Join-Path $env:ProgramData "LocalMock"

if (-not (Test-Path (Join-Path $SourceDir "LocalMock.exe"))) {
    throw "LocalMock.exe not found in '$SourceDir'. Run scripts\publish.ps1 first, or pass -SourceDir."
}

Write-Host "Installing LocalMock service..."
Write-Host "  Source:  $SourceDir"
Write-Host "  Install: $InstallDir"
Write-Host "  Data:    $dataDir"

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

$preserveAppSettings = Test-Path (Join-Path $InstallDir "appsettings.json")
Get-ChildItem -Path $SourceDir -Force | ForEach-Object {
    $dest = Join-Path $InstallDir $_.Name
    if ($_.Name -eq "appsettings.json" -and $preserveAppSettings) {
        Write-Host "Keeping existing appsettings.json"
        return
    }
    if (Test-Path $dest) {
        Remove-Item -Path $dest -Recurse -Force
    }
    Copy-Item -Path $_.FullName -Destination $dest -Recurse -Force
}

$seedMocks = Join-Path $SourceDir "mocks.json"
$targetMocks = Join-Path $dataDir "mocks.json"
if ((Test-Path $seedMocks) -and -not (Test-Path $targetMocks)) {
    Copy-Item -Path $seedMocks -Destination $targetMocks -Force
    Write-Host "Seeded mocks.json into ProgramData (first install)."
}

New-Service `
    -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName $DisplayName `
    -Description "API e UI LocalMock para mocks HTTP locais." `
    -StartupType Automatic | Out-Null

Start-Service -Name $ServiceName
Start-Sleep -Seconds 2

$svc = Get-Service -Name $ServiceName
Write-Host "Service status: $($svc.Status)"
Write-Host "UI: http://localhost:5183/ui/"
