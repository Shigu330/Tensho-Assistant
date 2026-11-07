// 0.771 (X-03, user report): the alarm's link, coloured around it.
internal static class AlarmLinkShape771Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm link: the link holds only its text; the colour starts before it and ends after its terminator",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains(".AddUiForeground(500).Add(link).AddText(localization.Get(\"Alarms.OpenInTensho\"))") && plugin.Contains("RawPayload.LinkTerminator).AddUiForegroundOff();"),"shape");
            Check(!plugin.Contains(".Add(link).AddUiForeground("),"no colour inside the link");
            Check(plugin.Contains("\"Alarm link printed: plugin {0}, id {1}, registered {2};"),"0.773: what Dalamud knows of it");
        }),
    ];
}
