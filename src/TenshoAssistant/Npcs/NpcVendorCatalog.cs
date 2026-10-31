using System.Collections.Frozen;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Npcs;

public enum NpcShopKind { Gil, GrandCompany, Special }
// CurrencyItemId 0 means the game encodes the currency indirectly (RawCurrency) and it is not resolved: never guessed.
public sealed record NpcCost(uint CurrencyItemId,uint Amount,uint RawCurrency=0);
public sealed record NpcShopSale(uint ShopId,NpcShopKind Kind,string ShopName,uint ItemId,uint Quantity,bool Hq,IReadOnlyList<NpcCost> Costs);
// X/Y are map coordinates as shown in game; World* are kept to place the map flag exactly.
public sealed record NpcLocation(uint TerritoryId,uint MapId,string Zone,float X,float Y,float WorldX,float WorldY,float WorldZ);
public sealed record NpcVendor(uint NpcId,string Name,IReadOnlyList<uint> ShopIds,IReadOnlyList<NpcLocation> Locations);
public sealed record NpcVendorOffer(NpcVendor Vendor,NpcShopSale Sale);
public sealed record NpcVendorCacheData(string DataVersion,IReadOnlyList<NpcVendor> Vendors,IReadOnlyList<NpcShopSale> Sales);

/// <summary>Shop links as copied from game sheets; resolution follows direct shops, topic menus, pre-handlers and talk scripts.</summary>
public static class NpcShopLinks
{
    public const int MaximumDepth=4;
    public static IReadOnlyList<uint> Resolve(IEnumerable<uint> npcData,IReadOnlySet<uint> shops,IReadOnlyDictionary<uint,IReadOnlyList<uint>> indirections)
    {
        var found=new List<uint>();var seen=new HashSet<uint>();
        void Visit(uint id,int depth)
        {
            if(id==0 || depth>MaximumDepth || !seen.Add(id))return;
            if(shops.Contains(id)){found.Add(id);return;}
            if(indirections.TryGetValue(id,out var targets))foreach(var target in targets)Visit(target,depth+1);
        }
        foreach(var id in npcData)Visit(id,0);
        return Array.AsReadOnly(found.Distinct().Order().ToArray());
    }
    /// <summary>
    /// Cost of a special shop entry: direct item ids are exact; encoded small ids stay raw. Probe 2026-10-30
    /// (docs/evidence/0.92.0-shop-currency-probe.log): scrip exchanges use type 16 id 4 for both crafter and gatherer scrips,
    /// so neither TomestonesItem nor any single table maps these ids; they are shown unresolved, never guessed.
    /// </summary>
    /// <summary>
    /// Talk scripts that open shops from compiled script code, never from a sheet. Each entry is backed by an in-game
    /// observation (2026-10-30: « Prêtresse à la robe noire », Wolves' Den, menu « Échanger des marques de loup » mêlée 1/2,
    /// distance) and docs/evidence/0.97.0-wolf-npc-probe.log: the only three wolf-mark shops (type 4) that no sheet references.
    /// </summary>
    public static readonly IReadOnlyDictionary<uint,IReadOnlyList<uint>> ObservedTalkShops=new Dictionary<uint,IReadOnlyList<uint>>
    {
        [721280]=[1769743,1770537,1769744],
        // « Officier de gestion du Maelstrom », Wolves' Den « Échange de cristaux-insignes » (in-game capture 2026-10-31):
        // talk RegSeaPvpCommendationCrystalShop, the only shop costing commendation crystals (40479), the only NPC using it.
        [721735]=[1770684],
    };
    public const byte TomestoneCost=2,ScripCost=3;
    public static NpcCost Currency(byte encoding,uint costId,uint amount,byte costType=0)
    {
        if(encoding==0 || costId>=100)return new(costId,amount);
        var table=costType switch{TomestoneCost=>Tomestones,ScripCost=>Scrips,_=>null};
        return table is not null && table.TryGetValue(costId,out var coded) || DirectCodes.TryGetValue((encoding,costId),out coded)
            ? new(coded,amount):new(0,amount,costId);
    }
    // Evidence (2026-10-30): in-game readings by the user and shop families of docs/evidence/0.95.0-shop-currency-probe.log.
    // CostType 2 = tomestones, CostType 3 = scrips, whatever UseCurrencyType. Tomestone codes follow the *current* patch
    // (old shops named astronomic/aphoristic now cost mathematics): revisit when the weekly tomestone changes.
    private static readonly IReadOnlyDictionary<uint,uint> Tomestones=new Dictionary<uint,uint>{[1]=28,[2]=48,[3]=49};          // poetics, mathematics, mnemonics
    private static readonly IReadOnlyDictionary<uint,uint> Scrips=new Dictionary<uint,uint>{[2]=33913,[4]=33914,[6]=41784,[7]=41785}; // purple/orange crafter/gatherer
    // CostType 0 codes whose shop family names the item: wolf marks, allied seals, MGP. t4:25 = wolf marks seen in game
    // (currency exchange, 2 000 marks, user capture 2026-10-30, shop 1769743 which no sheet links to an NPC).
    private static readonly IReadOnlyDictionary<(byte Type,uint Id),uint> DirectCodes=new Dictionary<(byte,uint),uint>{[(16,25)]=25,[(4,25)]=25,[(4,27)]=27,[(4,29)]=29};
    /// <summary>Header check run before handing a territory layout to Lumina (LGB1, declared size, LGP1 chunk at offset 12).</summary>
    public static bool ValidLayout(ReadOnlySpan<byte> bytes)
        =>bytes.Length>=16 && bytes[..4].SequenceEqual("LGB1"u8) && BitConverter.ToInt32(bytes[4..8])==bytes.Length && bytes[12..16].SequenceEqual("LGP1"u8);
    /// <summary>Map coordinate shown in game for a world axis value (same formula as the game's map, 2048 units per map, scale = SizeFactor).</summary>
    public static float MapCoordinate(float world,int offset,uint scale)
    {
        var factor=scale/100f;
        return 41f/factor*((world+offset)*factor+1024f)/2048f+1f;
    }
}

