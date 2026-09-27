#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendDir = Join-Path $repoRoot "frontend"

if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) {
    throw "pnpm is required to build the Vue interface. Install Node.js and pnpm, then run this script again."
}

Push-Location $frontendDir
try {
    pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw "pnpm install failed with exit code $LASTEXITCODE" }

    pnpm run build
    if ($LASTEXITCODE -ne 0) { throw "Vue build failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}
