namespace TenshoAssistant.GrandCompanies;

/// <summary>W-11 (0.714): a grade of a Grand Company. Names and promotion quest by company: 0 Maelstrom, 1 Twin Adder, 2 Immortal Flames. docs/evidence/0.714-grand-company.md.</summary>
public sealed record GcRank(int Rank,int MaxSeals,int RequiredSeals,IReadOnlyList<string> Names,IReadOnlyList<string> PromotionQuests);
public sealed record GcShopItem(int Company,uint Item,int Cost,int RequiredRank);
public sealed record GcCatalog(IReadOnlyList<string> Companies,IReadOnlyList<GcRank> Ranks,IReadOnlyList<GcShopItem> Shop);

public enum GcReadStatus { Loaded,NotLoaded,Unavailable }
/// <summary>Company 0: none yet, else 1 to 3. The lists have one value per company.</summary>
public sealed record GcSnapshot(GcReadStatus Status,int Company,IReadOnlyList<int> Ranks,IReadOnlyList<int> Seals,IReadOnlyList<int> MaxSeals)
{
    public static GcSnapshot Of(GcReadStatus status)=>new(status,0,[],[],[]);
}

public static class GcBoard
{
    public static GcRank? Rank(GcCatalog catalog,int rank)=>catalog.Ranks.FirstOrDefault(row=>row.Rank==rank);

    /// <summary>The grade after this one, null at the last. A sheet row with no name for this company is no grade.</summary>
    public static GcRank? Next(GcCatalog catalog,int company,int rank)=>
        catalog.Ranks.Where(row=>row.Rank>rank && company>=1 && company<=row.Names.Count && row.Names[company-1].Length>0).OrderBy(row=>row.Rank).FirstOrDefault();

    /// <summary>What the company sells, cheapest first; with a rank, only what that rank can buy.</summary>
    public static IReadOnlyList<GcShopItem> Shop(GcCatalog catalog,int company,int? rank)=>
        catalog.Shop.Where(item=>item.Company==company && (rank is null || item.RequiredRank<=rank)).OrderBy(item=>item.RequiredRank).ThenBy(item=>item.Cost).ToArray();

    public static GcReadStatus Check(int company,IReadOnlyList<int> ranks,int rankRows)=>
        company is <0 or >3 || ranks.Any(rank=>rank<0 || rank>=rankRows) ? GcReadStatus.Unavailable:GcReadStatus.Loaded;
}
