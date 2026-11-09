// Test audit of 2026-11-09, batch 2 (0.1021): AchievementProgress368Tests, AchievementSaved987Tests merged into AchievementTests; every case is kept.
using TenshoAssistant.Collections;
internal static class AchievementTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From AchievementProgress368Tests: 0.368 (g, C-15): achievement progress read passively, asked only on a click (address checked, 3 s apart), tracked list kept on reset.
        ("Achievement progress: passive read, request on click only with its address checked, tracked list kept",() =>
        {
            var watcher = Read("Collections/AchievementProgressWatcher.Dalamud.cs");
            Check(watcher.Contains("(nint)Achievement.MemberFunctionPointers.RequestAchievementProgress!=0",StringComparison.Ordinal)
                && watcher.Contains("TimeSpan.FromSeconds(3)",StringComparison.Ordinal),"request guarded and spaced");
            Check(watcher.Contains("if(player.ContentId!=character){character=player.ContentId;progress=all.TryGetValue(character,out var saved) ? new(saved):[];}",StringComparison.Ordinal),"per character (0.987: that character's saved progress)");
            var page = Read("MainWindow.AchievementProgress.cs");
            Check(page.Contains("ImGui.SmallButton(localization.Get(\"AchievementProgress.Ask\")",StringComparison.Ordinal)
                && Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
                    .Sum(file => System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file),@"AchievementProgressSource\?\.Request\(").Count) == 1,"the server is asked from one button only");
            Check(Read("Configuration.cs").Contains("\"TrackedAchievements\",",StringComparison.Ordinal),"tracked list kept on reset");
        }),
        // From AchievementSaved987Tests: X-102 (0.987): achievement progress kept on disk per character; a tracked one seen reaching its maximum is said once.
        ("Achievements 987: completed only from below to the maximum, round trip per character",() =>
        {
            var at=DateTimeOffset.UtcNow;
            Check(AchievementProgressStore.JustCompleted(new(9,10,at),new(10,10,at)),"completed");
            Check(!AchievementProgressStore.JustCompleted(null,new(10,10,at)) && !AchievementProgressStore.JustCompleted(new(10,10,at),new(10,10,at)) && !AchievementProgressStore.JustCompleted(new(3,10,at),new(5,10,at)),"not completed");
            var all=new Dictionary<ulong,Dictionary<uint,AchievementProgress>>{[42]=new(){[7]=new(3,10,at)}};
            Check(AchievementProgressStore.Read(AchievementProgressStore.Write(all)) is { } back && back[42][7].Current==3,"round trip");
            Check(AchievementProgressStore.Read("{bad")==null,"broken file");
        }),
        ("Achievements 987: saved off the frame, the tracked list decides the line",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Collections/AchievementProgressWatcher.Dalamud.cs"));
            Check(watcher.Contains("writer.Enqueue(") && watcher.Contains("tracked?.Invoke(id)==true)completed?.Invoke(id);"),"saved, said");
        }),
    ];
}
