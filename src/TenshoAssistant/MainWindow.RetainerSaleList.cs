using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// X-695 (0.1667, the user's request of 20/11 around 23:00): a retainer's 20 most profitable items to sell, by a button of the retainer sale
// panel (MainWindow.RetainerSales.cs). Display only: nothing is put up for sale; Universalis is asked on the player's click only (D-82).
internal sealed partial class MainWindow
{
    // X-695 (0.1667): the sale list shown or not, the retainer chosen (null: the one recorded last, the one open), the list worked out again
    // only when the inventory, the prices seen, Universalis' answer, the tax rate or the minute change, never each frame.
    private bool retainerSaleListOpen;
    private BankKey? retainerSaleListChosen;
    private (InventorySnapshot? Snapshot, long Revision, object? Universalis, long Minute, int Tax, SaleList List) retainerSaleListSeen = (null, -1, null, 0, -1, new([], 0, []));
    /// <summary>X-698 M1 (0.1671): the sale list's own Universalis requests (it accumulates its answers), apart from the sale panel's.</summary>
    internal RetainerCompareService? RetainerSaleListCompare { get; set; }
    /// <summary>X-698 M4 (0.1671): the market tax rates the game sent (MarketTaxReader), null before.</summary>
    internal Func<MarketTaxRates?>? MarketTaxRatesSource { get; set; }

