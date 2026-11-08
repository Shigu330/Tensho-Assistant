using TenshoAssistant.Discoveries;

// X-76 P8 (0.935): kills and duty chests counted, the denominators of drop rates (sector visits: VoyageVisit, 0.934).
internal static class DropCounts935Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Drop counts 935: a kill per credited death, a chest once per object, merge tool aware",() =>
        {
            Check((int)DiscoveryKind.MonsterKill==33 && (int)DiscoveryKind.DutyChestOpened==34,"kinds");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.MonsterKill,npc.NameId,0,npc.Level,at,territory);") && watcher.Contains("if(chestsSeen.Add(item.ChestObjectId))store.SeeSource(DiscoveryKind.DutyChestOpened,duty,0,1,at,territory);"),"counted");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("KINDS=set(range(1,35))-{26,27}"),"merge tool");
        }),
    ];
}
