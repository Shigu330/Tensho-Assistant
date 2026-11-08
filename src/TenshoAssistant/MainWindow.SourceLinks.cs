using System.Numerics;
using TenshoAssistant.Maps;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Clickable sources (0.178): the « how to obtain » tooltip cannot be clicked, so a right click on a collection entry or an item opens a
/// menu with one link per known source — NPC vendor (vendor tool, map flag), achievement, quest, recipe, gathering.
/// Since 0.251 the lines are grouped by kind in hover sub-menus (« Exchanges (12) »…), computed once (SourceLines) and drawn either in
/// Tensho or in the game's inventory menu (0.256). Shops known only from LuminaSupplemental (ENpcShop / ENpcPlace) are added; Grand
/// Company seals of another company than the character's are left out (0.296).
/// </summary>
internal sealed partial class MainWindow
{
    private IReadOnlyList<ItemDefinition> UnlockItemsOf(CollectionEntry entry)
    {
        var catalog=itemCatalog.State.Catalog;
        return catalog is null ? []:entry.Kind switch
        {
            CollectionKind.Mount=>catalog.ForMount(entry.Id),
            CollectionKind.Companion=>catalog.ForCompanion(entry.Id),
            CollectionKind.TripleTriad=>catalog.ForTriadCard(entry.Id),
            CollectionKind.Orchestrion=>catalog.ForOrchestrion(entry.Id),
            // 0.387: the teaching item of the 0.365 kinds, when known.
            _ when entry.ItemId!=0 && catalog.Find(entry.ItemId) is { } teaching=>[teaching],
            _=>[],
        };
    }

