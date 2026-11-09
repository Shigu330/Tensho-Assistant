using System.Text.RegularExpressions;

// 0.1017 (release guards, before the patch-day release): the release checks GitHub's CI of the commit first, moves main by
// fast-forward only without changing branch, never publishes the maintainer's agent files or a webhook address, and an urgent fix
// says so in its notes.
internal static class ReleaseGuards1017Tests
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
        ("Release: CI green, pushed, no agent file and no secret are checked before the tag",() =>
        {
            var release=Read("scripts/Release.ps1");
            var tag=At(release,"Step \"git tag -a $tag\"");
            Check(At(release,"Assert-CiGreen ((Invoke-Git @('rev-parse', 'HEAD')).Trim())")<tag,"CI checked before the tag");
            Check(At(release,"origin/$developBranch..HEAD")<tag,"unpushed commits checked before the tag");
            Check(At(release,"Assert-NoAgentFiles 'HEAD'; Assert-NoSecret")<tag,"agent files and secrets checked before the tag");
            Check(release.Contains("$requiredWorkflow = 'Build and test'",StringComparison.Ordinal),"the required workflow is the build");
            Check(Regex.IsMatch(release,@"function Fail-Ci[^\n]*if \(\$DryRun\)[^\n]*Stop-Release"),"a dry run only warns about the CI");
            Check(release.Contains("finally { $ErrorActionPreference = $previous; $global:LASTEXITCODE = 0 }",StringComparison.Ordinal),"a probe's exit code never becomes the script's (dry run of -Commit)");
        }),
        ("Release: main moves by fast-forward only, no branch change, no merge commit",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(!Regex.IsMatch(release,@"git (checkout|switch)\b"),"no branch change in the release script");
            Check(!release.Contains("--no-ff",StringComparison.Ordinal),"no merge commit on main");
            Check(!Regex.IsMatch(release,@"git push[^\n]*(--force|\s-f\b|\s\+)"),"no forced push");
            Check(release.Contains("git push origin \"HEAD:refs/heads/$releaseBranch\"",StringComparison.Ordinal),"main pushed as the tagged commit");
            Check(At(release,"git push origin \"HEAD:refs/heads/$releaseBranch\"")>At(release,"gh release create $tag $zip"),"main still last");
        }),
        ("Release: the maintainer's working files (.gitignore block 0.802) are still ignored and never committed",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Contains("$line.StartsWith('# 0.802')",StringComparison.Ordinal),"the list is read from the .gitignore block");
            Check(release.Contains("git check-ignore -q --no-index $probe",StringComparison.Ordinal),"each one still ignored");
            Check(At(release,"Assert-NoAgentFiles; Assert-NoSecret")<At(release,"Step \"git add -A\""),"checked before git add");
            var ignore=Read(".gitignore").Split('\n');
            var start=Array.FindIndex(ignore,line=>line.StartsWith("# 0.802",StringComparison.Ordinal));
            Check(start>=0,"the .gitignore block 0.802 exists");
            var block=ignore.Skip(start+1).TakeWhile(line=>line.Trim().Length>0).Where(line=>!line.StartsWith('#')).ToList();
            Check(block.Count>=5,$"the block keeps its five lines ({block.Count})");
        }),
        ("Release: -Hotfix needs a reason, goes with -Release only, and heads the notes in both languages",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Contains("[string]$Hotfix",StringComparison.Ordinal),"Release.ps1 takes -Hotfix");
            Check(release.Contains("if ($Hotfix -and $Mode -ne 'Release') { throw",StringComparison.Ordinal),"not with -Commit");
            Check(release.Contains("$Hotfix.Trim().Length -lt 8) { throw",StringComparison.Ordinal),"a reason is required");
            Check(release.Contains("**Urgent fix** ($reason)",StringComparison.Ordinal) && release.Contains("**Correctif urgent** ($reason)",StringComparison.Ordinal),"notes in English then French");
            Check(Regex.Matches(release,@"\$notes = Add-HotfixNote \$notes").Count==2,"new release and resumed release");
            var verify=Read("scripts/Verify.ps1");
            Check(verify.Contains("[string]$Hotfix",StringComparison.Ordinal) && verify.Contains("$taskReleaseArgs.Hotfix = $Hotfix",StringComparison.Ordinal),"Verify.ps1 passes -Hotfix on");
            Check(verify.Contains("-Hotfix goes with -Release only.",StringComparison.Ordinal),"Verify.ps1 refuses -Hotfix without -Release");
        }),
    ];
}
