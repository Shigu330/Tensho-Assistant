using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TenshoAssistant.Desktop;

/// <summary>Where the update reads a release and its files: GitHub (GitHubReleases) or, in Verify.ps1's test, a fake release in a folder.</summary>
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
        await DesktopUpdater.Copy(source,target,progress,cancel).ConfigureAwait(false);
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

/// <summary>Verify.ps1's fake release: a GitHub-shaped release JSON in a folder; each file is read from that folder by its name.</summary>
internal sealed class FolderRelease(string releaseJson) : IReleaseSource
{
    private readonly string folder=Path.GetDirectoryName(Path.GetFullPath(releaseJson))!;
    /// <summary>Test only: a file whose name is listed here is read with one byte changed (a damaged download).</summary>
    public string? Damaged;

    public Task<string?> Release(UpdatePlan plan,CancellationToken cancel)=>Task.FromResult<string?>(File.ReadAllText(releaseJson));
    public Task<string> Text(ReleaseAsset asset,CancellationToken cancel)=>Task.FromResult(File.ReadAllText(Path.Combine(folder,asset.Name)));

    public async Task Download(ReleaseAsset asset,Stream target,Action<long> progress,CancellationToken cancel)
    {
        await using var source=File.OpenRead(Path.Combine(folder,asset.Name));
        if(Damaged!=asset.Name){await DesktopUpdater.Copy(source,target,progress,cancel).ConfigureAwait(false);return;}
        using var memory=new MemoryStream();
        await source.CopyToAsync(memory,cancel).ConfigureAwait(false);
        var bytes=memory.ToArray();bytes[bytes.Length/2]^=0x5A;
        await target.WriteAsync(bytes,cancel).ConfigureAwait(false);
        progress(bytes.Length);
    }
}

/// <summary>
/// X-121 (R33): the desktop application's automatic update. At start (and on « Check for update ») it asks the release the plan names
/// (DesktopUpdate.Plan: the installed plugin's version, else the latest Stable release), checks it (DesktopUpdate.Refusal), downloads
/// TenshoAssistant.Desktop.zip and its .sha256 from the same release, compares the SHA-256, unpacks it beside in versions\&lt;version&gt;
/// of %LOCALAPPDATA%\TenshoAssistant\Desktop, checks the versions inside, and records it as the copy to start (update.json), the running copy
/// as the previous one. The running exe is never overwritten: the new copy is started (when the window is untouched and the catalogues are
/// loaded), or at the next start, when the copy the user started hands over to it (Forward). « Previous version » goes back and sets the
/// version aside; a new copy that fails to start goes back by itself (RollBackAfterFailure). The setting « Automatic update » turns it off.
/// </summary>
internal sealed class DesktopUpdater
{
    private readonly string own;
    private readonly Version running;
    private readonly string runningFolder;
    private readonly string[] args;
    private readonly DesktopVersionResult plugin;
    private readonly IReleaseSource source;
    private readonly object gate=new();
    private Task? work;
    private (string Key,object[] Args,bool Warning) status=("Desktop.UpdateOff",[],false);
    private InstalledCopy? pending;

    public DesktopUpdater(string own,Version running,string runningFolder,string[] args,DesktopVersionResult plugin,IReleaseSource source)
    {
        this.own=own;this.running=running;this.runningFolder=runningFolder;this.args=args;this.plugin=plugin;this.source=source;
        State=Load(own);
    }

    public UpdateState State { get; private set; }
    /// <summary>Set once a restart into another copy was started: the window closes.</summary>
    public bool ExitRequested { get; private set; }
    /// <summary>A version installed this session, waiting for a restart.</summary>
    public InstalledCopy? Pending{get{lock(gate)return pending;}}
    public bool Busy{get{lock(gate)return work is { IsCompleted: false };}}
    public (string Key,object[] Args,bool Warning) Status{get{lock(gate)return status;}}

    public static string StatePath(string own)=>Path.Combine(own,"update.json");
    public static string VersionsFolder(string own)=>Path.Combine(own,"versions");

    public static UpdateState Load(string own)
    {
        try{return DesktopUpdate.ReadState(File.Exists(StatePath(own)) ? File.ReadAllText(StatePath(own)) : null);}
        catch(IOException){return new();}
        catch(UnauthorizedAccessException){return new();}
    }

    private void Save(UpdateState state)
    {
        Directory.CreateDirectory(own);
        var temp=StatePath(own)+".tmp";
        File.WriteAllText(temp,DesktopUpdate.WriteState(state));
        File.Move(temp,StatePath(own),true);
        State=state;
    }

