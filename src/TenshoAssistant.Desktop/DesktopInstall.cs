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

/// <summary>What an install is doing or why it stopped, told to its caller (the app's updater or the plugin's installer word it).</summary>
public enum InstallStep { Downloading, Unpacking, ErrorHash, ErrorPackage }

/// <summary>
/// The one install of a desktop application release (X-121, shared with the plugin in X-126): the .sha256 of the release read, the zip
/// downloaded (bounded), its SHA-256 compared with the published one, unpacked beside in &lt;folder&gt;\&lt;version&gt;.partial (an entry that
/// would land outside is refused), the exe and both versions inside checked, then moved to &lt;folder&gt;\&lt;version&gt;. The state file
/// (update.json) is the caller's: the app records the running copy as previous, the plugin the copy that was current.
/// </summary>
public static class DesktopInstall
{
    /// <summary>The app's own writable folder (%LOCALAPPDATA%\TenshoAssistant\Desktop): update.json, versions\, downloads\.</summary>
    public static string OwnFolder(string localAppData)=>Path.Combine(localAppData,"TenshoAssistant","Desktop");
    public static string StatePath(string own)=>Path.Combine(own,"update.json");
    public static string VersionsFolder(string own)=>Path.Combine(own,"versions");

    public static UpdateState Load(string own)
    {
        try{return DesktopUpdate.ReadState(File.Exists(StatePath(own)) ? File.ReadAllText(StatePath(own)) : null);}
        catch(IOException){return new();}
        catch(UnauthorizedAccessException){return new();}
    }

    /// <summary>Written whole through a temporary file, then moved over the old one.</summary>
    public static void Save(string own,UpdateState state)
    {
        Directory.CreateDirectory(own);
        var temp=StatePath(own)+".tmp";
        File.WriteAllText(temp,DesktopUpdate.WriteState(state));
        File.Move(temp,StatePath(own),true);
    }

    /// <summary>
    /// Download, SHA-256, unpack in folder\&lt;version&gt;, versions inside; the installed copy, or null after report(ErrorHash or ErrorPackage).
    /// report(step, version, megabytes done, megabytes in all) for the progress; log for the journal (English). The running copy's folder is
    /// never replaced.
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
        var zip=Path.Combine(downloads,DesktopUpdate.ZipName+".partial");
        var total=(Math.Max(zipAsset.Size,1)/1048576.0).ToString("0");
        string actual;
        await using(var file=new FileStream(zip,FileMode.Create,FileAccess.ReadWrite,FileShare.None))
        {
            await source.Download(zipAsset,file,bytes=>report(InstallStep.Downloading,version,(bytes/1048576.0).ToString("0"),total),cancel).ConfigureAwait(false);
            if(file.Length>DesktopUpdate.MaxZipBytes){report(InstallStep.ErrorPackage,version,"","");return null;}
            file.Position=0;
            actual=Convert.ToHexString(await SHA256.HashDataAsync(file,cancel).ConfigureAwait(false)).ToLowerInvariant();
        }
        if(actual!=expected)
        {
            File.Delete(zip);
            log($"Desktop install: {DesktopUpdate.ZipName} of {release.Tag} has SHA-256 {actual}, the release publishes {expected}: refused");
            report(InstallStep.ErrorHash,version,"","");
            return null;
        }
        report(InstallStep.Unpacking,version,"","");
        var target=Path.Combine(folder,version);
        var partial=target+".partial";
        if(Directory.Exists(partial))Directory.Delete(partial,true);
        // ExtractToDirectory refuses an entry that would land outside the folder.
        ZipFile.ExtractToDirectory(zip,partial);
        File.Delete(zip);
        var problem=CheckCopy(partial,release.Version!);
        if(problem is not null)
        {
            Directory.Delete(partial,true);
            log($"Desktop install: {release.Tag} refused after unpacking: {problem}");
            report(InstallStep.ErrorPackage,version,"","");
            return null;
        }
        if(Directory.Exists(target))
        {
            if(DesktopUpdate.SameFolder(target,runningFolder))throw new InvalidOperationException("the running copy is "+target);
            Directory.Delete(target,true);
        }
        Directory.Move(partial,target);
        log($"Desktop install: {release.Tag} installed in {target} (SHA-256 {actual} as published)");
        return new InstalledCopy(version,target);
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
