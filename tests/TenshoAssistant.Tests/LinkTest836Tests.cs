// 0.836 (X-03): the developer command that prints the link in four forms.
internal static class LinkTest836Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Link test: developer mode only, four forms, each click logged",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("is \"linktest\" && mainWindow.DeveloperMode"),"developer mode only");
            Check(plugin.Contains("Link test clicked: Tensho link") && plugin.Contains("Type=Dalamud.Game.Text.XivChatType.Echo"),"logged, Echo forms");
        }),
    ];
}
