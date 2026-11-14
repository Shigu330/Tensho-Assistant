using TenshoAssistant.Maps;

// 0.1335 (X-351, review of the block 0.1318 to 0.1329, I3): on a map the game has not built, the bit's reading alone hid icons the game
// draws (bit 83 at Mor Dhona: drawn by the game, read locked; dalamud.log of 11/14 11:39:48, docs/evidence/0.1320-map-marker-unlock.md).
// What the game was seen to draw is kept for the session: the maps it built, the bits it drew while read locked. The self-check's verdict
// is real (the review saw it always « Ok »).
internal static class MapMarkerRemember1335Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Map 25 as the game listed it on 11/14 11:39:48: bit 1 drawn and unlocked, bit 83 drawn but read locked, bits 43 and 44 neither.
    private static readonly MapMarkerUnlock.Check[] MorDhona=
    [
        new(1,60442,1045,393,true,0,true),
        new(43,60935,1082,244,false,-1,false),
        new(44,60935,1019,225,false,-1,false),
        new(83,60467,1375,653,true,2,false),
    ];
    private static readonly HashSet<ushort> Unlocked=[1];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marker remember 1335: the case of bit 83: once seen drawn by the game, never hidden on a map the game has not built",() =>
        {
            Check(!MapMarkerUnlock.Visible(83,60467,500,500,26,null,Unlocked),"before: map 26, bit 83 read locked, hidden (the bug)");
            var seen=MapMarkerUnlock.Remember(null,25,MorDhona);
            Check(seen.MapId==25 && seen.Listed.Count==2 && seen.DrawnWhileLocked.SetEquals(new ushort[]{83}),"bit 83 learned as drawn while read locked");
            Check(MapMarkerUnlock.Visible(83,60467,500,500,26,seen,Unlocked),"map 26: bit 83 drawn now");
            Check(!MapMarkerUnlock.Visible(44,60935,500,500,26,seen,Unlocked),"bit 44 (not drawn by the game, read locked) stays hidden elsewhere");
            Check(MapMarkerUnlock.Visible(83,60467,1375,653,25,seen,Unlocked) && !MapMarkerUnlock.Visible(44,60935,1019,225,25,seen,Unlocked),"map 25 built: the game's list decides");
        }),
        ("Map marker remember 1335: a map built earlier keeps its list once the game builds another",() =>
        {
            var first=MapMarkerUnlock.Remember(null,25,MorDhona);
            var other=MapMarkerUnlock.Remember(first,26,[new(7,60412,300,300,false,-1,false)]);
            Check(other.MapId==26 && other.Remembered.ContainsKey(25) && other.Remembered.ContainsKey(26),"both maps remembered");
            Check(MapMarkerUnlock.Visible(83,60467,1375,653,25,other,Unlocked),"back on map 25 (no longer built): the icon the game drew stays");
            Check(!MapMarkerUnlock.Visible(44,60935,1019,225,25,other,Unlocked),"and what it did not draw stays hidden");
            Check(MapMarkerUnlock.Visible(1,60442,1045,393,25,other,Unlocked),"bit 1 read unlocked: drawn");
            Check(other.DrawnWhileLocked.Contains(83),"the learned bit survives a map change");
        }),
        ("Map marker remember 1335: the same object while nothing changes; the memory is bounded",() =>
        {
            var first=MapMarkerUnlock.Remember(null,25,MorDhona);
            Check(ReferenceEquals(MapMarkerUnlock.Remember(first,25,MorDhona),first),"same map, same list: same object (the map's filter is kept)");
            var many=first;
            for(uint map=100;map<100+MapMarkerUnlock.MaximumRemembered+50;map++)many=MapMarkerUnlock.Remember(many,map,[]);
            Check(many.Remembered.Count<=MapMarkerUnlock.MaximumRemembered,"at most "+MapMarkerUnlock.MaximumRemembered+" maps: "+many.Remembered.Count);
            Check(many.DrawnWhileLocked.Contains(83),"the learned bits are kept");
        }),
        ("Map marker remember 1335: the self-check's verdict is real",() =>
        {
            Check(!MapMarkerUnlock.Agrees(MorDhona),"bit 83 disagrees: Suspect, not Ok");
            Check(MapMarkerUnlock.Agrees(MorDhona.Where(check=>check.Bit!=83).ToArray()),"the other eight agree");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("markerUnlockCheckBox=Tuple.Create(MapMarkerUnlock.Agrees(checks),line);") && !reader.Contains("markerUnlockCheckBox=Tuple.Create(true,line);"),"the reader gives the verdict");
            Check(reader.Contains("; reading wrong on: "),"the probe: the sheet columns of each icon the reading gets wrong");
        }),
    ];
}
