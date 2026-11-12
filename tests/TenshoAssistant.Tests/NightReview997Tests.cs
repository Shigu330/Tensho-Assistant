using TenshoAssistant.Beastmaster;

// 0.997: the four defects the review of the night's versions confirmed.
internal static class NightReview997Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Night review 997: beasts not read and none held are different plan keys",() =>
        {
            Check(BeastTeams.Key(null,null)!=BeastTeams.Key(new HashSet<uint>(),null),"keys");
        }),
        ("Night review 997: the duty « unknown » line only without progress; unreadable saved files kept; one harvest per gain burst, ripe only",() =>
        {
            var duties=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Duties.cs"));
            Check(duties.Contains("if (progress is null) ImGui.TextDisabled(localization.Get(DutyProgressUnavailable"),"duty line");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Collections/AchievementProgressWatcher.Dalamud.cs")).Contains("else{saveBlocked=true;log.Warning(read.Exception?.GetBaseException(),\"Saved achievement progress unreadable: kept as is, not written over.\");}"),"achievements kept (0.1140: read off the frame)");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Duties/DutyProgressReader.Dalamud.cs")).Contains("else{log.Warning(\"Saved duty progress unreadable: kept as is, not written over.\");this.file=null;}"),"duties kept");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs")).Contains("if(at-lastHarvest>=TimeSpan.FromSeconds(5))"),"one harvest");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs")).Contains("if(ripe.Length==0){ChatNotice?.Invoke(localization.Get(\"Garden.HarvestNoneRipe\"));return;}"),"ripe only");
        }),
    ];
}
