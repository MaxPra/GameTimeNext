<#
.SYNOPSIS
    Interactive menu to reset a test machine for installation and migration tests of arcora.

.DESCRIPTION
    Start it without arguments and pick from the menu:
      - Uninstall every installed version (Inno Setup per user / system wide, old MSI), found by the AppId.
      - Delete the data of arcora and of older versions (Documents\GameTimeNXT of the 0.x betas, ...).
      - Copy a mock data set from TestData\<name> into the user folders.
      - Start an installer.
      - A test run doing all of it in one go.

    Running arcora processes are ended by their path (installation folder), never by image name.

    Mock data sets are folders next to this script in TestData\<name> (TestData\Generators holds the scripts that
    create them). Their content is copied over the user folders as it is:
      TestData\<name>\AppData\...       -> %APPDATA%
      TestData\<name>\LocalAppData\...  -> %LOCALAPPDATA%
      TestData\<name>\Documents\...     -> Documents

    To use it on another machine, copy this script together with its TestData folder.
    -WhatIf shows what would be done without changing anything.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Reset-TestInstall.ps1
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'

$Publisher    = 'CryloudStudios'
$AppName      = 'arcora'
$AppGuid      = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}'
$InstallDir   = Join-Path $env:LOCALAPPDATA "Programs\$Publisher\$AppName"
$Documents    = [Environment]::GetFolderPath('MyDocuments')
$TestDataDir  = Join-Path $PSScriptRoot 'TestData'
$FirewallRule = "$AppName Remote Monitoring ($env:USERNAME)"

# --- Output helpers ---------------------------------------------------------------------------------------------

function Write-Title([string]$Text) { Write-Host "`n== $Text" -ForegroundColor Cyan }
function Write-Done([string]$Text) { Write-Host "   $Text" -ForegroundColor Green }
function Write-Skip([string]$Text) { Write-Host "   $Text" -ForegroundColor DarkGray }
function Write-Fail([string]$Text) { Write-Host "   $Text" -ForegroundColor Yellow }

function Confirm-Action([string]$Question) {
    if ($WhatIfPreference) { return $true }
    $answer = Read-Host "$Question (y/n)"
    return $answer -in @('y', 'j', 'yes', 'ja')
}

# Shows a numbered list and returns the chosen item, or $null for "back".
function Select-Item([string]$Title, [object[]]$Items, [scriptblock]$Label) {
    Write-Title $Title
    for ($i = 0; $i -lt $Items.Count; $i++) { Write-Host ("   [{0}] {1}" -f ($i + 1), (& $Label $Items[$i])) }
    Write-Host '   [0] Back'
    while ($true) {
        $choice = Read-Host 'Choice'
        if ($choice -eq '0' -or $choice -eq '') { return $null }
        $n = 0
        if ([int]::TryParse($choice, [ref]$n) -and $n -ge 1 -and $n -le $Items.Count) { return $Items[$n - 1] }
        Write-Fail 'Invalid choice.'
    }
}

# --- State ------------------------------------------------------------------------------------------------------

# Uninstall entries of all versions: Inno Setup ("<AppId>_is1", also the old AppId with a double "}") and the old MSI.
function Get-UninstallEntries {
    $roots = @(
        @{ Hive = 'HKCU'; View = [Microsoft.Win32.RegistryView]::Default; Base = [Microsoft.Win32.RegistryHive]::CurrentUser }
        @{ Hive = 'HKLM64'; View = [Microsoft.Win32.RegistryView]::Registry64; Base = [Microsoft.Win32.RegistryHive]::LocalMachine }
        @{ Hive = 'HKLM32'; View = [Microsoft.Win32.RegistryView]::Registry32; Base = [Microsoft.Win32.RegistryHive]::LocalMachine }
    )
    foreach ($root in $roots) {
        $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($root.Base, $root.View)
        foreach ($name in @("$AppGuid`_is1", "$AppGuid}_is1", $AppGuid)) {
            $key = $base.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$name")
            if ($null -eq $key) { continue }
            [pscustomobject]@{
                Hive            = $root.Hive
                KeyName         = $name
                DisplayName     = $key.GetValue('DisplayName')
                DisplayVersion  = $key.GetValue('DisplayVersion')
                UninstallString = $key.GetValue('UninstallString')
                IsMsi           = ($name -eq $AppGuid)
                IsSystemWide    = ($root.Hive -ne 'HKCU')
            }
            $key.Close()
        }
        $base.Close()
    }
}

