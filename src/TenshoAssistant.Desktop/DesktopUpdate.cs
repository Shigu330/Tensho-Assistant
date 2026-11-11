using System.Text.Json;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Desktop;

/// <summary>One file of a GitHub release (name, download address, size in bytes).</summary>
public sealed record ReleaseAsset(string Name,string Url,long Size);

/// <summary>A GitHub release as the update reads it: its tag (Stable-x.y.z), the version of the tag, draft or pre-release, its files.</summary>
public sealed record ReleaseInfo(string Tag,Version? Version,bool Draft,bool Prerelease,IReadOnlyList<ReleaseAsset> Assets)
{
    public ReleaseAsset? Asset(string name)=>Assets.FirstOrDefault(asset=>string.Equals(asset.Name,name,StringComparison.Ordinal));
}

/// <summary>Which release the update asks GitHub for: none, the release of one version (the installed plugin's), or the latest release.</summary>
public enum UpdateAsk { None, Tag, Latest }

/// <summary>The update's plan before any request: what to ask and why (Reason is a language key of Desktop.*, Version its number).</summary>
public sealed record UpdatePlan(UpdateAsk Ask,Version? Version,string Reason);

/// <summary>An installed copy of the application: its version (major.minor.build) and its folder.</summary>
public sealed record InstalledCopy(string Version,string Folder);

/// <summary>
/// The update's saved state (%LOCALAPPDATA%\TenshoAssistant\Desktop\update.json): the copy to start (Current), the one before it (Previous,
/// for going back), a version set aside after going back (Skipped: never installed again by itself), and the setting that turns the
/// automatic update off.
/// </summary>
public sealed record UpdateState(InstalledCopy? Current=null,InstalledCopy? Previous=null,string? Skipped=null,bool AutoUpdate=true);

/// <summary>
/// X-121 (R33, 2026-11-10): the desktop application updates itself from this repository's GitHub releases, with no step for the user. Pure
/// (the requests, files and processes are DesktopUpdater's), so the tests run every decision without network:
/// - target: the installed plugin's version (R33: same number), else the latest Stable release; never an older version than the running one
///   (going back is the « previous version » button), never a version set aside after going back;
/// - checks: the release is a published Stable-x.y.z (not a draft nor a pre-release) of this repository; both files
///   TenshoAssistant.Desktop.zip and TenshoAssistant.Desktop.zip.sha256 are there, at this repository's download address of that tag; the
///   zip's SHA-256 is the one published in the same release (a damaged or incomplete file is refused);
/// - install: beside, in versions\&lt;version&gt; of the app's own folder; the running exe is never overwritten: the copy the user started
///   hands over to the newest installed copy at start (Forward), and keeps working as the « previous version ».
/// Limit (said in SECURITY.md, the FAQ and docs/evidence/x121-desktop-update.md): without code signing, the SHA-256 protects against a
/// damaged download, not against a stolen GitHub account, which could publish both a bad zip and its matching hash.
/// </summary>
public static class DesktopUpdate
{
    public const string Repository="Shigu330/Tensho-Assistant";
    public const string ZipName="TenshoAssistant.Desktop.zip";
    public const string HashName=ZipName+".sha256";
    public const string ExeName="TenshoAssistant.Desktop.exe";
    public const string TagPrefix="Stable-";
    /// <summary>The only place a file is downloaded from: this repository's release downloads (GitHub then redirects to its file host).</summary>
    public const string DownloadPrefix="https://github.com/"+Repository+"/releases/download/";
    /// <summary>Largest package accepted (the app is about 37 MB zipped).</summary>
    public const long MaxZipBytes=300L*1024*1024;

    /// <summary>The version of a tag « Stable-x.y.z », else null.</summary>
    public static Version? TagVersion(string? tag)
    {
        if(tag is null || !tag.StartsWith(TagPrefix,StringComparison.Ordinal))return null;
        var text=tag[TagPrefix.Length..];
        return Regex.IsMatch(text,@"^\d+\.\d+\.\d+$") && Version.TryParse(text,out var version) ? version : null;
    }

    public static string Tag(Version version)=>TagPrefix+DesktopVersionCheck.Three(version);

