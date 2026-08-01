# Produces publish\HpCommander.exe: one self-contained file, no .NET runtime needed on the target
# machine, plus the Data folder it reads at runtime. publish\ is gitignored, so this is the only
# record of how the shipped binary is built - keep it and the README in step.
#
# -Version stamps the build so the in-app update check has something true to compare against.
# Pass the release tag ("v1.8.0" or "1.8.0"); omit it for a local build and the csproj default is
# used. .github/workflows/release.yml passes the tag it was triggered by, which is what keeps the
# binary and the tag from drifting apart.
#
# -Zip additionally produces the release asset, named exactly as the updater expects to find it.

param(
    [string]$Version,
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$publishArgs = @(
    'HpCommander/HpCommander.csproj'
    '-c', 'Release'
    '-r', 'win-x64'
    '--self-contained', 'true'
    '-p:PublishSingleFile=true'
    '-p:IncludeNativeLibrariesForSelfExtract=true'
    # Roughly halves the download the updater has to pull, for a small one-time extract cost.
    '-p:EnableCompressionInSingleFile=true'
    '-o', 'publish'
)

$normalisedVersion = $null
if ($Version) {
    $normalisedVersion = $Version.TrimStart('v', 'V')
    $publishArgs += "-p:Version=$normalisedVersion"
}

if (Test-Path publish) { Remove-Item publish -Recurse -Force }

dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $PSScriptRoot 'publish\HpCommander.exe'
if (-not (Test-Path $exe)) { throw "publish succeeded but $exe is missing" }

# Debug symbols are not part of what users download.
Remove-Item (Join-Path $PSScriptRoot 'publish\*.pdb') -Force -ErrorAction SilentlyContinue

$dataDir = Join-Path $PSScriptRoot 'publish\Data'
if (-not (Test-Path $dataDir)) { throw "publish\Data is missing - the app cannot start without it" }

$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "$exe  ($size MB)"

if ($Zip) {
    if (-not $normalisedVersion) { throw "-Zip needs -Version so the asset can be named" }

    # UpdateService looks for exactly this name; see ReleaseInfo.AssetPrefix/AssetSuffix.
    $zipPath = Join-Path $PSScriptRoot "HpCommander-v$normalisedVersion-win-x64.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path (Join-Path $PSScriptRoot 'publish\*') -DestinationPath $zipPath
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "$zipPath  ($zipSize MB)"
}
