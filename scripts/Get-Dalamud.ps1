# X-148 (GPT audit of 11/11, A16): Dalamud's official distribution for the CI, checked before it is used. goatcorp publishes no hash of
# latest.zip, only its AssemblyVersion (https://goatcorp.github.io/dalamud-distrib/version), and a hash kept here would break the CI at each
# Dalamud update (decision of 11/13: not pinned). So the download is checked against what is published: the zip is bounded in size and
# whole, its Dalamud.dll carries the AssemblyVersion goatcorp publishes and the major version of Dalamud.NET.Sdk in global.json (the API
# level the plugin is built for). Its SHA-256 is written to the log (a trace of what was built against, not a pin).
# -ZipPath and -PublishedVersion skip the downloads (checks on a PC).
param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [string]$ZipPath = '',
    [string]$PublishedVersion = '',
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
$base = 'https://goatcorp.github.io/dalamud-distrib'
$maximumBytes = 300MB

$zip = $ZipPath
if (-not $zip) {
    $zip = Join-Path ([System.IO.Path]::GetTempPath()) ('dalamud-' + [guid]::NewGuid().ToString('N') + '.zip')
    Invoke-WebRequest -Uri "$base/latest.zip" -OutFile $zip -UseBasicParsing
}
if (-not $PublishedVersion) {
    $raw = (Invoke-WebRequest -Uri "$base/version" -UseBasicParsing).Content
    if ($raw -is [byte[]]) { $raw = [System.Text.Encoding]::UTF8.GetString($raw) }
    $PublishedVersion = ($raw | ConvertFrom-Json).AssemblyVersion
}
if (-not $PublishedVersion) { throw 'Dalamud: no AssemblyVersion in the published version file' }

$size = (Get-Item -LiteralPath $zip).Length
if ($size -le 0 -or $size -gt $maximumBytes) { throw "Dalamud: latest.zip is $size bytes (expected 1 to $maximumBytes)" }
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash

New-Item -ItemType Directory -Force -Path $Destination | Out-Null
Expand-Archive -LiteralPath $zip -DestinationPath $Destination -Force
$dll = Join-Path $Destination 'Dalamud.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Dalamud: no Dalamud.dll in latest.zip' }
$version = [System.Reflection.AssemblyName]::GetAssemblyName((Resolve-Path -LiteralPath $dll).Path).Version

$sdk = (Get-Content -Raw -LiteralPath (Join-Path $Root 'global.json') | ConvertFrom-Json).'msbuild-sdks'.'Dalamud.NET.Sdk'
if (-not $sdk) { throw 'Dalamud: no Dalamud.NET.Sdk version in global.json' }
$apiLevel = [int]($sdk.Split('.')[0])

if ($version.ToString() -ne $PublishedVersion) {
    throw "Dalamud: latest.zip holds Dalamud.dll $version, goatcorp publishes $PublishedVersion (a partial or stale download, or a mirror that differs)"
}
if ($version.Major -ne $apiLevel) {
    throw "Dalamud: latest.zip is Dalamud $version, the plugin is built for API level $apiLevel (Dalamud.NET.Sdk $sdk in global.json): update the SDK first"
}
"Dalamud $version (published $PublishedVersion, API level $apiLevel), latest.zip $size bytes, SHA-256 $hash"
