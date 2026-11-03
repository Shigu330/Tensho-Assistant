using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// g, I-13 (0.354): under Storage › Currencies, « What to do with it » for one currency: what it buys (vendor directory), its price in
// that currency, the market price (Universalis lowest of the current world, fresh only) and the gil each unit is worth, collection items
// marked owned or missing. Market prices fetched on a click, 32 items at most, Universalis switch honoured. Rows rebuilt on change only.
internal sealed partial class MainWindow
{
    private uint currencyValueItem;
    private readonly PreparationPriceSession currencyPrices=new();
    private (uint Item,object? Catalog,uint World,int Done,bool Loading,IReadOnlyList<CollectionEntry> Collections)? currencyValueKey;
    private IReadOnlyList<CurrencyBuy> currencyValueRows=[];
    // Mount or minion items among the rows: owned (true) or not confirmed (false); computed with the rows, not per frame.
    private Dictionary<uint,bool> currencyValueOwned=[];

    private void DrawCurrencyValueButton(uint currency)
    {
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get(currencyValueItem==currency ? "CurrencyValue.Close":"CurrencyValue.Open")+$"##Value{currency}"))
            currencyValueItem=currencyValueItem==currency ? 0:currency;
        Tip("CurrencyValue.OpenTip");
    }

    private void DrawCurrencyValue()
    {
        if(currencyValueItem==0)return;
        var vendors=npcVendors?.State.Catalog;
        var items=itemCatalog.State.Catalog;
        var currencyName=items?.Find(currencyValueItem)?.Name ?? $"#{currencyValueItem}";
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CurrencyValue.Title",currencyName)+"###CurrencyValue",ImGuiTreeNodeFlags.DefaultOpen))return;
        if(vendors is null){ImGui.TextDisabled(localization.Get("Events.ShopsLoading"));return;}
        var world=market.State;var prices=currencyPrices.State;var collected=collections.State.Entries;
        var key=(currencyValueItem,(object?)vendors,world.WorldId,prices.Completed,prices.Loading,collected);
        if(currencyValueKey!=key)
        {
            var now=DateTimeOffset.UtcNow;
            currencyValueRows=CurrencyValue.Rank(vendors.PaidWith(currencyValueItem).DistinctBy(offer=>(offer.Sale.ItemId,offer.Sale.Hq)).Select(offer=>
            {
                var cost=offer.Sale.Costs.FirstOrDefault(entry=>entry.CurrencyItemId==currencyValueItem)?.Amount ?? 0;
                var quote=world.WorldId==0 ? null:market.GetCached(world.WorldId,offer.Sale.ItemId);
                decimal? price=quote is not null && market.IsFresh(quote,now) ? offer.Sale.Hq ? quote.Hq.Minimum:quote.Nq.Minimum:null;
                return new CurrencyBuy(offer.Sale.ItemId,Math.Max(1,offer.Sale.Quantity),cost,offer.Sale.Hq,price);
            }).Where(buy=>buy.Cost>0));
            var mounts=collected.Where(entry=>entry.Kind==CollectionKind.Mount && entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            var minions=collected.Where(entry=>entry.Kind==CollectionKind.Companion && entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            currencyValueOwned=currencyValueRows.Select(row=>items?.Find(row.ItemId)).OfType<Items.ItemDefinition>().Where(item=>item.MountId!=0 || item.CompanionId!=0)
                .DistinctBy(item=>item.Id).ToDictionary(item=>item.Id,item=>item.MountId!=0 ? mounts.Contains(item.MountId):minions.Contains(item.CompanionId));
            currencyValueKey=key;
        }
        ImGui.TextWrapped(localization.Get("CurrencyValue.Help"));
        // Missing market prices of marketable items, the cheapest in currency first; 32 per click.
        var missing=currencyValueRows.Where(row=>row.MarketPrice is null && items?.Find(row.ItemId) is { IsMarketable:true }).Select(row=>row.ItemId).Distinct().ToArray();
        if(prices.Loading)ImGui.TextDisabled(localization.Format("RoutePlanner.FetchingPrices",prices.Completed,prices.Items.Count));
        else
        {
            var character=bankBridge.State.CharacterId;
            ImGui.BeginDisabled(world.WorldId==0 || character==0 || missing.Length==0);
            try
            {
                if(SourceButton(localization.Format("RoutePlanner.FetchPrices",Math.Min(32,missing.Length),missing.Length),configuration.CommunityUniversalis,true))
                    try{_=currencyPrices.StartListAsync(market,character,world.WorldId,missing.Take(32));}
                    catch(Exception error){log.Warning(error,"Currency value prices could not be started.");}
            }
            finally{ImGui.EndDisabled();}
            Tip("CurrencyValue.FetchTip");
        }
        if(currencyValueRows.Count==0){ImGui.TextDisabled(localization.Get("CurrencyValue.None"));return;}
        if(!ImGui.BeginTable("CurrencyValue",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollY,
            new Vector2(0,Math.Min(ImGui.GetTextLineHeightWithSpacing()*16,ImGui.GetTextLineHeightWithSpacing()*(currencyValueRows.Count+2)))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"Item","Cost","Market","PerUnit","Collection"})ImGui.TableSetupColumn(localization.Get("CurrencyValue.Column"+column));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var row in currencyValueRows.Take(200))
            {
                var definition=items?.Find(row.ItemId);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawItemIcon(definition,size);ImGui.SameLine();
                ImGui.TextUnformatted((definition?.Name ?? $"#{row.ItemId}")+(row.Hq ? " HQ":"")+(row.Quantity>1 ? $" ×{row.Quantity}":""));
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Cost.ToString("N0"));
                ImGui.TableNextColumn();
                if(row.MarketPrice is { } price)ImGui.TextUnformatted(price.ToString("N0"));
                else ImGui.TextDisabled(definition is { IsMarketable:false } ? localization.Get("CurrencyValue.NotSold"):"—");
                ImGui.TableNextColumn();
                if(row.GilPerUnit is { } perUnit)ImGui.TextColored(Success,perUnit.ToString("N1"));else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                bool? owned=currencyValueOwned.TryGetValue(row.ItemId,out var known) ? known:null;
                if(owned==true)ImGui.TextColored(Success,localization.Get("CurrencyValue.Owned"));
                else if(owned==false)ImGui.TextColored(Accent,localization.Get("CurrencyValue.Missing"));
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }
}
