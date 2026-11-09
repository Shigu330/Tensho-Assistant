using TenshoAssistant.Reminders;

// #36 (0.978, docs/evidence/0.978-garden-harvest.md): a harvest is an item gain line while a garden bed is the target.
internal static class GardenHarvest978Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden harvest 978: gain line on a bed only, no sender, wired to the journal",() =>
        {
            Check(GardenChat.IsHarvest(62,"",2003757),"harvest");
            Check(!GardenChat.IsHarvest(62,"",0) && !GardenChat.IsHarvest(57,"",2003757) && !GardenChat.IsHarvest(62,"Player",2003757),"not a harvest");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs"));
            Check(watcher.Contains("InHousing() && GardenChat.IsHarvest((int)message.LogKind,sender,target())"),"housing only");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs"));
            Check(page.Contains("\"Garden.HarvestedNotice\"") && page.Contains("kind==GardenChatKind.Harvested"),"journal");
        }),
    ];
}
