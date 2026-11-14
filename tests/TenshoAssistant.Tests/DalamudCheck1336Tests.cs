using System.Diagnostics;
using System.IO.Compression;

// 0.1336 (X-148, GPT audit of 11/11, A16): the CI checks Dalamud's latest.zip before building against it, without a pinned hash (goatcorp
// publishes none; decision of 11/13): its Dalamud.dll must carry the AssemblyVersion goatcorp publishes and the API level of global.json.
// Behaviour: scripts/Get-Dalamud.ps1 run on a zip of the installed Dalamud.dll, accepted then refused (other published version, other
// API level); wiring: the three workflow downloads go through it.
internal static class DalamudCheck1336Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static string InstalledDalamud()
    {
        var home=Environment.GetEnvironmentVariable("DALAMUD_HOME");
        var hooks=!string.IsNullOrEmpty(home) ? home:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"XIVLauncher","addon","Hooks","dev");
        var dll=Path.Combine(hooks,"Dalamud.dll");
        Check(File.Exists(dll),"Dalamud.dll installed (the plugin builds against it): "+dll);
        return dll;
    }

    private static (int Code,string Output) Run(string zip,string destination,string published,string root)
    {
        var start=new ProcessStartInfo("pwsh"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-File",Path.GetFullPath("scripts/Get-Dalamud.ps1"),"-Destination",destination,"-ZipPath",zip,"-PublishedVersion",published,"-Root",root})
            start.ArgumentList.Add(argument);
        using var process=Process.Start(start) ?? throw new Exception("pwsh could not be started");
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        Check(process.WaitForExit(60_000),"pwsh ends");
        return (process.ExitCode,output.Result+error.Result);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dalamud check 1336: the installed Dalamud.dll is accepted at its version, refused under another published version or API level",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-dalamud-1336-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var dll=InstalledDalamud();
                var version=System.Reflection.AssemblyName.GetAssemblyName(dll).Version!;
                var zip=Path.Combine(folder,"latest.zip");
                using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))archive.CreateEntryFromFile(dll,"Dalamud.dll");
                var root=Path.Combine(folder,"root");Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root,"global.json"),$"{{\"msbuild-sdks\": {{\"Dalamud.NET.Sdk\": \"{version.Major}.0.0\"}}}}");
                var ok=Run(zip,Path.Combine(folder,"ok"),version.ToString(),root);
                Check(ok.Code==0 && ok.Output.Contains($"Dalamud {version} (published {version}, API level {version.Major})") && ok.Output.Contains("SHA-256 "),"accepted, hash written: "+ok.Output);
                Check(File.Exists(Path.Combine(folder,"ok","Dalamud.dll")),"unpacked where asked");
                var stale=Run(zip,Path.Combine(folder,"stale"),$"{version.Major}.{version.Minor}.{version.Build+1}.0",root);
                Check(stale.Code!=0 && stale.Output.Contains("goatcorp publishes"),"another published version: refused: "+stale.Output);
                File.WriteAllText(Path.Combine(root,"global.json"),$"{{\"msbuild-sdks\": {{\"Dalamud.NET.Sdk\": \"{version.Major+1}.0.0\"}}}}");
                var api=Run(zip,Path.Combine(folder,"api"),version.ToString(),root);
                Check(api.Code!=0 && api.Output.Contains($"built for API level {version.Major+1}"),"another API level: refused: "+api.Output);
            }
            finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Dalamud check 1336: every workflow download goes through the check",() =>
        {
            var build=Read(".github/workflows/build.yml");var codeql=Read(".github/workflows/CodeQL.yml");
            Check(build.Split("./scripts/Get-Dalamud.ps1 -Destination").Length==3,"the two build jobs");
            Check(codeql.Contains("./scripts/Get-Dalamud.ps1 -Destination \"$HOME/.xlcore/dalamud/Hooks/dev\""),"CodeQL");
            foreach(var (name,text) in new[]{("build.yml",build),("CodeQL.yml",codeql)})
                Check(!text.Contains("dalamud-distrib/latest.zip",StringComparison.Ordinal),"no download left unchecked in "+name);
            var script=Read("scripts/Get-Dalamud.ps1");
            Check(script.Contains("$version.ToString() -ne $PublishedVersion") && script.Contains("$version.Major -ne $apiLevel") && script.Contains("Get-FileHash"),"the checks");
        }),
    ];
}
