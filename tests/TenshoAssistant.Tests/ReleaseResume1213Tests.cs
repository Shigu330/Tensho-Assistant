using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

// 0.1213 (X-185 part 3, night review of 12/11, N18, N19). N18: the release proof was about a stamp (commit, clean tree), never about the bytes of
// the zip uploaded: Verify now writes each package's SHA-256 in its stamp (HEAD and the tree read again after the build), the release copies the
// packages once into its proof folder, checks the copies against the stamp and uploads only them; a renamed source counts as a source change.
// N19: a resumed release checked only the desktop files before pushing main (repo.json): a draft, or a release without latest.zip, now stops it or
// gets latest.zip uploaded first; a package attached with another size stops it. Behaviour tests (R28): the release script's own functions
// (scripts/LaunchProof.ps1) run by Windows PowerShell 5.1 on real files and a real git repository.
internal static class ReleaseResume1213Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Quote(string text)=>"'"+text.Replace("'","''")+"'";
    private static string Sha(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    // Every case in one Windows PowerShell; each answer « Ok|Reason|Upload|Attached » comes back as UTF-8 in base64 on its own line.
    private static string[] PowerShell(IReadOnlyList<string> cases)
    {
        var script=Quote(Path.GetFullPath("scripts/LaunchProof.ps1"));
        var body=string.Join("; ",cases.Select(call=>$"$r = {call}; Write-Output ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(('{{0}}|{{1}}|{{2}}|{{3}}' -f $r.Ok, $r.Reason, (@($r.Upload) -join ','), (@($r.Attached) -join ',')))))"));
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
    private static string Git(string folder,params string[] arguments)
    {
        var start=new ProcessStartInfo("git"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(var argument in new[]{"-C",folder,"-c","user.name=Tensho test","-c","user.email=test@example.invalid","-c","commit.gpgsign=false","-c","core.autocrlf=false"})start.ArgumentList.Add(argument);
        foreach(var argument in arguments)start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("git could not be started");
        var output=process.StandardOutput.ReadToEnd();var error=process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode==0,"git "+string.Join(' ',arguments)+": "+error);
        return output.Trim();
    }
    private static void InTemp(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-1213-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(folder);}
        finally
        {
            try{foreach(var file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))File.SetAttributes(file,FileAttributes.Normal);Directory.Delete(folder,true);}
            catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Release resume 1213: behaviour: the files uploaded must be the packages Verify built (SHA-256 in the stamp) (N18)",() => InTemp(root =>
        {
            var repository=Path.Combine(root,"repo");Directory.CreateDirectory(repository);
            Git(repository,"init","-q");File.WriteAllText(Path.Combine(repository,"a.cs"),"class A{}");Git(repository,"add","-A");Git(repository,"commit","-q","-m","a");
            var commit=Git(repository,"rev-parse","HEAD");
            var zip=Path.Combine(root,"latest.zip");var desktop=Path.Combine(root,"TenshoAssistant.Desktop.zip");
            File.WriteAllBytes(zip,[1,2,3,4,5]);File.WriteAllBytes(desktop,[9,8,7]);
            string StampOf(string name,bool packages)
            {
                var path=Path.Combine(root,name);
                var hashes=packages ? $",\"Packages\":{{\"latest.zip\":\"{Sha(zip)}\",\"TenshoAssistant.Desktop.zip\":\"{Sha(desktop)}\"}}":"";
                File.WriteAllText(path,$"{{\"Version\":\"1.2.3\",\"Fingerprint\":\"X\",\"Commit\":\"{commit}\",\"Clean\":true{hashes},\"VerifiedAt\":\"2026-11-13T04:00:00\"}}");
                return path;
            }
            var withHashes=StampOf("with.json",true);var without=StampOf("without.json",false);
            var changed=Path.Combine(root,"changed");Directory.CreateDirectory(changed);
            var changedZip=Path.Combine(changed,"latest.zip");File.WriteAllBytes(changedZip,[1,2,3,4,6]);
            string Call(string stamp,params string[] packages)=>$"Test-PackagesFromCommit '1.2.3' {Quote(commit)} {Quote(stamp)} {Quote(repository)}"+(packages.Length>0 ? " @("+string.Join(", ",packages.Select(Quote))+")":"");
            var answers=PowerShell([Call(withHashes,zip,desktop),Call(withHashes,changedZip,desktop),Call(without,zip,desktop),Call(without)]);
            Check(answers[0].StartsWith("True|",StringComparison.Ordinal) && answers[0].Contains("the files uploaded are the packages built",StringComparison.Ordinal),"the packages built: accepted: "+answers[0]);
            Check(answers[1].StartsWith("False|latest.zip differs from the package Verify built",StringComparison.Ordinal),"one byte changed in the zip uploaded: refused: "+answers[1]);
            Check(answers[2].StartsWith("False|the build stamp has no SHA-256 of latest.zip",StringComparison.Ordinal),"a stamp without the packages' SHA-256: refused: "+answers[2]);
            Check(answers[3].StartsWith("True|",StringComparison.Ordinal),"without packages to check: as before: "+answers[3]);
        })),
        ("Release resume 1213: behaviour: a draft, a release without latest.zip or a package of another size is seen before main is pushed (N19)",() => InTemp(root =>
        {
            var zip=Path.Combine(root,"latest.zip");var desktop=Path.Combine(root,"TenshoAssistant.Desktop.zip");var hash=desktop+".sha256";
            File.WriteAllBytes(zip,new byte[100]);File.WriteAllBytes(desktop,new byte[200]);File.WriteAllText(hash,"abc");
            var files=$"@{{ 'latest.zip' = {Quote(zip)}; 'TenshoAssistant.Desktop.zip' = {Quote(desktop)}; 'TenshoAssistant.Desktop.zip.sha256' = {Quote(hash)} }}";
            string Asset(string name,long size)=>$"{{\"name\":\"{name}\",\"size\":{size}}}";
            string View(bool draft,params string[] assets)=>$"{{\"isDraft\":{(draft ? "true":"false")},\"assets\":[{string.Join(",",assets)}]}}";
            var all=new[]{Asset("latest.zip",100),Asset("TenshoAssistant.Desktop.zip",200),Asset("TenshoAssistant.Desktop.zip.sha256",3),Asset("release-proof.json",900)};
            var views=0;
            // Each answer of gh written to a file and read back raw, as the release script joins gh's lines (no quoting on the command line).
            string Call(string view){var path=Path.Combine(root,$"view{views++}.json");File.WriteAllText(path,view);return $"Test-ResumeAssets ([IO.File]::ReadAllText({Quote(path)})) {files}";}
            var answers=PowerShell([Call(View(true,all)),Call(View(false,all[1],all[2])),Call(View(false,Asset("latest.zip",99),all[1],all[2])),Call(View(false,all)),Call("not json"),Call(View(false))]);
            Check(answers[0].StartsWith("False|the GitHub release is a draft",StringComparison.Ordinal),"a draft: stopped: "+answers[0]);
            Check(answers[1].StartsWith("True|missing on the release: latest.zip|latest.zip|",StringComparison.Ordinal),"latest.zip missing: uploaded before main: "+answers[1]);
            Check(answers[2].StartsWith("False|latest.zip attached to the release has 99 bytes",StringComparison.Ordinal),"latest.zip of another size: stopped: "+answers[2]);
            Check(answers[3]=="True|every package attached, same sizes||latest.zip,release-proof.json,TenshoAssistant.Desktop.zip,TenshoAssistant.Desktop.zip.sha256","all there: nothing to upload: "+answers[3]);
            Check(answers[4].StartsWith("False|the GitHub release cannot be read",StringComparison.Ordinal),"an answer that cannot be read: stopped: "+answers[4]);
            Check(answers[5].StartsWith("True|missing on the release: latest.zip, TenshoAssistant.Desktop.zip, TenshoAssistant.Desktop.zip.sha256|",StringComparison.Ordinal),"nothing attached: all uploaded: "+answers[5]);
        })),
        ("Release resume 1213: the release and Verify are wired: checks before main, copies uploaded, SHA-256 in the stamp",() =>
        {
            var release=Read("scripts/Release.ps1");var verify=Read("scripts/Verify.ps1");var proof=Read("scripts/LaunchProof.ps1");
            var check=release.IndexOf("$onGitHub = Test-ResumeAssets ($view -join \"`n\") $packagesOf",StringComparison.Ordinal);
            var stop=release.IndexOf("if (-not $onGitHub.Ok) { Stop-Release \"Stopped before $releaseBranch is pushed: $($onGitHub.Reason).\" }",StringComparison.Ordinal);
            var main=release.IndexOf("Step \"git push origin ${commit}:$releaseBranch (fast-forward)\"",StringComparison.Ordinal);
            Check(check>0 && stop>check && main>stop && release.Contains("--json 'isDraft,assets'"),"N19: the release on GitHub checked, and stopped on a problem, before main is pushed");
            Check(release.Contains("$script:zip = Copy-Package $zip; $script:desktopZip = Copy-Package $desktopZip")
                && release.Contains("$root @($zip, $desktopZip)"),"N18: the packages copied into the proof folder, the copies checked and uploaded");
            Check(verify.Contains("Clean = [bool]($taskClean -and $taskCleanAfter -and $taskHeadAfter -eq $taskHead); Packages = $taskPackages;")
                && verify.Contains("$taskPackages[[IO.Path]::GetFileName($taskPackage)] = (Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256).Hash.ToLowerInvariant()"),"N18: the stamp holds the packages' SHA-256 and the tree read again after the build");
            Check(proof.Contains("diff --no-renames --name-only $built $Commit"),"N18: a renamed source is a source change");
        }),
    ];
}
