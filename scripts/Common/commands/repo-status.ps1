<#
.SYNOPSIS
  Show the current Git status for PromptMeUp.
#>
[CmdletBinding()]
param([switch]$Help)

if ($Help) { Get-Help $PSCommandPath -Detailed; return }
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))

& (Get-Command git -ErrorAction Stop).Source -C $root status --short --branch
if ($LASTEXITCODE -ne 0) { throw "git status failed with exit code $LASTEXITCODE." }
