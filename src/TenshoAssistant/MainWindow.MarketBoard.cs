using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// R-06 step 4 (0.270): the Market page lists every Universalis listing of the item for a world, data center or region, with
// filters (quality, quantity, unit price, world), the cheapest set of whole listings for N units and the recent sales.
// Filtering and the plan are computed only when the board or a filter changes, never every frame. Columns sort on a header
// click (0.271). Data: Market/MarketBoardService.cs (asked by this page only; last known board shown, marked old, if Universalis fails).
internal sealed partial class MainWindow
{
    private MarketBoardService? marketBoard;
    // 0.273: « Market prices » from an item opens the Summary tab, not the last tab used.
    private bool marketFocusSummary;
    internal void ConfigureMarketBoard(MarketBoardService service)=>marketBoard=service;
    // e) (audit 2026-11-02): the scope is saved (Configuration.MarketBoardScope, also in Settings › Interface) and the page opens on
    // it next time; an unknown saved value (hand-edited file) falls back to the data centre, the scope used before e).
    private MarketScope MarketScopeShown=>Enum.IsDefined(configuration.MarketBoardScope) ? configuration.MarketBoardScope:MarketScope.DataCenter;
    private void ChooseMarketScope(MarketScope scope)
    {
        if(configuration.MarketBoardScope==scope)return;
        // X-76 H3 (0.914): the shared save; the previous scope comes back when saving fails (it lasted until the plugin was reloaded).
        SaveSetting("MarketBoardScope",()=>configuration.MarketBoardScope,value=>configuration.MarketBoardScope=value,scope);
    }
    private int marketQuality,marketMinQuantity,marketMaxQuantity,marketMinPrice,marketMaxPrice,marketWanted;
    private string marketWorld="";
    private (MarketBoard? Board,MarketFilter Filter,int Wanted) marketViewKey;
    private MarketOffer[] marketShown=[];
    // X-73.12 (0.868): the board shown, completed by what was seen at the game's board; worked out again when the board, the prices seen or the minute change.
    private (MarketBoard? Board,string Scope,uint Item,long Revision,long Minute,MarketBoard? Shown) marketSeenKey;
    private MarketBoard? BoardWithSeen(MarketBoard? board,string scope,uint item)
    {
        if(SeenPrices is not { } store || !configuration.Discovery.Enabled || !configuration.Discovery.MarketPrices)return board;
        var now=DateTimeOffset.UtcNow;var minute=now.ToUnixTimeSeconds()/60;var revision=store.Revision;
        if(marketSeenKey.Item==item && ReferenceEquals(marketSeenKey.Board,board) && marketSeenKey.Scope==scope && marketSeenKey.Revision==revision && marketSeenKey.Minute==minute)
            return marketSeenKey.Shown;
        var state=market.State;
        var worlds=new Dictionary<uint,string>();
        if(state.WorldNames is { } names)foreach(var (id,name) in names)worlds[id]=name;
        if(state.WorldId!=0)worlds[state.WorldId]=state.WorldName;
        var seen=new List<(string World,Market.SeenMarketEntry Entry)>();
        foreach(var (id,name) in worlds)
            if((MarketScopeShown!=MarketScope.World || name==scope) && store.Get(id,item) is { } entry)seen.Add((name,entry));
        var shown=TenshoAssistant.Market.SeenPrices.WithSeen(board,scope,item,seen,now);
        marketSeenKey=(board,scope,item,revision,minute,shown);
        return shown;
    }
    private MarketPlan? marketPlan;
    // 0.271: column sorting (click a header; again to reverse). Sorted copies are rebuilt only when the data or the order change.
    private (int Column,bool Descending) marketOfferSort=(0,false),marketSaleSort=(0,true);
    private (MarketBoard? Board,MarketFilter Filter,(int,bool) Sort) marketSalesKey;
    private MarketSale[] marketSalesShown=[];
    // I22 (0.669): the filtered sales and their statistics, once per change, not at each frame.
    private MarketSale[] marketSalesFiltered=[];
    private TenshoAssistant.Market.MarketSaleStats? marketSalesStats;