    private void DrawRetainerSaleListButton(RetainerSalesState state)
    {
        if (ImGui.SmallButton(localization.Get(retainerSaleListOpen ? "RetainerSales.SaleListHide" : "RetainerSales.SaleList"))) retainerSaleListOpen = !retainerSaleListOpen;
        Tip("RetainerSales.SaleListTip");
        if (!retainerSaleListOpen) return;
        var character = bankBridge.State.CharacterId;
        var retainers = bank.State.Entries.Where(entry => entry.Key.CharacterId == character && entry.Key.Kind == BankStorageKind.Retainer)
            .OrderByDescending(entry => entry.Snapshot.ObservedAt).ToArray();
        if (retainers.Length == 0) { EmptyState("RetainerSales.SaleListNone"); ImGui.Separator(); return; }
        var shown = retainers.FirstOrDefault(entry => entry.Key == retainerSaleListChosen) ?? retainers[0];
        ImGui.SetNextItemWidth(Widths.Medium);
        if (ImGui.BeginCombo(localization.Get("RetainerSales.SaleListRetainer") + "###RetainerSaleListRetainer", BankEntryName(shown)))
        {
            try
            {
                foreach (var entry in retainers)
                    if (ImGui.Selectable(BankEntryName(entry) + "###SaleListRetainer" + entry.Key.StorageId, entry.Key == shown.Key)) retainerSaleListChosen = entry.Key;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("RetainerSales.SaleListRetainerTip");
        var compare = RetainerSaleListCompare?.State;
        var universalis = compare is { Items: not null } && compare.World == state.HomeWorld ? compare.Items : null;
        // X-698 M4 (0.1671), X-700 M1 (0.1686): the tax of the retainer's city when the game sent the rates, for any retainer whose town was read.
        var (tax, taxFrom) = MarketTax.ForRetainer(MarketTaxRatesSource?.Invoke(), state.Towns, shown.Key.StorageId, DateTimeOffset.UtcNow);
        var list = RetainerSaleListOf(shown, state, universalis, tax);
        ImGui.SameLine();
        ImGui.BeginDisabled(RetainerSaleListCompare is null || compare is { Loading: true } || !state.AtHome || !SourceOn(SourceUse.UniversalisRetainerCompare) || list.ToAsk.Count == 0);
        try
        {
            if (ImGui.SmallButton(localization.Get("RetainerSales.SaleListPrices")))
                RetainerSaleListCompare?.Request(state.HomeWorld, list.ToAsk, DateTimeOffset.UtcNow, RetainerSaleList.Asked);
        }
        finally { ImGui.EndDisabled(); }
        Tip(SourceOn(SourceUse.UniversalisRetainerCompare) ? "RetainerSales.SaleListPricesTip" : "RetainerSales.SourceOffTip");
        ImGui.SameLine();
        ImGui.TextDisabled(compare is { Loading: true } ? localization.Get("RetainerSales.Loading")
            : compare?.Error is { } error ? localization.Get("RetainerSales.Error" + error)
            : compare?.FetchedAt is { } at ? localization.Format("RetainerSales.FetchedAt", at.ToLocalTime().ToString("HH:mm"))
            : localization.Get("RetainerSales.NotAsked"));
        ImGui.TextDisabled(localization.Format("RetainerSales.SaleListRecorded", shown.Snapshot.ObservedAt.ToLocalTime().ToString("g")));
        if (list.Rows.Count == 0) EmptyState("RetainerSales.SaleListEmpty");
        else if (UI.UiComponents.BeginDataTable("RetainerSaleListTable", 7))
        {
            try
            {
                var titles = new[] { "RetainerSales.Item", "RetainerSales.Quantity", "RetainerSales.SaleListUnit", "RetainerSales.SaleListTotal", "RetainerSales.SaleListNpc", "RetainerSales.SaleListSource", "RetainerSales.Copy" }
                    .Select(title => localization.Get(title)).ToArray();
                // X-719 (0.1726): the « Copy » column keeps its button's width (never squeezed out of the window).
                for (var column = 0; column < titles.Length; column++) ImGui.TableSetupColumn(titles[column], column == titles.Length - 1 ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.None);
                // The amounts' titles on the right, above them.
                UI.UiComponents.HeadersRow(titles, column => column is 1 or 2 or 3 or 4);
                var catalog = itemCatalog.State.Catalog;
                foreach (var row in list.Rows)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted((catalog?.Find(row.Item)?.Name ?? $"#{row.Item}") + (row.Hq ? " HQ" : ""));
                    ItemTip(row.Item, hq: row.Hq);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(row.Quantity.ToString("N0", Numbers));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(row.Price?.Unit.ToString("N0", Numbers) ?? localization.Get("RetainerSales.SaleListUnknownPrice"));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(row.Total?.ToString("N0", Numbers) ?? localization.Get("RetainerSales.SaleListUnknownPrice"));
                    ImGui.TableNextColumn();
                    if (row.Npc is { } npc) ImGui.TextColored(row.NpcBetter ? UI.FfxivTheme.Warning : Muted, npc.ToString("N0", Numbers)); else ImGui.TextDisabled("—");
                    if (row.NpcBetter) TipText(localization.Get("RetainerSales.SaleListNpcBetter"));
                    ImGui.TableNextColumn();
                    // X-719 (0.1726): short (the time today, else the date), the whole time on hover.
                    if (row.Price is { } price)
                    {
                        var key = price.Source == SaleSource.Board ? "RetainerSales.SaleListBoard" : "RetainerSales.SaleListUniversalis";
                        var seen = price.At?.ToLocalTime();
                        ImGui.TextDisabled(localization.Format(key, seen is { } local ? RetainerSaleList.Short(local, DateTimeOffset.Now) : "?"));
                        if (seen is { } whole) if(TipHovered())TipText(localization.Format(key, whole.ToString("g")));
                    }
                    else ImGui.TextDisabled("—");
                    ImGui.TableNextColumn();
                    if (row.Price is { } unit)
                    {
                        // X-719 (0.1726): the lowest price seen minus 1 gil, never under 1.
                        var paste = RetainerSaleList.Paste(unit.Unit);
                        CopyButton($"{paste:N0}##RetainerSaleListCopy{row.Item}{(row.Hq ? "h" : "n")}", () => paste.ToString());
                        Tip("RetainerSales.SaleListCopyTip");
                    }
                }
            }
            finally { ImGui.EndTable(); }
        }
        if (list.Unknown > 0) ImGui.TextDisabled(localization.Format("RetainerSales.SaleListUnknown", list.Unknown));
        ImGui.TextDisabled(localization.Format("RetainerSales.SaleListFooter", tax, localization.Get(taxFrom switch { MarketTax.Source.City => "RetainerSales.SaleListTaxCity", MarketTax.Source.NotReceived => "RetainerSales.SaleListTaxHelp", _ => "RetainerSales.SaleListTaxTown" })));
        ImGui.Separator();
    }

    private SaleList RetainerSaleListOf(BankEntry entry, RetainerSalesState state, IReadOnlyDictionary<uint, RetainerItemOffers>? universalis, int tax)
    {
        var store = SeenPrices is { } seen && configuration.Discovery.Enabled && configuration.Discovery.MarketPrices ? seen : null;
        var now = DateTimeOffset.UtcNow; var minute = now.ToUnixTimeSeconds() / 60; var revision = store?.Revision ?? -1;
        if (ReferenceEquals(retainerSaleListSeen.Snapshot, entry.Snapshot) && retainerSaleListSeen.Revision == revision && ReferenceEquals(retainerSaleListSeen.Universalis, universalis)
            && retainerSaleListSeen.Minute == minute && retainerSaleListSeen.Tax == tax)
            return retainerSaleListSeen.List;
        var prices = market.State;
        var world = prices.WorldName == state.HomeWorld ? prices.WorldId : prices.WorldNames?.FirstOrDefault(pair => pair.Value == state.HomeWorld).Key ?? 0;
        var catalog = itemCatalog.State.Catalog;
        var stacks = entry.Snapshot.Containers.Values.Where(container => RetainerSaleList.IsRetainerPage(container.Location.Container.Value))
            .SelectMany(container => container.Stacks).Select(stack => (stack.Item.Id.Value, stack.Item.IsHighQuality, stack.Quantity));
        SalePrice? Price(uint item, bool hq)
        {
            SalePrice? board = null, fetched = null;
            if (store is not null && world != 0 && store.Get(world, item) is { } seenEntry && seenEntry.Listings.Where(listing => listing.Hq == hq && listing.Price > 0).Select(listing => listing.Price).DefaultIfEmpty(0).Min() is var low && low > 0)
                board = new SalePrice((long)low, SaleSource.Board, seenEntry.ListingsAt);
            if (universalis?.GetValueOrDefault(item) is { } offers && offers.Offers.Where(offer => offer.Hq == hq && offer.UnitPrice > 0).Select(offer => offer.UnitPrice).DefaultIfEmpty(0).Min() is var cheapest && cheapest > 0)
                fetched = new SalePrice(cheapest, SaleSource.Universalis, offers.DataAt);
            return RetainerSaleList.Newest(board, fetched);
        }
        var list = RetainerSaleList.Build(stacks, item => catalog?.Find(item), Price, tax, item => universalis?.ContainsKey(item) == true);
        retainerSaleListSeen = (entry.Snapshot, revision, universalis, minute, tax, list);
        return list;
    }
}
