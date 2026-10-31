namespace TenshoAssistant.Inventory;

/// <summary>Managed facts copied from a container header, without retaining native addresses.</summary>
internal readonly record struct InventoryReadEvidence(bool IsLoaded, bool HasItems, bool MatchesType, int Size)
{
    // Sanity bound, not an assertion that all bags have a hard-coded slot count.
    public bool CanRead => IsLoaded && HasItems && MatchesType && Size is > 0 and <= 1000;

    public static InventoryCoverage GetCoverage(InventoryReadEvidence before, InventoryReadEvidence after,
        int slotsRead, bool sameBuffer)
    {
        if (!before.CanRead || !after.CanRead) return InventoryCoverage.Unknown;
        return sameBuffer && before.Size == after.Size && slotsRead == before.Size
            ? InventoryCoverage.Complete : InventoryCoverage.Partial;
    }
}
