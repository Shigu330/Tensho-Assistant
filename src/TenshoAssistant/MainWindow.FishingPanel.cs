using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// In-game fishing help (0.226): while the fisher is fishing, the nearest fishing spot of the zone with its fish (game data) and, for each
/// fish, the bait routes community data gives for that spot (Garland, one request at a time, cached), time window and weather.
/// </summary>
internal sealed partial class MainWindow
{
    private Community.GatherBuddyFishService? gatherBuddyFish;
    private Community.FishBiteTimeService? biteTimes;
    internal void ConfigureBiteTimes(Community.FishBiteTimeService service)=>biteTimes=service;
    // Stopwatch of the fishing help (0.231): starts when fishing starts, restartable by hand for each cast.
    private DateTimeOffset? fishingSince;private bool fishingBefore;
    internal void ConfigureGatherBuddyFish(Community.GatherBuddyFishService service)=>gatherBuddyFish=service;
    private Func<(uint Job,uint Territory,float X,float Z,bool Fishing)?>? fishingNow;
    // Opened by the button or « /tensho peche » (0.228): shown whatever the job and even when not fishing, until closed.
    private bool fishingPanelManual;
    internal void ToggleFishingPanel()=>fishingPanelManual=!fishingPanelManual;
    private Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?>? fishingSpotAt;
    internal void ConfigureFishingHelp(Func<(uint Job,uint Territory,float X,float Z,bool Fishing)?> now,Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?> spotAt)
    {fishingNow=now;fishingSpotAt=spotAt;}

