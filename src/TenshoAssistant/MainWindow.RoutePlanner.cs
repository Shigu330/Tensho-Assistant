using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// R-07 (0.309, user request: « best routes for submarines … mostly loot »): best submarine routes for a vessel's rank, speed and
// range on one map, ranked by expected loot value (community loot averages × NPC sell price), value per day or experience.
// Computed on a worker when an input changes; the previous search is cancelled; the 25 best are listed. Loot averages:
// FFXIVGachaSpreadsheet (MIT, embedded); formulas understood from SubmarineTracker (MIT), original code (FreeCompany/VoyagePlanner.cs).
// 0.315: « unlocked sectors only » filter (read in the chosen submarine's company workshop), « planned build » button, and a
// « Build » button per route that opens it in the Builds tab.
// X-604 (0.1706): the submarine part of the one route planner of both kinds (MainWindow.Planner.cs: the kind, the vessel, rank, speed, range).
// 0.335 (DEC-023, point 2): value from the NPC price or the best of NPC and market (Universalis lowest NQ of the current world, fetched
// on a click, 32 items at most), and from all records or those of one surveillance band (the source splits its records by band).
internal sealed partial class MainWindow
{
    internal Func<SubmarineCatalog?>? SubmarineCatalog { get; set; }
    // 0.331 (audit): the route catalog could not be built (VoyageWatcher.CatalogFailed): « unavailable » instead of « loading ».
    internal Func<bool>? SubmarineCatalogFailed { get; set; }
    private uint plannerMap;
    private RouteGoal plannerGoal=RouteGoal.ValuePerVoyage;
    // X-604 (0.1706): the search on a worker, one per set of inputs (FreeCompany/PlannerSearch.cs, as the airships').
    private readonly PlannerSearch<IReadOnlyList<PlannedRoute>> submarineSearch=new();
    private IReadOnlyList<PlannedRoute> plannerRoutes=[];
    private bool plannerMarket;
    private SurveillanceBand plannerBand=SurveillanceBand.All;
    private readonly Market.PreparationPriceSession routePrices=new();
    // Unit values the search was made with (copied on the UI thread), items whose market price is missing, sectors without a record
    // in the chosen band (valued from all records), market prices used; and the missing items of the listed routes, most expected first.
    private sealed record RouteValues(IReadOnlyDictionary<uint,double> Unit,IReadOnlySet<uint> Missing,IReadOnlySet<uint> BandFallback,
        IReadOnlyDictionary<uint,decimal> Market,uint World,IReadOnlySet<uint> NoListing);
    // 0.337 (review): quotes read for the routes, kept here so the shared 128-entry price cache (hover, other pages) cannot evict them
    // before the next search; an item fetched with no NQ listing is « no offer », not « missing », so it is not fetched again and again.
    private readonly Dictionary<(uint World,uint Item),Market.MarketQuote> routeQuotes=[];
    private int routeQuotesCaptured=-1;
    private IReadOnlyList<uint>? routeQuotesList;
    private bool routeQuotesLoading;
    private RouteValues? plannerValues;
    private uint[] plannerWanted=[];
    private int[] plannerMissingCounts=[];
    // X-200 (0.1703): the sector aimed at (0: none), for the way to unlock it.
    private uint plannerUnlockTarget;

