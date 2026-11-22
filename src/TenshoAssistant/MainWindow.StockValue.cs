using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// X-207 (0.1782, the user's request of 12/11, K-14): Storage › Report › the estimated value of the stocks, per storage of the character
// logged in and per character, at the market prices of your world asked on your click only (Universalis, DEC-048), by lists of 100. Advice
// only: nothing is sold or moved. An item without a price is counted apart, never valued 0 (R06); old prices are said with their date.
internal sealed partial class MainWindow
{
    private Task<string?>? stockValueTask;
    private CancellationTokenSource? stockValueCancel;
    private int stockValueAsked,stockValueTotal;
    private IReadOnlyList<StockValueLine> stockValuePlaces=[],stockValueCharacters=[];
    private DateTimeOffset? stockValueAt;
    private string? stockValueError;
    // X-779 (the review of 0.1784, M1): the quotes of this estimate, copied as each list comes: the market cache (4096 quotes, the oldest
    // dropped first) could drop the first lists of a long estimate before the end. Written by the task, read once it has ended.
    private Dictionary<uint,Market.MarketQuote> stockValueQuotes=[];
    // X-207 (part 2): « sits and sells », built with the same prices (no request more).
    private const int StockSleepersShown=50;
    private IReadOnlyList<StockSleeper> stockSleepers=[];
    private int stockSleepersUnknown;

    // The kinds of storage valued: what a character can sell from (bags, saddlebags, retainers, armoury, crystals); not the glamour dresser
    // nor the armoire (kept for their looks), nor the currencies, nor the company chest.
    private static bool StockValued(BankStorageKind kind)=>kind is BankStorageKind.Bags or BankStorageKind.Saddlebag or BankStorageKind.PremiumSaddlebag
        or BankStorageKind.Retainer or BankStorageKind.Armoury or BankStorageKind.Crystals;

    // Every character's places: the live bags for the character logged in (fresher than the saved ones), the saved storages for all.
    private List<(ulong Character,string Place,IReadOnlyList<InventoryStack> Stacks)> StockValuePlaces(ulong character)
    {
        var places=new List<(ulong,string,IReadOnlyList<InventoryStack>)>();
        if(inventory.Current is { } live)
            places.Add((character,localization.Get("BankUi.Bags"),[..live.Containers.Values.Where(container=>container.Location.Owner.Value==character
                && container.Location.Container.Value is >= 0 and <= 3 && container.Coverage!=InventoryCoverage.Unknown).SelectMany(container=>container.Stacks)]));
        foreach(var entry in bank.State.Entries.Where(entry=>StockValued(entry.Key.Kind) && !(entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.Bags && inventory.Current is not null)))
            places.Add((entry.Key.CharacterId,BankEntryName(entry),[..entry.Snapshot.Containers.Values.Where(container=>container.Coverage!=InventoryCoverage.Unknown).SelectMany(container=>container.Stacks)]));
        return places;
    }

