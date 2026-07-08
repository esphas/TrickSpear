#Requires -Version 5.1
<#
.SYNOPSIS
  Build TrickSpear and stage a clean mod folder under dist/.

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
$ModInfoFile = Join-Path $Root 'modinfo.json'
$IdFile = Join-Path $Root 'id.txt'
$PluginsDir = Join-Path $Root 'plugins'
$TextDir = Join-Path $Root 'text'
$LibDll = Join-Path $Root 'lib\ImprovedInput.dll'
$AssemblyName = 'TrickSpear'

function Write-Step([string] $Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Read-JsonFile([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Ensure-Directory([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Copy-IfExists([string] $Source, [string] $Destination) {
    if (Test-Path -LiteralPath $Source) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        return $true
    }

    return $false
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

$modInfo = Read-JsonFile $ModInfoFile
$modIdFromFile = (Get-Content -LiteralPath $IdFile -Raw -Encoding UTF8).Trim()

if ([string]::IsNullOrWhiteSpace($modIdFromFile)) {
    throw "Mod id is empty: $IdFile"
}

if ($modInfo.id -ne $modIdFromFile) {
    throw "Mod id mismatch: modinfo.json has '$($modInfo.id)', id.txt has '$modIdFromFile'."
}

$dllSource = Join-Path $PluginsDir "$AssemblyName.dll"
$stageDir = Join-Path (Join-Path $Root 'dist') $modIdFromFile

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

if (-not (Test-Path -LiteralPath $dllSource)) {
    throw "Built assembly not found: $dllSource"
}

Write-Step "Staging mod folder: $stageDir"

if (Test-Path -LiteralPath $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}

Ensure-Directory $stageDir
Ensure-Directory (Join-Path $stageDir 'plugins')

Copy-Item -LiteralPath $ModInfoFile -Destination (Join-Path $stageDir 'modinfo.json') -Force
Copy-Item -LiteralPath $IdFile -Destination (Join-Path $stageDir 'id.txt') -Force
Copy-Item -LiteralPath $dllSource -Destination (Join-Path $stageDir "plugins\$AssemblyName.dll") -Force

if (Test-Path -LiteralPath $TextDir) {
    Copy-Item -LiteralPath $TextDir -Destination (Join-Path $stageDir 'text') -Recurse -Force
} else {
    Write-Warning 'No text/ directory found; localization files were not packaged.'
}

foreach ($asset in @('thumbnail.png', 'banner.png')) {
    if (Copy-IfExists (Join-Path $Root $asset) (Join-Path $stageDir $asset)) {
        Write-Host "  included $asset"
    }
}

Write-Step 'Validating staged mod'

$requiredPaths = @(
    (Join-Path $stageDir 'modinfo.json'),
    (Join-Path $stageDir 'id.txt'),
    (Join-Path $stageDir "plugins\$AssemblyName.dll"),
    (Join-Path $stageDir 'text\text_eng\strings.txt'),
    (Join-Path $stageDir 'text\text_chi\strings.txt')
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Staged mod validation failed, missing: $path"
    }
}

Write-Host ''
Write-Host 'Build complete.' -ForegroundColor Green
Write-Host "  Staged mod : $stageDir"
Write-Host "  DLL        : plugins\$AssemblyName.dll"
