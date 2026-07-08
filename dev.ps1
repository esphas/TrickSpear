#Requires -Version 5.1
<#
.SYNOPSIS
  Link or unlink a junction from dist/ to Rain World's mods folder.

.DESCRIPTION
  Links:
    <Rain World>/RainWorld_Data/StreamingAssets/mods/<mod id>/
  to:
    dist/<mod id>/

  Run .\build.ps1 first to populate dist/.

.PARAMETER Action
  link   - Create the junction (default).
  unlink - Remove the junction only.
  status - Show whether the game mod path exists and where it points.

.EXAMPLE
  .\dev.ps1 link

.EXAMPLE
  .\dev.ps1 unlink

.EXAMPLE
  .\dev.ps1 status -RWDir "D:\SteamLibrary\steamapps\common\Rain World"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('link', 'unlink', 'status')]
    [string] $Action = 'link',

    [string] $RWDir = '',

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$ModInfoFile = Join-Path $Root 'modinfo.json'
$IdFile = Join-Path $Root 'id.txt'
$GamePathsLocal = Join-Path $Root 'GamePaths.local.props'
$GamePathsShared = Join-Path $Root 'GamePaths.props'

function Write-Step([string] $Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string] $Message) {
    Write-Host $Message -ForegroundColor Green
}

function Write-Warn([string] $Message) {
    Write-Host $Message -ForegroundColor Yellow
}

