using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1113 (X-126, D-137): the desktop application installed from Settings › Advanced › Desktop application, on a click only. The install is
// DesktopInstall.Install, shared with the app's own update (X-121); here a fake release installed in a temporary folder, a damaged zip refused,
// the plugin's release missing (the latest one taken), the rules and the wiring.
internal static class DesktopInstall1113Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    /// <summary>A release in memory: its JSON for one tag (null for another or for « latest » when told), its .sha256 and its zip.</summary>
    private sealed class FakeSource(string tag,byte[] zip,bool latestOnly=false,bool damaged=false) : IReleaseSource
    {
        public int Asked;
        public Task<string?> Release(UpdatePlan plan,CancellationToken cancel)
        {
            Asked++;
            var wanted=plan.Ask==UpdateAsk.Tag ? DesktopUpdate.Tag(plan.Version!) : null;
            if(latestOnly ? plan.Ask!=UpdateAsk.Latest : wanted!=tag)return Task.FromResult<string?>(null);
            var url=DesktopUpdate.DownloadPrefix+tag+"/";
            return Task.FromResult<string?>($$"""{"tag_name":"{{tag}}","draft":false,"prerelease":false,"assets":[{"name":"TenshoAssistant.Desktop.zip","browser_download_url":"{{url}}TenshoAssistant.Desktop.zip","size":{{zip.Length}}},{"name":"TenshoAssistant.Desktop.zip.sha256","browser_download_url":"{{url}}TenshoAssistant.Desktop.zip.sha256","size":94}]}""");
        }
        public Task<string> Text(ReleaseAsset asset,CancellationToken cancel)=>Task.FromResult(DesktopUpdate.HashLine(Convert.ToHexString(SHA256.HashData(zip)),DesktopUpdate.ZipName));
        public async Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel)
        {
            var bytes=(byte[])zip.Clone();
            if(damaged)bytes[bytes.Length/2]^=0x5A;
            await target.WriteAsync(bytes,cancel);
            progress(bytes.Length);
        }
    }

    /// <summary>A package as Verify.ps1 makes it: the exe, the app's DLL and plugin\TenshoAssistant.dll, both of one version (this test assembly).</summary>
    private static (byte[] Zip,Version Version) Package(string temp)
    {
        var assembly=typeof(DesktopInstall1113Tests).Assembly.Location;
        var version=DesktopUpdate.Three(AssemblyName.GetAssemblyName(assembly).Version!);
        var folder=Path.Combine(temp,"package");
        Directory.CreateDirectory(Path.Combine(folder,"plugin"));
        File.WriteAllText(Path.Combine(folder,DesktopUpdate.ExeName),"exe");
        File.Copy(assembly,Path.Combine(folder,"TenshoAssistant.Desktop.dll"));
        File.Copy(assembly,Path.Combine(folder,"plugin","TenshoAssistant.dll"));
        var zip=Path.Combine(temp,"package.zip");
        // 0.1202: stored, not compressed (the same real zip and files, made in a fraction of the time).
        ZipFile.CreateFromDirectory(folder,zip,CompressionLevel.NoCompression,includeBaseDirectory:false);
        return (File.ReadAllBytes(zip),version);
    }

    private static DesktopInstaller Run(string own,Version plugin,IReleaseSource source,string? folder=null)
    {
        var installer=new DesktopInstaller(own,plugin,source,_=>{});
        Check(installer.Install(folder),"the install starts");
        installer.Work!.Wait(TimeSpan.FromSeconds(60));
        return installer;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop install 1113: rules (state against the plugin, folder, previous copy, releases asked, release page, switch on)",() =>
        {
            var plugin=new Version(0,1113,0,0);
            Check(DesktopInstallRules.View(new UpdateState(),plugin,_=>true).Kind==DesktopInstallKind.NotInstalled,"nothing recorded");
            var state=new UpdateState(Current:new InstalledCopy("0.1113.0",@"C:\A\0.1113.0"));
            Check(DesktopInstallRules.View(state,plugin,_=>true) is { Kind: DesktopInstallKind.Same, Version: "0.1113.0" },"same version (major.minor.build)");
            Check(DesktopInstallRules.View(state,new Version(0,1114,0),_=>true).Kind==DesktopInstallKind.Older,"older than the plugin");
            Check(DesktopInstallRules.View(state,new Version(0,1112,0),_=>true).Kind==DesktopInstallKind.Newer,"newer than the plugin");
            Check(DesktopInstallRules.View(state,plugin,_=>false).Kind==DesktopInstallKind.Missing,"recorded but its exe is gone");
            Check(DesktopInstallRules.Installed(DesktopInstallRules.View(state,plugin,_=>false))==new Version(0,0,0) && DesktopInstallRules.Installed(DesktopInstallRules.View(state,plugin,_=>true))==new Version(0,1113,0),"what an install compares with");
            Check(DesktopInstallRules.Folder(@"C:\own",null)==DesktopInstall.VersionsFolder(@"C:\own") && DesktopInstallRules.Folder(@"C:\own"," D:\\Tensho ")==@"D:\Tensho","default folder, or the chosen one");
            Check(DesktopInstallRules.FolderProblem(null) is null && DesktopInstallRules.FolderProblem(@"D:\Tensho") is null && DesktopInstallRules.FolderProblem(@"Tensho\apps")=="Advanced.DesktopInstallFolderBad","a full path only");
            var after=DesktopInstallRules.AfterInstall(state with {Skipped="0.1114.0"},new InstalledCopy("0.1114.0",@"C:\A\0.1114.0"));
            Check(after.Current!.Version=="0.1114.0" && after.Previous!.Version=="0.1113.0" && after.Skipped is null && after.AutoUpdate,"new Current, old one Previous, a version set aside forgotten when installed on purpose");
            var plans=DesktopInstallRules.Plans(plugin);
            Check(plans.Length==2 && plans[0].Ask==UpdateAsk.Tag && plans[0].Version==new Version(0,1113,0) && plans[1].Ask==UpdateAsk.Latest,"the plugin's own release first, then the latest");
            Check(DesktopInstallRules.ReleasePage(plugin)=="https://github.com/Shigu330/Tensho-Assistant/releases/tag/Stable-0.1113.0","release page");
            Check(DesktopInstallRules.Enabled,"on in this build (custom repository, D-137)");
            Check(DesktopPaths.Own(@"C:\L")==DesktopInstall.OwnFolder(@"C:\L") && DesktopInstall.StatePath(@"C:\o")==Path.Combine(@"C:\o","update.json"),"one source for the app's folders");
        }),
        ("Desktop install 1113: a fake release installed beside, recorded as Current, then up to date; a damaged zip refused; a missing release falls back to the latest",() =>
        {
            var temp=Path.Combine(Path.GetTempPath(),"tensho-install-1113-"+Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(temp);
                var (zip,version)=Package(temp);
                var tag=DesktopUpdate.Tag(version);var three=DesktopVersionCheck.Three(version);
                var own=Path.Combine(temp,"own");
                var installer=Run(own,version,new FakeSource(tag,zip));
                Check(installer.Status is { Key: "Advanced.DesktopInstallDone", Warning: false },"installed: "+installer.Status);
                var current=DesktopInstall.Load(own).Current;
                Check(current is not null && current.Version==three && File.Exists(Path.Combine(current.Folder,DesktopUpdate.ExeName)) && DesktopUpdate.SameFolder(current.Folder,Path.Combine(DesktopInstall.VersionsFolder(own),three)),"unpacked in versions\\<version> and recorded");
                Check(installer.View().Kind==DesktopInstallKind.Same,"the section says up to date");
                Check(!Directory.EnumerateDirectories(DesktopInstall.VersionsFolder(own),"*.partial").Any() && !File.Exists(Path.Combine(own,"downloads",DesktopUpdate.ZipName+".partial")),"no partial file left");
                installer.Dispose();
                var again=Run(own,version,new FakeSource(tag,zip));
                Check(again.Status?.Key=="Advanced.DesktopInstallUpToDate","the same version is not installed again");
                again.Dispose();
                var chosen=Path.Combine(temp,"chosen");
                var elsewhere=Run(Path.Combine(temp,"own3"),version,new FakeSource(tag,zip),chosen);
                Check(DesktopInstall.Load(Path.Combine(temp,"own3")).Current?.Folder==Path.Combine(chosen,three),"the chosen folder");
                elsewhere.Dispose();
                var damaged=Run(Path.Combine(temp,"own2"),version,new FakeSource(tag,zip,damaged:true));
                Check(damaged.Status is { Key: "Advanced.DesktopInstallErrorHash", Warning: true } && DesktopInstall.Load(Path.Combine(temp,"own2")).Current is null,"a damaged zip refused, nothing recorded");
                damaged.Dispose();
                var newer=new Version(version.Major,version.Minor+1,0);
                var source=new FakeSource(tag,zip,latestOnly:true);
                var fallback=Run(Path.Combine(temp,"own4"),newer,source);
                Check(source.Asked==2 && fallback.Status is { Key: "Advanced.DesktopInstallDoneOther" } && fallback.Status.Value.Args[1]==DesktopVersionCheck.Three(newer),"the plugin's release is not out yet: the latest one, said");
                fallback.Dispose();
                var bad=new DesktopInstaller(Path.Combine(temp,"own5"),version,new FakeSource(tag,zip),_=>{});
                Check(!bad.Install("relative") && bad.Status?.Key=="Advanced.DesktopInstallFolderBad" && bad.Work is null,"a bad folder: no request");
            }
            finally{try{Directory.Delete(temp,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Desktop install 1113: wiring (Settings › Advanced, click only, never started by itself, switch for an official build, FAQ, SECURITY, reset)",() =>
        {
            var window=Read("src/TenshoAssistant/MainWindow.DesktopInstall.cs");
            Check(Read("src/TenshoAssistant/MainWindow.Advanced.cs").Contains("DrawDesktopInstall();"),"in Settings › Advanced, Desktop application");
            var installs=window.Split('\n').Where(line=>line.Contains("installer.Install(",StringComparison.Ordinal)).ToArray();
            Check(installs.Length==1 && installs[0].Contains("if(ImGui.Button(",StringComparison.Ordinal),"one install call, under its button");
            Check(window.Split("installer.Open()").Length==2 && window.Contains("if(ImGui.Button(localization.Get(\"Advanced.DesktopInstallOpen\")))desktopOpenProblem=installer.Open();"),"started on « Open the desktop app » only");
            var installer=Read("src/TenshoAssistant/Link/DesktopInstaller.cs");
            Check(installer.Split("Process.Start(").Length==2 && installer.IndexOf("Process.Start(",StringComparison.Ordinal)>installer.IndexOf("public string? Open()",StringComparison.Ordinal),"Process.Start only in Open()");
            foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs"))
                Check(!File.ReadAllText(file).Contains("Process.Start(",StringComparison.Ordinal),"no start from the window: "+Path.GetFileName(file));
            var plugin=Read("src/TenshoAssistant/Plugin.Desktop.cs");
            Check(plugin.Contains("if(Link.DesktopInstallRules.Enabled)") && plugin.Contains("mainWindow.DesktopInstaller?.Dispose();mainWindow.DesktopInstaller=null;"),"created only when the build has it, disposed at unload");
            var project=Read("src/TenshoAssistant/TenshoAssistant.csproj");
            Check(project.Contains("TENSHO_NO_DESKTOP_INSTALL") && project.Contains("..\\TenshoAssistant.Desktop\\DesktopInstall.cs"),"switch and shared install in the plugin");
            Check(Read("src/TenshoAssistant/Link/DesktopInstaller.cs").Contains("#if TENSHO_NO_DESKTOP_INSTALL"),"the switch turns Enabled off");
            var updater=Read("src/TenshoAssistant.Desktop/DesktopUpdater.cs");
            Check(updater.Contains("await DesktopInstall.Install(own,VersionsFolder(own)") && !updater.Contains("class GitHubReleases") && !updater.Contains("ExtractToDirectory"),"the app's update uses the same install");
            Check(TenshoAssistant.Help.Faq.PageOf("DesktopInstall")=="Settings","FAQ entry");
            var docgen=Read("tools/docgen.py");
            Check(docgen.Contains("(\"Desktop app install\", [\"api.github.com\", \"github.com\"], None,") && docgen.Contains("\"Link/DesktopInstaller.cs\""),"network source declared (SECURITY)");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"DesktopLink\", \"DesktopInstallFolder\", ") && new TenshoAssistant.Configuration().DesktopInstallFolder=="","folder setting, empty by default, reset with the others");
            Check(TenshoAssistant.SettingsIndex.Entries.Contains(("Advanced.DesktopAutoUpdate","Advanced")),"the automatic update is found by the settings search");
        }),
    ];
}
