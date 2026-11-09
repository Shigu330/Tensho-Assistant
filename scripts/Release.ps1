param(
    [Parameter(Mandatory)][ValidateSet('Commit', 'Release')][string]$Mode,
    [switch]$DryRun
)
# Git steps after a green Verify.ps1 (called by Verify.ps1 -Commit / -Release; see CONTRIBUTING.md 'Versions et releases').
#   Commit  : on Devloppement, commits every change as 'x.y.z' with the CHANGELOG entry as body, then pushes Devloppement.
#   Release : from a clean Devloppement, commits repo.json (Dalamud custom repository), merges into main and tags Stable-x.y.z
#             (local steps); then pushes the tag, creates the GitHub release with latest.zip (GitHub CLI 'gh'), pushes main
#             (repo.json) last, and brings Devloppement up to date. A release stopped halfway (tag pushed, GitHub release or main
#             missing) is resumed by running the same command again: only the missing steps run.
# -DryRun prints every command without running it.
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
    finally { $ErrorActionPreference = $previous }
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
    if ($branch -ne $developBranch) { throw "Current branch is '$branch': switch to '$developBranch' (git checkout $developBranch) first." }

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
            Step "git add -A" { git add -A }
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
    $notesFile = Join-Path ([IO.Path]::GetTempPath()) "tensho-notes-$version.md"

    if ($resume) {
        Invoke-Git @('fetch', 'origin', '--tags') | Out-Null
        $commit = (Invoke-Git @('rev-list', '-n', '1', $tag)).Trim()
        $lastReleased = @(Invoke-Git @('tag', '--list', 'Stable-*') | ForEach-Object { $_ -replace '^Stable-', '' } | Where-Object { $_ -match '^\d+\.\d+\.\d+$' -and [version]$_ -lt [version]$version } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1)
        if ($lastReleased.Count -gt 0) { $notes = Get-NotesSince $lastReleased[0] }
        [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))
        if (-not (& git ls-remote --tags origin "refs/tags/$tag")) { Step "git push origin $tag" { git push origin $tag } }
        if (-not $hasRelease) { Step "gh release create $tag latest.zip" { & $gh release create $tag $zip --repo $repository --title "Tensho Assistant $version" --notes-file $notesFile --verify-tag } }
        Step "git push origin ${commit}:$releaseBranch" { git push origin "${commit}:refs/heads/$releaseBranch" }
        # A run stopped after « checkout main » left main checked out: Devloppement is the branch brought up to date.
        Step "git checkout $developBranch" { git checkout $developBranch }
        Step "git merge --ff-only $tag (on $developBranch)" { git merge --ff-only $commit }
        Step "git push origin $developBranch" { git push origin $developBranch }
        Write-Output ($(if ($DryRun) { "DRY RUN: nothing was published ($tag)." } else { "RELEASE $tag published (resumed): $releaseUrl" }))
        return
    }

    $newCommits = @(Invoke-Git @('log', '--format=%s', "origin/$releaseBranch..HEAD") | Where-Object { $_ })
    if ($newCommits.Count -eq 0) { Stop-Release "Nothing new on $developBranch since the last release (origin/$releaseBranch)." }

    # Release notes: every version since the last published release (highest Stable-x.y.z tag here or on GitHub).
    Invoke-Git @('fetch', 'origin', '--tags') | Out-Null
    $lastReleased = @(Invoke-Git @('tag', '--list', 'Stable-*') | ForEach-Object { $_ -replace '^Stable-', '' } | Where-Object { $_ -match '^\d+\.\d+\.\d+$' -and [version]$_ -lt [version]$version } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1)
    if ($lastReleased.Count -gt 0) { $notes = Get-NotesSince $lastReleased[0]; Write-Output "Release notes cover the versions after $($lastReleased[0])." }
    if ($DryRun) { Write-Output "--- release notes ---`n$notes`n---------------------" }
    # repo.json: Dalamud custom repository entry built from the packaged manifest, pointing at this release's zip.
    $output = 'src\TenshoAssistant\bin\x64\Release'
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $output 'TenshoAssistant.json') | ConvertFrom-Json
    $download = "https://github.com/$repository/releases/download/$tag/latest.zip"
    $entry = [ordered]@{
        Author = $manifest.Author; Name = $manifest.Name; InternalName = $manifest.InternalName
        AssemblyVersion = $manifest.AssemblyVersion; TestingAssemblyVersion = $manifest.AssemblyVersion
        Description = $manifest.Description; Punchline = $manifest.Punchline; ApplicableVersion = $manifest.ApplicableVersion
        RepoUrl = $manifest.RepoUrl; Tags = $manifest.Tags; DalamudApiLevel = $manifest.DalamudApiLevel
        IconUrl = "https://raw.githubusercontent.com/$repository/$releaseBranch/src/TenshoAssistant/images/icon.png"
        DownloadLinkInstall = $download; DownloadLinkUpdate = $download; DownloadLinkTesting = $download
        IsHide = $false; IsTestingExclusive = $false; AcceptsFeedback = $manifest.AcceptsFeedback
        LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds(); Changelog = $notes
    }
    $json = ConvertTo-Json @($entry) -Depth 4
    [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))

    # Local steps first: repo.json committed on Devloppement, merged into main, tagged. Nothing is public yet.
    Step "write repo.json ($version)" { [IO.File]::WriteAllText((Join-Path $root 'repo.json'), $json + "`n", [Text.UTF8Encoding]::new($false)) }
    Step "git commit repo.json" { git add repo.json; git commit -m "$version · repo.json" }
    # The release branch is GitHub's main: the local main is set to origin/main before merging (no local Stable branch needed).
    Step "git checkout -B $releaseBranch origin/$releaseBranch" { git checkout -B $releaseBranch "origin/$releaseBranch" }
    Step "git merge --no-ff $developBranch" { git merge --no-ff $developBranch -m "Release $version" }
    Step "git tag -a $tag" { git tag -a $tag -m "Tensho Assistant $version" }
    # Public steps: the tag and the GitHub release with the zip, then main (repo.json) last, then Devloppement.
    Step "git push origin $tag" { git push origin $tag }
    Step "gh release create $tag latest.zip" { & $gh release create $tag $zip --repo $repository --title "Tensho Assistant $version" --notes-file $notesFile --verify-tag }
    Step "git push origin $releaseBranch" { git push origin $releaseBranch }
    Step "git checkout $developBranch" { git checkout $developBranch }
    Step "git merge --ff-only $releaseBranch" { git merge --ff-only $releaseBranch }
    Step "git push origin $developBranch" { git push origin $developBranch }
    Write-Output ($(if ($DryRun) { "DRY RUN: nothing was merged, tagged or published ($tag)." } else { "RELEASE $tag published: https://github.com/$repository/releases/tag/$tag" }))
    Write-Output "Custom repository URL: https://raw.githubusercontent.com/$repository/$releaseBranch/repo.json"
}
finally { Pop-Location }
