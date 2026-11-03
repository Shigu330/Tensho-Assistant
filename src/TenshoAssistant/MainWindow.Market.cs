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
        ImGui.TextColored(Accent, state.WorldId == 0 ? localization.Get("MarketUi.World") : state.WorldName);
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
        if (state.Loading) ImGui.TextDisabled(localization.Get("MarketUi.Loading"));
        if (state.Error is { } error) {ImGui.TextWrapped(localization.Get(SourceOff(error) ? "Advanced.SourceOffTip":"MarketDates.Error"));if(developerMode)ImGui.TextWrapped(RawMessage(error));}
        if (state.Quote is not { } quote) return;
        ImGui.TextDisabled(localization.Format("MarketDates.Fetched",quote.FetchedAt.ToLocalTime().ToString("g")));
        ImGui.TextDisabled(market.IsFresh(quote,DateTimeOffset.UtcNow) ? localization.Get("MarketUi.Fresh"):localization.Get("MarketUi.Expired"));
        ImGui.TextDisabled(quote.UploadedAt is { } date ? localization.Format("MarketDates.Uploaded",date.ToLocalTime().ToString("g")) : localization.Get("MarketUi.NoDate"));
        if (!quote.HasRecentUpload(DateTimeOffset.UtcNow))
            ImGui.TextColored(Accent, localization.Get("MarketUi.Old"));
        if (ImGui.BeginTable("MarketPrices", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
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
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(value.Item2.Minimum?.ToString("N0") ?? localization.Get("MarketUi.Missing"));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(value.Item2.RecentSale?.ToString("N0") ?? localization.Get("MarketUi.Missing"));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(value.Item2.SaleAt?.ToLocalTime().ToString("g") ?? localization.Get("MarketUi.Unknown"));
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.TextWrapped(localization.Get("MarketUi.Help"));
    }
}
