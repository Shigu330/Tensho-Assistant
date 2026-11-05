using TenshoAssistant.Gathering;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// In-game fishing help (0.226): while a fisher is fishing (setting FishingPanel), or opened by hand (button, « /tensho peche », 0.228), the
/// nearest fishing spot of the zone with its fish (game data) in a compact table (0.232): stock, bait route with mooches, bite and hookset,
/// Teamcraft bite time at this spot (0.231) against a stopwatch started with the cast, then ET window, weathers, lure, snagging and
/// predators — GatherBuddy data first (Apache-2.0, 0.227), Garland's item sources otherwise. Job, position and fishing state come from the
/// plugin (copied on the framework thread); community data are requested once and cached.
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
    // 0.398 (g, R-04): the bait in hand, copied on the framework thread (Gathering/FishingBaitReader); null = not a fisher or not read.
    internal Func<uint?>? FishingBait { get; set; }
    internal void ToggleFishingPanel()=>fishingPanelManual=!fishingPanelManual;
    private Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?>? fishingSpotAt;
    internal void ConfigureFishingHelp(Func<(uint Job,uint Territory,float X,float Z,bool Fishing)?> now,Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?> spotAt)
    {fishingNow=now;fishingSpotAt=spotAt;}

    // 0.568 (C9): time window and weather now, against what GatherBuddy gives for this fish. The zone's weather is computed once per period.
    private (uint Territory,long Period,uint? Current,uint? Previous) fishNowWeather;
    private void DrawFishNow(uint fishId,uint territory)
    {
        if(gatherBuddyFish?.State.Data?.Find(fishId) is not { } fish)return;
        var moment=DateTimeOffset.UtcNow;var period=moment.ToUnixTimeSeconds()/1400;
        if(fishNowWeather.Territory!=territory || fishNowWeather.Period!=period)
        {
            var forecast=weather.State.Catalog;
            fishNowWeather=(territory,period,forecast?.Forecast(territory,moment,1)[0].Weather?.Id,forecast?.Forecast(territory,moment.AddSeconds(-1400),1)[0].Weather?.Id);
        }
        string Mark(bool? value)=>localization.Get(value switch{true=>"FishingPanel.NowYes",false=>"FishingPanel.NowNo",_=>"FishingPanel.NowUnknown"});
        var parts=new List<string>();
        if(fish.StartMinute is not null && fish.EndMinute is not null)parts.Add(localization.Format("FishingPanel.NowWindow",Mark(FishNow.InWindow(fish.StartMinute,fish.EndMinute,EorzeaMinutes()))));
        if(fish.Weathers.Count>0)parts.Add(localization.Format("FishingPanel.NowWeather",Mark(FishNow.WeatherFits(fish.Weathers,fishNowWeather.Current))));
        if(fish.Transitions.Count>0)parts.Add(localization.Format("FishingPanel.NowBefore",Mark(FishNow.WeatherFits(fish.Transitions,fishNowWeather.Previous))));
        if(parts.Count==0)return;
        ImGui.TextDisabled(string.Join(" · ",parts));
    }
    internal void DrawFishingPanel()
    {
        if(windowDisposed || fishingNow?.Invoke() is not { } now)return;
        var automatic=configuration.FishingPanel && now.Fishing && now.Job==18;
        if(!automatic && !fishingPanelManual)return;
        var spot=fishingSpotAt?.Invoke(now.Territory,now.X,now.Z);
        var title=localization.Format("FishingPanel.Title",spot?.Name ?? localization.Get("FishingPanel.NoSpotTitle"));
        Panels?.Show("TenshoFishingPanel",title,ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,null,()=>
        {
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
            // 0.398 (g, R-04): the bait in hand and its stock; the fish whose route starts with it are marked in the table.
            var inHand=FishingBait?.Invoke() is > 0 and var baitId ? baitId:(uint?)null;
            if(inHand is { } held)
            {
                var baitItem=items?.Find(held);var baitStock=GetRecipeHoldings(held);
                ImGui.TextDisabled(localization.Get("FishingPanel.BaitInHand"));ImGui.SameLine();
                ItemCell(baitItem,held,20,$"{baitItem?.Name ?? held.ToString()}{(baitStock.HasObservation ? $" ×{baitStock.Total:N0}":"")}");
                ImGui.SameLine();
                ImGui.TextColored(Success,localization.Format("FishingPanel.BaitMatches",rows.Count(row=>row.Route.Count>0 && row.Route[0]==held)));
            }
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
                            var stock=GetRecipeHoldings(row.FishId);
                            ItemCell(fish,row.FishId,22,$"{fish?.Name ?? row.FishId.ToString()}{(stock.HasObservation && stock.Total>0 ? $" ×{stock.Total:N0}":"")}");
                            if(ChainNeed(row.FishId,stock) is { } need)ImGui.TextColored(Accent,need);
                            DrawFishNow(row.FishId,now.Territory);
                            ImGui.TableNextColumn();
                            for(var step=0;step<row.Route.Count;step++)
                            {
                                if(step>0){ImGui.SameLine(0,2);ImGui.TextDisabled("›");ImGui.SameLine(0,2);}
                                var bait=items?.Find(row.Route[step]);
                                DrawItemIcon(bait,new Vector2(22));
                                // 0.398: green frame around the first bait when it is the one in hand.
                                if(step==0 && inHand==row.Route[0])
                                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin()-new Vector2(1),ImGui.GetItemRectMax()+new Vector2(1),ImGui.GetColorU32(Success),3,ImDrawFlags.None,2);
                                if(ImGui.IsItemHovered())WrappedTooltip((bait?.Name ?? row.Route[step].ToString())+(step==0 ? "":"\n"+localization.Get("FishingSources.Mooch"))+"\n"+localization.Get("FishingPanel.RightClick"));
                            }
                            if(row.Route.Count==0)ImGui.TextDisabled("?");
                            ImGui.TableNextColumn();
                            // 0.330: « !!! » only for a legendary bite; any other bite is « ? » in a muted colour, and only known hooksets are named
                            // (an unknown one showed a raw key).
                            var bite=(row.Bite is "Weak" or "Strong" or "Legendary") ? row.Bite:null;var hook=(row.HookSet is "Precise" or "Powerful") ? row.HookSet:null;
                            if(bite is not null || hook is not null)
                            {
                                var colour=bite switch{"Weak"=>new Vector4(0.45f,0.85f,0.45f,1),"Strong"=>new Vector4(0.95f,0.80f,0.30f,1),"Legendary"=>new Vector4(0.95f,0.35f,0.35f,1),_=>Muted};
                                ImGui.TextColored(colour,(bite switch{"Weak"=>"!","Strong"=>"!!","Legendary"=>"!!!",_=>"?"})+(hook is not null ? " "+localization.Get("FishingPanel.HookShort"+hook):""));
                                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(bite is not null ? "FishingPanel.Bite"+bite:"FishingPanel.BiteUnknown")+(hook is not null ? " · "+localization.Get("FishingPanel.Hook"+hook):""));
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
        },fishingPanelManual ? ()=>fishingPanelManual=false:null);
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