public sealed class NpcVendorCatalog
{
    private readonly FrozenDictionary<uint,IReadOnlyList<NpcVendorOffer>> byItem;
    public string Source{get;}
    public int VendorCount{get;}
    public int SaleCount{get;}
    public IReadOnlyList<NpcVendor> Vendors{get;}
    public IReadOnlyList<NpcShopSale> Sales{get;}
    private NpcVendorCatalog(string source,IReadOnlyList<NpcVendor> vendors,IReadOnlyList<NpcShopSale> sales,FrozenDictionary<uint,IReadOnlyList<NpcVendorOffer>> byItem)
    {Source=source;Vendors=vendors;Sales=sales;this.byItem=byItem;VendorCount=vendors.Count;SaleCount=sales.Count;}

    public static NpcVendorCatalog Build(IEnumerable<NpcVendor> vendors,IEnumerable<NpcShopSale> sales,string source,CancellationToken cancellation=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var npcs=vendors.Where(vendor=>vendor is not null && vendor.NpcId!=0 && vendor.ShopIds.Count>0).GroupBy(vendor=>vendor.NpcId).Select(group=>group.First()).ToArray();
        var rows=sales.Where(sale=>sale is not null && sale.ItemId!=0 && sale.ShopId!=0).ToArray();
        var byShop=rows.GroupBy(sale=>sale.ShopId).ToDictionary(group=>group.Key,group=>group.ToArray());
        var offers=new Dictionary<uint,List<NpcVendorOffer>>();
        foreach(var vendor in npcs)
        {
            cancellation.ThrowIfCancellationRequested();
            foreach(var shop in vendor.ShopIds)
                if(byShop.TryGetValue(shop,out var shopSales))
                    foreach(var sale in shopSales)
                    {
                        if(!offers.TryGetValue(sale.ItemId,out var list))offers[sale.ItemId]=list=[];
                        list.Add(new(vendor,sale));
                    }
        }
        // Located vendors first, then cheapest single-currency cost, then name: the hover shows the head of this order.
        var index=offers.ToFrozenDictionary(pair=>pair.Key,pair=>(IReadOnlyList<NpcVendorOffer>)Array.AsReadOnly(pair.Value
            .OrderBy(offer=>offer.Vendor.Locations.Count==0).ThenBy(offer=>offer.Sale.Kind).ThenBy(offer=>offer.Sale.Costs.Count==1 ? offer.Sale.Costs[0].Amount:uint.MaxValue)
            .ThenBy(offer=>offer.Vendor.Name,StringComparer.OrdinalIgnoreCase).ThenBy(offer=>offer.Vendor.NpcId).ThenBy(offer=>offer.Sale.ShopId).ToArray()));
        return new(source,Array.AsReadOnly(npcs),Array.AsReadOnly(rows),index);
    }
    public IReadOnlyList<NpcVendorOffer> ForItem(uint itemId)=>byItem.GetValueOrDefault(itemId) ?? [];
}

public sealed record NpcVendorState(NpcVendorCatalog? Catalog,int Copied,int Total,string? Error);
public sealed class NpcVendorService
{
    private NpcVendorState state=new(null,0,0,null);
    public NpcVendorState State=>Volatile.Read(ref state);
    internal void Progress(int copied,int total)=>Interlocked.Exchange(ref state,new(null,copied,total,null));
    internal void Publish(NpcVendorCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,catalog.SaleCount,catalog.SaleCount,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,0,0,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,0,0,null));
}

public static class NpcVendorCache
{
    public const string Family="catalog-npc-vendors";
    public const int MaximumRows=500_000;
    // Bump when the sheet-to-record copy rules change without a change of record shape.
    // 2: FateShop links (gemstone traders). 3: InclusionShop links (scrip exchanges), encoded currencies no longer mapped (2026-10-30).
    private const int CopyRules=10; // 8: observed talk links; 9: planevent.lgb; 10: planner.lgb + header check + crystal exchange. // 4: purple scrips; 5: (type, id) table; 6: currencies keyed by CostType (tomestones vs scrips).
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(NpcVendor),typeof(NpcLocation),typeof(NpcShopSale),typeof(NpcCost),typeof(NpcShopKind));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static bool Valid(NpcVendorCacheData? data,string dataVersion)
        =>data is not null && data.DataVersion==dataVersion && data.Vendors is { Count:>0 and <=MaximumRows } && data.Sales is { Count:>0 and <=MaximumRows }
          && data.Vendors.All(vendor=>vendor is not null && vendor.Name is not null && vendor.ShopIds is not null && vendor.Locations is not null && vendor.Locations.All(location=>location is not null && location.Zone is not null))
          && data.Sales.All(sale=>sale is not null && sale.ShopName is not null && sale.Costs is not null && sale.Costs.All(cost=>cost is not null) && Enum.IsDefined(sale.Kind));
}
