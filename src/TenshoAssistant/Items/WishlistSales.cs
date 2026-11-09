using TenshoAssistant.Market;

namespace TenshoAssistant.Items;

/// <summary>
/// X-101 (0.1000, DEC-048): the wishes on sale under the player's own price. Off by default; when on, at most one check every 30 minutes, only the
/// wishes with a price, 20 items a check, one Universalis request (MarketService.FillCacheAsync). A wish is said once each time it goes under its price.
/// </summary>
public static class WishlistSales
{
    public const int PerCheck=20;
    public static readonly TimeSpan Every=TimeSpan.FromMinutes(30);

    /// <summary>The next items to check, in turn from where the previous check stopped.</summary>
    public static IReadOnlyList<uint> Batch(IReadOnlyList<uint>? wishes,IReadOnlyDictionary<uint,long>? prices,ref int offset)
    {
        var priced=(wishes ?? []).Where(id=>prices?.GetValueOrDefault(id) is > 0).Distinct().ToArray();
        if(priced.Length==0){offset=0;return [];}
        offset%=priced.Length;
        var batch=priced.Skip(offset).Concat(priced.Take(offset)).Take(PerCheck).ToArray();
        offset=(offset+batch.Length)%priced.Length;
        return batch;
    }

    /// <summary>The cheapest listing (NQ or HQ) of a quote, or null when none.</summary>
    public static MarketListing? Cheapest(MarketQuote? quote)
        =>new[]{quote?.NqCheapest,quote?.HqCheapest}.Where(listing=>listing is { Price:>0 }).MinBy(listing=>listing!.Price);

    /// <summary>Whether to say it now: under the price, and not already said since it last went over it (said holds the items said).</summary>
    public static bool Say(uint item,MarketListing? cheapest,long price,HashSet<uint> said)
    {
        if(cheapest is null || cheapest.Price>price){said.Remove(item);return false;}
        return said.Add(item);
    }
}
