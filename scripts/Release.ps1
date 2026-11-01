param(
    [Parameter(Mandatory)][ValidateSet('Commit', 'Release')][string]$Mode,
    [switch]$DryRun
)
# Git steps after a green Verify.ps1 (called by Verify.ps1 -Commit / -Release; see CONTRIBUTING.md 'Versions et releases').
#   Commit  : on Devloppement, commits every change as 'x.y.z' with the CHANGELOG entry as body, then pushes Devloppement.
#   Release : from a clean Devloppement, updates repo.json (Dalamud custom repository), merges into main and pushes it,
#             tags Stable-x.y.z, creates the GitHub release with latest.zip (GitHub CLI 'gh'), then brings Devloppement up to date.
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
    $notes = ($lines[($start + 1)..($end - 1)] -join "`n").Trim()

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
            [IO.File]::WriteAllText($message, "$subject`n`n$notes`n", [Text.UTF8Encoding]::new($false))
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

    # Release: stop early when this version is out or nothing is new since the last release.
    if ($released) { Stop-Release "Version $version is already released: $releaseUrl. Raise <Version> (and the CHANGELOG entry) for a new release." }
    Invoke-Git @('fetch', 'origin', $releaseBranch) | Out-Null
    $newCommits = @(Invoke-Git @('log', '--format=%s', "origin/$releaseBranch..HEAD") | Where-Object { $_ })
    if ($newCommits.Count -eq 0) { Stop-Release "Nothing new on $developBranch since the last release (origin/$releaseBranch)." }
    # gh from PATH, else its usual install folders (a terminal opened before installing GitHub CLI does not see it yet).
    $command = Get-Command gh -ErrorAction SilentlyContinue; $gh = if ($command) { $command.Source } else { $null }
    foreach ($candidate in @("$env:ProgramFiles\GitHub CLI\gh.exe", "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe")) { if (-not $gh -and (Test-Path -LiteralPath $candidate)) { $gh = $candidate } }
    if (-not $gh) {
        throw "GitHub CLI 'gh' not found: install it (winget install GitHub.cli), then run 'gh auth login' once."
    }
    if (Invoke-Git @('status', '--porcelain')) { Stop-Release "Uncommitted changes: run '.\scripts\Verify.ps1 -Commit' first, then -Release." }
    if (Invoke-Git @('tag', '--list', $tag)) { Stop-Release "Tag $tag already exists: raise <Version> for a new release." }
    $remoteTag = & git ls-remote --tags origin "refs/tags/$tag"
    if ($remoteTag) { Stop-Release "Tag $tag already exists on GitHub." }

    # repo.json: Dalamud custom repository entry built from the packaged manifest, pointing at this release's zip.
    $output = 'src\TenshoAssistant\bin\x64\Release'
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $output 'TenshoAssistant.json') | ConvertFrom-Json
    $zip = Join-Path $root (Join-Path $output 'TenshoAssistant\latest.zip')
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
    $notesFile = Join-Path ([IO.Path]::GetTempPath()) "tensho-notes-$version.md"
    [IO.File]::WriteAllText($notesFile, $notes, [Text.UTF8Encoding]::new($false))

    Step "write repo.json ($version)" { [IO.File]::WriteAllText((Join-Path $root 'repo.json'), $json + "`n", [Text.UTF8Encoding]::new($false)) }
    Step "git commit repo.json" { git add repo.json; git commit -m "$version · repo.json" }
    Step "git push origin $developBranch" { git push origin $developBranch }
    # The release branch is GitHub's main: the local main is set to origin/main before merging (no local Stable branch needed).
    Step "git fetch origin" { git fetch origin }
    Step "git checkout -B $releaseBranch origin/$releaseBranch" { git checkout -B $releaseBranch "origin/$releaseBranch" }
    Step "git merge --no-ff $developBranch" { git merge --no-ff $developBranch -m "Release $version" }
    Step "git push origin $releaseBranch" { git push origin $releaseBranch }
    Step "git tag -a $tag" { git tag -a $tag -m "Tensho Assistant $version" }
    Step "git push origin $tag" { git push origin $tag }
    Step "gh release create $tag latest.zip" { & $gh release create $tag $zip --repo $repository --title "Tensho Assistant $version" --notes-file $notesFile --target $releaseBranch }
    Step "git checkout $developBranch" { git checkout $developBranch }
    Step "git merge --ff-only $releaseBranch" { git merge --ff-only $releaseBranch }
    Step "git push origin $developBranch" { git push origin $developBranch }
    Write-Output ($(if ($DryRun) { "DRY RUN: nothing was merged, tagged or published ($tag)." } else { "RELEASE $tag published: https://github.com/$repository/releases/tag/$tag" }))
    Write-Output "Custom repository URL: https://raw.githubusercontent.com/$repository/$releaseBranch/repo.json"
}
finally { Pop-Location }
