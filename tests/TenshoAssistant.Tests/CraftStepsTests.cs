// Test audit of 2026-11-09, batch 2 (0.1021): CraftSteps789Tests, CraftStepsFold790Tests merged into CraftStepsTests; every case is kept.
using TenshoAssistant.Craft;
internal static class CraftStepsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static RecipeDefinition Recipe(uint item,int made,params (uint Item,int Amount)[] ingredients)=>
        new(item,item,made,"Job",1,ingredients.Select(row=>new RecipeIngredient(row.Item,row.Amount)).ToArray(),1);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From CraftSteps789Tests: 0.789 (X-30, user request): the crafting steps left for the workshop and crafting lists.
        ("Craft steps: stock first, ingredients a step before what uses them, gathered items not crafted",() =>
        {
            // A (plank-like) needs 2 B; B needs 3 C, C gathered; D needs 1 B and 1 A.
            var recipes=new Dictionary<uint,RecipeDefinition>{[1]=Recipe(1,1,(2,2)),[2]=Recipe(2,1,(3,3)),[3]=Recipe(3,1,(9,1)),[4]=Recipe(4,2,(2,1),(1,1))};
            var stock=new Dictionary<uint,long>{[2]=1};
            var steps=BaseMaterials.Crafts([(4,4L)],item=>stock.GetValueOrDefault(item),item=>recipes.GetValueOrDefault(item),item=>item==3);
            var by=steps.ToDictionary(step=>step.Item);
            Check(!by.ContainsKey(3),"gathered: not a craft");
            Check(by[4].Runs==2 && by[1].Runs==2 && by[2].Runs==5,"D twice (2 each), A twice, B 2+4-1 held");
            Check(by[4].Needed==4 && by[1].Needed==2 && by[2].Needed==6,"0.790: asked in all, the held one included");
            Check(by[2].Step<by[1].Step && by[1].Step<by[4].Step && by[2].Step==1,"B, then A, then D");
            Check(steps.Select(step=>step.Step).SequenceEqual(steps.Select(step=>step.Step).Order()),"first step first");
        }),
        ("Craft steps: nothing left when the stock covers it",() =>
        {
            var recipes=new Dictionary<uint,RecipeDefinition>{[1]=Recipe(1,1,(2,2))};
            Check(BaseMaterials.Crafts([(1,3L)],item=>item==1 ? 5:0,item=>recipes.GetValueOrDefault(item),_=>false).Count==0,"held");
        }),
        ("Craft steps: worked out with the list lines, drawn above them in every list panel, folded at first",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("listPanelCrafts[key]=BaseMaterials.Crafts(wanted,ListPanelStock,ListPanelRecipe,ListPanelFetched);"),"once a second with the lines");
            Check(page.IndexOf("DrawListPanelCrafts(key);")<page.IndexOf("if(configuration.ListPanelsFinishedOnly && ListPanelFinished(key) is { } finished)"),"also with finished items only");
            Check(page.Contains("ImGui.CollapsingHeader($\"{localization.Format(\"ListPanels.Crafts\",steps.Count)}###ListPanelCrafts{key}\")"),"folded");
        }),
        // From CraftStepsFold790Tests: 0.790 (X-31, user request): each step folds; one way of writing the figures.
        ("Craft steps: a fold per step, open at first, and the same « held / needed » line as the materials",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("ImGui.TreeNodeEx($\"{localization.Format(\"ListPanels.CraftStep\",group.Key)} ({done}/{group.Count()})###ListCraftStep{key}{group.Key}\",ImGuiTreeNodeFlags.DefaultOpen)"),"fold per step");
            Check(page.Contains("DrawListPanelLine(key+\"Crafts\",craft.Item,craft.Needed,GetRecipeHoldings(craft.Item));"),"same line");
            Check(page.Contains("finally{ImGui.TreePop();}"),"popped");
        }),
    ];
}