    private void Set(string key,bool warning=false,params object[] values){lock(gate)status=(key,values,warning);}

    /// <summary>Starts a check in the background (at start when the setting is on, or on the button); one at a time.</summary>
    public void Check(bool manual)
    {
        lock(gate)
        {
            if(work is { IsCompleted: false } || pending is not null)return;
            if(!State.AutoUpdate && !manual){status=("Desktop.UpdateOff",[],false);return;}
            work=Task.Run(()=>Run(CancellationToken.None));
        }
    }

    public Task? Work{get{lock(gate)return work;}}

    public void SetAuto(bool on)
    {
        try{Save(State with {AutoUpdate=on});}
        catch(IOException){}
        catch(UnauthorizedAccessException){}
        if(!on && !Busy && Pending is null)Set("Desktop.UpdateOff");
    }

    private async Task Run(CancellationToken cancel)
    {
        var three=DesktopVersionCheck.Three(running);
        try
        {
            var plan=DesktopUpdate.Plan(running,plugin,State.Skipped);
            Console.WriteLine($"Update: plan {plan.Ask} {plan.Version} ({plan.Reason})");
            if(plan.Ask==UpdateAsk.None){Set(plan.Reason,plan.Reason!="Desktop.UpdateCurrent",DesktopVersionCheck.Three(plan.Version!),three);return;}
            Set("Desktop.UpdateChecking");
            var json=await source.Release(plan,cancel).ConfigureAwait(false);
            if(json is null){Set("Desktop.UpdateNoRelease",false,plan.Version is null ? "Stable" : DesktopUpdate.Tag(plan.Version),three);return;}
            var release=DesktopUpdate.ParseRelease(json);
            if(release is null){Set("Desktop.UpdateErrorRelease",true,plan.Version is null ? "Stable" : DesktopVersionCheck.Three(plan.Version),three);return;}
            var refusal=DesktopUpdate.Refusal(release,running,State.Skipped,plan.Ask==UpdateAsk.Tag ? plan.Version : null);
            if(refusal is not null)
            {
                Console.WriteLine($"Update: {release.Tag} not installed ({refusal})");
                Set(refusal,refusal.Contains("Error",StringComparison.Ordinal),release.Version is null ? release.Tag : DesktopVersionCheck.Three(release.Version),three);
                return;
            }
            var installed=await Install(release,cancel).ConfigureAwait(false);
            if(installed is null)return;
            lock(gate){pending=installed;status=("Desktop.UpdateInstalled",[installed.Version],false);}
        }
        catch(Exception error) when(error is HttpRequestException or IOException or UnauthorizedAccessException or TaskCanceledException or InvalidDataException or InvalidOperationException)
        {
            Console.WriteLine($"Update failed: {error.GetType().Name}: {error.Message}");
            Set("Desktop.UpdateErrorNetwork",true,"GitHub",three);
        }
    }

