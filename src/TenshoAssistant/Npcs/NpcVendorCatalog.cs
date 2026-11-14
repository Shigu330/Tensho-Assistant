using System.Collections.Frozen;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Npcs;

// 0.295: FreeCompany = company credit shops (FccShop, docs/evidence/0.295-fcc-shops.md).
public enum NpcShopKind { Gil, GrandCompany, Special, FreeCompany }
// CurrencyItemId 0 means the game encodes the currency indirectly (RawCurrency) and it is not resolved: never guessed.
public sealed record NpcCost(uint CurrencyItemId,uint Amount,uint RawCurrency=0)
{
    /// <summary>Company credits are not an item: this reserved id (outside the Item sheet) names them.</summary>
    public const uint CompanyCredits=0xFFFF0FCC;
}
// RequiredRank: free company rank needed to buy (FccShop), 0 = none.
// 0.352 (g, I-11): QuestRequired / AchievementRequired = what unlocks the article in the shop (GilShopItem, SpecialShop), 0 = nothing.
public sealed record NpcShopSale(uint ShopId,NpcShopKind Kind,string ShopName,uint ItemId,uint Quantity,bool Hq,IReadOnlyList<NpcCost> Costs,byte RequiredRank=0,
    uint QuestRequired=0,uint AchievementRequired=0);
// X/Y are map coordinates as shown in game; World* are kept to place the map flag exactly.
/// <summary>0.811 (X-45): Condition 0 = always there, 1 = only while its festival (Festival) runs, 2 = only during a quest or the opening (layer QST_, QB_, EVT_OP).</summary>
/// X-266 (0.1307): Quests, for a Level position placed in its territory only by quest layers: the quests of those layers (shown while one is under way or done).
public sealed record NpcLocation(uint TerritoryId,uint MapId,string Zone,float X,float Y,float WorldX,float WorldY,float WorldZ,int Condition=0,uint Festival=0,IReadOnlyList<uint>? Quests=null);
public sealed record NpcVendor(uint NpcId,string Name,IReadOnlyList<uint> ShopIds,IReadOnlyList<NpcLocation> Locations);
public sealed record NpcVendorOffer(NpcVendor Vendor,NpcShopSale Sale);
public sealed record NpcVendorCacheData(string DataVersion,IReadOnlyList<NpcVendor> Vendors,IReadOnlyList<NpcShopSale> Sales);

/// <summary>Shop links as copied from game sheets; resolution follows direct shops, topic menus, pre-handlers and talk scripts.</summary>
public static class NpcShopLinks
{
    public const int Always=0,DuringFestival=1,DuringQuest=2,Unplaced=3;
    /// <summary>
    /// 0.811 (X-45, user report: event vendors on the map while absent): a layout layer's condition. Limsa's planevent.lgb (tools/MapMaskProbe --lgb 128,
    /// docs/evidence/0.811-conditional-npcs.md): 136 layers with a FestivalID, and layers « EVT_OP_ONLY_ENPC », « QST_… », « QB_… » for the opening and quests.
    /// </summary>
    /// 0.829 (tools/MapMaskProbe --lgb 128, planner.lgb): « QST_ENPC01 » to « QST_ENPC14 » hold the Upper Decks' permanent NPCs (up to 34 a layer): always there.
    /// X-266 (0.1307): « QST_ENPC » whatever its case: « QST_ENpc_001 » and « _002 » hold two permanent vendors of territory 180 (map 30).
    public static (int Condition,uint Festival) LayerCondition(string? layer,uint festival)=>festival!=0 ? (DuringFestival,festival)
        :layer is { } name && !name.StartsWith("QST_ENPC",StringComparison.OrdinalIgnoreCase)
         && (name.StartsWith("QST_",StringComparison.Ordinal) || name.StartsWith("QB_",StringComparison.Ordinal) || name.StartsWith("EVT_OP",StringComparison.Ordinal)) ? (DuringQuest,0u):(Always,0u);
    /// <summary>0.823: of two layers placing one NPC, the one that keeps it there more: always, then a festival, then a quest or the opening.</summary>
    public static (int Condition,uint Festival) MorePresent((int Condition,uint Festival) first,(int Condition,uint Festival) second)
        =>first.Condition==Always || second.Condition==Always ? (Always,0u):first.Condition==DuringFestival ? first:second;
    /// <summary>0.811: a place shown on the map: always there, or its festival running; never a quest's or the opening's.</summary>
    /// 0.829: never a vendor placed by no layer at all (only a Level row): on the Upper Decks all nine are event NPCs (docs/evidence/0.811-conditional-npcs.md).
    /// X-266 (0.1307): a quest's place linked to quests (Quests) shows while one of them is under way or done (seen); without a link it stays hidden.
    public static bool Present(NpcLocation spot,IReadOnlySet<uint> running,Func<uint,bool>? seen=null)=>spot.Condition==Always || spot.Condition==DuringFestival && running.Contains(spot.Festival)
        || spot.Condition==DuringQuest && seen is not null && spot.Quests is { Count:>0 } quests && quests.Any(seen);

