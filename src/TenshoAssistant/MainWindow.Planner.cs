using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// X-604 (0.1706, the user's request of 18/11: « airships and submarines each have their own route planner; keep the best of both in one »):
// one route planner for both kinds of vessels, in the Routes tab: the kind at its top, then the common part (the vessel read in the
// workshop or manual input, its planned build, rank, speed and range), then the kind's own part: MainWindow.RoutePlanner.cs (submarines:
// community loot values, maps, unlocked sectors, the way to unlock a sector) and MainWindow.AirshipPlanner.cs (airships: the game's leg
// times, targets, sectors chosen by hand, the fleet). Comparison of the two planners: docs/evidence/0.1706-route-planners.md.
internal sealed partial class MainWindow
{
    /// <summary>X-604: what the planner is set to for one kind of vessel (kept while the window lives, one set per kind).</summary>
    private sealed class PlannerInputs
    {
        // 0.331 (audit): the chosen vessel by its key (VesselKey, as the Builds tab), not by its index in a list re-sorted at each workshop read.
        public string? VesselKey;
        public int Rank,Speed=1,Range=1;
        // X-718 (0.1722): where speed and range come from (Read: read, typed or the planned build); Unknown: said, nothing searched.
        public VesselStatsSource StatsFrom=VesselStatsSource.Read;
        // X-730 M1 (0.1728): speed and range typed since the vessel was chosen (unknown stats become known once both are).
        public bool SpeedTyped,RangeTyped;
    }

    private VesselKind plannerKind=VesselKind.Submarine;
    private readonly PlannerInputs submarineInputs=new(){Rank=1};
    private readonly PlannerInputs airshipInputs=new();
    private PlannerInputs Inputs(VesselKind kind)=>kind==VesselKind.Airship ? airshipInputs:submarineInputs;
    private string plannerTargetSearch="";

    private void DrawRoutePlanner()
    {
        foreach(var kind in new[]{VesselKind.Submarine,VesselKind.Airship})
        {
            if(kind!=VesselKind.Submarine)ImGui.SameLine();
            if(ImGui.RadioButton(localization.Get("FreeCompany."+kind)+"##PlannerKind"+kind,plannerKind==kind))plannerKind=kind;
            Tip("RoutePlanner.KindTip");
        }
        if(plannerKind==VesselKind.Airship)DrawAirshipRoutes();
        else DrawSubmarineRoutes();
    }

    private void DrawAirshipRoutes()
    {
        var catalog=AirshipCatalog?.Invoke();
        if(catalog is null){ImGui.TextDisabled(AirshipCatalogFailed?.Invoke()==true ? localization.Get("AirshipSectors.Unavailable"):LoadingText("AirshipSectors.Loading"));return;}
        ImGui.TextWrapped(localization.Get("AirshipPlanner.Help"));
        DrawAirshipPlanner(catalog);
    }

