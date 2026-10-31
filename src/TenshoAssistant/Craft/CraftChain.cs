using System.Collections.Frozen;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

public enum CraftChainReason { RawMaterial, RecipeChoice, Cycle, DepthLimit, MissingIngredients }
public sealed record CraftChainMaterial(uint ItemId, long Amount, CraftChainReason Reason);
public sealed record CraftChainStep(RecipeDefinition Recipe, long Crafts, long Produced);
public sealed record CraftChainResult(long Requested, long Produced, IReadOnlyList<CraftChainStep> Steps,
    IReadOnlyList<CraftChainMaterial> Materials, IReadOnlyDictionary<uint, ItemHoldingsSummary> Observations)
{
    public bool HasUnresolved => Materials.Any(material => material.Reason != CraftChainReason.RawMaterial);
    public bool HasUnknown => Observations.Values.Any(value => !value.HasObservation || value.HasUnknown);
    public bool HasCached => Observations.Values.Any(value => value.HasCached);
}

/// <summary>A bounded proposal, not a proof of recipe unlocks or successful crafting.</summary>
public static class CraftChain
{
    public static CraftChainResult Build(RecipeDefinition root, long requested, RecipeCatalog catalog,
        Func<uint, ItemHoldingsSummary> holdings, CancellationToken cancellation = default,
        int maxDepth = 12, int maxNeeds = 2048, IReadOnlyDictionary<uint, uint>? recipeChoices = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(holdings);
        if (requested < 1 || maxDepth is < 1 or > 32 || maxNeeds is < 1 or > 100000)
            throw new ArgumentOutOfRangeException(nameof(requested));
        cancellation.ThrowIfCancellationRequested();
        var available = new Dictionary<uint, long>();
        var choices = recipeChoices?.ToFrozenDictionary();
        var observations = new Dictionary<uint, ItemHoldingsSummary>();
        var materials = new Dictionary<(uint Item, CraftChainReason Reason), long>();
        var steps = new List<CraftChainStep>();
        var path = new HashSet<uint> { root.ResultItemId };
        var needs = 0;

        void Acquire(uint item, long amount, CraftChainReason reason) =>
            materials[(item, reason)] = checked(materials.GetValueOrDefault((item, reason)) + amount);

        void Need(uint item, long amount, int depth)
        {
            cancellation.ThrowIfCancellationRequested();
            if (++needs > maxNeeds) throw new InvalidOperationException("La chaîne dépasse la limite de besoins ; réduisez la quantité ou choisissez une sous-recette.");
            if (!available.TryGetValue(item, out var stock))
            {
                var observation = holdings(item);
                if (observation.Nq < 0 || observation.Hq < 0) throw new InvalidDataException("Stock négatif invalide.");
                observations[item] = observation with { Rows = Array.AsReadOnly(observation.Rows.ToArray()) };
                stock = observation.HasObservation ? observation.Total : 0;
            }
            var consumed = Math.Min(stock, amount);
            available[item] = stock - consumed;
            var missing = amount - consumed;
            if (missing == 0) return;
            if (path.Contains(item)) { Acquire(item, missing, CraftChainReason.Cycle); return; }
            var variants = catalog.ForResult(item);
            RecipeDefinition? recipe;
            if (choices is not null && choices.TryGetValue(item, out var selected))
            {
                recipe = variants.FirstOrDefault(variant => variant.RecipeId == selected);
                if (recipe is null) { Acquire(item, missing, CraftChainReason.RecipeChoice); return; }
            }
            else
            {
                if (variants.Count == 0) { Acquire(item, missing, CraftChainReason.RawMaterial); return; }
                if (variants.Count != 1) { Acquire(item, missing, CraftChainReason.RecipeChoice); return; }
                recipe = variants[0];
            }
            if (depth >= maxDepth) { Acquire(item, missing, CraftChainReason.DepthLimit); return; }
            if (recipe.Ingredients.Count == 0) { Acquire(item, missing, CraftChainReason.MissingIngredients); return; }
            path.Add(item);
            try
            {
                var produced = Produce(recipe, missing, depth);
                available[item] = checked(available[item] + produced - missing);
            }
            finally { path.Remove(item); }
        }

        long Produce(RecipeDefinition recipe, long amount, int depth)
        {
            cancellation.ThrowIfCancellationRequested();
            if (recipe.RecipeId == 0 || recipe.ResultItemId == 0 || recipe.ResultAmount <= 0 || recipe.Ingredients.Count == 0)
                throw new ArgumentException("Recette sans résultat, rendement ou ingrédients valides.", nameof(root));
            var crafts = (amount - 1) / recipe.ResultAmount + 1;
            var produced = checked(crafts * recipe.ResultAmount);
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.ItemId == 0 || ingredient.Amount <= 0) throw new ArgumentException("Ingrédient invalide.", nameof(root));
                Need(ingredient.ItemId, checked(crafts * ingredient.Amount), depth + 1);
            }
            // Do not merge repeated steps: this order preserves all dependencies.
            steps.Add(new(recipe with { Ingredients = Array.AsReadOnly(recipe.Ingredients.ToArray()) }, crafts, produced));
            return produced;
        }

        var total = Produce(root, requested, 0);
        cancellation.ThrowIfCancellationRequested();
        return new(requested, total, Array.AsReadOnly(steps.ToArray()),
            Array.AsReadOnly(materials.OrderBy(pair => pair.Key.Item).ThenBy(pair => pair.Key.Reason)
                .Select(pair => new CraftChainMaterial(pair.Key.Item, pair.Value, pair.Key.Reason)).ToArray()), observations.ToFrozenDictionary());
    }
}
