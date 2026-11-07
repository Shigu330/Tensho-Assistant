// 0.781 (X-03): what the game opens on a right click, in developer mode.
internal static class MenuLog781Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Menu log: developer mode only, before any early return",() =>
        {
            var bridge=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            Check(bridge.Contains("if(configuration.DeveloperMode)") && bridge.IndexOf("Context menu opened:")<bridge.IndexOf("if(disposed || !configuration.ItemContextMenuEnabled"),"logged first");
        }),
    ];
}
