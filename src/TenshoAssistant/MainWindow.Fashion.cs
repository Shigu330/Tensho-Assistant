using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fashion;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string fashionHintQuery="",fashionItemQuery="";
    private uint fashionHintId;
    private EquipmentSlots fashionSlot;
    private bool fashionShowItems;
    private FashionHintCatalog? fashionNativeView;
    private FashionDataset? fashionHintDataView,fashionItemDataView;
    private string fashionHintQueryView="",fashionItemQueryView="";
    private uint fashionItemHintView;
    private EquipmentSlots fashionSlotView;
    private ItemCatalog? fashionItemsView;
    private FashionHint[] fashionAllHints=[],fashionHintMatches=[];
    private IReadOnlyList<FashionCandidate> fashionCandidates=[];
    private bool fashionHintsNeedFilter=true;
    private int fashionLanguageRevision=-1;
    private static readonly (EquipmentSlots Slot,string Name)[] FashionSlots=
    [ (EquipmentSlots.None,"All"),(EquipmentSlots.Head,"Head"),(EquipmentSlots.Body,"Body"),
      (EquipmentSlots.Hands,"Hands"),(EquipmentSlots.Legs,"Legs"),(EquipmentSlots.Feet,"Feet"),
      (EquipmentSlots.Ears,"Ears"),(EquipmentSlots.Neck,"Neck"),(EquipmentSlots.Wrists,"Wrists"),(EquipmentSlots.Ring,"Ring") ];
    private void DrawFashion()
    {
        Heading(localization.Get("FashionUi.Title"),localization.Get("FashionUi.Description"));
        var native=fashionHints.State;
        // 0.213: the week first (theme, 11 slots of the game window, gear per slot), then every category.
        if(native.Catalog is not null && ImGui.CollapsingHeader(localization.Get("FashionWeek.Title")+"###FashionWeek",ImGuiTreeNodeFlags.DefaultOpen))DrawFashionWeek();
        if(!ImGui.CollapsingHeader(localization.Get("FashionWeek.AllTitle")+"###FashionAll",fashionShowItems ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None))return;
        ImGui.TextWrapped(localization.Get("FashionUi.Help"));
        if(native.Catalog is not { } catalog) { if(native.Error is { } hintError)DrawDataError(hintError,"ServiceErrors.Fashion");else ImGui.TextWrapped(localization.Get("FashionUi.LoadingHints")); return; }
        var state=fashionData.State; var dataset=state.Dataset;
        if(state.Loading) { if(ImGui.Button(localization.Get("FashionUi.Cancel"))) fashionData.Cancel(); ImGui.SameLine(); ImGui.TextDisabled(localization.Get("FashionUi.Loading")); }
        else
        {
            if(ImGui.Button(localization.Get("FashionUi.Fetch"))) _=fashionData.RequestAsync();
            if(dataset is not null) { ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("FashionUi.Refresh"))) _=fashionData.RequestAsync(true); }
        }
        if(state.Error is { } error) DrawDataError(error,"ServiceErrors.Fashion");
        if(dataset is not null)
        {
            ImGui.TextDisabled(localization.Format("FashionDates.Fetched",dataset.FetchedAt.ToLocalTime().ToString("g")));
            if(dataset.ProviderUpdatedAt is { } date) ImGui.TextDisabled(localization.Format("FashionDates.Updated",date.ToLocalTime().ToString("g")));
            if(dataset.DateStatus!=FashionDateStatus.Parsed) ImGui.TextWrapped(dataset.DateStatus==FashionDateStatus.Future
                ? localization.Get("FashionUi.Future")
                : localization.Get("FashionUi.NoDate"));
            DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Fashion,dataset.FetchedAt);
        }
        ImGui.TextDisabled(localization.Get("FashionUi.Source"));
        if(!ReferenceEquals(fashionNativeView,catalog) || !ReferenceEquals(fashionHintDataView,dataset) || fashionLanguageRevision!=localization.Revision)
        {
            var known=catalog.Hints.ToDictionary(hint=>hint.Id);
            fashionAllHints=catalog.Hints.Concat((dataset?.HintIds ?? []).Where(id=>!known.ContainsKey(id)).Select(id=>new FashionHint(id,localization.Format("UiFragments.UnresolvedHint",id))))
                .OrderBy(hint=>hint.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(hint=>hint.Id).ToArray();
            fashionNativeView=catalog; fashionHintDataView=dataset; fashionHintsNeedFilter=true;fashionLanguageRevision=localization.Revision;
        }
        if(fashionShowItems && fashionHintId!=0)
        {
            if(ImGui.SmallButton(localization.Get("FashionUi.Back"))) fashionShowItems=false;
            DrawFashionItems(dataset); return;
        }
        ImGui.SetNextItemWidth(-1); ImGui.InputTextWithHint("##FashionHintSearch",localization.Get("FashionUi.HintQuery"),ref fashionHintQuery,128);
        if(fashionHintsNeedFilter || fashionHintQueryView!=fashionHintQuery)
        {
            var query=fashionHintQuery.Trim();
            fashionHintMatches=fashionAllHints.Where(hint=>query.Length==0 || hint.Id.ToString(CultureInfo.InvariantCulture)==query
                || CultureInfo.CurrentCulture.CompareInfo.IndexOf(hint.Name,query,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0).ToArray();
            fashionNativeView=catalog; fashionHintDataView=dataset; fashionHintQueryView=fashionHintQuery;
            fashionHintsNeedFilter=false;
        }
        ImGui.TextDisabled(localization.Format("FashionDates.Hints",fashionHintMatches.Length));
        if(!ImGui.BeginTable("FashionHints",2,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("FashionUi.Hint"),ImGuiTableColumnFlags.WidthStretch,3); ImGui.TableSetupColumn(localization.Get("FashionUi.ReportedItems")); ImGui.TableHeadersRow();
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(fashionHintMatches.Length);
                while(clipper.Step())
                for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                {
                    var hint=fashionHintMatches[index]; ImGui.TableNextRow(); ImGui.TableNextColumn();
                    if(ImGui.Selectable($"{hint.Name}##FashionHint{hint.Id}",fashionHintId==hint.Id)) { fashionHintId=hint.Id; fashionShowItems=true; fashionItemQuery=""; }
                    if(developerMode && ImGui.IsItemHovered()) WrappedTooltip($"ID: {hint.Id}");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(dataset is null ? localization.Get("FashionUi.NotFetched") : dataset.ForHint(hint.Id).Count.ToString(CultureInfo.InvariantCulture));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }
    private void DrawFashionItems(FashionDataset? dataset)
    {
        var hint=fashionAllHints.FirstOrDefault(entry=>entry.Id==fashionHintId);
        ImGui.TextColored(Accent,hint?.Name ?? localization.Format("UiFragments.Hint",fashionHintId));
        ImGui.SetNextItemWidth(240);
        if(ImGui.BeginCombo(localization.Get("FashionUi.Slot"),localization.Get("FashionSlot."+FashionSlots.First(pair=>pair.Slot==fashionSlot).Name)))
        {
            try { foreach(var pair in FashionSlots) if(ImGui.Selectable(localization.Get("FashionSlot."+pair.Name),pair.Slot==fashionSlot)) fashionSlot=pair.Slot; }
            finally { ImGui.EndCombo(); }
        }
        ImGui.SetNextItemWidth(-1); ImGui.InputTextWithHint("##FashionItemSearch",localization.Get("FashionUi.ItemQuery"),ref fashionItemQuery,128);
        if(dataset is null) { ImGui.TextWrapped(localization.Get("FashionUi.NeedFetch")); return; }
        var items=itemCatalog.State.Catalog;
        if(items is null) { DrawCatalogStatus(); return; }
        if(!ReferenceEquals(fashionItemDataView,dataset) || !ReferenceEquals(fashionItemsView,items) || fashionItemHintView!=fashionHintId
            || fashionItemQueryView!=fashionItemQuery || fashionSlotView!=fashionSlot)
        {
            fashionCandidates=dataset.Filter(fashionHintId,fashionSlot,items,fashionItemQuery);
            fashionItemDataView=dataset; fashionItemsView=items; fashionItemHintView=fashionHintId; fashionItemQueryView=fashionItemQuery; fashionSlotView=fashionSlot;
        }
        ImGui.TextDisabled(localization.Format("FashionDates.Results",fashionCandidates.Count,dataset.ForHint(fashionHintId).Count));
        if(fashionSlot!=EquipmentSlots.None) ImGui.TextWrapped(localization.Get("FashionUi.SlotHelp"));
        ImGui.TextWrapped(localization.Get("FashionUi.EquipHelp"));
        if(itemId>0)
        {
            ImGui.TextDisabled(localization.Format("UiFragments.Context",SelectedItem?.Name ?? localization.Format("BankUi.Item",itemId)));
            if(ImGui.SmallButton(localization.Get("FashionUi.Stocks"))) { page=Page.Items; focusStocksTab=true; }
            ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("FashionUi.Recipes"))) OpenContextTool("Craftimizer");
            ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("FashionUi.Prices"))) OpenContextTool("Price Insight");
        }
        if(fashionCandidates.Count==0) { ImGui.TextWrapped(localization.Get("FashionUi.Empty")); return; }
        RefreshRecipeHoldingsCache();
        if(!ImGui.BeginTable("FashionCandidates",3,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("FashionUi.Equipment"),ImGuiTableColumnFlags.WidthStretch,3); ImGui.TableSetupColumn(localization.Get("FashionUi.Reports")); ImGui.TableSetupColumn(localization.Get("FashionUi.Stock")); ImGui.TableHeadersRow();
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(fashionCandidates.Count);
                while(clipper.Step())
                for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                {
                    var entry=fashionCandidates[index]; var id=entry.Match.ItemId; var item=entry.Item;
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); DrawItemIcon(item,new Vector2(28)); var clicked=ImGui.IsItemClicked(); ImGui.SameLine();
                    var selectable=item is not null && id<=int.MaxValue;
                    ImGui.BeginDisabled(!selectable);
                    try { if((ImGui.Selectable($"{item?.Name ?? localization.Format("BankUi.Item",id)}##FashionItem{id}",itemId==id,ImGuiSelectableFlags.None,new Vector2(0,28)) || clicked) && selectable) SelectItem(id); }
                    finally { ImGui.EndDisabled(); }
                    if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) WrappedTooltip(localization.Format("UiFragments.Equipment",item?.Name ?? localization.Get("FashionUi.Unresolved"),item is null ? localization.Get("UiFragments.Unknown"):FashionSlotNames(item.Equipment))+(developerMode ? $"\nID {id}":""));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Match.Reports.ToString("N0"));
                    ImGui.TableNextColumn();
                    var stock=GetRecipeHoldings(id);
                    ImGui.TextUnformatted(!stock.HasObservation ? localization.Get("FashionUi.Unknown") : $"{stock.Total:N0}{(stock.HasCached ? localization.Get("UiFragments.CacheSuffix"):"")}{(stock.HasUnknown ? localization.Get("UiFragments.PartialSuffix"):"")}");
                    if(ImGui.IsItemHovered()) WrappedTooltip(localization.Get("FashionUi.StockHelp"));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }
    private string FashionSlotNames(EquipmentSlots slots)=>(slots&EquipmentSlots.Wearable)==EquipmentSlots.None ? localization.Get("FashionUi.NoSlots")
        : string.Join(", ",FashionSlots.Skip(1).Where(pair=>(slots&pair.Slot)!=0).Select(pair=>localization.Get("FashionSlot."+pair.Name)));
}
