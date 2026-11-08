using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// R-11 step 3 (0.315, user request: « the same style for airships, submarines and every useful gear »): vessel builds in the style
// of the Gear page — the stats on the left (read in the workshop, planned, difference), the four parts on the right, one plan per
// vessel. Submarines also get a route check (range, voyage time, community breakpoints, wear estimate) and the best builds for that
// route, computed on a worker. Part stats, ranks and capacity come from the game sheets; breakpoints from SubmarineTracker (MIT).
// Airships: stats, capacity and part rank only (no breakpoint or unlock data under a compatible licence). The ⚠ shown when the
// read stats differ from the computed sum guards a rule not yet verified in game (CHANGELOG 0.315).
internal sealed partial class MainWindow
{
    internal Func<VesselPartCatalog?>? VesselParts { get; set; }
    // 0.331 (audit): the part catalog could not be built (VoyageWatcher.PartCatalogFailed): « unavailable » instead of « loading ».
    internal Func<bool>? VesselPartsFailed { get; set; }
    private string? buildVesselKey;
    // 0.331 (audit): the rank being typed (or stepped) for one vessel key, shown at once and saved once when the field is left.
    private (string Key,int Rank)? buildRankEdit;
    private List<uint> buildRoute=[];
    private SurveillanceTier buildSurveillance=SurveillanceTier.High;
    private RetrievalTier buildRetrieval=RetrievalTier.Optimal;
    private bool buildFavor;
    private string? buildSearchKey;
    private Task<IReadOnlyList<BuildCandidate>>? buildSearch;
    private CancellationTokenSource? buildSearchCancel;
    private IReadOnlyList<BuildCandidate> buildCandidates=[];
    private int freeCompanyTabRequest=-1;

    private static string VesselKey(VoyageVessel vessel)=>$"{(vessel.Kind==VesselKind.Submarine ? "sub":"air")}:{vessel.HouseId}:{vessel.Slot}";
    private static string ManualKey(VesselKind kind)=>kind==VesselKind.Submarine ? "sub:manual":"air:manual";
    private static VesselKind KindOfKey(string key)=>key.StartsWith("air",StringComparison.Ordinal) ? VesselKind.Airship:VesselKind.Submarine;

    /// <summary>The build read in the workshop (null when the parts were not read yet: vessels saved before 0.315).</summary>
    private static VesselBuild? ReadBuild(VoyageVessel? vessel)
        =>vessel is { Parts.Count:4 } && vessel.Parts.All(row=>row!=0) ? new(vessel.Kind,vessel.Rank,vessel.Parts[0],vessel.Parts[1],vessel.Parts[2],vessel.Parts[3]):null;

    private VesselBuild PlannedBuild(VesselPartCatalog catalog,string key,VoyageVessel? vessel)
    {
        var kind=KindOfKey(key);
        if(VesselBuilds.EffectivePlan(VesselBuild.FromList(kind,configuration.VesselPlans?.GetValueOrDefault(key)),vessel?.Rank ?? 0) is { } saved)return saved;
        if(ReadBuild(vessel) is { } read)return read;
        // Nothing planned or read: the first part of each slot (lowest rank).
        var first=Enumerable.Range(0,4).Select(slot=>catalog.ForSlot(kind,slot).FirstOrDefault()?.Row ?? 0).ToArray();
        return new(kind,Math.Max(1,vessel?.Rank ?? 1),first[0],first[1],first[2],first[3]);
    }

    private void StoreVesselPlan(string key,VesselBuild? build)
    {
        var plans=configuration.VesselPlans??=[];
        if(build is null)plans.Remove(key);else plans[key]=build.ToList();
        try{save();saveError=null;}
        catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Vessel build could not be saved.");}
    }

