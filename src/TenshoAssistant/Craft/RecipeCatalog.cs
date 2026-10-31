using System.Collections.Frozen;

namespace TenshoAssistant.Craft;

public sealed record RecipeIngredient(uint ItemId, int Amount);

public sealed record RecipeDefinition(
    uint RecipeId,
    uint ResultItemId,
    int ResultAmount,
    string CraftType,
    int Level,
    IReadOnlyList<RecipeIngredient> Ingredients,
    uint CraftTypeId = uint.MaxValue,CraftRecipeParameters? Simulation=null);

public sealed class RecipeCatalog
{
    private readonly FrozenDictionary<uint, IReadOnlyList<RecipeDefinition>> byResult;
    private readonly FrozenDictionary<uint, IReadOnlyList<RecipeDefinition>> byIngredient;

    public int Count { get; }
    public string Source { get; }

    private RecipeCatalog(string source, Dictionary<uint, List<RecipeDefinition>> recipes, CancellationToken cancellation)
    {
        Source = source;
        byResult = recipes.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlyList<RecipeDefinition>)Array.AsReadOnly(pair.Value.ToArray()));
        var ingredients = new Dictionary<uint, List<RecipeDefinition>>();
        foreach (var recipe in byResult.Values.SelectMany(static values => values))
        {
            cancellation.ThrowIfCancellationRequested();
            foreach (var itemId in recipe.Ingredients.Select(static ingredient => ingredient.ItemId).Distinct())
            {
                if (!ingredients.TryGetValue(itemId, out var list)) ingredients[itemId] = list = [];
                list.Add(recipe);
            }
        }
        byIngredient = ingredients.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlyList<RecipeDefinition>)Array.AsReadOnly(pair.Value.ToArray()));
        Count = byResult.Values.Sum(static recipes => recipes.Count);
    }

    public static RecipeCatalog Build(IEnumerable<RecipeDefinition> source, string dataVersion, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataVersion);
        cancellation.ThrowIfCancellationRequested();
        var recipes = new Dictionary<uint, List<RecipeDefinition>>();
        var ids = new HashSet<uint>();
        foreach (var recipe in source)
        {
            cancellation.ThrowIfCancellationRequested();
            if (recipe.RecipeId == 0 || recipe.ResultItemId == 0 || recipe.ResultAmount <= 0) continue;
            if (!ids.Add(recipe.RecipeId)) throw new ArgumentException("Duplicate recipe ID.", nameof(source));
            if (recipe.Ingredients.Any(ingredient => ingredient.ItemId == 0 || ingredient.Amount <= 0))
                throw new ArgumentException("Recipe ingredients must be positive and identified.", nameof(source));
            if (!recipes.TryGetValue(recipe.ResultItemId, out var list)) recipes[recipe.ResultItemId] = list = [];
            list.Add(recipe with { Ingredients = Array.AsReadOnly(recipe.Ingredients.ToArray()) });
        }
        var catalog = new RecipeCatalog(dataVersion, recipes, cancellation);
        cancellation.ThrowIfCancellationRequested();
        return catalog;
    }

    public IReadOnlyList<RecipeDefinition> ForResult(uint itemId) => byResult.GetValueOrDefault(itemId) ?? [];
    public IReadOnlyList<RecipeDefinition> ForIngredient(uint itemId) => byIngredient.GetValueOrDefault(itemId) ?? [];
}

public sealed record RecipeCatalogState(RecipeCatalog? Catalog, int RowsCopied, int TotalRows, string? Error);

public sealed class RecipeCatalogService
{
    private RecipeCatalogState state = new(null, 0, 0, null);
    public RecipeCatalogState State => Volatile.Read(ref state);
    internal void Progress(int copied, int total) => Interlocked.Exchange(ref state, new(null, copied, total, null));
    internal void Publish(RecipeCatalog catalog) => Interlocked.Exchange(ref state, new(catalog, 0, 0, null));
    internal void Fail(Exception error) => Interlocked.Exchange(ref state, new(null, 0, 0, error.Message));
    internal void Clear() => Interlocked.Exchange(ref state, new(null, 0, 0, null));
}

