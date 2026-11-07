using TenshoAssistant.Events;

// 0.810 (X-45, user report): event vendors shown on the map while absent in game.
internal static class FestivalVendors810Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Festival vendors: absent unless their festival runs; an NPC placed by a running festival too stays",() =>
        {
            var layouts=new FestivalLayouts(new Dictionary<uint,IReadOnlyList<(uint Npc,uint Territory)>>{[1]=[(100,128),(101,128)],[2]=[(101,128),(102,129)]},new Dictionary<uint,string>());
            var none=Festivals.Absent(layouts,new HashSet<uint>());
            Check(none.SetEquals(new[]{((uint)100,(uint)128),((uint)101,(uint)128),((uint)102,(uint)129)}),"nothing running: all absent");
            var second=Festivals.Absent(layouts,new HashSet<uint>{2});
            Check(second.SetEquals(new[]{((uint)100,(uint)128)}),"festival 2 running: its NPCs stay, 101 too");
        }),
        ("Festival vendors: the map's vendor rows leave them out, again when the running festivals change",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("rows=rows.Where(row=>!absent.Contains((row.vendor.NpcId,row.spot.TerritoryId))).ToArray();") && map.Contains("|| mapVendorCache.Festivals!=festivalKey)"),"filtered, rebuilt");
        }),
    ];
}
