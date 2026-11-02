<#
.SYNOPSIS
    Publishes a built installer to the download server and updates the channel's latest.json.

.DESCRIPTION
    Takes the newest installer from Installer\out (or -InstallerPath), uploads it to
    <RemoteRoot>/<Channel>/<Version>/ on the server, verifies its SHA-256 there and then
    replaces <RemoteRoot>/<Channel>/latest.json, which the app reads for the update check.
    An already published version is never overwritten. A build suffix after "_Installer"
    (e.g. "_masterOF16") makes a version of its own (0.4.0-beta.masterOF16).

.EXAMPLE
    .\Scripts\publish_release.ps1
    .\Scripts\publish_release.ps1 -Channel stable
    .\Scripts\publish_release.ps1 -InstallerPath .\Installer\out\GameTimeNext_v1.3.0-beta_Installer.exe
#>
param(
    [ValidateSet("beta", "stable")]
    [string]$Channel = "beta",

    [string]$InstallerPath
)

$ErrorActionPreference = "Stop"

# ------------------------------------------------------------
# Configuration
# ------------------------------------------------------------
$Server     = "clsdeploy@cle1"                             # SSH user and host of the download server
$KeyFile    = "$HOME\.ssh\id_ed25519_2026"                 # private key of clsdeploy
$RemoteRoot = "/srv/cls-downloads/gtn"                     # host folder, mounted into Caddy as /srv/gtn
$BaseUrl    = "https://dl.eval.cryloud-studios.com/gtn"    # public URL of $RemoteRoot (Pangolin resource)

$SshOptions = @("-i", $KeyFile, "-o", "BatchMode=yes", "-o", "ConnectTimeout=15")

# ------------------------------------------------------------
# Helpers
# ------------------------------------------------------------
function Invoke-Ssh([string]$command) {
    $output = & ssh @SshOptions $Server $command
    if ($LASTEXITCODE -ne 0) { throw "ssh failed (exit code $LASTEXITCODE): $command" }
    return $output
}

function Invoke-Scp([string]$source, [string]$target) {
    & scp @SshOptions $source "${Server}:$target"
    if ($LASTEXITCODE -ne 0) { throw "scp failed (exit code $LASTEXITCODE): $source" }
}

function Write-Step([string]$text) {
    Write-Host "==> $text" -ForegroundColor Cyan
}

# ------------------------------------------------------------
# Checks
# ------------------------------------------------------------
if ($Server.Contains("<") -or $BaseUrl.Contains("<")) {
    throw "Set `$Server and `$BaseUrl in the configuration block of this script first."
}
if (-not (Test-Path $KeyFile)) { throw "SSH key not found: $KeyFile" }
foreach ($tool in "ssh", "scp") {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool not found. Install the Windows feature 'OpenSSH Client'."
    }
}

# ------------------------------------------------------------
# Installer and version
# ------------------------------------------------------------
if ($InstallerPath) {
    $installer = Get-Item $InstallerPath
} else {
    $installer = Get-ChildItem (Join-Path $PSScriptRoot "..\Installer\out") -Filter "*_v*_Installer*.exe" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $installer) { throw "No installer found in Installer\out. Compile the Inno Setup script first." }
}

# <App>_v<version>_Installer[_<build>].exe, e.g. GameTimeNext_v0.4.0-beta_Installer_masterOF16.exe
$match = [regex]::Match($installer.Name, '^.+_v(?<version>[0-9A-Za-z.+-]+)_Installer(?:_(?<build>[0-9A-Za-z.-]+))?\.exe$')
if (-not $match.Success) { throw "Cannot read the version from the file name: $($installer.Name)" }
$baseVersion = $match.Groups["version"].Value
$build       = if ($match.Groups["build"].Success) { $match.Groups["build"].Value } else { $null }

# Each build suffix is a version of its own: 0.4.0-beta + masterOF16 -> 0.4.0-beta.masterOF16 (SemVer pre-release)
if ($build) {
    $separator = if ($baseVersion.Contains("-")) { "." } else { "-" }
    $version   = "$baseVersion$separator$build"
} else {
    $version = $baseVersion
}

$sha256 = (Get-FileHash $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()

$channelDir    = "$RemoteRoot/$Channel"
$versionDir    = "$channelDir/$version"
$remoteFile    = "$versionDir/$($installer.Name)"
$remoteLatest  = "$channelDir/latest.json"

Write-Host ""
Write-Host "Installer : $($installer.FullName)"
Write-Host "Version   : $version"
if ($build) { Write-Host "            (base $baseVersion, build $build)" }
Write-Host "Channel   : $Channel"
Write-Host "Size      : $([math]::Round($installer.Length / 1MB, 1)) MB"
Write-Host "SHA-256   : $sha256"
Write-Host "Target    : ${Server}:$remoteFile"
Write-Host ""

if ((Read-Host "Publish? (y/n)") -ne "y") {
    Write-Host "Cancelled."
    exit 1
}

# ------------------------------------------------------------
# Upload installer
# ------------------------------------------------------------
Write-Step "Creating version folder"
Invoke-Ssh "if [ -e '$versionDir' ]; then echo 'Version $version already exists in channel $Channel' >&2; exit 3; fi; mkdir -p '$versionDir'" | Out-Null

Write-Step "Uploading installer"
Invoke-Scp $installer.FullName $remoteFile

Write-Step "Verifying SHA-256 on the server"
$remoteHash = ((Invoke-Ssh "sha256sum '$remoteFile'") -split '\s+')[0]
if ($remoteHash -ne $sha256) {
    Invoke-Ssh "rm -rf '$versionDir'" | Out-Null
    throw "SHA-256 mismatch after upload (local $sha256, server $remoteHash). Upload removed."
}

# ------------------------------------------------------------
# Update latest.json (last step: clients only see the new version now)
# ------------------------------------------------------------
$manifest = [ordered]@{
    channel     = $Channel
    version     = $version
    baseVersion = $baseVersion
    build       = $build
    publishedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", [Globalization.CultureInfo]::InvariantCulture)
    download    = [ordered]@{
        url      = "$BaseUrl/$Channel/$version/$($installer.Name)"
        fileName = $installer.Name
        size     = $installer.Length
        sha256   = $sha256
    }
} | ConvertTo-Json -Depth 3

$localManifest = Join-Path ([IO.Path]::GetTempPath()) "latest.json"
[IO.File]::WriteAllText($localManifest, $manifest, [Text.UTF8Encoding]::new($false))

Write-Step "Publishing latest.json"
Invoke-Scp $localManifest "$remoteLatest.tmp"
Invoke-Ssh "mv -f '$remoteLatest.tmp' '$remoteLatest'" | Out-Null
Remove-Item $localManifest

# ------------------------------------------------------------
# Check via the public URL
# ------------------------------------------------------------
Write-Step "Checking public URL"
try {
    $published = Invoke-RestMethod -Uri "$BaseUrl/$Channel/latest.json" -Headers @{ "Cache-Control" = "no-cache" } -TimeoutSec 15
    if ($published.version -eq $version) {
        Write-Host "OK: $BaseUrl/$Channel/latest.json shows $version" -ForegroundColor Green
    } else {
        Write-Warning "Public latest.json shows '$($published.version)' instead of '$version'."
    }
} catch {
    Write-Warning "Public URL not reachable: $($_.Exception.Message). Check the Pangolin resource (authentication must be off)."
}

Write-Host ""
Write-Host "Published $version to channel '$Channel'." -ForegroundColor Green
