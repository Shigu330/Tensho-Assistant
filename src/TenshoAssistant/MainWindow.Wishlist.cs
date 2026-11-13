using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, C-09 (0.360): the wishlist — « Add to my wishlist » in every item's right-click menu, and the « Wishlist » tab of Collections: each
// item with its collection state (mount, minion, roll, card) or the copies the character holds, its sources on right click, and removal.
// X-127 (0.1106, user request of 2026-11-11): a page of its own (ITEMS › Wishlist) instead of a Collections tab; « added to your wishlist »
// in the window's footer with a button that opens it; where each gatherable wish is gathered; the alarms of its timed points in one click.
internal sealed partial class MainWindow
{
    /// <summary>X-127: how long the footer says « added to your wishlist ».</summary>
    private const long WishAddedShownMs=15_000;
    private (string Text,long At)? wishAdded;
    // X-155 (0.1261): the item whose addition was just saved, for a brief outline of its button (none with reduced motion).
    private const int WishConfirmPulseMs=300;
    private (uint Item,long At) wishConfirmed=(0,-100_000);

    /// <summary>
    /// X-155 (0.1261, pilot of the visual plan): the wish button of an item's header: « Add to my wishlist », then on the same spot and at the
    /// same width « ✓ On my wishlist » once the save is confirmed (a failed save leaves « Add » and says why in the footer). A click on the
    /// confirmed button takes the item off the list. Greyed in the desktop application (nothing is saved there).
    /// </summary>
    private void DrawWishButton(ItemDefinition item)
    {
        var wished=configuration.Wishlist?.Contains(item.Id)==true;
        var add=localization.Get("Wishlist.Add");var inList=localization.Get("Wishlist.InList");
        var style=ImGui.GetStyle();var glyph=ImGui.GetFontSize();
        // One width for both labels: the confirmation moves nothing.
        var width=MathF.Max(ImGui.CalcTextSize(add).X,ImGui.CalcTextSize(inList).X+glyph+style.ItemInnerSpacing.X)+style.FramePadding.X*2;
        bool clicked;
        DesktopEditBegin();
        try{clicked=ImGui.Button($"##WishButton{item.Id}",new Vector2(width,ImGui.GetFrameHeight()));}
        finally{DesktopEditEnd();}
        Tip(wished ? "Wishlist.InListTip":"Wishlist.AddTip");
        var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
        var draw=ImGui.GetWindowDrawList();
        var text=ImGui.GetColorU32(ImGuiCol.Text);
        if(wished)
        {
            DrawGlyphIn(Dalamud.Interface.FontAwesomeIcon.Check,new Vector2(min.X+style.FramePadding.X,min.Y),new Vector2(min.X+style.FramePadding.X+glyph,max.Y),Success,glyph*0.85f);
            draw.AddText(new Vector2(min.X+style.FramePadding.X+glyph+style.ItemInnerSpacing.X,min.Y+style.FramePadding.Y),text,inList);
            var pulse=wishConfirmed.Item==item.Id ? TenshoAssistant.UI.Motion.Pulse(wishConfirmed.At,Environment.TickCount64,WishConfirmPulseMs,ReducedMotion):0;
            if(pulse>0)draw.AddRect(min,max,ImGui.GetColorU32(Success with{W=Success.W*pulse}),style.FrameRounding,ImDrawFlags.None,2);
        }
        else draw.AddText(new Vector2(min.X+style.FramePadding.X,min.Y+style.FramePadding.Y),text,add);
        if(clicked)ToggleWish(item.Id);
    }

    private void DrawWishlistToggle(ItemDefinition item)
    {
        var wished=configuration.Wishlist?.Contains(item.Id)==true;
        if(ImGui.Selectable(localization.Get(wished ? "Wishlist.Remove":"Wishlist.Add")+$"##Wish{item.Id}"))ToggleWish(item.Id);
    }

    private void ToggleWish(uint itemId)
    {
        if(Wishlist.Toggle(configuration.Wishlist,itemId) is not { } next){saveError=localization.Format("Wishlist.Full",Wishlist.MaximumItems);return;}
        var added=next.Contains(itemId) && configuration.Wishlist?.Contains(itemId)!=true;
        SaveSetting("Wishlist.Title",()=>configuration.Wishlist,value=>configuration.Wishlist=value,next.Count==0 ? null:next);
        // X-155 (0.1261): said only once the save is confirmed (a failed save puts the list back: nothing was added).
        added&=configuration.Wishlist?.Contains(itemId)==true;
        if(added)wishConfirmed=(itemId,Environment.TickCount64);
        if(added)SayWishAdded(localization.Format("Wishlist.Added",itemCatalog.State.Catalog?.Find(itemId)?.Name ?? $"#{itemId}"));
    }

    /// <summary>X-127: the footer's line « added to your wishlist », for a while.</summary>
    private void SayWishAdded(string text)=>wishAdded=(text,Environment.TickCount64);

