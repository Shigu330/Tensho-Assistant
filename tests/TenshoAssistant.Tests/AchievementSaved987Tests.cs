using TenshoAssistant.Collections;

// X-102 (0.987): achievement progress kept on disk per character; a tracked one seen reaching its maximum is said once.
internal static class AchievementSaved987Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
