// 0.759 (X-03, user report): « Open in Tensho » on an alarm's ring.
internal static class AlarmLinks759Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm links: registered again at load for every alarm, once each, so rings printed before a reload still open",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("foreach(var item in (configuration.Alarms ?? []).Select(alarm=>alarm.ItemId)") && plugin.Contains("try{RegisterAlarmLink(item);}catch(ArgumentException){}"),"at load");
            Check(plugin.Contains("if(!alarmLinks.TryGetValue(alarm.ItemId,out var link))") && plugin.Contains("link=RegisterAlarmLink(alarm.ItemId);"),"once per item at ring");
        }),
    ];
}
