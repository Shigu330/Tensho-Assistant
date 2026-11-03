namespace TenshoAssistant.Craft;

/// <summary>
/// 0.438 (user request): crafting actions the player forbids (Settings › Crafting). Kept as a key — the sorted names of the forbidden
/// actions joined by commas — so the search inputs compare by value. A forbidden action is never proposed: rotation search, crafting
/// log assistant (its saved sequences too), live synthesis rows; the manual simulator greys it out; imports and recordings say so.
/// </summary>
public static class CraftActionRules
{
    public static string Key(IEnumerable<string>? names)
        =>string.Join(',',(names ?? []).Select(name=>Enum.TryParse<CraftActionKind>(name,false,out var kind) && Enum.IsDefined(kind) ? kind.ToString():null)
            .OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    public static IReadOnlySet<CraftActionKind> Parse(string? key)
        =>(key ?? "").Split(',',StringSplitOptions.RemoveEmptyEntries).Select(name=>Enum.TryParse<CraftActionKind>(name,false,out var kind) ? kind:(CraftActionKind?)null)
            .OfType<CraftActionKind>().ToHashSet();

    public static bool Forbidden(string? key,CraftActionKind kind)=>!string.IsNullOrEmpty(key) && Parse(key).Contains(kind);

    public static bool UsesForbidden(IEnumerable<CraftActionKind> actions,string? key)
    {
        if(string.IsNullOrEmpty(key))return false;
        var forbidden=Parse(key);
        return actions.Any(forbidden.Contains);
    }
}