    internal void DrawFishingPanel()
    {
        if(windowDisposed || fishingNow?.Invoke() is not { } now)return;
        var automatic=configuration.FishingPanel && now.Fishing && now.Job==18;
        if(!automatic && !fishingPanelManual)return;
        var spot=fishingSpotAt?.Invoke(now.Territory,now.X,now.Z);
        using var theme=UI.FfxivTheme.Apply();
        var open=true;
        var title=localization.Format("FishingPanel.Title",spot?.Name ?? localization.Get("FishingPanel.NoSpotTitle"))+"###TenshoFishingPanel";
        var visible=fishingPanelManual ? ImGui.Begin(title,ref open,ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize)
            :ImGui.Begin(title,ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize);
        try
        {
            if(!open){fishingPanelManual=false;return;}
            if(!visible)return;
            if(spot is not { } here){ImGui.TextWrapped(localization.Get("FishingPanel.NoSpot"));return;}
            ImGui.TextDisabled(localization.Get(now.Fishing ? "FishingPanel.Fishing":"FishingPanel.NotFishing"));
            if(now.Fishing && !fishingBefore)fishingSince=DateTimeOffset.UtcNow;
            if(!now.Fishing)fishingSince=null;
            fishingBefore=now.Fishing;
            var elapsed=fishingSince is { } since ? (int)(DateTimeOffset.UtcNow-since).TotalSeconds:(int?)null;
            if(elapsed is { } seconds)
            {
                ImGui.TextColored(Accent,localization.Format("FishingPanel.Elapsed",seconds));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("FishingPanel.Restart")))fishingSince=DateTimeOffset.UtcNow;
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FishingPanel.RestartHelp"));
            }
            var bites=biteTimes?.Get(here.Id);
            var items=itemCatalog.State.Catalog;
            RefreshRecipeHoldingsCache();
            gatherBuddyFish?.Request();
            // 0.232: one row per fish (fish, bait route, bite, bite time, conditions); every detail stays in the tooltips.
            var rows=here.Fish.Select(fishId=>FishRow(fishId,here.Id)).ToArray();
            var withConditions=rows.Any(row=>row.Conditions.Length>0);
            if(ImGui.BeginTable("FishingRows",withConditions ? 5:4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingFixedFit|ImGuiTableFlags.BordersInnerV))
            {
                try
                {
                    foreach(var row in rows)
                    {
                        var fish=items?.Find(row.FishId);
                        ImGui.PushID((int)row.FishId);
                        try
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            DrawItemIcon(fish,new Vector2(22));var clicked=ImGui.IsItemClicked();DrawCollectionMark(fish);ImGui.SameLine();
                            var stock=GetRecipeHoldings(row.FishId);
                            clicked|=ImGui.Selectable($"{fish?.Name ?? row.FishId.ToString()}{(stock.HasObservation && stock.Total>0 ? $" ×{stock.Total:N0}":"")}##Fish",false,ImGuiSelectableFlags.None,new Vector2(0,22));
                            if(ImGui.IsItemHovered())DrawItemInfoTooltip(row.FishId);
                            ItemLinksMenu($"FishMenu{row.FishId}",fish);
                            if(clicked)SelectItem(row.FishId);
                            ImGui.TableNextColumn();
                            for(var step=0;step<row.Route.Count;step++)
                            {
                                if(step>0){ImGui.SameLine(0,2);ImGui.TextDisabled("›");ImGui.SameLine(0,2);}
                                var bait=items?.Find(row.Route[step]);
                                DrawItemIcon(bait,new Vector2(22));
                                if(ImGui.IsItemHovered())WrappedTooltip((bait?.Name ?? row.Route[step].ToString())+(step==0 ? "":"\n"+localization.Get("FishingSources.Mooch"))+"\n"+localization.Get("FishingPanel.RightClick"));
                                ItemLinksMenu($"BaitMenu{row.FishId}/{step}",bait);
                            }
                            if(row.Route.Count==0)ImGui.TextDisabled("?");
                            ImGui.TableNextColumn();
                            if(row.Bite is { } bite)
                            {
                                var colour=bite switch{"Weak"=>new Vector4(0.45f,0.85f,0.45f,1),"Strong"=>new Vector4(0.95f,0.80f,0.30f,1),_=>new Vector4(0.95f,0.35f,0.35f,1)};
                                ImGui.TextColored(colour,(bite switch{"Weak"=>"!","Strong"=>"!!",_=>"!!!"})+(row.HookSet is { } hook ? " "+localization.Get("FishingPanel.HookShort"+hook):""));
                                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FishingPanel.Bite"+bite)+(row.HookSet is { } hookFull ? " · "+localization.Get("FishingPanel.Hook"+hookFull):""));
                            }
                            else ImGui.TextDisabled("—");
                            ImGui.TableNextColumn();
                            if(bites?.Times?.GetValueOrDefault(row.FishId) is { } time)
                            {
                                var inside=elapsed is { } spent && spent>=time.Minimum && spent<=time.Maximum;
                                var text=localization.Format("FishingPanel.BiteShort",time.Minimum,time.Maximum,time.Usual);
                                if(inside)ImGui.TextColored(Success,text);else ImGui.TextUnformatted(text);
                                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FishingPanel.BiteTime",time.Minimum,time.Maximum,time.Usual)+"\n"+localization.Format("FishingPanel.BiteTimeHelp",time.Reports));
                            }
                            else ImGui.TextDisabled(bites is { Loading:true } ? "…":"—");
                            if(withConditions)
                            {
                                ImGui.TableNextColumn();
                                if(row.Conditions.Length>0)
                                {
                                    ImGui.TextUnformatted(row.Conditions);
                                    if(ImGui.IsItemHovered())WrappedTooltip(row.ConditionsDetail);
                                }
                            }
                        }
                        finally{ImGui.PopID();}
                    }
                }
                finally{ImGui.EndTable();}
            }
            // Sources in a tooltip, so the window keeps the width of its table.
            ImGui.TextDisabled(localization.Get("FishingPanel.SourcesShort"));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(gatherBuddyFish?.State.Data is not null ? "FishingPanel.FooterGatherBuddy":"FishingPanel.Footer"));
            if(gatherBuddyFish?.State is { Loading:true })ImGui.TextDisabled(localization.Get("FishingPanel.GatherBuddyLoading"));
        }
        finally{ImGui.End();}
    }

    // Button that opens or closes the fishing help (Gathering pages).
    private void DrawFishingPanelButton()
    {
        if(ImGui.SmallButton(localization.Get(fishingPanelManual ? "FishingPanel.Close":"FishingPanel.Open")+"##FishingPanelButton"))ToggleFishingPanel();
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FishingPanel.OpenHelp"));
    }

    // One fish of the panel: bait route, bite, hookset and conditions (short text + full tooltip), GatherBuddy first, else Garland.
    private (uint FishId,IReadOnlyList<uint> Route,string? Bite,string? HookSet,string Conditions,string ConditionsDetail) FishRow(uint fishId,uint spotId)
    {
        var items=itemCatalog.State.Catalog;var catalog=weather.State.Catalog;
        string Weathers(IEnumerable<uint> ids)=>string.Join(", ",ids.Select(id=>catalog?.Weather(id)?.Name ?? id.ToString()));
        if(gatherBuddyFish?.State.Data is { } data && data.Find(fishId) is { } known)
        {
            var parts=new List<string>();var detail=new List<string>();
            if(known.StartMinute is { } start && known.EndMinute is { } stop)
            {var window=$"{start/60:00}:{start%60:00}–{stop/60%24:00}:{stop%60:00} ET";parts.Add(window);detail.Add(window);}
            if(known.Transitions.Count>0){parts.Add(Weathers(known.Transitions)+" ›");detail.Add(localization.Format("FishingPanel.Previous",Weathers(known.Transitions)));}
            if(known.Weathers.Count>0){parts.Add(Weathers(known.Weathers));detail.Add(localization.Format("FishingPanel.Weather",Weathers(known.Weathers)));}
            if(known.Lure is { } lure){parts.Add(lure);detail.Add(localization.Format("FishingPanel.Lure",lure));}
            if(known.Snag){parts.Add(localization.Get("FishingPanel.SnagShort"));detail.Add(localization.Get("FishingPanel.Snag"));}
            if(known.Predators.Count>0)
            {
                parts.Add(localization.Format("FishingPanel.PredatorsShort",known.Predators.Count));
                detail.Add(localization.Format("FishingPanel.Predators",string.Join(", ",known.Predators.Select(pair=>$"{items?.Find(pair.Fish)?.Name ?? pair.Fish.ToString()} ×{pair.Count}"))));
            }
            return (fishId,data.Route(fishId),known.Bite,known.HookSet,string.Join(" · ",parts),string.Join("\n",detail));
        }
        var here=itemSources?.Get(fishId).List?.Fishing?.Spots.FirstOrDefault(entry=>entry.SpotId==spotId);
        var route=here?.BaitChains is { Count:>0 } chains ? chains[0]:(IReadOnlyList<uint>)[];
        var conditions=new List<string>();
        if(here?.Conditions is { Error:null } found)
        {
            if(found.Timing.Kind==Gathering.GatheringScheduleKind.Limited)conditions.Add(found.Timing.Label);
            if(found.Weather is { Count:>0 } names)conditions.Add(string.Join(", ",names));
        }
        return (fishId,route,null,null,string.Join(" · ",conditions),string.Join("\n",conditions));
    }
}
