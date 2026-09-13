<#
.SYNOPSIS
    Publishes a local build and mirrors the executable to any extra folders you test from.

.DESCRIPTION
    Wraps `dotnet publish` so the repo's own output (obj\<Configuration>\publish) is always
    refreshed, then copies the single-file executable to any folders passed in -Also. A folder
    that has no config yet is seeded with the repo output's appsettings.json, so a fresh test
    folder does not ask you to redo the capture-region setup.

    The executable cannot be overwritten while the app is running, so the script checks first and
    stops rather than failing halfway. Pass -Stop to close a running instance for you.

.EXAMPLE
    .\scripts\build-local.ps1
    Publishes and refreshes obj\Release\publish\RuneshapePriceChecker.exe.

.EXAMPLE
    .\scripts\build-local.ps1 -Stop -Also E:\Git\RuneshapeBuilds\v4 -FreshLibrary
    Closes the running app, publishes, and drops a copy in v4 with an empty rune library.

.EXAMPLE
    .\scripts\build-local.ps1 -Installed -Stop
    Refreshes the installed copy under %LOCALAPPDATA% so an existing shortcut runs this build.
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",

    # Extra folders to receive the published executable.
    [string[]]$Also = @(),

    # Also refresh the installed copy, so an existing Start menu shortcut runs this build.
    [switch]$Installed,

    # Close a running instance that would otherwise lock the executable.
    [switch]$Stop,

    # Delete rune-catalog.json in the output folders, so the rune library resets to the shipped
    # seed (ocr/rune-seed.json) instead of carrying this machine's sightings forward.
    [switch]$FreshLibrary,

    # Skip the csproj's post-publish test and zip targets.
    [switch]$NoTests
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "RuneshapePriceChecker.csproj"
$publishDir = Join-Path $repoRoot "obj\$Configuration\publish"
$exeName = "RuneshapePriceChecker.exe"

if ($Installed) {
    $installedDir = Join-Path $env:LOCALAPPDATA "RuneshapePriceChecker"
    if (-not (Test-Path -LiteralPath (Join-Path $installedDir $exeName))) {
        throw "No installed copy found at $installedDir."
    }
    $Also += $installedDir
}

# Every folder this run will write an executable into.
$targets = @($publishDir) + $Also
$targetExes = $targets | ForEach-Object {
    try { [System.IO.Path]::GetFullPath((Join-Path $_ $exeName)) } catch { Join-Path $_ $exeName }
}

# Only an instance running from one of those folders can block the write. An instance started
# from anywhere else — the installed copy, an older test folder — is none of this run's business.
$blocking = @(
    Get-Process -Name "RuneshapePriceChecker" -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($targetExes -contains [System.IO.Path]::GetFullPath($_.Path)) }
)
if ($blocking.Count -gt 0) {
    if ($Stop) {
        Write-Host "Stopping $($blocking.Count) instance(s) holding a target executable..." -ForegroundColor Yellow
        $blocking | Stop-Process -Force
        Start-Sleep -Milliseconds 800
    }
    else {
        $paths = ($blocking | ForEach-Object { $_.Path }) -join ", "
        throw "RuneshapePriceChecker is running from a folder this build writes to ($paths). Close it, or re-run with -Stop."
    }
}

$arguments = @("publish", $project, "-c", $Configuration, "-r", $Runtime, "--nologo")
if ($NoTests) { $arguments += "-p:SkipTest=true" }

Write-Host "dotnet $($arguments -join ' ')" -ForegroundColor Cyan
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

$publishedExe = Join-Path $publishDir $exeName
if (-not (Test-Path -LiteralPath $publishedExe)) { throw "Expected a published executable at $publishedExe but it is not there." }

$outputs = @($publishDir)

foreach ($destination in $Also) {
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $publishedExe -Destination (Join-Path $destination $exeName) -Force

    # Seed settings so a new folder does not trigger the first-run setup flow again.
    $destinationConfig = Join-Path $destination "config"
    New-Item -ItemType Directory -Path $destinationConfig -Force | Out-Null
    $settings = Join-Path $destinationConfig "appsettings.json"
    $sourceSettings = Join-Path $publishDir "config\appsettings.json"
    if ((-not (Test-Path -LiteralPath $settings)) -and (Test-Path -LiteralPath $sourceSettings)) {
        Copy-Item -LiteralPath $sourceSettings -Destination $settings
        Write-Host "  seeded settings from the repo output" -ForegroundColor DarkGray
    }

    $outputs += $destination
}

if ($FreshLibrary) {
    foreach ($folder in $outputs) {
        $catalog = Join-Path $folder "config\rune-catalog.json"
        if (Test-Path -LiteralPath $catalog) {
            Remove-Item -LiteralPath $catalog -Force
            Write-Host "  reset the rune library in $folder to the shipped seed" -ForegroundColor DarkGray
        }
    }
}

$version = (Get-Item -LiteralPath $publishedExe).LastWriteTime
Write-Host ""
Write-Host "Published $exeName ($version):" -ForegroundColor Green
foreach ($folder in $outputs) { Write-Host "  $folder" }
