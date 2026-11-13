using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// « How to catch it » for a fish on the Gathering page (0.230), from GatherBuddy first (Apache-2.0, requested once and cached): bait route
/// with mooches, bite and hookset, lure, snagging, predators, ET window with the next openings, weathers and previous weathers, Teamcraft
/// bite time at each spot (0.231), and the matching alarm (daily ET with the weather condition), the zone weather and map. Garland stays
/// below as « other sources ».
/// </summary>
internal sealed partial class MainWindow
{
    private bool DrawGatherBuddyFish(GatheringCatalog catalog)
    {
        var fishId=(uint)Math.Max(0,itemId);
        gatherBuddyFish?.Request();
        var data=gatherBuddyFish?.State.Data;
        var spots=catalog.ForItem(fishId).Where(source=>source.IsFishing).ToArray();
        if(data?.Find(fishId) is not { } fish)
        {
            if(spots.Length>0 && gatherBuddyFish?.State is { Loading:true })StateLine(UI.UiState.Waiting,localization.Get("FishingPanel.GatherBuddyLoading"));
            return false;
        }
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("GatheringFish.Title")+"###GatherBuddyFish",ImGuiTreeNodeFlags.DefaultOpen))return true;
        var items=itemCatalog.State.Catalog;
        // Bait route: bait → mooched fish → … → this fish.
        var route=data.Route(fishId);
        if(route.Count==0)ImGui.TextDisabled(localization.Get("FishingPanel.NoBait"));
        for(var step=0;step<route.Count;step++)
        {
            var bait=items?.Find(route[step]);
            if(step>0){ImGui.SameLine();ImGui.TextDisabled("→");ImGui.SameLine();}
            DrawItemIcon(bait,new Vector2(28));
            if(ImGui.IsItemClicked() && bait is not null)SelectItem(bait.Id);
        }
        if(route.Count>0)
        {
            ImGui.SameLine();ImGui.TextDisabled("→");ImGui.SameLine();
            DrawItemIcon(SelectedItem,new Vector2(28));
            ImGui.TextUnformatted(localization.Format("GatheringFish.Route",string.Join(" → ",route.Select(id=>items?.Find(id)?.Name ?? id.ToString()))));
        }
        var details=new List<string>();
        if(fish.Bite is { } bite)details.Add(localization.Get("FishingPanel.Bite"+bite)+(fish.HookSet is { } hook ? " · "+localization.Get("FishingPanel.Hook"+hook):""));
        if(fish.Lure is { } lure)details.Add(localization.Format("FishingPanel.Lure",lure));
        if(fish.Snag)details.Add(localization.Get("FishingPanel.Snag"));
        if(details.Count>0)ImGui.TextColored(Accent,string.Join(" · ",details));
        if(fish.Predators.Count>0)ImGui.TextWrapped(localization.Format("FishingPanel.Predators",string.Join(", ",fish.Predators.Select(pair=>$"{items?.Find(pair.Fish)?.Name ?? pair.Fish.ToString()} ×{pair.Count}"))));
        if(fish.Intuition is { } intuition)ImGui.TextDisabled(localization.Format("FishingPanel.Intuition",intuition));
        // ET window with the next openings (same countdown as the gathering points).
        if(fish.StartMinute is { } start && fish.EndMinute is { } stop)
            DrawGatheringTime(GatheringSchedule.FromEphemeral((ushort)(start/60*100+start%60),(ushort)(stop/60%24*100+stop%60)));
        else ImGui.TextDisabled(localization.Get("GatheringFish.AnyTime"));
        var weathers=weather.State.Catalog;
        if(fish.Transitions.Count>0)ImGui.TextWrapped(localization.Format("FishingPanel.Previous",string.Join(", ",fish.Transitions.Select(id=>weathers?.Weather(id)?.Name ?? id.ToString()))));
        if(fish.Weathers.Count>0)ImGui.TextWrapped(localization.Format("FishingPanel.Weather",string.Join(", ",fish.Weathers.Select(id=>weathers?.Weather(id)?.Name ?? id.ToString()))));
        // 0.396 (g, R-02): the next real windows (ET hours, weather and previous weather together) of the first spot's zone.
        if(spots.FirstOrDefault() is { } windowSpot && weathers is not null)DrawFishWindows(fish,weathers,windowSpot.TerritoryId,SelectedItem?.Name ?? fishId.ToString());
        // Links: alarm on every window (with the weather condition), zone weather and map of the first spot, fishing help.
        var spot=spots.FirstOrDefault();
        // Bite time at each spot of the fish (Teamcraft reports, 0.231).
        foreach(var place in spots.Take(4))
            if(biteTimes?.Get(place.PointId) is { Times:{ } times } && times.GetValueOrDefault(fishId) is { } biteAt)
            {
                ImGui.TextUnformatted(localization.Format("GatheringFish.BiteAt",localization.Format("FishingPanel.BiteTime",biteAt.Minimum,biteAt.Maximum,biteAt.Usual),place.Place));
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FishingPanel.BiteTimeHelp",biteAt.Reports));
            }
        if(spot is not null)
        {
            if(GatherBuddyFishAlarmButton($"GbFish{fishId}",fish,spot.TerritoryId,SelectedItem?.Name ?? fishId.ToString()))ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("GatheringDetails.ZoneWeather")+"##GbWeather"))OpenWeather(spot.TerritoryId,spot.Zone);
            ImGui.SameLine();
            if(spot.MapId!=0 && ImGui.SmallButton(localization.Get("GatheringDetails.ZoneMap")+"##GbMap"))ShowMapOnTenshoMap(spot.MapId);
            ImGui.SameLine();
        }
        DrawFishingPanelButton();
        ImGui.TextDisabled(localization.Get("GatheringFish.Source"));
        return true;
    }
}
