namespace TenshoAssistant.Desktop;

/// <summary>
/// The desktop application's own places and languages (X-119). What it does not ship is found by PluginUiTest.Host's DalamudPaths (moved
/// there in X-120: Dalamud's assemblies and cimgui.dll, Dalamud's fonts, the game's sqpack); here the plugin's saved settings, the app's own
/// writable folder (%LOCALAPPDATA%\TenshoAssistant\Desktop: catalogue cache, the temporary plugin copy) and the languages. Pure.
/// </summary>
public static class DesktopPaths
{
    public static string PluginConfig(string appData)=>Path.Combine(PluginUiTest.Host.DalamudPaths.XivLauncher(appData),"pluginConfigs","TenshoAssistant.json");

    public static string Own(string localAppData)=>DesktopInstall.OwnFolder(localAppData);

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
