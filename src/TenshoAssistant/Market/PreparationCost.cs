using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

public enum PreparationCostBasis { Missing, AllRequired, TerminalChain }
public enum PreparationPriceQuality { Nq, Hq, Cheapest }
public enum PreparationCostStatus { Covered, Priced, UnknownStock, UnknownWorld, UnknownItem, NotMarketable, UnsupportedQuality, MissingQuote, WrongScope, MissingPrice, Overflow, UnresolvedChain }
public sealed record PreparationCostDemand(uint ItemId, long Required, long? Quantity, bool StockUncertain, bool CachedStock, bool Unresolved = false);
public sealed record PreparationCostLine(uint ItemId, long Required, long? Quantity, PreparationCostStatus Status,
    decimal? UnitPrice, decimal? Cost, bool? HighQuality, MarketQuote? Quote, bool StockUncertain, bool CachedStock, bool StalePrice);
public sealed record PreparationCostEstimate(uint WorldId, PreparationCostBasis Basis, PreparationPriceQuality Quality,
    DateTimeOffset EvaluatedAt, IReadOnlyList<PreparationCostLine> Lines, decimal? KnownSubtotal, bool InputStockUncertain = false, bool InputCachedStock = false)
{
    public bool AllCosted => KnownSubtotal is not null && Lines.All(line => line.Cost is not null);
    public bool StockUncertain => InputStockUncertain || Lines.Any(line => line.StockUncertain);
    public bool CachedStock => InputCachedStock || Lines.Any(line => line.CachedStock);
    public bool StalePrices => Lines.Any(line => line.StalePrice);
}

/// <summary>Unit reference price times indicative quantity; not an executable basket, listing supply or tax calculation.</summary>
public static class PreparationCost
{
    public static PreparationCostEstimate Build(CraftPlan plan, uint world, PreparationCostBasis basis, PreparationPriceQuality quality,
        Func<uint,ItemDefinition?> item, Func<uint,MarketQuote?> quote, DateTimeOffset now,TimeSpan? cacheLifetime=null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (basis is not (PreparationCostBasis.Missing or PreparationCostBasis.AllRequired)) throw new ArgumentOutOfRangeException(nameof(basis));
        var demands = new List<PreparationCostDemand>();
        foreach (var material in plan.Materials)
        {
            if (material.Holdings.Nq < 0 || material.Holdings.Hq < 0) throw new ArgumentException("Stock invalide.",nameof(plan));
            var stockRelevant = basis == PreparationCostBasis.Missing;
            demands.Add(new(material.ItemId,material.Required,
                !stockRelevant ? material.Required : !material.Holdings.HasObservation ? null : material.Uncovered,
                stockRelevant && (!material.Holdings.HasObservation || material.Holdings.HasUnknown),stockRelevant && material.Holdings.HasCached));
        }
        return BuildDemands(demands,world,basis,quality,item,quote,now,cacheLifetime:cacheLifetime);
    }

    public static PreparationCostEstimate FromChain(CraftChainResult chain, uint world, PreparationPriceQuality quality,
        Func<uint,ItemDefinition?> item, Func<uint,MarketQuote?> quote, DateTimeOffset now,TimeSpan? cacheLifetime=null)
    {
        ArgumentNullException.ThrowIfNull(chain);
        if (chain.Materials.Count==0 && chain.Observations.Count==0)
            throw new ArgumentException("Aucune observation ne permet d'établir l'absence de besoins.",nameof(chain));
        if (chain.Materials.Any(material => material.ItemId == 0 || material.Amount < 1 || !Enum.IsDefined(material.Reason)))
            throw new ArgumentException("Besoins de chaîne invalides.",nameof(chain));
        var unobserved = chain.Observations.Values.Any(stock => !stock.HasObservation)
            || chain.Materials.Any(material => !chain.Observations.TryGetValue(material.ItemId,out var stock) || !stock.HasObservation);
        var uncertain = chain.HasUnknown || unobserved;
        var demands = chain.Materials.GroupBy(material => material.ItemId).Select(group =>
        {
            var amount = group.Sum(material => material.Amount);
            return new PreparationCostDemand(group.Key,amount,unobserved ? null : amount,uncertain,chain.HasCached,
                group.Any(material => material.Reason != CraftChainReason.RawMaterial));
        }).ToArray();
        return BuildDemands(demands,world,PreparationCostBasis.TerminalChain,quality,item,quote,now,uncertain,chain.HasCached,cacheLifetime);
    }

