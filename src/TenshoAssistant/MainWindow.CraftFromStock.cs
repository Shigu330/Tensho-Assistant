using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// C21 (0.592): Crafting › What can I craft?: the recipes the bags and the saved Bank can make now, worked out on a click off the frame
// (every recipe of the catalogue), filtered by job and level here. Nothing is read again until the next click.
// 0.696 (user request): with sub-recipes too, on by default: from the ore through every step.
internal sealed partial class MainWindow
{
    private CancellationTokenSource? fromStockCancel;
    private Task<CraftFromStockResult?>? fromStockTask;
    private InventorySnapshot? fromStockLive;
    private BankState? fromStockBank;
    private string fromStockJob="";
    private int fromStockMin=1,fromStockMax=100;
    private bool fromStockChained=true;
    private (CraftFromStockResult Source,string Job,int Min,int Max,int Sort,int Prices,bool Loading,string Query,int Page,object? Scrips,CraftFromStockRow[] Rows,CraftFromStockLine[] Shown,int Pages)? fromStockView;
    // 0.709 (user request): a list by pages. On the market sort, prices come page by page, or all on a click; the whole list is ranked once they are all there.
    private const int FromStockPageSize=25;
    private int fromStockPage;
    private string fromStockQuery="";
    private bool fromStockLoadAll,fromStockMarketComplete;
    // W-06 (0.707, user request): 0 level, 1 market, 2 NPC sale, 3 scrips.
    private int fromStockSort;
    private bool fromStockPricesOn=true;
    private readonly TenshoAssistant.Market.PreparationPriceSession fromStockPrices=new();
    private readonly HashSet<uint> fromStockAsked=[];
    private Dictionary<uint,string> fromStockValues=[];

