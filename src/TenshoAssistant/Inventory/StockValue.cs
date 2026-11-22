using TenshoAssistant.Market;

namespace TenshoAssistant.Inventory;

/// <summary>X-207: a market unit price for a stock (gil) and the date of the data it comes from (null: not dated).</summary>
public readonly record struct StockPrice(long Unit,DateTimeOffset? At);

/// <summary>
/// X-207: the estimated value of one place (a storage, or a character's storages together): <paramref name="Value"/> the stacks priced;
/// <paramref name="Priced"/> how many distinct items (and quality) have a price; <paramref name="Unpriced"/> those the market could sell
/// but for which no price is known (left out of the value, counted apart, never 0); <paramref name="NotSold"/> those the market does not
/// take (untradable, or not on the board); <paramref name="Stale"/> the priced ones whose data is older than <see cref="StockValue.OldAfter"/>,
/// <paramref name="Oldest"/> the oldest date used.
/// </summary>
public sealed record StockValueLine(string Place,long Value,int Priced,int Unpriced,int NotSold,int Stale,DateTimeOffset? Oldest);

/// <summary>
/// X-207 (part 2): one item (and quality) that sits in the storages and sells on the world: <paramref name="Quantity"/> held in all,
/// <paramref name="Places"/> where (in the order met), <paramref name="Sold"/> what one unit really sold for (MarketQuote.Sold: the lower
/// of the last and the average sale), <paramref name="PerDay"/> the units sold a day, <paramref name="PerDayGil"/> sold price × sales a
/// day (what the market moves of it each day), <paramref name="NpcPrice"/> what an NPC pays for one (0: none known).
/// </summary>
/// <param name="NpcBuy">X-797 (0.1810): the lowest price an NPC sells one piece for, in gil (normal quality); null when no NPC sells it.</param>
public sealed record StockSleeper(uint ItemId,bool Hq,long Quantity,IReadOnlyList<string> Places,long Sold,decimal PerDay,long PerDayGil,uint NpcPrice,uint? NpcBuy=null);

/// <summary>
/// X-207 (the user's request of 12/11, the comparison with other plugins K-14): the estimated value of the stocks, per storage and per
/// character, at the market prices asked on the player's click only (DEC-048). Advice only: an estimate at the lowest listing, nothing sold.
/// Pure (tests).
/// </summary>
public static class StockValue
{
    /// <summary>A price older than this is said old (the same day as the market tooltip's « recent upload »).</summary>
    public static readonly TimeSpan OldAfter=TimeSpan.FromHours(24);

    /// <summary>
    /// One place: each stack at its quality's unit price × its quantity. An item the market does not take is counted apart; an item it takes
    /// without a price for that quality is counted apart too, and left out of the value (never valued 0, R06).
    /// </summary>
    public static StockValueLine Of(string place,IEnumerable<InventoryStack> stacks,Func<uint,bool> marketable,Func<uint,bool,StockPrice?> price,DateTimeOffset now)
    {
        long value=0;
        var priced=new HashSet<InventoryItem>();var unpriced=new HashSet<InventoryItem>();var notSold=new HashSet<InventoryItem>();var stale=new HashSet<InventoryItem>();
        DateTimeOffset? oldest=null;
        foreach(var stack in stacks)
        {
            if(stack.Quantity<=0 || stack.Item.Id.Value==0)continue;
            if(!marketable(stack.Item.Id.Value)){notSold.Add(stack.Item);continue;}
            if(price(stack.Item.Id.Value,stack.Item.IsHighQuality) is not { Unit:>0 } known){unpriced.Add(stack.Item);continue;}
            value=checked(value+known.Unit*stack.Quantity);
            priced.Add(stack.Item);
            if(known.At is not { } at || now-at>OldAfter)stale.Add(stack.Item);
            if(known.At is { } dated && (oldest is null || dated<oldest))oldest=dated;
        }
        return new(place,value,priced.Count,unpriced.Count,notSold.Count,stale.Count,oldest);
    }

