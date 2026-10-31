using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Duties;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Duty loot beside the game's Duty Finder (0.185), as Duty Loot Preview: selecting a dungeon, trial or raid shows its loot (Garland Tools,
/// cached) in a compact panel (0.186): items grouped by origin (rewards, each boss, coffers), ✓ when the item's collection is obtained, full
/// item tooltip on hover, right click = links to the other tools (item sheet, market, recipes, collection, sources).
/// </summary>
internal sealed partial class MainWindow
{
    private DutyFinderService? dutyFinder;
    private uint dutyFinderRequested;
    private int dutyFinderFilter;
    private string dutyFinderQuery=string.Empty;
    internal void ConfigureDutyFinder(DutyFinderService service)=>dutyFinder=service;

    internal void DrawDutyFinderPanel()
    {
        if(windowDisposed || !configuration.ShowDutyFinderLoot || dutyFinder?.Selection is not { } selection)return;
        // 0.207: no panel for a roulette (no fixed loot) or when no duty is selected.
        if(selection.Kind!=2 || selection.Id==0)return;
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(selection.X+selection.Width+6,selection.Y),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(400,Math.Max(320,selection.Height)),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("DutyFinder.Title")+"###TenshoDutyFinder",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove);
        try
        {
            if(!visible)return;
            if(selection.Kind==1){ImGui.TextWrapped(localization.Get("DutyFinder.Roulette"));return;}
            if(selection.Kind!=2 || selection.Id==0){ImGui.TextWrapped(localization.Get("DutyFinder.Select"));return;}
            if(duties.State.Catalog?.Find(selection.Id) is not { } duty){ImGui.TextWrapped(localization.Format("DutyFinder.Unknown",selection.Id));return;}
            if(dutyLoot.State.Duty?.DutyId!=duty.DutyId){dutyLoot.Select(duty);dutyFinderQuery="";}
            // One request per selected duty (the player's selection), served from the cache when fresh.
            if(dutyFinderRequested!=duty.DutyId && dutyLoot.State is { Loading:false, Document:null })
            {dutyFinderRequested=duty.DutyId;_=dutyLoot.RequestAsync();}
            DrawDutyFinderLoot(duty);
        }
        finally{ImGui.End();}
    }

    private void DrawDutyFinderLoot(DutyDefinition duty)
    {
        var state=dutyLoot.State;
        DrawItemIcon(new ItemDefinition(0,duty.Name,false,duty.Icon),new Vector2(28));ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextColored(Accent,duty.Name);
        ImGui.TextDisabled(localization.Format("DutyCounts.Level",duty.Category,duty.Level));
        ImGui.EndGroup();
        // Source, freshness and refresh on one line; the long explanations go into the (i) tooltip.
        if(state.Loading)ImGui.TextDisabled(localization.Get("DutyUi.Loading"));
        else if(state.Document is { } fetched)ImGui.TextDisabled(localization.Format("DutyFinder.Fetched",fetched.FetchedAt.ToLocalTime().ToString("g")));
        else if(state.Error is not null)ImGui.TextColored(Accent,localization.Get("ServiceErrors.Duty"));
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            ImGui.BeginDisabled(state.Loading);
            try{if(ImGui.SmallButton(FontAwesomeIcon.SyncAlt.ToIconString()+"##DutyFinderRefresh"))_=dutyLoot.RequestAsync(true);}
            finally{ImGui.EndDisabled();}
            ImGui.SameLine();ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
        }
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())
            WrappedTooltip(localization.Get("DutyUi.Source")+(state.Document is { HasRewards:true, HasFights:true, HasCoffers:true } ? "":"\n"+localization.Get("DutyUi.Partial")));
        if(DutyDocument(state) is not { } document)return;
        if(lumina?.State.Data?.ForDuty(duty.DutyId) is { Count:>0 })ImGui.TextDisabled(localization.Get("DutyFinder.Lumina"));
        // Counts in the filter names; a kind the source leaves empty is said so (0.187: Garland has no coffer for recent duties).
        int Count(int kind)=>document.Entries.Where(entry=>(int)entry.Kind==kind).Select(entry=>entry.ItemId).Distinct().Count();
        var labels=new[]{localization.Get("DutyUi.AllSources"),$"{localization.Get("DutyUi.Rewards")} ({Count(0)})",
            $"{localization.Get("DutyUi.FightCoffers")} ({Count(1)})",$"{localization.Get("DutyUi.ZoneCoffers")} ({Count(2)})"};
        if(Count(1)==0 && Count(2)==0)ImGui.TextColored(Accent,localization.Get("DutyFinder.NoCoffers"));
        ImGui.SetNextItemWidth(130);
        if(ImGui.BeginCombo("##DutyFinderFilter",labels[dutyFinderFilter]))
        {
            try{for(var index=0;index<labels.Length;index++)if(ImGui.Selectable(labels[index],dutyFinderFilter==index))dutyFinderFilter=index;}
            finally{ImGui.EndCombo();}
        }
        ImGui.SameLine();ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutyFinderQuery",localization.Get("DutyUi.ItemQuery"),ref dutyFinderQuery,64);
        var items=itemCatalog.State.Catalog;var query=dutyFinderQuery.Trim();
        DrawLootKindFilter("DutyFinder");
        var groups=document.Entries.Where(entry=>dutyFinderFilter==0 || (int)entry.Kind==dutyFinderFilter-1)
            .Where(entry=>LootKindMatches(items?.Find(entry.ItemId)))
            .Where(entry=>query.Length==0 || (items?.Find(entry.ItemId)?.Name ?? "").Contains(query,StringComparison.CurrentCultureIgnoreCase))
            .GroupBy(entry=>(entry.Kind,entry.SourceIndex)).OrderBy(group=>group.Key.Kind).ThenBy(group=>group.Key.SourceIndex).ToArray();
        if(groups.Length==0){ImGui.TextDisabled(localization.Get("DutyUi.Empty"));return;}
        var childVisible=ImGui.BeginChild("DutyFinderList",Vector2.Zero);
        try
        {
            if(!childVisible)return;
            foreach(var group in groups)
            {
                var first=group.First();
                if(!ImGui.CollapsingHeader(DutySourceLabel(first)+$" ({group.Select(entry=>entry.ItemId).Distinct().Count()})###DutyFinderGroup{group.Key.Kind}{group.Key.SourceIndex}",ImGuiTreeNodeFlags.DefaultOpen))continue;
                foreach(var id in group.Select(entry=>entry.ItemId).Distinct())
                {
                    var item=items?.Find(id);
                    DrawItemIcon(item,new Vector2(24));DrawCollectionMark(item);
                    var hovered=ImGui.IsItemHovered();ImGui.SameLine();
                    ImGui.Selectable((item?.Name ?? localization.Format("BankUi.Item",id))+$"##DutyFinderItem{group.Key.Kind}{group.Key.SourceIndex}/{id}",false,ImGuiSelectableFlags.None,new Vector2(0,24));
                    if(hovered || ImGui.IsItemHovered())DrawItemInfoTooltip(id);
                    ItemLinksMenu($"DutyFinderLinks{group.Key.Kind}{group.Key.SourceIndex}/{id}",item);
                }
            }
        }
        finally{ImGui.EndChild();}
    }

    // Right click on an item: the other tools for it (0.186) — item sheet, market, recipes, collection, then its sources.
    private void ItemLinksMenu(string id,ItemDefinition? item)
    {
        if(item is null || !ImGui.BeginPopupContextItem(id))return;
        try
        {
            ImGui.TextColored(Accent,item.Name);
            if(ImGui.Selectable(localization.Get("ItemLinks.Sheet"))){SelectItem(item.Id);page=Page.Items;IsOpen=true;}
            if(item.IsMarketable && ImGui.Selectable(localization.Get("ItemLinks.Market"))){SelectItem(item.Id);OpenContextTool("Price Insight");IsOpen=true;}
            if(ImGui.Selectable(localization.Get("ItemLinks.Recipes"))){SelectItem(item.Id);OpenContextTool("Craftimizer");IsOpen=true;}
            if(CollectionOf(item) is { } entry && ImGui.Selectable(localization.Get("HoverUi.OpenCollection")))
            {
                if(entry.Kind==Collections.CollectionKind.TripleTriad)page=Page.TripleTriad;
                else{page=Page.Collections;collectionKind=entry.Kind;focusCollectionKind=entry.Kind;if(entry.Kind==Collections.CollectionKind.Orchestrion)focusOrchestrionTab=true;}
                collectionQuery=entry.Name;IsOpen=true;
            }
            ImGui.Separator();
            if(!DrawSourceLinks([item]))ImGui.TextDisabled(localization.Get("SourceLinks.None"));
        }
        finally{ImGui.EndPopup();}
    }
}
