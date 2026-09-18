<#
.SYNOPSIS
Run a Magenheim Blender tool with the campaign's standard flags and a summarised log.

.DESCRIPTION
Two user-installed Blender addons fail on import in background mode and print about 45
lines of traceback on every invocation. --factory-startup skips addon loading and removes
all of it. The glTF exporter is also extremely chatty, roughly six lines per mesh part, so
full output goes to a log file and only a summary is printed.

Always invoke Blender through this wrapper during the model quality campaign.

.EXAMPLE
tools/blender.ps1 export-model-assets crystal-weapon-sword
tools/blender.ps1 render-staff-icons
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Tool,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ToolArgs,
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe',
    [string]$LogDirectory,
    [int]$Tail = 12
)
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot ($Tool -replace '\.py$', '') 
$script = "$script.py"
if (!(Test-Path -LiteralPath $script)) { throw "No such Blender tool: $script" }
if (!(Test-Path -LiteralPath $Blender)) { throw "Blender not found at $Blender" }

if (!$LogDirectory) { $LogDirectory = Join-Path ([IO.Path]::GetTempPath()) 'magenheim-blender' }
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
$log = Join-Path $LogDirectory ("{0}-{1}.log" -f $Tool, (Get-Date -Format 'yyyyMMdd-HHmmss'))

$arguments = @('--background', '--factory-startup', '--python', $script)
if ($ToolArgs) { $arguments += '--'; $arguments += $ToolArgs }

# Windows PowerShell 5.1 wraps every native stderr line in an ErrorRecord, so under the
# script-scope 'Stop' preference the first one terminates this wrapper before the log is even
# written. Blender writes DeprecationWarnings to stderr on a completely successful run, so a
# passing gate reported a build failure. Let the redirect capture both streams, then decide from
# the log below, which is what this wrapper was already designed to do.
$previousPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try { & $Blender @arguments > $log 2>&1 } finally { $ErrorActionPreference = $previousPreference }
$code = $LASTEXITCODE

# Blender exits 0 even when the script raised, so surface the failure explicitly.
$failed = Select-String -Path $log -Pattern 'Traceback \(most recent call last\)|SystemError|^Error:' -SimpleMatch:$false
$done = (Select-String -Path $log -Pattern '^(EXPORTED|RENDERED|RESCALED|AUTHORED|REVISED|REPAIRED)' ).Count

Write-Host ("{0}: exit={1} completed={2} log={3}" -f $Tool, $code, $done, $log)
if ($failed) {
    Write-Host '--- failure ---'
    Get-Content $log | Select-Object -Last $Tail
    exit 1
}
Get-Content $log | Select-String -Pattern '^(EXPORTED|RENDERED|RESCALED|AUTHORED|REVISED|REPAIRED)|^[A-Z][a-z]+ed \d+' | Select-Object -Last 4
