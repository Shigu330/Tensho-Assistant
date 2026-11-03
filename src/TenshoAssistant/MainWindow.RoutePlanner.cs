using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// R-07 (0.309, user request: « best routes for submarines … mostly loot »): best submarine routes for a vessel's rank, speed and
// range on one map, ranked by expected loot value (community loot averages × NPC sell price), value per day or experience.
// Computed on a worker when an input changes; the previous search is cancelled; the 25 best are listed. Loot averages:
// FFXIVGachaSpreadsheet (MIT, embedded); formulas understood from SubmarineTracker (MIT), original code (FreeCompany/VoyagePlanner.cs).
// 0.315: « unlocked sectors only » filter (read in the chosen submarine's company workshop), « planned build » button, and a
// « Build » button per route that opens it in the Builds tab. No airship planner: no public loot records for airships.
// 0.335 (DEC-023, point 2): value from the NPC price or the best of NPC and market (Universalis lowest NQ of the current world, fetched
// on a click, 32 items at most), and from all records or those of one surveillance band (the source splits its records by band).
internal sealed partial class MainWindow
{
    internal Func<SubmarineCatalog?>? SubmarineCatalog { get; set; }
    // 0.331 (audit): the route catalog could not be built (VoyageWatcher.CatalogFailed): « unavailable » instead of « loading ».
    internal Func<bool>? SubmarineCatalogFailed { get; set; }
    // 0.331 (audit): the chosen submarine by its key (VesselKey, as the Builds tab), not by its index in a list re-sorted at each workshop read.
    private string? plannerVesselKey;
    private uint plannerMap;
    private int plannerRank=1,plannerSpeed=1,plannerRange=1;
    private RouteGoal plannerGoal=RouteGoal.ValuePerVoyage;
    private string? plannerKey;
    private Task<IReadOnlyList<PlannedRoute>>? plannerTask;
    private CancellationTokenSource? plannerCancel;
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

