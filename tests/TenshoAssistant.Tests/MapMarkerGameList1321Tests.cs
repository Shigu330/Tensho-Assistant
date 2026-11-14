using TenshoAssistant.Maps;

// 0.1321 (X-322, dalamud.log of 2026-11-14 11:39:48 at Mor Dhona, docs/evidence/0.1320-map-marker-unlock.md): the bit read by
// IsUnlockLinkUnlocked agreed with the game on 8 icons of 9 on map 25; the icon of bit 83 is drawn by the game while that reading says
// locked. On the map the game has built its own list decides; elsewhere the bit's reading.
internal static class MapMarkerGameList1321Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Map 25 as the game listed it: bit 1 (60442 at 1045,393) and bit 83 (60467 at 1375,653) drawn, bits 43 and 44 not.
    private static readonly MapMarkerUnlock.GameList MorDhona=new(25,new HashSet<(uint,float,float)>{(60442,1045,393),(60467,1375,653)});
    private static readonly HashSet<ushort> Unlocked=[1];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marker game list 1321: on the map the game has built, its list decides (bit 83 drawn, bit 44 hidden)",() =>
        {
            Check(MapMarkerUnlock.Visible(83,60467,1375,653,25,MorDhona,Unlocked),"bit 83: the game draws it, the bit's reading said locked");
            Check(!MapMarkerUnlock.Visible(44,60935,1019,225,25,MorDhona,Unlocked),"bit 44: the icon of the report, not drawn by the game");
            Check(!MapMarkerUnlock.Visible(43,60935,1082,244,25,MorDhona,Unlocked),"bit 43: not drawn");
            Check(MapMarkerUnlock.Visible(1,60442,1045,393,25,MorDhona,Unlocked),"bit 1: drawn");
            Check(MapMarkerUnlock.Visible(0,60412,1056,194,25,MorDhona,Unlocked),"no bit: always");
            Check(!MapMarkerUnlock.Visible(83,60467,1376,653,25,MorDhona,Unlocked),"another pixel is another icon");
        }),
        ("Map marker game list 1321: on another map the bit's reading decides; unknown hides",() =>
        {
            Check(!MapMarkerUnlock.Visible(83,60467,1375,653,26,MorDhona,Unlocked),"map 26: bit 83 read locked, hidden");
            Check(MapMarkerUnlock.Visible(1,60442,10,10,26,MorDhona,Unlocked),"map 26: bit 1 read unlocked, drawn");
            Check(!MapMarkerUnlock.Visible(1,60442,10,10,26,null,null),"nothing read: hidden");
            Check(MapMarkerUnlock.Visible(1,60442,10,10,26,null,Unlocked),"no game map built: the bit's reading");
        }),
        ("Map marker game list 1321: the reader builds the game's list, keeps it while unchanged, and the self-check reports the agreement",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            // 0.1335 (X-351): the list is built, and kept while unchanged, by MapMarkerUnlock.Remember (tested in MapMarkerRemember1335Tests).
            Check(reader.Contains("var remembered=MapMarkerUnlock.Remember(gameListedMarkers,mapId,checks);") && reader.Contains("gameListedMarkers=remembered;"),"listed icons, same set kept");
            Check(reader.Contains("undiscovered,questPoints,unlockedMarkerBits,gameListedMarkers){ActiveLeve=ReadActiveLeve()});"),"published with the state");
            Check(reader.Contains("the bit's reading agrees on {agree} of {checks.Count}"),"the agreement said in the log");
        }),
    ];
}