    private static bool ReadSort(ref (int Column,bool Descending) sort)
    {
        var specs=ImGui.TableGetSortSpecs();
        if(specs.IsNull || !specs.SpecsDirty || specs.SpecsCount==0)return false;
        sort=(specs.Specs.ColumnIndex,specs.Specs.SortDirection==ImGuiSortDirection.Descending);
        specs.SpecsDirty=false;
        return true;
    }
    private static IEnumerable<T> Ordered<T,TKey>(IEnumerable<T> rows,Func<T,TKey> key,bool descending)=>descending ? rows.OrderByDescending(key):rows.OrderBy(key);
    private MarketOffer[] SortOffers(IEnumerable<MarketOffer> offers)=>(marketOfferSort.Column switch
    {
        1=>Ordered(offers,offer=>offer.Quantity,marketOfferSort.Descending),
        2=>Ordered(offers,offer=>offer.Cost,marketOfferSort.Descending),
        3=>Ordered(offers,offer=>offer.Hq,marketOfferSort.Descending),
        4=>Ordered(offers,offer=>offer.World,marketOfferSort.Descending),
        5=>Ordered(offers,offer=>offer.Retainer,marketOfferSort.Descending),
        6=>Ordered(offers,offer=>offer.ReviewedAt ?? DateTimeOffset.MinValue,marketOfferSort.Descending),
        _=>Ordered(offers,offer=>offer.UnitPrice,marketOfferSort.Descending),
    }).ToArray();
    private MarketSale[] SortSales(IEnumerable<MarketSale> sales)=>(marketSaleSort.Column switch
    {
        1=>Ordered(sales,sale=>sale.UnitPrice,marketSaleSort.Descending),
        2=>Ordered(sales,sale=>sale.Quantity,marketSaleSort.Descending),
        3=>Ordered(sales,sale=>sale.Hq,marketSaleSort.Descending),
        4=>Ordered(sales,sale=>sale.World,marketSaleSort.Descending),
        _=>Ordered(sales,sale=>sale.At,marketSaleSort.Descending),
    }).ToArray();

    private string? MarketScopeName(MarketScope scope)
    {
        var state=market.State;
        return scope switch
        {
            MarketScope.World=>state.WorldName.Length>0 ? state.WorldName:null,
            MarketScope.DataCenter=>state.DataCenterName.Length>0 ? state.DataCenterName:null,
            _=>state.DataCenterName.Length>0 ? marketBoard?.RegionOf(state.DataCenterName):null,
        };
    }
    private MarketFilter CurrentMarketFilter()=>new(marketQuality switch{1=>false,2=>true,_=>null},marketMinQuantity,marketMaxQuantity,marketMinPrice,marketMaxPrice,marketWorld);

