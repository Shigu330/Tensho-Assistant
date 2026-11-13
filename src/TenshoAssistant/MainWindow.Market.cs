using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// Market prices page (Objects) for the selected item, from the Universalis API, on demand only. Summary tab: lowest NQ / HQ price
// and last sale on the current world (Market/MarketService.cs, kept for the « Prices » cache duration); the Listings and History
// tabs are in MainWindow.MarketBoard.cs. Prices are community uploads: their date is shown and old data is flagged.
internal sealed partial class MainWindow
{
    private void DrawMarket()
    {
        Heading(localization.Get("MarketUi.Title"), localization.Get("MarketUi.Description"));
        var state = market.State;
        UI.UiComponents.SubTitle(state.WorldId == 0 ? localization.Get("MarketUi.World") : state.WorldName);
        if (itemId <= 0) { ImGui.TextWrapped(localization.Get("MarketUi.Select")); return; }
        DrawItemIcon(SelectedItem, new Vector2(40));
        ImGui.SameLine(); ImGui.TextUnformatted(SelectedItem?.Name ?? $"ID {itemId}");
        // 0.270: summary (lowest price and last sale on the world), every listing with filters, recent sales; only the open tab works.
        if (!ImGui.BeginTabBar("MarketTabs")) return;
        try
        {
            var summaryFlags = marketFocusSummary ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None; marketFocusSummary = false;
            if (ImGui.BeginTabItem(localization.Get("MarketBoard.TabSummary"), summaryFlags)) { try { DrawMarketSummary(state); } finally { ImGui.EndTabItem(); } }
            if (ImGui.BeginTabItem(localization.Get("MarketBoard.TabListings"))) { try { DrawMarketListings(); } finally { ImGui.EndTabItem(); } }
            if (ImGui.BeginTabItem(localization.Get("MarketBoard.TabHistory"))) { try { DrawMarketHistory(); } finally { ImGui.EndTabItem(); } }
        }
        finally { ImGui.EndTabBar(); }
    }

    private void DrawMarketSummary(TenshoAssistant.Market.MarketState state)
    {
        ImGui.BeginDisabled(state.WorldId == 0 || state.Loading);
        try
        {
            if (ImGui.Button(localization.Get("MarketUi.Fetch"))) _ = market.RequestAsync((uint)itemId);
            ImGui.SameLine();
            if (SourceButton(localization.Get("MarketUi.Refresh"),configuration.CommunityUniversalis)) _ = market.RequestAsync((uint)itemId, true);
        }
        finally { ImGui.EndDisabled(); }
        ImGui.TextWrapped(localization.Format("MarketDates.Cache",localization.Get("CacheDuration."+CacheChoiceKeys[(int)configuration.Cache.Get(TenshoAssistant.Caching.CacheArea.Prices)])));
        if (state.ItemId != (uint)itemId) return;
        if (state.Loading) StateLine(UI.UiState.Waiting,localization.Get("MarketUi.Loading"));
        if (state.Error is { } error) DrawDataError(error,"MarketDates.Error");
        if (state.Quote is not { } quote) return;
        ImGui.TextDisabled(localization.Format("MarketDates.Fetched",quote.FetchedAt.ToLocalTime().ToString("g")));
        // X-156 (0.1264): a cache past its duration and an old upload are both the « old data » state, said as everywhere.
        if (market.IsFresh(quote,DateTimeOffset.UtcNow)) ImGui.TextDisabled(localization.Get("MarketUi.Fresh"));
        else StateLine(UI.UiState.Stale,localization.Get("MarketUi.Expired"));
        ImGui.TextDisabled(quote.UploadedAt is { } date ? localization.Format("MarketDates.Uploaded",date.ToLocalTime().ToString("g")) : localization.Get("MarketUi.NoDate"));
        if (!quote.HasRecentUpload(DateTimeOffset.UtcNow))
            StateLine(UI.UiState.Stale,localization.Get("MarketUi.Old"));
        if (UI.UiComponents.BeginDataTable("MarketPrices", 4))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("MarketUi.Quality"));
                ImGui.TableSetupColumn(localization.Get("MarketUi.Minimum"));
                ImGui.TableSetupColumn(localization.Get("MarketUi.Last"));
                ImGui.TableSetupColumn(localization.Get("MarketUi.SaleDate"));
                ImGui.TableHeadersRow();
                foreach (var value in new[] { ("NQ", quote.Nq), ("HQ", quote.Hq) })
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(value.Item1);
                    ImGui.TableNextColumn(); UI.UiComponents.NumberCell(value.Item2.Minimum?.ToString("N0",Numbers) ?? localization.Get("MarketUi.Missing"));
                    ImGui.TableNextColumn(); UI.UiComponents.NumberCell(value.Item2.RecentSale?.ToString("N0",Numbers) ?? localization.Get("MarketUi.Missing"));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(value.Item2.SaleAt?.ToLocalTime().ToString("g") ?? localization.Get("MarketUi.Unknown"));
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.TextWrapped(localization.Get("MarketUi.Help"));
    }
}
