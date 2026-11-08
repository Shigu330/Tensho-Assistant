using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using TenshoAssistant.Npcs;

namespace TenshoAssistant;

// NPC vendors page (World): the NPCs selling the selected item, from the game's shop sheets (Npcs/, cached): shop, cost with the
// currency icons (company credits with the needed company rank, 0.295), place, and a click to flag it on the map. Also the vendor
// sheet opened from the Tensho map (one NPC's places and stock) and the short vendor rows of the item hover panel.
internal sealed partial class MainWindow
{
    private NpcVendorService? npcVendors;
    internal void ConfigureNpcVendors(NpcVendorService service)=>npcVendors=service;

    private void DrawNpcVendors()
    {
        Heading(localization.Get("NpcUi.Title"),localization.Get("NpcUi.Subtitle"));
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("NpcUi.Choose")+"##Npc",itemId<=0 ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None))DrawItemSearch(160);
        if(npcVendors?.State is not { } state)return;
        if(state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("NpcUi.Loading",state.Copied.ToString("N0")):localization.Get("NpcUi.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(RawMessage(error));
            return;
        }
        if(npcVendorFocus!=0 && catalog.Vendors.FirstOrDefault(vendor=>vendor.NpcId==npcVendorFocus) is { } focused){DrawNpcVendorSheet(catalog,focused);return;}
        if(itemId<=0){ImGui.TextWrapped(localization.Get("NpcUi.Help"));return;}
        var id=(uint)itemId;
        DrawItemIcon(SelectedItem,new Vector2(40));
        ItemCollectionMenu("SelectedItemCollection",SelectedItem);
        ImGui.SameLine();
        ImGui.TextColored(Accent,SelectedItem?.Name ?? localization.Format("MonsterUi.Item",id));
        DrawItemCollection(SelectedItem);
        var offers=catalog.ForItem(id);
        if(offers.Count==0){ImGui.TextWrapped(localization.Get("NpcUi.None"));return;}
        ImGui.TextDisabled(localization.Format("NpcUi.Count",offers.Count,offers.Select(offer=>offer.Vendor.NpcId).Distinct().Count()));
        ImGui.TextDisabled(localization.Get("NpcUi.FlagHelp"));
        if(mapOpenError is { } flagError)ImGui.TextColored(Accent,flagError);
        if(!ImGui.BeginTable("NpcOffers",4,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,new Vector2(0,Math.Clamp(offers.Count*26+30,90,420))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn(localization.Get("NpcUi.Npc"),ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableSetupColumn(localization.Get("NpcUi.Location"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("NpcUi.Cost"),ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableSetupColumn(localization.Get("NpcUi.Shop"),ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableHeadersRow();
            for(var index=0;index<offers.Count;index++)
            {
                var offer=offers[index];
                ImGui.PushID(index);
                try
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.Selectable(NpcName(offer.Vendor)+"##Offer",false,ImGuiSelectableFlags.SpanAllColumns);
                    var places=VendorLocations(offer.Vendor);
                    NpcMenu("NpcFlag",NpcName(offer.Vendor),offer.Vendor.NpcId,places);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(places.Count==0 ? localization.Get("NpcUi.NoLocation")
                        :string.Join(" · ",places.Take(2).Select(NpcPlace))+(places.Count>2 ? localization.Format("HoverUi.More",places.Count-2):""));
                    ImGui.TableNextColumn();DrawNpcCosts(offer.Sale,18,true);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(offer.Sale.ShopName.Length>0 ? offer.Sale.ShopName:localization.Get("NpcUi.Kind"+offer.Sale.Kind));
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Get("NpcUi.Source"));
    }

    // Vendor sheet (0.140, opened from the map's right-click « Vendors » tab): positions with a Tensho map link, then each shop's
    // stock with item icon, name and price; a click on an item opens the item view of this tool.
    private uint npcVendorFocus;
    private void DrawNpcVendorSheet(NpcVendorCatalog catalog,NpcVendor vendor)
    {
        if(ImGui.SmallButton(localization.Get("NpcUi.BackToItem")))npcVendorFocus=0;
        ImGui.TextColored(Accent,NpcName(vendor));
        foreach(var location in vendor.Locations)
        {
            ImGui.TextUnformatted(NpcPlace(location));
            if(maps.State.Catalog?.Find(location.MapId) is { } map)
            {
                PlaceMenu($"VendorPlace{location.MapId}/{location.X}/{location.Y}",map,new(location.X,location.Y),NpcName(vendor));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("MapView.ShowOnMap")+$"##VendorMap{location.MapId}/{location.X}/{location.Y}"))ShowOnTenshoMap(map,new Vector3(location.WorldX,0,location.WorldZ));
            }
        }
        var sales=catalog.Sales.Where(sale=>vendor.ShopIds.Contains(sale.ShopId)).GroupBy(sale=>sale.ShopId).ToArray();
        if(sales.Length==0){ImGui.TextDisabled(localization.Get("MapLocal.ShopsNone"));return;}
        var items=itemCatalog.State.Catalog;
        foreach(var shop in sales)
        {
            var first=shop.First();
            var title=first.ShopName.Length>0 && !(localization.CurrentCode!="ja" && first.ShopName.Any(c=>c is >= '\u3040' and <= '\u30FF' or >= '\u4E00' and <= '\u9FFF'))
                ? first.ShopName:localization.Get("MapLocal.ShopKind"+first.Kind);
            if(!TenshoAssistant.UI.UiComponents.Header($"{title} ({shop.Count()})##Shop{shop.Key}"))continue;
            if(!ImGui.BeginTable($"VendorShop{shop.Key}",2,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))continue;
            try
            {
                foreach(var sale in shop)
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    var item=items?.Find(sale.ItemId);
                    DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                    if(ImGui.Selectable($"{item?.Name ?? localization.Format("MonsterUi.Item",sale.ItemId)}##Sale{shop.Key}/{sale.ItemId}/{sale.Quantity}")){itemId=(int)sale.ItemId;npcVendorFocus=0;}
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(sale.ItemId);
                    ItemCollectionMenu($"SaleCollection{shop.Key}/{sale.ItemId}",item);
                    ImGui.TableNextColumn();DrawNpcCosts(sale,16,true);
                }
            }
            finally{ImGui.EndTable();}
        }
    }

    // Generic NPC names are stored lowercase in the game data ("officier de la logistique"); only the first letter is raised.
    private string NpcName(NpcVendor vendor)=>vendor.Name.Length>0 ? char.ToUpper(vendor.Name[0],System.Globalization.CultureInfo.CurrentCulture)+vendor.Name[1..]
        :localization.Format("NpcUi.Unnamed",vendor.NpcId);
    private string NpcPlace(NpcLocation location)=>localization.Format("NpcUi.Place",location.Zone.Length>0 ? location.Zone:localization.Get("NpcUi.UnknownZone"),
        Maps.MapProjection.CoordinateNumber(location.X),Maps.MapProjection.CoordinateNumber(location.Y));
    // Amount followed by the currency's game icon; the name is only a tooltip (when the window accepts input) or a fallback text.
    private void DrawNpcCosts(NpcShopSale sale,float iconSize,bool tooltips)
    {
        DrawNpcCostLine(sale,iconSize,tooltips);
        // 0.352 (g, I-11): what unlocks the article, with done / to do from the quest journal and the achievements read.
        if(sale.QuestRequired!=0 || sale.AchievementRequired!=0){ImGui.SameLine(0,6);DrawShopRequirement(sale);}
    }

    private void DrawNpcCostLine(NpcShopSale sale,float iconSize,bool tooltips)
    {
        if(sale.Costs.Count==0){ImGui.TextDisabled(localization.Get("NpcUi.UnknownCost"));return;}
        for(var index=0;index<sale.Costs.Count;index++)
        {
            var cost=sale.Costs[index];
            if(index>0){ImGui.SameLine(0,4);ImGui.TextUnformatted("+");ImGui.SameLine(0,4);}
            ImGui.TextUnformatted(cost.Amount.ToString("N0"));ImGui.SameLine(0,3);
            // 0.295: company credits (FccShop) are named, with the company rank the item needs.
            if(cost.CurrencyItemId==NpcCost.CompanyCredits)
            {
                ImGui.TextUnformatted(localization.Get("NpcUi.CompanyCredits"));
                if(sale.RequiredRank>0){ImGui.SameLine(0,4);ImGui.TextDisabled(localization.Format("NpcUi.CompanyRank",sale.RequiredRank));}
                continue;
            }
            var currency=cost.CurrencyItemId==0 ? null:itemCatalog.State.Catalog?.Find(cost.CurrencyItemId);
            if(currency is null)ImGui.TextDisabled(cost.CurrencyItemId==0 ? localization.Format("NpcUi.EncodedCurrency",cost.RawCurrency):localization.Format("MonsterUi.Item",cost.CurrencyItemId));
            else
            {
                DrawItemIcon(currency,new Vector2(iconSize),tooltips);
            }
        }
        if(sale.Quantity>1 || sale.Hq){ImGui.SameLine(0,6);ImGui.TextDisabled(localization.Format("NpcUi.Receives",sale.Quantity,sale.Hq ? " HQ":""));}
    }
}
