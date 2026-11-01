param(
    [string]$DotNet,
    # After a green run (0.219): -Commit commits and pushes Devloppement; -Release publishes Stable, the tag, the GitHub release and repo.json.
    [switch]$Commit,
    [switch]$Release,
    [switch]$DryRun,
    # Re-run everything even when this exact source state already passed (see the stamp below).
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $DotNet) {
    # Prefer the locally installed pinned SDK; explicit -DotNet supports other hosts.
    $taskSdkVersion = (Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'global.json') | ConvertFrom-Json).sdk.version
    $taskLocalSdk = Join-Path $env:USERPROFILE ".codex\dotnet-$taskSdkVersion\dotnet.exe"
    $taskLegacySdk = 'C:\ia-code\.tools\dotnet10\dotnet.exe'
    $DotNet = if (Test-Path -LiteralPath $taskLocalSdk) { $taskLocalSdk }
        elseif (Test-Path -LiteralPath $taskLegacySdk) { $taskLegacySdk } else { 'dotnet' }
}
# Stamp of the last green run (0.219): version + SHA-256 of every git-tracked or new file except *.md, docs/ and repo.json. When nothing
# changed since that run (e.g. -Commit then -Release) and the DLL is still there, the build/tests/package checks are not run again.
function Get-TaskFingerprint {
    $files = & git -C $taskRoot ls-files -co --exclude-standard | Where-Object { $_ -notmatch '(\.md$|^docs/|^repo\.json$)' } | Sort-Object
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        foreach ($file in $files) {
            $path = Join-Path $taskRoot $file
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
            $name = [Text.Encoding]::UTF8.GetBytes($file + "`n"); [void]$hasher.TransformBlock($name, 0, $name.Length, $null, 0)
            $bytes = [IO.File]::ReadAllBytes($path); [void]$hasher.TransformBlock($bytes, 0, $bytes.Length, $null, 0)
        }
        [void]$hasher.TransformFinalBlock([byte[]]::new(0), 0, 0)
        return [BitConverter]::ToString($hasher.Hash).Replace('-', '')
    }
    finally { $hasher.Dispose() }
}
$taskStampFile = Join-Path $taskRoot 'artifacts\verify-stamp.json'
$taskVersionNow = ([xml](Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\TenshoAssistant.csproj'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
# -Release of a version already published: stop before building anything (scripts/Release.ps1 checks it again).
if ($Release -and ((& git -C $taskRoot tag --list "Stable-$taskVersionNow") -or (& git -C $taskRoot ls-remote --tags origin "refs/tags/Stable-$taskVersionNow"))) {
    Write-Host "Version $taskVersionNow is already released (tag Stable-$taskVersionNow): raise <Version> and the CHANGELOG entry for a new release." -ForegroundColor Yellow
    exit 2
}
$taskFingerprint = Get-TaskFingerprint
$taskSkip = $false
if (-not $Force -and (Test-Path -LiteralPath $taskStampFile) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip'))) {
    $taskStamp = Get-Content -Raw -LiteralPath $taskStampFile | ConvertFrom-Json
    $taskSkip = $taskStamp.Version -eq $taskVersionNow -and $taskStamp.Fingerprint -eq $taskFingerprint
}
if ($taskSkip) { Write-Output "SKIP checks: version $taskVersionNow already verified green on $($taskStamp.VerifiedAt) with the same sources (-Force to run again)." }
else {
Push-Location $taskRoot
try {
    & $DotNet build TenshoAssistant.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Managed tests failed.' }
    & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build -- --validate-localization src/TenshoAssistant/Localization/Languages | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Localization validation failed.' }
    Write-Output 'PASS localization metadata, duplicate keys and format compatibility (partial translations allowed)'
    $taskOutput = Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release'
    $taskManifest = Get-Content -Raw -LiteralPath (Join-Path $taskOutput 'TenshoAssistant.json') | ConvertFrom-Json
    # The source manifest leaves InternalName/AssemblyVersion/DalamudApiLevel to DalamudPackager (Dalamud plugin metadata rules);
    # the version comes from <Version> in the csproj only.
    $taskSourceManifest = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\TenshoAssistant.json') | ConvertFrom-Json
    if ($null -ne $taskSourceManifest.AssemblyVersion -or $null -ne $taskSourceManifest.InternalName -or $null -ne $taskSourceManifest.DalamudApiLevel) {
        throw 'Source manifest must not set InternalName, AssemblyVersion or DalamudApiLevel (filled by DalamudPackager).'
    }
    $taskProjectVersion = ([xml](Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\TenshoAssistant.csproj'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    $taskAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $taskOutput 'TenshoAssistant.dll')).Version.ToString()
    if ($taskManifest.InternalName -ne 'TenshoAssistant' -or $taskManifest.DalamudApiLevel -ne 15 -or $taskManifest.AssemblyVersion -ne "$taskProjectVersion.0" -or $taskManifest.AssemblyVersion -ne $taskAssemblyVersion) {
        throw 'Packaged manifest identity/API/version mismatch.'
    }
    $taskZip = [IO.Compression.ZipFile]::OpenRead((Join-Path $taskOutput 'TenshoAssistant\latest.zip'))
    try {
        $taskEntries = @($taskZip.Entries.FullName)
        foreach ($taskExpected in @('TenshoAssistant.dll', 'TenshoAssistant.json')) {
            if ($taskExpected -notin $taskEntries) { throw "Missing package entry: $taskExpected" }
            if (@($taskEntries | Where-Object { $_ -ceq $taskExpected }).Count -ne 1) { throw "Duplicate package entry: $taskExpected" }
            $taskZipStream = $taskZip.GetEntry($taskExpected).Open()
            $taskHasher = [Security.Cryptography.SHA256]::Create()
            try { $taskZipHash = [BitConverter]::ToString($taskHasher.ComputeHash($taskZipStream)).Replace('-', '') }
            finally { $taskHasher.Dispose(); $taskZipStream.Dispose() }
            $taskCanonicalHash = (Get-FileHash -LiteralPath (Join-Path $taskOutput $taskExpected) -Algorithm SHA256).Hash
            if ($taskZipHash -ne $taskCanonicalHash) { throw "Package entry differs from canonical x64 Release output: $taskExpected" }
        }
        $taskLanguageSource = Join-Path $taskRoot 'src\TenshoAssistant\Localization\Languages'
        foreach ($taskLanguageFile in Get-ChildItem -LiteralPath $taskLanguageSource -Filter '*.json' -File) {
            $taskLanguageEntry = 'Localization/Languages/' + $taskLanguageFile.Name
            $taskLanguageMatches = @($taskZip.Entries | Where-Object { $_.FullName.Replace('\', '/') -ceq $taskLanguageEntry })
            if ($taskLanguageMatches.Count -ne 1) { throw "Missing or duplicate language package entry: $taskLanguageEntry" }
            $taskLanguageStream = $taskLanguageMatches[0].Open()
            $taskLanguageHasher = [Security.Cryptography.SHA256]::Create()
            try { $taskLanguageHash = [BitConverter]::ToString($taskLanguageHasher.ComputeHash($taskLanguageStream)).Replace('-', '') }
            finally { $taskLanguageHasher.Dispose(); $taskLanguageStream.Dispose() }
            $taskLanguageSourceHash = (Get-FileHash -LiteralPath $taskLanguageFile.FullName -Algorithm SHA256).Hash
            if ($taskLanguageHash -ne $taskLanguageSourceHash) { throw "Packaged language differs from source: $taskLanguageEntry" }
        }
        if (@($taskEntries | Where-Object { $_ -match '(^|/)(Dalamud|FFXIVClientStructs|Lumina|Dalamud.Bindings.ImGui)\.dll$' }).Count) {
            throw 'Host assemblies must not be redistributed in the plugin package.'
        }
    }
    finally { $taskZip.Dispose() }
    Write-Output 'PASS package identity, API, version, exact canonical ZIP hashes, language files and host assembly exclusion'
    Write-Output "VERSION $taskProjectVersion (DLL $taskAssemblyVersion)"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $taskStampFile) | Out-Null
    [ordered]@{ Version = $taskProjectVersion; Fingerprint = $taskFingerprint; VerifiedAt = (Get-Date).ToString('s') } | ConvertTo-Json | Set-Content -LiteralPath $taskStampFile -Encoding UTF8
}
finally { Pop-Location }
}
# Git steps only after everything above passed (scripts/Release.ps1).
if ($Commit) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Commit -DryRun:$DryRun }
if ($Release) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Release -DryRun:$DryRun }
