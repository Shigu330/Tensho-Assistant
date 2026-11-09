using TenshoAssistant.Gathering;
using TenshoAssistant.Craft;
using TenshoAssistant.UI;

// 0.728 (user request): the next action of a node's plan. The plan of the report: 2026-11-07, 823 GP, four attempts.
internal static class GatherNext728Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static GpAction Act(uint id,int cost,GpEffect effect)=>new(new(id,17,50,cost,effect,0),"a"+id,0,"");
    private static readonly GpAction Gift1=Act(21178,50,GpEffect.BoonChance1),Gift2=Act(25590,100,GpEffect.BoonChance2),Tidings=Act(21204,200,GpEffect.BoonYield),Bountiful=Act(273,100,GpEffect.Bountiful2);
    private static readonly GpPlan Plan=new(750,24m,[new(Gift1,1),new(Gift2,1),new(Tidings,1),new(Bountiful,4)]);
    private static GpNextStep Next(int gp,int attempts,GpPlan? plan=null)=>GpNextAction.Next(plan ?? Plan,823,gp,4,attempts);

    private static string ReadLf(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static CollectableRule Rule(uint id,string name,int level,int high,int reward,string group="G")=>new(id,name,level,level+1,high/2,high*3/4,high,1,2,reward,true,group);
    private static GpOptimization Node(int budget,string forbidden="",int attempts=4,int success=100)=>GpOptimizer.Solve(new(16,90,budget,attempts,1,success,0,1,MoreActions:true,Forbidden:forbidden),Catalog());
    private static string[] Raw(int current,int scour,int low,int high,int meticulous,bool scrutinyBox,bool intuitionBox=false)
    {
        var raw=Enumerable.Range(0,59).Select(index=>$"{index}:uint 0").ToArray();
        raw[2]="2:uint 36291";raw[13]=$"13:uint {current}";raw[14]="14:uint 1000";
        raw[26]="26:uint 22182";raw[29]="29:uint 22183";raw[32]="32:uint 22184";raw[35]="35:uint 240";raw[38]="38:uint 22185";
        raw[48]=$"48:uint {scour}";raw[49]=$"49:uint {low}";raw[50]=$"50:uint {high}";raw[51]=$"51:uint {meticulous}";raw[52]="52:text 8 %";raw[54]=$"54:bool {(scrutinyBox ? "True":"False")}";raw[56]=$"56:bool {(intuitionBox ? "True":"False")}";raw[41]="41:uint 21205";
        return raw;
    }
    private static GpActionCatalog Catalog()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action {spec.Id}",0,"Text")),"fixture");
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
            // 0.733 (user report): Ageless Words asked at 4/4 attempts, when the game does not allow it.
            Check(GpNextAction.Next(more,800,800,4,4).Action==Bountiful,"no attempt spent: the bonus first");
            Check(GpNextAction.Next(more,800,700,4,4).Kind==GpNextKind.Gather,"then gather");
            Check(GpNextAction.Next(more,800,706,4,3).Action==attempt,"after the first gather, the attempt back");
            Check(GpNextAction.Next(new(300,9m,[new(attempt,1)]),800,800,4,4).Kind==GpNextKind.Gather,"alone: gather first");
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
            Check(locator.Contains("saved.ApparentActionId==wanted") && locator.Contains("GetAdjustedActionId(saved.CommandId)==wanted"),"0.734: an action the game shows at its next rank");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("configuration.GatheringNextHighlight && BarLocator is { } located") && panel.Contains("located.Want(action.Spec.Id)") && panel.Contains("DrawGatheringBarHighlight(located)"),"setting, asked and drawn inside the panel");
            Check(!panel.Contains("located.Wanted=0"),"0.733: nothing clears the request after the panel asked");
            Check(!Read("Gathering/GpNextAction.cs").Contains("Dalamud"),"pure");
            Check(panel.IndexOf("DrawGatheringNext(shown")<panel.IndexOf("if(next is { Kind:not GpNextKind.Done })ImGui.Separator();"),"a line between the next action and the plan");
        }),
        ("The help follows what is unlocked: an action the window does not offer or the job has not learned is never advised",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            string[] Flags(params int[] closed){var raw=Raw(0,177,88,265,132,false);foreach(var index in closed)raw[index]=$"{index}:bool False";return raw;}
            var full=new CollectableBonuses(175,88,809,200,100,false,true,0,4,13);
            Check(CollectableTurn.Parse(Flags())! is {ScrutinyOpen:true,FocusOpen:true,BrazenOpen:true},"nothing closed");
            var noScrutiny=CollectableTurn.Parse(Flags(40))!;
            Check(noScrutiny.ScrutinyOpen==false && CollectableAdvisor.Advise(noScrutiny,4,ore,full) is {Scrutinies:0} and {Target:CollectableTier.Low},"Scrutiny not offered: not counted, the plan is what is left");
            Check(CollectableAdvisor.Advise(noScrutiny,4,ore,full)!.Move!=CollectableMove.Scrutiny,"nor advised");
            var noFocus=CollectableTurn.Parse(Flags(40,43))! with {IntuitionChance=40};
            Check(CollectableAdvisor.Advise(noFocus,3,ore,full)!.Move!=CollectableMove.Focus,"Collector's Focus below its level: not advised");
            var noBrazen=CollectableTurn.Parse(Flags(31))!;
            Check(CollectableAdvisor.Advise(noBrazen,4,ore)!.Move!=CollectableMove.Brazen,"nor a roll the window does not offer");
            var locked=GpActionRules.Joined(GpActionCatalog.Specs.Where(spec=>spec.JobId==16 && spec.Effect==GpEffect.Attempt).Select(GpActionRules.Key));
            Check(Node(1000,locked).Frontier.All(plan=>plan.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Attempt)),"an action not learned leaves the GP plan");
            var list=ReadLf("MainWindow.GatheringPlanSettings.cs");
            Check(list.Contains("!unlocked(spec.Id)") && ReadLf("MainWindow.GatheringPanel.cs").Contains("Forbidden:GpForbiddenFor(start.JobId)") && ReadLf("MainWindow.GatheringGp.cs").Contains("GpForbiddenFor(gpJob)") && ReadLf("MainWindow.CollectablePanel.cs").Contains("GpForbiddenFor(job)"),"used by the panel, the GP page and the collectable plan");
        }),
        ("Gathering panel: readable at its width; a collectable says where its GP goes; the regeneration is measured, or said to be unknown",() =>
        {
            var panel=ReadLf("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("\"GatheringPanel.CollectablePlan\"") && panel.Contains("Collectables?.Catalog?.For(slot.ItemId) is {Count:>0}"),"a collectable has no GP plan here, and says why");
            Check(panel.Contains("ImGui.PushTextWrapPos(0);") && panel.Contains("BeginGroup"),"texts wrap; name and rates on two lines");
            foreach(var language in new[]{"en","fr"})
            {
                var texts=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")))!["CollectablePanel"]!;
                Check(!texts["Unknown"]!.GetValue<string>().Contains("confirm",StringComparison.OrdinalIgnoreCase),"the player is not asked to confirm anything: "+language);
            }
        }),
        ("0.560: the gathering help says what the last chain still needs",() =>
        {
            TenshoAssistant.Inventory.ItemHoldingsSummary Stock(long total)=>new([],total,0,true,false,false,true);
            var result=new TenshoAssistant.Craft.CraftChainResult(1,1,[],[new(5,14,TenshoAssistant.Craft.CraftChainReason.RawMaterial)],new Dictionary<uint,TenshoAssistant.Inventory.ItemHoldingsSummary>{[5]=Stock(10)});
            Check(result.Remaining(5,10)==14 && result.Remaining(5,13)==11,"falls as you gather");
            Check(result.Remaining(5,99)==0 && result.Remaining(5,3)==14,"bounded");
            Check(result.Remaining(6,10) is null,"not needed");
        }),
    ];
}
