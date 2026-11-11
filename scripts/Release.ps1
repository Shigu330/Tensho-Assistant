param(
    [Parameter(Mandatory)][ValidateSet('Commit', 'Release')][string]$Mode,
    [switch]$DryRun,
    # 0.1017: urgent fix (game patch, privacy) released before its in-game tests; the reason goes at the top of the release notes.
    [string]$Hotfix,
    # 0.1020: the major number (x of x.y.z, 1.0.0 first) is the maintainer's decision: raising it needs this switch.
    [switch]$Major
)
# Git steps after a green Verify.ps1 (called by Verify.ps1 -Commit / -Release; see CONTRIBUTING.md 'Versions et releases').
#   Commit  : on Devloppement, commits every change as 'x.y.z' with the CHANGELOG entry as body, then pushes Devloppement.
#   Release : from a clean, pushed Devloppement whose CI is green, commits repo.json (Dalamud custom repository) and tags it
#             Stable-x.y.z (local steps); then pushes the tag, creates the GitHub release with latest.zip, TenshoAssistant.Desktop.zip
#             (the desktop application, X-119, R33) and its SHA-256 TenshoAssistant.Desktop.zip.sha256 (its automatic update, X-121;
#             GitHub CLI 'gh'), moves
#             main to the same commit last (fast-forward only: no merge commit, no branch change), and pushes Devloppement.
#             A release stopped halfway (tag pushed, GitHub release or main missing) is resumed by running the same command again:
#             only the missing steps run.
# -DryRun prints every command without running it (the CI check only warns).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$repository = 'Shigu330/Tensho-Assistant'
$developBranch = 'Devloppement'; $releaseBranch = 'main'