    /// <summary>Download, SHA-256, unpack beside, versions inside, state; the installed copy, or null (status says why).</summary>
    internal async Task<InstalledCopy?> Install(ReleaseInfo release,CancellationToken cancel)
    {
        var three=DesktopVersionCheck.Three(running);
        var version=DesktopVersionCheck.Three(release.Version!);
        var zipAsset=release.Asset(DesktopUpdate.ZipName)!;
        var expected=DesktopUpdate.ParseHash(await source.Text(release.Asset(DesktopUpdate.HashName)!,cancel).ConfigureAwait(false),DesktopUpdate.ZipName);
        if(expected is null){Set("Desktop.UpdateErrorHash",true,version,three);return null;}
        var downloads=Path.Combine(own,"downloads");
        Directory.CreateDirectory(downloads);
        var zip=Path.Combine(downloads,DesktopUpdate.ZipName+".partial");
        var total=Math.Max(zipAsset.Size,1)/1048576.0;
        string actual;
        await using(var file=new FileStream(zip,FileMode.Create,FileAccess.ReadWrite,FileShare.None))
        {
            await source.Download(zipAsset,file,bytes=>Set("Desktop.UpdateDownloading",false,version,(bytes/1048576.0).ToString("0"),total.ToString("0")),cancel).ConfigureAwait(false);
            if(file.Length>DesktopUpdate.MaxZipBytes){Set("Desktop.UpdateErrorPackage",true,version,three);return null;}
            file.Position=0;
            actual=Convert.ToHexString(await SHA256.HashDataAsync(file,cancel).ConfigureAwait(false)).ToLowerInvariant();
        }
        if(actual!=expected)
        {
            File.Delete(zip);
            Console.WriteLine($"Update: {DesktopUpdate.ZipName} of {release.Tag} has SHA-256 {actual}, the release publishes {expected}: refused");
            Set("Desktop.UpdateErrorHash",true,version,three);
            return null;
        }
        Set("Desktop.UpdateUnpacking",false,version);
        var versions=VersionsFolder(own);
        var target=Path.Combine(versions,version);
        var partial=target+".partial";
        if(Directory.Exists(partial))Directory.Delete(partial,true);
        // ExtractToDirectory refuses an entry that would land outside the folder.
        ZipFile.ExtractToDirectory(zip,partial);
        File.Delete(zip);
        var problem=CheckCopy(partial,release.Version!);
        if(problem is not null)
        {
            Directory.Delete(partial,true);
            Console.WriteLine($"Update: {release.Tag} refused after unpacking: {problem}");
            Set("Desktop.UpdateErrorPackage",true,version,three);
            return null;
        }
        if(Directory.Exists(target))
        {
            if(DesktopUpdate.SameFolder(target,runningFolder))throw new InvalidOperationException("the running copy is "+target);
            Directory.Delete(target,true);
        }
        Directory.Move(partial,target);
        var installed=new InstalledCopy(version,target);
        Save(DesktopUpdate.AfterInstall(State,installed,new InstalledCopy(DesktopVersionCheck.Three(running),runningFolder)));
        foreach(var old in DesktopUpdate.Obsolete(State,Directory.GetDirectories(versions),runningFolder))
            try{Directory.Delete(old,true);}catch(IOException){}catch(UnauthorizedAccessException){}
        Console.WriteLine($"Update: {release.Tag} installed in {target} (SHA-256 {actual} as published); previous copy {runningFolder}");
        return installed;
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

    /// <summary>Called once per frame: an installed version restarts the app when nothing is going on (DesktopUpdate.MayRestart).</summary>
    public void Tick(bool loading,bool used)
    {
        var ready=Pending;
        if(ready is not null && !ExitRequested && DesktopUpdate.MayRestart(loading,used))Restart(ready);
    }

    public void RestartNow(){if(Pending is { } ready)Restart(ready);}

    /// <summary>« Previous version »: back to the previous copy, the version left set aside, and restart into it.</summary>
    public void GoBack()
    {
        if(DesktopUpdate.RollBack(State) is not { } back || back.Current is not { } current)return;
        Save(back);
        lock(gate)pending=null;
        if(DesktopUpdate.SameFolder(current.Folder,runningFolder)){Set("Desktop.UpdateSkipped",false,back.Skipped ?? "",DesktopVersionCheck.Three(running));return;}
        Restart(current);
    }

    private void Restart(InstalledCopy copy)
    {
        var exe=Path.Combine(copy.Folder,DesktopUpdate.ExeName);
        Console.WriteLine($"Update: restarting into {exe}");
        Set("Desktop.UpdateRestarting",false,copy.Version);
        Start(exe,DesktopUpdate.RestartArgs(args));
        ExitRequested=true;
    }

    internal static Process? Start(string exe,IEnumerable<string> arguments)
    {
        var info=new ProcessStartInfo(exe){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(exe)!};
        foreach(var argument in arguments)info.ArgumentList.Add(argument);
        return Process.Start(info);
    }

    /// <summary>At start, before anything else: the copy the user started hands over to the newest installed copy; true when it did.</summary>
    public static bool Forward(string own,Version running,string runningFolder,string[] args)
    {
        if(args.Contains("--no-forward"))return false;
        var exe=DesktopUpdate.ForwardTo(Load(own),running,runningFolder,File.Exists);
        if(exe is null)return false;
        try{return Start(exe,DesktopUpdate.RestartArgs(args)) is not null;}
        catch(System.ComponentModel.Win32Exception){return false;}
    }

    /// <summary>A copy started as Current that fails before its window: back to the previous copy (version set aside); the text to show, or null.</summary>
    public static string? RollBackAfterFailure(string own,string runningFolder)
    {
        var state=Load(own);
        if(state.Current is not { } current || !DesktopUpdate.SameFolder(current.Folder,runningFolder) || DesktopUpdate.RollBack(state) is not { } back)return null;
        try
        {
            Directory.CreateDirectory(own);
            File.WriteAllText(StatePath(own),DesktopUpdate.WriteState(back));
        }
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
        return $"Tensho Assistant (desktop) {current.Version} could not start: the next start goes back to {back.Current!.Version}.";
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