    /// <summary>
    /// X-604: the vessel of this kind (read in the workshop, else manual input; a key no longer read is manual input): choosing one fills rank,
    /// speed and range (still editable, to try another build); its planned build (Builds tab) can give them too, for both kinds.
    /// </summary>
    private VoyageVessel? DrawPlannerVessel(VesselKind kind,Action<VoyageVessel>? chosenNow=null)
    {
        var inputs=Inputs(kind);
        var vessels=(configuration.Voyages ?? []).Where(vessel=>vessel.Kind==kind).ToArray();
        var chosen=inputs.VesselKey is null ? null:vessels.FirstOrDefault(vessel=>VesselKey(vessel)==inputs.VesselKey);
        if(chosen is null){inputs.VesselKey=null;if(inputs.StatsFrom!=VesselStatsSource.Read)inputs.StatsFrom=VesselStatsSource.Read;}
        // X-718 (0.1722): the chosen vessel's speed and range unknown, then read in the workshop meanwhile: taken at once.
        else if(inputs.StatsFrom==VesselStatsSource.Unknown && VesselBuilds.PlannerStats(chosen,VesselParts?.Invoke()) is { Source:not VesselStatsSource.Unknown } known)
        {inputs.Speed=known.Speed;inputs.Range=known.Range;inputs.StatsFrom=known.Source;}
        var airship=kind==VesselKind.Airship;
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get(airship ? "AirshipPlanner.Vessel":"RoutePlanner.Vessel")+"##PlannerVessel",chosen?.Name ?? localization.Get("RoutePlanner.Manual")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RoutePlanner.Manual"),chosen is null)){inputs.VesselKey=null;chosen=null;inputs.StatsFrom=VesselStatsSource.Read;}
                for(var index=0;index<vessels.Length;index++)
                    if(ImGui.Selectable($"{vessels[index].Name} · {localization.Format("FreeCompany.Rank",vessels[index].Rank)}##PlannerVessel{index}",VesselKey(vessels[index])==inputs.VesselKey))
                    {
                        // X-718 (0.1722): its speed and range read, else from its parts read, else unknown (said below), never 1 in silence;
                        // choosing it again takes its values again.
                        var vessel=vessels[index];inputs.VesselKey=VesselKey(vessel);chosen=vessel;
                        var (speed,range,source)=VesselBuilds.PlannerStats(vessel,VesselParts?.Invoke());
                        inputs.Rank=vessel.Rank;inputs.StatsFrom=source;inputs.SpeedTyped=false;inputs.RangeTyped=false;
                        if(source!=VesselStatsSource.Unknown){inputs.Speed=speed;inputs.Range=range;}
                        chosenNow?.Invoke(vessel);
                    }
            }
            finally{ImGui.EndCombo();}
        }
        Tip(airship ? "AirshipPlanner.VesselTip":"RoutePlanner.VesselTip");
        // 0.315 (R-11): the vessel's planned build (Free company › Builds) gives its rank, speed and range; X-604: airships too.
        if(chosen is not null && VesselParts?.Invoke() is { } parts
            && VesselBuilds.EffectivePlan(VesselBuild.FromList(kind,configuration.VesselPlans?.GetValueOrDefault(VesselKey(chosen))),chosen.Rank) is { } plannedBuild)
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("RoutePlanner.UsePlan")))
            {
                var summary=VesselBuilds.Summarize(parts,plannedBuild);
                inputs.Rank=plannedBuild.Rank;inputs.Speed=Math.Max(1,summary.Stats.Speed);inputs.Range=Math.Max(1,summary.Stats.Range);inputs.StatsFrom=VesselStatsSource.Read;
            }
            Tip("RoutePlanner.UsePlanTip");
        }
        return chosen;
    }

    /// <summary>
    /// X-604 (0.1707, from the airships' X-201): the target of the loot and unlock goals, for both kinds: a list with a search box, sorted by
    /// name; the id chosen (0: none).
    /// </summary>
    private uint DrawPlannerTarget(string labelKey,string tipKey,uint chosen,string shown,IEnumerable<(uint Id,string Name)> rows)
    {
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo(localization.Get(labelKey)+"##PlannerTarget",chosen==0 ? "—":shown))
        {
            try
            {
                ImGui.SetNextItemWidth(220);ImGui.InputTextWithHint("##PlannerTargetSearch",localization.Get("AirshipPlanner.TargetSearch"),ref plannerTargetSearch,60);Tip("AirshipPlanner.TargetSearchTip");
                var search=plannerTargetSearch.Trim();
                foreach(var (id,name) in rows.Where(row=>search.Length==0 || SearchText.Contains(row.Name,search,StringComparison.CurrentCultureIgnoreCase))
                            .OrderBy(row=>row.Name,StringComparer.CurrentCultureIgnoreCase))
                    if(ImGui.Selectable($"{name}##PlannerTarget{id}",id==chosen))chosen=id;
            }
            finally{ImGui.EndCombo();}
        }
        Tip(tipKey);
        return chosen;
    }

    // X-604 (0.1707): the goals both kinds share are named once (the airships' texts).
    private static string RouteGoalKey(RouteGoal goal)=>goal switch
    {
        RouteGoal.ExperiencePerVoyage=>"AirshipPlanner.GoalExpPerVoyage",
        RouteGoal.Shortest or RouteGoal.Loot or RouteGoal.Unlock=>"AirshipPlanner.Goal"+goal,
        _=>"RoutePlanner.Goal"+goal,
    };

    /// <summary>X-604: rank, speed and range of this kind's search (an airship's rank 0: every sector, as before). X-718 (0.1722): the chosen
    /// vessel's speed and range unknown: said (the planners search nothing until they are typed); taken from its parts: said too.</summary>
    private void DrawPlannerStats(VesselKind kind)
    {
        var inputs=Inputs(kind);
        var airship=kind==VesselKind.Airship;
        ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("RoutePlanner.Rank")+"##PlannerRank",ref inputs.Rank);Tip(airship ? "AirshipPlanner.RankTip":"RoutePlanner.RankTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);if(ImGui.InputInt(localization.Get("RoutePlanner.Speed")+"##PlannerSpeed",ref inputs.Speed)){inputs.SpeedTyped=true;inputs.StatsFrom=VesselBuilds.AfterTyping(inputs.StatsFrom,inputs.SpeedTyped,inputs.RangeTyped);}Tip(airship ? "AirshipPlanner.SpeedTip":"RoutePlanner.SpeedTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);if(ImGui.InputInt(localization.Get("RoutePlanner.Range")+"##PlannerRange",ref inputs.Range)){inputs.RangeTyped=true;inputs.StatsFrom=VesselBuilds.AfterTyping(inputs.StatsFrom,inputs.SpeedTyped,inputs.RangeTyped);}Tip(airship ? "AirshipPlanner.RangeTip":"RoutePlanner.RangeTip");
        inputs.Rank=Math.Clamp(inputs.Rank,airship ? 0:1,200);inputs.Speed=Math.Clamp(inputs.Speed,1,1000);inputs.Range=Math.Clamp(inputs.Range,1,1000);
        if(inputs.StatsFrom==VesselStatsSource.Unknown)StateLine(UI.UiState.Waiting,localization.Get("RoutePlanner.StatsUnknown"));
        else if(inputs.StatsFrom==VesselStatsSource.Parts)OriginLine(UI.UiOrigin.Estimated,localization.Get("RoutePlanner.StatsFromParts"));
    }
}
