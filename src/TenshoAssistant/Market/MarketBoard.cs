using System.Text.Json;

namespace TenshoAssistant.Market;

// R-06 step 4 (0.270): every listing and recent sale of an item on a world, data center or region (Universalis
// /api/v2/{scope}/{item}, fields probed in docs/evidence/0.270-universalis-listings.md). A world scope gives no world name per
// listing: the scope name is used. Tax is the 5 % the buyer pays on top of the listing total.
public sealed record MarketOffer(long UnitPrice,int Quantity,bool Hq,string World,string Retainer,long Tax,DateTimeOffset? ReviewedAt)
{
    public long Total=>UnitPrice*Quantity;
    public long Cost=>Total+Tax;
}
public sealed record MarketSale(long UnitPrice,int Quantity,bool Hq,string World,DateTimeOffset At);
public sealed record MarketBoard(string Scope,uint ItemId,DateTimeOffset FetchedAt,IReadOnlyList<MarketOffer> Offers,IReadOnlyList<MarketSale> Sales,
    double? UnitsPerDay,double? NqUnitsPerDay,double? HqUnitsPerDay)
{
    public const int MaximumOffers=100,MaximumSales=100;

    public static MarketBoard Parse(string json,string scope,uint itemId,DateTimeOffset now)
    {
        using var document=JsonDocument.Parse(json);
        var root=document.RootElement;
        var offers=new List<MarketOffer>();
        if(root.TryGetProperty("listings",out var listings) && listings.ValueKind==JsonValueKind.Array)
            foreach(var row in listings.EnumerateArray().Take(MaximumOffers))
            {
                var price=Long(row,"pricePerUnit");var quantity=(int)Math.Clamp(Long(row,"quantity"),0,999_999);
                if(price<=0 || quantity<=0)continue;
                offers.Add(new(price,quantity,Bool(row,"hq"),Text(row,"worldName") ?? scope,Text(row,"retainerName") ?? "",Math.Max(0,Long(row,"tax")),
                    Long(row,"lastReviewTime") is > 0 and var reviewed ? DateTimeOffset.FromUnixTimeSeconds(reviewed):null));
            }
        var sales=new List<MarketSale>();
        if(root.TryGetProperty("recentHistory",out var history) && history.ValueKind==JsonValueKind.Array)
            foreach(var row in history.EnumerateArray().Take(MaximumSales))
            {
                var price=Long(row,"pricePerUnit");var quantity=(int)Math.Clamp(Long(row,"quantity"),0,999_999);var at=Long(row,"timestamp");
                if(price<=0 || quantity<=0 || at<=0)continue;
                sales.Add(new(price,quantity,Bool(row,"hq"),Text(row,"worldName") ?? scope,DateTimeOffset.FromUnixTimeSeconds(at)));
            }
        return new(scope,itemId,now,offers.OrderBy(offer=>offer.UnitPrice).ToArray(),sales.OrderByDescending(sale=>sale.At).ToArray(),
            Number(root,"regularSaleVelocity"),Number(root,"nqSaleVelocity"),Number(root,"hqSaleVelocity"));
    }

    private static long Long(JsonElement row,string name)
        =>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetInt64(out var number) ? number:0;
    private static double? Number(JsonElement row,string name)
        =>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number) && number>=0 ? number:null;
    private static bool Bool(JsonElement row,string name)=>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.True;
    private static string? Text(JsonElement row,string name)
        =>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.String && value.GetString() is { Length:>0 } text ? text:null;
}

/// <summary>Listing filters; null quality = both, 0 = no bound, empty world = every world.</summary>
public sealed record MarketFilter(bool? Hq=null,int MinQuantity=0,int MaxQuantity=0,long MinPrice=0,long MaxPrice=0,string World="")
{
    public bool Accepts(MarketOffer offer)=>(Hq is null || offer.Hq==Hq) && (MinQuantity<=0 || offer.Quantity>=MinQuantity)
        && (MaxQuantity<=0 || offer.Quantity<=MaxQuantity) && (MinPrice<=0 || offer.UnitPrice>=MinPrice) && (MaxPrice<=0 || offer.UnitPrice<=MaxPrice)
        && (World.Length==0 || offer.World==World);
    public bool Accepts(MarketSale sale)=>(Hq is null || sale.Hq==Hq) && (World.Length==0 || sale.World==World);
}

public sealed record MarketPlan(int Wanted,int Bought,long Cost,IReadOnlyList<MarketOffer> Offers);
public sealed record MarketSaleStats(int Sales,int Units,long Minimum,double Average,long Maximum);

public static class MarketBoardMath
{
    public const int MaximumWanted=9999;

    /// <summary>Cheapest set of whole listings (a market listing cannot be split) giving at least <paramref name="wanted"/> units, tax
    /// included; null when the listings do not hold enough units.</summary>
    public static MarketPlan? Cheapest(IReadOnlyList<MarketOffer> offers,int wanted)
    {
        if(wanted<=0 || wanted>MaximumWanted || offers.Count==0 || offers.Sum(offer=>(long)offer.Quantity)<wanted)return null;
        // 0/1 knapsack on units, capped at « wanted »: best[q] = lowest cost reaching q units. from[i][q] = the amount before offer i
        // when offer i was the last one to improve q (-1 otherwise); walking the offers backwards rebuilds the chosen listings.
        var best=new long[wanted+1];Array.Fill(best,long.MaxValue);best[0]=0;
        var from=new short[offers.Count][];
        for(var i=0;i<offers.Count;i++)
        {
            var row=from[i]=new short[wanted+1];Array.Fill(row,(short)-1);
            var offer=offers[i];
            for(var q=wanted;q>=0;q--)
            {
                if(best[q]==long.MaxValue)continue;
                var next=Math.Min(wanted,q+offer.Quantity);var cost=best[q]+offer.Cost;
                if(cost<best[next]){best[next]=cost;row[next]=(short)q;}
            }
        }
        var chosen=new List<MarketOffer>();var units=wanted;
        for(var i=offers.Count-1;i>=0 && units>0;i--)
        {
            if(from[i][units]<0)continue;
            chosen.Add(offers[i]);
            units=from[i][units];
        }
        return new(wanted,chosen.Sum(offer=>offer.Quantity),chosen.Sum(offer=>offer.Cost),chosen.OrderBy(offer=>offer.UnitPrice).ToArray());
    }

    /// <summary>Recent sales summary: lowest, unit average weighted by quantity, highest; null without sales.</summary>
    public static MarketSaleStats? Stats(IEnumerable<MarketSale> sales)
    {
        var list=sales.ToArray();
        if(list.Length==0)return null;
        var units=list.Sum(sale=>sale.Quantity);
        return new(list.Length,units,list.Min(sale=>sale.UnitPrice),(double)list.Sum(sale=>sale.UnitPrice*sale.Quantity)/units,list.Max(sale=>sale.UnitPrice));
    }
}
