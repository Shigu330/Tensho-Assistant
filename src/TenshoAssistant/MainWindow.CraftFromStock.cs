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
    private (CraftFromStockResult Source,string Job,int Min,int Max,int Sort,int Prices,bool Loading,string Query,int Page,CraftFromStockRow[] Rows,CraftFromStockRow[] Shown,int Pages)? fromStockView;
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
        var canHq=itemCatalog.State.Catalog?.Find(row.Recipe.ResultItemId)?.CanBeHq==true;
        return canHq ? quote.Hq.Minimum ?? quote.Nq.Minimum:quote.Nq.Minimum;
    }

    // Scrips at the high tier; only rows of one kind of scrip compare, so each kind stays together, by its group's name.
    private TenshoAssistant.Gathering.CollectableRule? FromStockScrips(CraftFromStockRow row)=>
        Collectables?.Catalog?.For(row.Recipe.ResultItemId).Where(rule=>rule.Scrips && rule.RewardHigh>0).OrderByDescending(rule=>rule.RewardHigh).FirstOrDefault();

    private CraftFromStockRow[] FromStockRanked(CraftFromStockRow[] rows)
    {
        var now=DateTimeOffset.UtcNow;var items=itemCatalog.State.Catalog;var culture=System.Globalization.CultureInfo.CurrentCulture;
        fromStockValues=[];
        string Gil(decimal value)=>value.ToString("N0",culture);
        switch(fromStockSort)
        {
            case 1:
                foreach(var row in rows)
                {
                    if(FromStockMarketUnit(row,now) is { } unit)fromStockValues[row.Recipe.RecipeId]=localization.Format("CraftStock.ValueMarket",Gil(CraftFromStock.Worth(row,unit)),Gil(unit));
                    else if(items?.Find(row.Recipe.ResultItemId) is { IsMarketable:false })fromStockValues[row.Recipe.RecipeId]=localization.Get("CraftStock.NotMarket");
                    else if(fromStockAsked.Contains(row.Recipe.ResultItemId))fromStockValues[row.Recipe.RecipeId]=localization.Get("CurrencyValue.NoOffer");
                }
                fromStockMarketComplete=rows.All(row=>FromStockMarketUnit(row,now) is not null || items?.Find(row.Recipe.ResultItemId) is not { IsMarketable:true } || fromStockAsked.Contains(row.Recipe.ResultItemId));
                return fromStockMarketComplete ? CraftFromStock.Ranked(rows,row=>FromStockMarketUnit(row,now) is { } unit ? CraftFromStock.Worth(row,unit):null):rows;
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
        ImGui.Checkbox(localization.Get("CurrencyValue.Market")+"###FromStockMarket",ref fromStockPricesOn);
        Tip("CraftStock.MarketTip");
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
        if(!configuration.CommunityUniversalis){ImGui.SameLine();ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));return;}
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
        Tip("CraftStock.JobTip");
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
        if(fromStockView is not { } known || !ReferenceEquals(known.Source,result) || known.Job!=fromStockJob || known.Min!=fromStockMin || known.Max!=fromStockMax || known.Sort!=fromStockSort || known.Prices!=priceState.Completed || known.Loading!=priceState.Loading || known.Query!=fromStockQuery || known.Page!=fromStockPage)
        {
            if(fromStockView is { } before && (before.Job!=fromStockJob || before.Min!=fromStockMin || before.Max!=fromStockMax || before.Sort!=fromStockSort || before.Query!=fromStockQuery || !ReferenceEquals(before.Source,result)))fromStockPage=0;
            var names=itemCatalog.State.Catalog;
            var filtered=CraftFromStock.Filter(result.Rows,fromStockJob,fromStockMin,fromStockMax).Where(row=>fromStockQuery.Length==0 || names?.Find(row.Recipe.ResultItemId)?.Name.Contains(fromStockQuery,StringComparison.CurrentCultureIgnoreCase)==true).ToArray();
            var ranked=FromStockRanked(filtered);
            var (shownRows,shownPage,pages)=CraftFromStock.Paged(ranked,fromStockPage,FromStockPageSize);
            fromStockPage=shownPage;
            // Until every price is there, only the page can be ranked: the whole list would move under the reader.
            if(fromStockSort==1 && !fromStockMarketComplete)
            {
                var now=DateTimeOffset.UtcNow;
                shownRows=CraftFromStock.Ranked(shownRows,row=>FromStockMarketUnit(row,now) is { } unit ? CraftFromStock.Worth(row,unit):null);
            }
            fromStockView=known=(result,fromStockJob,fromStockMin,fromStockMax,fromStockSort,priceState.Completed,priceState.Loading,fromStockQuery,fromStockPage,ranked,shownRows,pages);
        }
        var rows=known.Shown;
        DrawFromStockPrices(known.Rows,rows);
        if(fromStockSort!=0)ImGui.TextDisabled(localization.Get("CraftStock.ValueHelp"));
        ImGui.TextColored(Accent,localization.Format("CraftStock.Count",known.Rows.Length.ToString("N0"),result.Rows.Count.ToString("N0")));
        ImGui.SameLine();
        ImGui.BeginDisabled(fromStockPage<=0);
        try{if(ImGui.SmallButton("<###CraftStockPrevious"))fromStockPage--;}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("CraftStock.PreviousTip"));
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format("CraftStock.Page",fromStockPage+1,known.Pages));
        ImGui.SameLine();
        ImGui.BeginDisabled(fromStockPage>=known.Pages-1);
        try{if(ImGui.SmallButton(">###CraftStockNext"))fromStockPage++;}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("CraftStock.NextTip"));
        if(result.Undecided>0)ImGui.TextDisabled(localization.Format("CraftStock.Undecided",result.Undecided.ToString("N0")));
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("CraftStock.Empty"));return;}
        DrawChainList("CraftStockList",rows.Length,index=>
        {
            var row=rows[index];
            ImGui.PushID((int)row.Recipe.RecipeId);
            try
            {
                DrawChainItem(row.Recipe.ResultItemId);
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format(row.Chained ? "CraftStock.RowChained":row.LowerBound ? "CraftStock.RowAtLeast":"CraftStock.Row",row.Count.ToString("N0"),row.Recipe.CraftType,row.Recipe.Level));
                if(row.Chained && ImGui.IsItemHovered())WrappedTooltip(localization.Get("CraftStock.RowChainedTip"));
                if(fromStockValues.TryGetValue(row.Recipe.RecipeId,out var worth)){ImGui.SameLine();ImGui.TextColored(Success,worth);}
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("ChainUi.Simulate")))OpenCraftSimulation(row.Recipe);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ChainUi.SimulateTip"));
            }
            finally{ImGui.PopID();}
        },Math.Max(260,ImGui.GetContentRegionAvail().Y));
    }
}