function Read-JsonFile([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Get-RWDirFromProps {
    foreach ($file in @($GamePathsLocal, $GamePathsShared)) {
        if (-not (Test-Path -LiteralPath $file)) {
            continue
        }

        [xml] $xml = Get-Content -LiteralPath $file -Raw -Encoding UTF8
        $value = $xml.Project.PropertyGroup.RWDir
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim().TrimEnd('\')
        }
    }

    return $null
}

function Resolve-RWDir {
    if (-not [string]::IsNullOrWhiteSpace($RWDir)) {
        return $RWDir.Trim().TrimEnd('\')
    }

    $fromProps = Get-RWDirFromProps
    if ($fromProps) {
        return $fromProps
    }

    throw @"
Rain World install path is not configured.
Pass -RWDir, or set RWDir in GamePaths.local.props.
"@
}

function Test-Junction([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return false
    }

    return ([IO.FileAttributes]::ReparsePoint -band (Get-Item -LiteralPath $Path).Attributes) -ne 0
}

function Get-JunctionTarget([string] $Path) {
    if (-not (Test-Junction $Path)) {
        return $null
    }

    return (Get-Item -LiteralPath $Path).Target
}

function Show-Status([string] $LinkPath, [string] $StageDir) {
    Write-Step 'Junction status'

    Write-Host "  Game root     : $script:ResolvedRWDir"
    Write-Host "  Mod link path : $LinkPath"
    Write-Host "  Stage dir     : $StageDir"

    if (-not (Test-Path -LiteralPath $LinkPath)) {
        Write-Host '  State         : not linked' -ForegroundColor DarkYellow
        return
    }

    if (Test-Junction $LinkPath) {
        $target = (Resolve-Path -LiteralPath (Get-JunctionTarget $LinkPath)).Path
        $matchesStage = $target -eq $StageDir

        Write-Host "  State         : junction"
        Write-Host "  Points to     : $target"

        if ($matchesStage) {
            Write-Ok '  Match         : yes'
        } else {
            Write-Warn '  Match         : no (junction target differs from dist/)'
        }

        return
    }

    Write-Warn '  State         : real directory (not a junction)'
    Write-Warn '  Remove or rename it manually before linking, or use -Force to replace.'
}

function Install-Link([string] $LinkPath, [string] $Target) {
    if ($PSCmdlet.ShouldProcess($LinkPath, "Create junction -> $Target")) {
        New-Item -ItemType Junction -Path $LinkPath -Target $Target | Out-Null
    }
}

function Remove-Link([string] $LinkPath) {
    if ($PSCmdlet.ShouldProcess($LinkPath, 'Remove junction')) {
        Remove-Item -LiteralPath $LinkPath -Force
    }
}

function Ensure-StageDir([string] $StageDir) {
    if (-not (Test-Path -LiteralPath $StageDir)) {
        throw @"
Staged mod folder not found:
  $StageDir
Run .\build.ps1 first.
"@
    }

    $required = @(
        (Join-Path $StageDir 'modinfo.json'),
        (Join-Path $StageDir 'plugins\TrickSpear.dll')
    )

    foreach ($path in $required) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw @"
Staged mod folder is incomplete:
  missing $path
Run .\build.ps1 first.
"@
        }
    }
}

$modInfo = Read-JsonFile $ModInfoFile
$modIdFromFile = (Get-Content -LiteralPath $IdFile -Raw -Encoding UTF8).Trim()

if ([string]::IsNullOrWhiteSpace($modIdFromFile)) {
    throw "Mod id is empty: $IdFile"
}

if ($modInfo.id -ne $modIdFromFile) {
    throw "Mod id mismatch: modinfo.json has '$($modInfo.id)', id.txt has '$modIdFromFile'."
}

$script:ResolvedRWDir = Resolve-RWDir
$modsRoot = Join-Path $script:ResolvedRWDir 'RainWorld_Data\StreamingAssets\mods'
$linkPath = Join-Path $modsRoot $modIdFromFile
$stageDirPath = Join-Path (Join-Path $Root 'dist') $modIdFromFile
$stageDir = $null
if (Test-Path -LiteralPath $stageDirPath) {
    $stageDir = (Resolve-Path -LiteralPath $stageDirPath).Path
}

if (-not (Test-Path -LiteralPath $modsRoot)) {
    throw "Game mods directory not found: $modsRoot`nCheck -RWDir / GamePaths.local.props."
}

switch ($Action) {
    'status' {
        if (-not $stageDir) {
            $stageDir = $stageDirPath
            Write-Warn "Stage dir not built yet: $stageDir"
        }

        Show-Status $linkPath $stageDir
        break
    }

    'link' {
        Write-Step "Linking $modIdFromFile"
        Ensure-StageDir $stageDirPath
        $stageDir = (Resolve-Path -LiteralPath $stageDirPath).Path

        if (Test-Path -LiteralPath $linkPath) {
            if (Test-Junction $linkPath) {
                $currentTarget = (Resolve-Path -LiteralPath (Get-JunctionTarget $linkPath)).Path
                if ($currentTarget -eq $stageDir) {
                    Write-Ok "Already linked: $linkPath -> $stageDir"
                    break
                }

                if (-not $Force) {
                    throw @"
Junction already exists but points elsewhere:
  $linkPath -> $currentTarget
Use -Force to replace it.
"@
                }

                Write-Warn "Replacing junction: $linkPath"
                Remove-Link $linkPath
            }
            else {
                if (-not $Force) {
                    throw @"
A real folder already exists at:
  $linkPath
Rename/remove it manually, or use -Force to delete that folder and create a junction.
"@
                }

                Write-Warn "Removing existing folder: $linkPath"
                if ($PSCmdlet.ShouldProcess($linkPath, 'Remove existing mod folder')) {
                    Remove-Item -LiteralPath $linkPath -Recurse -Force
                }
            }
        }

        Install-Link $linkPath $stageDir
        Write-Ok "Linked: $linkPath -> $stageDir"
        break
    }

    'unlink' {
        Write-Step "Unlinking $modIdFromFile"

        if (-not (Test-Path -LiteralPath $linkPath)) {
            Write-Ok "Nothing to unlink; path does not exist: $linkPath"
            break
        }

        if (-not (Test-Junction $linkPath)) {
            throw @"
Path exists but is not a junction:
  $linkPath
Refusing to delete a real mod folder. Remove it manually if that is intended.
"@
        }

        $target = Get-JunctionTarget $linkPath
        Remove-Link $linkPath
        Write-Ok "Removed junction: $linkPath (staged mod kept at $target)"
        break
    }
}
