#Requires -Version 5.1
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "artifacts\publish"
$zipPath = Join-Path $repoRoot "artifacts\LocalMock-win-x64.zip"

Write-Host "Publishing LocalMock ($Configuration, win-x64, framework-dependent)..."

& (Join-Path $PSScriptRoot "build-ui.ps1")
if ($LASTEXITCODE -ne 0) {
    throw "Vue interface build failed."
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

$publishArgs = @(
    "publish", (Join-Path $repoRoot "LocalMock.csproj"),
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "false",
    "-o", $publishDir
)

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

foreach ($name in @("mocks.json", "appsettings.Development.json", "nuget.config", "web.config")) {
    $unwanted = Join-Path $publishDir $name
    if (Test-Path -LiteralPath $unwanted) {
        Remove-Item -LiteralPath $unwanted -Force
    }
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Publish OK."
Write-Host "  Folder: $publishDir"
Write-Host "  Zip:    $zipPath"
Write-Host "Upload the zip as a GitHub Release asset named LocalMock-win-x64.zip"
