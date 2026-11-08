using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// R-06 step 3 (0.272, user example « Carte Bugaal Ja »): selecting an item shows everything about it on the same page — header (icon with
// collection badge, name, category, levels, stock, last cached market price) and tabs: Overview (how to get it, usages summary), Stocks,
// then only the tabs with content (0.282, decided like the right-click menu; an unloaded catalogue keeps its tab): Craft (« Made by » /
// « Used in », 0.292), Usages, Gathering, and Market unless the item cannot be sold (0.281). The tabs reuse the existing views; since 0.280
// the Items page is only the search and this sheet (storages are on the Storage page). The tab set is recomputed only when the item or a
// catalogue changes.
internal sealed partial class MainWindow
{
    private string? itemSheetFocus;
    // Tabs of the sheet that have content, recomputed when the item or a catalogue changes (not every frame).
    private (uint Item,object? Recipes,object? Uses,object? Gathering,object? Lumina,object? Vendors) sheetTabsKey;
    private HashSet<string> sheetTabs=[];
    private bool SheetHas(uint id,string tab)
    {
        var key=(id,(object?)recipeCatalog.State.Catalog,(object?)itemUses.State.Catalog,(object?)gatheringCatalog.State.Catalog,(object?)lumina?.State.Data,(object?)npcVendors?.State.Catalog);
        if(key!=sheetTabsKey)
        {
            sheetTabsKey=key;
            var routes=RelevantRoutes(id);
            var recipes=recipeCatalog.State.Catalog;
            sheetTabs=[];
            // « Related crafting » (ItemsUi.Craft) shows both directions: made from a recipe, or an ingredient of one.
            if(recipes is null || recipes.ForResult(id).Count>0 || recipes.ForIngredient(id).Count>0)sheetTabs.Add("Craft");
            if(routes.Contains(Items.ItemContextRoute.Usages))sheetTabs.Add("Uses");
            if(routes.Contains(Items.ItemContextRoute.Gathering))sheetTabs.Add("Gathering");
        }
        return sheetTabs.Contains(tab);
    }

    // 0.273: every link to an item lands on the item's sheet, open on the requested tab (Overview by default); since 0.280 the
    // « Items » page has no other top tab.
    private void OpenItemSheet(string tab="Overview"){page=Page.Items;itemSheetFocus=tab;}

    private void DrawItemSheet()
    {
        if(itemId<=0)return;
        var id=(uint)itemId;var item=SelectedItem;
        ImGui.Separator();
        DrawItemIcon(item,new Vector2(48));
        ImGui.SameLine();
        ImGui.BeginGroup();
        try
        {
            ImGui.TextColored(Accent,item?.Name ?? localization.Format("BankUi.Item",id));
            DrawEnglishItemName(item);
            var facts=new List<string>();
            if(item is { UiCategoryName.Length:>0 })facts.Add(item.UiCategoryName);
            if(item is { ItemLevel:>1 })facts.Add(localization.Format("HoverUi.Levels",item.ItemLevel,item.EquipLevel));
            // 0.329: the per-item stock cache follows the live bags and the bank here too (only the Craft tab and a few panels
            // refreshed it: the header said « stock unknown » or kept an old count). Three reference comparisons.
            RefreshRecipeHoldingsCache();
            var stock=GetRecipeHoldings(id);
            facts.Add(stock.HasObservation ? localization.Format("ItemSheet.Stock",stock.Total.ToString("N0")):localization.Get("ItemSheet.StockUnknown"));
            ImGui.TextDisabled(string.Join(" · ",facts));
            if(market.GetCached(market.State.WorldId,id) is { } quote && (quote.Nq.Minimum ?? quote.Hq.Minimum) is { } lowest)
            {
                // 0.329: the date is when Tensho fetched the price (« Fetched », HoverUi.Fetched); an old or undated world upload is flagged as
                // on the Market page and the hover.
                ImGui.TextDisabled(localization.Format("ItemSheet.Price",lowest.ToString("N0"),quote.FetchedAt.ToLocalTime().ToString("g")));
                DrawGilIcon();
                if(!quote.HasRecentUpload(DateTimeOffset.UtcNow))ImGui.TextColored(Accent,localization.Get("HoverUi.Old"));
            }
            if(ShowIds)ImGui.TextDisabled($"ID {id}");
            // 0.349 (g, X-07): the item on the wiki and the Lodestone (browser, on click).
            foreach(var (label,open) in ExternalLinksFor(id))
            {
                if(ImGui.SmallButton(label+$"##External{label}"))open();
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ExternalLinks.Tip"));
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }
        finally{ImGui.EndGroup();}
        DetailTabs("ItemSheet",
        [
            ("Overview",localization.Get("ItemSheet.Overview"),()=>
            {
                ImGui.PushTextWrapPos(0);
                try{DrawItemInfoBody(item,id);}
                finally{ImGui.PopTextWrapPos();}
            }),
            ("Stocks",localization.Get("ItemsUi.Stocks"),()=>DrawSelectedItemHoldings(false)),
            // 0.347 (g, I-08): the player's note and labels on this item.
            ("Note",localization.Get("ItemNotes.Tab"),()=>DrawItemNoteTab(id)),
            // 0.282: only the tabs with something to show, decided like the right-click menu (RelevantRoutes); an unloaded
            // catalogue keeps its tab (unknown is not « none »).
            ..(SheetHas(id,"Craft") ? [("Craft",localization.Get("ItemsUi.Craft"),(Action)DrawCraftTab)]:Array.Empty<(string,string,Action)>()),
            ..(SheetHas(id,"Uses") ? [("Uses",localization.Get("ItemsUi.Uses"),(Action)DrawItemUses)]:Array.Empty<(string,string,Action)>()),
            ..(SheetHas(id,"Gathering") ? [("Gathering",localization.Get("ItemsUi.Gathering"),(Action)DrawGathering)]:Array.Empty<(string,string,Action)>()),
            // 0.281: no Market tab for an item that cannot be sold on the market board.
            ..(item is { IsMarketable:false } ? Array.Empty<(string,string,Action)>():[("Market",localization.Get("Navigation.Market"),DrawMarketListings)]),
        ],ref itemSheetFocus);
    }
}
