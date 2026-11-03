using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;

namespace TenshoAssistant;

// g, C-08 (0.359): Collections filter by source (vendor, exchange, duty, quest, achievement, craft, gathering, from another item, market,
// or no known source) for the entries unlocked by an item (mounts, minions, orchestrion rolls). The sources are those of the right-click
// menu (SourceLines), reduced to one mask per entry, computed when the filter is used and kept until a catalog changes.
internal sealed partial class MainWindow
{
    private const int SourceMarketBit=1<<20;
    // -1 all, a SourceGroup value, SourceMarket or SourceNone.
    private const int SourceMarket=100,SourceNone=101;
    private int collectionSourceFilter=-1;
    private int collectionViewSourceFilter=-1;
    private readonly Dictionary<(CollectionKind,uint),int> collectionSourceMasks=[];
    private (object?,object?,object?,object?,object?,object?,object?,object?) collectionSourceCatalogs;
    private int collectionSourceRevision,collectionViewSourceRevision;
    // 0.395: blue magic list order and the wiki index revision it was sorted with.
    private int collectionViewBlueOrder,collectionViewBlueRevision;

    private static bool HasSourceFilter(CollectionKind kind)=>kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Orchestrion;

    private void DrawCollectionSourceFilter(CollectionKind kind)
    {
        // The masks follow the catalogs they come from: a reloaded catalog drops them.
        var catalogs=((object?)itemCatalog.State.Catalog,(object?)npcVendors?.State.Catalog,(object?)lumina?.State.Data,(object?)duties.State.Catalog,
            (object?)gatheringCatalog.State.Catalog,(object?)recipeCatalog.State.Catalog,(object?)quests.State.RewardIndex,(object?)collections.State);
        if(!Equals(catalogs,collectionSourceCatalogs)){collectionSourceCatalogs=catalogs;collectionSourceMasks.Clear();collectionSourceRevision++;}
        if(!HasSourceFilter(kind))return;
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo(localization.Get("CollectionSources.Filter")+"##CollectionSource",SourceFilterLabel(collectionSourceFilter)))
        {
            try
            {
                foreach(var value in new[]{-1}.Concat(Enum.GetValues<SourceGroup>().Select(group=>(int)group)).Append(SourceMarket).Append(SourceNone))
                    if(ImGui.Selectable(SourceFilterLabel(value),collectionSourceFilter==value))collectionSourceFilter=value;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("CollectionSources.FilterTip");
    }

    private string SourceFilterLabel(int value)=>value switch
    {
        -1=>localization.Get("CollectionSources.All"),
        SourceMarket=>localization.Get("CollectionSources.Market"),
        SourceNone=>localization.Get("CollectionSources.None"),
        _=>localization.Get("CollectionSources."+(SourceGroup)value),
    };

    /// <summary>Whether the entry passes the source filter; a filter on a kind without unlock items lets everything through.</summary>
    private bool CollectionSourceMatches(CollectionEntry entry,int filter)
    {
        if(filter<0 || !HasSourceFilter(entry.Kind))return true;
        var mask=CollectionSourceMask(entry);
        return filter switch
        {
            SourceMarket=>(mask&SourceMarketBit)!=0,
            SourceNone=>mask==0,
            _=>(mask&(1<<filter))!=0,
        };
    }

    private int CollectionSourceMask(CollectionEntry entry)
    {
        if(collectionSourceMasks.TryGetValue((entry.Kind,entry.Id),out var known))return known;
        var items=UnlockItemsOf(entry);
        var mask=0;
        // No wiki or Garland request from a filter: only what was already fetched.
        foreach(var line in SourceLines(items,request:false))mask|=1<<line.Group;
        if(items.Any(item=>item.IsMarketable))mask|=SourceMarketBit;
        collectionSourceMasks[(entry.Kind,entry.Id)]=mask;
        return mask;
    }
}
