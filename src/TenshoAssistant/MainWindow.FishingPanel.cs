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
    internal Func<double?>? FishingIntuitionLeft { get; set; }
    /// <summary>C10 (0.1011): start of the last cast (FishingBaitReader.CastAt); the stopwatches restart from it, the button stays for a manual restart.</summary>
    internal Func<DateTimeOffset?>? FishingCastAt { get; set; }
    private static DateTimeOffset? FromCast(DateTimeOffset? since,DateTimeOffset? cast)=>Gathering.FishingStopwatch.FromCast(since,cast);
    internal void ToggleFishingPanel()=>fishingPanelManual=!fishingPanelManual;
    private Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?>? fishingSpotAt;
    internal void ConfigureFishingHelp(Func<(uint Job,uint Territory,float X,float Z,bool Fishing)?> now,Func<uint,float,float,(uint Id,string Name,float X,float Z,uint[] Fish)?> spotAt)
    {fishingNow=now;fishingSpotAt=spotAt;}

    // 0.568 (C9): time window and weather now, against what GatherBuddy gives for this fish. The zone's weather is computed once per period.
    private (uint Territory,long Period,uint? Current,uint? Previous) fishNowWeather;
    /// <summary>
    /// X-73.3 (0.859, user request: learn from the game what comes from community data): the game's own fishing log says whether a fish has a time or a weather
    /// condition (FishingNoteInfo.TimeRestriction / WeatherRestriction, copied by FishingNotebookCopy). False there means none, whatever a community source says;
    /// null: not known from the game. Several notebook rows for one fish: only when all agree.
    /// </summary>
    private (bool? Time,bool? Weather) FishRestrictions(uint fishId)
    {
        if(gatheringCatalog.State.Catalog?.Fishing.ForItem(fishId) is not { Count:>0 } notes)return (null,null);
        static bool? All(IEnumerable<bool?> values){var list=values.ToArray();return list.All(value=>value==false) ? false:list.All(value=>value==true) ? true:null;}
        return (All(notes.Select(note=>note.TimeRestricted)),All(notes.Select(note=>note.WeatherRestricted)));
    }
    private void DrawFishNow(uint fishId,uint territory)
    {
        if(gatherBuddyFish?.State.Data?.Find(fishId) is not { } fish)return;
        var (timeLimited,weatherLimited)=FishRestrictions(fishId);
        var moment=DateTimeOffset.UtcNow;var period=moment.ToUnixTimeSeconds()/1400;
        if(fishNowWeather.Territory!=territory || fishNowWeather.Period!=period)
        {
            var forecast=weather.State.Catalog;
            fishNowWeather=(territory,period,forecast?.Forecast(territory,moment,1)[0].Weather?.Id,forecast?.Forecast(territory,moment.AddSeconds(-1400),1)[0].Weather?.Id);
        }
        string Mark(bool? value)=>localization.Get(value switch{true=>"FishingPanel.NowYes",false=>"FishingPanel.NowNo",_=>"FishingPanel.NowUnknown"});
        var parts=new List<string>();
        if(timeLimited!=false && fish.StartMinute is not null && fish.EndMinute is not null)parts.Add(localization.Format("FishingPanel.NowWindow",Mark(FishNow.InWindow(fish.StartMinute,fish.EndMinute,EorzeaMinutes()))));
        if(weatherLimited!=false && fish.Weathers.Count>0)parts.Add(localization.Format("FishingPanel.NowWeather",Mark(FishNow.WeatherFits(fish.Weathers,fishNowWeather.Current))));
        if(weatherLimited!=false && fish.Transitions.Count>0)parts.Add(localization.Format("FishingPanel.NowBefore",Mark(FishNow.WeatherFits(fish.Transitions,fishNowWeather.Previous))));
        if(parts.Count==0)return;
        ImGui.TextDisabled(string.Join(" · ",parts));
    }
    internal void DrawFishingPanel()
    {
        if(windowDisposed || fishingNow?.Invoke() is not { } now)return;
        // X-85 V1 (0.917): at sea the usual help picks a wrong spot or none: off while the voyage director is read, or in an ocean fishing
        // territory (read in the game data) when it is not; the ocean voyage panel takes over. Opening it by hand still works.
        var atSea=OceanVoyage?.Frame is not null || OceanVoyageData?.Invoke()?.Territories.Contains(now.Territory)==true;
        var automatic=configuration.FishingPanel && now.Fishing && now.Job==18 && !atSea;
        if(!automatic && !fishingPanelManual)return;
        var spot=fishingSpotAt?.Invoke(now.Territory,now.X,now.Z);
        var title=localization.Format("FishingPanel.Title",spot?.Name ?? localization.Get("FishingPanel.NoSpotTitle"));
        Panels?.Show("TenshoFishingPanel",title,ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,null,()=>
        {
            if(spot is not { } here){ImGui.TextWrapped(localization.Get("FishingPanel.NoSpot"));return;}
            ImGui.TextDisabled(localization.Get(now.Fishing ? "FishingPanel.Fishing":"FishingPanel.NotFishing"));
            if(now.Fishing && !fishingBefore)fishingSince=DateTimeOffset.UtcNow;
            if(!now.Fishing)fishingSince=null;
            else fishingSince=FromCast(fishingSince,FishingCastAt?.Invoke());
            fishingBefore=now.Fishing;
            var elapsed=fishingSince is { } since ? (int)(DateTimeOffset.UtcNow-since).TotalSeconds:(int?)null;
            if(elapsed is { } seconds)
            {
                ImGui.TextColored(Accent,localization.Format("FishingPanel.Elapsed",seconds));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("FishingPanel.Restart")))fishingSince=DateTimeOffset.UtcNow;
                Tip("FishingPanel.RestartHelp");
            }
            // C28 (0.663): the time left of the intuition, from the character's status.
            if(FishingIntuitionLeft?.Invoke() is { } intuitionLeft)
                ImGui.TextColored(Accent,localization.Format("FishingPanel.IntuitionLeft",(int)intuitionLeft/60,(int)intuitionLeft%60));
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
                ItemCell(baitItem,held,20,$"{baitItem?.Name ?? held.ToString()}{(baitStock.HasObservation ? $" ×{(baitStock.Total).ToString("N0",Numbers)}":"")}");
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
                            ItemCell(fish,row.FishId,22,$"{fish?.Name ?? row.FishId.ToString()}{(stock.HasObservation && stock.Total>0 ? $" ×{(stock.Total).ToString("N0",Numbers)}":"")}");
                            if(ChainNeed(row.FishId,stock) is { } need)ImGui.TextColored(Accent,need);
                            DrawFishNow(row.FishId,now.Territory);
                            // X-97 (0.982, user report: « collectable fish not taken into account », seen in the fishing helper; DEC-045): the collectability of each
                            // tier and its scrips, from the game's sheets (CollectablesShopRefine, CollectablesShopRewardScrip), the same rules as the gathering panel.
                            if(Collectables?.Catalog?.For(row.FishId) is { Count:>0 } collectable && collectable.FirstOrDefault(rule=>rule.Scrips) is { } scrip)
                            {
                                ImGui.TextColored(Accent,localization.Format("FishingPanel.Collectable",scrip.Low,scrip.Mid,scrip.High));
                                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FishingPanel.CollectableTip",scrip.Low,scrip.RewardLow,scrip.Mid,scrip.RewardMid,scrip.High,scrip.RewardHigh,scrip.Group));
                            }
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
                                var colour=bite switch{"Weak"=>Success,"Strong"=>Warning,"Legendary"=>Error,_=>Muted};
                                ImGui.TextColored(colour,(bite switch{"Weak"=>"!","Strong"=>"!!","Legendary"=>"!!!",_=>"?"})+(hook is not null ? " "+localization.Get("FishingPanel.HookShort"+hook):""));
                                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(bite is not null ? "FishingPanel.Bite"+bite:"FishingPanel.BiteUnknown")+(hook is not null ? " · "+localization.Get("FishingPanel.Hook"+hook):""));
                            }
                            else ImGui.TextDisabled("—");
                            ImGui.TableNextColumn();
                            if(bites?.Times?.GetValueOrDefault(row.FishId) is { } time)
                            {
                                var inside=elapsed is { } spent && spent>=time.Minimum && spent<=time.Maximum;
                                var text=localization.Format("FishingPanel.BiteShort",time.Minimum,time.Maximum,time.Usual);
                                // X-98 (0.958, user request): green while the bite time runs, red once the stopwatch has passed it.
                        if(inside)ImGui.TextColored(Success,text);else if(elapsed is { } passed && passed>time.Maximum)ImGui.TextColored(Error,text);else ImGui.TextUnformatted(text);
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
            Tip(gatherBuddyFish?.State.Data is not null ? "FishingPanel.FooterGatherBuddy":"FishingPanel.Footer");
            if(gatherBuddyFish?.State is { Loading:true })StateLine(UI.UiState.Waiting,localization.Get("FishingPanel.GatherBuddyLoading"));
        },fishingPanelManual ? ()=>fishingPanelManual=false:null);
    }

    // Button that opens or closes the fishing help (Gathering pages).
    private void DrawFishingPanelButton()
    {
        if(ImGui.SmallButton(localization.Get(fishingPanelManual ? "FishingPanel.Close":"FishingPanel.Open")+"##FishingPanelButton"))ToggleFishingPanel();
        Tip("FishingPanel.OpenHelp");
    }

    // One fish of the panel: bait route, bite, hookset and conditions (short text + full tooltip), GatherBuddy first, else Garland.
    private (uint FishId,IReadOnlyList<uint> Route,string? Bite,string? HookSet,string Conditions,string ConditionsDetail) FishRow(uint fishId,uint spotId)
    {
        var items=itemCatalog.State.Catalog;var catalog=weather.State.Catalog;
        string Weathers(IEnumerable<uint> ids)=>string.Join(", ",ids.Select(id=>catalog?.Weather(id)?.Name ?? id.ToString()));
        // X-73.3 (0.859): what the game's fishing log says first; a community window or weather is left out when the game says there is none.
        var (timeLimited,weatherLimited)=FishRestrictions(fishId);
        string? free=timeLimited==false && weatherLimited==false ? localization.Get("FishingPanel.NoCondition"):null;
        if(gatherBuddyFish?.State.Data is { } data && data.Find(fishId) is { } known)
        {
            var parts=new List<string>();var detail=new List<string>();
            if(free is not null){parts.Add(free);detail.Add(localization.Get("FishingPanel.NoConditionDetail"));}
            if(timeLimited!=false && known.StartMinute is { } start && known.EndMinute is { } stop)
            {var window=EtRange(start,stop%1440);parts.Add(window);detail.Add(window);}
            if(weatherLimited!=false && known.Transitions.Count>0){parts.Add(Weathers(known.Transitions)+" ›");detail.Add(localization.Format("FishingPanel.Previous",Weathers(known.Transitions)));}
            if(weatherLimited!=false && known.Weathers.Count>0){parts.Add(Weathers(known.Weathers));detail.Add(localization.Format("FishingPanel.Weather",Weathers(known.Weathers)));}
            if(known.Lure is { } lure){parts.Add(lure);detail.Add(localization.Format("FishingPanel.Lure",lure));}
            if(known.Snag){parts.Add(localization.Get("FishingPanel.SnagShort"));detail.Add(localization.Get("FishingPanel.Snag"));}
            if(known.Predators.Count>0)
            {
                parts.Add(localization.Format("FishingPanel.PredatorsShort",known.Predators.Count));
                detail.Add(localization.Format("FishingPanel.Predators",string.Join(", ",known.Predators.Select(pair=>$"{items?.Find(pair.Fish)?.Name ?? pair.Fish.ToString()} ×{pair.Count}"))));
                if(known.Intuition is { } intuition)detail.Add(localization.Format("FishingPanel.Intuition",intuition));
            }
            // X-75.3 (0.877): what was seen in game (yours or shared), after the community conditions, in the detail.
            if(SeenFishLine(fishId) is { } seenLine){parts.Add(localization.Get("FishingPanel.SeenShort"));detail.Add(seenLine);}
            return (fishId,data.Route(fishId),known.Bite,known.HookSet,string.Join(" · ",parts),string.Join("\n",detail));
        }
        var here=itemSources?.Get(fishId).List?.Fishing?.Spots.FirstOrDefault(entry=>entry.SpotId==spotId);
        var route=here?.BaitChains is { Count:>0 } chains ? chains[0]:(IReadOnlyList<uint>)[];
        var conditions=new List<string>();
        if(free is not null)conditions.Add(free);
        if(here?.Conditions is { Error:null } found)
        {
            if(timeLimited!=false && found.Timing.Kind==Gathering.GatheringScheduleKind.Limited)conditions.Add(found.Timing.Label);
            if(weatherLimited!=false && found.Weather is { Count:>0 } names)conditions.Add(string.Join(", ",names));
        }
        return (fishId,route,null,null,string.Join(" · ",conditions),string.Join("\n",conditions));
    }
}
