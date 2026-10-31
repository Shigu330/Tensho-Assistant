using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;

internal static class CraftChainTests
{
    private static ItemHoldingsSummary Stock(long quantity = 0) => new([], quantity, 0, true, false, false, true);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
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
    ];
}
