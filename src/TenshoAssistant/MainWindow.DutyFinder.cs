using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Duties;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Duty loot beside the game's Duty Finder (0.185), as Duty Loot Preview: selecting a dungeon, trial or raid (not a roulette) shows its loot
/// in a compact panel (0.186) — LuminaSupplemental first, Garland Tools requested once per selected duty and cached (MainWindow.Lumina.cs) —
/// grouped by origin (rewards, each boss, coffers), with the kind filter and « not obtained only » (0.207), the ✓ / ✗ badge, the full item
/// tooltip and the item tools menu. Setting ShowDutyFinderLoot; the selection is copied by Duties/DutyFinderReader on the framework thread.
/// </summary>
internal sealed partial class MainWindow
{
    private DutyFinderService? dutyFinder;
    // 0.331: the panel has its own DutyLootService on the shared Garland transport (cadence, disk cache and switch shared): the duty the
    // game's Duty Finder shows no longer replaces the one chosen on the Duty loot page, nor cancels its request.
    private DutyLootService? dutyFinderLoot;
    private uint dutyFinderRequested;
    private int dutyFinderFilter;
    private string dutyFinderQuery=string.Empty;
    // 0.325: counts and groups worked out once per input (document, catalogue, filters, query, language), not on every frame.
    private sealed record DutyFinderGroup(DutyLootEntry First,uint[] Ids);
    private (DutyLootDocument? Document,ItemCatalog? Items,int Filter,string Query,int Kind,bool Missing,object? Collections,int Revision,string[] Labels,bool NoCoffers,DutyFinderGroup[] Groups) dutyFinderView;
    internal void ConfigureDutyFinder(DutyFinderService service,DutyLootService loot){dutyFinder=service;dutyFinderLoot=loot;}

