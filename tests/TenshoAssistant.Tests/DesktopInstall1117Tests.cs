using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1117 (X-140, audit A01 and A03): the desktop app's installer never deletes a folder it did not install, keeps the copies in use,
// gives every download and unpacking its own place (removed after a failure), bounds the unpacked zip, and changes update.json and the
// old copies under one lock shared by the plugin and the app. Behaviour tests (R28): real folders, real files, two writers at once.
internal static class DesktopInstall1117Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    /// <summary>A release in memory (one tag), its .sha256 and its zip; throwAfter: the download breaks after that many bytes.</summary>
    private sealed class FakeSource(string tag,byte[] zip,int throwAfter=-1) : IReleaseSource
    {
        public Task<string?> Release(UpdatePlan plan,CancellationToken cancel)
        {
            var url=DesktopUpdate.DownloadPrefix+tag+"/";
            return Task.FromResult<string?>($$"""{"tag_name":"{{tag}}","draft":false,"prerelease":false,"assets":[{"name":"TenshoAssistant.Desktop.zip","browser_download_url":"{{url}}TenshoAssistant.Desktop.zip","size":{{zip.Length}}},{"name":"TenshoAssistant.Desktop.zip.sha256","browser_download_url":"{{url}}TenshoAssistant.Desktop.zip.sha256","size":94}]}""");
        }
        public Task<string> Text(ReleaseAsset asset,CancellationToken cancel)=>Task.FromResult(DesktopUpdate.HashLine(Convert.ToHexString(SHA256.HashData(zip)),DesktopUpdate.ZipName));
        public async Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel)
        {
            if(throwAfter>=0){await target.WriteAsync(zip.AsMemory(0,Math.Min(throwAfter,zip.Length)),cancel);throw new IOException("connection lost (test)");}
            await target.WriteAsync(zip,cancel);
            progress(zip.Length);
        }
    }

    /// <summary>A package as Verify.ps1 makes it (exe, app DLL, plugin\TenshoAssistant.dll of one version); extra: more entries added.</summary>
    private static (byte[] Zip,Version Version) Package(string temp,Action<ZipArchive>? extra=null)
    {
        var assembly=typeof(DesktopInstall1117Tests).Assembly.Location;
        var version=DesktopUpdate.Three(AssemblyName.GetAssemblyName(assembly).Version!);
        using var memory=new MemoryStream();
        using(var archive=new ZipArchive(memory,ZipArchiveMode.Create,leaveOpen:true))
        {
            using(var exe=new StreamWriter(archive.CreateEntry(DesktopUpdate.ExeName).Open()))exe.Write("exe");
            // 0.1202: stored, not compressed (the same real zip and files; packing two 7 MB DLLs at each install was most of the time).
            archive.CreateEntryFromFile(assembly,"TenshoAssistant.Desktop.dll",CompressionLevel.NoCompression);
            archive.CreateEntryFromFile(assembly,"plugin/TenshoAssistant.dll",CompressionLevel.NoCompression);
            extra?.Invoke(archive);
        }
        return (memory.ToArray(),version);
    }

    private static ReleaseInfo Release(FakeSource source,Version version)
        => DesktopUpdate.ParseRelease(source.Release(new UpdatePlan(UpdateAsk.Tag,version,""),CancellationToken.None).Result!)!;

    /// <summary>DesktopInstall.Install as the app or the plugin calls it; the steps reported.</summary>
    private static (InstalledCopy? Copy,List<InstallStep> Steps) Install(string own,string folder,FakeSource source,Version version,string? running=null)
    {
        var steps=new List<InstallStep>();
        var copy=DesktopInstall.Install(own,folder,Release(source,version),source,running,(step,_,_,_)=>{lock(steps)steps.Add(step);},_=>{},CancellationToken.None).GetAwaiter().GetResult();
        return (copy,steps);
    }

    private static bool NoLeftovers(string own,string folder)
        => (!Directory.Exists(Path.Combine(own,"downloads")) || !Directory.EnumerateFileSystemEntries(Path.Combine(own,"downloads")).Any())
            && (!Directory.Exists(folder) || !Directory.EnumerateDirectories(folder,"*.partial").Any() && !Directory.EnumerateDirectories(folder,"*.old").Any());

    private static void InTemp(Action<string> run)
    {
        var temp=Path.Combine(Path.GetTempPath(),"tensho-install-1117-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try{run(temp);}
        finally{try{Directory.Delete(temp,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop install 1117: a foreign folder named like the version is refused and left intact, from the plugin's button and from the app's update",() => InTemp(temp =>
        {
            var (zip,version)=Package(temp);var three=DesktopVersionCheck.Three(version);var tag=DesktopUpdate.Tag(version);
            var chosen=Path.Combine(temp,"My documents");
            var foreign=Path.Combine(chosen,three);
            Directory.CreateDirectory(Path.Combine(foreign,"photos"));
            File.WriteAllText(Path.Combine(foreign,"notes.txt"),"mine");
            File.WriteAllText(Path.Combine(foreign,"photos","a.jpg"),"jpeg");
            var own=Path.Combine(temp,"own");
            var installer=new DesktopInstaller(own,version,new FakeSource(tag,zip),_=>{});
            Check(installer.Install(chosen),"the install starts");
            installer.Work!.Wait(TimeSpan.FromSeconds(60));
            Check(installer.Status is { Key: "Advanced.DesktopInstallErrorFolder", Warning: true } && installer.Status.Value.Args[1]==foreign,"explicit refusal naming the folder: "+installer.Status);
            Check(File.ReadAllText(Path.Combine(foreign,"notes.txt"))=="mine" && File.ReadAllText(Path.Combine(foreign,"photos","a.jpg"))=="jpeg"
                && Directory.GetFileSystemEntries(foreign).Length==2 && !File.Exists(Path.Combine(foreign,DesktopInstall.Marker)),"the foreign folder is untouched");
            Check(DesktopInstall.Load(own).Current is null,"nothing recorded");
            Check(NoLeftovers(own,chosen),"no download nor unpacking left behind");
            installer.Dispose();
            // The same folder in the app's own versions folder (a copy the user put there by hand, no marker): the app's update refuses too.
            var appOwn=Path.Combine(temp,"app");
            var handMade=Path.Combine(DesktopInstall.VersionsFolder(appOwn),three);
            Directory.CreateDirectory(handMade);File.WriteAllText(Path.Combine(handMade,"keep.txt"),"keep");
            var (copy,steps)=Install(appOwn,DesktopInstall.VersionsFolder(appOwn),new FakeSource(tag,zip),version,Path.Combine(temp,"running"));
            Check(copy is null && steps.Contains(InstallStep.ErrorFolder) && File.ReadAllText(Path.Combine(handMade,"keep.txt"))=="keep","the app's update refuses it too and keeps it");
            Check(NoLeftovers(appOwn,DesktopInstall.VersionsFolder(appOwn)),"nothing left by the refused update");
        })),
        ("Desktop install 1117: Tensho's own copy is kept when valid, replaced when damaged and unused, never replaced when it is Current, Previous or running",() => InTemp(temp =>
        {
            var (zip,version)=Package(temp);var three=DesktopVersionCheck.Three(version);var tag=DesktopUpdate.Tag(version);
            var own=Path.Combine(temp,"own");var folder=Path.Combine(temp,"apps");var target=Path.Combine(folder,three);
            var (first,_)=Install(own,folder,new FakeSource(tag,zip),version);
            Check(first is not null && DesktopUpdate.SameFolder(first.Folder,target) && File.Exists(Path.Combine(target,DesktopInstall.Marker)),"installed and marked as Tensho's");
            File.WriteAllText(Path.Combine(target,"settings-of-the-copy.txt"),"kept");
            var (again,_)=Install(own,folder,new FakeSource(tag,zip),version);
            Check(again is not null && File.Exists(Path.Combine(target,"settings-of-the-copy.txt")),"a valid copy of Tensho is kept as it is, not deleted and unpacked again");
            File.Delete(Path.Combine(target,DesktopUpdate.ExeName));
            var (repaired,_)=Install(own,folder,new FakeSource(tag,zip),version);
            Check(repaired is not null && File.Exists(Path.Combine(target,DesktopUpdate.ExeName)) && NoLeftovers(own,folder),"Tensho's damaged, unused copy is replaced");
            foreach(var inUse in new[]{"Current","Previous","running"})
            {
                File.Delete(Path.Combine(target,DesktopUpdate.ExeName));
                File.WriteAllText(Path.Combine(target,"witness.txt"),inUse);
                var recorded=new InstalledCopy(three,target);
                DesktopInstall.Update(own,_=>inUse switch{"Current"=>new UpdateState(Current:recorded),"Previous"=>new UpdateState(Previous:recorded),_=>new UpdateState()});
                var (refused,steps)=Install(own,folder,new FakeSource(tag,zip),version,inUse=="running" ? target : null);
                Check(refused is null && steps.Contains(InstallStep.ErrorFolder) && File.ReadAllText(Path.Combine(target,"witness.txt"))==inUse,"the copy in use ("+inUse+") is never replaced");
                File.WriteAllText(Path.Combine(target,DesktopUpdate.ExeName),"exe");
            }
            Check(NoLeftovers(own,folder),"no leftovers");
        })),
        ("Desktop install 1117: a broken download and a refused zip leave nothing; the zip is bounded (entries, unpacked bytes, entries outside)",() => InTemp(temp =>
        {
            var (zip,version)=Package(temp);var tag=DesktopUpdate.Tag(version);
            var own=Path.Combine(temp,"own");var folder=Path.Combine(temp,"apps");
            try{Install(own,folder,new FakeSource(tag,zip,throwAfter:1000),version);Check(false,"the broken download throws");}
            catch(IOException){}
            Check(NoLeftovers(own,folder),"a broken download leaves no partial file");
            var (many,_)=Package(temp,archive=>{for(var i=0;i<DesktopInstall.MaxEntries;i++)archive.CreateEntry($"extra/{i}.txt");});
            var (tooMany,steps)=Install(own,folder,new FakeSource(tag,many),version);
            Check(tooMany is null && steps.Contains(InstallStep.ErrorPackage) && NoLeftovers(own,folder) && !Directory.Exists(Path.Combine(folder,DesktopVersionCheck.Three(version))),"too many entries: refused, nothing left");
            var (outside,_)=Package(temp,archive=>{using var w=new StreamWriter(archive.CreateEntry("../escaped.txt").Open());w.Write("x");});
            var (escaped,steps2)=Install(own,folder,new FakeSource(tag,outside),version);
            Check(escaped is null && steps2.Contains(InstallStep.ErrorPackage) && !File.Exists(Path.Combine(folder,"escaped.txt")) && NoLeftovers(own,folder),"an entry outside the folder: refused, nothing written outside");
            // The unpacked-bytes cap counts what is written, whatever the entries announce.
            byte[] big;
            using(var memory=new MemoryStream())
            {
                using(var archive=new ZipArchive(memory,ZipArchiveMode.Create,leaveOpen:true))
                    using(var w=archive.CreateEntry("big.bin",CompressionLevel.SmallestSize).Open())w.Write(new byte[200_000]);
                big=memory.ToArray();
            }
            var bounded=Path.Combine(temp,"bounded");
            using(var stream=new MemoryStream(big))Check(DesktopInstall.Unpack(stream,bounded,DesktopInstall.MaxEntries,100_000)?.Contains("bytes unpacked")==true,"more bytes than allowed: refused");
            using(var stream=new MemoryStream(big))Check(DesktopInstall.Unpack(stream,Path.Combine(temp,"fits"),DesktopInstall.MaxEntries,10_000_000) is null,"within the bounds: unpacked");
        })),
        ("Desktop install 1117: two installs at once in the same folders, and two writers of update.json at once, lose nothing",() => InTemp(temp =>
        {
            var (zip,version)=Package(temp);var three=DesktopVersionCheck.Three(version);var tag=DesktopUpdate.Tag(version);
            var own=Path.Combine(temp,"own");var folder=Path.Combine(temp,"apps");
            var both=Enumerable.Range(0,2).Select(_=>Task.Run(()=>Install(own,folder,new FakeSource(tag,zip),version))).ToArray();
            Task.WaitAll(both,TimeSpan.FromSeconds(60));
            Check(both.All(task=>task.Result.Copy is not null && DesktopUpdate.SameFolder(task.Result.Copy.Folder,Path.Combine(folder,three))),"both installs end on the same valid copy (one moved, one kept)");
            Check(DesktopInstall.CheckCopy(Path.Combine(folder,three),version) is null && NoLeftovers(own,folder),"the copy is whole; no temporary place left");
            // Plugin and app changing update.json at the same time: every change survives (read again under the lock).
            DesktopInstall.Update(own,_=>new UpdateState(Skipped:"0"));
            var writers=Enumerable.Range(0,2).Select(_=>Task.Run(()=>{for(var i=0;i<40;i++)DesktopInstall.Update(own,state=>state with {Skipped=(int.Parse(state.Skipped!)+1).ToString()});})).ToArray();
            Task.WaitAll(writers,TimeSpan.FromSeconds(60));
            Check(DesktopInstall.Load(own).Skipped=="80","80 changes from two writers, none lost: "+DesktopInstall.Load(own).Skipped);
            Check(!Directory.EnumerateFiles(own,"*.tmp").Any(),"no temporary state file left");
            // The setting changed from the plugin's window while the app holds the lock: refused quickly, the file unchanged.
            var installer=new DesktopInstaller(own,version,new FakeSource(tag,zip),_=>{});
            using(new FileStream(DesktopInstall.LockPath(own),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();
                Check(!installer.SetAutoUpdate(false) && watch.ElapsedMilliseconds<5000 && DesktopInstall.Load(own).AutoUpdate,"a busy lock: « not saved », nothing written, no long wait");
            }
            Check(installer.SetAutoUpdate(false) && !DesktopInstall.Load(own).AutoUpdate && DesktopInstall.Load(own).Skipped=="80","then saved, the rest of the state kept");
            installer.Dispose();
        })),
        ("Desktop install 1117: the clean-up after an update removes only Tensho's old copies, never Current, Previous, running nor a foreign folder",() => InTemp(temp =>
        {
            var own=Path.Combine(temp,"own");var versions=DesktopInstall.VersionsFolder(own);
            string Copy(string name,bool marker,bool exe)
            {
                var path=Path.Combine(versions,name);Directory.CreateDirectory(path);
                if(marker)File.WriteAllText(Path.Combine(path,DesktopInstall.Marker),name);
                if(exe)File.WriteAllText(Path.Combine(path,DesktopUpdate.ExeName),"exe");
                return path;
            }
            var current=Copy("0.1117.0",true,true);var previous=Copy("0.1116.0",false,true);var running=Copy("0.1115.0",false,true);
            var oldMarked=Copy("0.1110.0",true,true);var oldLegacy=Copy("0.1113.0",false,true);
            var foreignNamed=Copy("0.1000.0",false,false);File.WriteAllText(Path.Combine(foreignNamed,"mine.txt"),"x");
            var foreign=Copy("photos",false,true);
            DesktopInstall.Update(own,_=>new UpdateState(Current:new("0.1117.0",current),Previous:new("0.1116.0",previous)));
            var removed=DesktopInstall.Clean(own,running);
            Check(removed.Count==2 && !Directory.Exists(oldMarked) && !Directory.Exists(oldLegacy),"Tensho's old copies removed: "+string.Join(", ",removed));
            Check(Directory.Exists(current) && Directory.Exists(previous) && Directory.Exists(running),"Current, Previous and the running copy kept");
            Check(File.Exists(Path.Combine(foreignNamed,"mine.txt")) && Directory.Exists(foreign),"folders Tensho did not install kept");
            Check(!Directory.EnumerateDirectories(versions,"*.old").Any(),"nothing set aside left");
        })),
        ("Desktop install 1117: wiring (no Save left, update.json changed under the lock on both sides, refusal worded in en and fr)",() =>
        {
            var install=Read("src/TenshoAssistant.Desktop/DesktopInstall.cs");
            Check(!install.Contains("public static void Save(") && !install.Contains("Directory.Delete(target,true)") && !install.Contains("ExtractToDirectory"),"no blind delete, no unpacking by path, no unlocked save");
            Check(install.Contains("FileOptions.DeleteOnClose") && install.Contains("version+\".\"+unique+\".partial\""),"unique places, the zip removed at close");
            var updater=Read("src/TenshoAssistant.Desktop/DesktopUpdater.cs");
            Check(!updater.Contains("File.WriteAllText(StatePath(own)") && updater.Contains("DesktopInstall.Clean(own,runningFolder)") && !updater.Contains("Directory.Delete("),"the app: state and clean-up through the shared lock");
            var plugin=Read("src/TenshoAssistant/Link/DesktopInstaller.cs");
            Check(plugin.Contains("DesktopInstall.Update(own,state=>DesktopInstallRules.AfterInstall(state,copy));") && plugin.Contains("InstallStep.ErrorFolder=>\"Advanced.DesktopInstallErrorFolder\""),"the plugin: state read again under the lock, refusal worded");
            foreach(var lang in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{lang}.json");
                Check(text.Contains("\"DesktopInstallErrorFolder\": ") && text.Contains("\"UpdateErrorFolder\": "),"refusal texts in "+lang);
            }
        }),
    ];
}