# Data folders of arcora and of older versions that exist on this machine
function Get-DataFolders {
    $folders = @(
        # arcora 1.x
        (Join-Path $env:APPDATA "$Publisher\$AppName"),
        (Join-Path $env:LOCALAPPDATA "$Publisher\$AppName"),
        # Logs of the installer (old MSI / Inno uninstall)
        (Join-Path $env:LOCALAPPDATA $AppName),
        # Older versions
        (Join-Path $env:LOCALAPPDATA "$Publisher\GameTimeNext"),
        (Join-Path $env:LOCALAPPDATA 'GameTimeNext')
    )
    # 0.x betas: Documents\GameTimeNXT (and GameTimeNXT_dev of debug builds)
    $folders += @(Get-ChildItem -LiteralPath $Documents -Directory -Filter 'GameTimeNXT*' -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName)
    $folders | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -Unique
}

# Processes started from the installation folder (arcora.exe and the web app RemoteMonitoring.exe)
function Get-AppProcesses {
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -and $_.Path.StartsWith($InstallDir, [StringComparison]::OrdinalIgnoreCase) } catch { $false }
    }
}

function Test-AutostartEntry {
    $null -ne (Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $AppName -ErrorAction SilentlyContinue)
}

function Test-FirewallRule {
    try { $null -ne (Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue) } catch { $false }
}

function Get-MockDataSets {
    if (-not (Test-Path -LiteralPath $TestDataDir)) { return @() }
    @(Get-ChildItem -LiteralPath $TestDataDir -Directory | Where-Object Name -ne 'Generators')
}

function Show-Status {
    Write-Title 'Current state'
    $entries = @(Get-UninstallEntries)
    if ($entries) { $entries | ForEach-Object { Write-Host "   Installed      $($_.DisplayName) $($_.DisplayVersion) [$($_.Hive)$(if ($_.IsMsi) { ', MSI' })]" } }
    else { Write-Skip 'Installed      nothing' }

    $processes = @(Get-AppProcesses)
    if ($processes) { $processes | ForEach-Object { Write-Host "   Running        $($_.Name) (PID $($_.Id))" } }

    $folders = @(Get-DataFolders)
    if ($folders) { $folders | ForEach-Object { Write-Host "   Data           $_" } }
    else { Write-Skip 'Data           none' }

    if (Test-AutostartEntry) { Write-Host '   Autostart      on' }
    if (Test-FirewallRule) { Write-Host "   Firewall rule  $FirewallRule" }
}

# --- Actions ----------------------------------------------------------------------------------------------------

function Stop-AppProcesses {
    foreach ($p in @(Get-AppProcesses)) {
        if ($PSCmdlet.ShouldProcess("$($p.Name) (PID $($p.Id))", 'Stop process')) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
            Write-Done "ended $($p.Name) (PID $($p.Id))"
        }
    }
}

