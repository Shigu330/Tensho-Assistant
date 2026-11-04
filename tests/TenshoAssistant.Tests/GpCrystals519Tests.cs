using TenshoAssistant.Gathering;

// 0.519 (user report): shards, crystals and clusters have their own gathering actions.
internal static class GpCrystals519Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));
    private static uint[] Ids(uint job,int level,int gp,uint item)=>GpCrystals.For(job,level,gp,item).Select(action=>action.Id).ToArray();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Crystal actions: by job and level; the shards' one on shards only; the general one first when GP is short",() =>
        {
            Check(Ids(16,85,809,10).SequenceEqual([4589u]),"a crystal, miner");
            Check(Ids(17,85,809,16).SequenceEqual([4590u]),"a cluster, botanist");
            Check(Ids(16,85,809,4).SequenceEqual([4589u,280u]),"a shard: both");
            Check(Ids(16,85,250,4).SequenceEqual([4589u]),"short of GP: the general one");
            Check(Ids(16,85,100,4).Length==0,"not enough GP");
            Check(Ids(16,60,809,4).SequenceEqual([280u]) && Ids(16,60,809,10).Length==0,"below the level");
            Check(GpCrystals.IsShard(2) && GpCrystals.IsShard(7) && !GpCrystals.IsShard(8) && !GpCrystals.IsShard(1) && !GpCrystals.IsShard(5111),"shards are items 2 to 7");
        }),
        ("Crystal plan: no computed amount, drawn before the GP plan, by the item's game category",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            var crystal=panel.IndexOf("UiCategoryId==GpCrystals.UiCategory",StringComparison.Ordinal);
            Check(crystal>=0 && crystal<panel.IndexOf("gpActions.State.Catalog is not { } catalog",StringComparison.Ordinal),"before the plan");
            var body=panel[panel.IndexOf("private void DrawGatheringCrystalPlan",StringComparison.Ordinal)..panel.IndexOf("private IReadOnlyList<GatherNodeEffect> NodeEffects",StringComparison.Ordinal)];
            Check(!body.Contains("Expected") && !body.Contains("GpOptimizer") && body.Contains("\"GatheringPanel.CrystalNote\""),"no figure");
        }),
    ];
}