    private void DrawSubmarineRoutes()
    {
        var catalog=SubmarineCatalog?.Invoke();
        // X-604 (0.1706): the catalogue is read in the game only (the desktop application says so, LoadingText).
        if(catalog is null){ImGui.TextDisabled(SubmarineCatalogFailed?.Invoke()==true ? localization.Get("RoutePlanner.Unavailable"):LoadingText("RoutePlanner.Loading"));return;}
        ImGui.TextWrapped(localization.Get("RoutePlanner.Help"));
        // X-604 (0.1706): the vessel, its planned build, rank, speed and range: the planner's common part (MainWindow.Planner.cs); a submarine
        // chosen also shows the map of its current route.
        var chosen=DrawPlannerVessel(VesselKind.Submarine,vessel=>{if(vessel.Points.Count>0 && catalog.Sectors.TryGetValue(vessel.Points[0],out var first))plannerMap=first.Map;});
        var inputs=submarineInputs;
        if(plannerMap==0 && catalog.Maps.Count>0)plannerMap=catalog.Maps[0].Id;
        ImGui.SameLine();ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Map")+"##PlannerMap",catalog.Maps.FirstOrDefault(map=>map.Id==plannerMap).Name ?? ""))
        {
            try{foreach(var (id,name) in catalog.Maps)if(ImGui.Selectable($"{name}##PlannerMap{id}",id==plannerMap))plannerMap=id;}
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.MapTip");
        DrawPlannerStats(VesselKind.Submarine);
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Goal")+"##PlannerGoal",localization.Get("RoutePlanner.Goal"+plannerGoal)))
        {
            try{foreach(var goal in Enum.GetValues<RouteGoal>())if(ImGui.Selectable(localization.Get("RoutePlanner.Goal"+goal),goal==plannerGoal))plannerGoal=goal;}
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.GoalTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Price")+"##PlannerPrice",localization.Get(plannerMarket ? "RoutePlanner.PriceMarket":"RoutePlanner.PriceNpc")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RoutePlanner.PriceNpc"),!plannerMarket))plannerMarket=false;
                if(ImGui.Selectable(localization.Get("RoutePlanner.PriceMarket"),plannerMarket))plannerMarket=true;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.PriceTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(180);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Band")+"##PlannerBand",localization.Get("RoutePlanner.Band"+plannerBand)))
        {
            try{foreach(var band in Enum.GetValues<SurveillanceBand>())if(ImGui.Selectable(localization.Get("RoutePlanner.Band"+band),band==plannerBand))plannerBand=band;}
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.BandTip");
        BoolSetting("RoutePlanner.UnlockedOnly","RoutePlanner.UnlockedOnlyTip",()=>configuration.RouteUnlockedOnly,value=>configuration.RouteUnlockedOnly=value,c=>c.RouteUnlockedOnly);
        // 0.315: sectors read as unlocked in the selected submarine's company workshop (manual: the only company read); not read = no filter.
        ulong? house=chosen?.HouseId;
        var unlockedList=Voyages.UnlockedFor(configuration.SubmarineUnlocked,house);
        var useUnlocked=configuration.RouteUnlockedOnly && unlockedList is not null;
        // 0.331 (audit): manual input with several companies read: the sectors were read, a submarine (its company) is what is missing.
        if(configuration.RouteUnlockedOnly && unlockedList is null)
            ImGui.TextDisabled(localization.Get(house is null && configuration.SubmarineUnlocked is { Count:>1 } ? "RoutePlanner.UnlockedChoose":"RoutePlanner.UnlockedUnknown"));
        DrawSubmarineUnlockTarget(catalog,unlockedList);

        // 0.335: market prices of the listed routes' loot, fetched on a click (32 items per click), for the current character and world.
        var world=market.State;var character=bankBridge.State.CharacterId;var prices=routePrices.State;
        if(prices.Context is { } priced && (priced.WorldId!=world.WorldId || priced.CharacterId!=character)){routePrices.Cancel();prices=routePrices.State;}
        // A list is told apart by its own item list (new per click), so one that ended between two frames is still seen; once it has
        // ended, the search runs again with what it fetched (0.337 fix check).
        if(prices.Context is { } fetching
            && (!ReferenceEquals(prices.Items,routeQuotesList) || prices.Completed!=routeQuotesCaptured || prices.Loading!=routeQuotesLoading))
        {
            foreach(var id in prices.Items.Take(prices.Completed))
                if(market.GetCached(fetching.WorldId,id) is { } quote)routeQuotes[(fetching.WorldId,id)]=quote;
            routeQuotesList=prices.Items;routeQuotesCaptured=prices.Completed;routeQuotesLoading=prices.Loading;
            if(!prices.Loading)submarineSearch.Again();
        }
        if(plannerMarket)DrawRoutePriceFetch(world,character,prices);

        // New search when an input changed or a price list ended (submarineSearch.Again above); the running one is cancelled.
        var key=$"{plannerMap}|{inputs.Rank}|{inputs.Speed}|{inputs.Range}|{plannerGoal}|{useUnlocked}|{house}|{unlockedList?.Count}|{plannerMarket}|{plannerBand}|{world.WorldId}";
        if(submarineSearch.Needs(key))
        {
            var (map,rank,speed,range,goal,band)=(plannerMap,inputs.Rank,inputs.Speed,inputs.Range,plannerGoal,plannerBand);
            var unlocked=unlockedList?.ToHashSet() ?? [];
            ISet<uint>? excluded=useUnlocked ? catalog.SectorsOf(map).Where(sector=>!sector.Start && !unlocked.Contains(sector.Id)).Select(sector=>sector.Id).ToHashSet():null;
            var values=BuildRouteValues(catalog,map,world.WorldId);plannerValues=values;
            submarineSearch.Start(key,token=>VoyagePlanner.Plan(catalog.SectorsOf(map),rank,speed,range,goal,id=>SectorValue(catalog,values,band,id),25,token,excluded));
        }
        if(submarineSearch.Take(out var found,out var failure))
        {
            plannerRoutes=found;
            // Missing market prices of the listed routes' loot, the most expected per voyage first (computed once per search, not per frame).
            plannerWanted=plannerValues is { } valued ? plannerRoutes.SelectMany(route=>route.Sectors).Distinct()
                .SelectMany(id=>VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id),plannerBand) ?? VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id)).ToArray())
                .Where(entry=>valued.Missing.Contains(entry.ItemId)).GroupBy(entry=>entry.ItemId)
                .OrderByDescending(group=>group.Sum(entry=>entry.PerVisit)).Select(group=>group.Key).ToArray():[];
            plannerMissingCounts=plannerValues is { } counted ? plannerRoutes.Select(route=>RouteMissing(catalog,counted,route,plannerBand)).ToArray():[];
        }
        else if(failure is not null)log.Warning(failure,"Submarine routes could not be planned.");
        if(submarineSearch.Searching){ImGui.TextDisabled(localization.Get("RoutePlanner.Searching"));return;}
        if(plannerRoutes.Count==0){ImGui.TextDisabled(localization.Get("RoutePlanner.None"));return;}
        if(!ImGui.BeginTable("PlannerRoutes",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollY,new Vector2(0,ImGui.GetContentRegionAvail().Y-ImGui.GetTextLineHeightWithSpacing()*2)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"Route","Distance","Duration","Value","PerDay","Exp"})ImGui.TableSetupColumn(localization.Get("RoutePlanner.Column"+column));
            ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            var index=0;
            foreach(var route in plannerRoutes)
            {
                index++;
                ImGui.TableNextRow();ImGui.TableNextColumn();
                var names=route.Sectors.Select(id=>catalog.Sectors.TryGetValue(id,out var sector) ? sector.Name:id.ToString());
                ImGui.Selectable($"{index}. {string.Join(" › ",names)}##PlannerRoute{index}",false,ImGuiSelectableFlags.SpanAllColumns|ImGuiSelectableFlags.AllowItemOverlap);
                // This ImGui ignores later overlapping items in IsItemHovered: one tooltip per row, chosen after the button (0.315 review).
                var rowHovered=ImGui.IsItemHovered();
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{route.Distance} / {inputs.Range}");
                ImGui.TableNextColumn();ImGui.TextUnformatted(HoursMinutes(TimeSpan.FromSeconds(route.Seconds)));
                // 0.335: a market price is missing on this route: its items count at the NPC price, the value is a lower bound (≥).
                var missingPrices=index-1<plannerMissingCounts.Length ? plannerMissingCounts[index-1]:0;
                var atLeast=missingPrices>0 ? "≥ ":"";
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{atLeast}{(route.Value).ToString("N0",Numbers)}");
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{atLeast}{(route.ValuePerDay).ToString("N0",Numbers)}");
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{(route.Exp).ToString("N0",Numbers)}");
                // 0.315: open this route in Builds (range, voyage time, breakpoints, best builds) for the chosen submarine.
                ImGui.TableNextColumn();
                if(ImGui.SmallButton(localization.Get("RoutePlanner.ToBuild")+$"##ToBuild{index}"))
                {
                    buildRoute=[..route.Sectors];buildSearchKey=null;freeCompanyTabRequest=2;
                    // Always a submarine in Builds: the planner's vessel, else the one already chosen, else the manual one at the planner's rank.
                    if(chosen is not null)buildVesselKey=VesselKey(chosen);
                    else if(buildVesselKey is null || KindOfKey(buildVesselKey)!=VesselKind.Submarine)
                    {
                        var manual=ManualKey(VesselKind.Submarine);buildVesselKey=manual;
                        if(VesselParts?.Invoke() is { } vesselParts)
                        {
                            var plan=PlannedBuild(vesselParts,manual,null);
                            var wanted=Math.Clamp(inputs.Rank,1,VesselBuilds.MaxRank(vesselParts.SubmarineRanks));
                            if(plan.Rank!=wanted)StoreVesselPlan(manual,plan with{Rank=wanted});
                        }
                    }
                }
                Tip("RoutePlanner.ToBuildTip");
                if(rowHovered && !ImGui.IsItemHovered())DrawRouteLootTooltip(catalog,route,missingPrices);
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(plannerMarket
            ? localization.Format("RoutePlanner.SourceMarket",catalog.LootSource,catalog.LootBuilt,world.WorldName.Length>0 ? world.WorldName:"—")
            :localization.Format("RoutePlanner.Source",catalog.LootSource,catalog.LootBuilt));
    }

