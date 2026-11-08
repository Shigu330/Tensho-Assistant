using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// I-05 (issue #26): beside the game's retainer sale list, your prices against the other sellers. Read-only: the only write is to the clipboard, on a click.
// Universalis is asked on the « Compare » click only: whether to ask when the list opens is a decision left to the player (docs/project/G-PROPOSALS.md).
internal sealed partial class MainWindow
{
    internal Func<RetainerSalesState>? RetainerSalesSource { get; set; }
    internal RetainerCompareService? RetainerCompare { get; set; }

    internal void DrawRetainerSalesPanel()
    {
        if (windowDisposed || !configuration.RetainerSalesPanel || RetainerSalesSource?.Invoke() is not { } state || state.Status == RetainerSalesStatus.Closed) return;
        var addon = gameGui.GetAddonByName("RetainerSellList");
        if (addon.IsNull || !addon.IsVisible) return;
        var viewport = ImGui.GetMainViewport();
        var width = Math.Min(620, viewport.WorkSize.X);
        var position = viewport.Pos + addon.Position + new Vector2(addon.ScaledWidth + 6, 0);
        if (position.X + width > viewport.WorkPos.X + viewport.WorkSize.X) position.X = viewport.Pos.X + addon.Position.X - width - 6;
        position = Vector2.Clamp(position, viewport.WorkPos, Vector2.Max(viewport.WorkPos, viewport.WorkPos + viewport.WorkSize - new Vector2(width, 200)));
        Panels?.Show("TenshoRetainerSales", localization.Get("RetainerSales.Title"), ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse, () => { ImGui.SetNextWindowPos(position, ImGuiCond.Always); ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(width, Math.Max(240, viewport.WorkSize.Y - 40))); }, () => DrawRetainerSalesContent(state));
    }

    // X-73.14 (0.870): by item, the other sellers seen at the game's board on the home world (SeenPrices.RetainerOffers); worked out again when the prices seen,
    // Universalis' answer or the minute change, never each frame.
    private (long Revision, object? Universalis, long Minute, object? Listings, Dictionary<uint, RetainerItemOffers> Offers) retainerSeen = (-1, null, 0, null, []);
    private Dictionary<uint, RetainerItemOffers> RetainerSeenOffers(RetainerSalesState state, IReadOnlyDictionary<uint, RetainerItemOffers>? universalis)
    {
        if (SeenPrices is not { } store || !configuration.Discovery.Enabled || !configuration.Discovery.MarketPrices) return [];
        var now = DateTimeOffset.UtcNow; var minute = now.ToUnixTimeSeconds() / 60; var revision = store.Revision;
        if (retainerSeen.Revision == revision && ReferenceEquals(retainerSeen.Universalis, universalis) && retainerSeen.Minute == minute && ReferenceEquals(retainerSeen.Listings, state))
            return retainerSeen.Offers;
        var prices = market.State;
        var world = prices.WorldName == state.HomeWorld ? prices.WorldId : prices.WorldNames?.FirstOrDefault(pair => pair.Value == state.HomeWorld).Key ?? 0;
        var offers = new Dictionary<uint, RetainerItemOffers>();
        if (world != 0)
            foreach (var item in state.Listings.Select(listing => listing.ItemId).Distinct())
                if (TenshoAssistant.Market.SeenPrices.RetainerOffers(store.Get(world, item), state.HomeWorld, universalis?.GetValueOrDefault(item), now) is { } seen) offers[item] = seen;
        retainerSeen = (revision, universalis, minute, state, offers);
        return offers;
    }

    private void DrawRetainerSalesContent(RetainerSalesState state)
    {
        if (state.Status == RetainerSalesStatus.Unavailable) { ImGui.TextWrapped(localization.Get("RetainerSales.Unavailable")); return; }
        if (state.Listings.Count == 0) { ImGui.TextDisabled(localization.Get("RetainerSales.Empty")); return; }
        var compare = RetainerCompare?.State;
        var fresh = compare is { Items: not null } && compare.World == state.HomeWorld ? compare.Items : null;
        var seen = RetainerSeenOffers(state, fresh);
        if (!state.AtHome) ImGui.TextWrapped(localization.Get("RetainerSales.NotHome"));
        ImGui.BeginDisabled(RetainerCompare is null || compare is { Loading: true } || !state.AtHome || !configuration.CommunityUniversalis);
        try
        {
            if (ImGui.SmallButton(localization.Get("RetainerSales.Compare")))
                RetainerCompare?.Request(state.HomeWorld, state.Listings.Select(listing => listing.ItemId), DateTimeOffset.UtcNow);
        }
        finally { ImGui.EndDisabled(); }
        Tip(configuration.CommunityUniversalis ? "RetainerSales.CompareTip" : "RetainerSales.SourceOffTip");
        ImGui.SameLine();
        ImGui.TextDisabled(compare is { Loading: true } ? localization.Get("RetainerSales.Loading")
            : compare?.Error is { } error ? localization.Get("RetainerSales.Error" + error)
            : compare?.FetchedAt is { } at ? localization.Format("RetainerSales.FetchedAt", at.ToLocalTime().ToString("HH:mm"))
            : localization.Get("RetainerSales.NotAsked"));
        if (!ImGui.BeginTable("RetainerSalesTable", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            foreach (var title in new[] { "RetainerSales.Item", "RetainerSales.Quantity", "RetainerSales.Yours", "RetainerSales.Lowest", "RetainerSales.Verdict", "RetainerSales.Copy" })
                ImGui.TableSetupColumn(localization.Get(title));
            ImGui.TableHeadersRow();
            var catalog = itemCatalog.State.Catalog;
            foreach (var listing in state.Listings)
            {
                // X-73.14 (0.870): the offers seen at the game's board on your home world when newer than Universalis'.
                var offers = seen.GetValueOrDefault(listing.ItemId) ?? fresh?.GetValueOrDefault(listing.ItemId);
                var verdict = RetainerSales.Compare(listing, offers, state.OwnNames);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted((catalog?.Find(listing.ItemId)?.Name ?? $"#{listing.ItemId}") + (listing.Hq ? " HQ" : ""));
                if (ImGui.IsItemHovered()) DrawItemInfoTooltip(listing.ItemId);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(listing.Quantity.ToString());
                ImGui.TableNextColumn(); ImGui.TextUnformatted(listing.UnitPrice?.ToString("N0") ?? "?");
                ImGui.TableNextColumn(); ImGui.TextUnformatted(verdict.Lowest?.ToString("N0") ?? "—");
                if (verdict.Competitor is { Length: > 0 } seller && ImGui.IsItemHovered()) WrappedTooltip(localization.Format("RetainerSales.Seller", seller, offers?.DataAt?.ToLocalTime().ToString("g") ?? "?"));
                else if (verdict.Lowest is not null && offers?.Offers.FirstOrDefault()?.Seen == true && ImGui.IsItemHovered()) WrappedTooltip(localization.Format("RetainerSales.SeenAt", offers.DataAt?.ToLocalTime().ToString("g") ?? "?"));
                ImGui.TableNextColumn();
                var (key, color) = verdict.Kind switch
                {
                    RetainerVerdictKind.Undercut => ("RetainerSales.Undercut", UI.FfxivTheme.Warning),
                    RetainerVerdictKind.Tied => ("RetainerSales.Tied", Accent),
                    RetainerVerdictKind.Lowest => ("RetainerSales.IsLowest", Success),
                    RetainerVerdictKind.Alone => ("RetainerSales.Alone", Success),
                    RetainerVerdictKind.PriceUnknown => ("RetainerSales.PriceUnknown", Muted),
                    _ => ("RetainerSales.Unknown", Muted),
                };
                ImGui.TextColored(color, verdict.Kind == RetainerVerdictKind.Undercut ? localization.Format(key, verdict.Gap?.ToString("N0") ?? "?") : localization.Get(key));
                ImGui.TableNextColumn();
                if (verdict.Paste is { } paste)
                {
                    if (ImGui.SmallButton($"{paste:N0}##RetainerPaste{listing.Row}")) ImGui.SetClipboardText(paste.ToString());
                    Tip("RetainerSales.CopyTip");
                }
            }
        }
        finally { ImGui.EndTable(); }
        ImGui.TextDisabled(localization.Get("RetainerSales.Footer"));
    }
}
