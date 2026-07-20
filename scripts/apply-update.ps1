#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,
    [string]$ServiceName = "LocalMock",
    [int]$DelaySeconds = 2
)

$ErrorActionPreference = "Stop"
$logPath = Join-Path $env:TEMP "LocalMock-update.log"

function Write-UpdateLog([string]$Message) {
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Add-Content -Path $logPath -Value $line -Encoding UTF8
}

try {
    Write-UpdateLog "Update started. Zip=$ZipPath InstallDir=$InstallDir"

    if (-not (Test-Path $ZipPath)) {
        throw "Zip not found: $ZipPath"
    }

    Start-Sleep -Seconds $DelaySeconds

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-UpdateLog "Stopping service $ServiceName..."
        if ($service.Status -ne "Stopped") {
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        }

        $deadline = (Get-Date).AddSeconds(60)
        do {
            Start-Sleep -Seconds 1
            $service.Refresh()
        } while ($service.Status -ne "Stopped" -and (Get-Date) -lt $deadline)

        if ($service.Status -ne "Stopped") {
            throw "Service did not stop in time."
        }
    }

    $staging = Join-Path $env:TEMP ("LocalMock-update-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $staging -Force | Out-Null

    try {
        Write-UpdateLog "Extracting zip to $staging"
        Expand-Archive -Path $ZipPath -DestinationPath $staging -Force

        if (-not (Test-Path $InstallDir)) {
            New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        }

        $preserveNames = @("appsettings.json", "appsettings.Production.json", "appsettings.Development.json")

        Get-ChildItem -Path $staging -Force | ForEach-Object {
            $dest = Join-Path $InstallDir $_.Name
            if ($preserveNames -contains $_.Name -and (Test-Path $dest)) {
                Write-UpdateLog "Preserving existing $($_.Name)"
                return
            }
            if (Test-Path $dest) {
                Remove-Item -Path $dest -Recurse -Force
            }
            Copy-Item -Path $_.FullName -Destination $dest -Recurse -Force
        }

        Write-UpdateLog "Files copied to $InstallDir"
    }
    finally {
        if (Test-Path $staging) {
            Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    if ($service) {
        Write-UpdateLog "Starting service $ServiceName..."
        Start-Service -Name $ServiceName -ErrorAction Stop
    }

    Write-UpdateLog "Update completed successfully."
}
catch {
    Write-UpdateLog ("Update FAILED: " + $_.Exception.Message)
    throw
}
finally {
    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
    }
}
