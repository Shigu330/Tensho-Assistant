using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;

// 0.478 (user requests, in-game campaign, lot 6): collectables with a long name, their tab and sort; GP over the nodes of a crafting list; forbidden actions; the panel's layout.
internal static class CollectablesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    // 0.1202: independent plans worked out at the same time (each search is its own: no shared state), then checked one by one in
    // their order, so a failure says the same case as before. Nothing is taken from the tests: 17 s to a few for this file.
    internal static TResult[] Each<T,TResult>(IEnumerable<T> cases,Func<T,TResult> work)=>cases.AsParallel().AsOrdered().Select(work).ToArray();
    private static CollectableRule Rule(uint id,string name,int level,int high,int reward,string group="G")=>new(id,name,level,level+1,high/2,high*3/4,high,1,2,reward,true,group);
    private static GpActionCatalog Catalog()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action {spec.Id}",0,"Text")),"fixture");
    private static GpOptimization Node(int budget,string forbidden="",int attempts=4,int success=100)=>GpOptimizer.Solve(new(16,90,budget,attempts,1,success,0,1,MoreActions:true,Forbidden:forbidden),Catalog());

    // The window's values, as logged in game (docs/evidence/0.479-collectable-window-values.md).
    private static string[] Raw(int current,int scour,int low,int high,int meticulous,bool scrutinyBox,bool intuitionBox=false)
    {
        var raw=Enumerable.Range(0,59).Select(index=>$"{index}:uint 0").ToArray();
        raw[2]="2:uint 36291";raw[13]=$"13:uint {current}";raw[14]="14:uint 1000";
        raw[26]="26:uint 22182";raw[29]="29:uint 22183";raw[32]="32:uint 22184";raw[35]="35:uint 240";raw[38]="38:uint 22185";
        raw[48]=$"48:uint {scour}";raw[49]=$"49:uint {low}";raw[50]=$"50:uint {high}";raw[51]=$"51:uint {meticulous}";raw[52]="52:text 8 %";raw[54]=$"54:bool {(scrutinyBox ? "True":"False")}";raw[56]=$"56:bool {(intuitionBox ? "True":"False")}";raw[41]="41:uint 21205";
        return raw;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable risk (0.529): three steps; a saved percentage lands on one; each step gives its plan, and only « Safe » lists the plan without luck",() =>
        {
            Check(CollectableRisk.Caps.SequenceEqual([0,50,100]),"steps");
            Check(CollectableRisk.Snap(0)==0 && CollectableRisk.Snap(24)==0 && CollectableRisk.Snap(25)==50 && CollectableRisk.Snap(74)==50 && CollectableRisk.Snap(75)==100 && CollectableRisk.Snap(100)==100,"nearest");
            var herb=Rule(43924,"Herb",90,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            var raw=Raw(0,200,100,300,150,false);raw[52]="52:text 24 %";
            var turn=CollectableTurn.Parse(raw)! with {IntuitionChance=40};
            var gains=new CollectableBonuses(250,100,625,200,100,false,true,300,4,40,0,0.5m,false,100,24);
            var full=gains with {Gp=823};var capture=gains with {Gp=589};
            var third=gains with {Gp=423,ScrutinyActive=true,MeticulousBase=8};var thirdTurn=turn with {MeticulousChance=8};
            var steps=new[]{100,200,300,423,523,623,723,823}.SelectMany(gp=>new[]{false,true},(gp,on)=>(gp,on)).ToArray();
            var paths=(from risk in CollectableRisk.Caps from gp in new[]{0,300,423,625,823} select (risk,gp)).ToArray();
            // 0.1202: every plan of this test worked out at once (73 searches), then checked below in the same order as before.
            var named=new (CollectableTurn Turn,CollectableBonuses Bonuses)[]{(turn,gains),(turn,gains with {RiskTolerance=100}),(turn,gains with {RiskTolerance=50}),(turn,full),
                (turn,full with {RiskTolerance=50}),(turn,capture),(turn,capture with {RiskTolerance=50}),(turn,capture with {RiskTolerance=100}),(thirdTurn,third),(thirdTurn,third with {RiskTolerance=50})};
            var stepJobs=steps.SelectMany(step=>new[]{0,50,100}.Select(risk=>(turn,gains with {Gp=step.gp,ScrutinyActive=step.on,RiskTolerance=risk})));
            var pathJobs=paths.Select(path=>(turn,gains with {Gp=path.gp,RiskTolerance=path.risk}));
            var plans=Each(named.Concat(stepJobs).Concat(pathJobs),job=>CollectableAdvisor.Advise(job.Item1,4,herb,job.Item2)!);
            var safe=plans[0];var luck=plans[1];var middle=plans[2];
            Check(safe.Worst>=20 && safe.Expected>=safe.Worst,$"Safe: at least what is certain on the capture: {safe}");
            Check(luck.Expected>safe.Expected-0.001m && luck.Worst<=safe.Worst,$"Try your luck: a better average for a lower floor: {luck} against {safe}");
            Check(middle.Expected<=luck.Expected && middle.Worst>=luck.Worst,$"Uncertain, in between: {middle}");
            // 0.531: the capture at 823 GP: Safe had 65.9 on average and Balanced 70.2, both 40 at worst.
            var safeFull=plans[3];var middleFull=plans[4];
            Check(safeFull.Worst>=middleFull.Worst && safeFull.Expected>=middleFull.Expected-0.001m,$"Safe is not worse than Balanced on the same floor: {safeFull} against {middleFull}");
            // 0.532: on no amount of GP does a step earn less than a safer one for the same floor.
            foreach(var ((gp,on),(s,b,l)) in steps.Select((step,at)=>(step,(plans[10+3*at],plans[11+3*at],plans[12+3*at]))))
            {
                Check(!(s.Worst>=b.Worst && s.Expected>b.Expected+0.01m),$"{gp} GP, Scrutiny {on}: Safe {s.Expected}/{s.Worst} beats Balanced {b.Expected}/{b.Worst}");
                Check(!(b.Worst>=l.Worst && b.Expected>l.Expected+0.01m),$"{gp} GP, Scrutiny {on}: Balanced {b.Expected}/{b.Worst} beats Luck {l.Expected}/{l.Worst}");
            }
            // 0.534: Safe and Balanced are the same on the capture (589 GP, two results only); they differ where a third result exists.
            Check(CollectableRisk.Same(plans[5],plans[6]),"the capture: the same");
            Check(!CollectableRisk.Same(plans[5],plans[7]),"the capture: Try your luck differs");
            var thirdSafe=plans[8];var thirdMiddle=plans[9];
            Check(!CollectableRisk.Same(thirdSafe,thirdMiddle),$"a node where Balanced has its own plan: {thirdSafe.Worst}/{thirdSafe.Expected} and {thirdMiddle.Worst}/{thirdMiddle.Expected}");
            Check(CollectableRisk.Same(null,thirdSafe)==false,"nothing to compare");
            var greyed=Read("MainWindow.CollectablePanel.cs");
            Check(greyed.Contains("var same=balancedOff && cap==50;") && greyed.Contains("\"CollectablePanel.BalancedSame\""),"greyed, said in its tooltip");
            // 0.533: every step has its moves, ends with what it gathers, never spends more GP than there is, nor gathers more than there are attempts.
            foreach(var ((risk,gp),advice) in paths.Zip(plans.Skip(10+3*steps.Length)))
            {
                Check(advice.Path is {Steps.Count:>0},$"moves at {risk}, {gp} GP");
                Check(advice.Path!.Gp<=gp,$"GP at {risk}, {gp} GP: {advice.Path.Gp}");
                var restores=advice.Path.Steps.Where(step=>step.Move==CollectableMove.Restore).Sum(step=>step.Count);
                var used=advice.Path.Steps.Where(step=>step.Move is CollectableMove.Scour or CollectableMove.Brazen or CollectableMove.Meticulous or CollectableMove.Collect).Sum(step=>step.Count);
                Check(used==4+restores,$"attempts at {risk}, {gp} GP: {used} for {restores} restored");
                Check(advice.Path.Steps[^1].Move==CollectableMove.Collect || advice.Worst==0,$"ends by gathering at {risk}, {gp} GP");
                Check(advice.Path.Steps[0].Move==advice.Move || advice.Path.Steps[0].Move==CollectableMove.Collect,$"starts with the move advised at {risk}, {gp} GP");
            }
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("\"CollectablePanel.Path\"") && !panel.Contains("if(safeStep && advice.Scrutinies"),"the moves on every step");
            Check(panel.Contains("else if(highest!=advice.Worst)") && panel.Contains("advice.Scrips!=advice.Worst"),"a figure is not said twice");
            Check(panel.Contains("note+=Environment.NewLine+localization.Format(\"CollectablePanel.BonusWhenWorth\""),"what is always true is in the tooltip");
            Check(panel.Contains("CollectablePanel.Cap{risk}Now") && panel.Contains("\"CollectablePanel.SafeWould\""),"one plan on screen");
            foreach(var language in new[]{"en","fr"})foreach(var cap in CollectableRisk.Caps)
            {
                var texts=Read($"Localization/Languages/{language}.json");
                Check(texts.Contains($"\"Cap{cap}\":") && texts.Contains($"\"Cap{cap}Tip\":"),$"{language}: step {cap} described");
            }
        }),
        ("Collectable plan (0.527): Priming Touch and Collector's Focus on the node of the report",() =>
        {
            var herb=Rule(43924,"Herb",90,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            var raw=Raw(0,200,100,300,150,false);raw[44]="44:uint 34872";raw[46]="46:bool True";raw[52]="52:text 24 %";
            var turn=CollectableTurn.Parse(raw)!;
            Check(turn is {PrimingAction:34872,PrimingOpen:true,MeticulousChance:24},$"read from the window: {turn}");
            var gains=new CollectableBonuses(250,100,623,200,100,false,true,300,4,40,100,0.5m,false,100,24);
            var scrutinyOn=gains with {ScrutinyActive=true};
            // The capture: Scrutiny on, 623 GP. Two restored attempts (300 GP, 1.5 attempts each on average) earn more: no room for the others.
            Check(CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,scrutinyOn)! is {Move:CollectableMove.Meticulous} capture && capture.Expected==CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,scrutinyOn with {PrimingCost=0})!.Expected,"623 GP: restored attempts first");
            // 100 GP more: the spare GP goes to Priming Touch.
            var spare=scrutinyOn with {Gp=723};
            var with=CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,spare)!;
            var without=CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,spare with {PrimingCost=0})!;
            Check(with.Move==CollectableMove.Priming && with.Expected>without.Expected,$"723 GP: {with.Move} {with.Expected} against {without.Expected}");
            var focusOff=CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,spare with {PrimingCost=0,FocusCost=0})!;
            Check(without.Expected>focusOff.Expected,$"Collector's Focus counts too: {without.Expected} against {focusOff.Expected}");
            Check(CollectableAdvisor.Advise(turn with {IntuitionChance=40,PrimingOpen=false},4,herb,spare)!.Move!=CollectableMove.Priming,"not offered by the window");
            // The capture with both on: 48 % and 70 %: already on, not advised again.
            Check(CollectableAdvisor.Advise(turn with {IntuitionChance=70,MeticulousChance=48},4,herb,spare)!.Move!=CollectableMove.Priming,"already on");
            Check(CollectableAdvisor.Advise(turn with {IntuitionChance=40},4,herb,gains with {Gp=50})!.Move!=CollectableMove.Priming,"not without the GP");
            // The third capture: 64 % after an Intuition is not Priming Touch: the chance counts as shown, and Priming is still possible.
            var keen=turn with {Current=500,Scour=200,BrazenMin=300,BrazenMax=300,Meticulous=200,MeticulousChance=64,IntuitionChance=40};
            var keenPlan=CollectableAdvisor.Advise(keen,4,herb,gains with {Gp=429,PrimingCost=0})!;
            var usual=CollectableAdvisor.Advise(keen with {MeticulousChance=24},4,herb,gains with {Gp=429,PrimingCost=0})!;
            Check(keenPlan.Expected>usual.Expected,$"64 % earns more than 24 %: {keenPlan.Expected} against {usual.Expected}");
        }),
        ("Collectable plan (0.520): Solid Reason's free attempt counts in the average, never in the worst case; a free attempt ready comes before the last gather",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=14,RewardMid=21,RewardHigh=34};
            var full=CollectableTurn.Parse(Raw(1000,177,88,265,132,false))!;
            var without=CollectableAdvisor.Advise(full,3,ore,new CollectableBonuses(Gp:349,RestoreCost:300,MaxAttempts:6))!;
            Check(without is {Expected:136m,Worst:136m,Restores:1},$"the capture before: {without}");
            var with=CollectableAdvisor.Advise(full,3,ore,new CollectableBonuses(Gp:349,RestoreCost:300,MaxAttempts:6,EurekaChance:0.5m))!;
            Check(with is {Expected:153m,Worst:136m,Restores:1,Scrips:136},$"3 + 1 + half a free one, the sure plan unchanged: {with}");
            var ready=CollectableAdvisor.Advise(full,3,ore,new CollectableBonuses(MaxAttempts:6,EurekaReady:true))!;
            Check(ready is {Expected:136m,Worst:136m,Collections:4},$"ready: one more for sure: {ready}");
            var last=CollectableAdvisor.Advise(full,1,ore,new CollectableBonuses(MaxAttempts:6,EurekaReady:true))!;
            Check(last.Move==CollectableMove.Eureka && last.Collections==2,$"before the last gather closes the node: {last}");
            var turnBelow=CollectableTurn.Parse(Raw(0,177,88,265,132,false))!;
            var one=CollectableAdvisor.Advise(full,1,ore,new CollectableBonuses(Gp:349,RestoreCost:300,MaxAttempts:6,EurekaChance:0.5m))!;
            Check(one.Move==CollectableMove.Restore,$"the last attempt: restore first: {one}");
            Check(CollectableAdvisor.Advise(turnBelow,4,ore,new CollectableBonuses(175,88,809,200,100,false,true,300,6,13,100,0.5m)) is {Expected:>0},"a search with the chance ends");
        }),
        ("Collectable with a long name: the game's cut name finds its thresholds, only when it names one item",() =>
        {
            var catalog=new CollectableCatalog([Rule(36291,"Minerai de durium noble collectionnable",80,1000,10),Rule(35636,"Poings en durium noble collectionnables",80,494,10),Rule(9,"Bois collectionnable",50,100,1),Rule(10,"Bois collectionné",50,120,1)]);
            Check(catalog.Named("Minerai de durium noble c...").Single().ItemId==36291,"cut with three dots (in-game capture)");
            Check(catalog.Named("Minerai de durium noble c…").Single().ItemId==36291,"cut with the single character");
            Check(catalog.Named("Minerai de durium noble collectionnable").Single().ItemId==36291,"whole name");
            Check(catalog.Named("Bois collectionn...").Count==0,"two candidates: nothing guessed");
            Check(catalog.Named("Mi...").Count==0 && catalog.Named("...").Count==0,"too short to say");
            Check(catalog.Named("Minerai").Count==0,"a name that is not cut must match whole");
        }),
        ("Collectables table: sorted both ways on every column",() =>
        {
            CollectableRule[] rules=[Rule(1,"B",50,200,5,"Y"),Rule(2,"A",90,100,9,"Z"),Rule(3,"C",70,300,1,"X")];
            uint[] Ids(int column,bool descending)=>Collectables.Sorted(rules,column,descending).Select(rule=>rule.ItemId).ToArray();
            Check(Ids(0,false).SequenceEqual([2u,1u,3u]) && Ids(0,true).SequenceEqual([3u,1u,2u]),"name");
            Check(Ids(1,false).SequenceEqual([3u,1u,2u]),"group");
            Check(Ids(2,false).SequenceEqual([1u,3u,2u]) && Ids(2,true).SequenceEqual([2u,3u,1u]),"level");
            Check(Ids(3,true).SequenceEqual([3u,1u,2u]),"high threshold");
            Check(Ids(4,true).SequenceEqual([2u,1u,3u]),"high reward");
            var table=Read("MainWindow.CollectableTable.cs");
            Check(table.Contains("ImGuiTableFlags.Sortable") && table.Contains("TableGetSortSpecs"),"table");
            Check(Read("MainWindow.Pages.cs").Contains("new(Page.GatheringCollectables,\"Production\",null,DrawCollectablesPage,") && Read("UI/Navigation/MainWindow.CategoryNavigation.cs").Contains("Page.GatheringCollectables") && !Read("MainWindow.GatheringBrowser.cs").Contains("DrawCollectableTable();"),"its own tab");
        }),
        ("Collectable window values: read from the in-game capture; anything missing or of another type gives no turn",() =>
        {
            var turn1=CollectableTurn.Parse(Raw(0,352,263,440,307,true))!;
            Check(turn1 is {ItemId:36291,Current:0,Maximum:1000,Scour:352,BrazenMin:263,BrazenMax:440,Meticulous:307,EstimateScrutiny:true,EstimateIntuition:false,ScourAction:22182,BrazenAction:22183,MeticulousAction:22184,CollectAction:240,ScrutinyAction:22185,FocusAction:21205},"first turn");
            Check(turn1.Extra==175,"the Scrutiny box adds 175: Brazen's width is the plain gain");
            Check(CollectableTurn.Parse(Raw(0,440,351,528,395,true,true))! is {Extra:263,EstimateScrutiny:true,EstimateIntuition:true},"both boxes, in-game log at 10:21:52: 175 + 88");
            var withChance=Raw(0,177,88,265,132,false);withChance[58]="58:text  Intuition\n13 %";
            Check(CollectableTurn.Parse(withChance)!.IntuitionChance==13 && CollectableTurn.Parse(Raw(0,177,88,265,132,false))!.IntuitionChance==0,"Intuition's chance, as logged; unread: 0");
            var turn3=CollectableTurn.Parse(Raw(588,177,88,265,132,false))!;
            Check(turn3.Current==588 && turn3.Extra==0,"later turn");
            Check(CollectableTurn.Parse(Raw(373,177,177,265,177,false))!.Extra is null,"the raised floor after an Intuition does not fit the rule: nothing is deduced");
            Check(CollectableTurn.Parse(null) is null && CollectableTurn.Parse(Raw(0,352,263,440,307,true).Take(40).ToArray()) is null,"too short");
            var moved=Raw(0,352,263,440,307,true);moved[13]="13:text 0";Check(CollectableTurn.Parse(moved) is null,"another type");
            var over=Raw(0,352,263,440,307,true);over[13]="13:uint 2000";Check(CollectableTurn.Parse(over) is null,"impossible value");
            var evidence=File.ReadAllText(Path.GetFullPath("docs/evidence/0.479-collectable-window-values.md"));
            Check(evidence.Contains("| 13 (collectabilité) | 0 | 373 | 588 | 834 |"),"evidence kept");
        }),
        ("Collectable plan: every move compared on what it earns on average; Meticulous at 8 % is not preferred for its chance",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            CollectableAdvice Advise(int current,int scour,int low,int high,int meticulous,int attempts,bool intuition=false,CollectableRule? rule=null)=>CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(current,scour,low,high,meticulous,intuition))!,attempts,rule ?? ore)!;
            Check(CollectableTurn.Parse(Raw(0,177,88,265,132,false))!.MeticulousChance==8,"the chance shown by the game");
            var first=Advise(0,177,88,265,132,4);
            Check(first.Move!=CollectableMove.Meticulous && first.Expected>0,$"in-game capture: not Meticulous at 8 %: {first}");
            Check(first is {Target:CollectableTier.Low,Selections:3,Collections:1,Scrips:3},$"what is certain: {first}");
            Check(first.Expected>=3,"a move is never worth less than the sure plan: "+first.Expected);
            Check(Advise(0,352,263,440,307,4,true)==first,"the box « with Scrutiny » is taken out of the plan");
            var high=CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(0,177,88,265,132,false))! with {MeticulousChance=100},4,ore)!;
            Check(high.Move==CollectableMove.Meticulous && high.Expected>first.Expected,$"at 100 % it is free: then it is the move: {high}");
            var done=Advise(834,177,88,265,132,1);
            Check(done is {Move:CollectableMove.Collect,Target:CollectableTier.Mid,Selections:0,Collections:1,Scrips:7},$"last attempt, 8 % is not worth the item: collect: {done}");
            var keep=Advise(720,50,25,75,37,4);
            Check(keep is {Move:CollectableMove.Collect,Target:CollectableTier.Mid,Collections:4,Scrips:28},$"already at mid, high out of reach: every attempt collects: {keep}");
            var worth=Advise(720,177,88,265,132,4);
            Check(worth is {Target:CollectableTier.High,Selections:2,Collections:2,Scrips:40} && worth.Move!=CollectableMove.Collect,$"2×20 beats 4×7: {worth}");
            var climb=Advise(900,177,88,265,132,2);
            Check(climb is {Target:CollectableTier.High,Selections:1,Collections:1,Scrips:20,Move:CollectableMove.Meticulous},$"Meticulous also reaches high here, and may cost nothing: {climb}");
            var roll=Advise(650,250,125,375,187,2);
            Check(roll.Move==CollectableMove.Brazen,$"a bad roll keeps mid, a good one reaches high: {roll}");
            var lucky=Advise(300,90,45,135,67,2);
            Check(lucky is {Target:CollectableTier.Below,Scrips:0,Move:CollectableMove.Brazen,Impossible:false},$"only a roll can reach low: {lucky}");
            var far=Advise(0,50,25,75,37,2);
            Check(far is {Target:CollectableTier.Below,Move:CollectableMove.Collect,Impossible:true},$"even the best roll is too low: said so: {far}");
            var last=Advise(0,177,88,265,132,1);
            Check(last is {Target:CollectableTier.Below,Move:CollectableMove.Collect,Selections:0,Collections:1,Scrips:0},$"in-game capture: 0 with one attempt left: {last}");
            var goods=ore with {Scrips=false,RewardLow=0,RewardMid=0,RewardHigh=0};
            Check(Advise(0,177,88,265,132,4,false,goods) is {Scrips:0},"item rewards: no scrips announced");
            Check(CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(0,177,88,265,132,false))!,0,ore) is null && CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(0,177,88,265,132,false))!,4,null) is null,"nothing to plan");
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("DrawCollectableAdvice(turn,frame.Integrity,") && panel.Contains("Collectables.Catalog?.For(turn.ItemId)"),"shown, the item found by its identifier");
            Check(!panel.Contains("UseAction") && !panel.Contains("ProcessChatBox"),"nothing executed");
        }),
        ("Collectable plan with bonuses: Scrutiny counted once measured, GP spent only if it earns more; Focus proposed, never relied on",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            var turn=CollectableTurn.Parse(Raw(0,177,88,265,132,false))!;
            var plain=CollectableAdvisor.Advise(turn,4,ore)!;
            Check(CollectableAdvisor.Advise(turn,4,ore,new(0,0,809,200,100))==plain,"not measured yet: not counted");
            var bonus=new CollectableBonuses(150,175,809,200,100);
            var plan=CollectableAdvisor.Advise(turn,4,ore,bonus)!;
            Check(plan is {Move:CollectableMove.Scrutiny,Target:CollectableTier.Mid,Selections:3,Collections:1,Scrips:7,Scrutinies:2} && plan.Expected>plain.Expected,$"it earns more: Scrutiny first: {plan}");
            Check(CollectableAdvisor.Advise(turn,4,ore,bonus with {Gp=100})!.Move!=CollectableMove.Scrutiny,"not enough GP");
            Check(CollectableAdvisor.Advise(turn,4,ore,bonus with {UseScrutiny=false})!.Move==plain.Move,"the setting");
            Check(CollectableAdvisor.Advise(turn,4,ore,bonus with {ScrutinyCost=0})!.Move!=CollectableMove.Scrutiny,"an unknown cost is not planned");
            var enough=CollectableTurn.Parse(Raw(900,177,88,265,132,false))!;
            Check(CollectableAdvisor.Advise(enough,2,ore,bonus)!.Move==CollectableMove.Meticulous,"it reaches high without any GP: none spent for nothing");
            var active=CollectableTurn.Parse(Raw(0,327,238,415,282,false))!;
            Check(active.Extra==150,"Scrutiny on: the gains shown hold it");
            var on=CollectableAdvisor.Advise(active,4,ore,bonus with {ScrutinyActive=true})!;
            Check(on.Move!=CollectableMove.Scrutiny && on.Target==CollectableTier.Mid,$"already on: act now: {on}");
            var close=CollectableTurn.Parse(Raw(200,177,88,265,132,false))! with {IntuitionChance=40};
            var focusBonus=new CollectableBonuses(0,175,809,0,100);
            var focus=CollectableAdvisor.Advise(close,3,ore,focusBonus)!;
            var noFocus=CollectableAdvisor.Advise(close,3,ore,focusBonus with {FocusCost=0})!;
            Check(focus.Move==CollectableMove.Focus && focus.Expected>noFocus.Expected && noFocus.Expected>noFocus.Scrips,$"Intuition counts in the average, Focus when it earns more: {focus}, {noFocus}");
            Check(focus.Target==CollectableTier.Low,"what is certain does not count on it");
            Check(CollectableAdvisor.Advise(close,3,ore,focusBonus with {Gp=50})!.Move!=CollectableMove.Focus,"no GP for Focus");
            Check(CollectableAdvisor.Advise(close with {IntuitionChance=70},3,ore,focusBonus with {IntuitionBase=40})!.Move!=CollectableMove.Focus,"Focus already on: the chance shown is well above the usual one");
            Check(CollectableAdvisor.Advise(close with {IntuitionChance=0},3,ore,focusBonus)!.Move!=CollectableMove.Focus,"chance not read: Focus not proposed");
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("LearnCollectableBonuses(turn,active);") && panel.Contains("\"CollectablePanel.LearnScrutiny\"") && Read("Plugin.cs").Contains("status.StatusId==757"),"measured, asked for, status read");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("CollectableUseFocus") && Read("MainWindow.GatheringPlanSettings.cs").Contains("\"CollectablePanel.UseFocus\"") && Read("MainWindow.GatheringPlanSettings.cs").Contains("\"CollectablePanel.UseScrutiny\""),"setting");
        }),
        ("Collectable plan always ends: at the maximum, above high, with a free Meticulous and the smallest gains (a stack overflow closed the game, in-game report)",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            CollectableTurn Turn(int current,int scour,int low,int high,int meticulous,int chance)=>CollectableTurn.Parse(Raw(current,scour,low,high,meticulous,false))! with {MeticulousChance=chance};
            foreach(var chance in new[]{0,8,50,100})
            foreach(var current in new[]{0,834,999,1000})
            foreach(var (scour,low,high,meticulous) in new[]{(177,88,265,132),(1,1,2,1),(2000,1000,3000,1500)})
            foreach(var attempts in new[]{1,2,4,12})
                Check(CollectableAdvisor.Advise(Turn(current,scour,low,high,meticulous,chance),attempts,ore,new(150,175,5000,200,100)) is not null,$"{chance} {current} {scour} {attempts}");
            var clock=System.Diagnostics.Stopwatch.StartNew();
            var heavy=CollectableAdvisor.Advise(Turn(0,177,88,265,132,8) with {IntuitionChance=13},6,ore,new(175,88,2000,200,100,false,true,300,6))!;
            Check(heavy.Expected>0 && clock.ElapsedMilliseconds<5000 && Read("MainWindow.CollectablePanel.cs").Contains("collectableAdviceRun=Task.Run("),"a hard case ends in reasonable time, off the draw: "+clock.ElapsedMilliseconds+" ms");
            var capped=Rule(1,"Capped",80,5000,20) with {Low=400,Mid=700,High=5000};
            Check(CollectableAdvisor.Advise(Turn(1000,177,88,265,132,100),4,capped)?.Move==CollectableMove.Collect,"at the window's maximum, even under the high threshold: collect");
        }),
        ("Collectable plan: the most scrips on one node: attempts restored with GP when that earns more, the high tier as the reference",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            var turn=CollectableTurn.Parse(Raw(0,177,88,265,132,false))!;
            var without=CollectableAdvisor.Advise(turn,4,ore,new(150,175,809,200,100))!;
            var with=CollectableAdvisor.Advise(turn,4,ore,new(150,175,809,200,100,false,true,300,4))!;
            Check(with.Expected>=without.Expected,$"more choices never earn less: {with.Expected} against {without.Expected}");
            Check(with.Move!=CollectableMove.Restore,$"never at the start: {with}");
            foreach(var left in new[]{2,3,4})
                Check(CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(500,177,88,265,132,false))!,left,ore,new(0,0,809,0,0,false,true,300,4))!.Move!=CollectableMove.Restore,$"user request: not before the end, {left} attempts left");
            Check(CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(1000,177,88,265,132,false))!,1,ore,new(0,0,700,0,0,false,true,300,4))!.Move==CollectableMove.Restore,"the last attempt: restore before it ends the node");
            Check(CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(1000,177,88,265,132,false))!,1,ore,new(0,0,100,0,0,false,true,300,4))!.Move==CollectableMove.Collect,"no GP for it: collect");
            Check(CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(1000,177,88,265,132,false))!,2,ore,new(0,0,700,0,0,false,true,300,4))!.Move==CollectableMove.Collect,"two attempts left: collect first");
            var done=CollectableTurn.Parse(Raw(1000,177,88,265,132,false))!;
            var full=CollectableAdvisor.Advise(done,3,ore,new(0,0,700,0,0,false,true,300,4))!;
            Check(full is {Move:CollectableMove.Collect,Target:CollectableTier.High,Collections:5,Restores:2,Scrips:100,HighScrips:100} && full.Expected==100,$"at high, the GP becomes gathers: {full}");
            Check(CollectableAdvisor.Advise(done,3,ore,new(0,0,700)) is {Collections:3,Scrips:60,HighScrips:60},"no restoring without its cost: the setting or a forbidden action");
            var low=CollectableAdvisor.Advise(turn,4,ore)!;
            Check(low.HighScrips is null && low.HighMissing==1000-531,$"high out of reach without luck: what is missing is said: {low}");
            var reach=CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(700,177,88,265,132,false))!,4,ore)!;
            Check(reach.HighScrips==40,$"reference: 2 actions, 2 gathers at 20: {reach}");
            var filled=CollectableAdvisor.Advise(turn,4,ore,new(0,0,809,0,0,false,true,300,4))!;
            Check(filled.Move!=CollectableMove.Restore,$"nothing is restored at full attempts: {filled}");
            var used=CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(600,177,88,265,132,false))!,1,ore,new(0,0,809,0,0,false,true,300,4))!;
            Check(used.Move==CollectableMove.Restore && used.Expected>7,$"one attempt left at mid, GP for more: restore: {used}");
            var shown=Read("MainWindow.CollectablePanel.cs");
            Check(shown.Contains("collectableAdviceKey!=adviceKey"),"worked out when something changes, not at each frame");
            Check(shown.Contains("if(collectableAdvice is null && (collectableAdvicePending") && !shown.Contains("if(status!=0)restore=null;"),"in-game report: no blink, a passing refusal does not change the plan");
        }),
        ("Collectable plan against a real run: 809 GP, 4 attempts, gains read in game; the player reached high and 20 scrips, the plan must find it",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            var measured=new CollectableBonuses(175,88,809,200,100,false,true,300,4,13);
            var start=CollectableTurn.Parse(Raw(0,177,88,265,132,false))! with {IntuitionChance=13};
            var plan=CollectableAdvisor.Advise(start,4,ore,measured)!;
            Check(plan is {HighScrips:20,HighMissing:0} && plan.Scrips>=20 && plan.Expected>=20,$"start: at least what the player reached: {plan}");
            Check(plan.Move==CollectableMove.Scrutiny,$"in-game report: Scrutiny pays, so it is the move now, not later: {plan}");
            var ticked=CollectableTurn.Parse(Raw(0,352,263,440,307,true))! with {IntuitionChance=13};
            Check(CollectableAdvisor.Advise(ticked,4,ore,measured)==plan,"in-game capture: the box « with Scrutiny » ticked changes nothing");
            var plainMove=CollectableAdvisor.Advise(ticked,4,ore,new CollectableBonuses(0,0,0))!;
            Check(plainMove.GainLow<=177,$"the gain announced is the real one, not the box's +352: {plainMove}");
            Check(plan is {Target:CollectableTier.Mid,Selections:2,Scrutinies:2,Restores:1,Collections:3,Scrips:21},$"3 gathers at 7 with Solid Reason beat 1 at 20: {plan}");
            var noRestore=CollectableAdvisor.Advise(start,4,ore,measured with {RestoreCost=0})!;
            Check(noRestore is {Target:CollectableTier.High,Selections:3,Scrutinies:3,Collections:1,Scrips:20} && noRestore.Expected>=20,$"without restoring attempts, the player's own run: {noRestore}");
            var scrutinyOn=CollectableTurn.Parse(Raw(0,352,263,440,307,false))! with {IntuitionChance=13};
            var acting=CollectableAdvisor.Advise(scrutinyOn,4,ore,measured with {Gp=609,ScrutinyActive=true})!;
            Check(acting.Move is CollectableMove.Scour or CollectableMove.Meticulous or CollectableMove.Brazen,$"Scrutiny on: act, not a second one: {acting}");
            var end=CollectableTurn.Parse(Raw(1000,177,88,265,132,false))! with {IntuitionChance=13};
            Check(CollectableAdvisor.Advise(end,1,ore,measured with {Gp=227}) is {Move:CollectableMove.Collect,Scrips:20,Target:CollectableTier.High},"in-game capture: 1000, one attempt: collect, 20 scrips");
        }),
        ("Restored attempts: the game's own answer says when to wait; nothing is used",() =>
        {
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("attemptsNow<most && CollectableActionStatus?.Invoke(restore.Spec.Id) is { } status") && panel.Contains("restoreStatus=status;") && panel.Contains("CollectablePanel.RestoreWait"),"refused now: the player waits, the plan stays");
            var state=Read("Gathering/CollectableActionState.Dalamud.cs");
            Check(state.Contains("GetActionStatus(ActionType.Action,action)") && state.Contains("MemberFunctionPointers.GetActionStatus==0") && !state.Contains("UseAction"),"guarded, read only");
        }),
        ("Risk accepted: at 0 what is certain decides, at 100 the average; the average and the worst are told",() =>
        {
            var ore=Rule(36291,"Ore",80,1000,20) with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
            CollectableAdvice At(int current,int scour,int low,int high,int meticulous,int attempts,int risk)=>CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(current,scour,low,high,meticulous,false))!,attempts,ore,new(RiskTolerance:risk))!;
            var bold=At(650,250,125,375,187,2,100);
            var careful=At(650,250,125,375,187,2,0);
            // 0.531: Brazen ends mid tier at worst, like Scour, and high at best: at the same floor Safe takes it too.
            Check(bold.Move==CollectableMove.Brazen && careful.Move==CollectableMove.Brazen && careful.Worst==bold.Worst,$"same floor, the better average: {bold.Move}, {careful.Move}");
            Check(careful.Expected>=careful.Worst,"the average is never under the floor");
            Check(bold.Expected>bold.Worst && bold.Worst==7,$"a roll: both are told: {bold}");
            // A gamble: a bad roll loses the tier that was safe.
            var gamble100=At(450,250,125,375,187,2,100);
            var gamble0=At(450,250,125,375,187,2,0);
            Check(gamble0.Worst>=gamble100.Worst,$"the careful line never ends lower: {gamble0.Worst} against {gamble100.Worst}");
            Check(gamble0.Expected<=gamble100.Expected,$"the bold line never averages lower: {gamble0.Expected} against {gamble100.Expected}");
            var lucky=At(300,90,45,135,67,2,0);
            // 0.531: nothing is certain here, so a roll costs nothing on the floor: Safe may take it, and it is not called impossible.
            Check(lucky is {Impossible:false,Worst:0},$"not called impossible: {lucky}");
            Check(At(300,90,45,135,67,2,100).Move==CollectableMove.Brazen,"at 100 the roll is taken");
            var risks=new[]{0,25,50,75,100};
            var riskPlans=Each(risks,risk=>CollectableAdvisor.Advise(CollectableTurn.Parse(Raw(0,177,88,265,132,false))! with {IntuitionChance=13},4,ore,new(175,88,809,200,100,false,true,300,4,13,risk))!);
            foreach(var (risk,plan) in risks.Zip(riskPlans))
            {
                Check(plan.Worst<=plan.Expected && plan.Expected>=20 && plan.Worst>=7,$"real run at {risk} %: {plan}");
            }
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("CollectableRisk.Snap(configuration.CollectableRiskTolerance)") && panel.Contains("FontAwesomeIcon.HandHolding:cap==50 ? FontAwesomeIcon.BalanceScale:FontAwesomeIcon.Dice") && panel.Contains("WrappedTooltip(localization.Get($\"CollectablePanel.Cap{cap}\")"),"three steps");
            Check(new TenshoAssistant.Configuration().CollectableRiskTolerance==50,"default");
        }),
        ("Collectables: thresholds by item and by the name shown, tiers, empty rows left out",() =>
        {
            // Values of docs/evidence/0.409-collectables.md (rows 23.0 and 14.0).
            var catalog=new CollectableCatalog([
                new(30970,"Arc long en cèdre collectionnable",50,50,110,150,190,45,49,54,true),
                new(12713,"Pic-à-glace",50,51,49,81,114,6,7,11,true),
                new(5108,"Minerai de cuivre jaune",50,50,0,0,0,0,0,0,true),
            ]);
            Check(catalog.Count==2,"rows with no threshold left out");
            Check(catalog.For(5108).Count==0 && catalog.For(30970).Count==1,"by item");
            Check(catalog.Named("Arc long en  cèdre collectionnable ") is [{ ItemId:30970 }],"by the name shown, icon and spaces removed");
            Check(catalog.Named("pic-à-glace") is [{ Low:49,Mid:81,High:114 }],"name case ignored");
            var rule=catalog.For(30970)[0];
            Check(Collectables.Tier(rule,109)==CollectableTier.Below && Collectables.Tier(rule,110)==CollectableTier.Low
                && Collectables.Tier(rule,150)==CollectableTier.Mid && Collectables.Tier(rule,999)==CollectableTier.High,"tiers");
        }),
        ("Collectables: numbers read as shown by the game; anything else is unknown",() =>
        {
            Check(Collectables.Number("600")==600 && Collectables.Number("1,234")==1234 && Collectables.Number("1 234")==1234,"numbers");
            Check(Collectables.Number(null) is null && Collectables.Number("") is null && Collectables.Number("12a") is null && Collectables.Number("-5") is null,"unknown");
        }),
        ("Collectables: the reader stays on the framework thread, reads named fields only, and no action is advised",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/CollectableReader.Dalamud.cs"));
            Check(reader.Contains("framework.IsInFrameworkUpdateThread") && reader.Contains("addon->ItemName") && reader.Contains("addon->IntegrityLeftover")
                && reader.Contains("MaxValues=64") && reader.Contains("_=Task.Run(Load)"),"reader");
            Check(!reader.Contains("FireCallback") && !reader.Contains("SetAtkValues"),"nothing sent to the game");
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectablePanel.cs"));
            Check(panel.Contains("CollectablePanel.Unknown") && panel.Contains("developerMode && ImGui.CollapsingHeader"),"current collectability unknown, raw values in developer mode");
        }),
        ("Collectables table 0.413: every kept rule by level, filtered by item or group words",() =>
        {
            var catalog=new CollectableCatalog([
                new(2,"Noyau de glacier",50,51,141,236,332,13,16,26,true,"Nv 50-60"),
                new(1,"Madrier de cèdre d'affinage",80,80,360,540,720,5,10,15,false,"Composants d'affinage pour menuisier"),
                new(3,"Pic-à-glace",50,51,49,81,114,6,7,11,true,"Nv 50-60"),
                new(4,"Vide",50,50,0,0,0,0,0,0,true,"Nv 50-60"),
            ]);
            Check(catalog.All.Select(rule=>rule.ItemId).SequenceEqual(new uint[]{2,3,1}),"by level then name, empty rows left out");
            Check(Collectables.Filter(catalog.All,"nv 50").Length==2 && Collectables.Filter(catalog.All,"menuisier madrier").Length==1
                && Collectables.Filter(catalog.All,"  ").Length==3 && Collectables.Filter(catalog.All,"glace 80").Length==0,"filter");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectableTable.cs"));
            Check(ui.Contains("Tip(\"CollectableTable.SearchTip\")") && ui.Contains("clipper.End();clipper.Destroy();") && ui.Contains("CollectableTable.ItemReward"),"table");
        }),
        ("Held collectables 0.432: tier and scrips by the first scrip rule; items the game does not take for scrips left out",() =>
        {
            var catalog=new CollectableCatalog([
                new(30970,"Arc long",50,50,110,150,190,45,49,54,true),
                new(31101,"Madrier",80,80,360,540,720,5,10,15,false),
            ]);
            Check(Collectables.Value(catalog,new(30970,160)) is { Tier:CollectableTier.Mid, Scrips:49 },"mid tier");
            Check(Collectables.Value(catalog,new(30970,100)) is { Tier:CollectableTier.Below, Scrips:0 },"below");
            Check(Collectables.Value(catalog,new(30970,190))!.Scrips==54 && Collectables.Value(catalog,new(31101,800)) is null && Collectables.Value(catalog,new(5,1)) is null,"high; item rewards and unknown: none");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/CollectableReader.Dalamud.cs"));
            Check(reader.Contains("now>Volatile.Read(ref holdingsWanted)") && reader.Contains("item->IsCollectable()") && reader.Contains("container->IsLoaded"),"bags read only while asked");
        }),
    ];
}
