using System.Diagnostics;
using System.Text;

// 0.1505 (X-526): release 0.1502 failed while uploading to GitHub after its tag Stable-0.1502.0 was made. The release script took as the
// last release the highest Stable-* tag below the version, so the next release's player notes would have left out 0.1501 and 0.1502.
// The last release is now the highest tag that has a release published on GitHub (Get-LastPublishedRelease, scripts/LaunchProof.ps1,
// run here by Windows PowerShell 5.1 as Release.ps1 runs it); a tag without release is reported, never deleted.
internal static class ReleaseBase1505Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Quote(string text)=>"'"+text.Replace("'","''")+"'";

    // Every case in one Windows PowerShell; each answer « Ok|Version|Unreleased|Reason » comes back as UTF-8 in base64 on its own line.
    private static string[] PowerShell(IReadOnlyList<string> cases)
    {
        var script=Quote(Path.GetFullPath("scripts/LaunchProof.ps1"));
        var body=string.Join("; ",cases.Select(call=>$"$r = {call}; Write-Output ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(('{{0}}|{{1}}|{{2}}|{{3}}' -f $r.Ok, $r.Version, (@($r.Unreleased) -join ','), $r.Reason))))"));
        var start=new ProcessStartInfo("powershell.exe"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,StandardOutputEncoding=Encoding.UTF8};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-Command","[Console]::OutputEncoding = [Text.Encoding]::UTF8; . "+script+"; "+body})start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("powershell.exe could not be started");
        var output=process.StandardOutput.ReadToEnd();var error=process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode==0,"powershell: "+error);
        var answers=output.Replace("\r\n","\n").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(line=>Encoding.UTF8.GetString(Convert.FromBase64String(line))).ToArray();
        Check(answers.Length==cases.Count,$"one answer per case: {answers.Length} for {cases.Count}: {error}");
        return answers;
    }
    private static string Tags(params string[] tags)=>"@("+string.Join(", ",tags.Select(tag=>Quote("Stable-"+tag)))+")";

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Release base 1505: behaviour: a tag without release is not the last release; the notes start after the last published one (X-526)",() =>
        {
            // The case of 16/11: 0.1500 released, 0.1501 never tagged, 0.1502 tagged but its release failed; the next release is 0.1505.
            var list=Quote("[{\"isDraft\":false,\"tagName\":\"Stable-0.1500.0\"},{\"isDraft\":false,\"tagName\":\"Stable-0.1497.0\"},{\"isDraft\":true,\"tagName\":\"Stable-0.1503.0\"}]");
            var answers=PowerShell([
                $"Get-LastPublishedRelease {list} '0.1505.0' {Tags("0.1497.0","0.1500.0","0.1502.0","0.1503.0")}",
                $"Get-LastPublishedRelease {list} '0.1500.0' {Tags("0.1497.0","0.1500.0")}",
                $"Get-LastPublishedRelease {Quote("[]")} '0.2.0' {Tags("0.1.0")}",
                $"Get-LastPublishedRelease {Quote("")} '0.1505.0' {Tags("0.1500.0")}",
                $"Get-LastPublishedRelease {Quote("error: HTTP 502")} '0.1505.0' {Tags("0.1500.0")}",
                $"Get-LastPublishedRelease {Quote("[{\"isDraft\":false,\"tagName\":\"Stable-0.99.0\"},{\"isDraft\":false,\"tagName\":\"Stable-0.1499.0\"}]")} '0.1505.0' {Tags("0.99.0","0.1499.0")}"]);
            Check(answers[0].StartsWith("True|0.1500.0|0.1502.0,0.1503.0|",StringComparison.Ordinal),"0.1502 tagged without release, 0.1503 a draft: the base stays 0.1500, both reported: "+answers[0]);
            Check(answers[0].Contains("kept",StringComparison.Ordinal),"reported as kept (nothing deleted): "+answers[0]);
            Check(answers[1].StartsWith("True|0.1497.0||",StringComparison.Ordinal),"a resumed release of 0.1500: the release before it: "+answers[1]);
            Check(answers[2].StartsWith("True||0.1.0|no release published yet",StringComparison.Ordinal),"nothing published yet: no base: "+answers[2]);
            Check(answers[3].StartsWith("False|",StringComparison.Ordinal) && answers[4].StartsWith("False|",StringComparison.Ordinal),"gh failed: no guess from the tags: "+answers[3]+" / "+answers[4]);
            Check(answers[5].StartsWith("True|0.1499.0|",StringComparison.Ordinal),"numeric order (0.1499 above 0.99): "+answers[5]);
        }),
        ("Release base 1505: wiring: both paths of Release.ps1 take the last published release, no tag-only guess left (X-526)",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Split("$lastReleased = @(Get-LastReleased").Length==3,"the resumed and the new release both read Get-LastReleased");
            Check(!release.Contains("$lastReleased = @(Invoke-Git @('tag'",StringComparison.Ordinal),"no last release taken from the tags alone");
            Check(release.Contains("gh release list --repo $repository --limit 1000 --json 'tagName,isDraft'",StringComparison.Ordinal)
                  || release.Contains("& $gh release list --repo $repository --limit 1000 --json 'tagName,isDraft'",StringComparison.Ordinal),"the releases read from GitHub");
            Check(release.Contains("if (-not $found.Ok) { Stop-Release",StringComparison.Ordinal),"GitHub unreadable: the release stops");
            Check(!release.Contains("tag -d",StringComparison.Ordinal) && !release.Contains("'tag', '-d'",StringComparison.Ordinal),"nothing deleted");
        }),
    ];
}
