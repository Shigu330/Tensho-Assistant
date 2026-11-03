// R-12 (0.414): the ringing alarm's chat line carries the game's map link to the gathering point and an « Open in Tensho » link.
internal static class AlarmLinks414Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm links: map link when the point is known, item link handlers bounded and removed on unload",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("if(mainWindow.AlarmPlace(alarm) is { } place)") && plugin.Contains(".AddMapLink(place.TerritoryId,place.MapId,place.X,place.Y,0.05f)"),"map link only when known");
            Check(plugin.Contains("chat.AddChatLinkHandler(alarm.ItemId,") && plugin.Contains("alarmLinks.Count>256") && plugin.Contains("chat.RemoveChatLinkHandler();alarmLinks.Clear();"),
                "one handler per item, bounded, removed on unload");
            Check(plugin.Contains("RawPayload.LinkTerminator") && plugin.Contains("Alarms.OpenInTensho"),"link closed, text translated");
            Check(Read("Alarms/AlarmService.Dalamud.cs").Contains("chat(alarm,$\"{alarm.Label}|{minutes}|{occurrence.ToLocalTime():HH:mm}\")"),"the alarm goes with its line");
            var links=Read("MainWindow.AlarmLinks.cs");
            Check(links.Contains("source.TerritoryId==alarm.TerritoryId") && links.Contains("GatheringPointPixel(source)") && links.Contains("float.IsFinite(coordinate.X)"),
                "the alarm's zone, a known position, a finite coordinate");
        }),
    ];
}
