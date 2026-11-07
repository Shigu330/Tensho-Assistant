// 0.759 (X-03): links registered again at load; 0.774: replaced by the game's item link.
internal static class AlarmLinks759Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm links: nothing left to register at load, the ring holds the game's item link",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(!plugin.Contains("foreach(var item in (configuration.Alarms ?? []).Select(alarm=>alarm.ItemId)"),"no load-time registration");
            Check(plugin.Contains(".AddItemLink(alarm.ItemId,false,"),"item link");
        }),
    ];
}
