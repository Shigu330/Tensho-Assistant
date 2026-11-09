using TenshoAssistant.Maps;

// 0.793 (X-32, user request): the leve areas from the game's own markers, without opening its map.
internal static class GameLeves793Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static (uint Map,uint Icon,float X,float Z) Key((uint Map,uint Icon,float X,float Z) marker)=>marker;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game leves: added when the map window's list lacks them, each once",() =>
        {
            // 17:35:27: the map window's list empty, the game's leve 1670 at 579.8, 276.4 on map 697.
            var game=new[]{((uint)697,(uint)60492,579.8f,276.4f),((uint)697,(uint)60492,541.8f,272.4f)};
            Check(MapGameLeves.Merge([],game,Key).Count==2,"from the game alone");
            var read=new[]{((uint)697,(uint)60492,579.5f,276.9f)};
            var merged=MapGameLeves.Merge(read,game,Key);
            Check(merged.Count==2 && merged[0]==read[0],"the same place within a yalm is not doubled");
            Check(ReferenceEquals(MapGameLeves.Merge(read,[],Key),read),"nothing from the game: the list as read");
        }),
        ("Game leves: read from the game's Map, bounded, merged before the leve memory",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("return WithLeveAreas(MapGameLeves.Merge(current,ReadGameLeves(),"),"merged, then remembered (0.848: after the current quests)");
            Check(reader.Contains("if(count is <=0 or >32)return;") && reader.Contains("From(&map->ActiveLevequestMarker);"),"bounded; the started leve's points too");
        }),
    ];
}