    // Scope choice, then the board of the item for that scope (asked once per item and scope; « Refresh » forces a new read).
    private MarketBoard? DrawMarketBoardHeader()
    {
        if(marketBoard is null)return null;
        foreach(var scope in Enum.GetValues<MarketScope>())
        {
            var name=MarketScopeName(scope);
            ImGui.BeginDisabled(name is null);
            try{if(ImGui.RadioButton(localization.Format("MarketBoard.Scope"+scope,name ?? "…")+$"##Scope{scope}",MarketScopeShown==scope))ChooseMarketScope(scope);}
            finally{ImGui.EndDisabled();}
            if(name is null && scope==MarketScope.Region && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(configuration.CommunityUniversalis ? "MarketBoard.RegionUnknown":"Advanced.SourceOffTip"));
            else Tip("MarketBoard.ScopeTip");
            ImGui.SameLine();
        }
        ImGui.NewLine();
        if(SelectedItem is { IsMarketable:false }){ImGui.TextDisabled(localization.Get("MarketBoard.NotMarketable"));return null;}
        if(MarketScopeName(MarketScopeShown) is not { } scopeName)
        {
            // 0.327: a saved Region whose name is not known yet says why; World and Data centre stay clickable.
            var why=MarketScopeShown==MarketScope.Region && market.State.DataCenterName.Length>0
                ? configuration.CommunityUniversalis ? "MarketBoard.RegionUnknown":"Advanced.SourceOffTip":"MarketUi.World";
            ImGui.TextDisabled(localization.Get(why));return null;
        }
        var item=(uint)itemId;var state=marketBoard.State;
        // 0.324: Universalis turned back on, a board stopped by the switch is asked again once.
        if(state.Scope!=scopeName || state.ItemId!=item || (state.Board is null && !state.Loading && state.Error is null)
            || state.Error==Market.MarketBoardService.SourceOff && configuration.CommunityUniversalis && !state.Loading)marketBoard.Request(scopeName,item);
        state=marketBoard.State;
        ImGui.BeginDisabled(state.Loading);
        try{if(SourceButton(localization.Get("MarketUi.Refresh")+"##Board",configuration.CommunityUniversalis,true))marketBoard.Request(scopeName,item,true);}
        finally{ImGui.EndDisabled();}
        ImGui.SameLine();
        // 0.281: an old board is shown, dated, when Universalis fails or while the pause between requests runs.
        if(state.Loading && state.Board is null)StateLine(UI.UiState.Waiting,localization.Get("MarketUi.Loading"));
        else if(state.Stale && state.Board is { } old)
        {
            StateLine(UI.UiState.Stale,localization.Format(state.Error is null ? "MarketBoard.Waiting":"MarketBoard.StaleCache",old.FetchedAt.ToLocalTime().ToString("g")));
            if(SourceOff(state.Error))ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));
            if(state.Error is { } staleError && developerMode){ImGui.SameLine();ImGui.TextDisabled(staleError);}
        }
        else if(state.Cooldown)StateLine(UI.UiState.Waiting,localization.Get("MarketBoard.Cooldown"));
        else if(state.Error is { } error)DrawDataError(error,"MarketDates.Error");
        else if(state.Board is { } fetched)ImGui.TextDisabled(localization.Format("MarketDates.Fetched",fetched.FetchedAt.ToLocalTime().ToString("g")));
        var shown=state.ItemId==item && state.Scope==scopeName ? state.Board:null;
        // X-73.12 (0.868): while Universalis is still asked, nothing yet; then its board completed by what was seen in game, or what was seen alone when it has none.
        if(shown is null && state.Loading)return null;
        var completed=BoardWithSeen(shown,scopeName,item);
        if(completed is not null && !ReferenceEquals(completed,shown))
        {
            ImGui.TextDisabled(localization.Format("MarketBoard.SeenAdded",completed.Offers.Count(offer=>offer.Seen),Math.Max(0,completed.Sales.Count-(shown?.Sales.Count ?? 0))));
            Tip("MarketBoard.SeenHelp");
        }
        return completed;
    }

    private void DrawMarketFilters(MarketBoard board,bool listings)
    {
        ImGui.SetNextItemWidth(120);
        var qualities=new[]{localization.Get("MarketBoard.QualityAll"),"NQ","HQ"};
        ImGui.Combo(localization.Get("MarketUi.Quality")+"##BoardQuality",ref marketQuality,qualities,qualities.Length);
        Tip("MarketBoard.QualityTip");
        ImGui.SameLine();
        var worlds=board.Offers.Select(offer=>offer.World).Concat(board.Sales.Select(sale=>sale.World)).Distinct().Order().ToArray();
        if(worlds.Length>1)
        {
            ImGui.SetNextItemWidth(150);
            if(ImGui.BeginCombo(localization.Get("MarketBoard.World")+"##BoardWorld",marketWorld.Length==0 ? localization.Get("MarketBoard.AllWorlds"):marketWorld))
            {
                try
                {
                    if(ImGui.Selectable(localization.Get("MarketBoard.AllWorlds"),marketWorld.Length==0))marketWorld="";
                    foreach(var world in worlds)if(ImGui.Selectable(world,marketWorld==world))marketWorld=world;
                }
                finally{ImGui.EndCombo();}
            }
            Tip("MarketBoard.WorldTip");
        }
        else marketWorld="";
        if(!listings)return;
        Bound("MarketBoard.Quantity","Quantity",ref marketMinQuantity,ref marketMaxQuantity,999);
        ImGui.SameLine();
        Bound("MarketBoard.Price","Price",ref marketMinPrice,ref marketMaxPrice,999_999_999);
        void Bound(string key,string id,ref int minimum,ref int maximum,int limit)
        {
            ImGui.TextUnformatted(localization.Get(key));ImGui.SameLine();
            ImGui.SetNextItemWidth(90);ImGui.InputInt("##Min"+id,ref minimum,0);minimum=Math.Clamp(minimum,0,limit);
            Tip("MarketBoard.MinHelp");
            ImGui.SameLine();ImGui.TextUnformatted("–");ImGui.SameLine();
            ImGui.SetNextItemWidth(90);ImGui.InputInt("##Max"+id,ref maximum,0);maximum=Math.Clamp(maximum,0,limit);
            Tip("MarketBoard.MaxHelp");
        }
    }

    private void DrawMarketListings()
    {
        if(DrawMarketBoardHeader() is not { } board)return;
        DrawMarketFilters(board,true);
        ImGui.TextUnformatted(localization.Get("MarketBoard.Wanted"));ImGui.SameLine();
        ImGui.SetNextItemWidth(90);ImGui.InputInt("##Wanted",ref marketWanted,0);marketWanted=Math.Clamp(marketWanted,0,MarketBoardMath.MaximumWanted);
        Tip("MarketBoard.WantedHelp");
        var filter=CurrentMarketFilter();
        if(!ReferenceEquals(marketViewKey.Board,board) || marketViewKey.Filter!=filter || marketViewKey.Wanted!=marketWanted)
        {
            marketViewKey=(board,filter,marketWanted);
            marketShown=SortOffers(board.Offers.Where(filter.Accepts));
            marketPlan=marketWanted>0 ? MarketBoardMath.Cheapest(marketShown,marketWanted):null;
        }
        if(marketWanted>0)
        {
            ImGui.SameLine();
            if(marketPlan is { } plan)
            {
                // 0.271: the gil icon right after the amount, then the details.
                ImGui.TextColored(Accent,plan.Cost.ToString("N0",Numbers));
                DrawGilIcon();
                ImGui.SameLine(0,6);
                ImGui.TextColored(Accent,localization.Format("MarketBoard.PlanCost",plan.Offers.Count,plan.Bought));
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("MarketBoard.PlanHelp",(plan.Cost/(double)plan.Bought).ToString("N0",Numbers)));
            }
            else ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MarketBoard.PlanNone"));
        }
        // 0.567 (I18): from this page too: your retainers are left out, by name, and the date says how old the list is (the game itself may be ahead).
        var ownRetainers=bankBridge.State.KnownRetainers?.Select(known=>known.Name).ToHashSet(StringComparer.Ordinal) ?? [];
        // X-73.12 (0.868): the offers seen in game have no seller name, so your own cannot be told apart: only Universalis' offers count here.
        // X-73.14 (0.870): a seen offer counts when known not to be yours.
        if(Undercut.Price(board.Offers.Where(offer=>(offer.Seen ? offer.Own==false:!ownRetainers.Contains(offer.Retainer)) && offer.UnitPrice is >0 and <=uint.MaxValue).Select(offer=>new UndercutListing((uint)offer.UnitPrice,offer.Hq,0)),new HashSet<ulong>()) is { } lowest)
        {
            CopyButton(localization.Format("MarketBoard.CopyUndercut",lowest.Paste.ToString("N0")),()=>lowest.Paste.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("MarketBoard.CopyUndercutTip",lowest.Lowest.ToString("N0",Numbers),board.FetchedAt.ToLocalTime().ToString("g")));
        }
        ImGui.TextDisabled(localization.Format("MarketBoard.Count",marketShown.Length,board.Offers.Count));
        if(marketShown.Length==0){ImGui.TextDisabled(localization.Get("MarketBoard.Empty"));return;}
        ImGui.TextDisabled(localization.Get("MarketBoard.SortHelp"));
        if(!UI.UiComponents.BeginDataTable("MarketListings",7,sortable:true,scrollHeight:Math.Max(160,ImGui.GetContentRegionAvail().Y)))return;
        try
        {
            var offerColumns=new[]{"UnitPrice","Quantity","Total","Quality","World","Retainer","Reviewed"};
            for(var column=0;column<offerColumns.Length;column++)
                ImGui.TableSetupColumn(localization.Get("MarketBoard.Column"+offerColumns[column]),column==0 ? ImGuiTableColumnFlags.DefaultSort:ImGuiTableColumnFlags.None);
            ImGui.TableSetupScrollFreeze(0,1);ImGui.TableHeadersRow();
            if(ReadSort(ref marketOfferSort))marketShown=SortOffers(marketShown);
            // 0.329: by instance (MarketOffer is a record): two identical listings are not both highlighted when the plan took one.
            var chosen=marketPlan?.Offers.ToHashSet(ReferenceEqualityComparer.Instance) ?? [];
            foreach(var offer in marketShown)
            {
                ImGui.TableNextRow();
                if(chosen.Contains(offer))ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1,ImGui.GetColorU32(new Vector4(Accent.X,Accent.Y,Accent.Z,0.22f)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(offer.UnitPrice.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextUnformatted(offer.Quantity.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextUnformatted(offer.Cost.ToString("N0",Numbers));
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("MarketBoard.TotalHelp",offer.Total.ToString("N0",Numbers),offer.Tax.ToString("N0",Numbers)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(offer.Hq ? "HQ":"NQ");
                ImGui.TableNextColumn();ImGui.TextUnformatted(offer.World);
                ImGui.TableNextColumn();
                if(offer.Seen){ImGui.TextDisabled(localization.Get(offer.Own==true ? "MarketBoard.SeenYours":"MarketBoard.SeenInGame"));Tip("MarketBoard.SeenHelp");}
                else ImGui.TextUnformatted(offer.Retainer);
                ImGui.TableNextColumn();ImGui.TextDisabled(offer.ReviewedAt?.ToLocalTime().ToString("g") ?? "—");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawMarketHistory()
    {
        if(DrawMarketBoardHeader() is not { } board)return;
        DrawMarketFilters(board,false);
        var filter=CurrentMarketFilter();
        if(!ReferenceEquals(marketSalesKey.Board,board) || marketSalesKey.Filter!=filter || marketSalesKey.Sort!=marketSaleSort)
        {
            marketSalesKey=(board,filter,marketSaleSort);
            marketSalesFiltered=board.Sales.Where(filter.Accepts).ToArray();
            marketSalesStats=MarketBoardMath.Stats(marketSalesFiltered);
            marketSalesShown=SortSales(marketSalesFiltered);
        }
        var velocity=filter.Hq switch{true=>board.HqUnitsPerDay,false=>board.NqUnitsPerDay,_=>board.UnitsPerDay};
        ImGui.TextUnformatted(velocity is { } perDay ? localization.Format("MarketBoard.Velocity",perDay.ToString("0.#",Numbers)):localization.Get("MarketBoard.VelocityUnknown"));
        Tip("MarketBoard.VelocityHelp");
        if(marketSalesStats is { } stats)
            ImGui.TextColored(Accent,localization.Format("MarketBoard.Stats",stats.Minimum.ToString("N0",Numbers),stats.Average.ToString("N0",Numbers),stats.Maximum.ToString("N0",Numbers),stats.Sales,stats.Units.ToString("N0",Numbers)));
        else{ImGui.TextDisabled(localization.Get("MarketBoard.NoSales"));return;}
        ImGui.TextDisabled(localization.Get("MarketBoard.SortHelp"));
        if(!UI.UiComponents.BeginDataTable("MarketSales",5,sortable:true,scrollHeight:Math.Max(160,ImGui.GetContentRegionAvail().Y)))return;
        try
        {
            var saleColumns=new[]{"Date","UnitPrice","Quantity","Quality","World"};
            for(var column=0;column<saleColumns.Length;column++)
                ImGui.TableSetupColumn(localization.Get("MarketBoard.Column"+saleColumns[column]),column==0 ? ImGuiTableColumnFlags.DefaultSort|ImGuiTableColumnFlags.PreferSortDescending:ImGuiTableColumnFlags.None);
            ImGui.TableSetupScrollFreeze(0,1);ImGui.TableHeadersRow();
            if(ReadSort(ref marketSaleSort))marketSalesShown=SortSales(marketSalesShown);
            foreach(var sale in marketSalesShown)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextDisabled(sale.At.ToLocalTime().ToString("g"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(sale.UnitPrice.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextUnformatted(sale.Quantity.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextUnformatted(sale.Hq ? "HQ":"NQ");
                ImGui.TableNextColumn();ImGui.TextUnformatted(sale.World);
            }
        }
        finally{ImGui.EndTable();}
    }
}