    /// <summary>X-127: the footer's line after an addition, with a button that opens the list; false when there is nothing to say.</summary>
    private bool DrawWishAdded()
    {
        if(wishAdded is not { } added)return false;
        if(Environment.TickCount64-added.At>WishAddedShownMs){wishAdded=null;return false;}
        ImGui.TextColored(Success,added.Text);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Wishlist.Open")+"###WishAddedOpen")){page=Page.Wishlist;wishAdded=null;}
        Tip("Wishlist.OpenTip");
        return true;
    }

    // X-127: where each wish is gathered, worked out once per catalogue (and again every minute, for the language), never in every frame.
    private object? wishPlacesCatalog;
    private long wishPlacesAt;
    private readonly Dictionary<uint,(int Count,string[] Lines)> wishPlaces=[];
    private (int Count,string[] Lines) WishPlaces(uint itemId)
    {
        var catalog=gatheringCatalog.State.Catalog;
        var now=Environment.TickCount64;
        if(!ReferenceEquals(catalog,wishPlacesCatalog) || now-wishPlacesAt>60_000){wishPlaces.Clear();wishPlacesCatalog=catalog;wishPlacesAt=now;}
        if(catalog is null)return (0,[]);
        if(wishPlaces.TryGetValue(itemId,out var known))return known;
        var places=WishlistGathering.Places(catalog.ForItem(itemId));
        known=(places.Count,places.Take(WishlistGathering.Shown).Select(place=>WishPlaceText(itemId,place)).ToArray());
        wishPlaces[itemId]=known;
        return known;
    }

    private string WishPlaceText(uint itemId,WishlistGathering.Place place)
    {
        var parts=new List<string>{place.IsHidden ? localization.Format("GatheringDetails.Hidden",place.TypeName):place.TypeName,place.Zone};
        if(place.Node.Length>0)parts.Add(place.Node);
        // A fish: GatherBuddy's hours and weathers when the game data gives no window (as the Gathering page does).
        if(place.IsFishing && place.Timing.Kind!=GatheringScheduleKind.Limited && gatherBuddyFish?.State.Data?.Find(itemId) is { } fish)
        {
            parts.Add(fish.StartMinute is { } start && fish.EndMinute is { } stop
                ? ScheduleText(GatheringSchedule.FromEphemeral((ushort)(start/60*100+start%60),(ushort)(stop/60%24*100+stop%60)))
                : localization.Get("GatheringFish.AnyTime"));
            var weathers=weather.State.Catalog;
            if(fish.Weathers.Count>0)parts.Add(localization.Format("FishingPanel.Weather",string.Join(", ",fish.Weathers.Select(id=>weathers?.Weather(id)?.Name ?? id.ToString()))));
        }
        else parts.Add(ScheduleText(place.Timing));
        return localization.Format("Wishlist.GatherAt",string.Join(" · ",parts));
    }

    private void DrawWishPlaces(uint itemId)
    {
        var (count,lines)=WishPlaces(itemId);
        foreach(var line in lines)ImGui.TextDisabled(line);
        if(count>lines.Length)ImGui.TextDisabled(localization.Format("Wishlist.MorePlaces",count-lines.Length));
    }

    /// <summary>X-127: a wish whose collection entry is acquired needs no alarm; the copies held are checked by ListAlarms.</summary>
    private bool WishObtained(uint itemId)
        =>CollectionOf(itemCatalog.State.Catalog?.Find(itemId)) is { } entry && CollectionStateOf(entry)==CollectionAcquisition.Acquired;

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
                    lines.Add(localization.Format("Wishlist.OnSale",items?.Find(id)?.Name ?? $"#{id}",cheapest!.Price.ToString("N0",Numbers),WorldName?.Invoke(cheapest.WorldId) ?? "?",target.ToString("N0",Numbers)));
                }
            return (IReadOnlyList<string>)lines;
        });
    }

    private void DrawWishlistPage()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("Wishlist.Title"),localization.Get("Wishlist.Help"));
        // X-101 (0.1000, DEC-048): the sale alert, off by default; it needs Universalis on.
        ImGui.BeginDisabled(!configuration.CommunityUniversalis);
        try{BoolSetting("Wishlist.SaleAlert","Wishlist.SaleAlertTip",()=>configuration.WishlistSaleAlert,value=>configuration.WishlistSaleAlert=value,c=>c.WishlistSaleAlert);}
        finally{ImGui.EndDisabled();}
        var list=configuration.Wishlist;
        if(list is not { Count:>0 }){ImGui.TextDisabled(localization.Get("Wishlist.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        RefreshRecipeHoldingsCache();
        ImGui.TextDisabled(localization.Format("Wishlist.Count",list.Count,Wishlist.MaximumItems));
        // X-127: the alarms of every timed point of the wishes still to get, in one click (same button and rules as a list's).
        ImGui.SameLine();
        DrawListPanelAlarms("Wishlist",WishlistGathering.AlarmLines(list,WishObtained));
        if(!UI.UiComponents.BeginDataTable("Wishlist",4))return;
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
                DrawWishPlaces(id);
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
