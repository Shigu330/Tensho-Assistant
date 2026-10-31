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

    /// <summary>Menu entries for the sources of these items; false when none is known.</summary>
    private bool DrawSourceLinks(IReadOnlyList<ItemDefinition> items)
    {
        var any=false;
        var ids=items.Select(item=>item.Id).ToArray();
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
            foreach(var trade in itemSources?.Get(item.Id).List?.Trades ?? [])
            {
                any=true;
                var currency=itemCatalog.State.Catalog?.Find(trade.CurrencyId);
                if(ImGui.Selectable(localization.Format("SourceLinks.Trade",trade.Amount,currency?.Name ?? trade.CurrencyId.ToString())+$"##SrcTrade{item.Id}/{trade.CurrencyId}"))
                {SelectItem(trade.CurrencyId);page=Page.Items;IsOpen=true;}
            }
        }
        foreach(var item in items.Take(3))
        {
            if(npcVendors?.State.Catalog?.ForItem(item.Id) is { Count:>0 } offers)
                foreach(var vendor in offers.Select(offer=>offer.Vendor).DistinctBy(vendor=>vendor.NpcId).Take(6))
                {
                    any=true;
                    var place=vendor.Locations.FirstOrDefault();
                    if(ImGui.Selectable(localization.Format("SourceLinks.Vendor",NpcName(vendor),place is null ? localization.Get("NpcUi.NoLocation"):NpcPlace(place))+$"##SrcNpc{item.Id}/{vendor.NpcId}"))
                    {npcVendorFocus=vendor.NpcId;page=Page.NpcVendors;IsOpen=true;}
                    if(place is not null && maps.State.Catalog?.Find(place.MapId) is { } map
                       && ImGui.Selectable("    "+localization.Get("SourceLinks.Flag")+$"##SrcFlag{item.Id}/{vendor.NpcId}"))PlaceFlagFromMapView(map,place.X,place.Y);
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
