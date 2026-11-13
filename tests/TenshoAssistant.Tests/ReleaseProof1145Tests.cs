using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using TenshoAssistant.Diagnostics;

// 0.1145 (X-170, end of X-144, GPT audit of 12/11): the positive note of a release says only what the launch proof holds, and the packages
// a release uploads (new or resumed) come from the sources of the commit it publishes. Behaviour tests (R28): the release script's own
// functions (scripts/LaunchProof.ps1) run by Windows PowerShell 5.1 on a real proof, a real latest.zip and a real git repository.
internal static class ReleaseProof1145Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Quote(string text)=>"'"+text.Replace("'","''")+"'";

    private static string PowerShell(string command)
    {
        var script=Quote(Path.GetFullPath("scripts/LaunchProof.ps1"));
        var start=new ProcessStartInfo("powershell.exe"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,StandardOutputEncoding=Encoding.UTF8};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-Command","[Console]::OutputEncoding = [Text.Encoding]::UTF8; . "+script+"; "+command})start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("powershell.exe could not be started");
        var output=process.StandardOutput.ReadToEnd(); var error=process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode==0,"powershell: "+error);
        return output.Trim();
    }

    private static string Git(string folder,params string[] arguments)
    {
        var start=new ProcessStartInfo("git"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(var argument in new[]{"-C",folder,"-c","user.name=Tensho test","-c","user.email=test@example.invalid","-c","commit.gpgsign=false","-c","core.autocrlf=false"})start.ArgumentList.Add(argument);
        foreach(var argument in arguments)start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("git could not be started");
        var output=process.StandardOutput.ReadToEnd(); var error=process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode==0,"git "+string.Join(' ',arguments)+": "+error);
        return output.Trim();
    }

    private static string Commit(string folder,string file,string text)
    {
        var path=Path.Combine(folder,file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,text);
        Git(folder,"add","-A");
        Git(folder,"commit","-q","-m",file);
        return Git(folder,"rev-parse","HEAD");
    }

    private static void Stamp(string path,string version,string commit,bool clean)=>
        File.WriteAllText(path,$"{{\"Version\":\"{version}\",\"Fingerprint\":\"X\",\"Commit\":\"{commit}\",\"Clean\":{(clean?"true":"false")},\"VerifiedAt\":\"2026-11-12T08:00:00\"}}");

    // 0.1202: every case in one Windows PowerShell (one start of about half a second instead of ten); each answer comes back whole
    // (« Ok|Reason », its UTF-8 in base64 on its own line), in the order asked.
    private static string[] Packages(string root,params (string Version,string Commit,string Stamp)[] cases)
    {
        var command=string.Join("; ",cases.Select(row=>$"$r = Test-PackagesFromCommit {Quote(row.Version)} {Quote(row.Commit)} {Quote(row.Stamp)} {Quote(root)}; Write-Output ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(('{{0}}|{{1}}' -f $r.Ok, $r.Reason))))"));
        var answers=PowerShell(command).Replace("\r\n","\n").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(line=>Encoding.UTF8.GetString(Convert.FromBase64String(line))).ToArray();
        Check(answers.Length==cases.Length,$"one answer per case: {answers.Length} for {cases.Length}");
        return answers;
    }
    private static string Stamped(string folder,string name,string version,string commit,bool clean)
    {
        var path=Path.Combine(folder,name);
        Stamp(path,version,commit,clean);
        return path;
    }

    private static string Temporary()
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-1145-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void Remove(string folder)
    {
        foreach(var file in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories))File.SetAttributes(file,FileAttributes.Normal);
        Directory.Delete(folder,true);
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Release proof: the positive note says only what the launch proof holds, in both languages",() =>
        {
            var folder=Temporary();
            try
            {
                var dll=Path.Combine(folder,"TenshoAssistant.dll");
                File.WriteAllBytes(dll,[9,8,7,6,5,4,3,2,1]);
                var zip=Path.Combine(folder,"latest.zip");
                using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))archive.CreateEntryFromFile(dll,"TenshoAssistant.dll");
                var hash=LaunchProof.Hash(dll);
                var at=new DateTimeOffset(DateTime.UtcNow.Year,DateTime.UtcNow.Month,DateTime.UtcNow.Day,0,0,0,TimeSpan.Zero).AddMinutes(-90);
                var proof=new LaunchProof(LaunchProof.CurrentFormat,"0.1145.0",hash,at,true,"auto",36,4,1,2,0);
                var file=Path.Combine(folder,"LaunchProof.json");
                File.WriteAllText(file,proof.Serialize());
                var note=PowerShell($"$p = Get-LaunchProof '0.1145.0' {Quote(zip)} {Quote(file)}; if (-not $p.Ok) {{ throw $p.Reason }}; Format-LaunchNote $p");
                var lines=note.Replace("\r\n","\n").Split('\n');
                Check(lines.Length==2,"two lines, English then French: "+note);
                var when=at.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
                Check(lines[0].StartsWith("**Checked at launch, not tried in detail yet**: on "+when+" UTC, Tensho 0.1145.0 loaded",StringComparison.Ordinal),"English, dated from the proof: "+lines[0]);
                Check(lines[0].Contains("SHA-256 "+hash[..12]+"…") && lines[0].Contains("36 ok, 4 seen before, 1 not read, 2 suspect, 0 failed"),"the proof's DLL and counts: "+lines[0]);
                Check(lines[1].StartsWith("**Vérifié au lancement, pas encore essayé en détail** : le "+when+" UTC",StringComparison.Ordinal),"French second, accents read right (UTF-8 with BOM for Windows PowerShell 5.1): "+lines[1]);
                Check(lines[1].Contains("36 ok, 4 vus avant, 1 non lus, 2 suspects, 0 échec"),"French counts: "+lines[1]);
                foreach(var word in new[]{"the log","journal","error"})Check(!note.Contains(word,StringComparison.OrdinalIgnoreCase),"nothing the proof does not hold: "+word);
            }
            finally { Remove(folder); }
        }),
        ("Release proof: packages are accepted only when built clean from the sources of the commit released",() =>
        {
            var folder=Temporary();
            try
            {
                var repository=Path.Combine(folder,"repository");
                Directory.CreateDirectory(repository);
                Git(repository,"init","-q");
                var built=Commit(repository,"src/Plugin.cs","class A {}");
                var documents=Commit(repository,"repo.json","{}");
                Commit(repository,"docs/a.md","a");
                var tagged=Commit(repository,"CHANGELOG.md","b");
                var code=Commit(repository,"src/Other.cs","class B {}");
                // One stamp per case (each read as the release reads it), asked in one run.
                var stamp=Stamped(folder,"verify-stamp.json","0.1145.0",built,true);
                var damaged=Path.Combine(folder,"damaged-stamp.json");
                File.WriteAllText(damaged,"{ not json");
                var answers=Packages(repository,
                    ("0.1145.0",built,stamp),("0.1145.0",tagged,stamp),("0.1145.0",documents,stamp),("0.1145.0",code,stamp),
                    ("0.1145.0",built,Stamped(folder,"newer.json","0.1145.0",code,true)),
                    ("0.1145.0",built,Stamped(folder,"dirty.json","0.1145.0",built,false)),
                    ("0.1145.0",built,Stamped(folder,"older-version.json","0.1144.0",built,true)),
                    ("0.1145.0",built,Stamped(folder,"no-commit.json","0.1145.0","",true)),
                    ("0.1145.0",built,Stamped(folder,"unknown.json","0.1145.0",new string('a',40),true)),
                    ("0.1145.0",built,damaged),
                    ("0.1145.0",built,Path.Combine(folder,"missing.json")));
                Check(answers[0].StartsWith("True|built from "+built[..7],StringComparison.Ordinal),"same commit");
                Check(answers[1].StartsWith("True|",StringComparison.Ordinal),"only repo.json and documents differ (the release's own repo.json commit)");
                Check(answers[2].StartsWith("True|",StringComparison.Ordinal),"resumed tag on the repo.json commit");
                var refused=answers[3];
                Check(refused.StartsWith("False|the packages were built from "+built[..7]+", whose sources differ",StringComparison.Ordinal) && refused.Contains("src/Other.cs"),"another source: "+refused);
                Check(answers[4].StartsWith("False|",StringComparison.Ordinal),"a resumed tag older than the build is refused too");
                Check(answers[5].StartsWith("False|the packages were built from uncommitted changes",StringComparison.Ordinal),"dirty tree");
                Check(answers[6].StartsWith("False|the packages were built for 0.1144.0",StringComparison.Ordinal),"another version");
                Check(answers[7].StartsWith("False|the build stamp does not say which commit",StringComparison.Ordinal),"stamp before 0.1145");
                Check(answers[8].StartsWith("False|the commit built",StringComparison.Ordinal),"unknown commit");
                Check(answers[9].StartsWith("False|the build stamp cannot be read",StringComparison.Ordinal),"damaged stamp");
                Check(answers[10].StartsWith("False|no build stamp",StringComparison.Ordinal),"no stamp");
            }
            finally { Remove(folder); }
        }),
        ("Release proof: Verify stamps the commit built, the release checks it on both paths before anything public",() =>
        {
            var verify=Read("scripts/Verify.ps1");
            Check(verify.Contains("$taskHead = \"$(& git -C $taskRoot rev-parse HEAD 2>$null)\".Trim(); $taskClean = -not (& git -C $taskRoot status --porcelain 2>$null)"),"commit and clean tree read before the build");
            Check(verify.IndexOf("$taskClean = ",StringComparison.Ordinal)<verify.IndexOf("& $DotNet build TenshoAssistant.sln",StringComparison.Ordinal),"read before the build");
            Check(verify.Contains("Commit = $taskHead; Clean = [bool]$taskClean;"),"written in the stamp");
            var release=Read("scripts/Release.ps1");
            Check(!release.Contains("the log showed no error") && !release.Contains("journal n'a montré"),"the note no longer claims what the proof does not hold");
            var resumed=release.IndexOf("Assert-PackagesFromCommit $commit",StringComparison.Ordinal);
            Check(resumed>0 && resumed<release.IndexOf("Write-ReleaseProof $commit",StringComparison.Ordinal) && resumed<release.IndexOf("git push origin $tag\" { git push origin $tag } }",StringComparison.Ordinal),"resumed: checked before the proof and the tag push");
            var fresh=release.IndexOf("Assert-PackagesFromCommit ((Invoke-Git @('rev-parse', 'HEAD')).Trim())",StringComparison.Ordinal);
            Check(fresh>0 && fresh<release.IndexOf("Step \"git tag -a $tag\"",StringComparison.Ordinal),"new release: checked before the tag");
            Check(release.Contains("built = \"$($packages.Built)\""),"the release proof names the commit built");
            Check(File.ReadAllBytes(Path.GetFullPath("scripts/LaunchProof.ps1")).AsSpan(0,3).SequenceEqual(new byte[]{0xEF,0xBB,0xBF}),"UTF-8 with BOM (Windows PowerShell 5.1 reads its accents right)");
        }),
    ];
}
