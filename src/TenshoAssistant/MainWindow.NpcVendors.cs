using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using TenshoAssistant.Npcs;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    public const int HoverNpcLines=2;
    private NpcVendorService? npcVendors;
    private string? npcFlagError;
    internal void ConfigureNpcVendors(NpcVendorService service)=>npcVendors=service;

    private void DrawNpcVendors()
    {
        Heading(localization.Get("NpcUi.Title"),localization.Get("NpcUi.Subtitle"));
        if(ImGui.CollapsingHeader(localization.Get("NpcUi.Choose")+"##Npc",itemId<=0 ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None))DrawItemSearch(160);
        if(npcVendors?.State is not { } state)return;
        if(state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("NpcUi.Loading",state.Copied.ToString("N0")):localization.Get("NpcUi.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        if(npcVendorFocus!=0 && catalog.Vendors.FirstOrDefault(vendor=>vendor.NpcId==npcVendorFocus) is { } focused){DrawNpcVendorSheet(catalog,focused);return;}
        if(itemId<=0){ImGui.TextWrapped(localization.Get("NpcUi.Help"));return;}
        var id=(uint)itemId;
        DrawItemIcon(SelectedItem,new Vector2(40));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(id);
        ItemCollectionMenu("SelectedItemCollection",SelectedItem);
        ImGui.SameLine();
        ImGui.TextColored(Accent,SelectedItem?.Name ?? localization.Format("MonsterUi.Item",id));
        DrawItemCollection(SelectedItem);
        var offers=catalog.ForItem(id);
        if(offers.Count==0){ImGui.TextWrapped(localization.Get("NpcUi.None"));return;}
        ImGui.TextDisabled(localization.Format("NpcUi.Count",offers.Count,offers.Select(offer=>offer.Vendor.NpcId).Distinct().Count()));
        ImGui.TextDisabled(localization.Get("NpcUi.FlagHelp"));
        if(npcFlagError is { } flagError)ImGui.TextColored(Accent,flagError);
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
                    DrawNpcFlagMenu(offer.Vendor);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(offer.Vendor.Locations.Count==0 ? localization.Get("NpcUi.NoLocation")
                        :string.Join(" · ",offer.Vendor.Locations.Take(2).Select(NpcPlace))+(offer.Vendor.Locations.Count>2 ? localization.Format("HoverUi.More",offer.Vendor.Locations.Count-2):""));
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
            if(!ImGui.CollapsingHeader($"{title} ({shop.Count()})##Shop{shop.Key}"))continue;
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

    // Right click on a vendor row: one entry per known position; the game map opens with its flag on that position.
    private void DrawNpcFlagMenu(NpcVendor vendor)
    {
        if(!ImGui.BeginPopupContextItem("NpcFlag"))return;
        try
        {
            ImGui.TextColored(Accent,NpcName(vendor));
            if(vendor.Locations.Count==0)ImGui.TextDisabled(localization.Get("NpcUi.NoLocation"));
            for(var index=0;index<vendor.Locations.Count;index++)
            {
                var location=vendor.Locations[index];
                if(!ImGui.Selectable(localization.Format("NpcUi.Flag",NpcPlace(location))+$"##Flag{index}"))continue;
                try
                {
                    npcFlagError=gameGui.OpenMapWithMapLink(new MapLinkPayload(location.TerritoryId,location.MapId,location.X,location.Y)) ? null:localization.Get("NpcUi.FlagError");
                }
                catch(Exception error){npcFlagError=localization.Get("NpcUi.FlagError");log.Warning(error,"Could not place the NPC map flag.");}
            }
        }
        finally{ImGui.EndPopup();}
    }

    // Generic NPC names are stored lowercase in the game data ("officier de la logistique"); only the first letter is raised.
    private string NpcName(NpcVendor vendor)=>vendor.Name.Length>0 ? char.ToUpper(vendor.Name[0],System.Globalization.CultureInfo.CurrentCulture)+vendor.Name[1..]
        :localization.Format("NpcUi.Unnamed",vendor.NpcId);
    private string NpcPlace(NpcLocation location)=>localization.Format("NpcUi.Place",location.Zone.Length>0 ? location.Zone:localization.Get("NpcUi.UnknownZone"),
        location.X.ToString("0.0"),location.Y.ToString("0.0"));
    // Amount followed by the currency's game icon; the name is only a tooltip (when the window accepts input) or a fallback text.
    private void DrawNpcCosts(NpcShopSale sale,float iconSize,bool tooltips)
    {
        if(sale.Costs.Count==0){ImGui.TextDisabled(localization.Get("NpcUi.UnknownCost"));return;}
        for(var index=0;index<sale.Costs.Count;index++)
        {
            var cost=sale.Costs[index];
            if(index>0){ImGui.SameLine(0,4);ImGui.TextUnformatted("+");ImGui.SameLine(0,4);}
            ImGui.TextUnformatted(cost.Amount.ToString("N0"));ImGui.SameLine(0,3);
            var currency=cost.CurrencyItemId==0 ? null:itemCatalog.State.Catalog?.Find(cost.CurrencyItemId);
            if(currency is null)ImGui.TextDisabled(cost.CurrencyItemId==0 ? localization.Format("NpcUi.EncodedCurrency",cost.RawCurrency):localization.Format("MonsterUi.Item",cost.CurrencyItemId));
            else
            {
                DrawItemIcon(currency,new Vector2(iconSize));
                if(tooltips && ImGui.IsItemHovered())ImGui.SetTooltip(currency.Name);
            }
        }
        if(sale.Quantity>1 || sale.Hq){ImGui.SameLine(0,6);ImGui.TextDisabled(localization.Format("NpcUi.Receives",sale.Quantity,sale.Hq ? " HQ":""));}
    }

    // Compact hover rows: first vendors with place and cost icons, then an explicit "+N" pointing at the tool.
    private int HoveredNpcCount(uint id)=>npcVendors?.State.Catalog?.ForItem(id).Count ?? 0;
    private void DrawHoveredNpcs(uint id)
    {
        if(npcVendors?.State.Catalog?.ForItem(id) is not { Count:>0 } offers)return;
        // Short cost first (amount + icon), then the wrapped name and place: wrapping text never pushes the icons off the panel.
        foreach(var offer in offers.Take(HoverNpcLines))
        {
            var place=offer.Vendor.Locations.Count>0 ? NpcPlace(offer.Vendor.Locations[0]):localization.Get("NpcUi.NoLocation");
            DrawNpcCosts(offer.Sale,ImGui.GetTextLineHeight(),false);ImGui.SameLine(0,6);
            ImGui.TextWrapped(localization.Format("HoverUi.Npc",NpcName(offer.Vendor),place));
        }
        if(offers.Count>HoverNpcLines)ImGui.TextDisabled(localization.Format("HoverUi.NpcMore",offers.Count-HoverNpcLines));
    }
}
