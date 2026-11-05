using TenshoAssistant.Craft;

// 0.591 (C18): the chain as a list to work through.
internal static class CraftChain591Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static CraftChainStep Step(uint recipe,uint item,long crafts,params uint[] ingredients)
        =>new(new RecipeDefinition(recipe,item,1,"Menuiserie",100,ingredients.Select(id=>new RecipeIngredient(id,1)).ToArray(),0,null),crafts,crafts);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chain: a recipe met twice is one row with its total",() =>
        {
            var merged=CraftChain.Merged([Step(1,10,2),Step(2,20,1,10),Step(1,10,3),Step(3,30,1,10,20)]);
            Check(merged.Select(row=>row.Recipe.RecipeId).SequenceEqual<uint>([1,2,3]),"one row each");
            Check(merged[0] is {Crafts:5,Produced:5},"the total");
        }),
        ("Chain: a row never comes before the recipe of its ingredient",() =>
        {
            // Stock covered X for A's first batch: X only appears before A's second one. A's first place would come before X.
            var merged=CraftChain.Merged([Step(1,10,1,99),Step(2,20,1,10),Step(9,99,1),Step(1,10,1,99),Step(3,30,1,10,20)]);
            var order=merged.Select(row=>row.Recipe.RecipeId).ToArray();
            Check(order.SequenceEqual<uint>([9,1,2,3]),"X, then A, then what needs A: "+string.Join(",",order));
        }),
        ("Chain: nothing to merge keeps the order; empty stays empty; a cycle still ends",() =>
        {
            Check(CraftChain.Merged([Step(1,10,1),Step(2,20,1,10)]).Select(row=>row.Recipe.RecipeId).SequenceEqual<uint>([1,2]),"same order");
            Check(CraftChain.Merged([]).Count==0,"empty");
            Check(CraftChain.Merged([Step(1,10,1,20),Step(2,20,1,10)]).Count==2,"a cycle Build never gives: both rows, no endless loop");
        }),
        ("Chain: the ticks last one calculation; each row has its buttons",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftChain.cs")).Replace("\r\n","\n");
            Check(view.Contains("craftChainDone.Clear();\n            _ = craftChain.StartAsync("),"a new calculation starts clean");
            Check(view.Contains("OpenCraftLog(step.Recipe)") && view.Contains("OpenCraftSimulation(step.Recipe)"),"Log and Simulate");
            Check(view.Contains("foreach (var step in merged) text.AppendLine("),"the copied text shows the same rows");
        }),
    ];
}
