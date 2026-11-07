// 0.815, 0.816 (X-51, user request): the collectable plan's move framed on the game's collectable window, not on the hotbars.
internal static class CollectableHighlight815Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable highlight: the window's button with the move's icon, behind the gathering help's setting, never the hotbars",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectablePanel.cs"));
            Check(panel.Contains("if(!advice.Impossible && info is { Icon:not 0 } wanted && configuration.GatheringNextHighlight && Collectables is { } reader)"),"conditions");
            Check(panel.Contains("reader.WantIcon(wanted.Icon);") && panel.Contains("DrawGameFrame(rect.X,rect.Y,rect.Width,rect.Height);") && !panel.Contains("BarLocator"),"the window, not the bars");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/CollectableReader.Dalamud.cs"));
            Check(reader.Contains("if(manager==null || depth>3 || manager->NodeList==null || manager->NodeListCount>512 || found.Count>=40)return;"),"bounded walk");
        }),
    ];
}
