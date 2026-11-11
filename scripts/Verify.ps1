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
    # 0.1093 (audit A070): the release notes cover every version since the last release; CHANGELOG.md must still hold them.
    & python -I -X utf8 (Join-Path $taskRoot 'tools\changelog.py') check
    if ($LASTEXITCODE -ne 0) { throw 'CHANGELOG.md and docs/changelog/ disagree: python -I -X utf8 tools/changelog.py check.' }
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
if (-not $Force -and -not $Release -and (Test-Path -LiteralPath $taskStampFile) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip')) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip')) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip.sha256'))) {
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
    # X-119 (R33): the desktop application (src/TenshoAssistant.Desktop) goes with the plugin: published here self-contained (win-x64, with
    # Microsoft's MIT .NET runtime, so a double-click works on a PC that has only XIVLauncher), with the plugin's own files under plugin\;
    # that very folder is run headless (every page drawn: a page that breaks the app fails Verify, so publish.sh and the release), then
    # zipped as TenshoAssistant.Desktop.zip with the same version. Without the game data or XIVLauncher's Dalamud (CI, contributors) the
    # headless run is an explicit SKIP; -Release refuses it.
    $taskDesktopPackage = Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\package'
    if (Test-Path -LiteralPath $taskDesktopPackage) { Remove-Item -LiteralPath $taskDesktopPackage -Recurse -Force }
    & $DotNet publish src/TenshoAssistant.Desktop -c Release -r win-x64 --self-contained -p:DebugType=none -o $taskDesktopPackage --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Desktop application publish failed (src/TenshoAssistant.Desktop).' }
    $taskDesktopPlugin = Join-Path $taskDesktopPackage 'plugin'
    New-Item -ItemType Directory -Force -Path $taskDesktopPlugin | Out-Null
    foreach ($taskName in @('TenshoAssistant.dll', 'TenshoAssistant.json', 'Raphael.Net.dll', 'raphael_bindings.dll')) { Copy-Item -LiteralPath (Join-Path $taskOutput $taskName) -Destination $taskDesktopPlugin }
    $taskDesktopVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $taskDesktopPackage 'TenshoAssistant.Desktop.dll')).Version.ToString()
    if ($taskDesktopVersion -ne $taskAssemblyVersion) { throw "Desktop application version $taskDesktopVersion differs from the plugin's $taskAssemblyVersion (one version source: the plugin's csproj)." }
    $taskHeadlessReport = Join-Path $taskRoot 'artifacts\desktop-headless.md'
    # stderr read as text: under 'Stop', Windows PowerShell 5.1 would turn the app's SKIP or FAIL line into a terminating error.
    $taskPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try {
        # The shipped exe itself, with its own runtime and its plugin/ folder: what is released is what is drawn.
        $taskHeadless = & (Join-Path $taskDesktopPackage 'TenshoAssistant.Desktop.exe') --headless "--out=$taskHeadlessReport" 2>&1 | ForEach-Object { "$_" }
        $taskHeadlessCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $taskPreference }
    $taskHeadless | Where-Object { $_ -notmatch '^\s+\S+\s+(AsIs|NeedsLink|InGame)\s+drawn\s' } | ForEach-Object { Write-Output "  desktop: $_" }
    if ($taskHeadlessCode -eq 4) {
        Write-Output "SKIP desktop headless: no game data or no Dalamud install here; the app was built and packaged but its pages were not drawn."
        if ($Release) { throw 'SKIP desktop headless: -Release refuses a release whose desktop application was not drawn (run Verify where the game and XIVLauncher are installed).' }
    }
    elseif ($taskHeadlessCode -ne 0) { throw "Desktop headless run failed (exit $taskHeadlessCode): a page throws, blocks or is not classified in src/TenshoAssistant.Desktop/DesktopPages.cs; report: $taskHeadlessReport" }
    else { Write-Output "PASS desktop headless: every page classified and drawn by the packaged exe (report artifacts/desktop-headless.md)" }
    # The package: the published app (with the .NET runtime) and the plugin's own files of latest.zip under plugin/; never Dalamud, Lumina,
    # ImGui, cimgui, HexaGen, Mono.Cecil, Newtonsoft or the game's files, read where the user installed them (R05).
    $taskDesktopZip = Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip'
    if (Test-Path -LiteralPath $taskDesktopZip) { Remove-Item -LiteralPath $taskDesktopZip }
    # Entries named with '/' (Windows PowerShell's CreateFromDirectory would write '\', which other unzip tools read as part of a file name).
    $taskDesktopFiles = @(Get-ChildItem -LiteralPath $taskDesktopPackage -Recurse -File | ForEach-Object { $_.FullName.Substring($taskDesktopPackage.Length + 1).Replace([char]92, [char]47) })
    $taskDesktopArchive = [IO.Compression.ZipFile]::Open($taskDesktopZip, [IO.Compression.ZipArchiveMode]::Create)
    try { foreach ($taskName in $taskDesktopFiles) { [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskDesktopArchive, (Join-Path $taskDesktopPackage $taskName), $taskName, [IO.Compression.CompressionLevel]::Optimal) } }
    finally { $taskDesktopArchive.Dispose() }
    $taskDesktopArchive = [IO.Compression.ZipFile]::OpenRead($taskDesktopZip)
    try {
        $taskDesktopEntries = @($taskDesktopArchive.Entries | ForEach-Object { $_.FullName })
        # X-120: PluginUiTest.Host.dll is our own library (the generic part of drawing a plugin without the game, tools/PluginUiTest).
        foreach ($taskRequired in @('TenshoAssistant.Desktop.exe', 'TenshoAssistant.Desktop.dll', 'TenshoAssistant.Desktop.runtimeconfig.json', 'PluginUiTest.Host.dll', 'hostfxr.dll', 'hostpolicy.dll', 'System.Private.CoreLib.dll',
                'plugin/TenshoAssistant.dll', 'plugin/TenshoAssistant.json', 'plugin/Raphael.Net.dll', 'plugin/raphael_bindings.dll')) {
            if ($taskRequired -notin $taskDesktopEntries) { throw "TenshoAssistant.Desktop.zip misses $taskRequired." }
        }
        $taskForbidden = @($taskDesktopEntries | Where-Object { $_ -match '(^|/)(Dalamud|Lumina|HexaGen|Mono\.Cecil|Newtonsoft|FFXIVClientStructs|InteropGenerator|ImGuiScene|cimgui|System\.Reflection\.MetadataLoadContext)[^/]*$|\.(dat\d*|index2?|tex|exd|exh|scd)$' })
        if ($taskForbidden.Count) { throw "TenshoAssistant.Desktop.zip must not carry Dalamud, Lumina, ImGui, cimgui, Mono.Cecil, Newtonsoft or game files (R05): $($taskForbidden -join ', ')" }
        if ($taskDesktopEntries.Count -ne $taskDesktopFiles.Count) { throw "TenshoAssistant.Desktop.zip holds $($taskDesktopEntries.Count) files, the package folder $($taskDesktopFiles.Count)." }
        foreach ($taskName in @('TenshoAssistant.Desktop.exe', 'TenshoAssistant.Desktop.dll', 'plugin/TenshoAssistant.dll')) {
            $taskZipStream = $taskDesktopArchive.GetEntry($taskName).Open()
            $taskHasher = [Security.Cryptography.SHA256]::Create()
            try { $taskZipHash = [BitConverter]::ToString($taskHasher.ComputeHash($taskZipStream)).Replace('-', '') }
            finally { $taskHasher.Dispose(); $taskZipStream.Dispose() }
            $taskSource = if ($taskName -eq 'plugin/TenshoAssistant.dll') { Join-Path $taskOutput 'TenshoAssistant.dll' } else { Join-Path $taskDesktopPackage $taskName }
            if ($taskZipHash -ne (Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash) { throw "TenshoAssistant.Desktop.zip entry differs from the build output: $taskName" }
        }
    }
    finally { $taskDesktopArchive.Dispose() }
    $taskDesktopSize = [math]::Round((Get-Item -LiteralPath $taskDesktopZip).Length / 1MB, 1)
    Write-Output "PASS desktop package: $taskDesktopZip ($($taskDesktopEntries.Count) files, $taskDesktopSize MB: the self-contained app and plugin/TenshoAssistant.dll of the same version $taskDesktopVersion, nothing of Dalamud or the game)"
    # X-121 (R33): the app updates itself from the GitHub release: the release carries the zip's SHA-256 beside it (TenshoAssistant.Desktop.zip.sha256,
    # « hex  name » as sha256sum writes it), which the app compares before installing; Release.ps1 refuses a release without it.
    $taskDesktopHash = "$taskDesktopZip.sha256"
    $taskDesktopHex = (Get-FileHash -LiteralPath $taskDesktopZip -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($taskDesktopHash, "$taskDesktopHex  TenshoAssistant.Desktop.zip`n", [Text.UTF8Encoding]::new($false))
    # The update tried on a fake local release (no network): a GitHub-shaped release JSON with this repository's download addresses, the two
    # files read from its folder; the packaged exe installs it beside in a temporary folder, hands over, goes back, refuses a damaged zip.
    $taskFakeTag = "Stable-$taskProjectVersion"
    $taskFakeBase = "https://github.com/Shigu330/Tensho-Assistant/releases/download/$taskFakeTag"
    $taskFakeRelease = Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\fake-release.json'
    $taskFake = [ordered]@{ tag_name = $taskFakeTag; draft = $false; prerelease = $false; assets = @(
        [ordered]@{ name = 'TenshoAssistant.Desktop.zip'; browser_download_url = "$taskFakeBase/TenshoAssistant.Desktop.zip"; size = (Get-Item -LiteralPath $taskDesktopZip).Length },
        [ordered]@{ name = 'TenshoAssistant.Desktop.zip.sha256'; browser_download_url = "$taskFakeBase/TenshoAssistant.Desktop.zip.sha256"; size = (Get-Item -LiteralPath $taskDesktopHash).Length }) }
    [IO.File]::WriteAllText($taskFakeRelease, ($taskFake | ConvertTo-Json -Depth 4), [Text.UTF8Encoding]::new($false))
    $taskUpdateOwn = Join-Path $taskRoot 'artifacts\update-selftest'
    $taskUpdate = & (Join-Path $taskDesktopPackage 'TenshoAssistant.Desktop.exe') "--update-selftest=$taskFakeRelease" "--update-own=$taskUpdateOwn" 2>&1 | ForEach-Object { "$_" }
    $taskUpdateCode = $LASTEXITCODE
    $taskUpdate | Where-Object { $_ -match '^(FAIL|PASS update selftest)' } | ForEach-Object { Write-Output "  desktop: $_" }
    if ($taskUpdateCode -ne 0) { $taskUpdate | ForEach-Object { Write-Host "  desktop: $_" }; throw "Desktop update selftest failed (exit $taskUpdateCode): the app could not install, hand over to or go back from a fake release of $taskProjectVersion." }
    Write-Output "PASS desktop update: $taskDesktopHash written; the packaged exe installed a fake release $taskFakeTag beside, handed over, went back, refused a damaged zip"
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
