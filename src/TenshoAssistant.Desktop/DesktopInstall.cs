using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TenshoAssistant.Desktop;

/// <summary>Where a release and its files are read: GitHub (GitHubReleases) or, in Verify.ps1's test, a fake release in a folder.</summary>
internal interface IReleaseSource
{
    /// <summary>The release object (GitHub's API JSON) asked by the plan, or null when there is none (404).</summary>
    Task<string?> Release(UpdatePlan plan,CancellationToken cancel);
    /// <summary>A small text file of the release (the .sha256).</summary>
    Task<string> Text(ReleaseAsset asset,CancellationToken cancel);
    /// <summary>A file of the release copied into the stream, progress in bytes.</summary>
    Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel);
}

/// <summary>
/// X-121: this repository's GitHub releases over HTTPS. Requests: GET https://api.github.com/repos/Shigu330/Tensho-Assistant/releases/tags/Stable-x.y.z
/// (or releases/latest), then the two files of that release at https://github.com/Shigu330/Tensho-Assistant/releases/download/… (GitHub
/// redirects to its file host). Headers: a fixed User-Agent and Accept only; no token, no player data, nothing sent but the version asked.
/// X-126 (0.1113): shared with the plugin (Settings › Advanced › Desktop application), which asks the same release on a click only.
/// </summary>
internal sealed class GitHubReleases : IReleaseSource
{
    private const string Api="https://api.github.com/repos/"+DesktopUpdate.Repository+"/releases/";
    private static readonly HttpClient Client=Create();

    private static HttpClient Create()
    {
        var client=new HttpClient(new SocketsHttpHandler{AllowAutoRedirect=true,MaxAutomaticRedirections=5,AutomaticDecompression=DecompressionMethods.All}){Timeout=TimeSpan.FromMinutes(10)};
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TenshoAssistant.Desktop","1"));
        return client;
    }

    public async Task<string?> Release(UpdatePlan plan,CancellationToken cancel)
    {
        var url=plan.Ask==UpdateAsk.Tag ? Api+"tags/"+Uri.EscapeDataString(DesktopUpdate.Tag(plan.Version!)) : Api+"latest";
        using var request=new HttpRequestMessage(HttpMethod.Get,url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var response=await Client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token).ConfigureAwait(false);
        if(response.StatusCode==HttpStatusCode.NotFound)return null;
        response.EnsureSuccessStatusCode();
        return await ReadLimited(response,1024*1024,timeout.Token).ConfigureAwait(false);
    }

    public async Task<string> Text(ReleaseAsset asset,CancellationToken cancel)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var response=await Client.GetAsync(Checked(asset.Url),HttpCompletionOption.ResponseHeadersRead,timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadLimited(response,4096,timeout.Token).ConfigureAwait(false);
    }

    public async Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel)
    {
        using var response=await Client.GetAsync(Checked(asset.Url),HttpCompletionOption.ResponseHeadersRead,cancel).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var source=await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
        await DesktopInstall.Copy(source,target,progress,cancel).ConfigureAwait(false);
    }

    /// <summary>Only this repository's release downloads are fetched (checked again here, after DesktopUpdate.Refusal).</summary>
    private static string Checked(string url)
        => url.StartsWith(DesktopUpdate.DownloadPrefix,StringComparison.Ordinal) ? url : throw new InvalidOperationException("download address outside the repository: "+url);

    private static async Task<string> ReadLimited(HttpResponseMessage response,int limit,CancellationToken cancel)
    {
        await using var stream=await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
        using var memory=new MemoryStream();
        var buffer=new byte[16384];
        int read;
        while((read=await stream.ReadAsync(buffer,cancel).ConfigureAwait(false))>0)
        {
            if(memory.Length+read>limit)throw new InvalidDataException("answer larger than "+limit+" bytes");
            memory.Write(buffer,0,read);
        }
        return Encoding.UTF8.GetString(memory.ToArray());
    }
}

/// <summary>What an install is doing or why it stopped, told to its caller (the app's updater or the plugin's installer word it).
/// ErrorFolder (X-140, 0.1117): the version's folder already exists and is not a copy Tensho may replace; it is left untouched.</summary>
public enum InstallStep { Downloading, Unpacking, ErrorHash, ErrorPackage, ErrorFolder }

