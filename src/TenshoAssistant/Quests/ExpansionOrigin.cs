namespace TenshoAssistant.Quests;

/// <summary>
/// X-89 (0.970, user request: the origin expansion in the tooltips). The game gives no expansion for an item; it is shown only when every sure
/// source of it gives one same expansion: the zones where it is gathered or fished (TerritoryType.ExVersion) and the quests that give it
/// (Quest.Expansion). A source whose expansion is unknown, or two sources that disagree, give nothing (never guessed).
/// </summary>
public static class ExpansionOrigin
{
    /// <summary>From: 1 = gathering or fishing zones, 2 = quests, 3 = both.</summary>
    public static (string Name,int From)? Of(IReadOnlyCollection<string?> zoneExpansions,IReadOnlyCollection<string?> questExpansions)
    {
        if(zoneExpansions.Count+questExpansions.Count==0)return null;
        if(zoneExpansions.Concat(questExpansions).Any(name=>string.IsNullOrWhiteSpace(name)))return null;
        var names=zoneExpansions.Concat(questExpansions).Select(name=>name!.Trim()).Distinct(StringComparer.Ordinal).Take(2).ToArray();
        if(names.Length!=1)return null;
        return (names[0],(zoneExpansions.Count>0 ? 1:0)|(questExpansions.Count>0 ? 2:0));
    }
}
