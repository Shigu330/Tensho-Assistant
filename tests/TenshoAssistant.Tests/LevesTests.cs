// Test audit of 2026-11-09, batch 2 (0.1021): Leves363Tests, Leves968Tests, LevePrepare988Tests, GameLeves793Tests merged into LevesTests; every case is kept.
using TenshoAssistant.Leves;
using TenshoAssistant.Maps;
internal static class LevesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static (uint Map,uint Icon,float X,float Z) Key((uint Map,uint Icon,float X,float Z) marker)=>marker;
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Leves363Tests: 0.363 (g, C-14): guildleves — filters by level, type, client and state; unknown completion never counts as completed.
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
        // From Leves968Tests: X-87 (0.968): an accepted leve's marker is dropped once that leve is under way (and any marker the game does not draw); leve targets get the leve icon.
        ("Leves 968: ShouldRender and active objective filter, active leve read, targets drawn with its icon",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(info->ShouldRender && (active==0 || info->ObjectiveId!=active))From(info);"),"filter");
            Check(reader.Contains("{ActiveLeve=ReadActiveLeve()}"),"active leve");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("(monster.EventId & 0xFFFF)!=leve.Objective") && map.Contains("\"MapView.LeveTarget\""),"targets");
        }),
        // From LevePrepare988Tests: X-103 (0.988): « Prepare » puts what the accepted crafting leves still need into a crafting list.
        ("Leve prepare 988: quantities added up per item, held taken off, recipes only, unknown quantity counts one",() =>
        {
            var held=new Dictionary<uint,long>{[1]=2};
            var missing=LevePrepare.Missing([(1u,3),(1u,3),(2u,null),(3u,5),(0u,4)],id=>held.GetValueOrDefault(id),id=>id!=3);
            Check(missing.Count==2 && missing[0]==(1u,4L) && missing[1]==(2u,1L),"missing");
        }),
        // From GameLeves793Tests: 0.793 (X-32, user request): the leve areas from the game's own markers, without opening its map.
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