    /// <summary>A version on major.minor.build only (the fourth number of an assembly version is ignored).</summary>
    public static Version Three(Version version)=>new(version.Major,version.Minor,Math.Max(version.Build,0));

    /// <summary>What to ask GitHub, from the running version, the start-up check against the installed plugin and a version set aside.</summary>
    public static UpdatePlan Plan(Version running,DesktopVersionResult plugin,string? skipped)
    {
        var app=Three(running);
        if(plugin.State==DesktopVersionState.Same)return new(UpdateAsk.None,app,"Desktop.UpdateCurrent");
        if(plugin.State==DesktopVersionState.Different && plugin.Installed is { } installed)
        {
            var wanted=Three(installed);
            if(wanted<=app)return new(UpdateAsk.None,wanted,"Desktop.UpdatePluginOlder");
            if(skipped==DesktopVersionCheck.Three(wanted))return new(UpdateAsk.None,wanted,"Desktop.UpdateSkipped");
            return new(UpdateAsk.Tag,wanted,"Desktop.UpdateChecking");
        }
        return new(UpdateAsk.Latest,null,"Desktop.UpdateChecking");
    }

    /// <summary>A release object of GitHub's API (tag_name, draft, prerelease, assets[name, browser_download_url, size]); null when unreadable.</summary>
    public static ReleaseInfo? ParseRelease(string? json)
    {
        if(string.IsNullOrWhiteSpace(json))return null;
        try
        {
            using var document=JsonDocument.Parse(json);
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("tag_name",out var tagElement) || tagElement.ValueKind!=JsonValueKind.String)return null;
            var tag=tagElement.GetString()!;
            var assets=new List<ReleaseAsset>();
            if(root.TryGetProperty("assets",out var list) && list.ValueKind==JsonValueKind.Array)
                foreach(var item in list.EnumerateArray())
                {
                    if(item.ValueKind!=JsonValueKind.Object)continue;
                    var name=item.TryGetProperty("name",out var n) && n.ValueKind==JsonValueKind.String ? n.GetString() : null;
                    var url=item.TryGetProperty("browser_download_url",out var u) && u.ValueKind==JsonValueKind.String ? u.GetString() : null;
                    var size=item.TryGetProperty("size",out var s) && s.ValueKind==JsonValueKind.Number && s.TryGetInt64(out var bytes) ? bytes : -1;
                    if(name is not null && url is not null)assets.Add(new(name,url,size));
                }
            return new(tag,TagVersion(tag),Flag(root,"draft"),Flag(root,"prerelease"),assets);
        }
        catch(JsonException){return null;}
    }

    private static bool Flag(JsonElement root,string name)=>root.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.True;

    /// <summary>True when the address is this repository's download of that file in that tag, over HTTPS (nothing else is downloaded).</summary>
    public static bool AssetUrlOk(string url,string tag,string name)
        => string.Equals(url,DownloadPrefix+Uri.EscapeDataString(tag)+"/"+Uri.EscapeDataString(name),StringComparison.Ordinal);

    /// <summary>
    /// The release checked before a download: null when it can be installed over the running version, else the reason (a language key):
    /// not a published Stable release, not newer, set aside, a file missing, an address outside this repository, a package too large.
    /// </summary>
    public static string? Refusal(ReleaseInfo release,Version running,string? skipped,Version? wanted)
    {
        if(release.Version is not { } version || release.Draft || release.Prerelease)return "Desktop.UpdateErrorRelease";
        if(wanted is not null && Three(version)!=Three(wanted))return "Desktop.UpdateErrorRelease";
        if(Three(version)<=Three(running))return "Desktop.UpdateLatest";
        if(skipped==DesktopVersionCheck.Three(version))return "Desktop.UpdateSkipped";
        var zip=release.Asset(ZipName);
        var hash=release.Asset(HashName);
        if(zip is null || hash is null)return "Desktop.UpdateErrorRelease";
        if(!AssetUrlOk(zip.Url,release.Tag,ZipName) || !AssetUrlOk(hash.Url,release.Tag,HashName))return "Desktop.UpdateErrorAddress";
        if(zip.Size>MaxZipBytes)return "Desktop.UpdateErrorPackage";
        return null;
    }

    /// <summary>The SHA-256 of a .sha256 file (« hex  name » as sha256sum writes it, or the hex alone), in lower case; null when unreadable or for another file.</summary>
    public static string? ParseHash(string? text,string fileName)
    {
        if(text is null)return null;
        foreach(var raw in text.Split('\n'))
        {
            var line=raw.Trim();
            if(line.Length==0)continue;
            var match=Regex.Match(line,@"^([0-9a-fA-F]{64})(?:\s+\*?(.+))?$");
            if(!match.Success)return null;
            if(match.Groups[2].Success && !string.Equals(match.Groups[2].Value.Trim(),fileName,StringComparison.Ordinal))continue;
            return match.Groups[1].Value.ToLowerInvariant();
        }
        return null;
    }

    /// <summary>The text of a .sha256 file for a hash (as Verify.ps1 writes it).</summary>
    public static string HashLine(string hex,string fileName)=>hex.ToLowerInvariant()+"  "+fileName+"\n";

    public static UpdateState ReadState(string? json)
    {
        if(string.IsNullOrWhiteSpace(json))return new();
        try{return JsonSerializer.Deserialize<UpdateState>(json,Options) ?? new();}
        catch(JsonException){return new();}
    }

    public static string WriteState(UpdateState state)=>JsonSerializer.Serialize(state,Options);

    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};

    /// <summary>The state once a version is installed: it becomes Current, the running copy Previous (the « previous version »); a set-aside version is forgotten once another installs.</summary>
    public static UpdateState AfterInstall(UpdateState state,InstalledCopy installed,InstalledCopy running)
        => state with {Current=installed,Previous=running,Skipped=state.Skipped==installed.Version ? state.Skipped : null};

    /// <summary>Going back: the previous copy becomes Current and the version left is set aside (never installed again by itself); null without a previous copy.</summary>
    public static UpdateState? RollBack(UpdateState state)
        => state.Previous is { } previous ? state with {Current=previous,Previous=null,Skipped=state.Current?.Version} : null;

    /// <summary>
    /// At start, the exe to hand over to: the Current copy when it is newer than the running one, in another folder, and its exe exists;
    /// else null (the running copy is the newest: a zip taken by hand stays itself).
    /// </summary>
    public static string? ForwardTo(UpdateState state,Version running,string runningFolder,Func<string,bool> exists)
    {
        if(state.Current is not { } current || !Version.TryParse(current.Version,out var version))return null;
        if(Three(version)<=Three(running) || SameFolder(current.Folder,runningFolder))return null;
        var exe=Path.Combine(current.Folder,ExeName);
        return exists(exe) ? exe : null;
    }

    /// <summary>The folders of versions\ to delete: every one but Current, Previous and the running copy's, and an install's own places
    /// (*.partial, *.old); DesktopInstall.Clean then keeps only Tensho's copies (X-140).</summary>
    public static IReadOnlyList<string> Obsolete(UpdateState state,IEnumerable<string> versionFolders,string runningFolder)
        => versionFolders.Where(folder=>!SameFolder(folder,runningFolder) && !SameFolder(folder,state.Current?.Folder) && !SameFolder(folder,state.Previous?.Folder)
            && !folder.EndsWith(".partial",StringComparison.OrdinalIgnoreCase) && !folder.EndsWith(".old",StringComparison.OrdinalIgnoreCase)).ToArray();

    public static bool SameFolder(string? a,string? b)
        => a is not null && b is not null && string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),StringComparison.OrdinalIgnoreCase);

    /// <summary>The arguments a restarted copy gets: the same ones, without a previous --no-forward, then --no-forward (no hand-over loop).</summary>
    public static string[] RestartArgs(IEnumerable<string> args)=>[..args.Where(arg=>arg!="--no-forward"),"--no-forward"];

    /// <summary>
    /// When an installed version may restart the app by itself: never while the catalogues load or once the player used the window (a click,
    /// a field being typed in); then it opens at the next start, or on « Restart now ».
    /// </summary>
    public static bool MayRestart(bool loading,bool used)=>!loading && !used;
}