    // Right-click menu of a collection row: the source links of its unlocking items.
    // 0.274: an entry unlocked by a single item (roll, card, mount item…) gets that item's full menu — the same tools and grouped
    // sources as a right click on the item itself; entries with several unlocking items keep the combined source list.
    private void CollectionSourcesMenu(string id,CollectionEntry entry)
    {
        var unlocking=UnlockItemsOf(entry);
        if(unlocking.Count==1){ItemLinksMenu(id,unlocking[0]);return;}
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            ImGui.TextColored(Accent,entry.Name);
            if(!DrawSourceLinks(unlocking))ImGui.TextDisabled(localization.Get("SourceLinks.None"));
            ImGui.Separator();
            ImGui.TextDisabled(localization.Get("SourceLinks.WikiNote"));
        }
        finally{ImGui.EndPopup();}
    }

    // A wiki line naming an achievement in English, matched to the game's achievement (0.180).
    private CollectionEntry? AchievementNamed(Community.WikiAcquisitionLine line)=>
        line.Heading.Equals("Achievement",StringComparison.OrdinalIgnoreCase) || line.Heading.Equals("Achievements",StringComparison.OrdinalIgnoreCase)
            ? AchievementByEnglishName(line.Text):null; // X-76 (0.955): a look-up in the collection index (UI/Components/DataIndexes.cs), not a scan per line and frame.
    // 0.331: the state filter and the category are reset to the achievement's, which they could hide (as OpenCompletion).
    private void OpenAchievement(CollectionEntry achievement)
    {
        page=Page.Collections;collectionKind=CollectionKind.Achievement;focusCollectionKind=CollectionKind.Achievement;focusAchievementTab=true;collectionQuery=achievement.Name;
        collectionFilter=0;achievementCategoryFilter=achievement.Achievement?.CategoryId;IsOpen=true;
    }

    private string VendorPlace(Npcs.NpcVendor vendor)=>VendorLocations(vendor).FirstOrDefault() is { } place ? NpcPlace(place):localization.Get("NpcUi.NoLocation");
    // Vendor line (opens the vendor tool) and, when placed, its map flag sub-link.
    private void DrawVendorLink(uint itemId,Npcs.NpcVendor vendor,string label)
    {
        if(ImGui.Selectable(label+$"##SrcNpc{itemId}/{vendor.NpcId}")){npcVendorFocus=vendor.NpcId;page=Page.NpcVendors;IsOpen=true;}
        NpcMenu($"SrcNpcMenu{itemId}/{vendor.NpcId}",NpcName(vendor),vendor.NpcId,VendorLocations(vendor));
        if(VendorLocations(vendor).FirstOrDefault() is { } place && maps.State.Catalog?.Find(place.MapId) is { } map
           && ImGui.Selectable("    "+localization.Get("SourceLinks.Flag")+$"##SrcFlag{itemId}/{vendor.NpcId}"))PlaceFlagFromMapView(map,place.X,place.Y);
    }

    // NPCs that LuminaSupplemental (ENpcShop) links to the game shops selling the item — only shops with no vendor in the sheets — with their
    // position (ENpcPlace), optionally only shops paid with that currency.
    private IReadOnlyList<(uint NpcId,string Name,Npcs.NpcLocation? Location)> LuminaSellers(uint itemId,uint? currency)
    {
        if(lumina?.State.Data is not { } data || npcVendors?.State.Catalog is not { } catalog)return [];
        var sold=catalog.ForItem(itemId).Select(offer=>offer.Sale.ShopId).ToHashSet();
        return catalog.SalesOf(itemId).Where(sale=>!sold.Contains(sale.ShopId) && (currency is null || sale.Costs.Any(cost=>cost.CurrencyItemId==currency)))
            .SelectMany(sale=>data.NpcsOfShop(sale.ShopId)).Distinct().Take(4)
            .Select(npc=>(npc,data.NpcName(npc) is { Length:>0 } name ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:npc.ToString(),
                SeenNpcPlace(npc) ?? (data.PlaceOf(npc) is { } place ? new Npcs.NpcLocation(place.TerritoryId,place.MapId,maps.State.Catalog?.Find(place.MapId)?.Zone ?? "",place.X,place.Y,0,0,0):null)))
            .ToArray();
    }
    /// <summary>Menu entries for the sources of these items; false when none is known.</summary>
    // 0.251 (user request): the sources are grouped by kind, each group a sub-menu that opens on hover (« Exchanges (12) »…);
    // lines with the same text are shown once (the same NPC listed under several ids). Craft and gathering stay one line each.
    // 0.256: the lines are computed once (SourceLines) and drawn either in Tensho (ImGui) or in the game's inventory menu.
    private enum SourceGroup { Achievements, Duties, FromItems, Trades, Vendors, Quests, Craft, Gathering }
    internal sealed record ItemSourceLine(int Group,string Text,Action Open,Action? Flag=null,string? Hint=null,(MapDefinition Map,Vector2 At)? Place=null);

    private bool DrawSourceLinks(IReadOnlyList<ItemDefinition> items)
    {
        var lines=SourceLines(items);
        var index=0;
        void Draw(ItemSourceLine line)
        {
            index++;
            if(ImGui.Selectable(line.Text+$"##SrcLine{index}"))line.Open();
            if(line.Place is { } spot)PlaceMenu($"SrcLinePlace{index}",spot.Map,spot.At,line.Text);
            if(line.Hint is not null && ImGui.IsItemHovered())WrappedTooltip(line.Hint);
            if(line.Flag is not null && ImGui.Selectable("    "+localization.Get("SourceLinks.Flag")+$"##SrcFlag{index}"))line.Flag();
        }
        foreach(var group in lines.GroupBy(line=>(SourceGroup)line.Group).OrderBy(group=>group.Key))
        {
            if(group.Key is SourceGroup.Craft or SourceGroup.Gathering){foreach(var line in group)Draw(line);continue;}
            if(!ImGui.BeginMenu(SourceGroupLabel(group.Key,group)+$"##SrcGroup{group.Key}"))continue;
            try{foreach(var line in group)Draw(line);}
            finally{ImGui.EndMenu();}
        }
        return lines.Count>0;
    }
    // Currency sub-lines are not counted as sources.
    private string SourceGroupLabel(SourceGroup group,IEnumerable<ItemSourceLine> lines)
        =>localization.Format("SourceLinks.Group"+group,lines.Count(line=>!line.Text.StartsWith("    ",StringComparison.Ordinal)));

    /// <summary>Sources of one item for the game's inventory menu: (group label, lines); craft and gathering are groups of one line.</summary>
    internal IReadOnlyList<(string Label,IReadOnlyList<ItemSourceLine> Lines)> SourceMenu(uint id)
    {
        if(itemCatalog.State.Catalog?.Find(id) is not { } item)return [];
        return SourceLines([item]).GroupBy(line=>(SourceGroup)line.Group).OrderBy(group=>group.Key)
            .Select(group=>(group.Key is SourceGroup.Craft or SourceGroup.Gathering ? group.First().Text:SourceGroupLabel(group.Key,group),(IReadOnlyList<ItemSourceLine>)group.ToArray()))
            .ToArray();
    }

    // 0.296 (user request): Grand Company seals (items 20 storm, 21 serpent, 22 flame = 19 + company id, as the vendor loader) of a
    // company other than the character's are left out of the sources; unknown company (0) = everything shown.
    internal Func<uint>? PlayerGrandCompany { get; set; }
    private bool OtherCompanySeal(uint currency)
        => PlayerGrandCompany?.Invoke() is { } company and >= 1 and <= 3 && currency is 20 or 21 or 22 && currency != 19 + company;

    // 0.359: request=false reads only what the community services already hold (no wiki or Garland request), for the Collections filter.
    private List<ItemSourceLine> SourceLines(IReadOnlyList<ItemDefinition> items,bool request=true)
    {
        var lines=new List<ItemSourceLine>();
        Community.WikiItemAcquisitionState? Wiki(uint id)=>request ? wikiItems?.Get(id):wikiItems?.Peek(id);
        Community.GarlandItemSourceState? Garland(uint id)=>request ? itemSources?.Get(id):itemSources?.Peek(id);
        void Add(SourceGroup group,string text,Action open,Action? flag=null,string? hint=null,Npcs.NpcLocation? at=null)
        {if(!lines.Any(line=>line.Group==(int)group && line.Text==text))lines.Add(new((int)group,text,()=>{open();IsOpen=true;},flag,hint,at is not null && maps.State.Catalog?.Find(at.MapId) is { } atMap ? (atMap,new Vector2(at.X,at.Y)):null));}
        Action? FlagAt(Npcs.NpcLocation? place)=>place is not null && maps.State.Catalog?.Find(place.MapId) is { } map ? ()=>PlaceFlagFromMapView(map,place.X,place.Y):null;
        void Vendor(SourceGroup group,uint itemId,Npcs.NpcVendor vendor,string text)
            =>Add(group,text,()=>{npcVendorFocus=vendor.NpcId;page=Page.NpcVendors;},FlagAt(vendor.Locations.FirstOrDefault()),null,vendor.Locations.FirstOrDefault());
        // No vendor-tool entry for LuminaSupplemental NPCs: the line places the map flag when the position is known.
        void LuminaNpc(SourceGroup group,Npcs.NpcLocation? location,string text)
            =>Add(group,text,FlagAt(location) ?? (()=>{}),null,localization.Get(location is null ? "SourceLinks.LuminaNpc":"SourceLinks.LuminaNpcFlag"),location);
        void Currency(uint currencyId,string text)=>Add(SourceGroup.Trades,text,()=>{SelectItem(currencyId);OpenItemSheet();});
        var ids=items.Select(item=>item.Id).ToArray();
        var shownVendors=new HashSet<uint>();
        foreach(var achievement in ids.SelectMany(AchievementsRewardingItem).Distinct().Take(4))
            Add(SourceGroup.Achievements,localization.Format("SourceLinks.Achievement",achievement.Name),()=>OpenAchievement(achievement));
        // Sources known only from the community data, made clickable when they name something of the game (0.180): an achievement named
        // by the wiki, the currency of a Garland exchange (opens the currency item and its own sources).
        foreach(var item in items.Take(3))
        {
            foreach(var line in Wiki(item.Id)?.Page?.Lines ?? [])
                if(AchievementNamed(line) is { } named)
                    Add(SourceGroup.Achievements,localization.Format("SourceLinks.Achievement",named.Name),()=>OpenAchievement(named));
            // Duties from LuminaSupplemental (ContentFinderCondition id = DutyId, 0.188), then those named by Garland (« instance », 0.181).
            // X-75.2 (0.875): duties whose chests were seen in game giving it (yours or shared) come first.
            var dutyList=(Seen()?.DutiesByItem.GetValueOrDefault(item.Id) ?? []).Concat(LuminaDutiesOf(item.Id)).Distinct().Take(4).Select(id=>duties.State.Catalog?.Find(id))
                .Concat((Garland(item.Id)?.List?.Sources ?? []).Where(source=>source.Type=="instance").Take(4)
                    .Select(source=>duties.State.Catalog?.Entries.FirstOrDefault(duty=>duty.InstanceId==source.Id)));
            foreach(var duty in dutyList)
                if(duty is { } lootDuty)
                    Add(SourceGroup.Duties,localization.Format("SourceLinks.Duty",lootDuty.Name),()=>{dutyLoot.Select(lootDuty);showDutyDetails=true;dutyItemQuery="";page=Page.DutyLoot;});
            // X-75.2 (0.875): the items it was seen in game coming from (yours or shared: desynthesis, opened), before LuminaSupplemental's.
            foreach(var source in (Seen()?.SourcesByItem.GetValueOrDefault(item.Id) ?? []).Take(4))
            {
                var name=itemCatalog.State.Catalog?.Find(source)?.Name ?? source.ToString();
                Add(SourceGroup.FromItems,localization.Format("SourceLinks.SeenFromItem",name),()=>{SelectItem(source);OpenItemSheet();});
            }
            // Items the item comes from (LuminaSupplemental ItemSupplement, 0.197): opens the source item and its own sources.
            foreach(var (kind,_,sources) in lumina?.State.Data?.SupplementsOf(item.Id) ?? [])
                foreach(var source in sources.Take(4))
                {
                    var name=itemCatalog.State.Catalog?.Find(source)?.Name ?? source.ToString();
                    Add(SourceGroup.FromItems,localization.Format("HoverUi.Supplement"+(kind is >= 1 and <= 19 ? kind:0),name),()=>{SelectItem(source);OpenItemSheet();});
                }
            // Garland exchanges (0.199): the line opens the NPC that sells the item for that currency (game's shops) when one is known, the
            // currency item is a sub-link; with no known NPC the line says so and opens the currency.
            var offers=npcVendors?.State.Catalog?.ForItem(item.Id) ?? [];
            foreach(var trade in (Garland(item.Id)?.List?.Trades ?? []).Where(trade=>!OtherCompanySeal(trade.CurrencyId)))
            {
                var currencyName=itemCatalog.State.Catalog?.Find(trade.CurrencyId)?.Name ?? trade.CurrencyId.ToString();
                var currencyText="    "+localization.Format("SourceLinks.Currency",currencyName);
                var sellers=offers.Where(offer=>offer.Sale.Costs.Any(cost=>cost.CurrencyItemId==trade.CurrencyId)).Select(offer=>offer.Vendor).DistinctBy(vendor=>vendor.NpcId).Take(4).ToArray();
                if(sellers.Length==0 && LuminaSellers(item.Id,trade.CurrencyId) is { Count:>0 } luminaSellers)
                {
                    foreach(var (_,name,location) in luminaSellers)
                        LuminaNpc(SourceGroup.Trades,location,localization.Format("SourceLinks.TradeAt",trade.Amount,currencyName,name,location is null ? localization.Get("NpcUi.NoLocation"):NpcPlace(location)));
                    Currency(trade.CurrencyId,currencyText);
                    continue;
                }
                if(sellers.Length==0)
                {
                    Currency(trade.CurrencyId,localization.Format("SourceLinks.TradeNoNpc",trade.Amount,currencyName));
                    continue;
                }
                foreach(var vendor in sellers)
                {
                    shownVendors.Add(vendor.NpcId);
                    Vendor(SourceGroup.Trades,item.Id,vendor,localization.Format("SourceLinks.TradeAt",trade.Amount,currencyName,NpcName(vendor),VendorPlace(vendor)));
                }
                Currency(trade.CurrencyId,currencyText);
            }
        }
        foreach(var item in items.Take(3))
        {
            foreach(var vendor in (npcVendors?.State.Catalog?.ForItem(item.Id) ?? [])
                .Where(offer=>offer.Sale.Costs.Count==0 || !offer.Sale.Costs.All(cost=>OtherCompanySeal(cost.CurrencyItemId))).Select(offer=>offer.Vendor).DistinctBy(vendor=>vendor.NpcId).Where(vendor=>!shownVendors.Contains(vendor.NpcId)).Take(6))
                Vendor(SourceGroup.Vendors,item.Id,vendor,localization.Format("SourceLinks.Vendor",NpcName(vendor),VendorPlace(vendor)));
            // Shops without an NPC in the sheets, opened by an NPC known to LuminaSupplemental (ENpcShop, 0.199).
            if(Garland(item.Id)?.List?.Trades is not { Count:>0 })
                foreach(var (npcId,name,location) in LuminaSellers(item.Id,null).Where(seller=>!shownVendors.Contains(seller.NpcId)))
                    LuminaNpc(SourceGroup.Vendors,location,localization.Format("SourceLinks.Vendor",name,location is null ? localization.Get("NpcUi.NoLocation"):NpcPlace(location)));
            if(quests.State.RewardIndex?.ForItem(item.Id) is { Count:>0 } rewards)
                foreach(var reward in rewards.Take(4))
                    Add(SourceGroup.Quests,localization.Format("SourceLinks.Quest",reward.Name),()=>{page=Page.Quests;SelectQuest(reward.QuestId);});
            if(recipeCatalog.State.Catalog?.ForResult(item.Id) is { Count:>0 })
            {
                var craftId=item.Id;
                // 0.423 (user report): the Crafting page opened with no recipe (« choose a recipe »); it now opens the item's recipe on Preparation.
                Add(SourceGroup.Craft,localization.Format("SourceLinks.Craft",item.Name),()=>
                {
                    if(recipeCatalog.State.Catalog?.ForResult(craftId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));
                    else{SelectItem(craftId);page=Page.CraftSimulation;}
                });
            }
            if(gatheringCatalog.State.Catalog?.ForItem(item.Id) is { Count:>0 } gathered)
            {
                var gatherId=item.Id;
                Add(SourceGroup.Gathering,localization.Format("SourceLinks.Gathering",gathered[0].TypeName,gathered[0].Zone),()=>{SelectItem(gatherId);page=Page.Gathering;});
            }
        }
        return lines;
    }
}
