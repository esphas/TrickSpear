#Requires -Version 5.1
<#
.SYNOPSIS
  Build TrickSpear.

.EXAMPLE
  .\build.ps1

.EXAMPLE
  .\build.ps1 -Configuration Debug

.EXAMPLE
  .\build.ps1 -RWDir "D:\SteamLibrary\steamapps\common\Rain World"
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $RWDir = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$ProjectFile = Join-Path $Root 'TrickSpear.csproj'
$PluginsDir = Join-Path $Root 'plugins'
$LibDll = Join-Path $Root 'lib\ImprovedInput.dll'
$AssemblyName = 'TrickSpear'

function Write-Step([string] $Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

Write-Step 'Checking prerequisites'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found. Install .NET SDK and ensure dotnet is on PATH.'
}

if (-not (Test-Path -LiteralPath $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if (-not (Test-Path -LiteralPath $LibDll)) {
    throw @"
Missing build dependency: $LibDll
Place ImprovedInput.dll from Improved Input Config into lib/ before building.
"@
}

$dllOutput = Join-Path $PluginsDir "$AssemblyName.dll"

Write-Step "Building $AssemblyName ($Configuration)"

$buildArgs = @(
    'build',
    $ProjectFile,
    '-c', $Configuration,
    '--nologo'
)

if (-not [string]::IsNullOrWhiteSpace($RWDir)) {
    $buildArgs += @('-p:RWDir=' + $RWDir)
}

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $dllOutput)) {
    throw "Built assembly not found: $dllOutput"
}

Write-Host ''
Write-Host 'Build complete.' -ForegroundColor Green
Write-Host "  DLL: $dllOutput"
