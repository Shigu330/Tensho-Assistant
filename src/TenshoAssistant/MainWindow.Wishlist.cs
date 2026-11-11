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

    // X-101 (0.1000, DEC-048): the check, from the frame (not during a cut: the plugin draws nothing then); the request runs off the frame.
    private DateTimeOffset wishSalesNext;
    private int wishSalesOffset;
    private Task<IReadOnlyList<string>>? wishSalesTask;
    private readonly HashSet<uint> wishSalesSaid=[];
    internal void WatchWishlistSales()
    {
        if(wishSalesTask is { IsCompleted:true } done)
        {
            wishSalesTask=null;
            if(done.IsCompletedSuccessfully)foreach(var line in done.Result)ChatNotice?.Invoke(line);
        }
        if(!configuration.WishlistSaleAlert || !configuration.CommunityUniversalis || wishSalesTask is not null)return;
        var now=DateTimeOffset.UtcNow;
        if(now<wishSalesNext)return;
        wishSalesNext=now+WishlistSales.Every;
        var prices=configuration.WishlistPrices;var world=market.State.WorldId;var character=bankBridge.State.CharacterId;
        if(world==0 || character==0)return;
        var batch=WishlistSales.Batch(configuration.Wishlist,prices,ref wishSalesOffset);
        if(batch.Count==0)return;
        var targets=batch.ToDictionary(id=>id,id=>prices![id]);
        var items=itemCatalog.State.Catalog;
        wishSalesTask=Task.Run(async ()=>
        {
            var lines=new List<string>();
            if(await market.FillCacheAsync(batch,character,world,CancellationToken.None,true).ConfigureAwait(false) is { } refused){log.Information("Wishlist sales check: {0}",refused);return (IReadOnlyList<string>)lines;}
            lock(wishSalesSaid)
                foreach(var (id,target) in targets)
                {
                    var cheapest=WishlistSales.Cheapest(market.GetCached(world,id));
                    if(!WishlistSales.Say(id,cheapest,target,wishSalesSaid))continue;
                    lines.Add(localization.Format("Wishlist.OnSale",items?.Find(id)?.Name ?? $"#{id}",cheapest!.Price.ToString("N0"),WorldName?.Invoke(cheapest.WorldId) ?? "?",target.ToString("N0")));
                }
            return (IReadOnlyList<string>)lines;
        });
    }

    private void DrawWishlist()
    {
        ImGui.TextWrapped(localization.Get("Wishlist.Help"));
        // X-101 (0.1000, DEC-048): the sale alert, off by default; it needs Universalis on.
        ImGui.BeginDisabled(!configuration.CommunityUniversalis);
        try{BoolSetting("Wishlist.SaleAlert","Wishlist.SaleAlertTip",()=>configuration.WishlistSaleAlert,value=>configuration.WishlistSaleAlert=value,c=>c.WishlistSaleAlert);}
        finally{ImGui.EndDisabled();}
        var list=configuration.Wishlist;
        if(list is not { Count:>0 }){ImGui.TextDisabled(localization.Get("Wishlist.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        RefreshRecipeHoldingsCache();
        ImGui.TextDisabled(localization.Format("Wishlist.Count",list.Count,Wishlist.MaximumItems));
        if(!ImGui.BeginTable("Wishlist",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        uint? remove=null;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Wishlist.ColumnItem"),ImGuiTableColumnFlags.WidthStretch,3);
            ImGui.TableSetupColumn(localization.Get("Wishlist.ColumnState"),ImGuiTableColumnFlags.WidthStretch,1);
            ImGui.TableSetupColumn(localization.Get("Wishlist.ColumnPrice"),ImGuiTableColumnFlags.WidthFixed,110);
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
                Tip("Wishlist.RowTip");
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
                // X-101 (0.1000): the player's price (0 = no alert for this wish); written when the field is left.
                var price=(int)Math.Clamp(configuration.WishlistPrices?.GetValueOrDefault(id) ?? 0,0,int.MaxValue);
                ImGui.SetNextItemWidth(100);
                if(ImGui.InputInt($"##WishPrice{id}",ref price,0))
                {
                    var prices=configuration.WishlistPrices ??= [];
                    if(price<=0)prices.Remove(id);else prices[id]=price;
                }
                Tip("Wishlist.PriceTip");
                if(ImGui.IsItemDeactivatedAfterEdit())TrySave("Wishlist.ColumnPrice");
                ImGui.TableNextColumn();
                if(DeleteButton("Wishlist.RemoveShort",$"{id}"))remove=id;
            }
        }
        finally{ImGui.EndTable();}
        if(remove is { } removed)ToggleWish(removed);
    }
}
