using System.Text.Json;

namespace TenshoAssistant.Desktop;

/// <summary>
/// Where the desktop application finds what it does not ship (R05, X-119): Dalamud's assemblies and cimgui.dll in the XIVLauncher install
/// (DALAMUD_HOME if set, as the plugin build, else %APPDATA%\XIVLauncher\addon\Hooks\dev), the game data in the user's sqpack (--sqpack=,
/// else the game folder of XIVLauncher's launcherConfigV3.json, else Steam's default folder), and its own writable folder
/// (%LOCALAPPDATA%\TenshoAssistant\Desktop: catalogue cache, the temporary plugin copy). Pure: the environment is passed in.
/// </summary>
public static class DesktopPaths
{
    public const string SteamSqpack=@"C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY XIV Online\game\sqpack";

    public static string XivLauncher(string appData)=>Path.Combine(appData,"XIVLauncher");

    public static string Hooks(string? dalamudHome,string appData)
        => !string.IsNullOrWhiteSpace(dalamudHome) ? dalamudHome : Path.Combine(XivLauncher(appData),"addon","Hooks","dev");

    public static string Fonts(string appData)=>Path.Combine(XivLauncher(appData),"dalamudAssets","dev","UIRes");

    public static string PluginConfig(string appData)=>Path.Combine(XivLauncher(appData),"pluginConfigs","TenshoAssistant.json");

    public static string Own(string localAppData)=>Path.Combine(localAppData,"TenshoAssistant","Desktop");

    /// <summary>The sqpack folders to try, in order.</summary>
    public static IReadOnlyList<string> SqpackCandidates(string? option,string? launcherConfigJson)
    {
        var list=new List<string>();
        if(!string.IsNullOrWhiteSpace(option))list.Add(option);
        if(GamePath(launcherConfigJson) is { } game)list.Add(Path.Combine(game,"game","sqpack"));
        list.Add(SteamSqpack);
        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>GamePath of launcherConfigV3.json (the folder holding game\ and boot\), or null.</summary>
    public static string? GamePath(string? launcherConfigJson)
    {
        if(string.IsNullOrWhiteSpace(launcherConfigJson))return null;
        try
        {
            using var document=JsonDocument.Parse(launcherConfigJson,new JsonDocumentOptions{AllowTrailingCommas=true,CommentHandling=JsonCommentHandling.Skip});
            return document.RootElement.ValueKind==JsonValueKind.Object && document.RootElement.TryGetProperty("GamePath",out var path)
                   && path.ValueKind==JsonValueKind.String && path.GetString() is { Length:>0 } text ? text : null;
        }
        catch(JsonException){return null;}
    }

    /// <summary>The plugin's text language: the saved one (Configuration.LanguageCode), else Windows' display language.</summary>
    public static string TextLanguage(string? saved,string windowsTwoLetter)
        => !string.IsNullOrWhiteSpace(saved) ? saved.Trim().ToLowerInvariant() : windowsTwoLetter.ToLowerInvariant();

    /// <summary>The game data language (Lumina's names) that goes with a text language: the four client languages, English otherwise.</summary>
    public static string GameLanguage(string code)=>code.ToLowerInvariant() switch
    {
        "fr"=>"French",
        "de"=>"German",
        "ja"=>"Japanese",
        _=>"English",
    };
}
