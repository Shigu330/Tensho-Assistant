using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Gathering page: resources searched by name and by zone or place, filtered by gathering type (job icons); the resource list on the left,
// the selected resource's details on the right (MainWindow.Gathering.cs), with the Eorzea clock, the fishing help button and links to
// stocks, recipes and prices. Game data only (GatheringCatalog); the match list is rebuilt only when the query, zone, filter or data change.
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
    // 0.402 (g, R-05): the character's notebooks (Gathering/NotebookReader), « hide what is in the notebook », and the export folder.
    internal Gathering.NotebookReader? NotebookSource { get; set; }
    internal Func<string>? NotebookExportFolder { get; set; }
    private bool sourceHideDone;
    private object? sourceViewNotebook;
    private bool sourceViewHideDone;
    private string? notebookExportMessage;
    private GatheringSource[] sourceTypes = [];

    // 0.588 (C7): recomputed once per Eorzea minute, and only while this section is open.
    // 0.732: worked out off the frame: the page sweep measured 41 ms for one frame of this page, every Eorzea minute (docs/evidence/0.724-page-sweep.md).
    private long gatheringNowKey=-1;private IReadOnlyList<(GatheringSource Source,GatheringOpening Opening)> gatheringNowRows=[];
    private Task<IReadOnlyList<(GatheringSource Source,GatheringOpening Opening)>>? gatheringNowTask;
    private void DrawGatheringNow(GatheringCatalog sources)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("GatheringBrowser.NowTitle")+"###GatheringNow"))return;
        var now=DateTimeOffset.UtcNow;var key=now.ToUnixTimeSeconds()*12/35;
        if(gatheringNowTask is { IsCompleted:true } done){gatheringNowTask=null;if(done.IsCompletedSuccessfully)gatheringNowRows=done.Result;}
        if(key!=gatheringNowKey && gatheringNowTask is null)
        {
            gatheringNowKey=key;
            var all=sources.Sources;
            gatheringNowTask=Task.Run(()=>GatheringNow.Rows(all,now,TimeSpan.FromHours(1),60));
        }
        if(gatheringNowRows.Count==0){ImGui.TextDisabled(localization.Get("GatheringBrowser.NowEmpty"));return;}
        if(!ImGui.BeginTable("GatheringNowRows",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            foreach(var (source,opening) in gatheringNowRows)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();
                ImGui.PushID($"{source.ItemId}/{source.PointId}");
                try
                {
                    var item=itemCatalog.State.Catalog?.Find(source.ItemId);
                    DrawItemIcon(item,new Vector2(22));ImGui.SameLine();
                    if(ImGui.Selectable(item?.Name ?? source.ItemId.ToString()))OpenGathering(source.ItemId);
                    ImGui.TableNextColumn();ImGui.TextDisabled($"{source.Zone} · {localization.Format("GatheringPanel.Level",source.Level)}");
                    ImGui.TableNextColumn();
                    var left=(opening.Current ? opening.EndsAt:opening.StartsAt)-now;
                    if(opening.Current)ImGui.TextColored(UI.FfxivTheme.Success,localization.Format("GatheringBrowser.NowOpen",(int)left.TotalMinutes,left.Seconds.ToString("00")));
                    else ImGui.TextUnformatted(localization.Format("GatheringBrowser.NowSoon",(int)left.TotalMinutes,left.Seconds.ToString("00")));
                    ImGui.TableNextColumn();DrawGatheringAlarmButton($"GatheringNowAlarm{source.ItemId}/{source.PointId}",source);
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }
    private void DrawGatheringBrowser()
    {
        Heading(localization.Get("GatheringBrowser.Title"),localization.Get("GatheringBrowser.Description"));
        DrawFishingPanelButton();
        DrawGatheringClock();
        if (gatheringCatalog.State.Catalog is not { } sources)
        {
            var state = gatheringCatalog.State;
            ImGui.TextWrapped(state.Error is null ? localization.Format("GatheringBrowser.Loading",state.Copied,state.Total):localization.Get("GatheringBrowser.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(RawMessage(error));
            return;
        }
        if (itemCatalog.State.Catalog is not { } items) { DrawCatalogStatus(); return; }
        if (!ReferenceEquals(sourceViewCatalog, sources))
            sourceTypes = sources.Sources.DistinctBy(source => source.TypeId).OrderBy(source => source.TypeId).ToArray();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##SourceSearch", localization.Get("GatheringBrowser.Search"), ref sourceQuery, 128);
        Tip("GatheringBrowser.SearchTip");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##SourceZoneSearch", localization.Get("GatheringBrowser.ZoneSearch"), ref sourceZoneQuery, 128);
        Tip("GatheringBrowser.ZoneSearchTip");
        if (ImGui.SmallButton(localization.Get("GatheringBrowser.AllJobs")+"###SourceAllJobs")) sourceTypeFilter = null;
        Tip("GatheringBrowser.AllJobsTip");
        foreach (var type in sourceTypes)
        {
            ImGui.SameLine();
            ImGui.PushID($"SourceType{type.TypeId}");
            try
            {
                DrawGameIcon(type.IconId,new Vector2(28));
                if (ImGui.IsItemClicked()) sourceTypeFilter = type.TypeId;
                if (ImGui.IsItemHovered()) WrappedTooltip(type.TypeName);
            }
            finally { ImGui.PopID(); }
        }
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("GatheringBrowser.Reset")+"###SourceReset")) { sourceQuery = ""; sourceZoneQuery = ""; sourceTypeFilter = null; }
        Tip("GatheringBrowser.ResetTip");
        DrawGatheringNow(sources);
        var notebook = DrawNotebookBar(items);
        if (!ReferenceEquals(sourceViewCatalog, sources) || !ReferenceEquals(sourceViewItems, items)
            || sourceViewQuery != sourceQuery || sourceViewZoneQuery != sourceZoneQuery || sourceViewTypeFilter != sourceTypeFilter
            || !ReferenceEquals(sourceViewNotebook, sourceHideDone ? notebook : null) || sourceViewHideDone != sourceHideDone)
        {
            sourceViewNotebook = sourceHideDone ? notebook : null; sourceViewHideDone = sourceHideDone;
            sourceMatches = sources.Sources
                .Where(source => (sourceTypeFilter is null || source.TypeId == sourceTypeFilter)
                    && (string.IsNullOrWhiteSpace(sourceZoneQuery) || SourceContains(source.Zone, sourceZoneQuery) || SourceContains(source.Place, sourceZoneQuery)))
                .Select(source => source.ItemId).Distinct().Select(items.Find).OfType<ItemDefinition>()
                .Where(item => string.IsNullOrWhiteSpace(sourceQuery) || SourceContains(item.Name, sourceQuery) || item.Id.ToString() == sourceQuery.Trim())
                .Where(item => !sourceHideDone || notebook?.Done(item.Id) != true)
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
                                if (ItemCell(item, item.Id, 28)) gatheringTypeFilter = sourceTypeFilter;
                                // 0.402 (g, R-05): already in the character's notebook.
                                if (notebook?.Done(item.Id) == true)
                                {
                                    ImGui.SameLine();
                                    ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                                    try { ImGui.TextColored(Success, Dalamud.Interface.FontAwesomeIcon.Check.ToIconString()); }
                                    finally { ImGui.PopFont(); }
                                    Tip("Notebooks.DoneTip");
                                }
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                    if (sourceMatches.Length == 0) EmptyState("GatheringBrowser.Empty");
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
