// R-12 (0.414): the ringing alarm's chat line carries the game's map link to the gathering point and an « Open in Tensho » link.
internal static class AlarmLinks414Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm links: map link when the point is known, item link handlers bounded and removed on unload",() =>
        {
            var plugin=Read("Plugin.cs")+Read("Plugin.Alarms.cs");
            // 0.1491 (X-509, review of 0.1490): the whole place link (Dalamud's CreateMapLink: arrow, place and coordinates, end); AddMapLink adds
            // the link's start alone (docs/evidence/0.1491-alarm-place-link.md).
            Check(plugin.Contains("if(mainWindow.AlarmPlace(alarm) is { } place && Alarms.AlarmPlaceLink.Build(place.TerritoryId,place.MapId,place.X,place.Y,log) is { } placeLink)")
                && plugin.Contains("message.AddText(\" \").Append(placeLink);") && !plugin.Contains(".AddMapLink("),"map link only when known, whole");
            // 0.774 (X-03): the game's item link instead of a Dalamud link the game never handed over.
            // 0.836: one Dalamud link only, the developer « linktest » one.
            Check(plugin.Contains(".AddItemLink(alarm.ItemId,false)") && plugin.Contains("Alarms.RightClickTensho") && plugin.Split("AddChatLinkHandler(").Length<=2 && (!plugin.Contains("AddChatLinkHandler(") || plugin.Contains("AddChatLinkHandler(uint.MaxValue-7,")),"item link, text translated");
            Check(Read("Alarms/AlarmService.Dalamud.cs").Contains("chat(alarm,$\"{alarm.Label}|{minutes}|{occurrence.ToLocalTime():HH:mm}\")"),"the alarm goes with its line");
            var links=Read("MainWindow.AlarmLinks.cs");
            Check(links.Contains("source.TerritoryId==alarm.TerritoryId") && links.Contains("GatheringPointPixel(source)") && links.Contains("float.IsFinite(coordinate.X)"),
                "the alarm's zone, a known position, a finite coordinate");
        }),
    ];
}
