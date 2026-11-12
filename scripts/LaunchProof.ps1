# 0.1126 (X-144, audit A13): the launch proof a release reads before its notes say « Checked at launch » (dot-sourced by Release.ps1).
# The plugin writes LaunchProof.json after a self-check pass with a character logged in (src/TenshoAssistant/Diagnostics/LaunchProof.cs):
# version, SHA-256 of the DLL the game loaded, time, counts. It is accepted only for the version released, with the very DLL that
# latest.zip carries, no failure, a character logged in and a date within the last 14 days. Windows PowerShell 5.1 compatible.

function Get-LaunchProofPath {
    if ($env:TENSHO_LAUNCH_PROOF) { return $env:TENSHO_LAUNCH_PROOF }
    return Join-Path $env:APPDATA 'XIVLauncher\pluginConfigs\TenshoAssistant\LaunchProof.json'
}

# SHA-256 (lower case) of TenshoAssistant.dll inside the package that is released.
function Get-PackagedDllHash([string]$Zip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        $entry = $archive.GetEntry('TenshoAssistant.dll')
        if (-not $entry) { return '' }
        $stream = $entry.Open()
        try { $sha = [Security.Cryptography.SHA256]::Create(); return (($sha.ComputeHash($stream) | ForEach-Object { $_.ToString('x2') }) -join '') }
        finally { $stream.Dispose() }
    }
    finally { $archive.Dispose() }
}

# Returns @{ Ok = $true/$false; Reason = '…'; Proof = <object or $null>; Dll = '<hash of the packaged DLL>' }.
function Get-LaunchProof([string]$Version, [string]$Zip, [string]$Path = (Get-LaunchProofPath), [DateTimeOffset]$Now = [DateTimeOffset]::UtcNow) {
    $result = @{ Ok = $false; Reason = ''; Proof = $null; Dll = ''; At = $null }
    if (-not (Test-Path -LiteralPath $Zip)) { $result.Reason = "package not found ($Zip)"; return $result }
    $result.Dll = Get-PackagedDllHash $Zip
    if (-not $result.Dll) { $result.Reason = 'the package has no TenshoAssistant.dll'; return $result }
    if (-not (Test-Path -LiteralPath $Path)) { $result.Reason = 'no launch proof (the plugin writes it after a self-check with a character logged in)'; return $result }
    try { $proof = [IO.File]::ReadAllText($Path) | ConvertFrom-Json } catch { $result.Reason = 'the launch proof cannot be read'; return $result }
    $result.Proof = $proof
    if ("$($proof.Format)" -ne '1') { $result.Reason = "unknown proof format '$($proof.Format)'"; return $result }
    if ("$($proof.Version)" -ne $Version) { $result.Reason = "the proof is for $($proof.Version), not $Version"; return $result }
    if ("$($proof.Dll)".ToLowerInvariant() -ne $result.Dll) { $result.Reason = 'the proof is for another build of the DLL than the one in latest.zip'; return $result }
    if ($proof.LoggedIn -ne $true) { $result.Reason = 'no character was logged in'; return $result }
    if ([int]$proof.Failed -ne 0) { $result.Reason = "the self-check had $($proof.Failed) failure(s)"; return $result }
    # PowerShell 7 turns an ISO date into a DateTime while reading the JSON; Windows PowerShell 5.1 keeps the text.
    try { $at = if ($proof.At -is [DateTime]) { [DateTimeOffset]$proof.At } else { [DateTimeOffset]::Parse("$($proof.At)", [Globalization.CultureInfo]::InvariantCulture) } }
    catch { $result.Reason = 'the proof has no valid date'; return $result }
    if ($at -gt $Now.AddMinutes(5)) { $result.Reason = 'the proof is dated in the future'; return $result }
    if ($at -lt $Now.AddDays(-14)) { $result.Reason = "the proof is older than 14 days ($($at.ToString('yyyy-MM-dd')))"; return $result }
    $result.Ok = $true
    $result.At = $at
    $result.Reason = "self-check of $($at.ToUniversalTime().ToString('yyyy-MM-dd HH:mm')) UTC: $($proof.Ok) ok, $($proof.Failed) failed, DLL $($result.Dll.Substring(0, 12))"
    return $result
}