    /// <summary>Several places together (a character's storages): the values and counts added, the oldest date kept.</summary>
    public static StockValueLine Sum(string place,IEnumerable<StockValueLine> lines)
    {
        long value=0;int priced=0,unpriced=0,notSold=0,stale=0;DateTimeOffset? oldest=null;
        foreach(var line in lines)
        {
            value=checked(value+line.Value);priced+=line.Priced;unpriced+=line.Unpriced;notSold+=line.NotSold;stale+=line.Stale;
            if(line.Oldest is { } at && (oldest is null || at<oldest))oldest=at;
        }
        return new(place,value,priced,unpriced,notSold,stale,oldest);
    }

    /// <summary>
    /// The unit price of a quality from a world's quote: its lowest listing (dated by the world's data), else its average sale price (dated by
    /// the last sale); null when the quote holds neither.
    /// </summary>
    public static StockPrice? FromQuote(MarketQuote? quote,bool hq)
    {
        if(quote is null)return null;
        var quality=hq ? quote.Hq:quote.Nq;
        if(quality.Minimum is > 0 and var minimum)return new((long)Math.Round(minimum),quote.UploadedAt);
        if(quality.Average is > 0 and var average)return new((long)Math.Round(average),quality.SaleAt ?? quote.UploadedAt);
        return null;
    }

    /// <summary>
    /// X-207 (part 2): « sits and sells »: the items held in the storages that the market takes and that sell on the world, the first
    /// <paramref name="most"/> by sold price × sales a day, then by sold price. An item that does not sell (0 a day) is not listed; one whose
    /// sold price or speed is not known is counted apart (<c>Unknown</c>), never taken as 0 (R06). Advice only.
    /// </summary>
    public static (IReadOnlyList<StockSleeper> Lines,int Unknown) Sleepers(IEnumerable<(string Place,InventoryStack Stack)> stacks,Func<uint,bool> marketable,
        Func<uint,bool,(decimal Price,decimal? PerDay)?> sold,Func<uint,uint> npcPrice,int most,Func<uint,uint?>? npcBuy=null)
    {
        var held=new Dictionary<(uint Id,bool Hq),(long Quantity,List<string> Places)>();
        foreach(var (place,stack) in stacks)
        {
            if(stack.Quantity<=0 || stack.Item.Id.Value==0 || !marketable(stack.Item.Id.Value))continue;
            var key=(stack.Item.Id.Value,stack.Item.IsHighQuality);
            var (quantity,places)=held.TryGetValue(key,out var known) ? known:(0L,new List<string>());
            if(!places.Contains(place))places.Add(place);
            held[key]=(checked(quantity+stack.Quantity),places);
        }
        var lines=new List<StockSleeper>();var unknown=0;
        foreach(var ((id,hq),(quantity,places)) in held)
        {
            if(sold(id,hq) is not { Price:>0 } price || price.PerDay is not { } perDay){unknown++;continue;}
            if(perDay<=0)continue;
            lines.Add(new(id,hq,quantity,places,(long)Math.Round(price.Price),perDay,(long)Math.Round(price.Price*perDay),npcPrice(id),npcBuy?.Invoke(id)));
        }
        return ([..lines.OrderByDescending(line=>line.PerDayGil).ThenByDescending(line=>line.Sold).ThenBy(line=>line.ItemId).ThenBy(line=>line.Hq).Take(Math.Max(0,most))],unknown);
    }

    /// <summary>
    /// X-207: the NPC's price against the market's for one unit, advice only: how many times more the market pays (≥ 1.0), or that the NPC
    /// pays more (&lt; 1.0); null when either price is unknown or 0.
    /// </summary>
    public static double? MarketOverNpc(uint npcPrice,long? marketUnit)=>npcPrice>0 && marketUnit is > 0 and var unit ? (double)unit/npcPrice:null;

    /// <summary>
    /// X-797 (0.1810, the user's test of 22/11: dark matter at 400 gil on the market, 120 gil at an NPC): the market's price of one piece less
    /// the NPC's selling price (buying from an NPC): above 0, the NPC is that much cheaper; below 0, the market is; null when either is unknown
    /// or 0. The NPC sells normal quality: compared with the market's normal quality.
    /// </summary>
    public static long? MarketOverNpcBuy(uint? npcBuy,long? marketUnit)=>npcBuy is > 0 and var buy && marketUnit is > 0 and var unit ? unit-buy:null;
}
