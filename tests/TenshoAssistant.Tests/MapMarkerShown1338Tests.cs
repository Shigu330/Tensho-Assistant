using TenshoAssistant.Maps;

// 0.1338 (X-351, probe of 2026-11-14 at about 15:45): the game's map opened on Mor Dhona and other zones from Western Thanalan wrote nothing:
// the check read the player's map (AgentMap.CurrentMapId) only. Now the map shown by the game's map window (SelectedMapId) is tried too, and
// the game's list is given to the map whose icons without an unlock bit (always drawn) it holds; none held well enough: nothing compared
// (the list of another map would have hidden this map's icons).
internal static class MapMarkerShown1338Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Mor Dhona's aetheryte, sheet 108.3 at pixel 1064,356, listed by the game at 640,-10688 (dalamud.log of 11/14 11:14:28; map 25 size 100).
    private static readonly (uint,float,float)[] MorDhonaPlain=[(60453,1064,356),(60412,1056,194)];
    private static readonly List<(uint Icon,int X,int Y,int Flags)> MorDhonaList=[(60453,640,-10688,0),(60412,512,-13280,0),(60467,5616,-5936,2)];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marker shown 1338: the game's list is held by its own map's icons without a bit, not by another map's",() =>
        {
            var own=MapMarkerUnlock.Coverage(MorDhonaPlain,MorDhonaList);
            Check(own==(2,2) && MapMarkerUnlock.Holds(own),"Mor Dhona's list holds Mor Dhona's plain icons: "+own);
            var other=MapMarkerUnlock.Coverage([(60453,300,1700),(60412,900,900),(60441,1500,400)],MorDhonaList);
            Check(other==(0,3) && !MapMarkerUnlock.Holds(other),"another map's icons are not in it: "+other);
            Check(MapMarkerUnlock.Holds((1,2)) && !MapMarkerUnlock.Holds((1,3)) && !MapMarkerUnlock.Holds((0,0)),"at least half, and one at least");
            Check(MapMarkerUnlock.Coverage(MorDhonaPlain,[(60453,672,-10688,0)])==(1,2) && MapMarkerUnlock.Coverage(MorDhonaPlain,[(60453,673,-10688,0)])==(0,2),
                "within two world units (32) only");
        }),
        ("Map marker shown 1338: wiring: the player's map and the shown map are both tried, nothing learned from a list that holds neither",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("new[]{agent->CurrentMapId,agent->SelectedMapId}.Where(id=>id!=0).Distinct()"),"both maps tried");
            Check(reader.Contains("if(!MapMarkerUnlock.Holds(coverage))") && reader.Contains("var remembered=MapMarkerUnlock.Remember(gameListedMarkers,mapId,checks);"),"learned for the chosen map only");
            Check(!reader.Contains("MapMarkerUnlock.Remember(gameListedMarkers,agent->CurrentMapId"),"never for the player's map by default");
        }),
    ];
}
