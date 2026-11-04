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