    /// <summary>
    /// X-266 (0.1307, user report of 13/11 at Mor Dhona, relais/diagnostic-pnj-absents-13-11.md): a Level position (« always ») takes the condition of the
    /// vendor's layers in its territory: a festival's (0.828), or a quest's when every layer there is a quest's (Cristiana 1016290, only layer
    /// « QST_JobRel101 », quest 67748 not taken: not in the game); placed by no layer anywhere, unplaced (0.829); otherwise always.
    /// </summary>
    public static int LevelCondition((int Condition,uint Festival)? placed,bool placedAnywhere)=>placed switch
    {
        { Condition:DuringFestival }=>DuringFestival,
        { Condition:DuringQuest }=>DuringQuest,
        null when !placedAnywhere=>Unplaced,
        _=>Always,
    };

    /// <summary>
    /// X-266 (0.1307): the quest name a quest layer stands for: « QST_JobRel101 » → « JobRel101 », « QST_AktKdd_PUB » → « AktKdd », « QST_SubLak001 »
    /// → « SubLak001 » (suffixes _PUB, _HANDLER and _ with digits taken off); null for another layer or « QST_ENPC… » (permanent).
    /// </summary>
    public static string? LayerQuestKey(string? layer)
    {
        if(layer is null || !layer.StartsWith("QST_",StringComparison.OrdinalIgnoreCase) || layer.StartsWith("QST_ENPC",StringComparison.OrdinalIgnoreCase))return null;
        var key=layer[4..];
        while(key.LastIndexOf('_') is var cut and >0
              && (key[(cut+1)..] is var tail && (tail.Equals("PUB",StringComparison.OrdinalIgnoreCase) || tail.Equals("HANDLER",StringComparison.OrdinalIgnoreCase) || tail.Length>0 && tail.All(char.IsAsciiDigit))))
            key=key[..cut];
        return key.Length>0 ? key:null;
    }

    /// <summary>X-266 (0.1307): the quests of a layer key: those whose script (Quest.Id, « JobRel101_02212 ») is the key then « _ ».</summary>
    public static IReadOnlyList<uint> QuestsOf(string key,IEnumerable<(uint Id,string Script)> quests)=>
        quests.Where(quest=>quest.Script.Length>key.Length && quest.Script[key.Length]=='_' && quest.Script.StartsWith(key,StringComparison.OrdinalIgnoreCase))
            .Select(quest=>quest.Id).Distinct().Order().ToArray();

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
    /// 0.679 (user report): NPCs of a service whose shop is opened by script code, with no sheet to give it. They are placed on the map by name and title, with no sales.
    /// On the Island Sanctuary: Marchandréa « Divers », Laracheteuse « Exportations », Rodolfermier « Champs », Béberger « Pâturage », Chamoiselle « Guide »: docs/evidence/0.679-island-map.md.
    /// </summary>
    public static readonly IReadOnlyList<uint> ServiceNpcs=[1043463,1043464,1043465,1043466,1043080];

    /// <summary>0.680: where the game placed an NPC that no sheet and no layout places, read in the game (docs/evidence/0.679-island-map.md). Id, territory, map, world position.</summary>
    public static readonly IReadOnlyList<(uint Npc,uint Territory,uint Map,float X,float Y,float Z)> ObservedPlaces=[(1043464,1055,773,-266.316f,41.001f,207.874f)];

