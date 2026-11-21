namespace TenshoAssistant.Craft;

/// <summary>X-684 (0.1692): what a recipe's tooltip says: the crafter's icon, the item made (and how many), the crafter and the level, then the
/// recipe's numbers (durability, progress, quality), the minimum craftsmanship and control, what it allows and asks (RecipeFacts), whether you
/// crafted it before, and its ingredients.</summary>
public sealed record RecipeTipFacts(string Name,uint Icon,string? Job,IReadOnlyList<string> Lines);

public static class RecipeTips
{
    /// <summary>Ingredients named before « and n more ».</summary>
    public const int MaxIngredients=6;

    /// <summary>The recipe's numbers that the game gives (0 left out), keyed by the end of a CraftSequence language key.</summary>
    public static IReadOnlyList<(string Key,int Value)> Stats(CraftRecipeParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var stats=new List<(string,int)>();
        if(p.Durability>0)stats.Add(("Durability",p.Durability));
        if(p.Progress>0)stats.Add(("Progress",p.Progress));
        if(p.Quality>0)stats.Add(("Quality",p.Quality));
        return stats;
    }

    /// <summary>The minimum attributes the recipe asks (0: none), keyed by the end of a CraftSimulation language key.</summary>
    public static IReadOnlyList<(string Key,int Value)> Minimums(CraftRecipeParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var minimums=new List<(string,int)>();
        if(p.RequiredCraftsmanship>0)minimums.Add(("Craftsmanship",p.RequiredCraftsmanship));
        if(p.RequiredControl>0)minimums.Add(("Control",p.RequiredControl));
        return minimums;
    }

    /// <summary>The facts with the name trimmed and the lines cleaned (blanks left out, each once).</summary>
    public static RecipeTipFacts Build(string name,uint icon,string? job,IEnumerable<string?> lines)
    {
        var kept=new List<string>();
        foreach(var line in lines)
            if(line is { } text && text.Trim() is { Length:>0 } trimmed && !kept.Contains(trimmed))kept.Add(trimmed);
        return new(name.Trim(),icon,job is { } value && value.Trim() is { Length:>0 } shown ? shown:null,kept);
    }
}
