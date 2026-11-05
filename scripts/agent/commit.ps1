# Verify.ps1 -Commit -DryRun, then -Commit, with the agent's trailer; a push refused for a moment is tried again. Never forced.
Set-Location (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
if (-not $env:TENSHO_COMMIT_TRAILER) { $env:TENSHO_COMMIT_TRAILER = 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>' }
function Short($text, $size) { "$text".Substring(0, [Math]::Min($size, "$text".Length)) }
.\scripts\Verify.ps1 -Commit -DryRun 2>&1 | Select-Object -Last 1 | ForEach-Object { Short $_ 100 }
try { .\scripts\Verify.ps1 -Commit 2>&1 | Select-Object -Last 1 | ForEach-Object { Short $_ 120 } }
catch { 'verify said: ' + $_.Exception.Message }
foreach ($attempt in 1..4) {
    if ((git status -sb | Select-Object -First 1) -notmatch 'ahead') { break }
    Start-Sleep -Seconds 15
    git push origin Devloppement 2>&1 | Select-Object -Last 1 | ForEach-Object { Short $_ 100 }
}
git status -sb | Select-Object -First 1
git log -1 --format='%h %s'
git status --short | Measure-Object | Select-Object -ExpandProperty Count
