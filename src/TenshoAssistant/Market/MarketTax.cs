namespace TenshoAssistant.Market;

/// <summary>
/// X-698 M4 (0.1671): the market tax of each city as the game last sent it (Dalamud IMarketBoard.TaxRatesReceived), by the Town sheet's
/// row (1 Limsa Lominsa, 2 Gridania, 3 Ul'dah, 4 Ishgard, 7 Kugane, 10 the Crystarium, 12 Old Sharlayan, 14 Tuliyollal; Town sheet and
/// RetainerManager.RetainerTown, docs/evidence/0.1671-retainer-sale-list-review.md), until the time the game gives. Memory only.
/// </summary>
public sealed record MarketTaxRates(IReadOnlyDictionary<byte,int> ByTown,DateTimeOffset ValidUntil);

public static class MarketTax
{
    /// <summary>The rate the game's help states for a retainer's sales (HowToPage 830): the one used when the city's rate was not received.</summary>
    public const int HelpPercent=5;
    /// <summary>The Town rows of the eight cities, in the order of Dalamud's IMarketTaxRates.</summary>
    public static readonly byte[] Towns=[1,2,3,4,7,10,12,14];

    /// <summary>The rates in the order of <see cref="Towns"/>; null when one is out of shape (above 100 %: R08, never a wrong rate).</summary>
    public static MarketTaxRates? From(IReadOnlyList<uint> rates,DateTimeOffset validUntil)
    {
        if(rates.Count!=Towns.Length || rates.Any(rate=>rate>100))return null;
        var byTown=new Dictionary<byte,int>();
        for(var index=0;index<Towns.Length;index++)byTown[Towns[index]]=(int)rates[index];
        return new(byTown,validUntil);
    }

    /// <summary>The city's rate when the game sent it and it still holds; null otherwise (the caller says so and uses the help's rate).</summary>
    public static int? RateFor(MarketTaxRates? rates,byte town,DateTimeOffset now)
        =>rates is not null && town!=0 && now<rates.ValidUntil && rates.ByTown.TryGetValue(town,out var percent) ? percent:null;

    /// <summary>X-700 M1 (0.1686): where a retainer's rate comes from: its city's rate sent by the game, the help's rate because the rates were not received (or no longer hold), or because its city was not read.</summary>
    public enum Source { City, NotReceived, TownUnknown }

    /// <summary>X-700 M1: the rate of <paramref name="retainer"/>'s city (any retainer whose town was read, not only the open one) and where it comes from; the help's rate otherwise.</summary>
    public static (int Percent,Source From) ForRetainer(MarketTaxRates? rates,IReadOnlyDictionary<ulong,byte>? towns,ulong retainer,DateTimeOffset now)
    {
        if(towns is null || !towns.TryGetValue(retainer,out var town) || town==0)return (HelpPercent,Source.TownUnknown);
        return RateFor(rates,town,now) is { } percent ? (percent,Source.City):(HelpPercent,Source.NotReceived);
    }
}
