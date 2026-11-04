using TenshoAssistant.Gathering;

// 0.519 (user report): shards, crystals and clusters have their own gathering actions.
internal static class GpCrystals519Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));
    private static uint[] Ids(uint job,int level,int gp)=>GpCrystals.For(job,level,gp).Select(action=>action.Id).ToArray();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Crystal actions: by job and level, both on shards, crystals and clusters (0.521); what GP cannot pay is left out",() =>
        {
            Check(Ids(16,85,809).SequenceEqual([4589u,280u]),"miner: both");
            Check(Ids(17,85,809).SequenceEqual([4590u,282u]),"botanist: both");
            Check(Ids(16,85,250).SequenceEqual([4589u]),"250 GP pays one");
            Check(Ids(16,85,160).SequenceEqual([280u]),"160 GP pays the cheaper one");
            Check(Ids(16,85,100).Length==0,"not enough GP");
            Check(Ids(16,60,809).SequenceEqual([280u]) && Ids(16,10,809).Length==0,"below the level");
        }),
        ("Crystal actions (0.522): one recasting is not advised and takes no GP; the panel says when it comes back",() =>
        {
            Check(GpCrystals.For(16,85,160,[4589u]).Single().Id==280,"its GP stays for the other");
            Check(GpCrystals.For(16,85,809,[4589u]).Select(action=>action.Id).SequenceEqual([280u]),"not advised");
            Check(GpCrystals.For(16,85,100,[4589u]).Count==0,"nothing payable");
            Check(GpCrystals.WithRecast(16).SequenceEqual([4589u]) && GpCrystals.WithRecast(17).SequenceEqual([4590u]) && !GpCrystals.WithRecast(18).Any(),"the sheet's recast");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("\"GatheringPanel.CrystalWait\"") && panel.Contains("Where(row=>row.Left>0)"),"said; an unknown recast is not a wait");
            Check(Read("Gathering/CollectableActionState.Dalamud.cs").Contains("MemberFunctionPointers.GetRecastTimeElapsed==0"),"guarded");
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