function Invoke-Uninstall {
    Write-Title 'Uninstall'
    Stop-AppProcesses

    $entries = @(Get-UninstallEntries)
    if (-not $entries) { Write-Skip 'nothing installed' }

    foreach ($e in $entries) {
        $label = "$($e.DisplayName) $($e.DisplayVersion) [$($e.Hive)]"
        if (-not $PSCmdlet.ShouldProcess($label, 'Uninstall')) { continue }

        if ($e.IsMsi) {
            $file = Join-Path $env:SystemRoot 'System32\msiexec.exe'
            $arguments = "/x $AppGuid /passive /norestart"
        }
        else {
            # UninstallString is the quoted path of unins000.exe
            $file = ($e.UninstallString -replace '^"([^"]+)".*$', '$1')
            $arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
        }

        if (-not $e.IsMsi -and -not (Test-Path -LiteralPath $file)) {
            if ($e.IsSystemWide) { Write-Fail "Uninstaller of $label is missing ($file); remove the system wide entry as administrator."; continue }
            Remove-Item -LiteralPath "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$($e.KeyName)" -Recurse -Force
            Write-Fail "Uninstaller of $label was missing; removed its registry entry."
            continue
        }

        $startArgs = @{ FilePath = $file; ArgumentList = $arguments; Wait = $true; PassThru = $true }
        if ($e.IsSystemWide) { $startArgs.Verb = 'RunAs' }
        $proc = Start-Process @startArgs
        if ($proc.ExitCode -eq 0) { Write-Done "uninstalled $label" }
        else { Write-Fail "uninstall of $label ended with exit code $($proc.ExitCode)" }
    }

    if ((Test-Path -LiteralPath $InstallDir) -and $PSCmdlet.ShouldProcess($InstallDir, 'Delete leftovers')) {
        # The Inno uninstaller deletes itself with a short delay
        Start-Sleep -Seconds 2
        Remove-Item -LiteralPath $InstallDir -Recurse -Force
        Write-Done "deleted $InstallDir"
    }
}

function Invoke-DeleteData {
    Write-Title 'Delete data'
    Stop-AppProcesses

    $folders = @(Get-DataFolders)
    if (-not $folders) { Write-Skip 'no data folders' }
    foreach ($f in $folders) {
        if ($PSCmdlet.ShouldProcess($f, 'Delete folder')) {
            Remove-Item -LiteralPath $f -Recurse -Force
            Write-Done "deleted $f"
        }
    }

    # Empty publisher folders (other contents, e.g. of other apps, stay)
    foreach ($parent in @((Join-Path $env:APPDATA $Publisher), (Join-Path $env:LOCALAPPDATA $Publisher), (Join-Path $env:LOCALAPPDATA "Programs\$Publisher"))) {
        if ((Test-Path -LiteralPath $parent) -and -not (Get-ChildItem -LiteralPath $parent -Force) -and $PSCmdlet.ShouldProcess($parent, 'Delete empty folder')) {
            Remove-Item -LiteralPath $parent -Force
        }
    }

    if ((Test-AutostartEntry) -and $PSCmdlet.ShouldProcess($AppName, 'Remove autostart entry')) {
        Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $AppName
        Write-Done 'removed autostart entry'
    }

    if ((Test-FirewallRule) -and (Confirm-Action "   Remove the firewall rule '$FirewallRule' (asks for administrator rights)?")) {
        if ($PSCmdlet.ShouldProcess($FirewallRule, 'Remove firewall rule')) {
            $cmd = "Remove-NetFirewallRule -DisplayName '$FirewallRule'"
            Start-Process powershell -Verb RunAs -Wait -ArgumentList '-NoProfile', '-Command', $cmd
            Write-Done "removed firewall rule '$FirewallRule'"
        }
    }
}

function Select-MockData {
    $sets = Get-MockDataSets
    if (-not $sets) { Write-Fail "No mock data sets in $TestDataDir."; return $null }
    Select-Item 'Mock data sets' $sets { param($s) $s.Name }
}

function Invoke-CopyMockData($Set) {
    Write-Title "Mock data '$($Set.Name)'"
    $targets = [ordered]@{ 'AppData' = $env:APPDATA; 'LocalAppData' = $env:LOCALAPPDATA; 'Documents' = $Documents }
    $copied = $false
    foreach ($part in $targets.Keys) {
        $source = Join-Path $Set.FullName $part
        if (-not (Test-Path -LiteralPath $source)) { continue }
        $copied = $true
        if ($PSCmdlet.ShouldProcess("$source -> $($targets[$part])", 'Copy')) {
            Copy-Item -Path (Join-Path $source '*') -Destination $targets[$part] -Recurse -Force
            Get-ChildItem -LiteralPath $source -Directory | ForEach-Object { Write-Done "copied $(Join-Path $targets[$part] $_.Name)" }
        }
    }
    if (-not $copied) { Write-Fail "No AppData, LocalAppData or Documents folder in $($Set.FullName)." }
}

