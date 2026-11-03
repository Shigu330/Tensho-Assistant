namespace TenshoAssistant.Market;

/// <summary>One listing as the game sent it when the player opened an item's offers: price per unit, HQ, the retainer selling it.</summary>
public readonly record struct UndercutListing(uint PricePerUnit,bool Hq,ulong RetainerId);

/// <summary>
/// g, I-07 (0.346): the price to paste when listing an item — the lowest offer minus 1 gil, the player's own retainers left out (no
/// undercutting oneself); null when only the player's own offers (or none) are listed. Copied to the clipboard only: the player opens
/// the offers, pastes and confirms in the game (no automation, Dalamud rules). Idea from Penny Pincher (MIT); original code.
/// </summary>
public static class Undercut
{
    public static (uint Lowest,uint Paste,bool Hq)? Price(IEnumerable<UndercutListing> listings,IReadOnlySet<ulong> ownRetainers)
    {
        UndercutListing? lowest=null;
        foreach(var listing in listings)
            if(listing.PricePerUnit>0 && !ownRetainers.Contains(listing.RetainerId) && (lowest is not { } known || listing.PricePerUnit<known.PricePerUnit))
                lowest=listing;
        return lowest is { } found ? (found.PricePerUnit,Math.Max(1,found.PricePerUnit-1),found.Hq):null;
    }
}
