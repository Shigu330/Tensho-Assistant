using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;

// 0.419: A-01 collectable thresholds as quality targets, X-11 Manipulation when learned, X-12 buffs' extra wait apart.
internal static class Craft419Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("A-01: a collectability threshold as the quality to reach, capped at the recipe's maximum",() =>
        {
            Check(Collectables.QualityFor(110,5000)==1100 && Collectables.QualityFor(720,6000)==6000 && Collectables.QualityFor(800,6000)==6000,"quality = collectability × 10, capped");
            Check(Collectables.QualityFor(-1,100)==0 && Collectables.QualityFor(5,-3)==0,"bounds");
            Check(Read("MainWindow.CraftSolver.cs").Contains("Collectables?.Catalog?.For(simulationRecipe!.ResultItemId)"),"solver buttons from the game's thresholds");
        }),
        ("X-11: the crafting log assistant allows Manipulation only when the job has learned it",() =>
        {
            Check(Read("Craft/CraftLogObservation.cs").Contains("return new(recipe,new(stats.Level,cs,control,cp,manipulation,specialist),null,starting);"),"passed through");
            Check(Read("MainWindow.CraftLog.cs").Contains("ManipulationLearned?.Invoke(job)==true"),"unknown = not learned");
            Check(Read("Plugin.cs").Contains("unlocks.IsActionUnlocked(action)"),"game unlock state");
        }),
        ("X-12: buffs wait their own extra time; unset, as before",() =>
        {
            Check(CraftMacroExporter.Wait(CraftActionKind.Innovation,1,null)==3 && CraftMacroExporter.Wait(CraftActionKind.Byregot,1,null)==4,"unset: same extra for all");
            Check(CraftMacroExporter.Wait(CraftActionKind.Innovation,1,0)==2 && CraftMacroExporter.Wait(CraftActionKind.Manipulation,0,2)==4
                && CraftMacroExporter.Wait(CraftActionKind.Byregot,1,3)==4,"buffs apart, other actions unchanged");
            Check(Read("Configuration.cs").Contains("public int? BoundedBuffExtraWait => BuffExtraWait is { } wait ? Math.Clamp(wait, 0, 3) : null;"),"setting bounded, unset = as before");
        }),
    ];
}
