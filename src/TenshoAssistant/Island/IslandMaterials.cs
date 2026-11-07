namespace TenshoAssistant.Island;

public sealed record IslandMaterialTotal(uint ItemId,string Name,string Category,int Amount);

/// <summary>
/// N-07 (0.673): the materials of a craft and of an agenda, from the game's sheets. An efficient craft counts once: it makes 2 units for the materials of one, a
/// guide's statement to compare in game. The island's stocks are not read: docs/evidence/0.673-island-materials.md.
/// </summary>
public static class IslandMaterials
{
    /// <summary>The slots a craft uses. Pouch row 0 is a real material (Island Palm Leaf) and also what an empty slot holds: only the amount tells them apart.</summary>
    public static IReadOnlyList<(uint Row,int Amount)> Used(IEnumerable<(uint Row,int Amount)> slots)=>slots.Where(slot=>slot.Amount>0).ToArray();

    /// <summary>One line per material, by category then name, the same order whatever the agenda's order.</summary>
    public static IReadOnlyList<IslandMaterialTotal> Totals(IslandAgenda agenda)
        =>agenda.Chains.SelectMany(chain=>chain.Steps).SelectMany(step=>step.Craft.Materials ?? []).GroupBy(item=>item.ItemId)
            .Select(group=>new IslandMaterialTotal(group.Key,group.First().Name,group.First().Category,group.Sum(item=>item.Amount)))
            .OrderBy(item=>item.Category,StringComparer.Ordinal).ThenBy(item=>item.Name,StringComparer.Ordinal).ThenBy(item=>item.ItemId).ToArray();

    /// <summary>A step whose materials the sheets did not give, so that a total is not taken for complete.</summary>
    public static bool Complete(IslandAgenda agenda)=>agenda.Chains.SelectMany(chain=>chain.Steps).All(step=>step.Craft.Materials is { Count:>0 });
}