    internal void DrawDutyFinderPanel()
    {
        if(windowDisposed || !configuration.ShowDutyFinderLoot || dutyFinder?.Selection is not { } selection || dutyFinderLoot is not { } loot)return;
        // 0.207: no panel for a roulette (no fixed loot) or when no duty is selected.
        if(selection.Kind!=2 || selection.Id==0)return;
        Panels?.Show("TenshoDutyFinder",localization.Get("DutyFinder.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove,()=>{ImGui.SetNextWindowPos(new Vector2(selection.X+selection.Width+6,selection.Y)+WindowMoved("ContentsFinder",selection.X,selection.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(400,Math.Max(320,selection.Height)),ImGuiCond.Always);},()=>
        {
            if(selection.Kind==1)
            {
                ImGui.TextWrapped(localization.Get("DutyFinder.Roulette"));
                // 0.372 (g, M-02): whether this roulette was done today (selection id = ContentRoulette row; to confirm in game).
                if(selection.Id is > 0 and <= byte.MaxValue && Reminders.GameTimers.RouletteDone((byte)selection.Id) is { } done)
                    ImGui.TextColored(done ? Success:Accent,localization.Get(done ? "Roulettes.Done":"Roulettes.ToDo"));
                return;
            }
            if(selection.Kind!=2 || selection.Id==0){ImGui.TextWrapped(localization.Get("DutyFinder.Select"));return;}
            if(duties.State.Catalog?.Find(selection.Id) is not { } duty){ImGui.TextWrapped(localization.Format("DutyFinder.Unknown",selection.Id));return;}
            if(loot.State.Duty?.DutyId!=duty.DutyId){loot.Select(duty);dutyFinderQuery="";}
            // One request per selected duty (the player's selection), served from the cache when fresh; 0.326: asked once more when
            // Garland is turned back on after a refusal of the switch.
            if(configuration.CommunityGarland && !loot.State.Loading && SourceOff(loot.State.Error))dutyFinderRequested=0;
            if(dutyFinderRequested!=duty.DutyId && loot.State is { Loading:false, Document:null })
            {dutyFinderRequested=duty.DutyId;_=loot.RequestAsync();}
            DrawDutyFinderLoot(loot,duty);
        });
    }

    private void DrawDutyFinderLoot(DutyLootService loot,DutyDefinition duty)
    {
        var state=loot.State;
        DrawItemIcon(new ItemDefinition(0,duty.Name,false,duty.Icon),new Vector2(28));ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextColored(Accent,duty.Name);
        ImGui.TextDisabled(localization.Format("DutyCounts.Level",duty.Category,duty.Level));
        DrawDutyUnlock(duty);
        ImGui.EndGroup();
        // Source, freshness and refresh on one line; the long explanations go into the (i) tooltip.
        if(state.Loading)ImGui.TextDisabled(localization.Get("DutyUi.Loading"));
        else if(state.Document is { } fetched)ImGui.TextDisabled(localization.Format("DutyFinder.Fetched",fetched.FetchedAt.ToLocalTime().ToString("g")));
        else if(state.Error is { } error)ImGui.TextColored(Accent,localization.Get(SourceOff(error) ? "Advanced.SourceOffTip":"ServiceErrors.Duty"));
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        // 0.325: the ↻ button has its own tooltip, drawn once the icon font is popped.
        var refreshHovered=false;
        try
        {
            ImGui.BeginDisabled(state.Loading || !configuration.CommunityGarland);
            try{if(ImGui.SmallButton(FontAwesomeIcon.SyncAlt.ToIconString()+"##DutyFinderRefresh"))_=loot.RequestAsync(true);}
            finally{ImGui.EndDisabled();}
            refreshHovered=ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
            ImGui.SameLine();ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
        }
        finally{ImGui.PopFont();}
        if(refreshHovered)WrappedTooltip(localization.Get(configuration.CommunityGarland ? "DutyFinder.RefreshTip":"Advanced.SourceOffTip"));
        else if(ImGui.IsItemHovered())
            WrappedTooltip(localization.Get("DutyUi.Source")+(state.Document is { HasRewards:true, HasFights:true, HasCoffers:true } ? "":"\n"+localization.Get("DutyUi.Partial")));
        if(DutyDocument(state,true) is not { } document)return;
        if(lumina?.State.Data?.ForDuty(duty.DutyId) is { Count:>0 })ImGui.TextDisabled(localization.Get("DutyFinder.Lumina"));
        var view=DutyFinderViewOf(document);
        // Counts in the filter names; a kind the source leaves empty is said so (0.187: Garland has no coffer for recent duties).
        if(view.NoCoffers)ImGui.TextColored(Accent,localization.Get("DutyFinder.NoCoffers"));
        ImGui.SetNextItemWidth(130);
        if(ImGui.BeginCombo("##DutyFinderFilter",view.Labels[dutyFinderFilter]))
        {
            try{for(var index=0;index<view.Labels.Length;index++)if(ImGui.Selectable(view.Labels[index],dutyFinderFilter==index))dutyFinderFilter=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("DutyUi.SourceFilterTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutyFinderQuery",localization.Get("DutyUi.ItemQuery"),ref dutyFinderQuery,64);
        Tip("DutyUi.ItemQueryTip");
        var items=itemCatalog.State.Catalog;
        DrawLootKindFilter("DutyFinder");
        // The filters drawn above may have changed this frame: the view follows on the next one.
        var groups=view.Groups;
        if(groups.Length==0){ImGui.TextDisabled(localization.Get("DutyUi.Empty"));return;}
        var childVisible=ImGui.BeginChild("DutyFinderList",Vector2.Zero);
        try
        {
            if(!childVisible)return;
            foreach(var group in groups)
            {
                var first=group.First;
                if(!TenshoAssistant.UI.UiComponents.Header(DutySourceLabel(first)+$" ({group.Ids.Length})###DutyFinderGroup{first.Kind}{first.SourceIndex}",ImGuiTreeNodeFlags.DefaultOpen))continue;
                foreach(var id in group.Ids)
                {
                    var item=items?.Find(id);
                    DrawItemIcon(item,new Vector2(24));
                    var hovered=ImGui.IsItemHovered();ImGui.SameLine();
                    ImGui.Selectable((item?.Name ?? localization.Format("BankUi.Item",id))+$"##DutyFinderItem{first.Kind}{first.SourceIndex}/{id}",false,ImGuiSelectableFlags.None,new Vector2(0,24));
                    if(hovered || ImGui.IsItemHovered())DrawItemInfoTooltip(id);
                    ItemLinksMenu($"DutyFinderLinks{first.Kind}{first.SourceIndex}/{id}",item);
                }
            }
        }
        finally{ImGui.EndChild();}
    }

    // 0.325: the item search also takes an exact item ID, as its help text says (the Duty loot page did already).
    private (DutyLootDocument? Document,ItemCatalog? Items,int Filter,string Query,int Kind,bool Missing,object? Collections,int Revision,string[] Labels,bool NoCoffers,DutyFinderGroup[] Groups) DutyFinderViewOf(DutyLootDocument document)
    {
        var items=itemCatalog.State.Catalog;var query=dutyFinderQuery.Trim();var cached=dutyFinderView;
        if(ReferenceEquals(cached.Document,document) && ReferenceEquals(cached.Items,items) && cached.Filter==dutyFinderFilter && cached.Query==query
            && cached.Kind==lootKind && cached.Missing==lootMissingOnly && (!lootMissingOnly || ReferenceEquals(cached.Collections,collections.State))
            && cached.Revision==localization.Revision)return cached;
        int Count(int kind)=>document.Entries.Where(entry=>(int)entry.Kind==kind).Select(entry=>entry.ItemId).Distinct().Count();
        var labels=new[]{localization.Get("DutyUi.AllSources"),$"{localization.Get("DutyUi.Rewards")} ({Count(0)})",
            $"{localization.Get("DutyUi.FightCoffers")} ({Count(1)})",$"{localization.Get("DutyUi.ZoneCoffers")} ({Count(2)})"};
        var groups=document.Entries.Where(entry=>dutyFinderFilter==0 || (int)entry.Kind==dutyFinderFilter-1)
            .Where(entry=>LootKindMatches(items?.Find(entry.ItemId)))
            .Where(entry=>query.Length==0 || (items?.Find(entry.ItemId)?.Name ?? "").Contains(query,StringComparison.CurrentCultureIgnoreCase)
                || entry.ItemId.ToString(CultureInfo.InvariantCulture)==query)
            .GroupBy(entry=>(entry.Kind,entry.SourceIndex)).OrderBy(group=>group.Key.Kind).ThenBy(group=>group.Key.SourceIndex)
            .Select(group=>new DutyFinderGroup(group.First(),group.Select(entry=>entry.ItemId).Distinct().ToArray())).ToArray();
        dutyFinderView=(document,items,dutyFinderFilter,query,lootKind,lootMissingOnly,collections.State,localization.Revision,labels,Count(1)==0 && Count(2)==0,groups);
        return dutyFinderView;
    }

    // Right click on an item: the other tools for it (0.186) — item sheet, market, recipes, collection, then its sources.
    // 0.233: the same useful tools as the inventory menu and the context item panel (RelevantRoutes), then the item's sources.
    /// <summary>0.561 (I9): prints a link to this item in the player's own chat, set by the plugin.</summary>
    internal Action<uint>? ItemLinkToChat { get; set; }
    /// <summary>
    /// 0.762 (X-15): the point to go to for an item: one whose place on the map is known, an open one first (a point with no hours is always open), then one in the zone
    /// the character stands in, then the cheapest teleport; ties by level. Null when the item is not gathered or no point is placed.
    /// </summary>
    private (Gathering.GatheringSource Source,(Maps.MapDefinition Map,Vector2 Pixel) Target)? GoThereSource(uint item)
    {
        if(gatheringCatalog.State.Catalog?.ForItem(item) is not { Count:>0 } sources)return null;
        var here=ListPanelZoneHere();var now=DateTimeOffset.UtcNow;
        return sources.Where(source=>!source.IsHidden).Select(source=>(Source:source,Target:GatheringPointPixel(source))).Where(pair=>pair.Target is not null)
            .Select(pair=>(pair.Source,Target:pair.Target!.Value))
            .OrderBy(pair=>pair.Source.Timing.Kind!=Gathering.GatheringScheduleKind.Limited
                || Gathering.GatheringTime.Next(pair.Source.Timing,now,1).Openings.FirstOrDefault() is { } next && next.StartsAt<=now && now<next.EndsAt ? 0:1)
            .ThenBy(pair=>pair.Source.Zone==here ? 0:1).ThenBy(pair=>ListPanelZoneCost(pair.Source.Zone) ?? uint.MaxValue).ThenBy(pair=>pair.Source.Level)
            .Cast<(Gathering.GatheringSource,(Maps.MapDefinition,Vector2))?>().FirstOrDefault();
    }

    private void ItemLinksMenu(string id,ItemDefinition? item)
    {
        if(item is null || !ImGui.BeginPopupContextItem(id))return;
        try
        {
            ImGui.TextColored(Accent,item.Name);
            foreach(var route in RelevantRoutes(item.Id))
                if(ImGui.Selectable(RouteTitle(route,item.Id)+$"##Route{route}")){SelectItem(item.Id);OpenRoute(route,item.Id);IsOpen=true;}
            // 0.310 (user request): the game's Try On window, for every piece of equipment shown by Tensho.
            if(TryOnItem is not null && (item.Equipment&(Items.EquipmentSlots.Wearable|Items.EquipmentSlots.MainHand|Items.EquipmentSlots.OffHand))!=0
                && ImGui.Selectable(localization.Get("ItemMenu.TryOn")+$"##TryOn{item.Id}"))TryOnItem(item.Id);
            // 0.360 (g, C-09): add to or remove from the wishlist.
            DrawWishlistToggle(item);
            // 0.826 (X-58): add its recipe to one of the crafting lists.
            if(CraftListAdds(item.Id) is { Count:>0 } adds && ImGui.BeginMenu(localization.Get("ItemMenu.AddToLists")+$"##AddToLists{item.Id}"))
            {
                try{foreach(var (label,open) in adds)if(ImGui.MenuItem(label))open();}
                finally{ImGui.EndMenu();}
            }
            // 0.762 (X-15, user request): « Go there » for an item that is gathered, as on the Gathering page: flag, Tensho map and the nearest aetheryte's teleport.
            if(GoThereSource(item.Id) is { } go && ImGui.Selectable(localization.Get("GatheringDetails.GoThere")+$"##GoThere{item.Id}"))
            {
                if(configuration.GoThereEquip)EquipGatheringGearset(go.Source);
                AskNearestAetheryteAt(go.Target.Map,go.Target.Pixel,string.IsNullOrWhiteSpace(go.Source.Place) ? go.Source.Zone:go.Source.Place,flag:true,direct:configuration.GoThereTeleport);
            }
            // 0.561 (I8, I9): the item's name, a link in your own chat, and its pages on the web; nothing is sent to the game's server.
            if(ImGui.Selectable(localization.Get("ItemMenu.CopyName")+$"##CopyName{item.Id}"))ImGui.SetClipboardText(item.Name);
            if(ItemLinkToChat is not null && ImGui.Selectable(localization.Get("ItemMenu.LinkChat")+$"##LinkChat{item.Id}"))ItemLinkToChat(item.Id);
            if(ImGui.BeginMenu(localization.Get("ItemMenu.OpenOn")+$"##OpenOn{item.Id}"))
            {
                try
                {
                    if(ImGui.MenuItem("Universalis"))Dalamud.Utility.Util.OpenLink(ExternalLinks.Universalis(item.Id));
                    if(ImGui.MenuItem("Garland Tools"))Dalamud.Utility.Util.OpenLink(ExternalLinks.Garland(item.Id));
                    if(ImGui.MenuItem("Teamcraft"))Dalamud.Utility.Util.OpenLink(ExternalLinks.Teamcraft(localization.CurrentCode,item.Id));
                }
                finally{ImGui.EndMenu();}
            }
            // 0.405 (user request): the game's crafting / gathering log, opened on this item.
            DrawGameLogEntries(item);
            ImGui.Separator();
            if(!DrawSourceLinks([item]))ImGui.TextDisabled(localization.Get("SourceLinks.None"));
        }
        finally{ImGui.EndPopup();}
    }
    // 0.266: a Triple Triad card has no Collections entry; its « collection » is the Triple Triad tool, and the menu says so.
    internal string RouteTitle(ItemContextRoute route,uint id)
        =>route==ItemContextRoute.Collection && CollectionOf(itemCatalog.State.Catalog?.Find(id))?.Kind==Collections.CollectionKind.TripleTriad
            ? localization.Get("ItemMenu.OpenTriad"):RouteLabel(route);
    private string RouteLabel(ItemContextRoute route)=>route switch
    {
        ItemContextRoute.Usages=>localization.Get("ItemUsesUi.Title"),
        ItemContextRoute.Stocks=>localization.Get("ItemsUi.Stocks"),
        ItemContextRoute.Craft=>localization.Get("ItemsUi.Craft"),
        ItemContextRoute.Gathering=>localization.Get("GatheringDetails.Title"),
        ItemContextRoute.NpcVendors=>localization.Get("NpcUi.Title"),
        ItemContextRoute.Market=>localization.Get("ItemLinks.Market"),
        ItemContextRoute.DutyLoot=>localization.Get("ItemMenu.DutyLoot"),
        ItemContextRoute.MonsterLoot=>localization.Get("ItemMenu.MonsterLoot"),
        ItemContextRoute.Collection=>localization.Get("HoverUi.OpenCollection"),
        ItemContextRoute.Quest=>localization.Get("ItemMenu.Quest"),
        ItemContextRoute.Sources=>localization.Get("ItemMenu.Sources"),
        _=>localization.Get("ItemMenu.Tools"),
    };
}