    /// <summary>
    /// X-200 (0.1703, the user's request of 12/11, as the airships' unlock goal): a sector of this map aimed at, and the sectors to explore
    /// first to unlock it (SubmarineUnlocks.Route: LuminaSupplemental's tree), from the last one already unlocked in the company's workshop
    /// when it was read; each step with the rank it asks (SubmarineExploration). Unknown, open from the start or already unlocked: said so.
    /// </summary>
    private void DrawSubmarineUnlockTarget(SubmarineCatalog catalog,IReadOnlyCollection<uint>? unlockedList)
    {
        string Step(uint id)=>catalog.Sectors.TryGetValue(id,out var sector) ? localization.Format("RoutePlanner.UnlockStep",sector.Name,sector.RankReq):id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if(plannerUnlockTarget!=0 && (!catalog.Sectors.TryGetValue(plannerUnlockTarget,out var aimed) || aimed.Map!=plannerMap))plannerUnlockTarget=0;
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.UnlockTarget")+"##PlannerUnlock",plannerUnlockTarget!=0 && catalog.Sectors.TryGetValue(plannerUnlockTarget,out var shown) ? shown.Name:"—"))
        {
            try
            {
                if(ImGui.Selectable("—##PlannerUnlockNone",plannerUnlockTarget==0))plannerUnlockTarget=0;
                foreach(var sector in catalog.SectorsOf(plannerMap))
                    if(!sector.Start && ImGui.Selectable($"{sector.Name}##PlannerUnlock{sector.Id}",sector.Id==plannerUnlockTarget))plannerUnlockTarget=sector.Id;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.UnlockTargetTip");
        if(plannerUnlockTarget==0)return;
        var (state,explore)=SubmarineUnlocks.Route(catalog.Sectors,catalog.Unlocks,plannerUnlockTarget,unlockedList);
        if(state!=SubmarineUnlockState.Path){ImGui.TextDisabled(localization.Get("RoutePlanner.Unlock"+state));return;}
        ImGui.TextWrapped(localization.Format("RoutePlanner.UnlockPath",catalog.Sectors[plannerUnlockTarget].Name,string.Join(" › ",explore.Select(Step))));
        if(unlockedList is null)ImGui.TextDisabled(localization.Get("RoutePlanner.UnlockNotRead"));
    }

    private void DrawRoutePriceFetch(Market.MarketState world,ulong character,Market.PreparationPriceState prices)
    {
        if(prices.Loading){ImGui.TextDisabled(localization.Format("RoutePlanner.FetchingPrices",prices.Completed,prices.Items.Count));return;}
        var wanted=plannerWanted;
        ImGui.BeginDisabled(world.WorldId==0 || character==0 || wanted.Length==0);
        try
        {
            if(SourceButton(localization.Format("RoutePlanner.FetchPrices",Math.Min(32,wanted.Length),wanted.Length),SourceOn(SourceUse.UniversalisToolPrices),true))
            {
                try{_=routePrices.StartListAsync(market,character,world.WorldId,wanted.Take(32));}
                catch(Exception error){log.Warning(error,"Route market prices could not be started.");}
            }
        }
        finally{ImGui.EndDisabled();}
        Tip(world.WorldId==0 || character==0 ? "RoutePlanner.FetchPricesNoWorld":"RoutePlanner.FetchPricesTip");
        if(prices.Error is { } failed){ImGui.SameLine();ImGui.TextColored(UI.FfxivTheme.Gold,failed);}
    }

    // Unit values copied on the UI thread before the worker runs: NPC price, or the best of NPC and the fresh market minimum (NQ).
    private RouteValues BuildRouteValues(SubmarineCatalog catalog,uint map,uint world)
    {
        var unit=new Dictionary<uint,double>();var missing=new HashSet<uint>();var fallback=new HashSet<uint>();var prices=new Dictionary<uint,decimal>();
        var noListing=new HashSet<uint>();
        var now=DateTimeOffset.UtcNow;
        if(routeQuotes.Count>1024)routeQuotes.Clear();
        foreach(var sector in catalog.SectorsOf(map))
        {
            if(catalog.Loot.GetValueOrDefault(sector.Id) is not { } loot)continue;
            if(plannerBand!=SurveillanceBand.All && VoyagePlanner.PerVisit(loot,plannerBand) is null)fallback.Add(sector.Id);
            foreach(var (item,_) in loot.Items)
            {
                if(unit.ContainsKey(item))continue;
                var npc=catalog.SellPrices.GetValueOrDefault(item);
                if(!plannerMarket){unit[item]=npc;continue;}
                var quote=world==0 ? null:market.GetCached(world,item) ?? routeQuotes.GetValueOrDefault((world,item));
                if(quote is not null && market.IsFresh(quote,now))routeQuotes[(world,item)]=quote;else quote=null;
                decimal? lowest=quote?.Nq.Minimum;
                // A fresh answer without any NQ listing: the market offers nothing to compare, the NPC price stands (said in the tooltip).
                if(quote is not null && lowest is not > 0 && catalog.Marketable.Contains(item)){unit[item]=npc;noListing.Add(item);continue;}
                var (value,isMissing)=VoyagePlanner.UnitValue(npc,catalog.Marketable.Contains(item),lowest);
                unit[item]=value;
                if(isMissing)missing.Add(item);
                if(lowest is > 0)prices[item]=lowest.Value;
            }
        }
        return new(unit,missing,fallback,prices,world,noListing);
    }

    // A sector's value from the chosen band's records; a sector without a record in that band is valued from all its records (listed).
    private static double SectorValue(SubmarineCatalog catalog,RouteValues values,SurveillanceBand band,uint sector)
    {
        var loot=catalog.Loot.GetValueOrDefault(sector);
        double Unit(uint item)=>values.Unit.GetValueOrDefault(item);
        return VoyagePlanner.ExpectedValue(loot,Unit,band) ?? VoyagePlanner.ExpectedValue(loot,Unit,SurveillanceBand.All) ?? 0;
    }

    // 0.337 (review): counted over the items the chosen band values (as the fetch list), so « ≥ » clears once they are all priced.
    private static int RouteMissing(SubmarineCatalog catalog,RouteValues values,PlannedRoute route,SurveillanceBand band)
        =>values.Missing.Count==0 ? 0:route.Sectors
            .SelectMany(id=>VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id),band) ?? VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id)).ToArray())
            .Select(entry=>entry.ItemId).Distinct().Count(values.Missing.Contains);

    // Hovering a route: what one voyage brings on average, item by item, with its NPC value.
    private void DrawRouteLootTooltip(SubmarineCatalog catalog,PlannedRoute route,int missingPrices)
    {
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("RoutePlanner.LootTitle"));
            var items=itemCatalog.State.Catalog;
            // 0.335: the quantities of the chosen surveillance band (all records for a sector without one), valued as in the table.
            var values=plannerValues;
            double Unit(uint item)=>values?.Unit.GetValueOrDefault(item) ?? catalog.SellPrices.GetValueOrDefault(item);
            var expected=route.Sectors.SelectMany(id=>VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id),plannerBand)
                    ?? VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id)).ToArray())
                .GroupBy(entry=>entry.ItemId).Select(group=>(Item:group.Key,Count:group.Sum(entry=>entry.PerVisit)))
                .OrderByDescending(entry=>entry.Count*Unit(entry.Item)).Take(12);
            foreach(var (item,count) in expected)
            {
                var definition=items?.Find(item);
                DrawItemIcon(definition,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                var name=definition?.Name ?? item.ToString();var total=(count*Unit(item)).ToString("N0");var npc=catalog.SellPrices.GetValueOrDefault(item).ToString("N0");
                ImGui.TextUnformatted(values?.Market.TryGetValue(item,out var lowest)==true
                    ? localization.Format("RoutePlanner.LootLineMarket",name,count.ToString("0.0",Numbers),total,npc,lowest.ToString("N0",Numbers))
                    :values?.Missing.Contains(item)==true ? localization.Format("RoutePlanner.LootLineNoPrice",name,count.ToString("0.0",Numbers),total,npc)
                    :values?.NoListing.Contains(item)==true ? localization.Format("RoutePlanner.LootLineNoListing",name,count.ToString("0.0",Numbers),total,npc)
                    :localization.Format("RoutePlanner.LootLine",name,count.ToString("0.0",Numbers),total));
            }
            if(route.Sectors.Any(id=>!catalog.Loot.ContainsKey(id)))ImGui.TextDisabled(localization.Get("RoutePlanner.NoLoot"));
            if(values is not null && route.Sectors.Any(values.BandFallback.Contains))ImGui.TextDisabled(localization.Get("RoutePlanner.BandFallback"));
            if(missingPrices>0)ImGui.TextDisabled(localization.Format("RoutePlanner.PricesMissing",missingPrices));
        }
    }
}
