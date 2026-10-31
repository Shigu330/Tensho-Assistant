using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

public sealed record CraftPlanMaterial(uint ItemId, long Required, ItemHoldingsSummary Holdings)
{
    public long Uncovered => Math.Max(0, Required - Holdings.Total);
}

public sealed record CraftPlan(long Requested, long Crafts, long Produced, IReadOnlyList<CraftPlanMaterial> Materials)
{
    public static CraftPlan Build(RecipeDefinition recipe, int requested, Func<uint, ItemHoldingsSummary> holdings)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(holdings);
        if (requested < 1 || recipe.ResultAmount < 1) throw new ArgumentOutOfRangeException(nameof(requested));
        var crafts = ((long)requested + recipe.ResultAmount - 1) / recipe.ResultAmount;
        var requirements = new Dictionary<uint, long>();
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.ItemId == 0 || ingredient.Amount <= 0) throw new ArgumentException("Invalid ingredient.", nameof(recipe));
            requirements[ingredient.ItemId] = checked(requirements.GetValueOrDefault(ingredient.ItemId) + checked(crafts * ingredient.Amount));
        }
        return new(requested, crafts, checked(crafts * recipe.ResultAmount),
            Array.AsReadOnly(requirements.Select(pair => new CraftPlanMaterial(pair.Key, pair.Value, holdings(pair.Key))).ToArray()));
    }
}