    private void DrawVesselBuilds()
    {
        var catalog=VesselParts?.Invoke();
        if(catalog is null){ImGui.TextDisabled(localization.Get(VesselPartsFailed?.Invoke()==true ? "VesselBuilds.Unavailable":"VesselBuilds.Loading"));return;}
        var vessels=configuration.Voyages ?? [];
        // First visit: the first submarine read, else the manual submarine (an airship has no route section).
        buildVesselKey??=vessels.FirstOrDefault(entry=>entry.Kind==VesselKind.Submarine) is { } firstSub ? VesselKey(firstSub):ManualKey(VesselKind.Submarine);
        var vessel=vessels.FirstOrDefault(entry=>VesselKey(entry)==buildVesselKey);
        string Label(string key)=>vessels.FirstOrDefault(entry=>VesselKey(entry)==key) is { } named
            ? $"{named.Name} · {localization.Get("FreeCompany."+named.Kind)} · {localization.Format("FreeCompany.Rank",named.Rank)}"
            :localization.Get(KindOfKey(key)==VesselKind.Submarine ? "VesselBuilds.ManualSubmarine":"VesselBuilds.ManualAirship");
        ImGui.SetNextItemWidth(300);
        if(ImGui.BeginCombo(localization.Get("VesselBuilds.Vessel")+"##BuildVessel",Label(buildVesselKey)))
        {
            try
            {
                foreach(var key in vessels.Select(VesselKey).Append(ManualKey(VesselKind.Submarine)).Append(ManualKey(VesselKind.Airship)))
                    if(ImGui.Selectable(Label(key)+"##"+key,key==buildVesselKey)){buildVesselKey=key;buildCandidates=[];buildSearchKey=null;}
            }
            finally{ImGui.EndCombo();}
        }
        Tip("VesselBuilds.VesselTip");
        var kind=KindOfKey(buildVesselKey);
        var plan=PlannedBuild(catalog,buildVesselKey,vessel);
        var read=ReadBuild(vessel);
        var ranks=kind==VesselKind.Submarine ? catalog.SubmarineRanks:catalog.AirshipRanks;
        ImGui.SameLine();ImGui.SetNextItemWidth(110);
        // 0.331 (audit): no disk write per key press or +/- step: the edited rank drives the page at once and is saved when the field is left.
        var rank=buildRankEdit is { } pending && pending.Key==buildVesselKey ? pending.Rank:plan.Rank;
        if(ImGui.InputInt(localization.Get("VesselBuilds.Rank")+"##BuildRank",ref rank))buildRankEdit=(buildVesselKey,Math.Clamp(rank,1,VesselBuilds.MaxRank(ranks)));
        if(ImGui.IsItemDeactivatedAfterEdit() && buildRankEdit is { } edited && edited.Key==buildVesselKey && edited.Rank!=plan.Rank)
        {
            StoreVesselPlan(buildVesselKey,plan with{Rank=edited.Rank});
            plan=PlannedBuild(catalog,buildVesselKey,vessel);
        }
        if(!ImGui.IsItemActive())buildRankEdit=null;
        else if(buildRankEdit is { } shown && shown.Key==buildVesselKey)plan=plan with{Rank=shown.Rank};
        Tip("VesselBuilds.RankTip");
        if(read is not null)
        {
            ImGui.SameLine();
            ImGui.BeginDisabled(read==plan);
            if(ImGui.Button(localization.Get("VesselBuilds.UseRead")))StoreVesselPlan(buildVesselKey,null);
            ImGui.EndDisabled();
            Tip("VesselBuilds.UseReadTip");
        }
        else if(vessel is not null)ImGui.TextDisabled(localization.Get("VesselBuilds.PartsUnknown"));

        var planned=VesselBuilds.Summarize(catalog,plan);
        var current=read is null ? null:VesselBuilds.Summarize(catalog,read);
        if(!ImGui.BeginTable("VesselBuildLayout",2,ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn("##Stats",ImGuiTableColumnFlags.WidthStretch,1.1f);
            ImGui.TableSetupColumn("##Parts",ImGuiTableColumnFlags.WidthStretch,1);
            ImGui.TableNextRow();ImGui.TableNextColumn();
            DrawVesselStats(catalog,kind,vessel,read,current,plan,planned);
            ImGui.TableNextColumn();
            for(var slot=0;slot<4;slot++)DrawVesselSlot(catalog,plan,read,slot);
        }
        finally{ImGui.EndTable();}
        if(kind==VesselKind.Submarine)DrawBuildRoute(catalog,plan,planned,vessel);
        else ImGui.TextDisabled(localization.Get("VesselBuilds.AirshipNote"));
        ImGui.TextDisabled(localization.Get("VesselBuilds.Sources"));
    }

    // Stats: read in the workshop, planned (parts + rank bonus), difference; then cost, part rank, repair and the build name.
    private void DrawVesselStats(VesselPartCatalog catalog,VesselKind kind,VoyageVessel? vessel,VesselBuild? read,BuildSummary? current,VesselBuild plan,BuildSummary planned)
    {
        var names=catalog.StatNames(kind);
        // Values read in the game (vessels saved before 0.315 have no airship stats: unknown, not zero).
        VesselStats? readStats=vessel is null ? null:kind==VesselKind.Airship && vessel.Parts.Count==0 ? null
            :new VesselStats(vessel.Surveillance,vessel.Retrieval,vessel.Speed,vessel.Range,vessel.Favor);
        if(!ImGui.BeginTable("VesselStats",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            foreach(var key in new[]{"Stat","Worn","Planned","Delta"})ImGui.TableSetupColumn(localization.Get("Gear.Column"+key));
            ImGui.TableHeadersRow();
            for(var index=0;index<5;index++)
            {
                var later=planned.Stats[index];
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(index<names.Count && names[index].Length>0 ? names[index]:localization.Get("VesselBuilds.Stat"+index));
                ImGui.TableNextColumn();
                if(readStats is { } now)ImGui.TextUnformatted(now[index].ToString());else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();ImGui.TextColored(readStats is { } before && before[index]!=later ? UI.FfxivTheme.Gold:Text,later.ToString());
                ImGui.TableNextColumn();
                if(readStats is { } reference && reference[index]!=later)
                    ImGui.TextColored(later>reference[index] ? Success:Error,$"{(later>reference[index] ? "+":"")}{later-reference[index]}");
            }
            Row(localization.Get("VesselBuilds.Cost"),current is null ? "—":$"{current.Cost} / {current.Capacity}",$"{planned.Cost} / {planned.Capacity}",planned.OverCapacity,localization.Get("VesselBuilds.CostTip"));
            Row(localization.Get("VesselBuilds.PartRank"),current is null ? "—":current.HighestPartRank.ToString(),localization.Format("VesselBuilds.PartRankValue",planned.HighestPartRank,plan.Rank),planned.RankTooLow,localization.Get("VesselBuilds.PartRankTip"));
            Row(localization.Get("VesselBuilds.Repair"),current is null ? "—":current.Repair.ToString(),planned.Repair.ToString(),false,localization.Get("VesselBuilds.RepairTip"));
            if(kind==VesselKind.Submarine)
                Row(localization.Get("VesselBuilds.Identifier"),read is null ? "—":VesselBuilds.Identifier(catalog,read),VesselBuilds.Identifier(catalog,plan),false,localization.Get("VesselBuilds.IdentifierTip"));
        }
        finally{ImGui.EndTable();}
        // Self-check: the read stats should equal the read parts plus the rank bonus (not yet verified in game; a gap is shown, not hidden).
        if(readStats is { } shown && current is not null && current.Stats!=shown)
        {
            ImGui.TextColored(Warning,localization.Get("VesselBuilds.Mismatch"));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("VesselBuilds.MismatchTip",
                string.Join(" / ",Enumerable.Range(0,5).Select(index=>shown[index])),string.Join(" / ",Enumerable.Range(0,5).Select(index=>current.Stats[index]))));
        }

        void Row(string label,string now,string later,bool problem,string tip)
        {
            ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(label);
            if(ImGui.IsItemHovered())WrappedTooltip(tip);
            ImGui.TableNextColumn();ImGui.TextDisabled(now);
            ImGui.TableNextColumn();ImGui.TextColored(problem ? Error:Text,later);
            ImGui.TableNextColumn();
            if(problem)ImGui.TextColored(Error,localization.Get("VesselBuilds.Problem"));
        }
    }

    // One part, like a Gear slot: icon, name, rank and cost, gold frame when the plan differs from the read build; click = choose.
    private void DrawVesselSlot(VesselPartCatalog catalog,VesselBuild plan,VesselBuild? read,int slot)
    {
        var part=catalog.Find(plan.Kind,plan[slot]);
        var changed=read is not null && read[slot]!=plan[slot];
        var side=Math.Max(36,ImGui.GetTextLineHeight()*2.4f);
        ImGui.PushID($"VesselSlot{plan.Kind}{slot}");
        try
        {
            var start=ImGui.GetCursorScreenPos();
            var definition=part is null ? null:itemCatalog.State.Catalog?.Find(part.ItemId);
            if(definition is null){ImGui.Dummy(new Vector2(side));ImGui.GetWindowDrawList().AddRect(start,start+new Vector2(side),ImGui.GetColorU32(Muted),4,ImDrawFlags.None,1.5f);}
            else DrawItemIcon(definition,new Vector2(side));
            if(changed)ImGui.GetWindowDrawList().AddRect(start-new Vector2(1),start+new Vector2(side+1),ImGui.GetColorU32(UI.FfxivTheme.Gold),4,ImDrawFlags.None,2);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left))ImGui.OpenPopup("VesselPick");
            ImGui.SameLine();ImGui.BeginGroup();
            ImGui.TextDisabled(localization.Get($"VesselBuilds.Slot{plan.Kind}{slot}"));
            ImGui.PushStyleColor(ImGuiCol.Text,changed ? UI.FfxivTheme.Gold:Text);
            var clicked=ImGui.Selectable($"{part?.Name ?? "—"}##VesselPartName",false);
            ImGui.PopStyleColor();
            if(clicked)ImGui.OpenPopup("VesselPick");
            if(part is not null)ImGui.TextDisabled(localization.Format("VesselBuilds.PartLine",part.Rank,part.Components,part.Repair));
            ImGui.EndGroup();
            DrawVesselPick(catalog,plan,slot,part);
        }
        finally{ImGui.PopID();}
        ImGui.Spacing();
    }

    private void DrawVesselPick(VesselPartCatalog catalog,VesselBuild plan,int slot,VesselPart? current)
    {
        if(!ImGui.BeginPopup("VesselPick"))return;
        try
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get($"VesselBuilds.Slot{plan.Kind}{slot}"));
            var names=catalog.StatNames(plan.Kind);
            foreach(var option in catalog.ForSlot(plan.Kind,slot))
            {
                var locked=option.Rank>plan.Rank;
                if(locked)ImGui.PushStyleColor(ImGuiCol.Text,Muted);
                if(ImGui.Selectable($"{option.Name}##Part{option.Row}",option.Row==plan[slot]))
                {
                    StoreVesselPlan(buildVesselKey!,plan.With(slot,option.Row));buildSearchKey=null;
                    ImGui.CloseCurrentPopup();
                }
                if(locked)ImGui.PopStyleColor();
                if(ImGui.IsItemHovered())
                    WrappedTooltip(string.Join("\n",Enumerable.Range(0,5).Select(index=>$"{(index<names.Count ? names[index]:"")} : {option.Stats[index]}"))
                        +"\n"+localization.Format("VesselBuilds.PartLine",option.Rank,option.Components,option.Repair));
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("VesselBuilds.PartLine",option.Rank,option.Components,option.Repair));
                // What it changes against the planned part, stat by stat.
                for(var index=0;index<5;index++)
                {
                    var delta=option.Stats[index]-(current?.Stats[index] ?? 0);
                    if(delta==0)continue;
                    ImGui.SameLine();
                    ImGui.TextColored(delta>0 ? Success:Error,$"{(delta>0 ? "+":"")}{delta} {localization.Get("VesselBuilds.Short"+index)}");
                }
            }
        }
        finally{ImGui.EndPopup();}
    }

    // Submarine route check: distance against range, voyage time with the planned speed, breakpoints reached, wear; best builds.
    private void DrawBuildRoute(VesselPartCatalog catalog,VesselBuild plan,BuildSummary planned,VoyageVessel? vessel)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("VesselBuilds.RouteTitle"),ImGuiTreeNodeFlags.DefaultOpen))return;
        var routes=SubmarineCatalog?.Invoke();
        if(vessel is { Points.Count:>0 } && ImGui.SmallButton(localization.Get("VesselBuilds.CurrentRoute"))){buildRoute=[..vessel.Points];buildSearchKey=null;}
        if(vessel is { Points.Count:>0 })Tip("VesselBuilds.CurrentRouteTip");
        if(routes is null && SubmarineCatalogFailed?.Invoke()==true){ImGui.TextDisabled(localization.Get("RoutePlanner.Unavailable"));return;}
        if(buildRoute.Count==0 || routes is null){ImGui.TextWrapped(localization.Get("VesselBuilds.NoRoute"));return;}
        var sectors=buildRoute.Select(id=>routes.Sectors.GetValueOrDefault(id)).OfType<SubmarineSector>().ToArray();
        if(sectors.Length!=buildRoute.Count || sectors.Length==0){ImGui.TextWrapped(localization.Get("VesselBuilds.NoRoute"));return;}
        var start=routes.SectorsOf(sectors[0].Map).FirstOrDefault(sector=>sector.Start);
        if(start is null){ImGui.TextWrapped(localization.Get("VesselBuilds.NoRoute"));return;}
        var (distance,_,order)=VoyagePlanner.BestOrder(start,sectors,Math.Max(1,planned.Stats.Speed));
        int SecondsOf(int speed)=>VoyagePlanner.Measure(start,order,speed).Seconds;
        ImGui.TextUnformatted(string.Join(" › ",order.Select(sector=>sector.Name)));
        // 0.315 review: the game refuses a route above the vessel's rank.
        var needed=VesselBuilds.RouteRank(sectors.Select(sector=>sector.RankReq));
        if(plan.Rank<needed)ImGui.TextColored(Error,localization.Format("VesselBuilds.RouteRankTooLow",needed,plan.Rank));
        var breakpoint=VesselBuilds.RouteBreakpoint(catalog.Breakpoints,buildRoute);
        var time=TimeSpan.FromSeconds(SecondsOf(Math.Max(1,planned.Stats.Speed)));
        ImGui.TextColored(distance>planned.Stats.Range ? Error:Success,localization.Format("VesselBuilds.Distance",distance,planned.Stats.Range));
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format("VesselBuilds.Duration",(int)time.TotalHours,time.Minutes));
        if(breakpoint is null)ImGui.TextDisabled(localization.Get("VesselBuilds.BreakpointsUnknown"));
        else
        {
            var names=catalog.StatNames(VesselKind.Submarine);
            var surveillance=VesselBuilds.Surveillance(planned.Stats.Surveillance,breakpoint);
            var retrieval=VesselBuilds.Retrieval(planned.Stats.Retrieval,breakpoint);
            var favor=VesselBuilds.FavorReached(planned.Stats.Favor,breakpoint)==true;
            ImGui.TextColored(surveillance==SurveillanceTier.High ? Success:UI.FfxivTheme.Gold,localization.Format("VesselBuilds.SurveillanceLine",names.ElementAtOrDefault(0) ?? "",planned.Stats.Surveillance,breakpoint.T2,breakpoint.T3,localization.Get("VesselBuilds.Tier"+surveillance)));
            Tip("VesselBuilds.SurveillanceTip");
            ImGui.TextColored(retrieval==RetrievalTier.Optimal ? Success:UI.FfxivTheme.Gold,localization.Format("VesselBuilds.RetrievalLine",names.ElementAtOrDefault(1) ?? "",planned.Stats.Retrieval,breakpoint.Normal,breakpoint.Optimal,localization.Get("VesselBuilds.Retrieval"+retrieval)));
            Tip("VesselBuilds.RetrievalTip");
            ImGui.TextColored(favor ? Success:UI.FfxivTheme.Gold,localization.Format("VesselBuilds.FavorLine",names.ElementAtOrDefault(4) ?? "",planned.Stats.Favor,breakpoint.Favor,localization.Get(favor ? "VesselBuilds.FavorYes":"VesselBuilds.FavorNo")));
            Tip("VesselBuilds.FavorTip");
            // 0.334 (DEC-023, point 1): the community table is kept; on its 19 sectors below every record, the tier is said unconfirmed.
            if(VesselBuilds.Unconfirmed(buildRoute))
            {
                ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("VesselBuilds.Unconfirmed"));
                Tip("VesselBuilds.UnconfirmedTip");
            }
        }
        var damage=VesselBuilds.DamagePerVoyage(catalog,plan,sectors.Select(sector=>sector.RankReq));
        if(VesselBuilds.VoyagesUntilRepair(damage) is { } voyages)
        {
            ImGui.TextDisabled(localization.Format("VesselBuilds.Wear",voyages,damage));
            Tip("VesselBuilds.WearTip");
        }
        DrawBestBuilds(catalog,plan,distance,breakpoint,SecondsOf,sectors.Select(sector=>sector.RankReq).ToArray());
    }

    private void DrawBestBuilds(VesselPartCatalog catalog,VesselBuild plan,int distance,SectorBreakpoint? breakpoint,Func<int,int> secondsOf,int[] sectorRanks)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("VesselBuilds.BestTitle"),ImGuiTreeNodeFlags.DefaultOpen))return;
        // Wanted tiers (none = no requirement); unknown breakpoints can only ask for range.
        ImGui.BeginDisabled(breakpoint is null);
        ImGui.SetNextItemWidth(150);
        if(ImGui.BeginCombo(localization.Get("VesselBuilds.WantSurveillance")+"##WantSurv",localization.Get("VesselBuilds.Want"+buildSurveillance)))
        {
            try{foreach(var tier in new[]{SurveillanceTier.High,SurveillanceTier.Mid,SurveillanceTier.Unknown})if(ImGui.Selectable(localization.Get("VesselBuilds.Want"+tier),tier==buildSurveillance))buildSurveillance=tier;}
            finally{ImGui.EndCombo();}
        }
        Tip("VesselBuilds.WantSurveillanceTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(150);
        if(ImGui.BeginCombo(localization.Get("VesselBuilds.WantRetrieval")+"##WantRet",localization.Get("VesselBuilds.WantRetrieval"+buildRetrieval)))
        {
            try{foreach(var tier in new[]{RetrievalTier.Optimal,RetrievalTier.Normal,RetrievalTier.Unknown})if(ImGui.Selectable(localization.Get("VesselBuilds.WantRetrieval"+tier),tier==buildRetrieval))buildRetrieval=tier;}
            finally{ImGui.EndCombo();}
        }
        Tip("VesselBuilds.WantRetrievalTip");
        ImGui.SameLine();ImGui.Checkbox(localization.Get("VesselBuilds.WantFavor")+"##WantFavor",ref buildFavor);
        Tip("VesselBuilds.WantFavorTip");
        ImGui.EndDisabled();
        var surveillance=breakpoint is null ? SurveillanceTier.Unknown:buildSurveillance;
        var retrieval=breakpoint is null ? RetrievalTier.Unknown:buildRetrieval;
        var favor=breakpoint is not null && buildFavor;
        // New search when an input changed; the running one is cancelled (10 000 combinations at most, on a worker).
        var key=$"{plan.Rank}|{distance}|{string.Join(",",buildRoute)}|{surveillance}|{retrieval}|{favor}";
        if(key!=buildSearchKey)
        {
            buildSearchKey=key;buildSearchCancel?.Cancel();buildSearchCancel?.Dispose();buildSearchCancel=new CancellationTokenSource();
            var token=buildSearchCancel.Token;var rank=plan.Rank;
            buildSearch=Task.Run(()=>VesselBuilds.BestBuilds(catalog,rank,distance,breakpoint,surveillance,retrieval,favor,secondsOf,sectorRanks,15,token),token);
        }
        if(buildSearch is { IsCompleted:true } done)
        {
            if(done.IsCompletedSuccessfully)buildCandidates=done.Result;
            else if(done.Exception is { } error)log.Warning(error,"Submarine builds could not be searched.");
            buildSearch=null;
        }
        if(buildSearch is not null){ImGui.TextDisabled(localization.Get("VesselBuilds.Searching"));return;}
        if(buildCandidates.Count==0){ImGui.TextDisabled(localization.Get("VesselBuilds.NoBuild"));return;}
        if(!ImGui.BeginTable("BestBuilds",9,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("VesselBuilds.Identifier"));
            for(var index=0;index<5;index++)ImGui.TableSetupColumn(localization.Get("VesselBuilds.Short"+index));
            ImGui.TableSetupColumn(localization.Get("RoutePlanner.ColumnDuration"));ImGui.TableSetupColumn(localization.Get("VesselBuilds.Repair"));ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            var row=0;
            foreach(var candidate in buildCandidates)
            {
                row++;
                ImGui.TableNextRow();ImGui.TableNextColumn();
                ImGui.TextUnformatted(VesselBuilds.Identifier(catalog,candidate.Build));
                if(ImGui.IsItemHovered())WrappedTooltip(string.Join("\n",Enumerable.Range(0,4).Select(slot=>$"{localization.Get($"VesselBuilds.SlotSubmarine{slot}")} : {catalog.Find(VesselKind.Submarine,candidate.Build[slot])?.Name}"))
                    +"\n"+localization.Format("VesselBuilds.CandidateCost",candidate.Summary.Cost,candidate.Summary.Capacity)
                    +(VesselBuilds.VoyagesUntilRepair(candidate.DamagePerVoyage) is { } voyages ? "\n"+localization.Format("VesselBuilds.Wear",voyages,candidate.DamagePerVoyage):""));
                for(var index=0;index<5;index++){ImGui.TableNextColumn();ImGui.TextUnformatted(candidate.Summary.Stats[index].ToString());}
                ImGui.TableNextColumn();ImGui.TextUnformatted(HoursMinutes(TimeSpan.FromSeconds(candidate.Seconds)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(candidate.Summary.Repair.ToString());
                ImGui.TableNextColumn();
                if(ImGui.SmallButton(localization.Get("VesselBuilds.Apply")+$"##Apply{row}")){StoreVesselPlan(buildVesselKey!,candidate.Build);}
                Tip("VesselBuilds.ApplyTip");
            }
        }
        finally{ImGui.EndTable();}
    }
}
