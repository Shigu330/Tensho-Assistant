using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Clickable sources (0.178): the « how to obtain » tooltip cannot be clicked, so a right click on a collection entry or an item opens a
/// menu with one link per known source — NPC vendor (vendor tool, map flag), achievement, quest, recipe, gathering.
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
            _=>[],
        };
    }

    // Right-click menu of a collection row: the source links of its unlocking items.
    private void CollectionSourcesMenu(string id,CollectionEntry entry)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            ImGui.TextColored(Accent,entry.Name);
            if(!DrawSourceLinks(UnlockItemsOf(entry)))ImGui.TextDisabled(localization.Get("SourceLinks.None"));
            ImGui.Separator();
            ImGui.TextDisabled(localization.Get("SourceLinks.WikiNote"));
        }
        finally{ImGui.EndPopup();}
    }

    // A wiki line naming an achievement in English, matched to the game's achievement (0.180).
    private CollectionEntry? AchievementNamed(Community.WikiAcquisitionLine line)=>
        line.Heading.Equals("Achievement",StringComparison.OrdinalIgnoreCase) || line.Heading.Equals("Achievements",StringComparison.OrdinalIgnoreCase)
            ? collections.State.Entries.FirstOrDefault(entry=>entry.Kind==CollectionKind.Achievement && entry.Achievement is { EnglishName.Length:>0 } metadata
                && metadata.EnglishName.Equals(line.Text,StringComparison.OrdinalIgnoreCase)):null;
    private void OpenAchievement(CollectionEntry achievement)
    {page=Page.Collections;collectionKind=CollectionKind.Achievement;focusCollectionKind=CollectionKind.Achievement;focusAchievementTab=true;collectionQuery=achievement.Name;IsOpen=true;}

    private string VendorPlace(Npcs.NpcVendor vendor)=>vendor.Locations.FirstOrDefault() is { } place ? NpcPlace(place):localization.Get("NpcUi.NoLocation");
    // Vendor line (opens the vendor tool) and, when placed, its map flag sub-link.
    private void DrawVendorLink(uint itemId,Npcs.NpcVendor vendor,string label)
    {
        if(ImGui.Selectable(label+$"##SrcNpc{itemId}/{vendor.NpcId}")){npcVendorFocus=vendor.NpcId;page=Page.NpcVendors;IsOpen=true;}
        if(vendor.Locations.FirstOrDefault() is { } place && maps.State.Catalog?.Find(place.MapId) is { } map
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
                data.PlaceOf(npc) is { } place ? new Npcs.NpcLocation(place.TerritoryId,place.MapId,maps.State.Catalog?.Find(place.MapId)?.Zone ?? "",place.X,place.Y,0,0,0):null))
            .ToArray();
    }
    private void DrawLuminaNpcLink(uint itemId,uint npcId,Npcs.NpcLocation? location,string label)
    {
        // No vendor-tool entry for these NPCs: the line places the map flag when the position is known.
        if(ImGui.Selectable(label+$"##SrcLNpc{itemId}/{npcId}") && location is not null && maps.State.Catalog?.Find(location.MapId) is { } map)PlaceFlagFromMapView(map,location.X,location.Y);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(location is null ? "SourceLinks.LuminaNpc":"SourceLinks.LuminaNpcFlag"));
    }

    /// <summary>Menu entries for the sources of these items; false when none is known.</summary>
    private bool DrawSourceLinks(IReadOnlyList<ItemDefinition> items)
    {
        var any=false;
        var ids=items.Select(item=>item.Id).ToArray();
        var shownVendors=new HashSet<uint>();
        foreach(var achievement in collections.State.Entries.Where(entry=>entry.Kind==CollectionKind.Achievement && entry.Achievement is { RewardItemId:not 0 } metadata && ids.Contains(metadata.RewardItemId)).Take(4))
        {
            any=true;
            if(ImGui.Selectable(localization.Format("SourceLinks.Achievement",achievement.Name)+$"##SrcAch{achievement.Id}"))OpenAchievement(achievement);
        }
        // Sources known only from the community data, made clickable when they name something of the game (0.180): an achievement named
        // by the wiki, the currency of a Garland exchange (opens the currency item and its own sources).
        foreach(var item in items.Take(3))
        {
            foreach(var line in wikiItems?.Get(item.Id).Page?.Lines ?? [])
                if(AchievementNamed(line) is { } named)
                {any=true;if(ImGui.Selectable(localization.Format("SourceLinks.Achievement",named.Name)+$"##SrcWikiAch{item.Id}/{named.Id}"))OpenAchievement(named);}
            // Duties from LuminaSupplemental (ContentFinderCondition id = DutyId, 0.188), then those named by Garland.
            foreach(var dutyId in LuminaDutiesOf(item.Id).Take(4))
                if(duties.State.Catalog?.Find(dutyId) is { } lootDuty)
                {
                    any=true;
                    if(ImGui.Selectable(localization.Format("SourceLinks.Duty",lootDuty.Name)+$"##SrcLumina{item.Id}/{lootDuty.DutyId}"))
                    {dutyLoot.Select(lootDuty);showDutyDetails=true;dutyItemQuery="";page=Page.DutyLoot;IsOpen=true;}
                }
            // Duties named by Garland (« instance », InstanceContent id) open in the duty loot tool (0.181).
            foreach(var source in (itemSources?.Get(item.Id).List?.Sources ?? []).Where(source=>source.Type=="instance").Take(4))
                if(duties.State.Catalog?.Entries.FirstOrDefault(duty=>duty.InstanceId==source.Id) is { } duty)
                {
                    any=true;
                    if(ImGui.Selectable(localization.Format("SourceLinks.Duty",duty.Name)+$"##SrcDuty{item.Id}/{duty.DutyId}"))
                    {dutyLoot.Select(duty);showDutyDetails=true;dutyItemQuery="";page=Page.DutyLoot;IsOpen=true;}
                }
            // Items the item comes from (LuminaSupplemental ItemSupplement, 0.197): opens the source item and its own sources.
            foreach(var (kind,_,sources) in lumina?.State.Data?.SupplementsOf(item.Id) ?? [])
                foreach(var source in sources.Take(4))
                {
                    any=true;
                    var name=itemCatalog.State.Catalog?.Find(source)?.Name ?? source.ToString();
                    if(ImGui.Selectable(localization.Format("HoverUi.Supplement"+(kind is >= 1 and <= 19 ? kind:0),name)+$"##SrcSupp{item.Id}/{kind}/{source}")){SelectItem(source);page=Page.Items;IsOpen=true;}
                }
            // Garland exchanges (0.199): the line opens the NPC that sells the item for that currency (game's shops) when one is known, the
            // currency item is a sub-link; with no known NPC the line says so and opens the currency.
            var offers=npcVendors?.State.Catalog?.ForItem(item.Id) ?? [];
            foreach(var trade in itemSources?.Get(item.Id).List?.Trades ?? [])
            {
                any=true;
                var currencyName=itemCatalog.State.Catalog?.Find(trade.CurrencyId)?.Name ?? trade.CurrencyId.ToString();
                var sellers=offers.Where(offer=>offer.Sale.Costs.Any(cost=>cost.CurrencyItemId==trade.CurrencyId)).Select(offer=>offer.Vendor).DistinctBy(vendor=>vendor.NpcId).Take(4).ToArray();
                if(sellers.Length==0 && LuminaSellers(item.Id,trade.CurrencyId) is { Count:>0 } luminaSellers)
                {
                    foreach(var (npcId,name,location) in luminaSellers)
                        DrawLuminaNpcLink(item.Id,npcId,location,localization.Format("SourceLinks.TradeAt",trade.Amount,currencyName,name,location is null ? localization.Get("NpcUi.NoLocation"):NpcPlace(location)));
                    if(ImGui.Selectable("    "+localization.Format("SourceLinks.Currency",currencyName)+$"##SrcCur{item.Id}/{trade.CurrencyId}")){SelectItem(trade.CurrencyId);page=Page.Items;IsOpen=true;}
                    continue;
                }
                if(sellers.Length==0)
                {
                    if(ImGui.Selectable(localization.Format("SourceLinks.TradeNoNpc",trade.Amount,currencyName)+$"##SrcTrade{item.Id}/{trade.CurrencyId}")){SelectItem(trade.CurrencyId);page=Page.Items;IsOpen=true;}
                    continue;
                }
                foreach(var vendor in sellers)
                {
                    shownVendors.Add(vendor.NpcId);
                    DrawVendorLink(item.Id,vendor,localization.Format("SourceLinks.TradeAt",trade.Amount,currencyName,NpcName(vendor),VendorPlace(vendor)));
                }
                if(ImGui.Selectable("    "+localization.Format("SourceLinks.Currency",currencyName)+$"##SrcCur{item.Id}/{trade.CurrencyId}")){SelectItem(trade.CurrencyId);page=Page.Items;IsOpen=true;}
            }
        }
        foreach(var item in items.Take(3))
        {
            foreach(var vendor in (npcVendors?.State.Catalog?.ForItem(item.Id) ?? []).Select(offer=>offer.Vendor).DistinctBy(vendor=>vendor.NpcId).Where(vendor=>!shownVendors.Contains(vendor.NpcId)).Take(6))
            {
                any=true;
                DrawVendorLink(item.Id,vendor,localization.Format("SourceLinks.Vendor",NpcName(vendor),VendorPlace(vendor)));
            }
            // Shops without an NPC in the sheets, opened by an NPC known to LuminaSupplemental (ENpcShop, 0.199).
            if(itemSources?.Get(item.Id).List?.Trades is not { Count:>0 })
                foreach(var (npcId,name,location) in LuminaSellers(item.Id,null).Where(seller=>!shownVendors.Contains(seller.NpcId)))
                {
                    any=true;
                    DrawLuminaNpcLink(item.Id,npcId,location,localization.Format("SourceLinks.Vendor",name,location is null ? localization.Get("NpcUi.NoLocation"):NpcPlace(location)));
                }
            if(quests.State.RewardIndex?.ForItem(item.Id) is { Count:>0 } rewards)
                foreach(var reward in rewards.Take(4))
                {
                    any=true;
                    if(ImGui.Selectable(localization.Format("SourceLinks.Quest",reward.Name)+$"##SrcQuest{item.Id}/{reward.QuestId}")){page=Page.Quests;SelectQuest(reward.QuestId);IsOpen=true;}
                }
            if(recipeCatalog.State.Catalog?.ForResult(item.Id) is { Count:>0 })
            {
                any=true;
                if(ImGui.Selectable(localization.Format("SourceLinks.Craft",item.Name)+$"##SrcCraft{item.Id}")){SelectItem(item.Id);page=Page.CraftSimulation;IsOpen=true;}
            }
            if(gatheringCatalog.State.Catalog?.ForItem(item.Id) is { Count:>0 } gathered)
            {
                any=true;
                if(ImGui.Selectable(localization.Format("SourceLinks.Gathering",gathered[0].TypeName,gathered[0].Zone)+$"##SrcGather{item.Id}")){SelectItem(item.Id);page=Page.Gathering;IsOpen=true;}
            }
        }
        return any;
    }
}
