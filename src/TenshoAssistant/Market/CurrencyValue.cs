namespace TenshoAssistant.Market;

/// <summary>One thing a currency buys: the item, how many come, its price in that currency, the market price per item (null = unknown).</summary>
public sealed record CurrencyBuy(uint ItemId,uint Quantity,uint Cost,bool Hq,decimal? MarketPrice)
{
    /// <summary>Gil the player gets per unit of currency by selling what it buys at the lowest market price; null when unknown.</summary>
    public double? GilPerUnit=>MarketPrice is { } price && price>0 && Cost>0 ? (double)(price*Quantity)/Cost:null;
}

/// <summary>
/// g, I-13 (0.354): « what to do with my seals or marks » — the items a currency buys, ranked by the gil each unit of it is worth on
/// the market (unknown prices last, never counted as zero). Original code (ideas from Currency Spender, read only).
/// </summary>
public static class CurrencyValue
{
    public static IReadOnlyList<CurrencyBuy> Rank(IEnumerable<CurrencyBuy> buys)
        =>buys.OrderBy(buy=>buy.GilPerUnit is null).ThenByDescending(buy=>buy.GilPerUnit ?? 0).ThenBy(buy=>buy.Cost).ThenBy(buy=>buy.ItemId).ToArray();
}