function Step([string]$Text, [scriptblock]$Action) {
    Write-Output "> $Text"
    if ($DryRun) { return }
    & $Action
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Failed: $Text" }
}
# Expected stop (already released, nothing new…): a clear message and exit code 2, not an error with a stack trace.
function Stop-Release([string]$Text) { Write-Host $Text -ForegroundColor Yellow; exit 2 }
# Changed paths that are only documentation (*.md, docs/, repo.json).
function Test-DocsOnly([string[]]$Paths) { -not ($Paths | Where-Object { $_ -notmatch '(\.md$|^docs/|^repo\.json$)' }) }
# True when a native command succeeds. Its output is dropped and, as in Invoke-Git, stderr cannot become a terminating error
# (Windows PowerShell 5.1 does that under 'Stop' for « release not found » from gh).
function Test-Native([scriptblock]$Command) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { & $Command *> $null; return ($LASTEXITCODE -eq 0) }
    # 0.1017: the probe's own exit code (git grep finding nothing = 1) must not become the script's exit code.
    finally { $ErrorActionPreference = $previous; $global:LASTEXITCODE = 0 }
}
function Invoke-Git([string[]]$Arguments) {
    # Windows PowerShell 5.1 turns any git line on stderr (e.g. 'From https://…' of a fetch) into a terminating error when redirected:
    # stderr is captured as plain text and only the exit code decides.
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $output = & git @Arguments 2>&1 | ForEach-Object { "$_" } }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') : $output" }
    return $output
}
# 0.1017 guards (stop with exit code 2 before anything is committed or published).
# The maintainer's working files stay outside git: their list is the .gitignore block written in 0.802 (the lines after its
# '# 0.802' comment, up to the next blank line). Each one is still ignored, never tracked, staged or in the released tree.
function Get-WorkingPaths {
    $ignore = @(Get-Content -LiteralPath (Join-Path $root '.gitignore') -Encoding UTF8)
    $start = [Array]::FindIndex($ignore, [Predicate[string]] { param($line) $line.StartsWith('# 0.802') })
    $paths = @()
    if ($start -ge 0) { for ($i = $start + 1; $i -lt $ignore.Count -and $ignore[$i].Trim(); $i++) { if (-not $ignore[$i].StartsWith('#')) { $paths += $ignore[$i].Trim() } } }
    if ($paths.Count -lt 5) { Stop-Release "Stopped: the .gitignore block of the maintainer's working files ('# 0.802') is missing or shorter than its five lines." }
    return $paths
}
function Assert-NoAgentFiles([string]$Tree) {
    $paths = @(Get-WorkingPaths)
    foreach ($path in $paths) {
        $probe = if ($path.EndsWith('/')) { "${path}probe" } else { $path }
        if (-not (Test-Native { git check-ignore -q --no-index $probe })) { Stop-Release "Stopped: '$path' is no longer ignored by .gitignore (the maintainer's working files stay outside git)." }
    }
    $specs = @($paths | ForEach-Object { $_.TrimEnd('/') })
    $found = @(Invoke-Git (@('ls-files', '--') + $specs)) + @(Invoke-Git (@('diff', '--cached', '--name-only', '--') + $specs))
    if ($Tree) { $found += @(Invoke-Git (@('ls-tree', '-r', '--name-only', $Tree, '--') + $specs)) }
    $found = @($found | Where-Object { $_ } | Select-Object -Unique)
    if ($found.Count) { Stop-Release "Stopped: the maintainer's working files are in git: $(($found | Select-Object -First 10) -join ', ')." }
}
# No Discord webhook address in a tracked file (same pattern as the agent's publication check).
function Assert-NoSecret {
    if (Test-Native { git grep -q -I -E 'discord(app)?\.com/api/webhooks/[0-9]+/' -- . ':!tests' }) { Stop-Release 'Stopped: a Discord webhook address is in a tracked file.' }
}
# The CI of the commit being released must be green on GitHub: 'Build and test' succeeded (Windows build, tests, Dalamud-like build),
# and no other finished workflow failed. A workflow still running elsewhere (CodeQL) does not block.
$requiredWorkflow = 'Build and test'
function Assert-CiGreen([string]$Sha) {
    $short = $Sha.Substring(0, 7)
    function Fail-Ci([string]$Text) { if ($DryRun) { Write-Host "WARNING (dry run): $Text" -ForegroundColor Yellow } else { Stop-Release $Text } }
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $json = & $gh run list --repo $repository --commit $Sha --json 'workflowName,status,conclusion' 2>$null; $code = $LASTEXITCODE }
    finally { $ErrorActionPreference = $previous }
    if ($code -ne 0 -or -not $json) { Fail-Ci "Could not read the CI of $short (gh run list): released only with a green CI."; return }
    $runs = @(($json -join "`n") | ConvertFrom-Json)
    $build = @($runs | Where-Object { $_.workflowName -eq $requiredWorkflow })
    if ($build.Count -eq 0) { Fail-Ci "No '$requiredWorkflow' run for $short on GitHub yet: push it first (Verify.ps1 -Commit), wait for the CI, then -Release again."; return }
    if ($build[0].status -ne 'completed') { Fail-Ci "CI '$requiredWorkflow' still running for ${short}: wait for it, then -Release again."; return }
    if ($build[0].conclusion -ne 'success') { Fail-Ci "CI '$requiredWorkflow' ended '$($build[0].conclusion)' for ${short}: fix it before releasing."; return }
    $failed = @($runs | Where-Object { $_.workflowName -ne $requiredWorkflow -and $_.status -eq 'completed' -and $_.conclusion -notin @('success', 'skipped', 'neutral') })
    if ($failed.Count) { Fail-Ci "CI of ${short}: $(($failed | ForEach-Object { "$($_.workflowName) $($_.conclusion)" }) -join ', ')."; return }
    Write-Output "CI: '$requiredWorkflow' green for $short."
}
if ($Hotfix -and $Mode -ne 'Release') { throw '-Hotfix goes with -Release only.' }
if ($Major -and $Mode -ne 'Release') { throw '-Major goes with -Release only.' }
# 0.1020: the plugin's screenshots for the Dalamud installer: images/image1.png to image5.png next to the icon, PNG of 730x380
# at most (docs/project/PUBLISHING.md, point 8), listed in repo.json as ImageUrls. None yet: the entry then has no ImageUrls.
$imageWidth = 730; $imageHeight = 380
function Get-PngSize([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $signature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
    if ($bytes.Length -lt 24) { return $null }
    for ($at = 0; $at -lt 8; $at++) { if ($bytes[$at] -ne $signature[$at]) { return $null } }
    $read = { param($at) ([int]$bytes[$at] -shl 24) -bor ([int]$bytes[$at + 1] -shl 16) -bor ([int]$bytes[$at + 2] -shl 8) -bor [int]$bytes[$at + 3] }
    return @((& $read 16), (& $read 20))
}
function Get-ImageUrls([string]$BaseUrl) {
    $urls = @()
    $folder = Join-Path $root 'src\TenshoAssistant\images'
    foreach ($index in 1..5) {
        $file = Join-Path $folder "image$index.png"
        if (-not (Test-Path -LiteralPath $file)) { continue }
        $size = Get-PngSize $file
        if (-not $size) { Stop-Release "Stopped: images/image$index.png is not a PNG file." }
        if ($size[0] -gt $imageWidth -or $size[1] -gt $imageHeight) { Stop-Release "Stopped: images/image$index.png is $($size[0])x$($size[1]); the installer's screenshots are ${imageWidth}x$imageHeight at most." }
        $urls += "$BaseUrl/image$index.png"
    }
    return , $urls
}
# 0.1020: repo.json is read back before it is committed: one entry, this version, this release's zip, an icon and screenshots that exist.
function Assert-RepoJson([string]$Text, [string]$Version, [string]$Download) {
    try { $parsed = @($Text | ConvertFrom-Json) } catch { Stop-Release "Stopped: repo.json is not valid JSON ($($_.Exception.Message))." }
    if ($parsed.Count -ne 1) { Stop-Release "Stopped: repo.json holds $($parsed.Count) entries; one is expected." }
    $item = $parsed[0]
    $problems = @()
    if ($item.InternalName -ne 'TenshoAssistant') { $problems += "InternalName '$($item.InternalName)'" }
    if ("$($item.AssemblyVersion)" -notmatch "^$([regex]::Escape($Version))(\.0)?$") { $problems += "AssemblyVersion '$($item.AssemblyVersion)' is not $Version" }
    foreach ($key in 'DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting') { if ($item.$key -ne $Download) { $problems += "$key does not point at $Download" } }
    foreach ($key in 'Author', 'Name', 'Description', 'Punchline', 'RepoUrl', 'DalamudApiLevel', 'Changelog') { if (-not "$($item.$key)".Trim()) { $problems += "$key is empty" } }
    $local = Join-Path $root 'src\TenshoAssistant\images'
    foreach ($url in @($item.IconUrl) + @($item.ImageUrls | Where-Object { $_ })) {
        if (-not $url) { $problems += 'IconUrl is empty'; continue }
        if (-not (Test-Path -LiteralPath (Join-Path $local ($url -replace '^.*/', '')))) { $problems += "$url has no file in src/TenshoAssistant/images" }
    }
    if ($problems.Count) { Stop-Release "Stopped: repo.json read back: $($problems -join '; ')." }
    Write-Output "repo.json read back: $($item.AssemblyVersion), icon and $(@($item.ImageUrls | Where-Object { $_ }).Count) screenshot(s)."
}
# X-119 (R33): the desktop application goes in every release next to latest.zip, with the plugin's version (built, drawn headless and
# checked by Verify.ps1, which -Release runs first). A release without it, or with another version inside, is refused.
$desktopZip = Join-Path $root 'src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip'
# X-121: the app updates itself from the release and installs a zip only when its SHA-256 is the one this file publishes beside it.
$desktopHash = "$desktopZip.sha256"
function Assert-DesktopZip([string]$Version) {
    if (-not (Test-Path -LiteralPath $desktopZip)) { throw "Desktop application package not found: $desktopZip (run .\scripts\Verify.ps1 first): a release carries it next to latest.zip (R33)." }
    # X-121: -Release refuses a release without the SHA-256 of the app (or with another one): the app's automatic update needs it.
    if (-not (Test-Path -LiteralPath $desktopHash)) { throw "TenshoAssistant.Desktop.zip.sha256 not found: Release.ps1 refuses a release without the SHA-256 of the desktop application (run .\scripts\Verify.ps1 again)." }
    $published = [regex]::Match([IO.File]::ReadAllText($desktopHash), '^([0-9a-fA-F]{64})  TenshoAssistant\.Desktop\.zip\r?\n?$')
    $actual = (Get-FileHash -LiteralPath $desktopZip -Algorithm SHA256).Hash
    if (-not $published.Success -or $published.Groups[1].Value.ToUpperInvariant() -ne $actual) { throw "TenshoAssistant.Desktop.zip.sha256 is not the SHA-256 of TenshoAssistant.Desktop.zip ($actual): Release.ps1 refuses a release without the SHA-256 of the app it carries (run .\scripts\Verify.ps1 again)." }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($desktopZip)
    $temp = Join-Path ([IO.Path]::GetTempPath()) ('tensho-desktop-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temp | Out-Null
    try {
        foreach ($name in 'TenshoAssistant.Desktop.dll', 'plugin/TenshoAssistant.dll') {
            $entry = $archive.GetEntry($name)
            if (-not $entry) { throw "TenshoAssistant.Desktop.zip has no $name (run .\scripts\Verify.ps1 again)." }
            $file = Join-Path $temp ($name -replace '/', '_')
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $file)
            $found = [Reflection.AssemblyName]::GetAssemblyName($file).Version
            if ("$($found.Major).$($found.Minor).$($found.Build)" -ne $Version) { throw "TenshoAssistant.Desktop.zip: $name is $found, not ${Version}: the app and the plugin are released together (run .\scripts\Verify.ps1 again)." }
        }
    }
    finally { $archive.Dispose(); Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Output "TenshoAssistant.Desktop.zip checked: the app and its plugin are $Version; its SHA-256 $($actual.ToLowerInvariant()) is in TenshoAssistant.Desktop.zip.sha256."
}
if ($PSBoundParameters.ContainsKey('Hotfix') -and $Hotfix.Trim().Length -lt 8) { throw "-Hotfix needs its reason in a few words, e.g. -Hotfix 'game patch 7.58'." }
function Add-HotfixNote([string]$Notes) {
    # 0.1105 (R20 changed on 2026-11-11): without -Hotfix, the release goes out on the « lancement ok » of its version (q.py release-check,
    # run by guard.py): Tensho loaded in game at this version, character logged in, self-test without failure, no error in the log.
    if (-not $Hotfix) {
        return "**Checked at launch, not tried in detail yet**: Tensho loaded in game with this version, a character logged in, its self-test passed and the log showed no error; the detailed in-game tests come later.`n" +
            "**Vérifié au lancement, pas encore essayé en détail** : Tensho s'est chargé en jeu à cette version, personnage connecté, son autotest est passé et le journal n'a montré aucune erreur ; les essais détaillés en jeu viendront ensuite.`n`n$Notes"
    }
    $reason = $Hotfix.Trim()
    return "**Urgent fix** ($reason): released without waiting for the in-game tests; not validated in game.`n" +
        "**Correctif urgent** ($reason) : publié sans attendre les essais en jeu ; non validé en jeu.`n`n$Notes"
}

Push-Location $root
try {
    $version = ([xml](Get-Content -Raw -LiteralPath 'src\TenshoAssistant\TenshoAssistant.csproj')).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $version) { throw 'No <Version> in src/TenshoAssistant/TenshoAssistant.csproj.' }
    $tag = "Stable-$version"

    # CHANGELOG entry of this version: from '## x.y.z' to the next '##'.
    $lines = Get-Content -LiteralPath 'CHANGELOG.md' -Encoding UTF8
    $start = [Array]::FindIndex($lines, [Predicate[string]] { param($line) $line.Trim() -eq "## $version" })
    if ($start -lt 0) { throw "CHANGELOG.md has no '## $version' entry: add it before committing." }
    $end = $start + 1
    while ($end -lt $lines.Count -and -not $lines[$end].StartsWith('## ')) { $end++ }
    # The commit body holds the '### English' part of the entry, then the French part above it (Join-Bilingual below); the release notes
    # and repo.json still hold the English part only.
    $entry = $lines[($start + 1)..($end - 1)]
    $english = [Array]::FindIndex($entry, [Predicate[string]] { param($line) $line.Trim() -eq '### English' })
    if ($english -lt 0) { Write-Host "CHANGELOG.md entry '## $version' has no '### English' section: add the English notes (used for git and GitHub)." -ForegroundColor Yellow; exit 2 }
    $notes = ($entry[($english + 1)..($entry.Count - 1)] -join "`n").Trim()
    if (-not $notes) { Write-Host "The '### English' section of '## $version' is empty." -ForegroundColor Yellow; exit 2 }
    $notesFr = if ($english -gt 0) { ($entry[0..($english - 1)] -join "`n").Trim() } else { '' }

    # Notes of every CHANGELOG version (0.234): a commit or a release covers all the versions since the last one, newest first.
    # English part after '### English', French part before it.
    $entries = [ordered]@{}; $entriesFr = [ordered]@{}
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -notmatch '^## (\d+\.\d+\.\d+)\s*$') { continue }
        $number = $Matches[1]; $next = $index + 1
        while ($next -lt $lines.Count -and -not $lines[$next].StartsWith('## ')) { $next++ }
        $body = [string[]]@(if ($next -gt $index + 1) { $lines[($index + 1)..($next - 1)] })
        $at = [Array]::FindIndex($body, [Predicate[string]] { param($line) $line.Trim() -eq '### English' })
        $entries[$number] = if ($at -ge 0) { ($body[($at + 1)..($body.Count - 1)] -join "`n").Trim() } else { '' }
        $entriesFr[$number] = if ($at -gt 0) { ($body[0..($at - 1)] -join "`n").Trim() } elseif ($at -lt 0) { ($body -join "`n").Trim() } else { '' }
    }
    function Get-NotesSince([string]$Since, [switch]$French) {
        $from = if ($Since) { [version]$Since } else { [version]'0.0.0' }
        # 0.1093 (audit A070): older entries live in docs/changelog/ (tools/changelog.py); the versions since the last release must stay here.
        $oldest = @($entries.Keys | Sort-Object { [version]$_ } | Select-Object -First 1)
        if ($Since -and $oldest.Count -gt 0 -and [version]$oldest[0] -gt $from) { throw "CHANGELOG.md starts after ${Since}: keep every version since the last release in it (tools/changelog.py moves only older ones)." }
        $covered = @($entries.Keys | Where-Object { [version]$_ -gt $from -and [version]$_ -le [version]$version } | Sort-Object { [version]$_ } -Descending)
        if ($covered.Count -le 1) { return $(if ($French) { $notesFr } else { $notes }) }
        $source = if ($French) { $entriesFr } else { $entries }
        $missing = if ($French) { '(pas de notes en français)' } else { '(no English notes)' }
        return ($covered | ForEach-Object { "## $_`n" + $(if ($source[$_]) { $source[$_] } else { $missing }) }) -join "`n`n"
    }
    # Git and GitHub in two languages (2026-11-09): English first, then this marker line, then French. Same format as tools/bilingual.py
    # (its self-test reads the marker here).
    $bilingualMarker = 'Français :'
    function Join-Bilingual([string]$English, [string]$French) {
        if (-not $French.Trim()) { return $English.Trim() }
        return "$($English.Trim())`n`n$bilingualMarker`n`n$($French.Trim())"
    }

    $branch = (Invoke-Git @('rev-parse', '--abbrev-ref', 'HEAD')).Trim()
    if ($branch -ne $developBranch) { throw "Current branch is '$branch': commits and releases start from '$developBranch' only." }

    # Already released: the tag of this version exists here or on GitHub.
    $released = [bool](Invoke-Git @('tag', '--list', $tag)) -or [bool](& git ls-remote --tags origin "refs/tags/$tag")
    $releaseUrl = "https://github.com/$repository/releases/tag/$tag"
    $changed = @(Invoke-Git @('status', '--porcelain') | ForEach-Object { ($_ -replace '^...', '').Trim('"') -replace '^.* -> ', '' })
    $lastSubject = (Invoke-Git @('log', '-1', '--format=%s')).Trim()

    if ($Mode -eq 'Commit') {
        if ($changed.Count -gt 0) {
            # Subject: 'x.y.z' the first time, 'x.y.z · suite' for more work before the release, 'x.y.z · docs' after it (docs only).
            if ($released -and -not (Test-DocsOnly $changed)) {
                Stop-Release "Version $version is already released ($releaseUrl). Raise <Version> in src/TenshoAssistant/TenshoAssistant.csproj and add '## <new version>' at the top of CHANGELOG.md before committing code."
            }
            $subject = if ($released) { "$version · docs" } elseif ($lastSubject -like "$version*") { "$version · suite" } else { $version }
            $message = Join-Path ([IO.Path]::GetTempPath()) "tensho-commit-$version.txt"
            # Body: every version since the last committed one (subject 'x.y.z…' of the latest commit carrying a version).
            $lastCommitted = @(Invoke-Git @('log', '-200', '--format=%s') | ForEach-Object { if ($_ -match '^(\d+\.\d+\.\d+)') { $Matches[1] } } | Select-Object -First 1)
            $since = if ($lastCommitted.Count -gt 0 -and [version]$lastCommitted[0] -lt [version]$version) { $lastCommitted[0] } else { $version }
            $commitNotes = if ($since -eq $version) { Join-Bilingual $notes $notesFr } else { Join-Bilingual (Get-NotesSince $since) (Get-NotesSince $since -French) }
            # 0.446: optional last line (e.g. the agent's 'Co-Authored-By: …'), from the environment; empty for the maintainer's own commits.
            $trailer = if ($env:TENSHO_COMMIT_TRAILER) { "`n$($env:TENSHO_COMMIT_TRAILER.Trim())`n" } else { '' }
            [IO.File]::WriteAllText($message, "$subject`n`n$commitNotes`n$trailer", [Text.UTF8Encoding]::new($false))
            if ($since -ne $version) { Write-Output "Commit message covers the versions after $since." }
            if ($DryRun) { Write-Output "--- commit message ---`n$subject`n`n$commitNotes`n$trailer----------------------" }
            # 0.323: the files « git add -A » adds for the first time are listed; a new archive, binary or file over 5 MB outside the
            # ignored folders stops the commit (exit code 2) instead of being published by mistake.
            $untracked = @(Invoke-Git @('-c', 'core.quotePath=false', 'ls-files', '--others', '--exclude-standard') | Where-Object { $_ })
            if ($untracked.Count -gt 0) { Write-Output "New files to add ($($untracked.Count)):"; $untracked | ForEach-Object { Write-Output "  + $_" } }
            $blocked = @($untracked | Where-Object { $_ -match '\.(zip|7z|dll|exe|pdb)$' -or ((Test-Path -LiteralPath $_ -PathType Leaf) -and (Get-Item -LiteralPath $_).Length -gt 5MB) })
            if ($blocked.Count -gt 0) { Stop-Release "Not committed: new archive, binary or large file(s): $($blocked -join ', '). Move them out of the repository or add them to .gitignore." }
            Assert-NoAgentFiles; Assert-NoSecret
            Step "git add -A" { git add -A }
            if (-not $DryRun) { Assert-NoAgentFiles }
            Step "git commit -F <message '$subject'>" { git commit -F $message }
        }
        else { Write-Output "Nothing to commit on $developBranch." }
        # Push only when there is something GitHub does not have yet.
        $ahead = if ($DryRun -and $changed.Count -gt 0) { 1 } else { [int](& git rev-list --count "origin/$developBranch..HEAD" 2>$null) }
        if ($ahead -eq 0) { Write-Output "Nothing to push: origin/$developBranch is up to date."; return }
        Step "git push origin $developBranch" { git push origin $developBranch }
        Write-Output ($(if ($DryRun) { "DRY RUN: nothing was committed or pushed (version $version)." } else { "COMMIT $version pushed on $developBranch." }))
        return
    }

    # Release (0.323): every check first; then the tag and the GitHub release with its zip; main, which carries repo.json (what the
    # players' Dalamud reads), is pushed only last, so repo.json never points at a download that does not exist yet. When the tag exists
    # but its GitHub release does not (gh failed last time), the release is resumed: only the missing steps run.
    # gh from PATH, else its usual install folders (a terminal opened before installing GitHub CLI does not see it yet).
    $command = Get-Command gh -ErrorAction SilentlyContinue; $gh = if ($command) { $command.Source } else { $null }
    foreach ($candidate in @("$env:ProgramFiles\GitHub CLI\gh.exe", "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe")) { if (-not $gh -and (Test-Path -LiteralPath $candidate)) { $gh = $candidate } }
    if (-not $gh) {
        throw "GitHub CLI 'gh' not found: install it (winget install GitHub.cli), then run 'gh auth login' once."
    }
    $resume = $false; $hasRelease = $false
    if ($released) {
        # 0.326: a release is finished when GitHub has it AND origin/main contains its tag; otherwise the missing steps are resumed.
        $hasRelease = Test-Native { & $gh release view $tag --repo $repository }
        Invoke-Git @('fetch', 'origin', '--tags') | Out-Null
        $mainHasTag = (Test-Native { git rev-parse --verify --quiet "refs/tags/$tag" }) -and (Test-Native { git merge-base --is-ancestor $tag "origin/$releaseBranch" })
        if ($hasRelease -and $mainHasTag) { Stop-Release "Version $version is already released: $releaseUrl. Raise <Version> (and the CHANGELOG entry) for a new release." }
        $resume = $true
        $missing = if (-not $hasRelease) { 'its GitHub release' } else { "origin/$releaseBranch (repo.json)" }
        Write-Output "Tag $tag exists but $missing is missing: resuming the release (only the missing steps run)."
    }
    if (-not (Test-Native { & $gh auth status })) { throw "GitHub CLI is not logged in: run 'gh auth login' once." }
    if (Invoke-Git @('status', '--porcelain')) { Stop-Release "Uncommitted changes: run '.\scripts\Verify.ps1 -Commit' first, then -Release." }
    Invoke-Git @('fetch', 'origin') | Out-Null
    $behind = [int](Invoke-Git @('rev-list', '--count', "HEAD..origin/$developBranch"))
    if ($behind -gt 0) { Stop-Release "$developBranch is $behind commit(s) behind origin/${developBranch}: pull first (git pull)." }
    if (-not (Test-Native { git merge-base --is-ancestor "origin/$releaseBranch" HEAD })) { Stop-Release "origin/$releaseBranch has commits that $developBranch does not have (a change merged on GitHub?): merge origin/$releaseBranch into $developBranch first." }
    $zip = Join-Path $root 'src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip'
    if (-not (Test-Path -LiteralPath $zip)) { throw "Package not found: $zip (run .\scripts\Verify.ps1 first)." }
    Assert-DesktopZip $version
    $notesFile = Join-Path ([IO.Path]::GetTempPath()) "tensho-notes-$version.md"

    if ($resume) {
        Invoke-Git @('fetch', 'origin', '--tags') | Out-Null
        $commit = (Invoke-Git @('rev-list', '-n', '1', $tag)).Trim()
        $lastReleased = @(Invoke-Git @('tag', '--list', 'Stable-*') | ForEach-Object { $_ -replace '^Stable-', '' } | Where-Object { $_ -match '^\d+\.\d+\.\d+$' -and [version]$_ -lt [version]$version } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1)
        if ($lastReleased.Count -gt 0) { $notes = Get-NotesSince $lastReleased[0] }
        $notes = Add-HotfixNote $notes
        [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))
        Assert-NoAgentFiles $commit
        if (-not (& git ls-remote --tags origin "refs/tags/$tag")) { Step "git push origin $tag" { git push origin $tag } }
        if (-not $hasRelease) { Step "gh release create $tag latest.zip TenshoAssistant.Desktop.zip TenshoAssistant.Desktop.zip.sha256" { & $gh release create $tag $zip $desktopZip $desktopHash --repo $repository --title "Tensho Assistant $version" --notes-file $notesFile --verify-tag } }
        else {
            # A release created before its desktop package (R33) or its SHA-256 (X-121) was attached gets them now.
            $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
            try { $assets = @(& $gh release view $tag --repo $repository --json assets --jq '.assets[].name' 2>$null) }
            finally { $ErrorActionPreference = $previous; $global:LASTEXITCODE = 0 }
            if ('TenshoAssistant.Desktop.zip' -notin $assets) { Step "gh release upload $tag TenshoAssistant.Desktop.zip" { & $gh release upload $tag $desktopZip --repo $repository } }
            if ('TenshoAssistant.Desktop.zip.sha256' -notin $assets) { Step "gh release upload $tag TenshoAssistant.Desktop.zip.sha256" { & $gh release upload $tag $desktopHash --repo $repository } }
        }
        # Fast-forward only: GitHub refuses a push of main that is not one (no force anywhere).
        Step "git push origin ${commit}:$releaseBranch (fast-forward)" { git push origin "${commit}:refs/heads/$releaseBranch" }
        Step "git merge --ff-only $tag (on $developBranch)" { git merge --ff-only $commit }
        Step "git push origin $developBranch" { git push origin $developBranch }
        Write-Output ($(if ($DryRun) { "DRY RUN: nothing was published ($tag)." } else { "RELEASE $tag published (resumed): $releaseUrl" }))
        return
    }

    $newCommits = @(Invoke-Git @('log', '--format=%s', "origin/$releaseBranch..HEAD") | Where-Object { $_ })
    if ($newCommits.Count -eq 0) { Stop-Release "Nothing new on $developBranch since the last release (origin/$releaseBranch)." }
    # 0.1017: what is released is what GitHub has and checked: Devloppement pushed, its CI green, no agent file, no secret.
    $ahead = [int](Invoke-Git @('rev-list', '--count', "origin/$developBranch..HEAD"))
    if ($ahead -gt 0) {
        $text = "$developBranch has $ahead commit(s) not pushed: run '.\scripts\Verify.ps1 -Commit' first, wait for the CI, then -Release."
        if ($DryRun) { Write-Host "WARNING (dry run): $text" -ForegroundColor Yellow } else { Stop-Release $text }
    }
    Assert-NoAgentFiles 'HEAD'; Assert-NoSecret
    Assert-CiGreen ((Invoke-Git @('rev-parse', 'HEAD')).Trim())

    # Release notes: every version since the last published release (highest Stable-x.y.z tag here or on GitHub).
    Invoke-Git @('fetch', 'origin', '--tags') | Out-Null
    $lastReleased = @(Invoke-Git @('tag', '--list', 'Stable-*') | ForEach-Object { $_ -replace '^Stable-', '' } | Where-Object { $_ -match '^\d+\.\d+\.\d+$' -and [version]$_ -lt [version]$version } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1)
    if ($lastReleased.Count -gt 0) { $notes = Get-NotesSince $lastReleased[0]; Write-Output "Release notes cover the versions after $($lastReleased[0])." }
    # 0.1020: a new major number (1.0.0 first) is released only on the maintainer's word (-Major).
    $lastMajor = if ($lastReleased.Count -gt 0) { ([version]$lastReleased[0]).Major } else { 0 }
    if (([version]$version).Major -gt $lastMajor -and -not $Major) { Stop-Release "Stopped: $version raises the major number (last release $($lastReleased[0])); that is the maintainer's decision: -Major." }
    $notes = Add-HotfixNote $notes
    if ($Hotfix) { Write-Output "Urgent fix: $($Hotfix.Trim())" }
    if ($DryRun) { Write-Output "--- release notes ---`n$notes`n---------------------" }
    # repo.json: Dalamud custom repository entry built from the packaged manifest, pointing at this release's zip.
    $output = 'src\TenshoAssistant\bin\x64\Release'
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $output 'TenshoAssistant.json') | ConvertFrom-Json
    $download = "https://github.com/$repository/releases/download/$tag/latest.zip"
    $imagesUrl = "https://raw.githubusercontent.com/$repository/$releaseBranch/src/TenshoAssistant/images"
    $imageUrls = Get-ImageUrls $imagesUrl
    $entry = [ordered]@{
        Author = $manifest.Author; Name = $manifest.Name; InternalName = $manifest.InternalName
        AssemblyVersion = $manifest.AssemblyVersion; TestingAssemblyVersion = $manifest.AssemblyVersion
        Description = $manifest.Description; Punchline = $manifest.Punchline; ApplicableVersion = $manifest.ApplicableVersion
        RepoUrl = $manifest.RepoUrl; Tags = $manifest.Tags; DalamudApiLevel = $manifest.DalamudApiLevel
        IconUrl = "$imagesUrl/icon.png"
        DownloadLinkInstall = $download; DownloadLinkUpdate = $download; DownloadLinkTesting = $download
        IsHide = $false; IsTestingExclusive = $false; AcceptsFeedback = $manifest.AcceptsFeedback
        LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds(); Changelog = $notes
    }
    if ($imageUrls.Count) { $entry.ImageUrls = $imageUrls }
    $json = ConvertTo-Json @($entry) -Depth 4
    Assert-RepoJson $json $version $download
    [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))

    # Local steps first: repo.json committed on Devloppement and tagged. Nothing is public yet. No branch change (0.1017).
    Step "write repo.json ($version)" { [IO.File]::WriteAllText((Join-Path $root 'repo.json'), $json + "`n", [Text.UTF8Encoding]::new($false)) }
    if (-not $DryRun) { Assert-RepoJson ([IO.File]::ReadAllText((Join-Path $root 'repo.json'))) $version $download }
    Step "git commit repo.json" { git add repo.json; git commit -m "$version · repo.json" }
    if (-not $DryRun) { Assert-NoAgentFiles 'HEAD' }
    Step "git tag -a $tag" { git tag -a $tag -m "Tensho Assistant $version" }
    # Public steps: the tag and the GitHub release with the zip, then main (repo.json) last, moved to the tagged commit as a
    # fast-forward (origin/main is an ancestor of Devloppement, checked above; GitHub refuses anything else, no force anywhere),
    # then Devloppement.
    Step "git push origin $tag" { git push origin $tag }
    Step "gh release create $tag latest.zip TenshoAssistant.Desktop.zip TenshoAssistant.Desktop.zip.sha256" { & $gh release create $tag $zip $desktopZip $desktopHash --repo $repository --title "Tensho Assistant $version" --notes-file $notesFile --verify-tag }
    Step "git push origin HEAD:$releaseBranch (fast-forward)" { git push origin "HEAD:refs/heads/$releaseBranch" }
    Step "git push origin $developBranch" { git push origin $developBranch }
    Write-Output ($(if ($DryRun) { "DRY RUN: nothing was committed, tagged or published ($tag)." } else { "RELEASE $tag published: https://github.com/$repository/releases/tag/$tag" }))
    Write-Output "Custom repository URL: https://raw.githubusercontent.com/$repository/$releaseBranch/repo.json"
}
finally { Pop-Location }
