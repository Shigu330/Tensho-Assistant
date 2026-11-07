using System.Collections.Frozen;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

public enum CraftChainReason { RawMaterial, RecipeChoice, Cycle, DepthLimit, MissingIngredients }
public sealed record CraftChainMaterial(uint ItemId, long Amount, CraftChainReason Reason);
public sealed record CraftChainStep(RecipeDefinition Recipe, long Crafts, long Produced);
public sealed record CraftChainResult(long Requested, long Produced, IReadOnlyList<CraftChainStep> Steps,
    IReadOnlyList<CraftChainMaterial> Materials, IReadOnlyDictionary<uint, ItemHoldingsSummary> Observations)
{
    /// <summary>0.560 (C24): what is still to find of a material: what was missing at the calculation, less what came in since. Null when the chain does not need it or its stock was unknown.</summary>
    public long? Remaining(uint item,long stockNow)
    {
        var missing=Materials.Where(material=>material.ItemId==item).Sum(material=>material.Amount);
        if(missing<=0 || !Observations.TryGetValue(item,out var seen) || !seen.HasObservation)return null;
        return Math.Max(0,missing-Math.Max(0,stockNow-seen.Total));
    }
    public bool HasUnresolved => Materials.Any(material => material.Reason != CraftChainReason.RawMaterial);
    public bool HasUnknown => Observations.Values.Any(value => !value.HasObservation || value.HasUnknown);
    public bool HasCached => Observations.Values.Any(value => value.HasCached);
}

/// <summary>A bounded proposal, not a proof of recipe unlocks or successful crafting.</summary>
public static class CraftChain
{
    /// <summary>
    /// 0.591 (C18): the steps as a list to work through: one row per recipe with its total, each after the recipes that make its ingredients.
    /// Keeping the first position alone could put a recipe before an ingredient only its second batch needed, so the order is worked out
    /// again: among the rows whose ingredients are ready, the one that came first. Build's own steps stay unmerged.
    /// </summary>
    public static IReadOnlyList<CraftChainStep> Merged(IReadOnlyList<CraftChainStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var rows=new List<CraftChainStep>();var positions=new Dictionary<uint,int>();
        foreach(var step in steps)
        {
            if(positions.TryGetValue(step.Recipe.RecipeId,out var position))
                rows[position]=rows[position] with {Crafts=checked(rows[position].Crafts+step.Crafts),Produced=checked(rows[position].Produced+step.Produced)};
            else {positions[step.Recipe.RecipeId]=rows.Count;rows.Add(step);}
        }
        var makers=rows.Select(row=>row.Recipe.ResultItemId).ToHashSet();
        var ordered=new List<CraftChainStep>(rows.Count);var left=new List<CraftChainStep>(rows);var made=new HashSet<uint>();
        while(left.Count>0)
        {
            bool Ready(CraftChainStep row)=>row.Recipe.Ingredients.All(ingredient=>!makers.Contains(ingredient.ItemId) || made.Contains(ingredient.ItemId) || ingredient.ItemId==row.Recipe.ResultItemId);
            // Build refuses cycles: the fallback only keeps this loop finite on data it never produces.
            var next=left.FirstOrDefault(Ready) ?? left[0];
            left.Remove(next);ordered.Add(next);
            // Two recipes for one item both count once the first is placed: Build uses one recipe per item.
            made.Add(next.Recipe.ResultItemId);
        }
        return ordered.AsReadOnly();
    }

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
            if (++needs > maxNeeds) throw new InvalidOperationException("The chain exceeds the needs limit; reduce the quantity or choose a sub-recipe.");
            if (!available.TryGetValue(item, out var stock))
            {
                var observation = holdings(item);
                if (observation.Nq < 0 || observation.Hq < 0) throw new InvalidDataException("Invalid negative stock.");
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
                throw new ArgumentException("Recipe without a valid result, yield or ingredients.", nameof(root));
            var crafts = (amount - 1) / recipe.ResultAmount + 1;
            var produced = checked(crafts * recipe.ResultAmount);
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.ItemId == 0 || ingredient.Amount <= 0) throw new ArgumentException("Invalid ingredient.", nameof(root));
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