    /// <summary>
    /// Talk scripts that open shops from compiled script code, never from a sheet. Each entry is backed by an in-game
    /// observation (2026-10-30: « Prêtresse à la robe noire », Wolves' Den, menu « Échanger des marques de loup » mêlée 1/2,
    /// distance) and docs/evidence/0.97.0-wolf-npc-probe.log: the only three wolf-mark shops (type 4) that no sheet references.
    /// </summary>
    public static readonly IReadOnlyDictionary<uint,IReadOnlyList<uint>> ObservedTalkShops=new Dictionary<uint,IReadOnlyList<uint>>
    {
        [721280]=[1769743,1770537,1769744],
        // Marchandréa, Island Sanctuary, talk CtsMjiSpecialShop_00789 with no script argument: the user reports the exchange of both island currencies there (2026-11-06);
        // these are the only special shops priced in them (37549, 37550). Which of them this NPC opens is inferred: docs/evidence/0.679-island-map.md.
        [721685]=[1770601,1770602,1770604,1770643,1770659,1770660,1770662,1770709],
        // « Officier de gestion du Maelstrom », Wolves' Den « Échange de cristaux-insignes » (in-game capture 2026-10-31):
        // talk RegSeaPvpCommendationCrystalShop, the only shop costing commendation crystals (40479), the only NPC using it.
        [721735]=[1770684],
        // Kaede « Fouilles archéologiques », Auxesia (in-game capture 2026-10-31: « Obtenir des batteries portatives » → « Échange de devises »,
        // Batterie portative (Auxesia), 200): talk CtsWksTreasure_00966 has no script argument; 1771038 is the special shop selling that item
        // (docs/evidence/0.176.0-kaede-talk-probe.log). The other battery shop, 1771006, is not linked until observed.
        [721862]=[1771038],
    };
    public const byte TomestoneCost=2,ScripCost=3;
    /// <summary>
    /// Cost of a special shop entry: direct item ids are exact; encoded small ids stay raw. Probe 2026-10-30
    /// (docs/evidence/0.92.0-shop-currency-probe.log): scrip exchanges use type 16 id 4 for both crafter and gatherer scrips,
    /// so neither TomestonesItem nor any single table maps these ids; they are shown unresolved, never guessed.
    /// </summary>
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
    {
        Source=source;Vendors=vendors;Sales=sales;this.byItem=byItem;VendorCount=vendors.Count;SaleCount=sales.Count;
        salesByItem=sales.GroupBy(sale=>sale.ItemId).ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<NpcShopSale>)group.ToArray());
        // X-76 (0.955): the vendor sheet read every sale and every vendor on each frame; these are built once with the catalogue.
        // Shops keep the order of their first sale in Sales, sales their order in Sales (as the page's former GroupBy did).
        var shops=sales.GroupBy(sale=>sale.ShopId).Select((group,order)=>(group.Key,Order:order,Sales:(IReadOnlyList<NpcShopSale>)Array.AsReadOnly(group.ToArray()))).ToArray();
        salesByShop=shops.ToFrozenDictionary(shop=>shop.Key,shop=>shop.Sales);
        // X-01 (0.1103): what the map's vendor labels need per shop, built here (off the frame) instead of grouping every sale in the map's frame.
        ShopSummaries=shops.ToFrozenDictionary(shop=>shop.Key,shop=>(shop.Sales[0].ShopName,shop.Sales[0].Kind,shop.Sales.Select(sale=>sale.ItemId).Distinct().Count()));
        // X-01 (0.1103): the vendors that stand on each map (index in Vendors), so a map change no longer scans every vendor's places.
        var onMap=new Dictionary<uint,List<int>>();
        for(var index=0;index<vendors.Count;index++)
            foreach(var map in vendors[index].Locations.Select(spot=>spot.MapId).Distinct())
            {
                if(!onMap.TryGetValue(map,out var list))onMap[map]=list=[];
                list.Add(index);
            }
        vendorsOnMap=onMap.ToFrozenDictionary(pair=>pair.Key,pair=>pair.Value.ToArray());
        var shopOrder=shops.ToDictionary(shop=>shop.Key,shop=>shop.Order);
        vendorByNpc=vendors.GroupBy(vendor=>vendor.NpcId).ToFrozenDictionary(group=>group.Key,group=>group.First());
        shopsByNpc=vendorByNpc.ToFrozenDictionary(pair=>pair.Key,pair=>(IReadOnlyList<IReadOnlyList<NpcShopSale>>)Array.AsReadOnly(pair.Value.ShopIds.Distinct()
            .Where(shopOrder.ContainsKey).OrderBy(shop=>shopOrder[shop]).Select(shop=>salesByShop[shop]).ToArray()));
    }
    private readonly FrozenDictionary<uint,IReadOnlyList<NpcShopSale>> salesByShop;
    private readonly FrozenDictionary<uint,int[]> vendorsOnMap;
    /// <summary>X-01 (0.1103): each shop that sells something: its name and kind (those of its first sale) and how many different items it sells.</summary>
    public IReadOnlyDictionary<uint,(string Name,NpcShopKind Kind,int Items)> ShopSummaries{get;}
    /// <summary>X-01 (0.1103): the vendors with at least one place on one of these maps, in the order of <see cref="Vendors"/>.</summary>
    public IReadOnlyList<NpcVendor> VendorsOnAny(IEnumerable<uint> maps)
    {
        var indexes=new SortedSet<int>();
        foreach(var map in maps)
            if(vendorsOnMap.TryGetValue(map,out var found))indexes.UnionWith(found);
        return indexes.Select(index=>Vendors[index]).ToArray();
    }
    private readonly FrozenDictionary<uint,NpcVendor> vendorByNpc;
    private readonly FrozenDictionary<uint,IReadOnlyList<IReadOnlyList<NpcShopSale>>> shopsByNpc;
    /// <summary>Every sale of one shop, in the order of Sales; empty when the shop sells nothing.</summary>
    public IReadOnlyList<NpcShopSale> SalesOfShop(uint shopId)=>salesByShop.GetValueOrDefault(shopId) ?? [];
    /// <summary>The vendor of this NPC id (the first one kept by Build), or null.</summary>
    public NpcVendor? Vendor(uint npcId)=>vendorByNpc.GetValueOrDefault(npcId);
    /// <summary>The shops of this NPC's vendor that sell something, each one's sales (never empty), shops in the order of their first sale in Sales.</summary>
    public IReadOnlyList<IReadOnlyList<NpcShopSale>> ShopsOf(uint npcId)=>shopsByNpc.GetValueOrDefault(npcId) ?? [];
    // Every sale of an item, also in shops no NPC of the sheets opens (0.199: matched with LuminaSupplemental ENpcShop).
    private readonly FrozenDictionary<uint,IReadOnlyList<NpcShopSale>> salesByItem;
    public IReadOnlyList<NpcShopSale> SalesOf(uint itemId)=>salesByItem.GetValueOrDefault(itemId) ?? [];
    /// <summary>0.575 (I3): the lowest price in gil of one piece at an NPC, normal quality; null when no shop sells it for gil alone.</summary>
    public uint? GilPrice(uint itemId)
    {
        uint? lowest=null;
        foreach(var sale in SalesOf(itemId))
            if(sale.Kind==NpcShopKind.Gil && !sale.Hq && sale.Quantity==1 && sale.Costs is [{CurrencyItemId:GilItem,Amount:>0} cost] && (lowest is null || cost.Amount<lowest))lowest=cost.Amount;
        return lowest;
    }
    private const uint GilItem=1;

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
    // Offers paid with an item (0.209: the item as a currency — scrips, certificates, MGP, seals…), built on first use.
    private FrozenDictionary<uint,IReadOnlyList<NpcVendorOffer>>? byCurrency;
    public IReadOnlyList<NpcVendorOffer> PaidWith(uint currencyItemId)
    {
        var index=byCurrency ??= byItem.Values.SelectMany(offers=>offers).SelectMany(offer=>offer.Sale.Costs.Select(cost=>(cost.CurrencyItemId,offer)))
            .Where(pair=>pair.CurrencyItemId!=0).GroupBy(pair=>pair.CurrencyItemId)
            .ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<NpcVendorOffer>)group.Select(pair=>pair.offer).Distinct().ToArray());
        return index.GetValueOrDefault(currencyItemId) ?? [];
    }
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
    private const int CopyRules=16; // 16: QST_ENPC whatever its case; Level positions only on quest layers take them, with the layers' quests (X-266). // 15: QST_ENPC layers permanent; Level-only vendors unplaced. // 14: Level positions take a festival's condition only. // 13: Level positions take their layer's condition. // 12: layer conditions (festival, quest, opening). 11: Kaede's archaeology talk (Auxesia). 8: observed talk links; 9: planevent.lgb; 10: planner.lgb + header check + crystal exchange. // 4: purple scrips; 5: (type, id) table; 6: currencies keyed by CostType (tomestones vs scrips).
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(NpcVendor),typeof(NpcLocation),typeof(NpcShopSale),typeof(NpcCost),typeof(NpcShopKind));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static bool Valid(NpcVendorCacheData? data,string dataVersion)
        =>data is not null && data.DataVersion==dataVersion && data.Vendors is { Count:>0 and <=MaximumRows } && data.Sales is { Count:>0 and <=MaximumRows }
          && data.Vendors.All(vendor=>vendor is not null && vendor.Name is not null && vendor.ShopIds is not null && vendor.Locations is not null && vendor.Locations.All(location=>location is not null && location.Zone is not null))
          && data.Sales.All(sale=>sale is not null && sale.ShopName is not null && sale.Costs is not null && sale.Costs.All(cost=>cost is not null) && Enum.IsDefined(sale.Kind));
}