    private void DrawRoutePlanner()
    {
        var catalog=SubmarineCatalog?.Invoke();
        if(catalog is null){ImGui.TextDisabled(localization.Get(SubmarineCatalogFailed?.Invoke()==true ? "RoutePlanner.Unavailable":"RoutePlanner.Loading"));return;}
        ImGui.TextWrapped(localization.Get("RoutePlanner.Help"));
        // Vessel: fills rank, speed and range (still editable, e.g. to try a new build). A key no longer read = manual input.
        var subs=(configuration.Voyages ?? []).Where(vessel=>vessel.Kind==VesselKind.Submarine).ToArray();
        var chosen=plannerVesselKey is null ? null:subs.FirstOrDefault(vessel=>VesselKey(vessel)==plannerVesselKey);
        if(chosen is null)plannerVesselKey=null;
        ImGui.SetNextItemWidth(220);
        var vesselLabel=chosen?.Name ?? localization.Get("RoutePlanner.Manual");
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Vessel")+"##PlannerVessel",vesselLabel))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RoutePlanner.Manual"),chosen is null)){plannerVesselKey=null;chosen=null;}
                for(var index=0;index<subs.Length;index++)
                    if(ImGui.Selectable($"{subs[index].Name} · {localization.Format("FreeCompany.Rank",subs[index].Rank)}##PlannerVessel{index}",VesselKey(subs[index])==plannerVesselKey))
                    {
                        var vessel=subs[index];plannerVesselKey=VesselKey(vessel);chosen=vessel;
                        plannerRank=vessel.Rank;plannerSpeed=Math.Max(1,vessel.Speed);plannerRange=Math.Max(1,vessel.Range);
                        if(vessel.Points.Count>0 && catalog.Sectors.TryGetValue(vessel.Points[0],out var first))plannerMap=first.Map;
                    }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.VesselTip");
        // 0.315 (R-11): the vessel's planned build (Free company › Builds) gives its rank, speed and range.
        if(chosen is not null && VesselParts?.Invoke() is { } parts
            && VesselBuilds.EffectivePlan(VesselBuild.FromList(VesselKind.Submarine,configuration.VesselPlans?.GetValueOrDefault(VesselKey(chosen))),chosen.Rank) is { } plannedBuild)
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("RoutePlanner.UsePlan")))
            {
                var summary=VesselBuilds.Summarize(parts,plannedBuild);
                plannerRank=plannedBuild.Rank;plannerSpeed=Math.Max(1,summary.Stats.Speed);plannerRange=Math.Max(1,summary.Stats.Range);
            }
            Tip("RoutePlanner.UsePlanTip");
        }
        if(plannerMap==0 && catalog.Maps.Count>0)plannerMap=catalog.Maps[0].Id;
        ImGui.SameLine();ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Map")+"##PlannerMap",catalog.Maps.FirstOrDefault(map=>map.Id==plannerMap).Name ?? ""))
        {
            try{foreach(var (id,name) in catalog.Maps)if(ImGui.Selectable($"{name}##PlannerMap{id}",id==plannerMap))plannerMap=id;}
            finally{ImGui.EndCombo();}
        }
        Tip("RoutePlanner.MapTip");
        ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Rank")+"##PlannerRank",ref plannerRank);Tip("RoutePlanner.RankTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Speed")+"##PlannerSpeed",ref plannerSpeed);Tip("RoutePlanner.SpeedTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Range")+"##PlannerRange",ref plannerRange);Tip("RoutePlanner.RangeTip");
        plannerRank=Math.Clamp(plannerRank,1,200);plannerSpeed=Math.Clamp(plannerSpeed,1,1000);plannerRange=Math.Clamp(plannerRange,1,1000);
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
            if(!prices.Loading)plannerKey=null;
        }
        if(plannerMarket)DrawRoutePriceFetch(world,character,prices);

        // New search when an input changed or a price list ended (plannerKey cleared above); the running one is cancelled.
        var key=$"{plannerMap}|{plannerRank}|{plannerSpeed}|{plannerRange}|{plannerGoal}|{useUnlocked}|{house}|{unlockedList?.Count}|{plannerMarket}|{plannerBand}|{world.WorldId}";
        if(key!=plannerKey)
        {
            plannerKey=key;plannerCancel?.Cancel();plannerCancel?.Dispose();plannerCancel=new CancellationTokenSource();
            var token=plannerCancel.Token;var (map,rank,speed,range,goal,band)=(plannerMap,plannerRank,plannerSpeed,plannerRange,plannerGoal,plannerBand);
            var unlocked=unlockedList?.ToHashSet() ?? [];
            ISet<uint>? excluded=useUnlocked ? catalog.SectorsOf(map).Where(sector=>!sector.Start && !unlocked.Contains(sector.Id)).Select(sector=>sector.Id).ToHashSet():null;
            var values=BuildRouteValues(catalog,map,world.WorldId);plannerValues=values;
            plannerTask=Task.Run(()=>VoyagePlanner.Plan(catalog.SectorsOf(map),rank,speed,range,goal,id=>SectorValue(catalog,values,band,id),25,token,excluded),token);
        }
        if(plannerTask is { IsCompleted:true } done)
        {
            if(done.IsCompletedSuccessfully)
            {
                plannerRoutes=done.Result;
                // Missing market prices of the listed routes' loot, the most expected per voyage first (computed once per search, not per frame).
                plannerWanted=plannerValues is { } found ? plannerRoutes.SelectMany(route=>route.Sectors).Distinct()
                    .SelectMany(id=>VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id),plannerBand) ?? VoyagePlanner.PerVisit(catalog.Loot.GetValueOrDefault(id)).ToArray())
                    .Where(entry=>found.Missing.Contains(entry.ItemId)).GroupBy(entry=>entry.ItemId)
                    .OrderByDescending(group=>group.Sum(entry=>entry.PerVisit)).Select(group=>group.Key).ToArray():[];
                plannerMissingCounts=plannerValues is { } counted ? plannerRoutes.Select(route=>RouteMissing(catalog,counted,route,plannerBand)).ToArray():[];
            }
            else if(done.Exception is { } error)log.Warning(error,"Submarine routes could not be planned.");
            plannerTask=null;
        }
        if(plannerTask is not null){ImGui.TextDisabled(localization.Get("RoutePlanner.Searching"));return;}
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
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{route.Distance} / {plannerRange}");
                ImGui.TableNextColumn();var time=TimeSpan.FromSeconds(route.Seconds);ImGui.TextUnformatted($"{(int)time.TotalHours} h {time.Minutes:00}");
                // 0.335: a market price is missing on this route: its items count at the NPC price, the value is a lower bound (≥).
                var missingPrices=index-1<plannerMissingCounts.Length ? plannerMissingCounts[index-1]:0;
                var atLeast=missingPrices>0 ? "≥ ":"";
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{atLeast}{route.Value:N0}");
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{atLeast}{route.ValuePerDay:N0}");
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{route.Exp:N0}");
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
                            var wanted=Math.Clamp(plannerRank,1,VesselBuilds.MaxRank(vesselParts.SubmarineRanks));
                            if(plan.Rank!=wanted)StoreVesselPlan(manual,plan with{Rank=wanted});
                        }
                    }
                }
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("RoutePlanner.ToBuildTip"));
                else if(rowHovered)DrawRouteLootTooltip(catalog,route,missingPrices);
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(plannerMarket
            ? localization.Format("RoutePlanner.SourceMarket",catalog.LootSource,catalog.LootBuilt,world.WorldName.Length>0 ? world.WorldName:"—")
            :localization.Format("RoutePlanner.Source",catalog.LootSource,catalog.LootBuilt));
    }

    private void DrawRoutePriceFetch(Market.MarketState world,ulong character,Market.PreparationPriceState prices)
    {
        if(prices.Loading){ImGui.TextDisabled(localization.Format("RoutePlanner.FetchingPrices",prices.Completed,prices.Items.Count));return;}
        var wanted=plannerWanted;
        ImGui.BeginDisabled(world.WorldId==0 || character==0 || wanted.Length==0);
        try
        {
            if(SourceButton(localization.Format("RoutePlanner.FetchPrices",Math.Min(32,wanted.Length),wanted.Length),configuration.CommunityUniversalis,true))
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
        ImGui.BeginTooltip();
        try
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
                    ? localization.Format("RoutePlanner.LootLineMarket",name,count.ToString("0.0"),total,npc,lowest.ToString("N0"))
                    :values?.Missing.Contains(item)==true ? localization.Format("RoutePlanner.LootLineNoPrice",name,count.ToString("0.0"),total,npc)
                    :values?.NoListing.Contains(item)==true ? localization.Format("RoutePlanner.LootLineNoListing",name,count.ToString("0.0"),total,npc)
                    :localization.Format("RoutePlanner.LootLine",name,count.ToString("0.0"),total));
            }
            if(route.Sectors.Any(id=>!catalog.Loot.ContainsKey(id)))ImGui.TextDisabled(localization.Get("RoutePlanner.NoLoot"));
            if(values is not null && route.Sectors.Any(values.BandFallback.Contains))ImGui.TextDisabled(localization.Get("RoutePlanner.BandFallback"));
            if(missingPrices>0)ImGui.TextDisabled(localization.Format("RoutePlanner.PricesMissing",missingPrices));
        }
        finally{ImGui.EndTooltip();}
    }
}
