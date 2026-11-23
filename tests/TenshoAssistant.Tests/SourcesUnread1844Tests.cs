using TenshoAssistant.Desktop;

// 0.1844 (X-843 M3, review of 0.1833 to 0.1839): when the desktop application could not read the player's settings at start (no file, a reading
// refused by SettingsGuard), its network services were wired to a default Configuration, every source open: « Game data only », ticked in the
// game, was not followed and « Fetch » still asked Garland Tools. Now each switch is closed until the settings are read, then it is the
// plugin's own.
internal static class SourcesUnread1844Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases=>
    [
        ("X-843 M3: a source switch is closed while the settings are unknown, then follows the plugin's switch",()=>
        {
            var known=false;var allowed=true;var asked=0;
            var gate=DesktopPaths.SourceSwitch(()=>{asked++;return allowed;},()=>known);
            Check(!gate() && asked==0,"settings unknown: closed, the plugin's switch not even asked");
            known=true;
            Check(gate() && asked==1,"settings read: the plugin's switch (open)");
            allowed=false;
            Check(!gate(),"settings read: the plugin's switch (closed: « game data only »)");
        }),
        ("X-843 M3: the application wraps every wired switch, closed until settings are read at start or applied later",()=>
        {
            var host=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant.Desktop/Host.cs"));
            Check(host.Contains("property.SetValue(service,DesktopPaths.SourceSwitch(wired,()=>settingsKnown));",StringComparison.Ordinal),"each wired switch wrapped");
            Check(host.Contains("settingsKnown=saved is not null;",StringComparison.Ordinal),"known when read at start");
            Check(host.Contains("settingsKnown=true;\n        return \"read again from \"+settingsPath;",StringComparison.Ordinal)
                || host.Contains("settingsKnown=true;\r\n        return \"read again from \"+settingsPath;",StringComparison.Ordinal),"known once a reading is applied");
        }),
    ];
}
