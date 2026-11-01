param(
    [string]$DotNet,
    # After a green run (0.219): -Commit commits and pushes Devloppement; -Release publishes Stable, the tag, the GitHub release and repo.json.
    [switch]$Commit,
    [switch]$Release,
    [switch]$DryRun
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
}
finally { Pop-Location }
# Git steps only after everything above passed (scripts/Release.ps1).
if ($Commit) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Commit -DryRun:$DryRun }
if ($Release) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Release -DryRun:$DryRun }