    private void DrawStockValue()
    {
        var character=bankBridge.State.CharacterId;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("StockValue.Title")+"###StockValue"))return;
        ImGui.TextWrapped(localization.Get("StockValue.Help"));
        if(stockValueTask is { IsCompleted:true } done)
        {
            stockValueTask=null;
            // Stopped or refused part way: the value is built with the prices already there (the items left are counted without a price).
            stockValueError=done.IsCanceled ? localization.Get("StockValue.Stopped"):done.IsCompletedSuccessfully ? done.Result:localization.Get("StockValue.Failed");
            BuildStockValue(character);
        }
        var world=market.State.WorldId;
        if(stockValueTask is not null)
        {
            StateLine(UI.UiState.Waiting,localization.Format("StockValue.Asking",Volatile.Read(ref stockValueAsked),stockValueTotal));
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("StockValue.Stop")))stockValueCancel?.Cancel();
            Tip("StockValue.StopTip");
        }
        else
        {
            var allowed=configuration.CommunityUniversalis && world!=0 && character!=0;
            ImGui.BeginDisabled(!allowed);
            try{if(ImGui.Button(localization.Get("StockValue.Estimate")))StartStockValue(character,world);}
            finally{ImGui.EndDisabled();}
            Tip(configuration.CommunityUniversalis ? "StockValue.EstimateTip":"StockValue.EstimateOffTip");
        }
        if(stockValueError is { } error)StateLine(UI.UiState.Failed,error);
        if(stockValueAt is not { } at){ImGui.TextDisabled(localization.Get("StockValue.NotYet"));return;}
        ImGui.TextDisabled(localization.Format("StockValue.At",at.ToLocalTime().ToString("g"),market.State.WorldName));
        DrawStockValueTable("StockValuePlaces","StockValue.ColumnPlace",stockValuePlaces);
        DrawStockValueTable("StockValueCharacters","StockValue.ColumnCharacter",stockValueCharacters);
        DrawStockSleepers();
    }

    // X-207 (part 2): « sits and sells »: what sits in the storages and sells on the world, by sold price × sales a day; the NPC's price beside.
    private void DrawStockSleepers()
    {
        ImGui.Spacing();
        TenshoAssistant.UI.UiComponents.SubTitle(localization.Format("StockValue.SleepersTitle",stockSleepers.Count));
        ImGui.TextWrapped(localization.Get("StockValue.SleepersHelp"));
        if(stockSleepersUnknown>0){ImGui.TextColored(Warning,localization.Format("StockValue.SleepersUnknown",stockSleepersUnknown));Tip("StockValue.SleepersUnknownTip");}
        if(stockSleepers.Count==0){EmptyState("StockValue.SleepersNone");return;}
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable("StockSleepers",7))return;
        try
        {
            TenshoAssistant.UI.UiComponents.HeadersRow([localization.Get("StockValue.ColumnItem"),localization.Get("StockValue.ColumnQuantity"),localization.Get("StockValue.ColumnWhere"),
                localization.Get("StockValue.ColumnSold"),localization.Get("StockValue.ColumnPerDay"),localization.Get("StockValue.ColumnPerDayGil"),localization.Get("StockValue.ColumnNpc")],
                column=>column is 1 or 3 or 4 or 5 or 6);
            var items=itemCatalog.State.Catalog;
            foreach(var line in stockSleepers)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted((items?.Find(line.ItemId)?.Name ?? $"#{line.ItemId}")+(line.Hq ? " HQ":""));
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(line.ItemId);
                ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(line.Quantity.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextUnformatted(line.Places.Count<=3 ? string.Join(", ",line.Places):string.Join(", ",line.Places.Take(3))+" …");
                if(line.Places.Count>3)TipText(()=>string.Join(", ",line.Places));
                ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(line.Sold.ToString("N0",Numbers));
                ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(line.PerDay.ToString(line.PerDay>=10 ? "N0":"0.#",Numbers));
                ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(line.PerDayGil.ToString("N0",Numbers));
                ImGui.TableNextColumn();
                if(line.NpcPrice>0)
                {
                    TenshoAssistant.UI.UiComponents.NumberCell(line.NpcPrice.ToString("N0",Numbers));
                    if(line.Sold<line.NpcPrice)TipText(localization.Get("HoverUi.NpcOverMarket"));
                }
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void StartStockValue(ulong character,uint world)
    {
        var items=itemCatalog.State.Catalog;
        var ids=StockValuePlaces(character).SelectMany(place=>place.Stacks).Select(stack=>stack.Item.Id.Value).Distinct()
            .Where(id=>items?.Find(id) is { IsMarketable:true }).ToArray();
        stockValueTotal=ids.Length;Volatile.Write(ref stockValueAsked,0);stockValueError=null;
        stockValueCancel?.Dispose();
        var cancel=stockValueCancel=new CancellationTokenSource();
        var quotes=stockValueQuotes=[];
        stockValueTask=Task.Run(async ()=>
        {
            // By lists of 100 (what Universalis answers at once), the market service's own pauses between them.
            for(var start=0;start<ids.Length;start+=Market.MarketService.BatchMaximum)
            {
                cancel.Token.ThrowIfCancellationRequested();
                var batch=ids[start..Math.Min(ids.Length,start+Market.MarketService.BatchMaximum)];
                var refused=await market.FillCacheAsync(batch,character,world,cancel.Token,true).ConfigureAwait(false);
                // X-779: this list's quotes kept for the estimate at once (those already fresh in the cache too).
                foreach(var id in batch)if(market.GetCached(world,id) is { } quote)quotes[id]=quote;
                if(refused is not null)return refused;
                Volatile.Write(ref stockValueAsked,Math.Min(ids.Length,start+batch.Length));
            }
            return (string?)null;
        },cancel.Token);
    }

    private void BuildStockValue(ulong character)
    {
        var items=itemCatalog.State.Catalog;var world=market.State.WorldId;var now=DateTimeOffset.UtcNow;
        bool Marketable(uint id)=>items?.Find(id) is { IsMarketable:true };
        // X-779: the quotes kept by this estimate first; the market cache for the items it did not ask (stopped before them).
        Market.MarketQuote? Quote(uint id)=>stockValueQuotes.GetValueOrDefault(id) ?? market.GetCached(world,id);
        StockPrice? Price(uint id,bool hq)=>StockValue.FromQuote(Quote(id),hq);
        var places=StockValuePlaces(character);
        var lines=places.Select(place=>(place.Character,Line:StockValue.Of(place.Place,place.Stacks,Marketable,Price,now))).ToList();
        stockValuePlaces=[..lines.Where(line=>line.Character==character).Select(line=>line.Line).OrderByDescending(line=>line.Value)];
        stockValueCharacters=[..lines.GroupBy(line=>line.Character).Select(group=>StockValue.Sum(CharacterName(bank.State.Entries,group.Key)
            +(group.Key==character ? " ★":""),group.Select(line=>line.Line))).OrderByDescending(line=>line.Value)];
        // X-207 (part 2): the places of another character named with it.
        var names=new Dictionary<ulong,string>();
        string Where(ulong owner,string place)=>owner==character ? place
            :(names.TryGetValue(owner,out var name) ? name:names[owner]=CharacterName(bank.State.Entries,owner))+" · "+place;
        (stockSleepers,stockSleepersUnknown)=StockValue.Sleepers(places.SelectMany(place=>place.Stacks.Select(stack=>(Where(place.Character,place.Place),stack))),
            Marketable,(id,hq)=>Quote(id)?.Sold(hq),id=>items?.Find(id)?.SellPrice ?? 0,StockSleepersShown);
        stockValueAt=now;
    }

    private void DrawStockValueTable(string id,string firstColumn,IReadOnlyList<StockValueLine> lines)
    {
        if(lines.Count==0)return;
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable(id,4))return;
        try
        {
            TenshoAssistant.UI.UiComponents.HeadersRow([localization.Get(firstColumn),localization.Get("StockValue.ColumnValue"),localization.Get("StockValue.ColumnUnpriced"),localization.Get("StockValue.ColumnOld")],column=>column is 1);
            foreach(var line in lines)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(line.Place);
                ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(line.Value.ToString("N0",Numbers));
                TipText(()=>localization.Format("StockValue.ValueTip",line.Priced,line.NotSold));
                ImGui.TableNextColumn();
                if(line.Unpriced>0){ImGui.TextColored(Warning,localization.Format("StockValue.Unpriced",line.Unpriced));Tip("StockValue.UnpricedTip");}
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(line.Stale>0 && line.Oldest is { } oldest){ImGui.TextColored(Warning,localization.Format("StockValue.Old",line.Stale,oldest.ToLocalTime().ToString("d")));Tip("StockValue.OldTip");}
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }
}
