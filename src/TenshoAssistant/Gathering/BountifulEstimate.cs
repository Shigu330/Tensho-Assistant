namespace TenshoAssistant.Gathering;

/// <summary>
/// Extra items of Bountiful Yield/Harvest II (0.224), estimated from the gathering rating against the item's base value
/// (ItemLevel.Gathering at the GatheringItemLevelConvertTable row of the item; docs/evidence/0.224-bountiful-base.md):
/// +3 from ⌊base × 1.1⌋, +2 from ⌊base × 0.9⌋, else +1. Community rule, not an official formula: shown as an estimate, never forced.
/// </summary>
public static class BountifulEstimate
{
    /// <summary>1–3, or null when the rating or the base is unknown.</summary>
    public static int? Bonus(int gathering, int baseGathering)
    {
        if (gathering <= 0 || baseGathering <= 0) return null;
        if (gathering >= (int)Math.Floor(baseGathering * 1.1)) return 3;
        if (gathering >= (int)Math.Floor(baseGathering * 0.9)) return 2;
        return 1;
    }

    /// <summary>Gathering rating needed for the next bonus step, or null when already at +3 or unknown.</summary>
    public static int? NextStep(int gathering, int baseGathering) => Bonus(gathering, baseGathering) switch
    {
        1 => (int)Math.Floor(baseGathering * 0.9),
        2 => (int)Math.Floor(baseGathering * 1.1),
        _ => null,
    };
}
