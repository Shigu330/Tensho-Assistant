using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;

// 0.478 (user requests, in-game campaign, lot 6): collectables with a long name, their tab and sort; GP over the nodes of a crafting list; forbidden actions; the panel's layout.
internal static class Gathering478Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
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
        ("GP actions can be forbidden, on both jobs, like macro actions",() =>
        {
            var free=Node(1000);
            Check(free.Best.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.Yield),"the yield action is used when free");
            var key=GpActionRules.Joined(GpActionCatalog.Specs.Where(spec=>spec.Effect==GpEffect.Yield).Select(GpActionRules.Key));
            var without=Node(1000,key);
            Check(without.Best.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield) && without.Frontier.All(plan=>plan.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield)),"never planned");
            var botanist=GpOptimizer.Solve(new(17,90,1000,4,1,100,0,1,MoreActions:true,Forbidden:key),Catalog());
            Check(botanist.Best.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield),"the botanist's twin too");
            Check(GpActionRules.Joined(["b","a","a",""])=="a|b" && GpActionRules.Joined(null)=="","joined");
            Check(GpActionRules.Groups(Catalog()).All(group=>group.Miner is not null && group.Botanist is not null),"each action has its twin");
            Check(Read("MainWindow.Settings.cs").Contains("DrawGpListSettings();") && Read("MainWindow.GatheringGp.cs").Contains("Forbidden:GpForbidden"),"in the settings and the GP page");
        }),
        ("GP route: the fewest nodes for the amount; GP shared out and coming back; the last node spends only what it needs",() =>
        {
            var whole=Node(800);
            var baseYield=whole.Baseline;
            Check(baseYield==4m,"fixture: 4 items per node without GP");
            var small=GpRoutes.Build(whole,new(2,800,800,0))!;
            Check(small is {Nodes:1,Reached:true,GpSpent:0},"two items: one node, no GP spent");
            var one=GpRoutes.Build(whole,new(6,800,800,0))!;
            Check(one is {Nodes:1,Reached:true} && one.First.Cost>0 && one.First.ExpectedYield>=6 && one.First==whole.Frontier.First(plan=>plan.ExpectedYield>=6),"one node, the cheapest plan that does it");
            var noRegen=GpRoutes.Build(whole,new(200,800,800,0))!;
            var regen=GpRoutes.Build(whole,new(200,800,800,300))!;
            Check(noRegen.Reached && regen.Reached && regen.Nodes<noRegen.Nodes,$"GP coming back means fewer nodes: {regen.Nodes} against {noRegen.Nodes}");
            Check(noRegen.GpSpent<=800,"no more GP spent than there is");
            Check(noRegen.First.Cost<=800/noRegen.Nodes+1 || noRegen.Nodes==1,"the GP is shared, not all on the first node");
            var poor=GpRoutes.Build(whole,new(40,0,800,0))!;
            Check(poor is {Nodes:10,GpSpent:0} && poor.First.Cost==0,"no GP: only nodes");
            Check(GpRoutes.Build(whole,new(0,800,800,0)) is null,"nothing needed: no route");
            var never=GpRoutes.Build(Node(800,success:0),new(5,800,800,0))!;
            Check(!never.Reached && never.Nodes==GpRoutes.MaxNodes,"out of reach is said");
        }),
        ("Crafting list raw needs: a recipe's materials over the whole list, shared materials added, an item without recipe as it is",() =>
        {
            RecipeDefinition Recipe(uint recipe,uint result,params (uint Id,int Amount)[] ingredients)=>new(recipe,result,1,"Test",100,ingredients.Select(row=>new RecipeIngredient(row.Id,row.Amount)).ToArray(),0,null);
            var ingot=Recipe(1,100,(1000,2));var sword=Recipe(2,200,(100,3),(1000,1));var plate=Recipe(3,300,(1000,5));
            var catalog=RecipeCatalog.Build([ingot,sword,plate],"fixture");
            var list=new CraftList("L",[new(200,2),new(300,1),new(1000,7),new(2000,4)]);
            var needs=CraftLists.RawNeeds(list,new Dictionary<uint,RecipeDefinition>{[200]=sword,[300]=plate},catalog);
            Check(needs[1000]==2*(3*2+1)+5+7,"shared raw material: "+needs[1000]);
            Check(needs[2000]==4,"an entry without recipe");
            Check(!needs.ContainsKey(100) && !needs.ContainsKey(200),"nothing crafted is gathered");
            Check(CraftLists.RawNeeds(new CraftList("E",[]),new Dictionary<uint,RecipeDefinition>(),catalog).Count==0,"empty list");
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
            Check(Read("Configuration.cs").Contains("\"CollectableUseFocus\"") && Read("MainWindow.GatheringListPlan.cs").Contains("\"CollectablePanel.UseFocus\"") && Read("MainWindow.GatheringListPlan.cs").Contains("\"CollectablePanel.UseScrutiny\""),"setting");
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
            Check(heavy.Expected>0 && clock.ElapsedMilliseconds<500,"a hard case stays quick: "+clock.ElapsedMilliseconds+" ms");
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
            Check(Read("MainWindow.CollectablePanel.cs").Contains("collectableAdviceKey!=adviceKey"),"worked out when something changes, not at each frame");
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
        ("Restored attempts: the game's own answer decides, below the maximum; nothing is used",() =>
        {
            var panel=Read("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("attemptsNow<most && CollectableActionStatus?.Invoke(restore.Spec.Id) is { } status") && panel.Contains("if(status!=0)restore=null;"),"refused now: not planned");
            var state=Read("Gathering/CollectableActionState.Dalamud.cs");
            Check(state.Contains("GetActionStatus(ActionType.Action,action)") && state.Contains("MemberFunctionPointers.GetActionStatus==0") && !state.Contains("UseAction"),"guarded, read only");
        }),
        ("Major bonus: normal is the plainest node seen; a normal node keeps its GP, a bonus node spends it",() =>
        {
            var norms=new GatherNodeNorms();
            var plain=new GatherNodeShape(4,1,10);
            Check(norms.Observe(1,plain)==plain && !GatherNodeNorms.IsBonus(plain,plain),"the first node is normal");
            Check(GatherNodeNorms.IsBonus(norms.Observe(1,plain with {Attempts=5}),plain with {Attempts=5}),"one attempt more");
            Check(GatherNodeNorms.IsBonus(norms.Observe(1,plain with {Quantity=2}),plain with {Quantity=2}),"a higher quantity");
            Check(GatherNodeNorms.IsBonus(norms.Observe(1,plain with {Boon=40}),plain with {Boon=40}) && !GatherNodeNorms.IsBonus(norms.Observe(1,plain with {Boon=39}),plain with {Boon=39}),"a much higher extra chance only");
            var lower=new GatherNodeShape(3,1,10);
            Check(norms.Observe(1,lower).Attempts==3 && norms.Observe(2,plain)==plain,"a plainer one becomes normal; by item");
            var whole=Node(800);
            var saved=GpRoutes.Build(whole,new(200,800,800,0,true,false))!;
            Check(saved.First.Cost==0 && saved.GpSpent<=800,"normal node, GP not full of regen: plain gathering");
            var overflow=GpRoutes.Build(whole,new(200,800,800,120,true,false))!;
            Check(overflow.First.Cost<=120,"GP that would be lost may be spent: "+overflow.First.Cost);
            var richer=Node(800,attempts:6);
            var bonus=GpRoutes.Build(richer,new(200,800,800,0,true,true),Node(800))!;
            Check(bonus.First.Cost>400 && bonus.First.Cost<=800,"bonus node: GP spent here: "+bonus.First.Cost);
            var last=GpRoutes.Build(whole,new(6,800,800,0,true,false))!;
            Check(last is {Nodes:1,Reached:true} && last.First.ExpectedYield>=6,"a node that can finish the list uses what it takes");
            var plan=Read("MainWindow.GatheringPanel.cs");
            Check(plan.Contains("Math.Clamp(first.Quantity ?? 1,1,20)") && plan.Contains("gatherNodeNorms.Observe(slot.ItemId,shape)") && plan.Contains("configuration.GpSaveForBonus,gatheringPanelBonus),usual)"),"wired");
        }),
        ("Gathering panel: readable at its width; a collectable says where its GP goes; the regeneration is measured, or said to be unknown",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("\"GatheringPanel.CollectablePlan\"") && panel.Contains("Collectables?.Catalog?.For(slot.ItemId) is {Count:>0}"),"a collectable has no GP plan here, and says why");
            Check(panel.Contains("ImGui.PushTextWrapPos(0);") && panel.Contains("BeginGroup"),"texts wrap; name and rates on two lines");
            Check(panel.Contains("configuration.GpRegenObserved=gained;") && panel.Contains("GpRoutes.Build("),"regeneration measured, route used");
            Check(Read("MainWindow.GatheringListPlan.cs").Contains("\"GatheringPanel.ListNoRegen\""),"no invented regeneration");
            foreach(var language in new[]{"en","fr"})
            {
                var texts=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")))!["CollectablePanel"]!;
                Check(!texts["Unknown"]!.GetValue<string>().Contains("confirm",StringComparison.OrdinalIgnoreCase),"the player is not asked to confirm anything: "+language);
            }
        }),
    ];
}
