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
    [switch]$TestsOnly,
    # X-283 (0.1299): says what this run would decide (built beside the game or where it loads, skipped by the stamp or not, the
    # build's arguments) as one JSON line, then stops: nothing built, nothing written. scripts/agent/noreload-test.sh (check.sh) runs it,
    # with a made-up stamp (TENSHO_VERIFY_STAMP, read with -Plan only), to prove the « player busy » path on the real decision.
    [switch]$Plan,
    # X-268 (R19.3): the player is in a duty (or an ocean voyage, competitive content): the plugin is built into artifacts\noreload\, not
    # into the folder the game loads, so the game does not reload it now; the next run without it (the next free publish) loads it. Every
    # check runs the same on that build. A release is always built where the game loads it: refused with -Release.
    [switch]$NoReload
)
$ErrorActionPreference = 'Stop'
# X-268 (0.1277, found by the trial of 0.1276): publish.sh sets TENSHO_NORELOAD=1 while the player is busy, and every Verify.ps1 started under
# it builds beside the game too: the pre-push hook (.githooks/pre-push runs Verify.ps1 with no switch) had rebuilt where the game loads.
if ($env:TENSHO_NORELOAD -eq '1' -and -not $Release) { $NoReload = [switch]::Present }
if ($NoReload -and $Release) { throw '-NoReload builds beside the game: a release is always built where the game loads it (R19.3); run it when the player is free.' }
if ($TestsOnly -and ($Commit -or $Release)) { throw '-TestsOnly checks the logic only: it cannot be combined with -Commit or -Release.' }
if ($Plan -and ($Commit -or $Release -or $TestsOnly)) { throw '-Plan only says the decision: it cannot be combined with -Commit, -Release or -TestsOnly.' }
if ($PSBoundParameters.ContainsKey('Hotfix') -and -not $Release) { throw '-Hotfix goes with -Release only.' }
if ($Major -and -not $Release) { throw '-Major goes with -Release only.' }
if ($PSBoundParameters.ContainsKey('Hotfix') -and $Hotfix.Trim().Length -lt 8) { throw "-Hotfix needs its reason in a few words, e.g. -Hotfix 'game patch 7.58'." }
# X-01 (0.1311): the whole run below normal priority (the build, the tests alongside and the desktop pages inherit it): the game keeps the
# processor while a relay publishes during play. 0.1312: the 190 to 430 ms frame 15 to 25 s after a load made by this run is not the
# processor's: it follows the size of the managed heap (none under 920 MB, then growing with it), filled by the earlier instances that a
# developer reload leaves alive (D-144); a player's single load does not have it.
try { (Get-Process -Id $PID).PriorityClass = [System.Diagnostics.ProcessPriorityClass]::BelowNormal } catch { }
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
# 0.1112 (system plan 4.21): the documents left out above (*.md, docs/, repo.json) have their own SHA-256 in the stamp (Documents): when only
# they changed, the build is still skipped but the managed tests run again (many of them read those documents), then the stamp is updated.
function Get-TaskEnvironment {
    $dalamudHome = if ($env:DALAMUD_HOME) { $env:DALAMUD_HOME } else { Join-Path $env:APPDATA 'XIVLauncher\addon\Hooks\dev' }
    $dalamud = Join-Path $dalamudHome 'Dalamud.dll'
    $dalamudVersion = if (Test-Path -LiteralPath $dalamud) { (Get-Item -LiteralPath $dalamud).VersionInfo.FileVersion } else { 'none' }
    $sdk = try { (& $DotNet --version 2>$null | Select-Object -First 1) } catch { 'unknown' }
    return "dalamud=$dalamudVersion;sdk=$sdk"
}
# 0.1203: -CountPlaceholder hashes CHANGELOG.md with its first test count put back to the placeholder publish.sh fills after a green run
# (« Verify.ps1 (agent) : VERIFYCOUNT, 0 avertissement »), byte for byte otherwise: equal to the stamp, that count is the only change since.
$taskCountLine = [regex]::new('Verify\.ps1 \(agent\) : \d+/\d+, 0 avertissement')
function Get-TaskFingerprint([switch]$Documents, [switch]$CountPlaceholder) {
    $files = & git -C $taskRoot ls-files -co --exclude-standard | Where-Object { ($_ -match '(\.md$|^docs/|^repo\.json$)') -eq [bool]$Documents } | Sort-Object
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        if (-not $Documents) { $environment = [Text.Encoding]::UTF8.GetBytes((Get-TaskEnvironment) + "`n"); [void]$hasher.TransformBlock($environment, 0, $environment.Length, $null, 0) }
        foreach ($file in $files) {
            $path = Join-Path $taskRoot $file
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
            $name = [Text.Encoding]::UTF8.GetBytes($file + "`n"); [void]$hasher.TransformBlock($name, 0, $name.Length, $null, 0)
            $bytes = [IO.File]::ReadAllBytes($path)
            if ($CountPlaceholder -and $file -eq 'CHANGELOG.md') { $bytes = [Text.Encoding]::UTF8.GetBytes($taskCountLine.Replace([Text.Encoding]::UTF8.GetString($bytes), 'Verify.ps1 (agent) : VERIFYCOUNT, 0 avertissement', 1)) }
            [void]$hasher.TransformBlock($bytes, 0, $bytes.Length, $null, 0)
        }
        [void]$hasher.TransformFinalBlock([byte[]]::new(0), 0, 0)
        return [BitConverter]::ToString($hasher.Hash).Replace('-', '')
    }
    finally { $hasher.Dispose() }
}
$taskStampFile = Join-Path $taskRoot 'artifacts\verify-stamp.json'
if ($Plan -and $env:TENSHO_VERIFY_STAMP) { $taskStampFile = $env:TENSHO_VERIFY_STAMP }   # X-283: a made-up stamp, for the decision only
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
$taskDocuments = Get-TaskFingerprint -Documents
# 0.1145 (X-170, end of X-144): the stamp also says which commit was built and whether the tree was clean, read before the build; a
# release (new or resumed) uploads its packages only when they come from the sources of the commit it publishes (scripts/LaunchProof.ps1).
$taskPrevious = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
try { $taskHead = "$(& git -C $taskRoot rev-parse HEAD 2>$null)".Trim(); $taskClean = -not (& git -C $taskRoot status --porcelain 2>$null) }
finally { $ErrorActionPreference = $taskPrevious; $global:LASTEXITCODE = 0 }
# Measure 7 (0.1201): the time of each step of a full run, said in one line (« TIMES … ») before the version line.
$taskClock = [Diagnostics.Stopwatch]::StartNew(); $taskWhole = [Diagnostics.Stopwatch]::StartNew(); $taskTimes = New-Object System.Collections.Generic.List[string]
function Add-TaskTime([string]$Name) { $taskTimes.Add("$Name " + $taskClock.Elapsed.TotalSeconds.ToString('0.0', [Globalization.CultureInfo]::InvariantCulture) + ' s'); $taskClock.Restart() }
$taskSkip = $false
# 0.1020: a release always runs every check again (as -Force): what is published is built and tested now, not taken from a stamp.
if (-not $Force -and -not $Release -and (Test-Path -LiteralPath $taskStampFile) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip')) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip')) -and (Test-Path -LiteralPath (Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip.sha256'))) {
    $taskStamp = Get-Content -Raw -LiteralPath $taskStampFile | ConvertFrom-Json
    $taskSkip = $taskStamp.Version -eq $taskVersionNow -and $taskStamp.Fingerprint -eq $taskFingerprint
    # X-268: a build made beside the game (-NoReload) is never taken for the one the game loads: a run without -NoReload builds again.
    if ($taskStamp.NoReload -and -not $NoReload) { $taskSkip = $false }
}
# X-268, X-283: where this run builds the plugin; the build, the package checks and -Plan read these same two lines.
$taskNoReloadOutput = Join-Path $taskRoot 'artifacts\noreload\'
$taskOutput = if ($NoReload) { $taskNoReloadOutput.TrimEnd('\') } else { Join-Path $taskRoot 'src\TenshoAssistant\bin\x64\Release' }
if ($Plan) {
    [ordered]@{ Version = $taskVersionNow; Fingerprint = $taskFingerprint; NoReload = [bool]$NoReload; Skip = [bool]$taskSkip; Output = $taskOutput
        BuildArgs = $(if ($NoReload) { "-p:TenshoOutputPath=$taskNoReloadOutput" } else { '' }) } | ConvertTo-Json -Compress
    exit 0
}
if ($taskSkip -and $taskStamp.Documents -ne $taskDocuments) {
    # 0.1203 (relay 37, publish.sh measured): publish.sh writes the test count into the new CHANGELOG entry right after the green run;
    # that alone ran every managed test again at -Commit (about 60 s a version). Only that count changed: nothing to run again.
    if ((Get-TaskFingerprint -Documents -CountPlaceholder) -eq $taskStamp.Documents) {
        Write-Output "SKIP checks: version $taskVersionNow already verified green on $($taskStamp.VerifiedAt); since then only the test count was written into CHANGELOG.md."
    }
    else {
    # 0.1112 (4.21): same sources, other documents: no build, but the managed tests again (they read CHANGELOG, README, docs/).
    Write-Output "SKIP build: version $taskVersionNow already built green on $($taskStamp.VerifiedAt) with the same sources; tracked documents changed: managed tests run again."
    Push-Location $taskRoot
    try {
        if (-not (Test-Path -LiteralPath (Join-Path $taskRoot 'tests\TenshoAssistant.Tests\bin\Release\net10.0\TenshoAssistant.Tests.dll'))) {
            & $DotNet build tests/TenshoAssistant.Tests -c Release --nologo
            if ($LASTEXITCODE -ne 0) { throw 'Test project build failed.' }
        }
        & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Managed tests failed (tracked documents changed since the last green run).' }
    }
    finally { Pop-Location }
    }
    $taskStamp | Add-Member -NotePropertyName Documents -NotePropertyValue $taskDocuments -Force
    $taskStamp | ConvertTo-Json | Set-Content -LiteralPath $taskStampFile -Encoding UTF8
}
elseif ($taskSkip) { Write-Output "SKIP checks: version $taskVersionNow already verified green on $($taskStamp.VerifiedAt) with the same sources and documents (-Force to run again)." }
else {
$taskTests = $null
Push-Location $taskRoot
try {
    if ($NoReload) { & $DotNet build TenshoAssistant.sln -c Release --nologo "-p:TenshoOutputPath=$taskNoReloadOutput" }
    else { & $DotNet build TenshoAssistant.sln -c Release --nologo }
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    Add-TaskTime 'build'
    # X-267 (0.1275, R36.2): the managed tests (in parallel by class: tests/TenshoAssistant.Tests/TestRunner.cs) run alongside the other
    # checks (languages, package, the desktop application and its pages), which do not change what they read; waited for after the update
    # step, their output said then, as before. Measured before (13 November): tests 104.4 s one after the other, then the rest.
    New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'artifacts') | Out-Null
    $taskTestsOut = Join-Path $taskRoot 'artifacts\tests-output.txt'; $taskTestsErr = Join-Path $taskRoot 'artifacts\tests-errors.txt'
    $taskTestsClock = [Diagnostics.Stopwatch]::StartNew()
    $taskTests = Start-Process -FilePath $DotNet -ArgumentList @('run', '--project', 'tests/TenshoAssistant.Tests', '-c', 'Release', '--no-build') -WorkingDirectory $taskRoot -NoNewWindow -PassThru -RedirectStandardOutput $taskTestsOut -RedirectStandardError $taskTestsErr
    $null = $taskTests.Handle   # kept, so that its exit code can be read once it ended
    # 0.323: the validator's report (MISSING / UNKNOWN keys) is shown when it fails; it was always discarded.
    $taskLanguages = & $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build -- --validate-localization src/TenshoAssistant/Localization/Languages
    if ($LASTEXITCODE -ne 0) { $taskLanguages | ForEach-Object { Write-Host "$_" }; throw 'Localization validation failed.' }
    Write-Output 'PASS localization metadata, duplicate keys and format compatibility (partial translations allowed)'
    Add-TaskTime 'languages'
    if ($NoReload) { Write-Output "NO RELOAD: the plugin was built into $taskOutput (the game keeps the DLL it loaded; the next free publish loads this version)." }
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
        foreach ($taskExpected in @('TenshoAssistant.dll', 'TenshoAssistant.json', 'Raphael.Net.dll', 'raphael_bindings.dll', 'Microsoft.Data.Sqlite.dll', 'SQLitePCLRaw.core.dll', 'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll', 'e_sqlite3.dll')) {
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
    Add-TaskTime 'package'
    # X-119 (R33): the desktop application (src/TenshoAssistant.Desktop) goes with the plugin: published here self-contained (win-x64, with
    # Microsoft's MIT .NET runtime, so a double-click works on a PC that has only XIVLauncher), with the plugin's own files under plugin\;
    # that very folder is run headless (every page drawn: a page that breaks the app fails Verify, so publish.sh and the release), then
    # zipped as TenshoAssistant.Desktop.zip with the same version. Without the game data or XIVLauncher's Dalamud (CI, contributors) the
    # headless run is an explicit SKIP; -Release refuses it.
    $taskDesktopPackage = Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\package'
    # Measure 7 (0.1201, measured: build 8.9 s, tests 104.4 s, languages 1.2 s, package 0.1 s, publish into the emptied folder 3.5 s, pages
    # 26.3 s, zip 3.2 s, update 1.5 s): emptying this folder at each run costs about 3 s of 149, so it stays emptied (no stale file, ever).
    if (Test-Path -LiteralPath $taskDesktopPackage) { Remove-Item -LiteralPath $taskDesktopPackage -Recurse -Force }
    & $DotNet publish src/TenshoAssistant.Desktop -c Release -r win-x64 --self-contained -p:DebugType=none -o $taskDesktopPackage --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Desktop application publish failed (src/TenshoAssistant.Desktop).' }
    $taskDesktopPlugin = Join-Path $taskDesktopPackage 'plugin'
    New-Item -ItemType Directory -Force -Path $taskDesktopPlugin | Out-Null
    foreach ($taskName in @('TenshoAssistant.dll', 'TenshoAssistant.json', 'Raphael.Net.dll', 'raphael_bindings.dll', 'Microsoft.Data.Sqlite.dll', 'SQLitePCLRaw.core.dll', 'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll', 'e_sqlite3.dll')) { Copy-Item -LiteralPath (Join-Path $taskOutput $taskName) -Destination $taskDesktopPlugin }
    $taskDesktopVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $taskDesktopPackage 'TenshoAssistant.Desktop.dll')).Version.ToString()
    if ($taskDesktopVersion -ne $taskAssemblyVersion) { throw "Desktop application version $taskDesktopVersion differs from the plugin's $taskAssemblyVersion (one version source: the plugin's csproj)." }
    Add-TaskTime 'desktop publish'
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
    Add-TaskTime 'desktop pages'
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
                'plugin/TenshoAssistant.dll', 'plugin/TenshoAssistant.json', 'plugin/Raphael.Net.dll', 'plugin/raphael_bindings.dll', 'plugin/e_sqlite3.dll', 'plugin/Microsoft.Data.Sqlite.dll')) {
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
    Add-TaskTime 'desktop zip'
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
    Add-TaskTime 'desktop update'
    $taskTests.WaitForExit(); $taskTestsClock.Stop()
    Get-Content -LiteralPath $taskTestsOut -Encoding UTF8 | ForEach-Object { Write-Output $_ }
    Get-Content -LiteralPath $taskTestsErr -Encoding UTF8 | ForEach-Object { Write-Output $_ }
    if ($taskTests.ExitCode -ne 0) { throw 'Managed tests failed.' }
    Add-TaskTime 'tests'
    $taskTimes.Add('tests alongside ' + $taskTestsClock.Elapsed.TotalSeconds.ToString('0.0', [Globalization.CultureInfo]::InvariantCulture) + ' s')
    Write-Output ("TIMES " + ($taskTimes -join ', ') + '; whole run ' + $taskWhole.Elapsed.TotalSeconds.ToString('0.0', [Globalization.CultureInfo]::InvariantCulture) + ' s')
    Write-Output "VERSION $taskProjectVersion (DLL $taskAssemblyVersion)"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $taskStampFile) | Out-Null
    # X-185 (0.1213, night review N18): HEAD and the tree read again after the build (a commit or a change during the run makes the stamp « not
    # clean »), and the SHA-256 of each package built, which a release compares with the very files it uploads (Test-PackagesFromCommit).
    $taskPrevious = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $taskHeadAfter = "$(& git -C $taskRoot rev-parse HEAD 2>$null)".Trim(); $taskCleanAfter = -not (& git -C $taskRoot status --porcelain 2>$null) }
    finally { $ErrorActionPreference = $taskPrevious; $global:LASTEXITCODE = 0 }
    $taskPackages = [ordered]@{}
    foreach ($taskPackage in @((Join-Path $taskOutput 'TenshoAssistant\latest.zip'), (Join-Path $taskRoot 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip'))) {
        if (Test-Path -LiteralPath $taskPackage) { $taskPackages[[IO.Path]::GetFileName($taskPackage)] = (Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    [ordered]@{ Version = $taskProjectVersion; Fingerprint = $taskFingerprint; Documents = $taskDocuments; Commit = $taskHead; Clean = [bool]($taskClean -and $taskCleanAfter -and $taskHeadAfter -eq $taskHead); Packages = $taskPackages; NoReload = [bool]$NoReload; VerifiedAt = (Get-Date).ToString('s') } | ConvertTo-Json | Set-Content -LiteralPath $taskStampFile -Encoding UTF8
}
finally {
    # X-267: a step that failed while the tests ran alongside never leaves them running.
    if ($taskTests -and -not $taskTests.HasExited) { $taskPrevious = $ErrorActionPreference; $ErrorActionPreference = 'Continue'; & taskkill.exe /T /F /PID $taskTests.Id 2>&1 | Out-Null; $ErrorActionPreference = $taskPrevious; $global:LASTEXITCODE = 0 }
    Pop-Location
}
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
