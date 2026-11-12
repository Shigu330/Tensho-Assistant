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
    $result = @{ Ok = $false; Reason = ''; Proof = $null; Dll = '' }
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
    $result.Reason = "self-check of $($at.ToUniversalTime().ToString('yyyy-MM-dd HH:mm')) UTC: $($proof.Ok) ok, $($proof.Failed) failed, DLL $($result.Dll.Substring(0, 12))"
    return $result
}
