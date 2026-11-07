using TenshoAssistant.Gathering;

// 0.728 (user request): the next action of a node's plan. The plan of the report: 2026-11-07, 823 GP, four attempts.
internal static class GatherNext728Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static GpAction Act(uint id,int cost,GpEffect effect)=>new(new(id,17,50,cost,effect,0),"a"+id,0,"");
    private static readonly GpAction Gift1=Act(21178,50,GpEffect.BoonChance1),Gift2=Act(25590,100,GpEffect.BoonChance2),Tidings=Act(21204,200,GpEffect.BoonYield),Bountiful=Act(273,100,GpEffect.Bountiful2);
    private static readonly GpPlan Plan=new(750,24m,[new(Gift1,1),new(Gift2,1),new(Tidings,1),new(Bountiful,4)]);
    private static GpNextStep Next(int gp,int attempts,GpPlan? plan=null)=>GpNextAction.Next(plan ?? Plan,823,gp,4,attempts);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gather next: the plan of the report, step by step",() =>
        {
            Check(Next(823,4) is { Kind:GpNextKind.Action,Action:var a } && a==Gift1,"first");
            Check(Next(773,4).Action==Gift2,"second");
            Check(Next(673,4).Action==Tidings,"third");
            Check(Next(473,4).Action==Bountiful,"before the first gather");
            var ready=Next(373,4);
            Check(ready.Kind==GpNextKind.Gather && ready.Used[273]==1,"it lasts for one gather: gather now");
            Check(Next(373,3) is { Kind:GpNextKind.Action,Action:var again } && again==Bountiful,"after the gather, again");
            Check(Next(273,3).Kind==GpNextKind.Gather && Next(273,2).Action==Bountiful,"and so on");
            Check(Next(73,1).Kind==GpNextKind.Gather && Next(73,0).Kind==GpNextKind.Done,"the last gather, then nothing");
        }),
        ("Gather next: the screenshot of the report: 379 GP of 823, 3 attempts left, a little GP came back",() =>
        {
            var seen=Next(379,3);
            Check(seen.Kind==GpNextKind.Action && seen.Action==Bountiful && seen.Used[273]==1,"next use");
            Check(GpNextAction.Tolerance+GpNextAction.ReturnPerGather<50,"one gather back is never the cheapest action");
        }),
        ("Gather next (0.731, user report): six attempts, 823 GP, the plan done with 53 GP left and 90 % boon: Pioneer's Gift I for the last gather",() =>
        {
            var catalog=new GpActionCatalog(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,"a"+spec.Id,1,"d")).ToArray(),"test");
            var opened=new GpScenario(17,100,823,6,1,100,60,3,MoreActions:true);
            GpPlan plan=new(800,31.8m,[new(catalog.Actions.Single(action=>action.Spec.Id==25590),1),new(catalog.Actions.Single(action=>action.Spec.Id==21204),1),new(catalog.Actions.Single(action=>action.Spec.Id==273),5)]);
            var step=GpNextAction.Next(plan,823,53,6,1);
            Check(step is { Kind:GpNextKind.Gather,Finished:true } && step.Used[273]==5,"the plan is behind, 30 GP came back over five gathers");
            var last=GpNextAction.Leftover(step,opened,plan,catalog,53,1,100,90);
            Check(last is { Kind:GpNextKind.Action,Leftover:true } && last.Action!.Spec.Id==21178,"Pioneer's Gift I");
            Check(GpNextAction.Leftover(step,opened,plan,catalog,40,1,100,90).Kind==GpNextKind.Gather,"not enough GP");
            Check(GpNextAction.Leftover(step,opened,plan,catalog,53,0,100,90).Kind==GpNextKind.Gather,"no attempt left");
            var pending=GpNextAction.Next(plan,823,153,6,2);
            Check(!pending.Finished && GpNextAction.Leftover(pending,opened,plan,catalog,153,2,100,90)==pending,"a bonus still waiting for its gather: the plan first");
            Check(GpNextAction.Leftover(step,opened,plan,catalog,53,1,100,100).Kind==GpNextKind.Gather,"already 100 %");
        }),
        ("Gather next: another order is understood, a passed step is skipped, what does not add up is said",() =>
        {
            var other=Next(723,4);
            Check(other.Action==Gift1 && other.Used[25590]==1,"second first");
            Check(Next(473,3).Action==Bountiful,"gathered without the bonus: the next one");
            Check(Next(473,0).Kind==GpNextKind.Done,"over");
            Check(Next(763,4).Kind==GpNextKind.OffPlan,"60 GP is no sum of this plan, even with what came back");
            Check(Next(0,4).Kind==GpNextKind.OffPlan,"all spent elsewhere");
        }),
        ("Gather next: a keep plan says gather; an added attempt is counted; a per-gather action comes with each attempt",() =>
        {
            Check(Next(823,4,new(0,5m,[])).Kind==GpNextKind.Gather,"nothing to buy");
            var attempt=Act(215,300,GpEffect.Attempt);
            GpPlan more=new(400,9m,[new(attempt,1),new(Bountiful,1)]);
            Check(GpNextAction.Next(more,800,800,4,4).Action==attempt,"attempt first");
            Check(GpNextAction.Next(more,800,500,4,5).Action==Bountiful,"no gather counted when the attempt was added");
            var clear=Act(4086,50,GpEffect.NextSuccess);
            GpPlan both=new(250,9m,[new(Bountiful,2),new(clear,1)]);
            Check(GpNextAction.Steps(both).Select(step=>(step.Action.Spec.Id,step.Attempt)).SequenceEqual([(273u,1),(4086u,1),(273u,2)]),"attempt by attempt");
            Check(GpNextAction.Next(both,800,650,4,3).Action==Bountiful,"after the first gather");
        }),
        ("Gather next: read on the framework thread, drawn only, cut with the rest, and a setting",() =>
        {
            var locator=Read("Gathering/ActionBarLocator.Dalamud.cs");
            Check(locator.Contains("CompetitiveGate.Add(framework,OnUpdate)") && locator.Contains("CompetitiveGate.Remove(framework,OnUpdate)"),"owned and cut");
            Check(!locator.Contains("PulseActionBarSlot") && !locator.Contains("UseAction") && !locator.Contains("Hook"),"nothing called or hooked");
            Check(locator.Contains("HotbarSlotType.Action"),"only an action");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("configuration.GatheringNextHighlight") && panel.Contains("located.Wanted=gatheringBarWanted"),"setting");
            Check(!Read("Gathering/GpNextAction.cs").Contains("Dalamud"),"pure");
            Check(panel.IndexOf("DrawGatheringNext(shown")<panel.IndexOf("if(next is { Kind:not GpNextKind.Done })ImGui.Separator();"),"a line between the next action and the plan");
        }),
    ];
}