# 0.1145 (X-170, end of X-144): the positive note of a release says only what the accepted proof holds (version, DLL, date, character
# logged in, the self-check's counts), nothing it does not (the proof says nothing of the log). English line, then French line.
function Format-LaunchNote($LaunchProof) {
    $proof = $LaunchProof.Proof
    $when = $LaunchProof.At.ToUniversalTime().ToString('yyyy-MM-dd HH:mm')
    $dll = $LaunchProof.Dll.Substring(0, 12)
    $counts = "$([int]$proof.Ok) ok, $([int]$proof.Seen) seen before, $([int]$proof.NotRead) not read, $([int]$proof.Suspect) suspect, $([int]$proof.Failed) failed"
    $countsFr = "$([int]$proof.Ok) ok, $([int]$proof.Seen) vus avant, $([int]$proof.NotRead) non lus, $([int]$proof.Suspect) suspects, $([int]$proof.Failed) échec"
    return "**Checked at launch, not tried in detail yet**: on $when UTC, Tensho $($proof.Version) loaded in game with the very DLL of this release (SHA-256 $dll…), a character logged in; its self-check: $counts. The detailed in-game tests come later.`n" +
        "**Vérifié au lancement, pas encore essayé en détail** : le $when UTC, Tensho $($proof.Version) s'est chargé en jeu avec la DLL même de cette release (SHA-256 $dll…), personnage connecté ; son autotest : $countsFr. Les essais détaillés en jeu viendront ensuite."
}

# 0.1145 (X-170, end of X-144): the packages a release uploads come from the commit it publishes, also when a release stopped halfway is
# resumed later. Verify.ps1 writes in its stamp the version built, the commit built (Commit) and whether the working tree was clean (Clean).
# Accepted only for this version, a clean tree, and a built commit whose sources (every file but *.md, docs/ and repo.json, as the stamp's
# fingerprint) are those of the commit released. Returns @{ Ok = $true/$false; Reason = '…'; Built = '<commit built>' }.
function Test-PackagesFromCommit([string]$Version, [string]$Commit, [string]$Stamp, [string]$Root) {
    $result = @{ Ok = $false; Reason = ''; Built = '' }
    if (-not (Test-Path -LiteralPath $Stamp)) { $result.Reason = 'no build stamp: run .\scripts\Verify.ps1 -Release again'; return $result }
    try { $data = [IO.File]::ReadAllText($Stamp) | ConvertFrom-Json -ErrorAction Stop } catch { $result.Reason = 'the build stamp cannot be read: run .\scripts\Verify.ps1 -Release again'; return $result }
    if ("$($data.Version)" -ne $Version) { $result.Reason = "the packages were built for $($data.Version), not ${Version}"; return $result }
    $built = "$($data.Commit)".Trim()
    if ($built -notmatch '^[0-9a-f]{40}$') { $result.Reason = 'the build stamp does not say which commit was built: run .\scripts\Verify.ps1 -Release again'; return $result }
    $result.Built = $built
    if ($data.Clean -ne $true) { $result.Reason = "the packages were built from uncommitted changes on $($built.Substring(0, 7))"; return $result }
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $files = @(& git -C $Root diff --name-only $built $Commit -- 2>$null | ForEach-Object { "$_" }); $code = $LASTEXITCODE }
    finally { $ErrorActionPreference = $previous; $global:LASTEXITCODE = 0 }
    if ($code -ne 0) { $result.Reason = "the commit built ($($built.Substring(0, 7))) or the commit released ($Commit) is unknown here"; return $result }
    $sources = @($files | Where-Object { $_ -and $_ -notmatch '(\.md$|^docs/|^repo\.json$)' })
    if ($sources.Count -gt 0) {
        $result.Reason = "the packages were built from $($built.Substring(0, 7)), whose sources differ from the commit released ($(($sources | Select-Object -First 3) -join ', '))"
        return $result
    }
    $result.Ok = $true
    $result.Reason = "built from $($built.Substring(0, 7)), same sources as the commit released"
    return $result
}