    public static PreparationCostEstimate BuildDemands(IEnumerable<PreparationCostDemand> demands, uint world, PreparationCostBasis basis,
        PreparationPriceQuality quality, Func<uint,ItemDefinition?> item, Func<uint,MarketQuote?> quote, DateTimeOffset now,
        bool inputStockUncertain = false, bool inputCachedStock = false,TimeSpan? cacheLifetime=null)
    {
        ArgumentNullException.ThrowIfNull(demands); ArgumentNullException.ThrowIfNull(item); ArgumentNullException.ThrowIfNull(quote);
        if (!Enum.IsDefined(basis) || !Enum.IsDefined(quality)) throw new ArgumentOutOfRangeException(nameof(basis));
        var rows = new List<PreparationCostLine>();
        decimal? subtotal = 0;
        var seen = new HashSet<uint>();
        foreach (var material in demands)
        {
            if (material.ItemId == 0 || material.Required < 1 || !seen.Add(material.ItemId) || material.Quantity is < 0 || material.Quantity > material.Required)
                throw new ArgumentException("Préparation invalide.",nameof(demands));
            var unknownStock = material.StockUncertain;
            var cachedStock = material.CachedStock;
            var quantity = material.Quantity;
            var definition = item(material.ItemId);
            var price = material.Unresolved ? null : quote(material.ItemId);
            decimal? unit = null, cost = null;
            bool? hq = null;
            var stale = false;
            var status = PreparationCostStatus.Priced;
            if (material.Unresolved) { status=PreparationCostStatus.UnresolvedChain; price=null; }
            else if (quantity == 0) { status=PreparationCostStatus.Covered; cost=0; price=null; }
            else if (world == 0) { status=PreparationCostStatus.UnknownWorld; price=null; }
            else if (definition is null || definition.Id != material.ItemId) { status=PreparationCostStatus.UnknownItem; price=null; }
            else if (!definition.IsMarketable) { status=PreparationCostStatus.NotMarketable; price=null; }
            else if (quality == PreparationPriceQuality.Hq && !definition.CanBeHq) { status=PreparationCostStatus.UnsupportedQuality; price=null; }
            else if (price is null) status=PreparationCostStatus.MissingQuote;
            else if (price.WorldId != world || price.ItemId != material.ItemId) { status=PreparationCostStatus.WrongScope; price=null; }
            else
            {
                decimal? nq = price.Nq.Minimum is > 0 ? price.Nq.Minimum : null;
                decimal? high = definition.CanBeHq && price.Hq.Minimum is > 0 ? price.Hq.Minimum : null;
                hq = quality == PreparationPriceQuality.Hq || quality == PreparationPriceQuality.Cheapest && high is { } hp && (nq is null || hp < nq);
                unit = hq.Value ? high : nq;
                stale = !price.HasRecentUpload(now) || !Caching.CacheAge.Fresh(price.FetchedAt,now,cacheLifetime ?? TimeSpan.FromMinutes(5));
                if (unit is null) status=PreparationCostStatus.MissingPrice;
            }
            if (quantity is null && !material.Unresolved) status=PreparationCostStatus.UnknownStock;
            else if (status == PreparationCostStatus.Priced && quantity is { } amount)
            {
                try { cost=checked(unit!.Value*amount); }
                catch (OverflowException) { status=PreparationCostStatus.Overflow; }
            }
            if (subtotal is { } total && cost is { } lineCost)
            {
                try { subtotal=checked(total+lineCost); }
                catch (OverflowException) { subtotal=null; }
            }
            rows.Add(new(material.ItemId,material.Required,quantity,status,unit,cost,hq,price,unknownStock,cachedStock,stale));
        }
        return new(world,basis,quality,now,Array.AsReadOnly(rows.ToArray()),subtotal,inputStockUncertain,inputCachedStock);
    }
}