/// <summary>
/// The one install of a desktop application release (X-121, shared with the plugin in X-126): the .sha256 of the release read, the zip
/// downloaded (bounded), its SHA-256 compared with the published one, unpacked from that same open file into a folder of its own
/// (&lt;folder&gt;\&lt;version&gt;.&lt;unique&gt;.partial; an entry that would land outside, too many entries or too many bytes refused), the exe and
/// both versions inside checked, marked as Tensho's, then moved to &lt;folder&gt;\&lt;version&gt; under the update lock. The state file
/// (update.json) is the caller's: the app records the running copy as previous, the plugin the copy that was current.
/// X-140 (0.1117, audit A01 and A03): a folder that is not Tensho's is never deleted nor replaced (explicit refusal); Current, Previous
/// and the running copy are never replaced; every download and unpacking has its own unique place, removed after a failure; update.json
/// and the clean-up of old copies are changed under one lock file shared by the plugin and the app (read again, changed, written whole).
/// </summary>
public static class DesktopInstall
{
    /// <summary>The app's own writable folder (%LOCALAPPDATA%\TenshoAssistant\Desktop): update.json, versions\, downloads\.</summary>
    public static string OwnFolder(string localAppData)=>Path.Combine(localAppData,"TenshoAssistant","Desktop");
    public static string StatePath(string own)=>Path.Combine(own,"update.json");
    public static string VersionsFolder(string own)=>Path.Combine(own,"versions");
    /// <summary>X-140: the lock file of update.json and of the copies' moves and clean-up, shared by the plugin and the app.</summary>
    public static string LockPath(string own)=>Path.Combine(own,"update.lock");

    /// <summary>X-140: the file written in every copy Tensho installs (its version and the zip's SHA-256): a folder holding it is Tensho's.</summary>
    public const string Marker=".tensho-desktop-copy";
    /// <summary>X-140: at most this many entries and bytes unpacked from the zip (the 0.1116 package: 196 entries, 84 MB).</summary>
    public const int MaxEntries=2000;
    public const long MaxUnpackedBytes=1024L*1024*1024;
    /// <summary>How long a change of the state waits for the lock held by the other side (plugin or app), in milliseconds.</summary>
    public const int LockWaitMs=10_000;
    /// <summary>The wait of a change asked from a window (a checkbox, a button): short, a busy lock says « not saved » instead of freezing a frame.</summary>
    public const int ClickWaitMs=500;

    public static UpdateState Load(string own)
    {
        try{return DesktopUpdate.ReadState(File.Exists(StatePath(own)) ? File.ReadAllText(StatePath(own)) : null);}
        catch(IOException){return new();}
        catch(UnauthorizedAccessException){return new();}
    }

    /// <summary>
    /// X-140: the one way update.json changes: under the lock, the state read again, changed, then written whole through a temporary file of
    /// its own (moved over the old one); nothing written when the change gives the same state back. The state now on disk. IOException when
    /// the lock stays held longer than waitMs.
    /// </summary>
    public static UpdateState Update(string own,Func<UpdateState,UpdateState> change,int waitMs=LockWaitMs)
        => Locked(own,waitMs,()=>
        {
            var before=Load(own);
            var after=change(before);
            if(!ReferenceEquals(after,before))Write(own,after);
            return after;
        });

