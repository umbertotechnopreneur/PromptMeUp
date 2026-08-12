<#
.SYNOPSIS
  Show the current Git status for PromptMeUp.
#>
[CmdletBinding()]
param([switch]$Help)

if ($Help) { Get-Help $PSCommandPath -Detailed; exit 0 }
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

& (Get-Command git -ErrorAction Stop).Source -C $root status --short --branch
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
