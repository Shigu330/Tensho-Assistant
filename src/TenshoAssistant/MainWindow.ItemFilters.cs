using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, I-12 (0.353): filters of the item search (category, equip level, item level, slot, tradable, sold on the market) and a search of
// the key items (EventItem: quest items, no item sheet). Each change of a filter searches again; key items searched on the worker-free
// cached list from the game data (built once by Plugin).
internal sealed partial class MainWindow
{
    internal Func<string,IReadOnlyList<(uint Id,string Name,uint Icon)>>? KeyItemSearch { get; set; }
    private ItemSearchFilter itemSearchFilter=ItemSearchFilter.None;
    private string keyItemQuery="";
    private IReadOnlyList<(uint Id,string Name,uint Icon)> keyItemResults=[];
    private static readonly EquipmentSlots[] FilterSlots=[EquipmentSlots.MainHand,EquipmentSlots.OffHand,EquipmentSlots.Head,EquipmentSlots.Body,EquipmentSlots.Hands,
        EquipmentSlots.Legs,EquipmentSlots.Feet,EquipmentSlots.Ears,EquipmentSlots.Neck,EquipmentSlots.Wrists,EquipmentSlots.Ring];

    /// <summary>Draws the filters (folded); true when one changed.</summary>
    private bool DrawItemSearchFilters(ItemCatalog catalog)
    {
        var title=itemSearchFilter.IsEmpty ? localization.Get("ItemFilters.Title"):localization.Get("ItemFilters.Active");
        if(!ImGui.TreeNode(title+"###ItemFilters"))return false;
        var filter=itemSearchFilter;
        try
        {
            ImGui.SetNextItemWidth(220);
            var categoryName=filter.Category==0 ? localization.Get("ItemFilters.Any"):catalog.Categories.FirstOrDefault(category=>category.Id==filter.Category)?.Name ?? $"#{filter.Category}";
            if(ImGui.BeginCombo(localization.Get("ItemFilters.Category")+"##FilterCategory",categoryName))
            {
                try
                {
                    if(ImGui.Selectable(localization.Get("ItemFilters.Any"),filter.Category==0))filter=filter with{Category=0};
                    foreach(var category in catalog.Categories)
                        if(ImGui.Selectable($"{category.Name}##Category{category.Id}",filter.Category==category.Id))filter=filter with{Category=category.Id};
                }
                finally{ImGui.EndCombo();}
            }
            Tip("ItemFilters.CategoryTip");
            int min=filter.MinEquipLevel,max=filter.MaxEquipLevel;
            ImGui.SetNextItemWidth(70);if(ImGui.InputInt("##FilterMinLevel",ref min,0))filter=filter with{MinEquipLevel=Math.Clamp(min,0,100)};
            Tip("ItemFilters.EquipLevelTip");
            ImGui.SameLine();ImGui.SetNextItemWidth(70);if(ImGui.InputInt(localization.Get("ItemFilters.EquipLevel")+"##FilterMaxLevel",ref max,0))filter=filter with{MaxEquipLevel=Math.Clamp(max,0,100)};
            Tip("ItemFilters.EquipLevelTip");
            int minIl=(int)filter.MinItemLevel,maxIl=(int)filter.MaxItemLevel;
            ImGui.SetNextItemWidth(70);if(ImGui.InputInt("##FilterMinIl",ref minIl,0))filter=filter with{MinItemLevel=(uint)Math.Clamp(minIl,0,9999)};
            Tip("ItemFilters.ItemLevelTip");
            ImGui.SameLine();ImGui.SetNextItemWidth(70);if(ImGui.InputInt(localization.Get("ItemFilters.ItemLevel")+"##FilterMaxIl",ref maxIl,0))filter=filter with{MaxItemLevel=(uint)Math.Clamp(maxIl,0,9999)};
            Tip("ItemFilters.ItemLevelTip");
            ImGui.SetNextItemWidth(220);
            if(ImGui.BeginCombo(localization.Get("ItemFilters.Slot")+"##FilterSlot",filter.Slot==EquipmentSlots.None ? localization.Get("ItemFilters.Any"):localization.Get("ItemFilters.Slot"+filter.Slot)))
            {
                try
                {
                    if(ImGui.Selectable(localization.Get("ItemFilters.Any"),filter.Slot==EquipmentSlots.None))filter=filter with{Slot=EquipmentSlots.None};
                    foreach(var slot in FilterSlots)if(ImGui.Selectable(localization.Get("ItemFilters.Slot"+slot),filter.Slot==slot))filter=filter with{Slot=slot};
                }
                finally{ImGui.EndCombo();}
            }
            Tip("ItemFilters.SlotTip");
            var tradable=filter.TradableOnly;
            if(ImGui.Checkbox(localization.Get("ItemFilters.Tradable")+"##FilterTradable",ref tradable))filter=filter with{TradableOnly=tradable};
            Tip("ItemFilters.TradableTip");
            ImGui.SameLine();
            var marketable=filter.MarketableOnly;
            if(ImGui.Checkbox(localization.Get("ItemFilters.Marketable")+"##FilterMarketable",ref marketable))filter=filter with{MarketableOnly=marketable};
            Tip("ItemFilters.MarketableTip");
            if(!filter.IsEmpty)
            {
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("ItemFilters.Reset")))filter=ItemSearchFilter.None;
                Tip("ItemFilters.ResetTip");
            }
        }
        finally{ImGui.TreePop();}
        if(filter==itemSearchFilter)return false;
        itemSearchFilter=filter;
        return true;
    }

    // Key items: names only (no item sheet), searched when the text changes.
    private void DrawKeyItemSearch()
    {
        if(KeyItemSearch is null || !ImGui.TreeNode(localization.Get("ItemFilters.KeyItems")+"###KeyItems"))return;
        try
        {
            ImGui.SetNextItemWidth(-1);
            if(ImGui.InputTextWithHint("##KeyItemQuery",localization.Get("ItemsUi.Query"),ref keyItemQuery,64))keyItemResults=KeyItemSearch(keyItemQuery);
            Tip("ItemFilters.KeyItemsTip");
            if(keyItemQuery.Trim().Length>0 && keyItemResults.Count==0)ImGui.TextDisabled(localization.Get("ItemFilters.KeyItemsNone"));
            foreach(var (id,name,icon) in keyItemResults)
            {
                if(icon!=0 && textures.TryGetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                {ImGui.Image(image.Handle,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();}
                ImGui.TextUnformatted(ShowIds ? $"{name}  ({id})":name);
            }
        }
        finally{ImGui.TreePop();}
    }
}
