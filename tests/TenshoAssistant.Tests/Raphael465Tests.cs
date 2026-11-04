using TenshoAssistant.Craft;

// A-03 (0.465, DEC-028): the optional Raphael solver: action numbers, pool, replay in Tensho's simulator, off by default.
internal static class Raphael465Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raphael actions: numbers of its enum both ways; actions it has no number for are left out",() =>
        {
            Check(RaphaelActions.Number(CraftActionKind.BasicSynthesis)==0 && RaphaelActions.Number(CraftActionKind.Byregot)==11 && RaphaelActions.Number(CraftActionKind.TrainedPerfection)==30,"numbers");
            Check(RaphaelActions.Kind(15)==CraftActionKind.Manipulation && RaphaelActions.Kind(4) is null && RaphaelActions.Kind(31) is null && RaphaelActions.Kind(99) is null,"back");
            Check(RaphaelActions.Number(CraftActionKind.FinalAppraisal) is null && RaphaelActions.Number(CraftActionKind.CarefulObservation) is null,"not in Raphael");
            foreach(var spec in CraftActionCatalog.Specs)
                if(RaphaelActions.Number(spec.Kind) is { } number)Check(RaphaelActions.Kind(number)==spec.Kind,"round trip: "+spec.Kind);
        }),
        ("Raphael pool: level, Manipulation, specialist, forbidden; nothing left to chance",() =>
        {
            var high=RaphaelActions.Pool(new CraftStats(100,4000,4000,600,true,true),"",90);
            Check(high.Contains(CraftActionKind.Manipulation) && high.Contains(CraftActionKind.QuickInnovation) && high.Contains(CraftActionKind.TrainedEye) && !high.Contains(CraftActionKind.HeartAndSoul),"all");
            Check(!RaphaelActions.Pool(new CraftStats(100,4000,4000,600,true,true),"",91).Contains(CraftActionKind.TrainedEye),"Trained Eye: ten levels above the recipe (in-game report, 0.466)");
            Check(!high.Any(CraftSimulation.Random) && !high.Contains(CraftActionKind.HastyTouch) && !high.Contains(CraftActionKind.RapidSynthesis) && !high.Contains(CraftActionKind.TricksOfTheTrade),"no chance, no condition");
            var plain=RaphaelActions.Pool(new CraftStats(100,4000,4000,600,false,false),"",90);
            Check(!plain.Contains(CraftActionKind.Manipulation) && !plain.Contains(CraftActionKind.QuickInnovation),"not learned, no specialist");
            var low=RaphaelActions.Pool(new CraftStats(10,100,100,100,true,false),"",1);
            Check(low.Contains(CraftActionKind.BasicSynthesis) && !low.Contains(CraftActionKind.Byregot),"level");
            Check(!RaphaelActions.Pool(new CraftStats(100,4000,4000,600,true,true),CraftActionRules.Key(["Veneration"]),90).Contains(CraftActionKind.Veneration),"forbidden");
        }),
        ("Raphael: unknown action or nothing refused before the simulator; off by default; on a worker; the setting exists in both places; package and credits",() =>
        {
            var recipe=TenshoAssistantTestRecipes.None;
            Check(RaphaelActions.Replay(recipe,new CraftStats(100,1,1,1),0,[]).Problem=="Raphael found no sequence.","nothing");
            Check(RaphaelActions.Replay(recipe,new CraftStats(100,1,1,1),0,[4]).Problem!.Contains("does not model"),"Tricks and others are refused");
            Check(!new TenshoAssistant.CraftSearchSettings().Raphael,"off by default");
            var solver=Read("Craft/RaphaelCraftSolver.Dalamud.cs");
            Check(solver.Contains("using(cancellation.Register(solver.Cancel))solver.Solve();") && solver.Contains("RaphaelActions.Replay(") && solver.Contains("if(result is null)throw"),"cancellable, replayed");
            var ui=Read("MainWindow.CraftSolver.cs");
            Check(ui.Contains("input.Options.Raphael") && ui.Contains("localization.Get(\"CraftSearch.SolverRaphael\")"),"session and page");
            Check(Read("MainWindow.Settings.cs").Contains("BoolSetting(\"CraftSearch.SettingRaphael\""),"settings");
            Check(Read("TenshoAssistant.csproj").Contains("<PackageReference Include=\"Raphael.Net\" Version=\"5.0.0\" />") && Read("Credits/CreditCatalog.cs").Contains("new(\"RaphaelRs\","),"package, credits");
        }),
    ];
}

internal static class TenshoAssistantTestRecipes
{
    public static readonly RecipeDefinition None=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RecipeDefinition)) is RecipeDefinition recipe ? recipe:throw new InvalidOperationException();
}
