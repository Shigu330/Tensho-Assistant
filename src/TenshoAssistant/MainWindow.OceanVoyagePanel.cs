using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-07 (0.418, user's choice in GAPS: all at once): during an ocean fishing voyage (setting FishingPanel, as the fishing help), a
/// panel with the stop (place, moment, spectral current), its fish with the GatherBuddy bait and bite, the next stop, the three
/// missions (progress / target) and the points. Game data and GatherBuddy (Apache-2.0); fish not documented by GatherBuddy show « ? ».
/// </summary>
internal sealed partial class MainWindow
{
    internal OceanVoyageReader? OceanVoyage { get; set; }
    internal Func<OceanVoyageData?>? OceanVoyageData { get; set; }
    /// <summary>
    /// X-75.6 (0.880): the community timetable checked against the game once per voyage: the route the game says aboard must be one of the two routes the loop
    /// (start and 2-hour step read in GatherBuddy) gives for that departure. The result is logged; a mismatch always, a match in developer mode.
    /// X-85 V1 (0.917): the departure is also saved as a sighting (slot, route): the sightings settle each port's shift (OceanSchedule.State);
    /// the log gives the slot, the loop row and the shifts still possible, for the evidence.
    /// </summary>
    private uint oceanRouteChecked;
    /// <summary>0.1095: the timetable once read in the background (null until then): the check waits for it instead of reading the sheets in the frame.</summary>
    internal Func<OceanSchedule?>? OceanTimetableNow { get; set; }
    internal (int Matches,int Mismatches) OceanScheduleChecks { get; private set; }
    private void CheckOceanSchedule(OceanVoyageFrame frame)
    {
        if(frame.RouteId==0 || frame.RouteId==oceanRouteChecked || (OceanTimetableNow ?? OceanTimetable)?.Invoke() is not { } timetable)return;
        oceanRouteChecked=frame.RouteId;
        // 0.1094: once per voyage, a reload of the plugin included (the voyage record keeps it).
        if(frame.ScheduleChecked)return;
        OceanVoyage?.MarkScheduleChecked();
        var slot=(long)Math.Floor((DateTimeOffset.UtcNow+OceanSchedule.Boarding-OceanSchedule.LoopStart)/OceanSchedule.Step);
        var expected=timetable.At(Math.Max(0,slot));
        if(expected.Aldenard?.Id==frame.RouteId || expected.Othard?.Id==frame.RouteId)
        {
            OceanScheduleChecks=(OceanScheduleChecks.Matches+1,OceanScheduleChecks.Mismatches);
            if(developerMode)log.Information("Ocean schedule check: route {0} matches departure {1:u}.",frame.RouteId,expected.At);
        }
        else
        {
            OceanScheduleChecks=(OceanScheduleChecks.Matches,OceanScheduleChecks.Mismatches+1);
            log.Warning("Ocean schedule check: the game says route {0}, the timetable gives {1} / {2} for departure {3:u}.",frame.RouteId,expected.Aldenard?.Id ?? 0,expected.Othard?.Id ?? 0,expected.At);
        }
        RecordOceanSighting(timetable,Math.Max(0,slot),frame.RouteId);
    }

    private void RecordOceanSighting(OceanSchedule timetable,long slot,uint route)
    {
        if(timetable.PortOf(route) is not { } port){log.Warning("Ocean schedule: route {0} is not in one port's column of the loop; sighting not kept.",route);return;}
        var sightings=configuration.OceanSightings ??= [];
        if(!sightings.Any(sighting=>sighting.Slot==slot && sighting.Route==route))
        {
            sightings.Add(new OceanSighting{Slot=slot,Route=route});
            if(sightings.Count>64)sightings.RemoveAt(0);
            try{save();}catch(Exception error){log.Warning(error,"Ocean fishing sighting could not be saved.");}
            oceanPortStates=null;
        }
        var state=timetable.State(port,sightings);
        log.Information("Ocean schedule sighting: slot {0}, loop row {1}, port {2}, route {3}; {4} sighting(s) of this port, {5} shift(s) still possible ({6}): {7}.",
            slot,slot%Math.Max(1,timetable.Count),port,route,state.Sightings,state.Candidates.Count,state.Check,string.Join(", ",state.Candidates.Take(24)));
    }

    // 0.1095 (user report: TenshoOceanVoyage 160 to 260 ms per frame after each reload): what the panel shows of each fish that does not change
    // from one frame to the next (the GatherBuddy row, the points seen in game) and the fish caught before a spectral current at this stop, built
    // once and kept three seconds instead of being rebuilt on every frame for every fish.
    private readonly Dictionary<(uint Fish,uint Spot),((IReadOnlyList<uint> Route,string? Bite,string? HookSet) Row,Discoveries.DiscoveryFact? Points)> oceanFishCache=[];
    private IReadOnlyList<Discoveries.DiscoveryFact> oceanSpectralSeen=[];private uint oceanSpectralFor;private long oceanCacheAt=-100_000;

    private void OceanCacheTick()
    {
        var now=Environment.TickCount64;
        if(now-oceanCacheAt<3_000)return;
        oceanCacheAt=now;oceanFishCache.Clear();oceanSpectralFor=0;
    }

    private ((IReadOnlyList<uint> Route,string? Bite,string? HookSet) Row,Discoveries.DiscoveryFact? Points) OceanFish(uint fishId,uint spotId)
    {
        if(oceanFishCache.TryGetValue((fishId,spotId),out var known))return known;
        var row=FishRow(fishId,spotId);
        var points=DiscoveryStore is { } pointsStore && DiscoveriesLoaded ? pointsStore.Find(Discoveries.DiscoveryKind.FishPoints,fishId,1).FirstOrDefault():null;
        return oceanFishCache[(fishId,spotId)]=((row.Route,row.Bite,row.HookSet),points);
    }

    private IReadOnlyList<Discoveries.DiscoveryFact> OceanSpectralSeen(OceanStopView stop)
    {
        if(oceanSpectralFor==stop.Spot.Id+1)return oceanSpectralSeen;
        oceanSpectralFor=stop.Spot.Id+1;
        var stopFish=stop.Fish.ToHashSet();
        return oceanSpectralSeen=DiscoveryStore is { } seenStore && DiscoveriesLoaded ? seenStore.Find(Discoveries.DiscoveryKind.SpectralBefore,fish=>stopFish.Contains(fish),3):[];
    }

    /// <summary>0.1095: in developer mode, a part of the panel's frame for the « Slow frame » line (measured before and after in game).</summary>
    private void OceanPart(string part,Action draw)
    {
        var started=developerMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        try{draw();}
        finally{if(started!=0)SlowFrames.Add(ImGui.GetFrameCount(),part,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);}
    }

    // X-85 V2 (0.918): stopwatch of the voyage panel (started when a cast starts, as the usual fishing help, which is off at sea).
    private DateTimeOffset? oceanFishingSince;private bool oceanFishingBefore;

    /// <summary>
    /// X-85 V2 (0.918): the fish of one ocean spot as in the usual fishing help: icon and stock, bait route with icons (green frame on the bait in
    /// hand, mooches), bite and hookset in colour, bite time of that spot (Teamcraft) against the stopwatch. Baits and bites: GatherBuddy.
    /// </summary>
    private void DrawOceanFishTable(string id,IReadOnlyList<uint> fishList,uint spotId,uint? inHand,int? elapsed,Func<uint,string?,IReadOnlyList<string>>? countsFor=null)
    {
        if(fishList.Count==0 || !ImGui.BeginTable(id,4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingFixedFit|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            var items=itemCatalog.State.Catalog;var bites=biteTimes?.Get(spotId);
            ImGui.TableSetupColumn(localization.Get("OceanVoyage.Fish"));ImGui.TableSetupColumn(localization.Get("OceanVoyage.Bait"));
            ImGui.TableSetupColumn(localization.Get("OceanVoyage.Bite"));ImGui.TableSetupColumn(localization.Get("OceanVoyage.BiteTime"));
            ImGui.TableHeadersRow();
            foreach(var fishId in fishList)
            {
                var cached=OceanFish(fishId,spotId);var row=cached.Row;var fish=items?.Find(fishId);
                ImGui.PushID((int)fishId);
                try
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    var stock=GetRecipeHoldings(fishId);
                    ItemCell(fish,fishId,22,$"{fish?.Name ?? fishId.ToString()}{(stock.HasObservation && stock.Total>0 ? $" ×{stock.Total:N0}":"")}");
                    // X-85 V2 (0.964): points this fish brings, as seen in game (one catch), and what it earned this voyage at this stop.
                    var seenPoints=cached.Points;
                    var earned=OceanVoyage?.Frame?.ZoneFish.FirstOrDefault(entry=>entry.Item==fishId) is { Item:not 0 } here ? here:default;
                    if(seenPoints is not null || earned.Points>0)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(Muted,seenPoints is { } seen ? localization.Format("OceanVoyage.FishPoints",LevelRange(seen.LevelMin,seen.LevelMax)):"");
                        if(earned.Points>0)TipText(localization.Format("OceanVoyage.FishEarned",earned.Nq+earned.Hq,earned.Points.ToString("N0")));
                        else TipText(localization.Get("OceanVoyage.FishPointsTip"));
                    }
                    // X-85 V5 (0.963): a fish that needs Fisher's Intuition (GatherBuddy predators): an eye, the fish to catch first in its tooltip.
                    if(gatherBuddyFish?.State.Data?.Find(fishId) is { Predators.Count:>0 } needs)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                        try{ImGui.TextColored(Warning,Dalamud.Interface.FontAwesomeExtensions.ToIconString(Dalamud.Interface.FontAwesomeIcon.Eye));}
                        finally{ImGui.PopFont();}
                        var predators=localization.Format("FishingPanel.Predators",string.Join(", ",needs.Predators.Select(pair=>$"{items?.Find(pair.Fish)?.Name ?? pair.Fish.ToString()} ×{pair.Count}")));
                        TipText(needs.Intuition is { } seconds ? predators+"\n"+localization.Format("FishingPanel.Intuition",seconds):predators);
                    }
                    // X-85 V3 (0.961, user request): a mark on the fish that count for a mission not done yet, the missions in its tooltip.
                    if(countsFor?.Invoke(fishId,row.Bite) is { Count:>0 } wanted)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                        try{ImGui.TextColored(Accent,Dalamud.Interface.FontAwesomeExtensions.ToIconString(Dalamud.Interface.FontAwesomeIcon.Bullseye));}
                        finally{ImGui.PopFont();}
                        TipText(localization.Format("OceanVoyage.ForMission",string.Join("\n",wanted)));
                    }
                    ImGui.TableNextColumn();
                    for(var step=0;step<row.Route.Count;step++)
                    {
                        if(step>0){ImGui.SameLine(0,2);ImGui.TextDisabled("›");ImGui.SameLine(0,2);}
                        var bait=items?.Find(row.Route[step]);
                        DrawItemIcon(bait,new Vector2(22));
                        if(step==0 && inHand==row.Route[0])
                            ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin()-new Vector2(1),ImGui.GetItemRectMax()+new Vector2(1),ImGui.GetColorU32(Success),3,ImDrawFlags.None,2);
                        if(ImGui.IsItemHovered())WrappedTooltip((bait?.Name ?? row.Route[step].ToString())+(step==0 ? "":"\n"+localization.Get("FishingSources.Mooch")));
                    }
                    if(row.Route.Count==0)ImGui.TextDisabled("?");
                    ImGui.TableNextColumn();
                    var bite=(row.Bite is "Weak" or "Strong" or "Legendary") ? row.Bite:null;var hook=(row.HookSet is "Precise" or "Powerful") ? row.HookSet:null;
                    if(bite is not null || hook is not null)
                    {
                        var colour=bite switch{"Weak"=>Success,"Strong"=>Warning,"Legendary"=>Error,_=>Muted};
                        ImGui.TextColored(colour,(bite switch{"Weak"=>"!","Strong"=>"!!","Legendary"=>"!!!",_=>"?"})+(hook is not null ? " "+localization.Get("FishingPanel.HookShort"+hook):""));
                        TipText(localization.Get(bite is not null ? "FishingPanel.Bite"+bite:"FishingPanel.BiteUnknown")+(hook is not null ? " · "+localization.Get("FishingPanel.Hook"+hook):""));
                    }
                    else ImGui.TextDisabled("?");
                    ImGui.TableNextColumn();
                    if(bites?.Times?.GetValueOrDefault(fishId) is { } time)
                    {
                        var inside=elapsed is { } spent && spent>=time.Minimum && spent<=time.Maximum;
                        var text=localization.Format("FishingPanel.BiteShort",time.Minimum,time.Maximum,time.Usual);
                        // X-98 (0.958, user request): green while the bite time runs, red once the stopwatch has passed it.
                        if(inside)ImGui.TextColored(Success,text);else if(elapsed is { } passed && passed>time.Maximum)ImGui.TextColored(Error,text);else ImGui.TextUnformatted(text);
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FishingPanel.BiteTime",time.Minimum,time.Maximum,time.Usual)+"\n"+localization.Format("FishingPanel.BiteTimeHelp",time.Reports));
                    }
                    else ImGui.TextDisabled(bites is { Loading:true } ? "…":"—");
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }

    internal void DrawOceanVoyagePanel()
    {
        if(OceanVoyage?.Frame is { } aboard)CheckOceanSchedule(aboard);
        if(windowDisposed || !configuration.FishingPanel || OceanVoyage?.Frame is not { } frame || frame.Status==4 || OceanVoyageData?.Invoke() is not { } data)return;
        Panels?.Show("TenshoOceanVoyage",localization.Get("OceanVoyage.Title"),ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(60,300),ImGuiCond.FirstUseEver);},()=>
        {
            if(OceanVoyages.Current(data,frame) is not { } stop){ImGui.TextDisabled(localization.Get("OceanVoyage.Unknown"));return;}
            OceanCacheTick();
            ImGui.TextColored(Accent,localization.Format("OceanVoyage.Stop",stop.Index+1,stop.Spot.Place,localization.Get("OceanFishing.Time"+stop.Time)));
            if(stop.Spectral)ImGui.TextColored(UI.FfxivTheme.Success,localization.Get("OceanVoyage.Spectral"));
            if(stop.NextSpot is { } next)ImGui.TextDisabled(localization.Format("OceanVoyage.Next",next.Place,localization.Get("OceanFishing.Time"+stop.NextTime)));
            gatherBuddyFish?.Request();
            RefreshRecipeHoldingsCache();
            // X-85 V2 (0.918): stopwatch of the cast, intuition and bait in hand, as the usual fishing help.
            var fishingState=fishingNow?.Invoke()?.Fishing==true;
            if(fishingState && !oceanFishingBefore)oceanFishingSince=DateTimeOffset.UtcNow;
            if(!fishingState)oceanFishingSince=null;
            else oceanFishingSince=FromCast(oceanFishingSince,FishingCastAt?.Invoke());
            oceanFishingBefore=fishingState;
            var elapsed=oceanFishingSince is { } since ? (int)(DateTimeOffset.UtcNow-since).TotalSeconds:(int?)null;
            if(elapsed is { } seconds)
            {
                ImGui.TextColored(Accent,localization.Format("FishingPanel.Elapsed",seconds));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("FishingPanel.Restart")+"##OceanRestart"))oceanFishingSince=DateTimeOffset.UtcNow;
                Tip("FishingPanel.RestartHelp");
            }
            if(FishingIntuitionLeft?.Invoke() is { } intuitionLeft)
                ImGui.TextColored(Accent,localization.Format("FishingPanel.IntuitionLeft",(int)intuitionLeft/60,(int)intuitionLeft%60));
            var items=itemCatalog.State.Catalog;
            var inHand=FishingBait?.Invoke() is > 0 and var baitId ? baitId:(uint?)null;
            var spotId=stop.Spectral ? stop.Spot.SpectralSpot:stop.Spot.MainSpot;
            if(inHand is { } held)
            {
                var baitItem=items?.Find(held);var baitStock=GetRecipeHoldings(held);
                ImGui.TextDisabled(localization.Get("FishingPanel.BaitInHand"));ImGui.SameLine();
                ItemCell(baitItem,held,20,$"{baitItem?.Name ?? held.ToString()}{(baitStock.HasObservation ? $" ×{baitStock.Total:N0}":"")}");
                ImGui.SameLine();
                ImGui.TextColored(Success,localization.Format("FishingPanel.BaitMatches",stop.Fish.Count(fish=>OceanFish(fish,spotId).Row.Route is [var first,..] && first==held)));
            }
            var missionsNow=frame.Missions.Where(mission=>mission.Type!=0 && data.Missions.ContainsKey(mission.Type))
                .Select(mission=>(data.Missions[mission.Type].Text,mission.Progress,data.Missions[mission.Type].Target,data.Missions[mission.Type].Criterion)).ToArray();
            IReadOnlyList<string> CountsFor(uint fish,string? bite)=>OceanMissionRules.CountsFor(fish,data.FishBonuses,bite,missionsNow);
            OceanPart("ocean fish table",()=>DrawOceanFishTable("OceanVoyageFish",stop.Fish,spotId,inHand,elapsed,CountsFor));
            // The spectral current's fish, highlighted, folded while the current is not there (they are never in the usual spot: IKDSpot.SpotSub).
            if(!stop.Spectral && stop.Spot.SpectralSpot!=0 && data.SpotFish.GetValueOrDefault(stop.Spot.SpectralSpot) is { Count:>0 } spectralFish)
            {
                ImGui.PushStyleColor(ImGuiCol.Text,Success);
                var open=ImGui.TreeNode(localization.Format("OceanVoyage.SpectralFish",spectralFish.Count)+"##OceanSpectralFish");
                ImGui.PopStyleColor();
                Tip("OceanVoyage.SpectralFishTip");
                if(open)
                {
                    try{OceanPart("ocean spectral table",()=>DrawOceanFishTable("OceanSpectralFish",spectralFish,stop.Spot.SpectralSpot,inHand,null,CountsFor));}
                    finally{ImGui.TreePop();}
                }
            }
            // X-85 V4 (0.962, user's priority 1): what was caught just before a spectral current at this stop (yours and shared), most seen first.
            if(!stop.Spectral)
            {
                var before=OceanSpectralSeen(stop);
                if(before.Count>0)
                {
                    ImGui.TextColored(Accent,localization.Get("OceanVoyage.SpectralSeenTitle"));
                    Tip("OceanVoyage.SpectralSeenTip");
                    var names=itemCatalog.State.Catalog;
                    foreach(var fact in before)
                        ImGui.TextUnformatted(localization.Format("OceanVoyage.SpectralSeen",names?.Find(fact.Subject)?.Name ?? $"#{fact.Subject}",
                            fact.Detail==0 ? "?":names?.Find(fact.Detail)?.Name ?? $"#{fact.Detail}",Discoveries.DiscoveryFact.TimesOf(fact)));
                }
            }
            var missions=OceanVoyages.Missions(data,frame);
            if(missions.Count>0)
            {
                ImGui.Separator();
                foreach(var (text,progress,target) in missions)
                    ImGui.TextColored(progress>=target && target>0 ? UI.FfxivTheme.Success:Muted,localization.Format("OceanVoyage.Mission",text,target>0 ? Math.Min(progress,target):progress,target));
            }
            ImGui.Separator();
            if(OceanVoyages.ShowTotals(frame) && OceanVoyages.Totals(frame) is var (points,caught))
                ImGui.TextUnformatted(localization.Format("OceanVoyage.Points",points.ToString("N0"),caught));
            ImGui.TextDisabled(localization.Get("FishingPanel.SourcesShort"));
            Tip("OceanVoyage.Credit");
        });
    }
}
