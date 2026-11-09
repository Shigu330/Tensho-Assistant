param(
    [string]$DotNet,
    # After a green run (0.219): -Commit commits and pushes Devloppement; -Release (once that commit's CI is green on GitHub, 0.1017)
    # tags Stable-x.y.z, creates the GitHub release with latest.zip, then moves main (which carries repo.json) forward to it.
    [switch]$Commit,
    [switch]$Release,
    [switch]$DryRun,
    # 0.1017, with -Release: urgent fix (game patch, privacy) released before its in-game tests; the reason heads the release notes.
    [string]$Hotfix,
    # 0.1020, with -Release: the major number (x of x.y.z) is the maintainer's decision; a release that raises it needs this switch.
    [switch]$Major,
    # Re-run everything even when this exact source state already passed (see the stamp below).
    [switch]$Force,
    # 0.323: contributors without the game: builds the test project only (pure logic, no Dalamud), runs the tests and the language
    # validation, skips the plugin build and the package. Not enough for a release.
    [switch]$TestsOnly
)
$ErrorActionPreference = 'Stop'
if ($TestsOnly -and ($Commit -or $Release)) { throw '-TestsOnly checks the logic only: it cannot be combined with -Commit or -Release.' }
if ($PSBoundParameters.ContainsKey('Hotfix') -and -not $Release) { throw '-Hotfix goes with -Release only.' }
if ($Major -and -not $Release) { throw '-Major goes with -Release only.' }
if ($PSBoundParameters.ContainsKey('Hotfix') -and $Hotfix.Trim().Length -lt 8) { throw "-Hotfix needs its reason in a few words, e.g. -Hotfix 'game patch 7.58'." }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $DotNet) {
    # Prefer the newest locally installed .NET 10 SDK (global.json accepts 10.0.100 and newer); explicit -DotNet supports other hosts.
    $taskLocalSdk = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.codex') -Directory -Filter 'dotnet-10.*' -ErrorAction SilentlyContinue |
        Sort-Object { [version]($_.Name -replace '^dotnet-', '') } -Descending |
        ForEach-Object { Join-Path $_.FullName 'dotnet.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    $taskLegacySdk = 'C:\ia-code\.tools\dotnet10\dotnet.exe'
    $DotNet = if ($taskLocalSdk) { $taskLocalSdk }
        elseif (Test-Path -LiteralPath $taskLegacySdk) { $taskLegacySdk } else { 'dotnet' }
}
# Stamp of the last green run (0.219): version + SHA-256 of every git-tracked or new file except *.md, docs/ and repo.json. When nothing
# changed since that run (e.g. -Commit after a plain run; never -Release since 0.1020) and the DLL is still there, the build/tests/package checks are not run again.
# 0.323: the Dalamud version (Dalamud.dll in DALAMUD_HOME, else XIVLauncher's dev hooks) and the SDK version are part of the stamp, so
# an update of either runs every check again.
function Get-TaskEnvironment {
    $dalamudHome = if ($env:DALAMUD_HOME) { $env:DALAMUD_HOME } else { Join-Path $env:APPDATA 'XIVLauncher\addon\Hooks\dev' }
    $dalamud = Join-Path $dalamudHome 'Dalamud.dll'
    $dalamudVersion = if (Test-Path -LiteralPath $dalamud) { (Get-Item -LiteralPath $dalamud).VersionInfo.FileVersion } else { 'none' }
    $sdk = try { (& $DotNet --version 2>$null | Select-Object -First 1) } catch { 'unknown' }
    return "dalamud=$dalamudVersion;sdk=$sdk"
}
function Get-TaskFingerprint {
    $files = & git -C $taskRoot ls-files -co --exclude-standard | Where-Object { $_ -notmatch '(\.md$|^docs/|^repo\.json$)' } | Sort-Object
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        $environment = [Text.Encoding]::UTF8.GetBytes((Get-TaskEnvironment) + "`n"); [void]$hasher.TransformBlock($environment, 0, $environment.Length, $null, 0)
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
# -Release when the tag already exists: not stopped here (0.326). Release.ps1 stops if the release is complete (GitHub release and
# main), and otherwise resumes the missing steps of a release stopped halfway.
if ($Release -and (& git -C $taskRoot tag --list "Stable-$taskVersionNow")) {
    Write-Host "Tag Stable-$taskVersionNow exists: Release.ps1 will stop if that release is complete, or resume its missing steps." -ForegroundColor Yellow
}
# 0.323: the CHANGELOG entry is checked here too (Release.ps1 still stops on it): a warning while working, before -Commit refuses it.
$taskChangelog = Get-Content -LiteralPath (Join-Path $taskRoot 'CHANGELOG.md') -Encoding UTF8
$taskTopEntry = $taskChangelog | Where-Object { $_ -match '^## (\d+\.\d+\.\d+)\s*$' } | Select-Object -First 1
if ($taskTopEntry -ne "## $taskVersionNow") { Write-Host "WARNING: the top entry of CHANGELOG.md is '$taskTopEntry', not '## $taskVersionNow' (-Commit will refuse it)." -ForegroundColor Yellow }
# System plan 4.14: a release publishes documents produced for its own version (tools/docgen.py: index, page table, version marks).
if ($Release) {
    & python -I -X utf8 (Join-Path $taskRoot 'tools\docgen.py') check --release
    if ($LASTEXITCODE -ne 0) { throw 'Documents not produced for this version: python -I -X utf8 tools/docgen.py write, then Verify again.' }
}
if ($TestsOnly) {
    Push-Location $taskRoot
    try {
        & $DotNet build tests/TenshoAssistant.Tests -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Test project build failed.' }
        & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Managed tests failed.' }
        $taskLanguages = & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build -- --validate-localization src/TenshoAssistant/Localization/Languages
        if ($LASTEXITCODE -ne 0) { $taskLanguages | ForEach-Object { Write-Host "$_" }; throw 'Localization validation failed.' }
        Write-Output 'PASS localization metadata, duplicate keys and format compatibility (partial translations allowed)'
        Write-Output "TESTS ONLY ${taskVersionNow}: the plugin was not built nor packaged (that needs Dalamud: run Verify.ps1 without -TestsOnly)."
    }
    finally { Pop-Location }
    exit 0
}
$taskFingerprint = Get-TaskFingerprint
$taskSkip = $false
# 0.1020: a release always runs every check again (as -Force): what is published is built and tested now, not taken from a stamp.
if (-not $Force -and -not $Release -and (Test-Path -LiteralPath $taskStampFile) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip'))) {
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
    # 0.323: the validator's report (MISSING / UNKNOWN keys) is shown when it fails; it was always discarded.
    $taskLanguages = & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build -- --validate-localization src/TenshoAssistant/Localization/Languages
    if ($LASTEXITCODE -ne 0) { $taskLanguages | ForEach-Object { Write-Host "$_" }; throw 'Localization validation failed.' }
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
        foreach ($taskExpected in @('TenshoAssistant.dll', 'TenshoAssistant.json', 'Raphael.Net.dll', 'raphael_bindings.dll')) {
            if ($taskExpected -notin $taskEntries) { throw "Missing package entry: $taskExpected" }
            if (@($taskEntries | Where-Object { $_ -ceq $taskExpected }).Count -ne 1) { throw "Duplicate package entry: $taskExpected" }
            $taskZipStream = $taskZip.GetEntry($taskExpected).Open()
            $taskHasher = [Security.Cryptography.SHA256]::Create()
            try { $taskZipHash = [BitConverter]::ToString($taskHasher.ComputeHash($taskZipStream)).Replace('-', '') }
            finally { $taskHasher.Dispose(); $taskZipStream.Dispose() }
            $taskCanonicalHash = (Get-FileHash -LiteralPath (Join-Path $taskOutput $taskExpected) -Algorithm SHA256).Hash
            if ($taskZipHash -ne $taskCanonicalHash) { throw "Package entry differs from canonical x64 Release output: $taskExpected" }
        }
        # 0.248: the languages are embedded in the DLL only; no language file may ship beside it.
        if (@($taskEntries | Where-Object { $_.Replace([char]92, [char]47) -like 'Localization/*' }).Count) {
            throw 'Language files must be embedded in TenshoAssistant.dll, not shipped in the package.'
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
# The git step's exit code is passed on (2 = expected stop such as 'already released'), so commit-release.bat can stop before releasing.
if ($Commit) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Commit -DryRun:$DryRun; if ($LASTEXITCODE) { exit $LASTEXITCODE } }
if ($Release) {
    $taskReleaseArgs = @{ Mode = 'Release'; DryRun = $DryRun }
    if ($PSBoundParameters.ContainsKey('Hotfix')) { $taskReleaseArgs.Hotfix = $Hotfix }
    if ($Major) { $taskReleaseArgs.Major = $true }
    & (Join-Path $PSScriptRoot 'Release.ps1') @taskReleaseArgs; if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
exit 0
