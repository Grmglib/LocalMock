#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)] [string]$ZipPath,
    [Parameter(Mandatory = $true)] [string]$InstallDir,
    [Parameter(Mandatory = $true)] [int]$AppProcessId,
    [Parameter(Mandatory = $true)] [string]$HealthUrl
)

$ErrorActionPreference = "Stop"
$logPath = Join-Path $env:TEMP "LocalMock-update.log"
$staging = Join-Path $env:TEMP ("LocalMock-stage-" + [guid]::NewGuid().ToString("N"))
$fullInstallDir = [IO.Path]::GetFullPath($InstallDir)
$installDir = $fullInstallDir.TrimEnd([IO.Path]::DirectorySeparatorChar)
$backup = Join-Path (Split-Path -Parent $installDir) ("LocalMock-backup-" + [guid]::NewGuid().ToString("N"))
$replaced = $false
$newProcess = $null

function Write-UpdateLog([string]$Message) {
    Add-Content -Path $logPath -Value ("[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message) -Encoding UTF8
}

try {
    if ($installDir -eq [IO.Path]::GetPathRoot($fullInstallDir).TrimEnd([IO.Path]::DirectorySeparatorChar)) {
        throw "The install directory cannot be a drive root."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $installDir "LocalMock.exe"))) {
        throw "LocalMock.exe was not found in the install directory."
    }
    if (-not (Test-Path -LiteralPath $ZipPath)) {
        throw "Update zip was not found."
    }

    New-Item -ItemType Directory -Path $staging | Out-Null
    Expand-Archive -LiteralPath $ZipPath -DestinationPath $staging
    if (-not (Test-Path -LiteralPath (Join-Path $staging "LocalMock.exe")) -or
        -not (Test-Path -LiteralPath (Join-Path $staging "wwwroot\ui\index.html"))) {
        throw "The release zip is missing LocalMock.exe or the web UI."
    }

    Write-UpdateLog "Waiting for LocalMock process $AppProcessId to exit."
    $oldProcess = Get-Process -Id $AppProcessId -ErrorAction SilentlyContinue
    if ($oldProcess) {
        $oldProcess | Wait-Process -Timeout 60
    }

    Move-Item -LiteralPath $installDir -Destination $backup
    $replaced = $true
    New-Item -ItemType Directory -Path $installDir | Out-Null
    Get-ChildItem -LiteralPath $staging -Force | Copy-Item -Destination $installDir -Recurse -Force

    foreach ($name in @("appsettings.json", "appsettings.Production.json", "appsettings.Development.json")) {
        $saved = Join-Path $backup $name
        if (Test-Path -LiteralPath $saved) {
            Copy-Item -LiteralPath $saved -Destination (Join-Path $installDir $name) -Force
        }
    }

    $newProcess = Start-Process -FilePath (Join-Path $installDir "LocalMock.exe") -ArgumentList "--background" -WorkingDirectory $installDir -WindowStyle Hidden -PassThru
    $deadline = (Get-Date).AddSeconds(30)
    $healthy = $false
    do {
        Start-Sleep -Seconds 1
        $newProcess.Refresh()
        if ($newProcess.HasExited) {
            throw "The updated application exited during startup."
        }
        try {
            $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 2
            $healthy = $response.StatusCode -eq 200
        }
        catch {
            # The web server is still starting.
        }
    } while (-not $healthy -and (Get-Date) -lt $deadline)

    if (-not $healthy) {
        throw "The updated application did not start within 30 seconds."
    }

    Write-UpdateLog "Update completed successfully."
    Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction SilentlyContinue
}
catch {
    Write-UpdateLog ("Update failed: " + $_.Exception.Message)
    if ($replaced) {
        if ($newProcess -and -not $newProcess.HasExited) {
            Stop-Process -Id $newProcess.Id -Force -ErrorAction SilentlyContinue
            $newProcess | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
        }
        for ($attempt = 0; $attempt -lt 10 -and (Test-Path -LiteralPath $installDir); $attempt++) {
            try {
                Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction Stop
            }
            catch {
                Start-Sleep -Milliseconds 500
            }
        }
        if (Test-Path -LiteralPath $installDir) {
            throw "Rollback could not clear the new installation. Previous version remains at $backup."
        }
        Move-Item -LiteralPath $backup -Destination $installDir
        Start-Process -FilePath (Join-Path $installDir "LocalMock.exe") -ArgumentList "--background" -WorkingDirectory $installDir -WindowStyle Hidden
        Write-UpdateLog "Previous version restored."
    }
    else {
        $oldProcess = Get-Process -Id $AppProcessId -ErrorAction SilentlyContinue
        if ($oldProcess) {
            $oldProcess | Wait-Process -Timeout 60
        }
        if (Test-Path -LiteralPath (Join-Path $installDir "LocalMock.exe")) {
            Start-Process -FilePath (Join-Path $installDir "LocalMock.exe") -ArgumentList "--background" -WorkingDirectory $installDir -WindowStyle Hidden
            Write-UpdateLog "Existing version restarted."
        }
    }
    throw
}
finally {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
}
