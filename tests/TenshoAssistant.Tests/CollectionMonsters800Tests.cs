using TenshoAssistant.Monsters;

// 0.800 (X-41, user request): nearby monsters that drop a missing mount, minion or orchestrion roll.
internal static class CollectionMonsters800Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collection monsters: only the drops still missing, each once",() =>
        {
            IReadOnlyList<uint> Drops(uint id)=>id==7 ? [1,2,3,2]:[];
            Check(NearbyMonsters.CollectionDrops(7,Drops,item=>item is 2 or 3).SequenceEqual(new uint[]{2,3}),"missing ones");
            Check(NearbyMonsters.CollectionDrops(8,Drops,_=>true).Count==0,"no drops");
        }),
        ("Collection monsters: mounts, minions and rolls read in game; settings on the map, above the heads and on the collections page",() =>
        {
            var notes=Read("MainWindow.NearbyMonsters.cs");
            Check(notes.Contains("found.MountId!=0 && missingMounts.Contains(found.MountId)") && notes.Contains("found.OrchestrionId!=0 && missingRolls.Contains(found.OrchestrionId)"),"kinds");
            Check(Read("MainWindow.Collections.cs").Contains("if(kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Orchestrion)"),"page");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.ShowCollectionMonsters && !settings.AboveCollection,"defaults");
        }),
    ];
}
