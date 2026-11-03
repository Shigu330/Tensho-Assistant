namespace TenshoAssistant.Market;

/// <summary>
/// A-06 (0.431, user's choice in GAPS): crafting profit — the crafted item's lowest listing on the world (or, as an option, the cheapest
/// of the data center) × the quantity produced, minus the purchases' cost of the preparation; the last sale is shown beside it.
/// Unknown stays unknown: no price, or a cost not fully known, gives no profit (a partial cost is said, never filled in).
/// </summary>
public sealed record CraftProfit(decimal? UnitPrice,uint? PriceWorld,decimal? LastSale,decimal? Revenue,decimal? Profit,bool CostComplete);

public static class CraftProfits
{
    public static CraftProfit Compute(MarketQuote? product,bool hq,long produced,decimal? cost,bool costComplete,bool dataCenter)
    {
        if(product is null || produced<=0)return new(null,null,null,null,null,costComplete);
        var prices=hq ? product.Hq:product.Nq;
        var cheapest=hq ? product.HqCheapest:product.NqCheapest;
        decimal? unit=dataCenter ? cheapest?.Price:prices.Minimum;
        uint? world=dataCenter ? cheapest?.WorldId:product.WorldId;
        var revenue=unit is { } price ? price*produced:(decimal?)null;
        var profit=revenue is { } gained && cost is { } spent && costComplete ? gained-spent:(decimal?)null;
        return new(unit,unit is null ? null:world,prices.RecentSale,revenue,profit,costComplete);
    }
}
