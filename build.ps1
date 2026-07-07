#Requires -Version 5.1
<#
.SYNOPSIS
  Build TrickSpear, stage a workshop-ready mod folder, and create a zip archive.

.EXAMPLE
  .\build.ps1

.EXAMPLE
  .\build.ps1 -RWDir "D:\SteamLibrary\steamapps\common\Rain World"
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputDir = '',

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

$version = [string]$modInfo.version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'modinfo.json is missing version.'
}

$modFolderName = $modIdFromFile
$distRoot = if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    Join-Path $Root 'dist'
} else {
    $OutputDir
}

$stageDir = Join-Path $distRoot $modFolderName
$dllSource = Join-Path $PluginsDir "$AssemblyName.dll"

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

$optionalAssets = @(
    'thumbnail.png',
    'banner.png'
)

foreach ($asset in $optionalAssets) {
    if (Copy-IfExists (Join-Path $Root $asset) (Join-Path $stageDir $asset)) {
        Write-Host "  included $asset"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $stageDir 'thumbnail.png'))) {
    Write-Warning 'thumbnail.png not found. Workshop uploads usually need a thumbnail at the mod root.'
}

Write-Step 'Validating package contents'

$requiredPaths = @(
    (Join-Path $stageDir 'modinfo.json'),
    (Join-Path $stageDir 'id.txt'),
    (Join-Path $stageDir "plugins\$AssemblyName.dll"),
    (Join-Path $stageDir 'text\text_eng\strings.txt'),
    (Join-Path $stageDir 'text\text_chi\strings.txt')
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Package validation failed, missing: $path"
    }
}

$zipPath = Join-Path $distRoot "$modFolderName-$version.zip"

Write-Step "Creating archive: $zipPath"

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -LiteralPath $stageDir -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host ''
Write-Host 'Build complete.' -ForegroundColor Green
Write-Host "  Mod folder : $stageDir"
Write-Host "  Version    : $version"
Write-Host "  DLL        : plugins\$AssemblyName.dll"
Write-Host "  Archive    : $zipPath"
Write-Host ''
Write-Host 'Workshop / local install:'
Write-Host "  Copy the '$modFolderName' folder into RainWorld_Data/StreamingAssets/mods/"
Write-Host "  Or upload/extract $modFolderName-$version.zip so that modinfo.json sits inside the mod folder."
