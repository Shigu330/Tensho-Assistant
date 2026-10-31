using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Duties;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string dutyQuery = "";
    private string dutyCategory = "";
    private string dutyItemQuery = "";
    private int dutySourceFilter;
    private bool showDutyDetails;
    private DutyCatalog? dutyViewCatalog;
    private string dutyViewQuery = "";
    private string dutyViewCategory = "";
    private DutyDefinition[] dutyMatches = [];
    private string[] dutyCategories = [];
    private DutyLootDocument? dutyViewDocument;
    private ItemCatalog? dutyViewItems;
    private string dutyViewItemQuery = "";
    private int dutyViewSourceFilter = -1;
    private int dutyViewLanguageRevision=-1;
    private (uint Id, string Name, DutyLootEntry[] Sources)[] dutyItemMatches = [];

    private void DrawDutyLoot()
    {
        Heading(localization.Get("DutyUi.Title"), localization.Get("DutyUi.Description"));
        var catalogState = duties.State;
        if (catalogState.Catalog is not { } catalog)
        { if(catalogState.Error is { } error)DrawDataError(error,"ServiceErrors.Duty");else ImGui.TextWrapped(localization.Format("ServiceErrors.DutyLoading",catalogState.Copied,catalogState.Total)); return; }
        if (showDutyDetails && dutyLoot.State.Duty is not null)
        {
            if (ImGui.SmallButton(localization.Get("DutyUi.Back"))) showDutyDetails = false;
            DrawDutyLootDetails(); return;
        }
        if (dutyLoot.State.Duty is not null && ImGui.SmallButton(localization.Get("DutyUi.Selected"))) showDutyDetails = true;
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutySearch",localization.Get("DutyUi.Search"),ref dutyQuery,128);
        if (!ReferenceEquals(dutyViewCatalog,catalog))
            dutyCategories = catalog.Entries.Select(duty => duty.Category).Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal).OrderBy(name => name,StringComparer.CurrentCultureIgnoreCase).ToArray();
        ImGui.SetNextItemWidth(240);
        if (ImGui.BeginCombo(localization.Get("DutyUi.Category"),dutyCategory.Length == 0 ? localization.Get("DutyUi.All") : dutyCategory))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("DutyUi.All"),dutyCategory.Length == 0)) dutyCategory = "";
                foreach (var category in dutyCategories)
                    if (ImGui.Selectable(category,dutyCategory == category)) dutyCategory = category;
            }
            finally { ImGui.EndCombo(); }
        }
        if (!ReferenceEquals(dutyViewCatalog,catalog) || dutyViewQuery != dutyQuery || dutyViewCategory != dutyCategory)
        {
            var query = dutyQuery.Trim();
            dutyMatches = catalog.Entries.Where(duty => (dutyCategory.Length == 0 || duty.Category == dutyCategory)
                && (query.Length == 0 || duty.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)
                    || duty.DutyId.ToString(CultureInfo.InvariantCulture) == query)).ToArray();
            dutyViewCatalog = catalog; dutyViewQuery = dutyQuery; dutyViewCategory = dutyCategory;
        }
        ImGui.TextDisabled(localization.Format("DutyCounts.Duties",dutyMatches.Length));
        if (!ImGui.BeginTable("DutyCatalog",3,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("DutyUi.Duty"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("DutyUi.Type")); ImGui.TableSetupColumn(localization.Get("DutyUi.Level")); ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(dutyMatches.Length);
                while (clipper.Step())
                for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                {
                    var duty = dutyMatches[index];
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    DrawItemIcon(new ItemDefinition(0,duty.Name,false,duty.Icon),new Vector2(24));
                    var clicked = ImGui.IsItemClicked(); ImGui.SameLine();
                    if (ImGui.Selectable($"{duty.Name}##Duty{duty.DutyId}",dutyLoot.State.Duty?.DutyId == duty.DutyId,ImGuiSelectableFlags.None,new Vector2(0,24)) || clicked)
                    { dutyLoot.Select(duty); showDutyDetails = true; dutyItemQuery = ""; }
                    if (developerMode && ImGui.IsItemHovered()) WrappedTooltip($"Duty {duty.DutyId} · instance {duty.InstanceId} · territory {duty.TerritoryId}");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(duty.Category);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(duty.Level.ToString(CultureInfo.InvariantCulture));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }

    private void DrawDutyLootDetails()
    {
        var state = dutyLoot.State;
        if (state.Duty is not { } duty) return;
        DrawItemIcon(new ItemDefinition(0,duty.Name,false,duty.Icon),new Vector2(32)); ImGui.SameLine();
        ImGui.TextColored(Accent,duty.Name);
        ImGui.TextDisabled(localization.Format("DutyCounts.Level",duty.Category,duty.Level));
        if(developerMode)ImGui.TextDisabled($"Duty {duty.DutyId} / instance {duty.InstanceId}");
        if (state.Loading)
        { if (ImGui.Button(localization.Get("DutyUi.Cancel"))) dutyLoot.Cancel(); ImGui.SameLine(); ImGui.TextDisabled(localization.Get("DutyUi.Loading")); }
        else
        {
            if (ImGui.Button(localization.Get("DutyUi.Fetch"))) _ = dutyLoot.RequestAsync();
            if (state.Document is not null)
            { ImGui.SameLine(); if (ImGui.Button(localization.Get("DutyUi.Refresh"))) _ = dutyLoot.RequestAsync(true); }
        }
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Duty");
        ImGui.TextWrapped(localization.Get("DutyUi.Source"));
        if (DutyDocument(state) is not { } document)
        { ImGui.TextDisabled(localization.Get("DutyUi.NeedFetch")); return; }
        ImGui.TextDisabled(localization.Format("DutyCounts.Fetched",document.FetchedAt.ToLocalTime().ToString("g")));
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,document.FetchedAt);
        if (!document.HasRewards || !document.HasFights || !document.HasCoffers)
            ImGui.TextWrapped(localization.Get("DutyUi.Partial"));
        if (document.HasUnparsedFightDrops)
            ImGui.TextColored(Accent,localization.Get("DutyUi.Unparsed"));
        if (itemId > 0)
        {
            ImGui.TextDisabled(localization.Format("UiFragments.Context",SelectedItem?.Name ?? localization.Format("BankUi.Item",itemId)));
            if (ImGui.SmallButton(localization.Get("DutyUi.Stocks"))) { page = Page.Items; focusStocksTab = true; }
            ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("DutyUi.Recipes"))) OpenContextTool("Craftimizer");
            ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("DutyUi.Prices"))) OpenContextTool("Price Insight");
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##DutyItemSearch",localization.Get("DutyUi.ItemQuery"),ref dutyItemQuery,128);
        var labels = new[] { localization.Get("DutyUi.AllSources"), localization.Get("DutyUi.Rewards"), localization.Get("DutyUi.FightCoffers"), localization.Get("DutyUi.ZoneCoffers") };
        for (var index = 0; index < labels.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(labels[index],dutySourceFilter == index)) dutySourceFilter = index;
        }
        DrawLootKindFilter("DutyLoot");
        var items = itemCatalog.State.Catalog;
        if (dutyViewLootKind != lootKind || dutyViewLootMissing != lootMissingOnly) { dutyViewDocument = null; dutyViewLootKind = lootKind; dutyViewLootMissing = lootMissingOnly; }
        if (!ReferenceEquals(dutyViewDocument,document) || !ReferenceEquals(dutyViewItems,items)
            || dutyViewItemQuery != dutyItemQuery || dutyViewSourceFilter != dutySourceFilter || dutyViewLanguageRevision!=localization.Revision)
        {
            var query = dutyItemQuery.Trim();
            dutyItemMatches = document.Entries.Where(entry => dutySourceFilter == 0 || (int)entry.Kind == dutySourceFilter-1)
                .GroupBy(entry => entry.ItemId).Select(group => (Id:group.Key,Name:items?.Find(group.Key)?.Name ?? localization.Format("BankUi.Item",group.Key),Sources:group.ToArray()))
                .Where(item => LootKindMatches(items?.Find(item.Id)))
                .Where(item => query.Length == 0 || item.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)
                    || item.Id.ToString(CultureInfo.InvariantCulture) == query)
                .OrderBy(item => item.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.Id).ToArray();
            dutyViewDocument = document; dutyViewItems = items; dutyViewItemQuery = dutyItemQuery; dutyViewSourceFilter = dutySourceFilter;
            dutyViewLanguageRevision=localization.Revision;
        }
        ImGui.TextDisabled(localization.Format("DutyCounts.Items",dutyItemMatches.Length));
        if (dutyItemMatches.Length == 0) { ImGui.TextWrapped(localization.Get("DutyUi.Empty")); return; }
        if (!ImGui.BeginTable("DutyLootEntries",2,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("DutyUi.Item"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("DutyUi.Sources")); ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(dutyItemMatches.Length);
                while (clipper.Step())
                for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                {
                    var item = dutyItemMatches[index];
                    var local = items?.Find(item.Id);
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    DrawItemIcon(local,new Vector2(28)); var clicked = ImGui.IsItemClicked(); DrawCollectionMark(local); ImGui.SameLine();
                    var selectable = local is not null && item.Id <= int.MaxValue;
                    ImGui.BeginDisabled(!selectable);
                    try
                    {
                        if ((ImGui.Selectable($"{item.Name}##DutyLoot{item.Id}",itemId == item.Id,ImGuiSelectableFlags.None,new Vector2(0,28)) || clicked) && selectable)
                            SelectItem(item.Id);
                    }
                    finally { ImGui.EndDisabled(); }
                    // 0.186: full item tooltip and right click = links to the other tools.
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { if (selectable) DrawItemInfoTooltip(item.Id); else WrappedTooltip(localization.Get("UiFragments.MissingItem")); }
                    ItemLinksMenu($"DutyLootLinks{item.Id}",local);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Sources.Length == 1 ? DutySourceLabel(item.Sources[0]) : $"{item.Sources.Length} sources");
                    if (ImGui.IsItemHovered())
                        WrappedTooltip(string.Join('\n',item.Sources.Select(DutySourceLabel)));
                }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
    }
    private string DutySourceLabel(DutyLootEntry entry) => entry.Kind switch
    {
        DutyLootSourceKind.Reward => localization.Get("DutyUi.Reward"),
        DutyLootSourceKind.FightCoffer => localization.Format("DutyCounts.Fight",entry.SourceIndex+1) + (string.IsNullOrWhiteSpace(entry.SourceName) ? "" : $" · {entry.SourceName}"),
        _ => localization.Format("DutyCounts.Coffer",entry.SourceIndex+1)
    };
}
