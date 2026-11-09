using TenshoAssistant.Caching;
using TenshoAssistant.Craft;

// 0.468 (user requests, in-game campaign): Raphael's sequences kept, target and state in the crafting log panel, a saved sequence that makes the search needless, the projection window's setting.
internal static class Craft468Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static void Wait(Task task)=>task.GetAwaiter().GetResult();
    private static RecipeDefinition Recipe()=>new(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,1000));
    private static readonly CraftStats Stats=new(100,1000,1000,500);
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    private static CraftActionKind[] Sequence=>[CraftActionKind.Reflect,CraftActionKind.BasicSynthesis];
    private static CraftSolveResult Solved(RecipeDefinition recipe,CraftStats stats,int starting,CraftSolveOptions options,CancellationToken token)
    {
        var simulation=CraftSimulation.Run(recipe,stats,starting,Sequence,token);
        return new(simulation,options.TargetQuality,simulation.Final.Quality>=options.TargetQuality,0,simulation.Steps.Count,false,false,false);
    }
    private static void InFolder(Action<string,DataCache> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"Tensho-Craft468-"+Guid.NewGuid().ToString("N"));
        var cache=new DataCache(root);
        try{run(root,cache);}
        finally{Wait(cache.FlushAsync());cache.Dispose();if(Directory.Exists(root))Directory.Delete(root,true);}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raphael's sequence is kept: found again by a new session, computed again for another target or on request",() => InFolder((_,store) =>
        {
            var calls=0;
            CraftSolveResult Counted(RecipeDefinition recipe,CraftStats stats,int starting,CraftSolveOptions options,CancellationToken token){calls++;return Solved(recipe,stats,starting,options,token);}
            var options=new CraftSolveOptions(100,Raphael:true);
            var first=new RaphaelSequenceCache(Counted);first.Configure(()=>TimeSpan.FromDays(1),store);
            Check(!first.Solve(Recipe(),Stats,0,options,default).FromCache && calls==1,"first search computes");
            Wait(store.FlushAsync());
            var reopened=new RaphaelSequenceCache(Counted);reopened.Configure(()=>TimeSpan.FromDays(1),store);
            var kept=reopened.Solve(Recipe(),Stats,0,options,default);
            Check(kept.FromCache && calls==1 && kept.Simulation!.Steps.Select(step=>step.Action).SequenceEqual(Sequence),"same input: replayed, not computed");
            Check(reopened.Solve(Recipe(),Stats,0,options with {MaxSteps=7,ExtraWait=2},default).FromCache && calls==1,"limits and macro options are not part of Raphael's input");
            reopened.Solve(Recipe(),Stats,0,options with {TargetQuality=50},default);Check(calls==2,"another target computes");
            reopened.Solve(Recipe(),Stats with {Cp=400},0,options,default);Check(calls==3,"other attributes compute");
            reopened.Solve(Recipe(),Stats,0,options with {Forbidden="Veneration"},default);Check(calls==4,"other forbidden actions compute");
            Check(!reopened.Solve(Recipe(),Stats,0,options with {Fresh=true},default).FromCache && calls==5,"Recalculate computes");
        })),
        ("Raphael's kept sequence: expired, or no longer valid in the simulator, is computed again",() => InFolder((root,store) =>
        {
            var calls=0;
            CraftSolveResult Counted(RecipeDefinition recipe,CraftStats stats,int starting,CraftSolveOptions options,CancellationToken token){calls++;return Solved(recipe,stats,starting,options,token);}
            var options=new CraftSolveOptions(100,Raphael:true);
            var cache=new RaphaelSequenceCache(Counted);cache.Configure(()=>TimeSpan.FromDays(1),store);
            cache.Solve(Recipe(),Stats,0,options,default);Wait(store.FlushAsync());
            var path=Directory.GetFiles(root,"*.json").Single();
            var value=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;value["Value"]=new System.Text.Json.Nodes.JsonArray();File.WriteAllText(path,value.ToJsonString());
            Check(!cache.Solve(Recipe(),Stats,0,options,default).FromCache && calls==2,"empty sequence refused");
            Wait(store.FlushAsync());
            cache.Configure(()=>TimeSpan.Zero,store);
            cache.Solve(Recipe(),Stats,0,options,default);Check(calls==3,"no retention: computed");
        })),
        ("Crafting log: Raphael's sequence survives another macro option, and the panel knows it",() => InFolder((_,store) =>
        {
            var calls=0;
            CraftSolveResult Counted(RecipeDefinition recipe,CraftStats stats,int starting,CraftSolveOptions options,CancellationToken token){calls++;return Solved(recipe,stats,starting,options,token);}
            var sequences=new RaphaelSequenceCache(Counted);sequences.Configure(()=>TimeSpan.FromDays(1),store);
            var input=new CraftLogInput(1,Recipe(),Stats,Catalog(),[],new(100,4,32,5000,Raphael:true),0,true);
            using var session=new CraftLogSession((value,token)=>CraftLogSession.Build(value,token,sequences.Solve));session.ConfigureCache(()=>TimeSpan.FromDays(1),store);
            Wait(session.StartAsync(input));Wait(store.FlushAsync());
            Check(calls==1 && session.State.Result is {UsedRaphael:true,RaphaelCached:false},"computed");
            Wait(session.StartAsync(input));Check(calls==1 && session.State.FromCache,"same input: the panel's result comes back from the disk");
            Wait(session.StartAsync(input with {ExtraWait=2}));
            Check(calls==1 && session.State.Result is {UsedRaphael:true,RaphaelCached:true} && !session.State.FromCache,"another macro option: new macro, same sequence");
            Wait(session.StartAsync(input,true));Check(calls==2 && session.State.Result is {RaphaelCached:false},"Recalculate runs Raphael again");
        })),
        ("Best saved sequence: reaching the target, nothing is computed; below it, or on request, the search runs",() =>
        {
            var recipe=Recipe();var saved=CraftPresets.Create(recipe,"Best",Sequence);var quality=CraftSimulation.Run(recipe,Stats,0,Sequence).Final.Quality;
            var calls=0;
            CraftSolveResult Counted(RecipeDefinition value,CraftStats stats,int starting,CraftSolveOptions options,CancellationToken token){calls++;return Solved(value,stats,starting,options,token);}
            CraftLogInput Input(int target,bool fresh=false)=>new(1,recipe,Stats,Catalog(),[saved],new(target,4,32,5000,Raphael:true,Fresh:fresh),0,true);
            var enough=CraftLogSession.Build(Input(quality),default,Counted);
            Check(enough is {SavedIsEnough:true,Suggested:null,SearchIssue:null,Saved.Name:"Best"} && calls==0,"saved reaches the target");
            var below=CraftLogSession.Build(Input(quality+1),default,Counted);Check(!below.SavedIsEnough && below.Suggested is not null && calls==1,"saved below the target");
            var forced=CraftLogSession.Build(Input(quality,true),default,Counted);Check(!forced.SavedIsEnough && forced.Suggested is not null && forced.Saved is not null && calls==2,"Recalculate");
            using var session=new CraftLogSession();
            Wait(session.StartAsync(Input(quality)));Check(session.State.Result?.SavedIsEnough==true,"through the session");
        }),
        ("Saved sequence names stay unique for a recipe",() =>
        {
            var recipe=Recipe();
            Check(CraftPresets.UniqueName([],recipe.RecipeId,"Best")=="Best","free");
            IReadOnlyList<CraftPreset> saved=[CraftPresets.Create(recipe,"best",Sequence),CraftPresets.Create(recipe,"Best (2)",Sequence)];
            var name=CraftPresets.UniqueName(saved,recipe.RecipeId,"Best");Check(name=="Best (3)","next number");
            Check(CraftPresets.Add(saved,CraftPresets.Create(recipe,name,Sequence)).Count==3,"accepted by Add");
            Check(CraftPresets.UniqueName(saved,recipe.RecipeId+1,"Best")=="Best","other recipe");
        }),
        ("Crafting log panel: target kept by recipe, state shown, specialist from the game, sequence saved",() =>
        {
            var ui=Read("MainWindow.CraftLog.cs");
            Check(ui.Contains("new(CraftLogTarget(recipe),craftSolveSteps") && ui.Contains("IsItemDeactivatedAfterEdit") && ui.Contains("Math.Clamp(target,0,maximum)"),"target");
            Check(Read("Configuration.cs").Contains("public Dictionary<uint,int>? CraftLogTargets") && Read("Configuration.cs").Contains("\"CraftLogTargets\""),"target kept");
            Check(ui.Contains("DrawCraftLogState(job,manipulationDetected,current.ManipulationAllowed)") && ui.Contains("craftLogDetected?.Specialist==true") && ui.Contains("craftLogManipulation[job]=manipulation"),"state");
            Check(ui.Contains("if(!result.SavedIsEnough)DrawCraftLogMacro(") && ui.Contains("CraftPresets.Commit(configuration,save,"),"saved");
            Check(Read("Craft/CraftLogObservation.cs").Contains("new(stats.Level,cs,control,cp,manipulation,specialist)"),"specialist passed to the calculation");
        }),
        ("Projection window: its setting is also under Crafting, and its cross turns it off",() =>
        {
            Check(Read("MainWindow.Settings.cs").Contains("DrawCraftLiveSettings();") && Read("MainWindow.CraftLive.cs").Contains("BoolSetting(\"CraftLiveUi.Panel\",\"CraftLiveUi.PanelTip\",()=>configuration.CraftLivePanel"),"setting");
            var panel=Read("MainWindow.CraftProjection.cs");
            Check(panel.Contains("\"TenshoCraftLivePanel\"") && panel.Contains("()=>{configuration.CraftLivePanel=false;craftLive.SetEnabled(CraftLiveWanted(configuration));SaveReminders();}"),"cross");
        }),
    ];
}
