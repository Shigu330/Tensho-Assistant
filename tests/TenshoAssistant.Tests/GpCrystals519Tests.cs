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
