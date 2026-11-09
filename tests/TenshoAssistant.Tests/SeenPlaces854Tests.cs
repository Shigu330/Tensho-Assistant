// 0.854 (X-72 A, user choice): the places seen, used by the monster loot page and the hunting log.
using TenshoAssistant.Discoveries;
internal static class SeenPlaces854Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static DiscoveryFact Fact(uint map,int seen,float x)=>new(){Kind=DiscoveryKind.MonsterPlace,Subject=1,Map=map,X=x,Seen=seen};
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen places: one per map, its most seen, the most seen maps first, bounded",() =>
        {
            var rows=SeenPlaces.ByMap([Fact(213,1,1),Fact(213,5,2),Fact(16,3,3),Fact(43,9,4),Fact(15,1,5),Fact(214,2,6)],4);
            Check(rows.Length==4 && rows[0].Map==43 && rows[1].Map==213 && rows[1].X==2 && rows[2].Map==16,"order and best place");
            var loot=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Monsters.cs"));
            var hunt=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.HuntingLog.cs"));
            Check(loot.Contains("DrawSeenPlaces(\"Loot\"+mob") && hunt.Contains("DrawSeenPlaces($\"Hunt{entry.RowId}.{target}\""),"used by the tools");
        }),
    ];
}
