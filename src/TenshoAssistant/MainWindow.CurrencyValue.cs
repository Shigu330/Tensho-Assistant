using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// g, I-13 (0.354): under Storage › Currencies, « What to do with it » for one currency: what it buys (vendor directory), its price in
// that currency, the market price (Universalis lowest of the current world, fresh only) and the gil each unit is worth, collection items
// marked owned or missing. Market prices fetched on a click, 32 items at a time, Universalis switch honoured. Rows rebuilt on change only.
// 0.695 (user report « no market price »): the page asked for a second click and never said a request had failed. Choosing a currency is the click now,
// the batches follow one another, a failure shows, and the page has its own tab.
internal sealed partial class MainWindow
{
    private uint currencyValueItem;
    private readonly PreparationPriceSession currencyPrices=new();
    private (uint Item,object? Catalog,uint World,int Done,bool Loading,IReadOnlyList<CollectionEntry> Collections,long? Balance,int Days)? currencyValueKey;
    /// <summary>
    /// X-76 H4 (0.914): the days of sales shared by « What can I craft? » and this page, one editor for both. The value is bounded and
    /// used as typed, written once when the field is left (it was written on every key press), and the value from before the edit comes
    /// back when saving fails.
    /// </summary>
    private void SaleDaysSetting(string id,string tipKey)
    {
        ImGui.SetNextItemWidth(70*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        var before=configuration.CraftStockSaleDays;var saleDays=FromStockDays;
        writeSaleDays??=WriteSaleDays;
        if(ImGui.InputInt(localization.Get("CraftStock.SaleDays")+"###"+id,ref saleDays,0))writeSaleDays(saleDays);
        SaveWhenEditEnds("CraftStockSaleDays",writeSaleDays,before);
        Tip(tipKey);
    }
    private Action<int>? writeSaleDays;
    private void WriteSaleDays(int saleDays)
    {
        configuration.CraftStockSaleDays=Math.Clamp(saleDays,1,30);
        fromStockView=null;
    }

    // 0.794 (X-33): the balance of the chosen currency, as the Currencies tab reads it; null: not read, which is not zero.
    private long? CurrencyValueBalance()
    {
        var watcher=Currencies?.Invoke();
        watcher?.Want();
        return watcher?.State.Reading is { } reading && reading.Character==characterStats.State.CharacterId && characterStats.State.CharacterId!=0
            ? reading.Balances.FirstOrDefault(balance=>balance.Currency.ItemId==currencyValueItem)?.Amount:null;
    }
    private IReadOnlyList<CurrencyBuy> currencyValueRows=[];
    // Mount or minion items among the rows: owned (true) or not confirmed (false); computed with the rows, not per frame.
    private Dictionary<uint,bool> currencyValueOwned=[];

    private bool focusCurrencyValueTab,currencyPricesWanted;
    // 0.697 (user request): 0 everything, 1 collection, 2 collection still missing, 3 sold on the market.
    private int currencyValueFilter;
    private string currencyValueSearch="";
    private readonly HashSet<uint> currencyPricesAsked=[];
    private void ChooseCurrencyValue(uint currency)
    {
        currencyValueItem=currency;
        currencyPricesAsked.Clear();
        currencyPricesWanted=true;
    }

    private void DrawCurrencyValueButton(uint currency)
    {
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("CurrencyValue.Open")+$"##Value{currency}")){ChooseCurrencyValue(currency);focusCurrencyValueTab=true;}
        Tip("CurrencyValue.OpenTip");
    }

    // The tab: the currency to look at, then what it buys.
    private void DrawCurrencyValueTab()
    {
        var watcher=Currencies?.Invoke();
        watcher?.Want();
        var balances=watcher?.State?.Reading?.Balances;
        ImGui.SetNextItemWidth(280);
        var items=itemCatalog.State.Catalog;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        if(ImGui.BeginCombo("###CurrencyValuePick",currencyValueItem==0 ? localization.Get("CurrencyValue.Pick"):Name(currencyValueItem)))
        {
            try
            {
                foreach(var balance in balances ?? [])
                    if(ImGui.Selectable($"{balance.Currency.Name}##ValuePick{balance.Currency.ItemId}",balance.Currency.ItemId==currencyValueItem))ChooseCurrencyValue(balance.Currency.ItemId);
            }
            finally{ImGui.EndCombo();}
        }
        Tip("CurrencyValue.PickTip");
        if(currencyValueItem==0){ImGui.TextDisabled(localization.Get("CurrencyValue.PickFirst"));return;}
        DrawCurrencyValue();
    }

    private void DrawCurrencyValue()
    {
        var vendors=npcVendors?.State.Catalog;
        var items=itemCatalog.State.Catalog;
        var currencyName=items?.Find(currencyValueItem)?.Name ?? $"#{currencyValueItem}";
        ImGui.TextColored(Accent,localization.Format("CurrencyValue.Title",currencyName));
        if(vendors is null){ImGui.TextDisabled(localization.Get("Events.ShopsLoading"));return;}
        var world=market.State;var prices=currencyPrices.State;var collected=collections.State.Entries;
        var balance=CurrencyValueBalance();var days=FromStockDays;
        var key=(currencyValueItem,(object?)vendors,world.WorldId,prices.Completed,prices.Loading,collected,balance,days);
        if(currencyValueKey!=key)
        {
            var now=DateTimeOffset.UtcNow;
            currencyValueRows=CurrencyValue.RankBySales(vendors.PaidWith(currencyValueItem).DistinctBy(offer=>(offer.Sale.ItemId,offer.Sale.Hq)).Select(offer=>
            {
                var cost=offer.Sale.Costs.FirstOrDefault(entry=>entry.CurrencyItemId==currencyValueItem)?.Amount ?? 0;
                var quote=world.WorldId==0 ? null:market.GetCached(world.WorldId,offer.Sale.ItemId);
                // 0.711 (user report): what it really sold for, not what is asked.
                var sold=quote is not null && market.IsFresh(quote,now) ? quote.Sold(offer.Sale.Hq):null;
                return new CurrencyBuy(offer.Sale.ItemId,Math.Max(1,offer.Sale.Quantity),cost,offer.Sale.Hq,sold?.Price,sold?.PerDay);
            }).Where(buy=>buy.Cost>0),balance,days);
            var mounts=collected.Where(entry=>entry.Kind==CollectionKind.Mount && entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            var minions=collected.Where(entry=>entry.Kind==CollectionKind.Companion && entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            currencyValueOwned=currencyValueRows.Select(row=>items?.Find(row.ItemId)).OfType<Items.ItemDefinition>().Where(item=>item.MountId!=0 || item.CompanionId!=0)
                .DistinctBy(item=>item.Id).ToDictionary(item=>item.Id,item=>item.MountId!=0 ? mounts.Contains(item.MountId):minions.Contains(item.CompanionId));
            currencyValueKey=key;
        }
        ImGui.TextWrapped(localization.Get("CurrencyValue.Help"));
        // 0.794 (X-33, user request): the same days of sales as « What can I craft? », and the balance they are counted against.
        SaleDaysSetting("CurrencyValueDays","CurrencyValue.SaleDaysTip");
        ImGui.SameLine();
        ImGui.TextDisabled(balance is { } held ? localization.Format("CurrencyValue.Balance",held.ToString("N0")):localization.Get("CurrencyValue.BalanceUnknown"));
        BoolSetting("CurrencyValue.Market","CurrencyValue.MarketTip",()=>configuration.CurrencyValueMarket,value=>
        {
            configuration.CurrencyValueMarket=value;
            if(value)currencyPricesWanted=true;
            else{currencyPricesWanted=false;currencyPrices.Cancel();}
        },c=>c.CurrencyValueMarket);
        // Missing market prices of marketable items, 32 at a time. An item Universalis gave no price for is not asked for again.
        var missing=currencyValueRows.Where(row=>row.MarketPrice is null && items?.Find(row.ItemId) is { IsMarketable:true } && !currencyPricesAsked.Contains(row.ItemId)).Select(row=>row.ItemId).Distinct().ToArray();
        var character=bankBridge.State.CharacterId;
        if(prices.Loading)ImGui.TextDisabled(localization.Format("CurrencyValue.Fetching",currencyPricesAsked.Count,currencyPricesAsked.Count+missing.Length));
        else if(configuration.CurrencyValueMarket)
        {
            if(prices.Error is { } failed)
            {
                currencyPricesWanted=false;
                ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CurrencyValue.FetchFailed"));
                if(developerMode)ImGui.TextDisabled(failed);
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("CurrencyValue.Retry"))){currencyPricesAsked.Clear();currencyPricesWanted=true;}
                Tip("CurrencyValue.FetchTip");
            }
            var refresh=false;
            if(world.WorldId==0 || character==0)ImGui.TextDisabled(localization.Get("CurrencyValue.NoWorld"));
            else if(!configuration.CommunityUniversalis)ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));
            else if(missing.Length>0 && !currencyPricesWanted && prices.Error is null)
            {
                refresh=ImGui.SmallButton(localization.Format("CurrencyValue.Fetch",missing.Length));
                Tip("CurrencyValue.FetchTip");
            }
            if(refresh)currencyPricesWanted=true;
            if(currencyPricesWanted && missing.Length>0 && (prices.Error is null || refresh) && world.WorldId!=0 && character!=0 && configuration.CommunityUniversalis)
                try
                {
                    var batch=missing.Take(MarketService.BatchMaximum).ToArray();
                    currencyPricesAsked.UnionWith(batch);
                    _=currencyPrices.StartListAsync(market,character,world.WorldId,batch,true);
                }
                catch(Exception error){currencyPricesWanted=false;log.Warning(error,"Currency value prices could not be started.");}
            else if(missing.Length==0)currencyPricesWanted=false;
        }
        if(currencyValueRows.Count==0){ImGui.TextDisabled(localization.Get("CurrencyValue.None"));return;}
        ImGui.SetNextItemWidth(220);
        string[] filters=[localization.Get("CurrencyValue.FilterAll"),localization.Get("CurrencyValue.FilterCollection"),localization.Get("CurrencyValue.FilterMissing"),localization.Get("CurrencyValue.FilterMarket")];
        ImGui.Combo("###CurrencyValueFilter",ref currencyValueFilter,filters,filters.Length);
        Tip("CurrencyValue.FilterTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("###CurrencyValueSearch",localization.Get("CurrencyValue.Search"),ref currencyValueSearch,100);
        Tip("CurrencyValue.SearchTip");
        var shownRows=currencyValueRows.Where(row=>
        {
            var definition=items?.Find(row.ItemId);
            if(currencyValueSearch.Length>0 && definition?.Name.Contains(currencyValueSearch,StringComparison.CurrentCultureIgnoreCase)!=true)return false;
            return currencyValueFilter switch
            {
                1=>currencyValueOwned.ContainsKey(row.ItemId),
                2=>currencyValueOwned.TryGetValue(row.ItemId,out var owned) && !owned,
                3=>definition is { IsMarketable:true },
                _=>true,
            };
        }).ToArray();
        ImGui.TextDisabled(localization.Format("CurrencyValue.Count",shownRows.Length,currencyValueRows.Count));
        if(shownRows.Length==0)return;
        if(!ImGui.BeginTable("CurrencyValue",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollY,
            new Vector2(0,Math.Max(120,ImGui.GetContentRegionAvail().Y))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"Item","Cost","Market","PerDay","PerUnit","Worth","Collection"})
                ImGui.TableSetupColumn(column=="Worth" ? localization.Format("CurrencyValue.ColumnWorth",days):localization.Get("CurrencyValue.Column"+column));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var row in shownRows.Take(200))
            {
                var definition=items?.Find(row.ItemId);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawItemIcon(definition,size);ImGui.SameLine();
                ImGui.TextUnformatted((definition?.Name ?? $"#{row.ItemId}")+(row.Hq ? " HQ":"")+(row.Quantity>1 ? $" ×{row.Quantity}":""));
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Cost.ToString("N0"));
                ImGui.TableNextColumn();
                if(row.MarketPrice is { } price)ImGui.TextUnformatted(price.ToString("N0"));
                else ImGui.TextDisabled(definition is { IsMarketable:false } ? localization.Get("CurrencyValue.NotSold"):currencyPricesAsked.Contains(row.ItemId) && !prices.Loading && prices.Error is null ? localization.Get("CurrencyValue.NoOffer"):"—");
                ImGui.TableNextColumn();
                if(row.PerDay is { } perDay)ImGui.TextUnformatted(perDay.ToString("0.##",System.Globalization.CultureInfo.CurrentCulture));else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(row.GilPerUnit is { } perUnit)ImGui.TextColored(Success,perUnit.ToString("N1"));else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(row.Worth(balance,days) is { } worth)
                {
                    ImGui.TextColored(worth>0 ? Success:Muted,worth.ToString("N0"));
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("CurrencyValue.WorthTip",(row.Sellable(balance,days) ?? 0).ToString("N0"),days));
                }
                else ImGui.TextDisabled("—");
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