# Installers found in the usual places, newest first
function Get-Installers {
    $places = @($PSScriptRoot, (Join-Path $PSScriptRoot '..\Installer\out'), (Join-Path $env:USERPROFILE 'Downloads'))
    $places | Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -File -Filter '*.exe' } |
        Where-Object { $_.Name -match '(?i)(arcora|GameTimeNext).*(installer|setup)' } |
        Sort-Object LastWriteTime -Descending
}

function Select-Installer {
    $found = @(Get-Installers)
    $items = @($found) + @('Enter a path ...')
    $choice = Select-Item 'Installers' $items { param($i) if ($i -is [IO.FileInfo]) { "$($i.Name)   ($($i.DirectoryName))" } else { $i } }
    if ($null -eq $choice) { return $null }
    if ($choice -is [IO.FileInfo]) { return $choice.FullName }

    $path = (Read-Host 'Path of the installer').Trim('"', ' ')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Write-Fail "Not found: $path"; return $null }
    return $path
}

function Invoke-Installer([string]$Path) {
    Write-Title 'Installer'
    if ($PSCmdlet.ShouldProcess($Path, 'Start')) {
        Write-Host "   started $Path, waiting until it is closed ..."
        Start-Process -FilePath $Path -Wait
        Write-Done 'installer closed'
    }
}

function Invoke-TestRun {
    # Ask everything first, so the run itself needs no input
    $set = Select-MockData
    if ($null -eq $set) { return }
    $old = Select-Installer
    Write-Host "`n   Optional: installer to start afterwards (e.g. the new version), 0 = none"
    $new = Select-Installer

    Write-Title 'Test run'
    Write-Host '   1. uninstall arcora and delete all data (also of older versions)'
    if ($old) { Write-Host "   2. install $(Split-Path $old -Leaf)" }
    Write-Host "   3. copy mock data '$($set.Name)'"
    if ($new) { Write-Host "   4. install $(Split-Path $new -Leaf)" }
    if (-not (Confirm-Action '   Start?')) { return }

    Invoke-Uninstall
    Invoke-DeleteData
    if ($old) {
        Invoke-Installer $old
        # The installer may start the app at the end; it must not run while the data is replaced
        Stop-AppProcesses
    }
    Invoke-CopyMockData $set
    if ($new) { Invoke-Installer $new }
}

# --- Menu -------------------------------------------------------------------------------------------------------

while ($true) {
    Show-Status

    Write-Title 'Menu'
    Write-Host '   [1] Reset completely (uninstall + delete all data)'
    Write-Host '   [2] Uninstall only'
    Write-Host '   [3] Delete data only'
    Write-Host '   [4] Copy mock data'
    Write-Host '   [5] Start an installer'
    Write-Host '   [6] Test run (reset, old version, mock data, new version)'
    Write-Host '   [0] Exit'
    $choice = Read-Host 'Choice'

    try {
        switch ($choice) {
            '1' { if (Confirm-Action '   Uninstall arcora and delete all data?') { Invoke-Uninstall; Invoke-DeleteData } }
            '2' { if (Confirm-Action '   Uninstall arcora?') { Invoke-Uninstall } }
            '3' { if (Confirm-Action '   Delete all data of arcora and older versions?') { Invoke-DeleteData } }
            '4' {
                $set = Select-MockData
                if ($set) {
                    if (@(Get-AppProcesses)) { Write-Fail 'arcora is running; it is ended first.'; Stop-AppProcesses }
                    Invoke-CopyMockData $set
                }
            }
            '5' { $path = Select-Installer; if ($path) { Invoke-Installer $path } }
            '6' { Invoke-TestRun }
            '0' { return }
            default { Write-Fail 'Invalid choice.' }
        }
    }
    catch {
        Write-Fail "Error: $($_.Exception.Message)"
    }
}
