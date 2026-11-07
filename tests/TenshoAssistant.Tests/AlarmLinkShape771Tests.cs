// 0.771–0.773 (X-03): the Dalamud link's shape and log; 0.774: the game's item link, whose right-click menu holds Tensho's entry.
internal static class AlarmLinkShape771Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm link: the game's item link, then the hint for the right click; the item menu has Tensho's entry",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("message.AddText(\" \").AddItemLink(alarm.ItemId,false,(mainWindow.AlarmItemName(alarm.ItemId) is { Length:>0 } name ? name+\" \":\"\")+localization.Get(\"Alarms.RightClickTensho\"));"),"0.780: the hint inside the link");
            Check(Read("Items/ItemContextMenuBridge.Dalamud.cs").Contains("raw=gui.HoveredItem;"),"a right click on an item link reaches Tensho's menu through the hovered item");
        }),
    ];
}
