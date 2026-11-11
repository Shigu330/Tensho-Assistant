// Test audit of 2026-11-09, batch 2 (0.1021): CraftChain591Tests, CraftChained696Tests merged into CraftChainTests; every case is kept.
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
internal static class CraftChainTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private static ItemHoldingsSummary Stock(long quantity = 0) => new([], quantity, 0, true, false, false, true);
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private static CraftChainStep Step(uint recipe,uint item,long crafts,params uint[] ingredients)
        =>new(new RecipeDefinition(recipe,item,1,"Menuiserie",100,ingredients.Select(id=>new RecipeIngredient(id,1)).ToArray(),0,null),crafts,crafts);
    private const uint Ore=1,Ingot=2,Rivet=3,Part=4;
    private static RecipeDefinition Recipe(uint id,uint result,int amount,params (uint Item,int Amount)[] ingredients)=>new(id,result,amount,"Job",1,ingredients.Select(row=>new RecipeIngredient(row.Item,row.Amount)).ToArray());
    private static readonly RecipeDefinition IngotRecipe=Recipe(10,Ingot,1,(Ore,2)),RivetRecipe=Recipe(11,Rivet,2,(Ingot,1)),PartRecipe=Recipe(12,Part,1,(Ingot,1),(Rivet,1));
    private static RecipeDefinition? Of(uint item)=>item switch { Ingot=>IngotRecipe,Rivet=>RivetRecipe,Part=>PartRecipe,_=>null };
    private static ItemHoldingsSummary Held(long total)=>new([],total,0,true,false,false,true);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft chain sessions publish managed results and invalidate every context boundary", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "A", 1, [new(5, 2)]);
            var catalog = RecipeCatalog.Build([root], "test");
            var bank = new BankState([], false, false, null);
            var input = new CraftChainInput(1, 100, root, 2, catalog, null, bank);
            using var session = new CraftChainSession();
            session.StartAsync(input).GetAwaiter().GetResult();
            Check(session.State.Result?.Materials.Single().Amount == 4 && session.State.Result.HasUnknown
                && !session.State.Loading && session.State.CalculatedAt is not null, "captured missing observations remain explicitly unknown");
            session.ValidateContext(1, 100, 2, catalog);
            Check(session.State.Result is not null, "same context keeps manual result");
            foreach (var change in new (ulong Owner, uint Item, int Target, RecipeCatalog Catalog)[] { (2ul, 100u, 2, catalog),
                (1ul, 101u, 2, catalog), (1ul, 100u, 3, catalog), (1ul, 100u, 2, RecipeCatalog.Build([root], "new")) })
            {
                var pending = session.StartAsync(input);
                session.ValidateContext(change.Owner, change.Item, change.Target, change.Catalog);
                pending.GetAwaiter().GetResult();
                Check(session.State.Input is null && session.State.Result is null && !session.State.Loading, "old worker cannot publish through a changed context");
            }
        }),
        ("Craft chain sessions cancel clear errors and prevent publication after disposal", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "A", 1, [new(5, 2)]);
            var catalog = RecipeCatalog.Build([root], "test");
            var input = new CraftChainInput(1, 100, root, 1, catalog, null, new([], false, false, null));
            var session = new CraftChainSession();
            session.StartAsync(input with { Recipe = root with { ResultAmount = 0 } }).GetAwaiter().GetResult();
            Check(session.State.Error is not null && !session.State.Loading && session.State.Result is null, "worker errors become a visible failed calculation");
            var pending = session.StartAsync(input);
            session.Cancel(); pending.GetAwaiter().GetResult();
            Check(session.State.Input is null && session.State.Error is null, "manual cancellation clears all previous evidence");
            pending = session.StartAsync(input);
            session.Dispose(); pending.GetAwaiter().GetResult();
            session.Dispose(); session.StartAsync(input).GetAwaiter().GetResult();
            Check(session.State.Input is null && session.State.Result is null && !session.State.Loading, "idempotent disposal blocks future and delayed publications");
        }),
        ("Craft chain rounds yields and keeps final target independent of owned output", () =>
        {
            var root = new RecipeDefinition(1, 100, 3, "Cuisine", 1, [new(10, 2)]);
            var sub = new RecipeDefinition(2, 10, 3, "Cuisine", 1, [new(5, 2)]);
            var plan = CraftChain.Build(root, 7, RecipeCatalog.Build([root, sub], "test"), id => Stock(id == 100 ? 99 : 0));
            Check(plan.Requested == 7 && plan.Produced == 9, "requested production is not reduced by owned final output");
            Check(plan.Steps.Select(step => step.Recipe.RecipeId).SequenceEqual(new uint[] {2, 1}), "dependencies precede root");
            Check(plan.Steps[0].Crafts == 2 && plan.Materials.Single().Amount == 4, "intermediate yield used");
            Check(!plan.HasUnresolved && !plan.HasUnknown, "complete proposal within observed scope");
        }),
        ("Craft chain spends shared stock once and reuses intermediate surplus", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "Cuisine", 1, [new(10, 1), new(20, 1)]);
            var first = new RecipeDefinition(2, 10, 3, "Cuisine", 1, [new(5, 2)]);
            var second = new RecipeDefinition(3, 20, 1, "Cuisine", 1, [new(10, 2), new(5, 3)]);
            var calls = new Dictionary<uint, int>();
            var plan = CraftChain.Build(root, 1, RecipeCatalog.Build([root, first, second], "test"), id =>
            { calls[id] = calls.GetValueOrDefault(id) + 1; return Stock(id == 5 ? 4 : 0); });
            Check(plan.Steps.Count(step => step.Recipe.RecipeId == 2) == 1, "surplus fulfills second branch without recrafting");
            Check(plan.Materials.Single().ItemId == 5 && plan.Materials.Single().Amount == 1, "stock four consumed once across total five raw materials");
            Check(calls.Values.All(count => count == 1), "one stable observation per item");
        }),
        ("Craft chain keeps recipe choices explicit and skips branches already covered by stock", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "Cuisine", 1, [new(10, 4), new(20, 2)]);
            var variants = new[] { root, new RecipeDefinition(2, 10, 1, "A", 1, [new(5, 1)]),
                new RecipeDefinition(3, 10, 2, "B", 1, [new(6, 1)]), new RecipeDefinition(4, 20, 1, "A", 1, [new(7, 2)]) };
            var plan = CraftChain.Build(root, 1, RecipeCatalog.Build(variants, "test"), id => Stock(id == 10 ? 1 : id == 20 ? 2 : 0));
            Check(plan.HasUnresolved && plan.Materials.Single() == new CraftChainMaterial(10, 3, CraftChainReason.RecipeChoice), "no arbitrary variant selected");
            Check(plan.Steps.Count == 1 && !plan.Observations.ContainsKey(7), "owned intermediate does not expand ingredients");
        }),
        ("Craft chain honors explicit variants and rejects choices for a different result", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "A", 1, [new(10, 4)]);
            var first = new RecipeDefinition(2, 10, 1, "A", 1, [new(5, 2)]);
            var second = new RecipeDefinition(3, 10, 3, "B", 1, [new(6, 2)]);
            var catalog = RecipeCatalog.Build([root, first, second], "test");
            var plan = CraftChain.Build(root, 1, catalog, _ => Stock(), recipeChoices: new Dictionary<uint, uint> { [10] = 3 });
            Check(!plan.HasUnresolved && plan.Steps[0].Recipe.RecipeId == 3 && plan.Steps[0].Crafts == 2
                && plan.Materials.Single().ItemId == 6 && plan.Materials.Single().Amount == 4, "chosen variant determines ingredients and yield");
            var wrong = CraftChain.Build(root, 1, catalog, _ => Stock(), recipeChoices: new Dictionary<uint, uint> { [10] = 1 });
            Check(wrong.HasUnresolved && wrong.Materials.Single().Reason == CraftChainReason.RecipeChoice, "root recipe is not a valid recipe for another result");
        }),
        ("Craft chain sessions copy variant choices before entering the worker", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "A", 1, [new(10, 1)]);
            var variants = new[] { root, new RecipeDefinition(2, 10, 1, "A", 1, [new(5, 2)]),
                new RecipeDefinition(3, 10, 1, "B", 1, [new(6, 3)]) };
            var catalog = RecipeCatalog.Build(variants, "test");
            var choices = new Dictionary<uint, uint> { [10] = 3 };
            using var session = new CraftChainSession();
            var pending = session.StartAsync(new(1, 100, root, 1, catalog, null, new([], false, false, null), choices, 7));
            choices[10] = 2;
            pending.GetAwaiter().GetResult();
            Check(session.State.Input?.RecipeChoices?[10] == 3 && session.State.Input.ChoiceRevision == 7
                && session.State.Result?.Materials.Single().ItemId == 6, "later UI edits cannot change worker inputs");
        }),
        ("Craft chain detects cycles and depth limits without claiming a complete proposal", () =>
        {
            var root = new RecipeDefinition(1, 100, 1, "A", 1, [new(10, 1)]);
            var sub = new RecipeDefinition(2, 10, 1, "A", 1, [new(100, 1)]);
            var catalog = RecipeCatalog.Build([root, sub], "test");
            var cyclic = CraftChain.Build(root, 1, catalog, _ => Stock());
            Check(cyclic.HasUnresolved && cyclic.Materials.Single().Reason == CraftChainReason.Cycle, "cyclic need visible");
            var bounded = CraftChain.Build(root, 1, catalog, _ => Stock(), maxDepth: 1);
            Check(bounded.HasUnresolved && bounded.Materials.Single().Reason == CraftChainReason.DepthLimit, "depth limit visible");
            Throws<InvalidOperationException>(() => CraftChain.Build(root, 1, catalog, _ => Stock(), maxNeeds: 1));
        }),
        ("Craft chain retains unknown and cached evidence and exposes immutable copies", () =>
        {
            var ingredients = new List<RecipeIngredient> { new(5, 3) };
            var rows = new List<StorageHolding> { new(StorageCategory.PlayerBags, "Bags", InventoryCoverage.Partial, false, DateTimeOffset.UtcNow, 1, 0) };
            var root = new RecipeDefinition(1, 100, 1, "A", 1, ingredients);
            var plan = CraftChain.Build(root, 1, RecipeCatalog.Build([root], "test"), _ => new(rows, 1, 0, true, true, true, false));
            rows.Clear(); ingredients.Clear();
            Check(plan.HasUnknown && plan.HasCached && plan.Materials.Single().Amount == 2, "minimum observed stock reduces only the covered amount");
            Check(plan.Observations[5].Rows.Count == 1 && plan.Steps[0].Recipe.Ingredients.Count == 1, "producer lists detached");
            Throws<NotSupportedException>(() => ((IList<CraftChainStep>)plan.Steps).Clear());
            Throws<NotSupportedException>(() => ((IList<CraftChainMaterial>)plan.Materials).Clear());
            Throws<NotSupportedException>(() => ((IList<RecipeIngredient>)plan.Steps[0].Recipe.Ingredients).Clear());
        }),
        ("Craft chain validates inputs and detects overflow cancellation and incomplete recipe data", () =>
        {
            var root = new RecipeDefinition(1, 100, 2, "A", 1, [new(5, 3)]);
            var empty = new RecipeDefinition(2, 5, 1, "A", 1, []);
            var catalog = RecipeCatalog.Build([root, empty], "test");
            Check(CraftChain.Build(root, 1, catalog, _ => Stock()).Materials.Single().Reason == CraftChainReason.MissingIngredients, "missing ingredients not an invented free craft");
            Throws<ArgumentOutOfRangeException>(() => CraftChain.Build(root, 0, catalog, _ => Stock()));
            Throws<OverflowException>(() => CraftChain.Build(root, long.MaxValue, catalog, _ => Stock()));
            using var cancel = new CancellationTokenSource(); cancel.Cancel();
            Throws<OperationCanceledException>(() => CraftChain.Build(root, 1, catalog, _ => Stock(), cancel.Token));
            Throws<InvalidDataException>(() => CraftChain.Build(root, 1, catalog, _ => Stock(-1)));
            var unknown = CraftChain.Build(root, 1, catalog, _ => new([], 0, 0, false, true, false, false));
            Check(unknown.HasUnknown, "no observation is not an exact zero");
        }),
        // From CraftChain591Tests: 0.591 (C18): the chain as a list to work through.
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
            Check(HasCode(view,"craftChainDone.Clear();\n            _ = craftChain.StartAsync("),"a new calculation starts clean");
            Check(view.Contains("OpenCraftLog(step.Recipe)") && view.Contains("OpenCraftSimulation(step.Recipe)"),"Log and Simulate");
            Check(view.Contains("foreach (var step in merged) text.AppendLine("),"the copied text shows the same rows");
        }),
        // From CraftChained696Tests: 0.696 (user request): « What can I craft? » from the ore, through every step, on one shared stock.
        ("Pages: rows by page, a page past the end gives the last, an empty list has one page",() =>
        {
            var rows=Enumerable.Range(1,60).ToArray();
            var (first,page,pages)=CraftFromStock.Paged(rows,0,25);
            Check(first.Length==25 && first[0]==1 && page==0 && pages==3,"first page");
            var (last,lastPage,_)=CraftFromStock.Paged(rows,9,25);
            Check(last.Length==10 && last[0]==51 && lastPage==2,"past the end");
            Check(CraftFromStock.Paged(Array.Empty<int>(),0,25).Pages==1,"empty");
        }),
        ("Pages: market prices follow the page, or all on a click, and the whole list is ranked only then",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftFromStock.cs"));
            Check(page.Contains("(fromStockLoadAll ? all:page).Where("),"page or all");
            Check(page.Contains("return fromStockMarketComplete ? CraftFromStock.Ranked("),"whole ranking when complete");
        }),
        ("Most profitable: value first, unknown last, the yield and the crafts counted",() =>
        {
            CraftFromStockRow Row(uint id,long count,int yield)=>new(new(id,id,yield,"Job",1,[new(Ore,1)]),count,false);
            var one=Row(1,1,1);var many=Row(2,3,3);var none=Row(3,9,1);
            Check(CraftFromStock.Worth(many,10)==90,"3 crafts of 3 at 10");
            var ranked=CraftFromStock.Ranked([none,one,many],row=>row==none ? null:CraftFromStock.Worth(row,10));
            Check(ranked[0]==many && ranked[1]==one && ranked[2]==none,"most valuable first, unknown last");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftFromStock.cs"));
            Check(page.Contains("missing.Take(TenshoAssistant.Market.MarketService.BatchMaximum)") && page.Contains("SourceOn(SourceUse.UniversalisToolPrices)"),"bounded, under the source switch");
        }),
        ("Chained crafts: from the ore alone, through every step, on one stock",() =>
        {
            // One part: 1 ingot and 1 rivet. Two parts: 2 ingots and one run of 2 rivets, so 3 ingots, 6 ore; three parts would need 10.
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item==Ore ? 8:0),Of)==2,"8 ore, 2 parts");
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item==Ore ? 3:0),Of)==0,"3 ore: none");
        }),
        ("Chained crafts: what is already made is used first",() =>
        {
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item switch { Ingot=>1,Rivet=>1,_=>0 }),Of)==1,"both in stock");
            Check(CraftFromStock.ChainedCount(PartRecipe,item=>Held(item switch { Rivet=>5,Ore=>4,_=>0 }),Of)==2,"rivets held, ingots from the ore");
        }),
        ("Chained crafts: a recipe loop and an endless chain stop",() =>
        {
            var a=Recipe(1,100,1,(101,1));var b=Recipe(2,101,1,(100,1));
            Check(CraftFromStock.ChainedCount(a,_=>Held(0),item=>item==100 ? a:item==101 ? b:null)==0,"a loop");
            RecipeDefinition? Deep(uint item)=>Recipe(item,item,1,(item+1,1));
            Check(CraftFromStock.ChainedCount(Recipe(9,1000,1,(1001,1)),_=>Held(0),Deep)==0,"endless, stopped by the depth");
        }),
        ("Chained crafts: the count is bounded, and a row says it comes from sub-recipes",() =>
        {
            Check(CraftFromStock.ChainedCount(Recipe(9,50,1,(Ore,1)),_=>Held(1000000),Of)==CraftFromStock.MaxChained,"the bound");
            var result=CraftFromStock.Find([PartRecipe],item=>Held(item==Ore ? 8:0),default,Of);
            Check(result.Rows.Single().Chained && result.Rows.Single().Count==2,"chained row");
            Check(CraftFromStock.Find([PartRecipe],item=>Held(item==Ore ? 8:0)).Rows.Count==0,"direct only without it");
        }),
    ];
}