    private decimal? FromStockMarketUnit(CraftFromStockRow row,DateTimeOffset now)
    {
        var world=market.State.WorldId;
        if(world==0 || market.GetCached(world,row.Recipe.ResultItemId) is not { } quote || !market.IsFresh(quote,now))return null;
        return quote.Sold(itemCatalog.State.Catalog?.Find(row.Recipe.ResultItemId)?.CanBeHq==true)?.Price;
    }
    private string FromStockRowText(CraftFromStockRow row)=>
        localization.Format(row.Chained ? "CraftStock.RowChained":row.LowerBound ? "CraftStock.RowAtLeast":"CraftStock.Row",row.Count.ToString("N0",Numbers),row.Recipe.Level);
    private void DrawFromStockJob(CraftFromStockRow row)
    {
        var recipe=row.Recipe;
        ImGui.PushID((int)recipe.RecipeId);
        try
        {
            // CraftType 0 to 7 are the crafters in ClassJob order, 8 to 15; their icons are 62100 + job, as the gathering panel uses.
            if(recipe.CraftTypeId<8)DrawGameIcon(62108+recipe.CraftTypeId,new System.Numerics.Vector2(ImGui.GetTextLineHeight()+4));
            else ImGui.TextUnformatted(recipe.CraftType);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("CraftStock.JobIconTip",recipe.CraftType,FromStockRowText(row)));
            if(ImGui.IsItemClicked())OpenCraftSimulation(recipe);
        }
        finally{ImGui.PopID();}
    }
    private decimal? FromStockPerDay(CraftFromStockRow row)
    {
        var world=market.State.WorldId;
        return world==0 ? null:market.GetCached(world,row.Recipe.ResultItemId)?.Sold(itemCatalog.State.Catalog?.Find(row.Recipe.ResultItemId)?.CanBeHq==true)?.PerDay;
    }
    private int FromStockDays=>Math.Clamp(configuration.CraftStockSaleDays,1,30);
    // 0.725 (user request): the market sort counts what can sell in the chosen days, not all that could be made.
    private decimal? FromStockSaleValue(CraftFromStockRow row,DateTimeOffset now)=>
        FromStockMarketUnit(row,now) is { } unit && CraftFromStock.Sellable(row,FromStockPerDay(row),FromStockDays) is { } pieces ? pieces*unit:null;
    private string FromStockMarketText(CraftFromStockRow row,decimal unit,Func<decimal,string> gil)
    {
        var pieces=row.Count*Math.Max(1,row.Recipe.ResultAmount);
        if(FromStockPerDay(row) is not { } perDay)return localization.Format("CraftStock.ValueMarketUnknown",gil(unit));
        if(perDay==0)return localization.Format("CraftStock.ValueMarketNone",pieces,gil(unit));
        var sold=CraftFromStock.Sellable(row,perDay,FromStockDays) ?? 0;
        return localization.Format("CraftStock.ValueMarket",gil(sold*unit),FromStockDays,sold,pieces,gil(unit),perDay.ToString("0.##",Numbers));
    }

    // Scrips at the high tier; only rows of one kind of scrip compare, so each kind stays together, by its group's name.
    private TenshoAssistant.Gathering.CollectableRule? FromStockScrips(CraftFromStockRow row)=>
        Collectables?.Catalog?.For(row.Recipe.ResultItemId).Where(rule=>rule.Scrips && rule.RewardHigh>0).OrderByDescending(rule=>rule.RewardHigh).FirstOrDefault();

    private CraftFromStockRow[] FromStockRanked(CraftFromStockRow[] rows)
    {
        var now=DateTimeOffset.UtcNow;var items=itemCatalog.State.Catalog;var culture=Numbers;
        fromStockValues=[];
        string Gil(decimal value)=>value.ToString("N0",culture);
        switch(fromStockSort)
        {
            case 1:
                foreach(var row in rows)
                {
                    if(FromStockMarketUnit(row,now) is { } unit)fromStockValues[row.Recipe.RecipeId]=FromStockMarketText(row,unit,Gil);
                    else if(items?.Find(row.Recipe.ResultItemId) is { IsMarketable:false })fromStockValues[row.Recipe.RecipeId]=localization.Get("CraftStock.NotMarket");
                    else if(fromStockAsked.Contains(row.Recipe.ResultItemId))fromStockValues[row.Recipe.RecipeId]=localization.Get("CurrencyValue.NoOffer");
                }
                fromStockMarketComplete=rows.All(row=>FromStockMarketUnit(row,now) is not null || items?.Find(row.Recipe.ResultItemId) is not { IsMarketable:true } || fromStockAsked.Contains(row.Recipe.ResultItemId));
                return fromStockMarketComplete ? CraftFromStock.Ranked(rows,row=>FromStockSaleValue(row,now)):rows;
            case 2:
                foreach(var row in rows)
                    if(items?.Find(row.Recipe.ResultItemId) is { SellPrice:>0 } sold)fromStockValues[row.Recipe.RecipeId]=localization.Format("CraftStock.ValueNpc",Gil(CraftFromStock.Worth(row,sold.SellPrice)),Gil(sold.SellPrice));
                return CraftFromStock.Ranked(rows,row=>items?.Find(row.Recipe.ResultItemId) is { SellPrice:>0 } sold ? CraftFromStock.Worth(row,sold.SellPrice):null);
            case 3:
                foreach(var row in rows)
                    if(FromStockScrips(row) is { } rule)fromStockValues[row.Recipe.RecipeId]=localization.Format("CraftStock.ValueScrips",Gil(CraftFromStock.Worth(row,rule.RewardHigh)),rule.Group);
                return CraftFromStock.Ranked(rows.OrderBy(row=>FromStockScrips(row)?.ScripKind ?? int.MaxValue),row=>null).GroupBy(row=>FromStockScrips(row)?.ScripKind ?? int.MaxValue)
                    .SelectMany(kind=>CraftFromStock.Ranked(kind,row=>FromStockScrips(row) is { } rule ? CraftFromStock.Worth(row,rule.RewardHigh):null)).ToArray();
            default:return rows;
        }
    }

    // Market prices of what is listed, 32 at a time, from the choice of this sort on; the box stops them.
    private void DrawFromStockPrices(CraftFromStockRow[] all,CraftFromStockRow[] page)
    {
        if(fromStockSort!=1)return;
        // X-76 step 3 (0.954): its own label: this page's box, not saved, not the currency value page's setting.
        ImGui.Checkbox(localization.Get("CraftStock.Market")+"###FromStockMarket",ref fromStockPricesOn);
        Tip("CraftStock.MarketTip");
        ImGui.SameLine();
        SaleDaysSetting("FromStockDays","CraftStock.SaleDaysTip");
        if(!fromStockPricesOn){if(fromStockPrices.State.Loading)fromStockPrices.Cancel();return;}
        var prices=fromStockPrices.State;var world=market.State.WorldId;var character=bankBridge.State.CharacterId;var now=DateTimeOffset.UtcNow;var items=itemCatalog.State.Catalog;
        var marketable=all.Where(row=>items?.Find(row.Recipe.ResultItemId) is { IsMarketable:true }).Select(row=>row.Recipe.ResultItemId).Distinct().Count();
        var left=all.Count(row=>FromStockMarketUnit(row,now) is null && items?.Find(row.Recipe.ResultItemId) is { IsMarketable:true } && !fromStockAsked.Contains(row.Recipe.ResultItemId));
        if(left>0 && !prices.Loading && !fromStockLoadAll)
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Format("CraftStock.LoadAll",left)))fromStockLoadAll=true;
            Tip("CraftStock.LoadAllTip");
        }
        else if(left==0 && !prices.Loading){ImGui.SameLine();ImGui.TextDisabled(localization.Get("CraftStock.Complete"));}
        var missing=(fromStockLoadAll ? all:page).Where(row=>FromStockMarketUnit(row,now) is null && items?.Find(row.Recipe.ResultItemId) is { IsMarketable:true } && !fromStockAsked.Contains(row.Recipe.ResultItemId)).Select(row=>row.Recipe.ResultItemId).Distinct().ToArray();
        if(prices.Loading){ImGui.SameLine();ImGui.TextDisabled(localization.Format("CurrencyValue.Fetching",fromStockAsked.Count,marketable));return;}
        if(prices.Error is { } failed)
        {
            ImGui.SameLine();ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CurrencyValue.FetchFailed"));
            if(developerMode)ImGui.TextDisabled(failed);
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("CurrencyValue.Retry")+"###FromStockRetry")){fromStockAsked.Clear();fromStockPrices.Cancel();}
            Tip("CurrencyValue.FetchTip");
            return;
        }
        if(world==0 || character==0){ImGui.SameLine();ImGui.TextDisabled(localization.Get("CurrencyValue.NoWorld"));return;}
        if(!SourceOn(SourceUse.UniversalisToolPrices)){ImGui.SameLine();ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));return;}
        if(missing.Length==0){fromStockLoadAll=false;return;}
        var batch=missing.Take(TenshoAssistant.Market.MarketService.BatchMaximum).ToArray();
        fromStockAsked.UnionWith(batch);
        try{_=fromStockPrices.StartListAsync(market,character,world,batch,true);}
        catch(Exception error){fromStockPricesOn=false;log.Warning(error,"Craftable prices could not be started.");}
    }

    private void StartCraftFromStock(RecipeCatalog catalog)
    {
        fromStockCancel?.Cancel();fromStockCancel?.Dispose();
        var cancel=fromStockCancel=new CancellationTokenSource();var token=cancel.Token;
        var character=bankBridge.State.CharacterId;var live=inventory.Current;var saved=bank.State;
        fromStockLive=live;fromStockBank=saved;
        bool Crystal(uint id)=>ClassifyCrystal(id) is not null;
        var chained=fromStockChained;
        // Worked out here, on the frame: the worn job and the levels are read once, not from the worker.
        var preferred=new Dictionary<uint,RecipeDefinition>();
        if(chained)foreach(var group in catalog.All.GroupBy(recipe=>recipe.ResultItemId))preferred[group.Key]=PreferredRecipe(group.ToArray());
        fromStockTask=Task.Run(()=>
        {
            try{return (CraftFromStockResult?)CraftFromStock.Find(catalog.All,id=>ItemHoldings.Build(id,character,live,saved.Entries,includeCrystals:true,crystal:Crystal(id)),token,chained ? id=>preferred.GetValueOrDefault(id):null);}
            catch(OperationCanceledException){return null;}
            catch(Exception error){log.Warning(error,"The recipes the stocks can make could not be worked out.");return null;}
        },token);
    }

    private void DrawCraftFromStock()
    {
        ImGui.TextWrapped(localization.Get("CraftStock.Help"));
        if(recipeCatalog.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("RecipeUi.Loading"));return;}
        if(ImGui.Button(localization.Get("CraftStock.Search")))StartCraftFromStock(catalog);
        ImGui.SameLine();
        if(ImGui.Checkbox(localization.Get("CraftStock.Chained"),ref fromStockChained) && fromStockTask is not null)StartCraftFromStock(catalog);
        Tip("CraftStock.ChainedTip");
        if(fromStockTask is not { } task)return;
        if(!task.IsCompleted){ImGui.SameLine();ImGui.TextDisabled(localization.Get("CraftStock.Searching"));return;}
        if(!task.IsCompletedSuccessfully || task.Result is not { } result){ImGui.TextWrapped(localization.Get("CraftStock.Failed"));return;}
        if(!ReferenceEquals(fromStockLive,inventory.Current) || !ReferenceEquals(fromStockBank,bank.State)){ImGui.SameLine();ImGui.TextColored(Accent,localization.Get("CraftStock.Stale"));}
        ImGui.SetNextItemWidth(170);
        if(ImGui.BeginCombo(localization.Get("CraftStock.Job")+"###CraftStockJob",fromStockJob.Length>0 ? fromStockJob:localization.Get("RecipeUi.AllJobs")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RecipeUi.AllJobs"),fromStockJob.Length==0))fromStockJob="";
                foreach(var job in result.Rows.Select(row=>row.Recipe.CraftType).Distinct().Order(StringComparer.CurrentCulture))
                    if(ImGui.Selectable(job,job==fromStockJob))fromStockJob=job;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("CraftStock.JobIconTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("CraftStock.MinLevel")+"###CraftStockMin",ref fromStockMin,0);Tip("CraftStock.LevelTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("CraftStock.MaxLevel")+"###CraftStockMax",ref fromStockMax,0);Tip("CraftStock.LevelTip");
        fromStockMin=Math.Clamp(fromStockMin,1,200);fromStockMax=Math.Clamp(fromStockMax,1,200);
        ImGui.SetNextItemWidth(230);
        string[] sorts=[localization.Get("CraftStock.SortLevel"),localization.Get("CraftStock.SortMarket"),localization.Get("CraftStock.SortNpc"),localization.Get("CraftStock.SortScrips")];
        ImGui.Combo(localization.Get("CraftStock.Sort")+"###CraftStockSort",ref fromStockSort,sorts,sorts.Length);
        Tip("CraftStock.SortTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###CraftStockName",localization.Get("CraftStock.NameSearch"),ref fromStockQuery,100);
        Tip("CraftStock.NameSearchTip");
        var priceState=fromStockPrices.State;
        // 0.712 (user report): the collectables' sheets load on first asking: the list is worked out again when they arrive, or no scrip would ever show.
        var scripCatalog=fromStockSort==3 ? Collectables?.Catalog:null;
        if(fromStockSort==3 && scripCatalog is null)ImGui.TextDisabled(localization.Get(Collectables?.CatalogFailed==true ? "CraftStock.ScripsFailed":"CraftStock.ScripsLoading"));
        if(fromStockView is not { } known || !ReferenceEquals(known.Source,result) || known.Job!=fromStockJob || known.Min!=fromStockMin || known.Max!=fromStockMax || known.Sort!=fromStockSort || known.Prices!=priceState.Completed || known.Loading!=priceState.Loading || known.Query!=fromStockQuery || known.Page!=fromStockPage || !ReferenceEquals(known.Scrips,scripCatalog))
        {
            if(fromStockView is { } before && (before.Job!=fromStockJob || before.Min!=fromStockMin || before.Max!=fromStockMax || before.Sort!=fromStockSort || before.Query!=fromStockQuery || !ReferenceEquals(before.Source,result)))fromStockPage=0;
            var names=itemCatalog.State.Catalog;
            var filtered=CraftFromStock.Filter(result.Rows,fromStockJob,fromStockMin,fromStockMax).Where(row=>fromStockQuery.Length==0 || names?.Find(row.Recipe.ResultItemId)?.Name.Contains(fromStockQuery,StringComparison.CurrentCultureIgnoreCase)==true).ToArray();
            var ranked=FromStockRanked(filtered);
            var (shownRows,shownPage,pages)=CraftFromStock.Paged(CraftFromStock.Lines(ranked),fromStockPage,FromStockPageSize);
            fromStockPage=shownPage;
            // Until every price is there, only the page can be ranked: the whole list would move under the reader.
            if(fromStockSort==1 && !fromStockMarketComplete)
            {
                var now=DateTimeOffset.UtcNow;
                shownRows=CraftFromStock.Lines(CraftFromStock.Ranked(shownRows.SelectMany(line=>line.All),row=>FromStockSaleValue(row,now)));
            }
            fromStockView=known=(result,fromStockJob,fromStockMin,fromStockMax,fromStockSort,priceState.Completed,priceState.Loading,fromStockQuery,fromStockPage,scripCatalog,ranked,shownRows,pages);
        }
        var rows=known.Shown;
        DrawFromStockPrices(known.Rows,rows.SelectMany(line=>line.All).ToArray());
        if(fromStockSort!=0)ImGui.TextDisabled(localization.Get("CraftStock.ValueHelp"));
        ImGui.TextColored(Accent,localization.Format("CraftStock.Count",known.Rows.Length.ToString("N0",Numbers),result.Rows.Count.ToString("N0",Numbers)));
        ImGui.SameLine();
        ImGui.BeginDisabled(fromStockPage<=0);
        try{if(ImGui.SmallButton("<###CraftStockPrevious"))fromStockPage--;}
        finally{ImGui.EndDisabled();}
        Tip("CraftStock.PreviousTip");
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format("CraftStock.Page",fromStockPage+1,known.Pages));
        ImGui.SameLine();
        ImGui.BeginDisabled(fromStockPage>=known.Pages-1);
        try{if(ImGui.SmallButton(">###CraftStockNext"))fromStockPage++;}
        finally{ImGui.EndDisabled();}
        Tip("CraftStock.NextTip");
        if(result.Undecided>0)ImGui.TextDisabled(localization.Format("CraftStock.Undecided",result.Undecided.ToString("N0",Numbers)));
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("CraftStock.Empty"));return;}
        DrawChainList("CraftStockList",rows.Length,index=>
        {
            var line=rows[index];
            var row=line.Lead;
            ImGui.PushID((int)row.Recipe.RecipeId);
            try
            {
                DrawChainItem(row.Recipe.ResultItemId);
                // 0.739 (user request): one icon per job that can make it, its name, level and crafts on hover; a click simulates that recipe.
                foreach(var recipe in line.All){ImGui.SameLine();DrawFromStockJob(recipe);}
                ImGui.SameLine();
                ImGui.TextDisabled(FromStockRowText(row));
                if(row.Chained)Tip("CraftStock.RowChainedTip");
                if(fromStockValues.TryGetValue(row.Recipe.RecipeId,out var worth)){ImGui.SameLine();ImGui.TextColored(Success,worth);}
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("ChainUi.Simulate")))OpenCraftSimulation(row.Recipe);
                Tip("ChainUi.SimulateTip");
            }
            finally{ImGui.PopID();}
        },Math.Max(260,ImGui.GetContentRegionAvail().Y));
    }
}
