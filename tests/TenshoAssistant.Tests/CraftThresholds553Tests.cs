using TenshoAssistant.Craft;

// 0.553 (C4): the lowest attributes of a finished sequence.
internal static class CraftThresholds553Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Other job (0.554): the attributes last read, marked; none or too low: wear the job",() =>
        {
            var recipe=new RecipeDefinition(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,1000));
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Craft/CraftLogObservation.cs")).Replace("\r\n","\n");
            Check(source.Split("return WrongJob();").Length==3,"both wrong-job answers");
            Check(source.Contains("RememberedAt=seen.SeenAt") && source.Contains("seen.Craftsmanship<p.RequiredCraftsmanship"),"marked, and under the recipe's minimum it is refused");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftLog.cs")).Replace("\r\n","\n");
            Check(view.Contains("known.Level==state.Level && known.Craftsmanship==craftsmanship") && view.Contains("CraftLogUi.Remembered"),"saved only when changed; the panel says it");
            Check(new TenshoAssistant.Configuration().LastCraftStats is null,"nothing until read");
            _=recipe;
        }),
        ("Sequence thresholds: the lowest attributes still finish at the same quality, one less does not",() =>
        {
            var recipe=new RecipeDefinition(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,1000));
            var stats=new CraftStats(100,3000,3000,500);
            var actions=new[]{CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis};
            var done=CraftSimulation.Run(recipe,stats,0,actions);
            Check(done.Final.Finish==CraftFinish.Completed,"the sample finishes");
            var found=CraftStatThresholds.Find(recipe,stats,0,actions);
            Check(found is not null,"found");
            var at=CraftSimulation.Run(recipe,stats with {Craftsmanship=found!.Craftsmanship},0,actions);
            Check(at.Final.Finish==CraftFinish.Completed && at.Final.Quality>=done.Final.Quality,"holds at the Craftsmanship found");
            var withControl=CraftSimulation.Run(recipe,stats with {Control=found.Control},0,actions);
            Check(withControl.Final.Quality>=done.Final.Quality,"holds at the Control found");
            if(found.Craftsmanship>recipe.Simulation!.RequiredCraftsmanship)
                Check(CraftSimulation.Run(recipe,stats with {Craftsmanship=found.Craftsmanship-1},0,actions).Final.Finish!=CraftFinish.Completed,"one less does not");
            Check(found.Cp==stats.Cp-done.Final.Cp && found.CpMargin==done.Final.Cp,"CP is what the sequence spent");
            Check(found.CraftsmanshipMargin==stats.Craftsmanship-found.Craftsmanship,"margin");
            Check(CraftStatThresholds.Find(recipe,stats,0,[CraftActionKind.BasicTouch]) is null,"an unfinished sequence has none");
        }),
    ];
}
