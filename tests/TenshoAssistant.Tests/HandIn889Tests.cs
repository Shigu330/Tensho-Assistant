// 0.889 (X-73.18): items asked at a hand-in, linked to the one quest whose step changes within 15 s after the window closes.
internal static class HandIn889Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hand-in: read while « Request » shows, linked to exactly one quest whose step changed within 15 s, Quest row = 0x10000 + id",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("var shown=gui.GetAddonByName(\"Request\") is { IsNull:false,IsVisible:true };"),"window");
            Check(watcher.Contains("if(now-pending.ClosedAt>15_000){handIn=null;return;}") && watcher.Contains("if(changed.Length!=1)return;"),"one quest, 15 s");
            Check(watcher.Contains("var quest=0x10000u+changed[0];"),"quest row");
        }),
    ];
}
