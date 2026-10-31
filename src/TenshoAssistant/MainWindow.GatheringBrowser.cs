using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private GatheringCatalog? sourceViewCatalog;
    private ItemCatalog? sourceViewItems;
    private string sourceQuery = "";
    private string sourceZoneQuery = "";
    private string sourceViewQuery = "";
    private string sourceViewZoneQuery = "";
    private uint? sourceTypeFilter;
    private uint? sourceViewTypeFilter;
    private ItemDefinition[] sourceMatches = [];
    private GatheringSource[] sourceTypes = [];

    private void DrawGatheringBrowser()
    {
        Heading(localization.Get("GatheringBrowser.Title"),localization.Get("GatheringBrowser.Description"));
        DrawGatheringClock();
        if (gatheringCatalog.State.Catalog is not { } sources)
        {
            var state = gatheringCatalog.State;
            ImGui.TextWrapped(state.Error is null ? localization.Format("GatheringBrowser.Loading",state.Copied,state.Total):localization.Get("GatheringBrowser.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        if (itemCatalog.State.Catalog is not { } items) { DrawCatalogStatus(); return; }
        if (!ReferenceEquals(sourceViewCatalog, sources))
            sourceTypes = sources.Sources.DistinctBy(source => source.TypeId).OrderBy(source => source.TypeId).ToArray();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##SourceSearch", localization.Get("GatheringBrowser.Search"), ref sourceQuery, 128);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##SourceZoneSearch", localization.Get("GatheringBrowser.ZoneSearch"), ref sourceZoneQuery, 128);
        if (ImGui.SmallButton(localization.Get("GatheringBrowser.AllJobs")+"###SourceAllJobs")) sourceTypeFilter = null;
        foreach (var type in sourceTypes)
        {
            ImGui.SameLine();
            ImGui.PushID($"SourceType{type.TypeId}");
            try
            {
                DrawItemIcon(new ItemDefinition(0, type.TypeName, false, type.IconId), new Vector2(28));
                if (ImGui.IsItemClicked()) sourceTypeFilter = type.TypeId;
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(type.TypeName);
            }
            finally { ImGui.PopID(); }
        }
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("GatheringBrowser.Reset")+"###SourceReset")) { sourceQuery = ""; sourceZoneQuery = ""; sourceTypeFilter = null; }
        if (!ReferenceEquals(sourceViewCatalog, sources) || !ReferenceEquals(sourceViewItems, items)
            || sourceViewQuery != sourceQuery || sourceViewZoneQuery != sourceZoneQuery || sourceViewTypeFilter != sourceTypeFilter)
        {
            sourceMatches = sources.Sources
                .Where(source => (sourceTypeFilter is null || source.TypeId == sourceTypeFilter)
                    && (string.IsNullOrWhiteSpace(sourceZoneQuery) || SourceContains(source.Zone, sourceZoneQuery) || SourceContains(source.Place, sourceZoneQuery)))
                .Select(source => source.ItemId).Distinct().Select(items.Find).OfType<ItemDefinition>()
                .Where(item => string.IsNullOrWhiteSpace(sourceQuery) || SourceContains(item.Name, sourceQuery) || item.Id.ToString() == sourceQuery.Trim())
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.Id).ToArray();
            sourceViewCatalog = sources; sourceViewItems = items;
            sourceViewQuery = sourceQuery; sourceViewZoneQuery = sourceZoneQuery; sourceViewTypeFilter = sourceTypeFilter;
        }
        var selectedType = sourceTypeFilter is { } typeId ? sourceTypes.FirstOrDefault(source => source.TypeId == typeId)?.TypeName ?? localization.Get("GatheringBrowser.UnknownJob") : localization.Get("GatheringBrowser.AllJobs");
        ImGui.TextDisabled(localization.Format("GatheringBrowser.Count",sourceMatches.Length,selectedType));
        if (!ImGui.BeginTable("GatheringBrowserPanels", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Resizable)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("GatheringBrowser.Resources"), ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn(localization.Get("GatheringBrowser.Locations"), ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableNextRow(); ImGui.TableNextColumn();
            var height = Math.Max(120, ImGui.GetContentRegionAvail().Y);
            var showList = ImGui.BeginChild("SourceItems", new Vector2(0, height), true);
            try
            {
                if (showList)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    try
                    {
                        clipper.Begin(sourceMatches.Length);
                        while (clipper.Step())
                        for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                        {
                            var item = sourceMatches[index];
                            ImGui.PushID((int)item.Id);
                            try
                            {
                                DrawItemIcon(item, new Vector2(28));
                                var clicked = ImGui.IsItemClicked();
                                ImGui.SameLine();
                                if (ImGui.Selectable(item.Name, itemId == item.Id, ImGuiSelectableFlags.None, new Vector2(0, 28)) || clicked)
                                { SelectItem(item.Id); gatheringTypeFilter = sourceTypeFilter; }
                                if (ImGui.IsItemHovered()) ImGui.SetTooltip(item.Name);
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                    if (sourceMatches.Length == 0) ImGui.TextWrapped(localization.Get("GatheringBrowser.Empty"));
                }
            }
            finally { ImGui.EndChild(); }
            ImGui.TableNextColumn();
            var showDetails = ImGui.BeginChild("SourceDetails", new Vector2(0, height), true);
            try
            {
                if (showDetails)
                {
                    if (itemId > 0)
                    {
                        if (ImGui.SmallButton(localization.Get("GatheringBrowser.Stocks")+"###SourceStocks")) { page = Page.Items; focusStocksTab = true; }
                        ImGui.SameLine();
                        if (ImGui.SmallButton(localization.Get("GatheringBrowser.Recipes")+"###SourceRecipes")) OpenContextTool("Craftimizer");
                        ImGui.SameLine();
                        if (ImGui.SmallButton(localization.Get("GatheringBrowser.Prices")+"###SourcePrices")) OpenContextTool("Price Insight");
                    }
                    DrawGathering();
                }
            }
            finally { ImGui.EndChild(); }
        }
        finally { ImGui.EndTable(); }
    }

    private static bool SourceContains(string text, string query) => System.Globalization.CultureInfo.CurrentCulture.CompareInfo
        .IndexOf(text, query.Trim(), System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;
}
