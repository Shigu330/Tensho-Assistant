using TenshoAssistant.Craft;

// 0.589 (C5): a sequence followed during a synthesis.
internal static class CraftFollow589Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly RecipeDefinition Recipe=new(123,456,1,"Menuiserie",100,[],0,new(80,1000,1000,100,100,100,100,0,0,1000));
    private static readonly CraftStats Stats=new(100,2000,2000,500,true);
    private static CraftSimulationResult Plan(params CraftActionKind[] actions)=>CraftSimulation.Run(Recipe,Stats,0,actions);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Follow: the first action, then each step against the plan",() =>
        {
            var plan=Plan(CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis);
            var start=CraftFollow.Compare(plan,80,1,0,0,80,500,true);
            Check(start is {Position:0,Total:3,Next:CraftActionKind.BasicTouch,Diverged:false},"starts on the first action");
            var after=plan.Steps[0].After;
            var second=CraftFollow.Compare(plan,80,2,after.Progress,after.Quality,after.Durability,after.Cp,true);
            Check(second is {Position:1,Next:CraftActionKind.BasicSynthesis,Diverged:false},"the next one, on plan");
            Check(second.Expected==after,"the plan's numbers at that point");
        }),
        ("Follow: numbers other than the plan's are a divergence, the next action stays the plan's",() =>
        {
            var plan=Plan(CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis);
            var after=plan.Steps[0].After;
            var view=CraftFollow.Compare(plan,80,2,after.Progress,after.Quality+50,after.Durability,after.Cp,true);
            Check(view is {Position:1,Next:CraftActionKind.BasicSynthesis,Diverged:true},"quality differs");
            Check(CraftFollow.Compare(plan,80,2,after.Progress,after.Quality,after.Durability,after.Cp-1,true).Diverged,"CP differs");
            Check(CraftFollow.Compare(plan,80,2,after.Progress,after.Quality,after.Durability-5,after.Cp,true).Diverged,"durability differs");
        }),
        ("Follow: an unknown starting quality is not compared",() =>
        {
            var plan=Plan(CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis);
            var after=plan.Steps[0].After;
            var view=CraftFollow.Compare(plan,80,2,after.Progress,after.Quality+300,after.Durability,after.Cp,false);
            Check(!view.Diverged && !view.QualityCompared,"only the other numbers count");
        }),
        ("Follow: an action that takes no turn shares its step with the next one",() =>
        {
            var plan=Plan(CraftActionKind.BasicSynthesis,CraftActionKind.FinalAppraisal,CraftActionKind.BasicTouch);
            var first=plan.Steps[0].After;var appraisal=plan.Steps[1].After;
            Check(first.Step==appraisal.Step && first.Cp!=appraisal.Cp,"the premise: same step, CP spent");
            Check(CraftFollow.Compare(plan,80,2,first.Progress,first.Quality,first.Durability,first.Cp,true) is {Position:1,Next:CraftActionKind.FinalAppraisal,Diverged:false},"before it");
            Check(CraftFollow.Compare(plan,80,2,appraisal.Progress,appraisal.Quality,appraisal.Durability,appraisal.Cp,true) is {Position:2,Next:CraftActionKind.BasicTouch,Diverged:false},"after it");
        }),
        ("Follow: the end of the plan, and a game gone past it",() =>
        {
            var plan=Plan(CraftActionKind.BasicTouch);
            var final=plan.Final;
            Check(CraftFollow.Compare(plan,80,2,final.Progress,final.Quality,final.Durability,final.Cp,true) is {Position:1,Next:null,Diverged:false},"finished");
            Check(CraftFollow.Compare(plan,80,9,final.Progress,final.Quality,final.Durability,final.Cp,true) is {Position:1,Next:null,Diverged:true},"past it");
        }),
        ("Follow: the tracker keeps the start and the states already played, a new synthesis clears them",() =>
        {
            var tracker=new CraftFollowTracker();
            tracker.Observe(5,1,120,true,"Normal");
            tracker.Observe(5,2,120,false,"Good");
            Check(tracker.StartingQuality==120,"quality at step 1");
            Check(tracker.Before(2).Count==0,"the state of the current step has changed nothing yet");
            tracker.Observe(5,3,300,true,"Normal");
            Check(tracker.StartingQuality==120 && tracker.Before(3) is [(2,"Good")],"kept once played on");
            tracker.Observe(5,2,10,true,"Normal");
            Check(tracker.StartingQuality is null && tracker.Before(9).Count==0,"a step going back is another synthesis, whose start was not seen");
            tracker.Observe(6,1,40,true,"Normal");
            Check(tracker.StartingQuality==40,"another recipe");
        }),
        ("Follow: read only, off with its setting, reset with the settings",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftFollow.cs"));
            Check(!view.Contains("UseAction") && !view.Contains("ExecuteCommand"),"nothing played");
            Check(view.Contains("configuration.CraftLiveFollow"),"its setting");
            Check(new TenshoAssistant.Configuration().CraftLiveFollow,"on by default");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftProjection.cs")).Contains("DrawCraftFollow(frame,catalog);"),"above the comparison");
        }),
    ];
}
