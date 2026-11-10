using System.Text.Json;

namespace TenshoAssistant.Desktop;

/// <summary>Result of the start-up check of the desktop application against the installed plugin (R33).</summary>
public enum DesktopVersionState { Same, Different, NotFound }

/// <summary>The app's version, the installed plugin found (path and version) and how they compare.</summary>
public sealed record DesktopVersionResult(DesktopVersionState State,Version App,Version? Installed,string? InstalledPath);

/// <summary>
/// R33: at start the desktop application says whether its version is the installed plugin's. The plugin is looked for where Dalamud loads
/// it: XIVLauncher's installedPlugins\TenshoAssistant\&lt;version&gt;\TenshoAssistant.dll (the custom repository's install), then the
/// developer plugin locations of dalamudConfig.json (DevPluginLoadLocations: a DLL or a folder). Same: one of them has the app's version
/// (major.minor.build); Different: the highest one found and the app's, both updated together from the same release; NotFound: none.
/// Pure (file reads given as delegates) so the tests run it without XIVLauncher.
/// </summary>
public static class DesktopVersionCheck
{
    public const string PluginFile="TenshoAssistant.dll";

    /// <summary>The DLL paths to look at, in order: installed versions (highest folder first), then the developer locations.</summary>
    public static IReadOnlyList<string> Candidates(string xivLauncher,Func<string,IEnumerable<string>> folders,string? dalamudConfigJson)
    {
        var list=new List<string>();
        var installed=Path.Combine(xivLauncher,"installedPlugins","TenshoAssistant");
        foreach(var folder in folders(installed).OrderByDescending(f=>Version.TryParse(Path.GetFileName(f),out var v) ? v : new Version(0,0)))
            list.Add(Path.Combine(folder,PluginFile));
        foreach(var location in DevLocations(dalamudConfigJson))
            list.Add(location.EndsWith(".dll",StringComparison.OrdinalIgnoreCase) ? location : Path.Combine(location,PluginFile));
        return list.Where(path=>string.Equals(Path.GetFileName(path),PluginFile,StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>The enabled developer plugin locations of dalamudConfig.json (Newtonsoft's "$values" list or a plain array).</summary>
    public static IReadOnlyList<string> DevLocations(string? dalamudConfigJson)
    {
        if(string.IsNullOrWhiteSpace(dalamudConfigJson))return [];
        try
        {
            using var document=JsonDocument.Parse(dalamudConfigJson,new JsonDocumentOptions{AllowTrailingCommas=true,CommentHandling=JsonCommentHandling.Skip});
            if(!document.RootElement.TryGetProperty("DevPluginLoadLocations",out var locations))return [];
            if(locations.ValueKind==JsonValueKind.Object && locations.TryGetProperty("$values",out var values))locations=values;
            if(locations.ValueKind!=JsonValueKind.Array)return [];
            return locations.EnumerateArray()
                .Where(item=>item.ValueKind==JsonValueKind.Object && item.TryGetProperty("Path",out var path) && path.ValueKind==JsonValueKind.String
                    && !(item.TryGetProperty("IsEnabled",out var enabled) && enabled.ValueKind==JsonValueKind.False))
                .Select(item=>item.GetProperty("Path").GetString()!)
                .Where(path=>path.Length>0).ToArray();
        }
        catch(JsonException){return [];}
    }

    /// <summary>Compares the app's version with the plugins found (path, version); versions compared on major.minor.build.</summary>
    public static DesktopVersionResult Compare(Version app,IEnumerable<(string Path,Version Version)> found)
    {
        var plugins=found.ToArray();
        if(plugins.Length==0)return new(DesktopVersionState.NotFound,app,null,null);
        var same=plugins.FirstOrDefault(plugin=>Three(plugin.Version)==Three(app));
        if(same.Path is not null)return new(DesktopVersionState.Same,app,same.Version,same.Path);
        var highest=plugins.OrderByDescending(plugin=>plugin.Version).First();
        return new(DesktopVersionState.Different,app,highest.Version,highest.Path);
    }

    /// <summary>Runs the check: candidates read through the delegates (exists, version of a DLL).</summary>
    public static DesktopVersionResult Run(Version app,IEnumerable<string> candidates,Func<string,bool> exists,Func<string,Version?> version)
        => Compare(app,candidates.Where(exists).Select(path=>(path,version(path))).Where(pair=>pair.Item2 is not null).Select(pair=>(pair.path,pair.Item2!)));

    /// <summary>A version as « major.minor.build ».</summary>
    public static string Three(Version version)=>$"{version.Major}.{version.Minor}.{Math.Max(version.Build,0)}";

    /// <summary>The language key of the result (Desktop.VersionSame, VersionDifferent, VersionNotFound) and its arguments.</summary>
    public static (string Key,object[] Args) Text(DesktopVersionResult result)=>result.State switch
    {
        DesktopVersionState.Same=>("Desktop.VersionSame",[Three(result.App)]),
        DesktopVersionState.Different=>("Desktop.VersionDifferent",[Three(result.Installed!),Three(result.App)]),
        _=>("Desktop.VersionNotFound",[Three(result.App)]),
    };

    /// <summary>The same result in English, for the console and the headless report (the log stays in English, R09).</summary>
    public static string English(DesktopVersionResult result)=>result.State switch
    {
        DesktopVersionState.Same=>$"Version check: same version, plugin {Three(result.Installed!)} installed ({result.InstalledPath}) and this app {Three(result.App)}.",
        DesktopVersionState.Different=>$"Version check: DIFFERENT versions, installed plugin {Three(result.Installed!)} ({result.InstalledPath}) and this app {Three(result.App)}; "
            +"both are updated together from the same release: take the plugin and the app from one release.",
        _=>$"Version check: no installed Tensho Assistant plugin found (XIVLauncher installedPlugins or Dalamud developer locations); this app is {Three(result.App)}.",
    };
}
