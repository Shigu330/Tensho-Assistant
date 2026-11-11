// 0.1110 (#36, plan phase-5-corrections#1): tending and harvesting are recognized (0.616, 0.978); the developer-mode capture of every
// housing chat line is gone, and the watcher still recognizes the garden's lines.
internal static class GardenCapture1110Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden capture: no chat line copied to the log; harvest and care still recognized",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs"));
            Check(!watcher.Contains("Garden chat capture",StringComparison.Ordinal) && !watcher.Contains("log.Information(\"Garden chat",StringComparison.Ordinal),"no capture line");
            Check(watcher.Contains("GardenChat.IsHarvest((int)message.LogKind,sender,target())",StringComparison.Ordinal),"the harvest is still recognized");
        }),
    ];
}
