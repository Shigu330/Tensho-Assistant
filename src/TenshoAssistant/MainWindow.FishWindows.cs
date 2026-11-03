using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// g, R-02 (0.396): on the GatherBuddy fish sheet, the next real windows of the fish (Gathering/FishWindows), each with a countdown and a
// one-off alarm; computed again once a minute, or when the fish or the zone changes.
internal sealed partial class MainWindow
{
    private (uint Fish,uint Territory,WeatherCatalog Catalog,long Minute)? fishWindowsKey;
    private IReadOnlyList<FishWindow>? fishWindows;

    private void DrawFishWindows(GatherBuddyFish fish,WeatherCatalog catalog,uint territory,string name)
    {
        var now=DateTimeOffset.UtcNow;
        var key=(fish.FishId,territory,catalog,now.ToUnixTimeSeconds()/60);
        if(fishWindowsKey!=key)
        {
            try{fishWindows=FishWindows.Next(fish,catalog,territory,now);}
            catch(Exception error){fishWindows=null;log.Warning(error,"Fish windows could not be computed.");}
            fishWindowsKey=key;
        }
        ImGui.TextColored(Accent,localization.Get("FishWindows.Title"));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FishWindows.Help"));
        if(fishWindows is not { } windows){ImGui.TextDisabled(localization.Get("FishWindows.Unknown"));return;}
        if(windows.Count==0){ImGui.TextDisabled(localization.Get(fish.Weathers.Count+fish.Transitions.Count==0 && fish.StartMinute is null ? "FishWindows.Always":"FishWindows.None"));return;}
        foreach(var window in windows)
        {
            ImGui.PushID("FishWindow"+window.StartsAt.ToUnixTimeSeconds());
            try
            {
                var length=window.EndsAt-window.StartsAt;
                var text=localization.Format("FishWindows.Line",window.StartsAt.ToLocalTime().ToString("ddd HH:mm"),window.EndsAt.ToLocalTime().ToString("HH:mm"),(int)Math.Round(length.TotalMinutes));
                if(window.StartsAt<=now)ImGui.TextColored(Success,text+" · "+localization.Get("FishWindows.Open"));
                else
                {
                    var left=window.StartsAt-now;
                    ImGui.TextUnformatted(text+" · "+localization.Format("FishWindows.In",(int)left.TotalHours,left.Minutes));
                    ImGui.SameLine();
                    AlarmButton("FishWindow",localization.Format("Alarms.PeriodTooltip",name,window.StartsAt.ToLocalTime().ToString("g")),
                        ()=>[new TenshoAlarm{Label=name,Repeat=AlarmRepeat.Once,At=window.StartsAt,LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead),
                            TerritoryId=territory,ItemId=fish.FishId,JobId=18,Source=AlarmSource.Fishing}]);
                }
            }
            finally{ImGui.PopID();}
        }
    }
}
