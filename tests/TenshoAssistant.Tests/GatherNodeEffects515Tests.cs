using TenshoAssistant.Gathering;

// 0.515 (user request): the GP plan is for the node in front of the player; the node's effects are read from the game and named.
internal static class GatherNodeEffects515Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Unlocks (0.517): a gathering action or trait without an unlock link comes with the level; Dalamud answers false for those",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("(row.UnlockLink.RowId==0 || unlocks.IsActionUnlocked(row))"),"actions: 22 of 24 have no link");
            Check(plugin.Contains("(row.Quest.RowId==0 ? player.GetClassJobLevel(row.ClassJob.Value)>=row.Level:unlocks.IsTraitUnlocked(row))"),"traits");
            Check(!plugin.Contains("is { } row && unlocks.IsActionUnlocked(row);"),"the old form is gone");
        }),
        ("GP plan (0.517): the node of the report gets a paid plan; without Bountiful Yield II the first one is used",() =>
        {
            var catalog=new GpActionCatalog(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,"A"+spec.Id,1,"D")),"test");
            var node=new GpScenario(16,85,809,4,1,54,1,1,MoreActions:true);
            var plan=GpOptimizer.Solve(node,catalog);
            Check(plan.Best.Cost is >0 and <=809 && plan.Best.ExpectedYield>plan.Baseline,$"809 GP, 54 %: {plan.Best.ExpectedYield} for {plan.Best.Cost}, without GP {plan.Baseline}");
            var noSecond=GpOptimizer.Solve(node with {Forbidden=GpActionRules.Key(GpActionCatalog.Specs.Single(spec=>spec.Id==272))},catalog);
            var onlyBounty=GpActionRules.Joined(GpActionCatalog.Specs.Where(spec=>spec.JobId==16 && spec.Effect!=GpEffect.Bountiful1).Select(GpActionRules.Key));
            var first=GpOptimizer.Solve(node with {Forbidden=onlyBounty},catalog);
            Check(first.Best.Actions.Single().Action.Spec.Id==4073 && first.Best.Actions.Single().Count==4,"Bountiful Yield, every attempt");
            Check(noSecond.Best.ExpectedYield>noSecond.Baseline,"still a plan");
        }),
        ("Node effects: every row with words is kept, worded by the game; the plan knows the four it can estimate",() =>
        {
            var attempts=GatherNodeEffects.From(19,770,"Max GP ≥ ",18,1," Gathering Attempts/Integrity +")!;
            Check(attempts==new GatherNodeEffect(GatherEffectStat.MaxGp,770,GatherEffectKind.Attempts,1,"Max GP ≥ 770","Gathering Attempts/Integrity +1"),"row 208: "+attempts);
            Check(GatherNodeEffects.From(14,34,"Gathering ≥ ",17,2," Gathering Yield +～")! is {Stat:GatherEffectStat.Gathering,Kind:GatherEffectKind.Yield,Text:"Gathering Yield +2～"},"the game's « and more » stays");
            Check(GatherNodeEffects.From(15,42,"Perception ≥ ",22,30," Gatherer's Boon Chance +%")! is {Stat:GatherEffectStat.Perception,Kind:GatherEffectKind.Boon,Text:"Gatherer's Boon Chance +30%"},"boon");
            Check(GatherNodeEffects.From(1,2,"Chain ≥ #",1,10," Gathering Rate +%")! is {Stat:GatherEffectStat.Other,Kind:GatherEffectKind.Rate,Condition:"Chain ≥ 2"},"a condition Tensho cannot read is still shown");
            Check(GatherNodeEffects.From(14,2000,"Gathering ≥ ",26,50," Collectability +～")! is {Kind:GatherEffectKind.Other,Text:"Collectability +50～"},"a collectable effect too");
            Check(GatherNodeEffects.From(14,42,"Gathering ≥ ",4,10," High-level Item Gathering Rate +%")!.Kind==GatherEffectKind.Other,"which items are « high level » is not known");
            Check(GatherNodeEffects.From(0,0,"",0,0,"") is null && GatherNodeEffects.From(20,29642," ",3,0,"") is null && GatherNodeEffects.From(20,30301," ",21,0," ") is null,"rows without words: the game does not show them");
        }),
        ("Node effects: on, how much is missing, and an unknown stat is never zero",() =>
        {
            var effect=GatherNodeEffects.From(19,770,"Max GP ≥ ",18,1," Attempts +")!;
            Check(GatherNodeEffects.Missing(effect,809,0,0)==0,"on at 809 max GP, GP spent or not");
            Check(GatherNodeEffects.Missing(effect,700,3000,3000)==70,"70 short");
            Check(GatherNodeEffects.Missing(effect,0,3000,3000) is null,"unknown");
            Check(GatherNodeEffects.Missing(GatherNodeEffects.From(15,100,"Perception ≥ ",22,30," Boon +%")!,800,50,0) is null,"another stat unknown");
            Check(GatherNodeEffects.Missing(GatherNodeEffects.From(1,2,"Chain ≥ #",1,10," Rate +%")!,800,50,50) is null,"a condition Tensho cannot read");
        }),
        ("Node effects: the estimate stays inside the optimizer's limits; what it cannot estimate has none",() =>
        {
            var node=new GpScenario(16,85,809,12,20,77,90,1);
            GatherNodeEffect Of(uint type,int value)=>GatherNodeEffects.From(19,1,"Max GP ≥ ",type,value," +")!;
            Check(GatherNodeEffects.With(node,Of(18,5))!.Attempts==12,"attempts");
            Check(GatherNodeEffects.With(node,Of(14,5))!.BaseYield==20,"yield");
            Check(GatherNodeEffects.With(node,Of(22,30))!.BoonPercent==100,"boon");
            Check(GatherNodeEffects.With(node,Of(1,30))!.SuccessPercent==100,"rate");
            Check(GatherNodeEffects.With(node,Of(26,50)) is null,"a collectable effect");
        }),
        ("Gathering window: « -% » is no extra chance, not an unread one, so a crystal has a plan",() =>
        {
            GatheringRawSlot[] Slots(string boon)=>Enumerable.Range(0,8).Select(index=>new GatheringRawSlot(index,index==0 ? 5u:0u,0,index==0,"100 %",boon,"1",index==0 ? 5u:0u,index==0)).ToArray();
            var frame=new GatheringObservedFrame(16,85,809,809,1,"5","5",Slots("-%"),33926);
            Check(frame.Slots[0].BoonPercent==0 && frame.NodeId==33926 && frame.Attempts==5,"read");
            Check(new GatheringObservedFrame(16,85,809,809,1,"5","5",Slots("-")).Slots[0].BoonPercent==0 && new GatheringObservedFrame(16,85,809,809,1,"5","5",Slots("- %")).Slots[0].BoonPercent==0,"with or without its percent sign");
            Check(new GatheringObservedFrame(16,85,809,809,1,"5","5",Slots("?")).Slots[0].BoonPercent is null,"anything else stays unknown");
            Check(new GatheringObservedFrame(16,85,809,809,1,"5","5",Slots("4 %")).Slots[0].BoonPercent==4,"a number");
        }),
        ("Gathering panel: one node only, no series of nodes; the list only says what it needs; every effect is drawn",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("var shown=plan.Best;") && panel.Contains("Math.Clamp(first.Quantity ?? 1,1,20)"),"the best plan for this node");
            foreach(var gone in new[]{"GpRoutes","gatherNodeNorms","GpSaveForBonus","GatherSecondsBetweenNodes"})Check(!panel.Contains(gone),"gone: "+gone);
            Check(panel.Contains("\"GatheringPanel.EffectOnAttempts\"") && panel.Contains("\"GatheringPanel.EffectMissing\"") && panel.Contains("\"GatheringPanel.EffectUnknown\"") && panel.Contains("DrawGatheringNodeEffects(frame,rating);"),"named, whichever item is chosen");
            Check(panel.Contains("\"GatheringPanel.ListNeedNodes\"") && panel.Contains("GatherListNeed(slot.ItemId)") && !panel.Contains("gatherNeeds"),"the list says what it needs");
            Check(Read("Gathering/GatheringObservationReader.Dalamud.cs").Contains("ObjectKind.GatheringPoint ? target.BaseId:0"),"the targeted node only");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.ConfigureNodeEffects(") && plugin.Contains("catch(Exception error){log.Warning(error,\"Node effects unavailable"),"guarded, once per node");
        }),
    ];
}