    private static void Write(string own,UpdateState state)
    {
        Directory.CreateDirectory(own);
        var temp=StatePath(own)+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllText(temp,DesktopUpdate.WriteState(state));
            File.Move(temp,StatePath(own),true);
        }
        finally{TryDelete(temp);}
    }

    /// <summary>
    /// X-140: work done while holding the lock file (opened without sharing: another process, or another thread here, waits); tried again
    /// every 50 ms up to waitMs, then the IOException of the busy file.
    /// </summary>
    internal static T Locked<T>(string own,int waitMs,Func<T> work)
    {
        Directory.CreateDirectory(own);
        var until=Environment.TickCount64+waitMs;
        while(true)
        {
            FileStream held;
            try{held=new FileStream(LockPath(own),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(Environment.TickCount64<until){Thread.Sleep(50);continue;}
            using(held)return work();
        }
    }

    /// <summary>
    /// Download, SHA-256, unpack in folder\&lt;version&gt;, versions inside; the installed copy, or null after report(ErrorHash, ErrorPackage or
    /// ErrorFolder). report(step, version, megabytes done or the folder refused, megabytes in all) for the progress; log for the journal
    /// (English). Placement (X-140): a missing folder receives the copy; Tensho's own valid copy of that version already there is kept and
    /// used as it is; Tensho's own damaged copy that is neither Current, Previous nor running is set aside and replaced; anything else
    /// (a folder Tensho did not install, a file of that name, the copy in use damaged) is refused and left untouched.
    /// </summary>
    internal static async Task<InstalledCopy?> Install(string own,string folder,ReleaseInfo release,IReleaseSource source,string? runningFolder,
        Action<InstallStep,string,string,string> report,Action<string> log,CancellationToken cancel)
    {
        var version=DesktopVersionCheck.Three(release.Version!);
        var zipAsset=release.Asset(DesktopUpdate.ZipName)!;
        var expected=DesktopUpdate.ParseHash(await source.Text(release.Asset(DesktopUpdate.HashName)!,cancel).ConfigureAwait(false),DesktopUpdate.ZipName);
        if(expected is null){report(InstallStep.ErrorHash,version,"","");return null;}
        var downloads=Path.Combine(own,"downloads");
        Directory.CreateDirectory(downloads);
        var unique=Guid.NewGuid().ToString("N");
        var zip=Path.Combine(downloads,DesktopUpdate.ZipName+"."+unique+".partial");
        var target=Path.Combine(folder,version);
        var partial=Path.Combine(folder,version+"."+unique+".partial");
        var total=(Math.Max(zipAsset.Size,1)/1048576.0).ToString("0");
        try
        {
            // The zip lives only while this file is open (removed at close, failure included) and is unpacked from it, never reopened by its path.
            await using var file=new FileStream(zip,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None,81920,FileOptions.DeleteOnClose);
            await source.Download(zipAsset,file,bytes=>report(InstallStep.Downloading,version,(bytes/1048576.0).ToString("0"),total),cancel).ConfigureAwait(false);
            if(file.Length>DesktopUpdate.MaxZipBytes){report(InstallStep.ErrorPackage,version,"","");return null;}
            file.Position=0;
            var actual=Convert.ToHexString(await SHA256.HashDataAsync(file,cancel).ConfigureAwait(false)).ToLowerInvariant();
            if(actual!=expected)
            {
                log($"Desktop install: {DesktopUpdate.ZipName} of {release.Tag} has SHA-256 {actual}, the release publishes {expected}: refused");
                report(InstallStep.ErrorHash,version,"","");
                return null;
            }
            report(InstallStep.Unpacking,version,"","");
            file.Position=0;
            var problem=Unpack(file,partial,MaxEntries,MaxUnpackedBytes) ?? CheckCopy(partial,release.Version!);
            if(problem is not null)
            {
                log($"Desktop install: {release.Tag} refused after unpacking: {problem}");
                report(InstallStep.ErrorPackage,version,"","");
                return null;
            }
            File.WriteAllText(Path.Combine(partial,Marker),version+" "+actual);
            var placed=Locked(own,LockWaitMs,()=>Place(partial,target,release.Version!,Load(own),runningFolder));
            if(placed==Placement.Refused)
            {
                log($"Desktop install: {release.Tag} not installed: {target} exists and is not a copy Tensho may replace; left untouched");
                report(InstallStep.ErrorFolder,version,target,"");
                return null;
            }
            log(placed==Placement.Kept ? $"Desktop install: {release.Tag} already installed in {target} (Tensho's copy, checked): kept as it is"
                : $"Desktop install: {release.Tag} installed in {target} (SHA-256 {actual} as published)");
            return new InstalledCopy(version,target);
        }
        finally{TryDeleteFolder(partial);}
    }

    /// <summary>X-140: what became of the version's folder.</summary>
    internal enum Placement { Moved, Kept, Refused }

    /// <summary>
    /// X-140, under the lock: the checked copy moved to target when it is free; Tensho's valid copy already there kept; Tensho's damaged copy,
    /// unused, set aside (renamed, then removed) and replaced; else refused. Tensho's = holding the Marker, or recorded in update.json, or the
    /// running copy; a recorded or running copy is never replaced.
    /// </summary>
    internal static Placement Place(string partial,string target,Version version,UpdateState state,string? runningFolder)
    {
        if(!Directory.Exists(target) && !File.Exists(target)){Directory.Move(partial,target);return Placement.Moved;}
        if(File.Exists(target))return Placement.Refused;
        var inUse=DesktopUpdate.SameFolder(target,state.Current?.Folder) || DesktopUpdate.SameFolder(target,state.Previous?.Folder) || DesktopUpdate.SameFolder(target,runningFolder);
        if(!inUse && !File.Exists(Path.Combine(target,Marker)))return Placement.Refused;
        if(CheckCopy(target,version) is null)return Placement.Kept;
        if(inUse)return Placement.Refused;
        var aside=target+"."+Guid.NewGuid().ToString("N")+".old";
        Directory.Move(target,aside);
        Directory.Move(partial,target);
        TryDeleteFolder(aside);
        return Placement.Moved;
    }

    /// <summary>
    /// X-140: the zip unpacked from the open stream into folder (a new folder): an entry that would land outside it, more than maxEntries
    /// entries or more than maxBytes bytes written (counted as written, not as the entries announce) refused; the problem, or null.
    /// </summary>
    internal static string? Unpack(Stream zip,string folder,int maxEntries,long maxBytes)
    {
        try
        {
            using var archive=new ZipArchive(zip,ZipArchiveMode.Read,leaveOpen:true);
            if(archive.Entries.Count>maxEntries)return $"{archive.Entries.Count} entries, at most {maxEntries}";
            var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder))+Path.DirectorySeparatorChar;
            Directory.CreateDirectory(root);
            long written=0;
            var buffer=new byte[81920];
            foreach(var entry in archive.Entries)
            {
                var path=Path.GetFullPath(Path.Combine(root,entry.FullName));
                if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))return "an entry lands outside the folder: "+entry.FullName;
                if(entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')){Directory.CreateDirectory(path);continue;}
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var input=entry.Open();
                using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                int read;
                while((read=input.Read(buffer,0,buffer.Length))>0)
                {
                    written+=read;
                    if(written>maxBytes)return $"more than {maxBytes} bytes unpacked";
                    output.Write(buffer,0,read);
                }
            }
            return null;
        }
        catch(InvalidDataException error){return "damaged zip: "+error.Message;}
        catch(IOException error){return "unpacking failed: "+error.Message;}
    }

    /// <summary>
    /// X-140: after the app's update, under the lock with the state read again: every copy of the app's own versions folder that is
    /// Tensho's (Marker, or a copy named by its version holding the exe, as installed before 0.1117) and is neither Current, Previous nor the
    /// running copy is set aside (renamed *.old), then removed after the lock; leftovers of a stopped install (*.old; *.partial older than an
    /// hour; downloads not in use) removed too. A folder that is not Tensho's is never touched. The folders removed.
    /// </summary>
    public static IReadOnlyList<string> Clean(string own,string runningFolder)
    {
        var versions=VersionsFolder(own);
        if(!Directory.Exists(versions))return [];
        var aside=Locked(own,LockWaitMs,()=>
        {
            var state=Load(own);
            var moved=new List<string>();
            foreach(var old in DesktopUpdate.Obsolete(state,Directory.GetDirectories(versions),runningFolder).Where(Owned))
            {
                var to=old+"."+Guid.NewGuid().ToString("N")+".old";
                try{Directory.Move(old,to);moved.Add(old);}
                catch(IOException){}
                catch(UnauthorizedAccessException){}
            }
            return moved;
        });
        foreach(var old in Directory.GetDirectories(versions,"*.old"))TryDeleteFolder(old);
        foreach(var stale in Directory.GetDirectories(versions,"*.partial").Where(dir=>Directory.GetCreationTimeUtc(dir)<DateTime.UtcNow.AddHours(-1)))TryDeleteFolder(stale);
        var downloads=Path.Combine(own,"downloads");
        if(Directory.Exists(downloads))foreach(var file in Directory.GetFiles(downloads,"*.partial"))TryDelete(file);
        return aside;
    }

    /// <summary>X-140: a copy in the app's own versions folder that Tensho installed: its Marker, or (before 0.1117) named by a version and holding the exe.</summary>
    internal static bool Owned(string folder)
        => File.Exists(Path.Combine(folder,Marker)) || Version.TryParse(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)),out _) && File.Exists(Path.Combine(folder,DesktopUpdate.ExeName));

    private static void TryDelete(string file)
    {
        try{if(File.Exists(file))File.Delete(file);}
        catch(IOException){}
        catch(UnauthorizedAccessException){}
    }

    private static void TryDeleteFolder(string folder)
    {
        try{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        catch(IOException){}
        catch(UnauthorizedAccessException){}
    }

    /// <summary>The unpacked copy holds the exe, and the app and its plugin are the release's version; else what is wrong.</summary>
    internal static string? CheckCopy(string folder,Version version)
    {
        if(!File.Exists(Path.Combine(folder,DesktopUpdate.ExeName)))return "no "+DesktopUpdate.ExeName;
        foreach(var dll in new[]{"TenshoAssistant.Desktop.dll",Path.Combine("plugin","TenshoAssistant.dll")})
        {
            var path=Path.Combine(folder,dll);
            if(!File.Exists(path))return "no "+dll;
            Version? found;
            try{found=AssemblyName.GetAssemblyName(path).Version;}
            catch(BadImageFormatException){found=null;}
            if(found is null || DesktopUpdate.Three(found)!=DesktopUpdate.Three(version))return $"{dll} is {found}, not {DesktopVersionCheck.Three(version)}";
        }
        return null;
    }

    internal static async Task Copy(Stream source,Stream target,Action<long> progress,CancellationToken cancel)
    {
        var buffer=new byte[81920];
        long done=0,shown=0;
        int read;
        while((read=await source.ReadAsync(buffer,cancel).ConfigureAwait(false))>0)
        {
            await target.WriteAsync(buffer.AsMemory(0,read),cancel).ConfigureAwait(false);
            done+=read;
            if(done>DesktopUpdate.MaxZipBytes)throw new InvalidDataException("package larger than "+DesktopUpdate.MaxZipBytes+" bytes");
            if(done-shown>=1048576){progress(done);shown=done;}
        }
        progress(done);
    }
}
