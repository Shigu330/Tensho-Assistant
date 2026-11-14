// X-99 (0.977): the quests not accepted yet come from the game's Map too, so they show after a teleport whatever map the game's window last held.
internal static class UnacceptedQuests977Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Unaccepted quests 977: read from Map.UnacceptedQuestMarkers, bounded, merged without doubles",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("var count=map->UnacceptedQuestMarkers.LongCount;") && reader.Contains("if(count is <=0 or >1024)return [];"),"bounded");
            Check(reader.Contains("current=MapGameLeves.Merge(current,unaccepted,") && reader.Contains("var unaccepted=ReadGameUnaccepted();"),"merged");
            var merged=TenshoAssistant.Maps.MapGameLeves.Merge<(uint,uint,float,float)>([(372u,71031u,10f,10f)],[(372u,71031u,10.5f,10f),(372u,71031u,50f,50f)],marker=>marker);
            Check(merged.Count==2,"no doubles");
        }),
    ];
}
