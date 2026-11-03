using TenshoAssistant.Leves;

// 0.363 (g, C-14): guildleves — filters by level, type, client and state; unknown completion never counts as completed.
internal static class Leves363Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Leves: level, type, client and text filters; completed only when read; accepted from the game",() =>
        {
            var leve = new LeveDefinition(5,"Ailes de nuit",20,2,"Combat",0,"Eloise","Noscea",900,1);
            var progress = new LeveProgressState(new HashSet<uint>{5},[7],80,DateTimeOffset.UnixEpoch);
            Check(LeveCatalog.Matches(leve,null,LeveStateFilter.All,2,"Eloise",15,25,"ailes") && !LeveCatalog.Matches(leve,null,LeveStateFilter.All,3,"",0,0,"")
                && !LeveCatalog.Matches(leve,null,LeveStateFilter.All,0,"",21,0,"") && !LeveCatalog.Matches(leve,null,LeveStateFilter.All,0,"",0,19,""),"filters");
            Check(LeveCatalog.Matches(leve,null,LeveStateFilter.NotCompleted,0,"",0,0,"") && !LeveCatalog.Matches(leve,null,LeveStateFilter.Completed,0,"",0,0,""),"unknown: not completed list only");
            Check(LeveCatalog.Matches(leve,progress,LeveStateFilter.Completed,0,"",0,0,"") && !LeveCatalog.Matches(leve,progress,LeveStateFilter.Accepted,0,"",0,0,""),"read state");
            var reader = Read("Leves/LeveProgressReader.Dalamud.cs");
            Check(reader.Contains("unlocks.IsLeveCompleted(row)",StringComparison.Ordinal) && reader.Contains("quests->LeveQuests",StringComparison.Ordinal)
                && reader.Contains("TimeSpan.FromMilliseconds(2)",StringComparison.Ordinal),"Dalamud completion, accepted leves from memory, 2 ms budget");
            Check(Read("Plugin.cs").Contains("()=>leveReader?.Dispose()",StringComparison.Ordinal),"reader disposed");
        }),
    ];
}
