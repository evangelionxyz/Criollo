param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug',
    [string]$Platform = '',
    [switch]$ManagedOnly
)

$ErrorActionPreference = 'Stop'

function Get-MSBuildPath {
    $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { return $null }

    $installPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if (-not $installPath) { return $null }

    $candidate = Join-Path $installPath 'MSBuild\Current\Bin\MSBuild.exe'
    if (Test-Path $candidate) { return $candidate }

    return $null
}

$msbuild = Get-MSBuildPath
if (-not $msbuild) {
    throw 'MSBuild not found. Open a Visual Studio Developer PowerShell, or install Visual Studio Build Tools (MSBuild).'
}

$solution = if ($ManagedOnly) { 'MochiSharp-Managed.sln' } else { 'MochiSharp.sln' }

if ([string]::IsNullOrWhiteSpace($Platform)) {
    $Platform = if ($ManagedOnly) { 'Any CPU' } else { 'x64' }
}
Write-Host "Using MSBuild: $msbuild"
Write-Host "Building: $solution (Configuration=$Configuration, Platform=$Platform)"

& $msbuild $solution /m /t:Build /p:Configuration=$Configuration /p:Platform="$Platform"
