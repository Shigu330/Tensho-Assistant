using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, C-09 (0.360): the wishlist — « Add to my wishlist » in every item's right-click menu, and the « Wishlist » tab of Collections: each
// item with its collection state (mount, minion, roll, card) or the copies the character holds, its sources on right click, and removal.
internal sealed partial class MainWindow
{
    private bool collectionWishlistTab;

    private void DrawWishlistToggle(ItemDefinition item)
    {
        var wished=configuration.Wishlist?.Contains(item.Id)==true;
        if(ImGui.Selectable(localization.Get(wished ? "Wishlist.Remove":"Wishlist.Add")+$"##Wish{item.Id}"))ToggleWish(item.Id);
    }

    private void ToggleWish(uint itemId)
    {
        if(Wishlist.Toggle(configuration.Wishlist,itemId) is not { } next){saveError=localization.Format("Wishlist.Full",Wishlist.MaximumItems);return;}
        SaveSetting("Wishlist.Title",()=>configuration.Wishlist,value=>configuration.Wishlist=value,next.Count==0 ? null:next);
    }

    private void DrawWishlist()
    {
        ImGui.TextWrapped(localization.Get("Wishlist.Help"));
        var list=configuration.Wishlist;
        if(list is not { Count:>0 }){ImGui.TextDisabled(localization.Get("Wishlist.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        RefreshRecipeHoldingsCache();
        ImGui.TextDisabled(localization.Format("Wishlist.Count",list.Count,Wishlist.MaximumItems));
        if(!ImGui.BeginTable("Wishlist",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        uint? remove=null;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Wishlist.ColumnItem"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("Wishlist.ColumnState"),ImGuiTableColumnFlags.WidthStretch,1);
            ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed,ImGui.GetFrameHeight()*3);
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight()*1.4f);
            foreach(var id in list)
            {
                var definition=items?.Find(id);
                ImGui.TableNextRow();ImGui.TableNextColumn();
                DrawItemIcon(definition,size);DrawCollectionMark(definition);ImGui.SameLine();
                ImGui.Selectable((definition?.Name ?? $"#{id}")+$"##WishItem{id}");
                ItemLinksMenu($"WishMenu{id}",definition);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Wishlist.RowTip"));
                ImGui.TableNextColumn();
                if(CollectionOf(definition) is { } entry)
                {
                    var state=CollectionStateOf(entry);
                    if(state==CollectionAcquisition.Acquired)ImGui.TextColored(Success,localization.Get("Wishlist.Obtained"));
                    else if(state==CollectionAcquisition.NotAcquired)ImGui.TextColored(Accent,localization.Get("Wishlist.Missing"));
                    else ImGui.TextDisabled("—");
                }
                else
                {
                    var holdings=GetRecipeHoldings(id);
                    if(!holdings.HasObservation)ImGui.TextDisabled("—");
                    else if(holdings.Total>0)ImGui.TextColored(Success,localization.Format("Wishlist.Held",holdings.Total));
                    else ImGui.TextColored(Accent,localization.Get("Wishlist.NotHeld"));
                }
                ImGui.TableNextColumn();
                if(ImGui.SmallButton(localization.Get("Wishlist.RemoveShort")+$"##WishRemove{id}"))remove=id;
            }
        }
        finally{ImGui.EndTable();}
        if(remove is { } removed)ToggleWish(removed);
    }
}
