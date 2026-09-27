#Requires -Version 5.1
param([string]$PublishDir = "")

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $PublishDir) {
    $PublishDir = Join-Path $repoRoot "artifacts\publish"
}

$exe = Join-Path $PublishDir "LocalMock.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Publish LocalMock before running the smoke test."
}

$port = Get-Random -Minimum 20000 -Maximum 60000
$dataPath = Join-Path $env:TEMP ("LocalMock-smoke-" + [guid]::NewGuid().ToString("N") + ".json")
$previousPort = [Environment]::GetEnvironmentVariable("LocalMock__Port")
$previousFilePath = [Environment]::GetEnvironmentVariable("Mock__FilePath")
$process = $null

try {
    $env:LocalMock__Port = [string]$port
    $env:Mock__FilePath = $dataPath
    $process = Start-Process -FilePath $exe -ArgumentList "--background" -WorkingDirectory $PublishDir -WindowStyle Hidden -PassThru

    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited) {
            throw "LocalMock exited before its web UI was ready."
        }
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$port/ui/" -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            # The web server is still starting.
        }
    }
    if (-not $ready) {
        throw "LocalMock did not serve its web UI."
    }

    $html = (Invoke-WebRequest -Uri "http://localhost:$port/ui/" -UseBasicParsing).Content
    if ($html -notmatch 'src="\./assets/index-[^"]+\.js"' -or $html -notmatch 'href="\./assets/index-[^"]+\.css"') {
        throw "The Vue manager bundles were not served from /ui/."
    }
    $assets = [regex]::Matches($html, '(?:src|href)="([^\"]+\.(?:js|css)(?:\?[^\"]*)?)"')
    if ($assets.Count -lt 2) {
        throw "The static manager did not include its JavaScript and CSS files."
    }
    foreach ($asset in $assets) {
        $assetUrl = [Uri]::new([Uri]"http://localhost:$port/ui/", $asset.Groups[1].Value)
        $assetResponse = Invoke-WebRequest -Uri $assetUrl -UseBasicParsing
        if ($assetResponse.StatusCode -ne 200) {
            throw "The interface asset was not served: $($asset.Groups[1].Value)"
        }
    }

    $version = Invoke-RestMethod -Uri "http://localhost:$port/api/version/current"
    if (-not ($version.currentVersion -or $version.CurrentVersion)) {
        throw "LocalMock did not report its current version."
    }

    $body = @{ method = "GET"; path = "/smoke"; responseBody = @{ message = "ok" } } | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Method Post -Uri "http://localhost:$port/mock" -ContentType "application/json" -Body $body | Out-Null
    $mock = Invoke-RestMethod -Uri "http://localhost:$port/mock/smoke"
    if ($mock.message -ne "ok" -or -not (Test-Path -LiteralPath $dataPath)) {
        throw "The mock response or data file did not match."
    }

    Write-Host "LocalMock smoke test passed on port $port."
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $dataPath -Force -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable("LocalMock__Port", $previousPort)
    [Environment]::SetEnvironmentVariable("Mock__FilePath", $previousFilePath)
}
