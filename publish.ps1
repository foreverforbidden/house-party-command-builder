# Produces publish\HpCommander.exe: one self-contained file, no .NET runtime needed on the target
# machine. publish\ is gitignored, so this is the only record of how the shipped binary is built -
# keep it and the README in step.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

dotnet publish HpCommander/HpCommander.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish

$exe = Join-Path $PSScriptRoot 'publish\HpCommander.exe'
if (-not (Test-Path $exe)) { throw "publish succeeded but $exe is missing" }

$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "$exe  ($size MB)"
