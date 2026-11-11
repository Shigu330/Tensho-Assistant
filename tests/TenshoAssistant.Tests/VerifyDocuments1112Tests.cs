// 0.1112 (system plan 4.21): Verify.ps1 -Commit skipped every check when only tracked documents had changed (its stamp leaves out *.md,
// docs/ and repo.json), and 096963e left with 3 failing document tests. The stamp keeps the documents' own SHA-256: same sources but
// other documents run the managed tests again (no build) and update the stamp.
internal static class VerifyDocuments1112Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static int At(string text,string part)
    {
        var index=text.IndexOf(part,StringComparison.Ordinal);
        Check(index>=0,$"missing: {part}");
        return index;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Verify 1112: a change of tracked documents alone runs the managed tests again instead of skipping every check",() =>
        {
            var verify=Read("scripts/Verify.ps1");
            Check(verify.Contains("function Get-TaskFingerprint([switch]$Documents)",StringComparison.Ordinal),"one fingerprint function, two halves");
            Check(verify.Contains("-eq [bool]$Documents }",StringComparison.Ordinal),"the documents half is exactly what the sources half leaves out");
            Check(verify.Contains("$taskDocuments = Get-TaskFingerprint -Documents",StringComparison.Ordinal),"documents measured on every run");
            var documentsRun=At(verify,"if ($taskSkip -and $taskStamp.Documents -ne $taskDocuments) {");
            var tests=verify.IndexOf("& $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build",documentsRun,StringComparison.Ordinal);
            var plainSkip=At(verify,"elseif ($taskSkip) { Write-Output \"SKIP checks:");
            Check(documentsRun<tests && tests<plainSkip,"the tests run in the documents branch, before the plain skip");
            Check(verify.Contains("throw 'Managed tests failed (tracked documents changed since the last green run).'",StringComparison.Ordinal),"a failure stops -Commit");
            Check(At(verify,"throw 'Managed tests failed (tracked documents changed")<At(verify,"$taskStamp | ConvertTo-Json | Set-Content -LiteralPath $taskStampFile"),"the stamp is updated only after green tests");
            Check(verify.Contains("Fingerprint = $taskFingerprint; Documents = $taskDocuments;",StringComparison.Ordinal),"a full run stamps the documents too");
            Check(At(verify,"if ($taskSkip -and $taskStamp.Documents")<At(verify,"if ($Commit) { & (Join-Path $PSScriptRoot 'Release.ps1') -Mode Commit"),"before the commit");
            Check(Read(".githooks/pre-push").Contains("only tracked documents changed",StringComparison.Ordinal),"the pre-push hook says it");
        }),
    ];
}
