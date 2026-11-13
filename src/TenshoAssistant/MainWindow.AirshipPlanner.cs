using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// DEC-023, point 3 (0.337, user's go-ahead for calling the game's voyage functions): airship routes by distance and duration. The vessel
// read in the workshop (or manual input) gives rank, speed and range; the game computes the legs for that speed on the framework thread
// (FreeCompany/AirshipVoyageTable.Dalamud.cs); the routes are searched on a worker, the previous search cancelled. Loot value unknown:
// ranked by experience or duration only. The assembly of a voyage (legs + surveys + 12 h, no return leg, seconds) is not confirmed in game.
internal sealed partial class MainWindow
{
    internal Func<AirshipTableState?>? AirshipTable { get; set; }
    internal Action<int,IReadOnlyList<uint>>? RequestAirshipTable { get; set; }
    private string? airshipVesselKey;
    private int airshipSpeed=1,airshipRange=1;
    private AirshipGoal airshipGoal=AirshipGoal.ExpPerDay;
    private readonly List<uint> airshipChosen=[];
    private string? airshipPlanKey;
    private Task<IReadOnlyList<AirshipRoute>>? airshipPlanTask;
    private CancellationTokenSource? airshipPlanCancel;
    private IReadOnlyList<AirshipRoute> airshipRoutes=[];
    private (AirshipLegTable Table,string Chosen)? airshipChosenKey;
    private AirshipRoute? airshipChosenRoute;
    private (AirshipCatalog Catalog,uint[] Ids)? airshipPointIds;
    // X-201 (0.1243): the item (loot goal) or the sector (unlock goal) the routes must serve; 0: none chosen yet.
    private uint airshipLootItem,airshipUnlockSector;
    private string airshipTargetSearch="";

