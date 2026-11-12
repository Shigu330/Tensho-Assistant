using System.Diagnostics;
using System.IO.Compression;
using TenshoAssistant.Diagnostics;

// 0.1126 (X-144, audit A13): « Checked at launch » in a release only on a launch proof bound to the version and to the very DLL released.
// Behaviour test (R28): the plugin's proof written to a real file, a real latest.zip, and the release script's own check
// (scripts/LaunchProof.ps1) run by Windows PowerShell on them: accepted when everything matches, refused for another DLL, another version,
// a failure, no character, an old date or no file.
internal static class LaunchProof1126Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static string Run(string zip,string proof,string version)
    {
        var script=Path.GetFullPath("scripts/LaunchProof.ps1").Replace("'","''");
        var command=$". '{script}'; $r = Get-LaunchProof '{version}' '{zip.Replace("'","''")}' '{proof.Replace("'","''")}'; Write-Output ('{{0}}|{{1}}' -f $r.Ok, $r.Reason)";
        var start=new ProcessStartInfo("powershell.exe"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-Command",command})start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("powershell.exe could not be started");
        var output=process.StandardOutput.ReadToEnd(); var error=process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode==0,"powershell: "+error);
        return output.Trim();
    }

    private static void Write(string path,LaunchProof proof)=>File.WriteAllText(path,proof.Serialize());

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Launch proof: the plugin's proof is accepted only for the released version and DLL",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-1126-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var dll=Path.Combine(folder,"TenshoAssistant.dll");
                File.WriteAllBytes(dll,[1,2,3,4,5,6,7,8]);
                var zip=Path.Combine(folder,"latest.zip");
                using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))archive.CreateEntryFromFile(dll,"TenshoAssistant.dll");
                var hash=LaunchProof.Hash(dll);
                Check(hash.Length==64 && hash==hash.ToLowerInvariant(),"lower-case SHA-256");
                Check(LaunchProof.Hash(Path.Combine(folder,"none.dll"))=="","a missing DLL gives no hash");
                var lines=new[]{new SelfCheckLine("Items",SelfCheckStatus.Ok,"1 rows"),new SelfCheckLine("Maps",SelfCheckStatus.NotRead,"loading")};
                var good=LaunchProof.Make("0.1126.0",hash,DateTimeOffset.UtcNow,true,"auto",lines);
                Check(good.Ok==1 && good.NotRead==1 && good.Failed==0,"counts");
                var proof=Path.Combine(folder,"LaunchProof.json");
                Write(proof,good);
                var text=File.ReadAllText(proof);
                Check(!text.Contains(folder,StringComparison.OrdinalIgnoreCase) && !text.Contains("Users",StringComparison.Ordinal),"no path in the proof");
                Check(Run(zip,proof,"0.1126.0").StartsWith("True|self-check of ",StringComparison.Ordinal),"accepted: "+Run(zip,proof,"0.1126.0"));
                Check(Run(zip,proof,"0.1127.0").StartsWith("False|the proof is for 0.1126.0",StringComparison.Ordinal),"another version");
                Write(proof,good with { Dll=LaunchProof.Hash(zip) });
                Check(Run(zip,proof,"0.1126.0").StartsWith("False|the proof is for another build",StringComparison.Ordinal),"another DLL");
                Write(proof,good with { Failed=1 });
                Check(Run(zip,proof,"0.1126.0").StartsWith("False|the self-check had 1 failure",StringComparison.Ordinal),"a failure");
                Write(proof,good with { LoggedIn=false });
                Check(Run(zip,proof,"0.1126.0").StartsWith("False|no character",StringComparison.Ordinal),"no character");
                Write(proof,good with { At=DateTimeOffset.UtcNow.AddDays(-20) });
                Check(Run(zip,proof,"0.1126.0").StartsWith("False|the proof is older than 14 days",StringComparison.Ordinal),"old");
                File.Delete(proof);
                Check(Run(zip,proof,"0.1126.0").StartsWith("False|no launch proof",StringComparison.Ordinal),"no file");
            }
            finally { Directory.Delete(folder,true); }
        }),
        ("Launch proof: the release note depends on it, the plugin writes it, the release carries its own proof",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Contains(". (Join-Path $PSScriptRoot 'LaunchProof.ps1')"),"the check is the shared script");
            var function=release.IndexOf("function Add-HotfixNote",StringComparison.Ordinal);
            var guard=release.IndexOf("if ($launchProof -and $launchProof.Ok) {",function,StringComparison.Ordinal);
            Check(guard>function && guard<release.IndexOf("**Checked at launch, not tried in detail yet**",function,StringComparison.Ordinal),"« Checked at launch » only behind the proof");
            Check(release.Contains("**Not checked in game yet**") && release.Contains("**Pas encore vérifié en jeu**"),"otherwise said, in both languages");
            Check(release.IndexOf("$launchProof = Get-LaunchProof $version $zip",StringComparison.Ordinal)<release.IndexOf("$notes = Add-HotfixNote $notes",StringComparison.Ordinal),"read before the notes");
            Check(release.Split("$releaseProof --repo $repository").Length==4,"release-proof.json attached (new, resumed, missing asset)");
            var runner=Read("src/TenshoAssistant/Diagnostics/SelfCheckRunner.Dalamud.cs");
            Check(runner.Contains("if (loggedIn) WriteProof(now, lines);") && runner.Contains("dllHash = Task.Run(() => LaunchProof.Hash(location));") && runner.Contains("AtomicFile.Write(file, text)"),"plugin side");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("Diagnostics.LaunchProof.FileName"),"wired");
        }),
    ];
}
