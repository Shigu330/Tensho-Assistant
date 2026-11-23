namespace TenshoAssistant.Desktop;

/// <summary>
/// The desktop application's own places and languages (X-119). What it does not ship is found by PluginUiTest.Host's DalamudPaths (moved
/// there in X-120: Dalamud's assemblies and cimgui.dll, Dalamud's fonts, the game's sqpack); here the plugin's saved settings, the app's own
/// writable folder (%LOCALAPPDATA%\TenshoAssistant\Desktop: catalogue cache, the temporary plugin copy) and the languages. Pure.
/// </summary>
public static class DesktopPaths
{
    /// <summary>
    /// X-843 M3 (0.1844, review of 0.1839): a network service's switch in the application: closed while the player's saved settings are not read
    /// (no settings file, or a reading refused), then the plugin's own switch (CommunitySources.Wire). The safe side when the settings are
    /// unknown: no request leaves, rather than every source open.
    /// </summary>
    public static Func<bool> SourceSwitch(Func<bool> wired,Func<bool> settingsKnown)=>()=>settingsKnown() && wired();

    /// <summary>
    /// X-836 M4 (0.1840, security review before the alpha): the plugin's Release build of the repository the app runs from (development only).
    /// Before, an app without its plugin/ copy (removed by an antivirus) walked up to the drive's root and loaded any
    /// src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll found on the way, and every signed-in user may create C:/src on a default Windows.
    /// Now the build is taken only from a folder that holds the app itself under its src/TenshoAssistant.Desktop/ (the app built in that
    /// repository); an installed copy finds none and says the plugin is missing.
    /// </summary>
    public static string? RepositoryBuild(string appFolder,Func<string,bool> exists)
    {
        var app=Path.TrimEndingDirectorySeparator(Path.GetFullPath(appFolder))+Path.DirectorySeparatorChar;
        for(var folder=new DirectoryInfo(app);folder is not null;folder=folder.Parent)
        {
            var desktop=Path.Combine(folder.FullName,"src","TenshoAssistant.Desktop")+Path.DirectorySeparatorChar;
            if(!app.StartsWith(desktop,StringComparison.OrdinalIgnoreCase))continue;
            var built=Path.Combine(folder.FullName,"src","TenshoAssistant","bin","x64","Release",DesktopVersionCheck.PluginFile);
            return exists(built) ? built:null;
        }
        return null;
    }

    public static string PluginConfig(string appData)=>Path.Combine(PluginUiTest.Host.DalamudPaths.XivLauncher(appData),"pluginConfigs","TenshoAssistant.json");

    /// <summary>X-685 part 2 (0.1745): Tensho's own settings file beside Dalamud's (pluginConfigs\TenshoAssistant\Settings.json), read first.</summary>
    public static string OwnSettings(string dalamudFile)=>Path.Combine(Path.ChangeExtension(dalamudFile,null),"Settings.json");

    /// <summary>
    /// X-763 M3 (review of 0.1746): the note of the settings read names the file actually read; Dalamud's older file read while Tensho's own
    /// is there (a copy of the plugin that cannot read it) is said as older, never as Tensho's own.
    /// </summary>
    public static string ReadNote(string from,string own,bool ownThere)=>"read from "+from+" (read-only, without the Discord addresses)"
        +(ownThere && !string.Equals(from,own,StringComparison.OrdinalIgnoreCase) ? "; older than "+own+", which this copy of the plugin cannot read" : "");

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
