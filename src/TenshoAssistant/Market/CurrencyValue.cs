namespace TenshoAssistant.Market;

/// <summary>One thing a currency buys: the item, how many come, its price in that currency, the market price per item (null = unknown).</summary>
public sealed record CurrencyBuy(uint ItemId,uint Quantity,uint Cost,bool Hq,decimal? MarketPrice,decimal? PerDay=null)
{
    /// <summary>
    /// 0.794 (X-33, user request: like « What can I craft? », count the sales per day): how many of these items can sell in that many days at the market's speed,
    /// and no more than the balance buys when it is known. Null: speed unknown, which is not zero.
    /// </summary>
    public long? Sellable(long? balance,int days)
    {
        if(PerDay is not { } speed)return null;
        // Rounded first, as in CraftFromStock.Sellable: one sale in seven days is 0.14285715 a day, and 7 days of it are one piece.
        var bySpeed=(long)Math.Floor(Math.Round(Math.Max(0,speed)*Math.Max(1,days),2));
        return balance is { } amount ? Math.Min(bySpeed,Math.Max(0,amount)/Math.Max(1,Cost)*Quantity):bySpeed;
    }
    /// <summary>The gil those sales bring; null when the price or the speed is unknown.</summary>
    public decimal? Worth(long? balance,int days)=>MarketPrice is { } price && Sellable(balance,days) is { } pieces ? pieces*price:null;

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

    /// <summary>
    /// 0.794 (X-33, user report: a single odd sale put an item first): most gil in that many days of sales first, unknown last; at equal gil, the gil per unit.
    /// </summary>
    public static IReadOnlyList<CurrencyBuy> RankBySales(IEnumerable<CurrencyBuy> buys,long? balance,int days)
        =>buys.Select(buy=>(Buy:buy,Worth:buy.Worth(balance,days))).OrderBy(entry=>entry.Worth is null).ThenByDescending(entry=>entry.Worth ?? 0)
            .ThenByDescending(entry=>entry.Buy.GilPerUnit ?? 0).ThenBy(entry=>entry.Buy.Cost).ThenBy(entry=>entry.Buy.ItemId).Select(entry=>entry.Buy).ToArray();
}