    private void DrawAirshipPlanner(AirshipCatalog catalog)
    {
        // Vessel: fills rank, speed and range (still editable). A key no longer read = manual input.
        var ships=(configuration.Voyages ?? []).Where(vessel=>vessel.Kind==VesselKind.Airship).ToArray();
        var chosen=airshipVesselKey is null ? null:ships.FirstOrDefault(vessel=>VesselKey(vessel)==airshipVesselKey);
        if(chosen is null)airshipVesselKey=null;
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("AirshipPlanner.Vessel")+"##AirshipVessel",chosen?.Name ?? localization.Get("RoutePlanner.Manual")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RoutePlanner.Manual"),chosen is null))airshipVesselKey=null;
                for(var index=0;index<ships.Length;index++)
                    if(ImGui.Selectable($"{ships[index].Name} · {localization.Format("FreeCompany.Rank",ships[index].Rank)}##AirshipVessel{index}",VesselKey(ships[index])==airshipVesselKey))
                    {
                        var vessel=ships[index];airshipVesselKey=VesselKey(vessel);
                        airshipRank=vessel.Rank;airshipSpeed=Math.Max(1,vessel.Speed);airshipRange=Math.Max(1,vessel.Range);
                    }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("AirshipPlanner.VesselTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Speed")+"##AirshipSpeed",ref airshipSpeed);Tip("AirshipPlanner.SpeedTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Range")+"##AirshipRange",ref airshipRange);Tip("AirshipPlanner.RangeTip");
        airshipSpeed=Math.Clamp(airshipSpeed,1,1000);airshipRange=Math.Clamp(airshipRange,1,1000);
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("RoutePlanner.Goal")+"##AirshipGoal",localization.Get("AirshipPlanner.Goal"+airshipGoal)))
        {
            try{foreach(var goal in Enum.GetValues<AirshipGoal>())if(ImGui.Selectable(localization.Get("AirshipPlanner.Goal"+goal),goal==airshipGoal))airshipGoal=goal;}
            finally{ImGui.EndCombo();}
        }
        Tip("AirshipPlanner.GoalTip");
        var targets=DrawAirshipTarget(catalog);

        // The game computes the legs for this speed on the framework thread; until then, or when it cannot, say why.
        if(airshipPointIds?.Catalog!=catalog)airshipPointIds=(catalog,catalog.Sectors.Select(sector=>sector.Id).ToArray());
        RequestAirshipTable?.Invoke(airshipSpeed,airshipPointIds.Value.Ids);
        var state=AirshipTable?.Invoke();
        if(state?.Table is not { } table || table.Speed!=airshipSpeed)
        {
            ImGui.TextDisabled(localization.Get(state?.Unavailable==true ? "AirshipPlanner.Unavailable":state?.NotLoggedIn==true ? "AirshipPlanner.NotLoggedIn":"AirshipPlanner.Computing"));
            return;
        }
        ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("AirshipPlanner.Unconfirmed"));
        Tip("AirshipPlanner.UnconfirmedTip");

        // Chosen sectors (ticked in the table below): best order, distance, duration, experience; recomputed only when they change.
        var chosenKey=(table,string.Join(",",airshipChosen));
        if(airshipChosenKey!=chosenKey)
        {
            airshipChosenRoute=AirshipPlanner.Best(table,airshipChosen.Select(catalog.Find).OfType<AirshipSector>().ToArray());
            airshipChosenKey=chosenKey;
        }
        if(airshipChosen.Count==0)ImGui.TextDisabled(localization.Get("AirshipPlanner.ChooseHint"));
        else if(airshipChosenRoute is { } mine)
        {
            ImGui.TextUnformatted(localization.Format("AirshipPlanner.Chosen",AirshipRouteNames(catalog,mine),mine.Distance,airshipRange,HoursMinutes(TimeSpan.FromSeconds(mine.Seconds)),AirshipExp(mine),mine.Tanks));
            if(mine.UnknownExp>0)Tip("AirshipPlanner.ExpUnknownTip");
            if(mine.Distance>airshipRange)ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("AirshipPlanner.OverRange"));
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("AirshipPlanner.Clear")))airshipChosen.Clear();
            Tip("AirshipPlanner.ClearTip");
        }
        else ImGui.TextDisabled(localization.Get("AirshipPlanner.LegUnknown"));
        if(developerMode)
            ImGui.TextDisabled(localization.Format("AirshipPlanner.Raw",table.Speed,
                table.Legs.TryGetValue((AirshipPlanner.Start,0),out var first) ? $"{first.Time} / {first.Distance}":"—",table.Survey.TryGetValue(0,out var survey) ? survey.ToString():"—"));

        // Best routes: searched on a worker when an input changed; the running search is cancelled.
        var rank=airshipRank==0 ? int.MaxValue:airshipRank;
        var key=$"{table.Speed}|{rank}|{airshipRange}|{airshipGoal}|{RuntimeHelpers.GetHashCode(table)}|{(targets is null ? "-":string.Join(",",targets))}";
        if(key!=airshipPlanKey)
        {
            airshipPlanKey=key;airshipPlanCancel?.Cancel();airshipPlanCancel?.Dispose();airshipPlanCancel=new CancellationTokenSource();
            var token=airshipPlanCancel.Token;var (range,goal,sectors)=(airshipRange,airshipGoal,catalog.Sectors);
            airshipPlanTask=Task.Run(()=>AirshipPlanner.Plan(sectors,table,rank,range,goal,15,token,targets),token);
        }
        if(airshipPlanTask is { IsCompleted:true } done)
        {
            if(done.IsCompletedSuccessfully)airshipRoutes=done.Result;
            else if(done.Exception is { } error)log.Warning(error,"Airship routes could not be planned.");
            airshipPlanTask=null;
        }
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("AirshipPlanner.Best")))return;
        if(airshipPlanTask is not null){ImGui.TextDisabled(localization.Get("RoutePlanner.Searching"));return;}
        if(airshipGoal!=AirshipGoal.Shortest)ImGui.TextDisabled(localization.Get("AirshipPlanner.ExpUnknownNote"));
        if(airshipRoutes.Count==0){ImGui.TextDisabled(localization.Get("RoutePlanner.None"));return;}
        if(!ImGui.BeginTable("AirshipRoutes",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"Route","Distance","Duration","Exp","PerDay"})ImGui.TableSetupColumn(localization.Get("AirshipPlanner.Column"+column));
            ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            var index=0;
            foreach(var route in airshipRoutes)
            {
                index++;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{index}. {AirshipRouteNames(catalog,route)}");
                ImGui.TableNextColumn();ImGui.TextUnformatted($"{route.Distance} / {airshipRange}");
                ImGui.TableNextColumn();ImGui.TextUnformatted(HoursMinutes(TimeSpan.FromSeconds(route.Seconds)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(AirshipExp(route));
                if(route.UnknownExp>0)Tip("AirshipPlanner.ExpUnknownTip");
                ImGui.TableNextColumn();ImGui.TextUnformatted((route.UnknownExp>0 ? "≥ ":"")+route.ExpPerDay.ToString("N0",Numbers));
                ImGui.TableNextColumn();
                if(ImGui.SmallButton(localization.Get("AirshipPlanner.Use")+$"##AirshipUse{index}")){airshipChosen.Clear();airshipChosen.AddRange(route.Sectors);}
                Tip("AirshipPlanner.UseTip");
            }
        }
        finally{ImGui.EndTable();}
    }

    // X-201 (0.1243, user request of 2026-11-12): the target of the loot and unlock goals, chosen in a list with a search box; the sectors
    // the routes must visit (null: no target for this goal; empty: none chosen yet, so no route). Drops and unlocks: LuminaSupplemental.
    private IReadOnlyCollection<uint>? DrawAirshipTarget(AirshipCatalog catalog)
    {
        if(airshipGoal is not (AirshipGoal.Loot or AirshipGoal.Unlock))return null;
        var loot=airshipGoal==AirshipGoal.Loot;
        var items=itemCatalog.State.Catalog;
        string ItemName(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        string SectorName(uint id)=>catalog.Find(id)?.ShortName ?? id.ToString();
        var chosen=loot ? airshipLootItem:airshipUnlockSector;
        var unlockable=loot ? []:catalog.Sectors.Where(sector=>AirshipSectors.Unlocker(catalog,sector.Id) is not null).ToArray();
        ImGui.SetNextItemWidth(260);
        var label=localization.Get(loot ? "AirshipPlanner.LootItem":"AirshipPlanner.UnlockSector");
        if(ImGui.BeginCombo(label+"##AirshipTarget",chosen==0 ? "—":loot ? ItemName(chosen):SectorName(chosen)))
        {
            try
            {
                ImGui.SetNextItemWidth(220);ImGui.InputTextWithHint("##AirshipTargetSearch",localization.Get("AirshipPlanner.TargetSearch"),ref airshipTargetSearch,60);Tip("AirshipPlanner.TargetSearchTip");
                var search=airshipTargetSearch.Trim();
                var rows=loot ? AirshipSectors.LootItems(catalog).Select(id=>(Id:id,Name:ItemName(id)))
                    :unlockable.Select(sector=>(sector.Id,Name:$"{sector.ShortName} · {sector.Name}"));
                foreach(var (id,name) in rows.Where(row=>search.Length==0 || row.Name.Contains(search,StringComparison.CurrentCultureIgnoreCase))
                            .OrderBy(row=>row.Name,StringComparer.CurrentCultureIgnoreCase))
                    if(ImGui.Selectable($"{name}##AirshipTarget{id}",id==chosen)){if(loot)airshipLootItem=id;else airshipUnlockSector=id;}
            }
            finally{ImGui.EndCombo();}
        }
        Tip(loot ? "AirshipPlanner.LootItemTip":"AirshipPlanner.UnlockSectorTip");
        if(chosen==0){ImGui.TextDisabled(localization.Get(loot ? "AirshipPlanner.LootChoose":"AirshipPlanner.UnlockChoose"));return [];}
        if(loot)
        {
            var where=AirshipSectors.DropSectors(catalog,chosen);
            ImGui.TextDisabled(localization.Format("AirshipPlanner.LootWhere",where.Count==0 ? "—":string.Join(", ",where.Select(SectorName))));
            return where;
        }
        if(AirshipSectors.Unlocker(catalog,chosen) is not { } unlocker){ImGui.TextDisabled(localization.Get("AirshipPlanner.UnlockChoose"));return [];}
        ImGui.TextWrapped(localization.Format("AirshipPlanner.UnlockBy",SectorName(unlocker.By),unlocker.Rank,unlocker.Surveillance,
            string.Join(" › ",AirshipSectors.UnlockPath(catalog,chosen).Select(SectorName))));
        return [unlocker.By];
    }

    // « H h mm » of a voyage or survey time, on the airship and submarine pages.
    private static string HoursMinutes(TimeSpan time)=>TenshoAssistant.Localization.TenshoCulture.Hours((int)time.TotalHours,time.Minutes);

    // Experience of a route; « ≥ » when a sector's experience is not known (0 in the game data).
    private static string AirshipExp(AirshipRoute route)=>(route.UnknownExp>0 ? "≥ ":"")+route.Exp.ToString("N0",Numbers);

    private static string AirshipRouteNames(AirshipCatalog catalog,AirshipRoute route)
        =>string.Join(" › ",route.Sectors.Select(id=>catalog.Find(id)?.ShortName ?? id.ToString()));

    // Ticking a sector in the table adds it to the chosen route (5 at most).
    private void DrawAirshipChoice(AirshipSector sector)
    {
        var ticked=airshipChosen.Contains(sector.Id);
        ImGui.BeginDisabled(!ticked && airshipChosen.Count>=AirshipPlanner.MaxSectors);
        try
        {
            if(ImGui.Checkbox($"##AirshipChoose{sector.Id}",ref ticked))
            {
                if(ticked && !airshipChosen.Contains(sector.Id))airshipChosen.Add(sector.Id);
                else if(!ticked)airshipChosen.Remove(sector.Id);
            }
        }
        finally{ImGui.EndDisabled();}
        Tip("AirshipPlanner.ChooseTip");
    }
}
