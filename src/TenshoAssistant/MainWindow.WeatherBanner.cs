using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Gathering;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// X-155 (0.1260, pilot of the visual plan): Weather read at a glance. Above the table, three blocks of the same size: now, next, after,
// each with the weather's icon and name and its Eorzea hours; « next » and « after » say in how long they start (it ticks each second in
// a fixed place), at what PC time, and carry the alarm button of the table. The current block has the selection background and mark;
// when the period changes the mark moves to the new block, with a brief fade of its background unless motion is reduced. The forecast
// is computed, never observed: the banner says so. Narrow window: the three blocks stack.
internal sealed partial class MainWindow
{
    private Func<bool> reducedMotion=()=>true;
    /// <summary>X-155: Dalamud's « reduced motion » preference, read when a highlight is drawn. Not given (the desktop application): no motion.</summary>
    internal void ConfigureMotion(Func<bool> reduced)=>reducedMotion=reduced;
    private bool ReducedMotion{get{try{return reducedMotion();}catch(Exception){return true;}}}

    private const int WeatherBannerFadeMs=150;
    private long weatherBannerPeriod=-1,weatherBannerChangedAt=-100_000;
    private uint weatherBannerTerritory;

    private IReadOnlyList<TenshoAlarm> WeatherPeriodAlarm(WeatherPeriod entry,WeatherDefinition named)
        =>[new TenshoAlarm{Label=$"{named.Name} · {weatherZone}",Group=$"{named.Name} · {weatherZone}",IconId=named.Icon,Repeat=AlarmRepeat.Once,At=entry.StartsAt,LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead),TerritoryId=weatherTerritory}];

    private void DrawWeatherBanner(DateTimeOffset now)
    {
        var count=Math.Min(3,weatherForecast.Count);
        if(count==0)return;
        // The period changed while this zone was shown: the highlight of the new current block starts (not on a change of zone).
        if(weatherBannerPeriod!=weatherPeriod || weatherBannerTerritory!=weatherTerritory)
        {
            if(weatherBannerPeriod>=0 && weatherBannerTerritory==weatherTerritory)weatherBannerChangedAt=Environment.TickCount64;
            weatherBannerPeriod=weatherPeriod;weatherBannerTerritory=weatherTerritory;
        }
        var style=ImGui.GetStyle();
        var icon=MathF.Round(ImGui.GetFrameHeight()*1.25f);
        var available=ImGui.GetContentRegionAvail().X;
        var stacked=available<ImGui.GetFontSize()*30;
        var width=stacked ? available:MathF.Floor((available-style.ItemSpacing.X*(count-1))/count);
        var height=ImGui.GetTextLineHeightWithSpacing()*2+icon+style.WindowPadding.Y*2;
        for(var index=0;index<count;index++)
        {
            var entry=weatherForecast[index];
            if(index>0 && !stacked)ImGui.SameLine();
            var current=index==0;
            var background=current ? Vector4.Lerp(Selected,Accent,0.35f*Motion.Pulse(weatherBannerChangedAt,Environment.TickCount64,WeatherBannerFadeMs,ReducedMotion)):FfxivTheme.Background;
            ImGui.PushStyleColor(ImGuiCol.ChildBg,background);
            var shown=ImGui.BeginChild($"WeatherBanner{index}",new Vector2(width,height),true,ImGuiWindowFlags.NoScrollbar);
            try
            {
                if(shown)DrawWeatherBannerBlock(index,entry,now,icon);
            }
            finally{ImGui.EndChild();ImGui.PopStyleColor();}
            if(current)
            {
                var (min,max)=SelectionMark.SideMark(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                ImGui.GetWindowDrawList().AddRectFilled(min,max,ImGui.GetColorU32(Accent));
            }
        }
        ImGui.TextDisabled(localization.Get("Weather.BannerComputed"));
    }

    private void DrawWeatherBannerBlock(int index,WeatherPeriod entry,DateTimeOffset now,float icon)
    {
        var label=localization.Get(index switch{0=>"Weather.BannerNow",1=>"Weather.BannerNext",_=>"Weather.BannerAfter"});
        if(index==0)ImGui.TextUnformatted(label);
        else
        {
            // The time left ticks each second, at a fixed place.
            ImGui.TextUnformatted(label);
            ImGui.SameLine();
            ImGui.TextDisabled(localization.Format("Weather.BannerIn",Motion.Countdown(entry.StartsAt-now)));
        }
        if(entry.Weather is { } weather)
        {
            DrawGameIcon(weather.Icon,new Vector2(icon));
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(weather.Name);
            if(index>0)
            {
                ImGui.SameLine();
                AlarmButton($"Banner{entry.StartsAt.ToUnixTimeSeconds()}",localization.Format("Alarms.PeriodTooltip",weather.Name,entry.StartsAt.ToLocalTime().ToString("g")),()=>WeatherPeriodAlarm(entry,weather));
            }
        }
        else
        {
            ImGui.Dummy(new Vector2(icon));
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(localization.Get("Weather.BannerUnavailable"));
        }
        ImGui.TextDisabled(index==0 ? localization.Format("Weather.BannerEt",entry.EtHours)
            : localization.Format("Weather.BannerEtPc",entry.EtHours,entry.StartsAt.ToLocalTime().ToString("t")));
    }
}
