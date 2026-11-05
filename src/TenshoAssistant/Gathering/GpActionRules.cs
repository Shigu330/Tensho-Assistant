namespace TenshoAssistant.Gathering;

/// <summary>The gathering actions the player does not want planned, as for crafting macros. One key covers the miner's action and the botanist's twin.</summary>
public static class GpActionRules
{
    public static string Key(GpActionSpec spec)=>$"{spec.Effect}:{spec.Magnitude}";
    public static string Joined(IEnumerable<string>? keys)=>string.Join('|',(keys ?? []).Where(key=>!string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    public static bool Forbidden(string? joined,GpActionSpec spec)=>!string.IsNullOrEmpty(joined) && joined.Split('|').Contains(Key(spec),StringComparer.Ordinal);
    /// <summary>Each key once, in the catalogue's order, with the miner's and the botanist's action.</summary>
    public static IReadOnlyList<(string Key,GpAction? Miner,GpAction? Botanist)> Groups(GpActionCatalog catalog)
        =>catalog.Actions.GroupBy(action=>Key(action.Spec)).Select(group=>(group.Key,group.FirstOrDefault(action=>action.Spec.JobId==16),group.FirstOrDefault(action=>action.Spec.JobId==17))).ToArray();
}
