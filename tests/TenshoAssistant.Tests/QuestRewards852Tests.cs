// 0.852 (X-47, user report): quest rewards: Tensho's entry in their menu; why the hover panel is missing, logged.
internal static class QuestRewards852Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest rewards: the quest windows are item windows; a hover without panel says why in developer mode",() =>
        {
            var bridge=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            var windows=bridge[bridge.IndexOf("ItemWindows=",StringComparison.Ordinal)..];
            windows=windows[..windows.IndexOf("};",StringComparison.Ordinal)];
            Check(new[]{"JournalAccept","JournalResult","JournalDetail"}.All(name=>windows.Contains("\""+name+"\"",StringComparison.Ordinal)),"quest windows");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemHoverReader.Dalamud.cs"));
            Check(reader.Contains("log.Information(\"Item hover without panel: item {0}, {1}.\",seen,why);") && reader.Contains("if(blockedLogged!=(seen,why))"),"logged once");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("Developer=()=>configuration.DeveloperMode,"),"wired");
        }),
    ];
}
