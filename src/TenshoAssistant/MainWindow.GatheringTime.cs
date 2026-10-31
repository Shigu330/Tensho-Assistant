using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private long gatheringTimeSecond=-1;
    private readonly Dictionary<string,GatheringTimeline> gatheringTimeCache=[];
    private void DrawGatheringClock()
    {
        var now=DateTimeOffset.UtcNow;var minute=GatheringTime.MinuteOfDay(now);
        ImGui.TextColored(Accent,localization.Format("GatheringClock.Time",minute/60,minute%60));
        ImGui.TextDisabled(localization.Format("LocalClock.Source",now.ToLocalTime().ToString("HH:mm:ss",System.Globalization.CultureInfo.CurrentCulture)));
    }
    private void DrawGatheringTime(GatheringSchedule schedule)
    {
        ImGui.TextWrapped(schedule.Label);
        if(schedule.Kind!=GatheringScheduleKind.Limited)return;
        var now=DateTimeOffset.UtcNow;var second=now.ToUnixTimeSeconds();
        if(second!=gatheringTimeSecond){gatheringTimeSecond=second;gatheringTimeCache.Clear();}
        if(!gatheringTimeCache.TryGetValue(schedule.Key,out var timeline) || timeline.Openings.FirstOrDefault() is { } cached && cached.EndsAt<=now)
        {timeline=GatheringTime.Next(schedule,now);gatheringTimeCache[schedule.Key]=timeline;}
        if(timeline.Openings.FirstOrDefault() is not { } first)return;
        var current=first.StartsAt<=now && now<first.EndsAt;
        ImGui.TextColored(current ? Success:Muted,localization.Format(current ? "LocalClock.Current":"LocalClock.Next",current ? first.EndsAt.ToLocalTime().ToString("HH:mm:ss"):first.StartsAt.ToLocalTime().ToString("g")));
        var remaining=(current ? first.EndsAt:first.StartsAt)-now;
        ImGui.TextDisabled(localization.Format("LocalClock.Countdown",Math.Max(0,(int)remaining.TotalHours),remaining.Minutes,remaining.Seconds));
        if(ImGui.IsItemHovered())ImGui.SetTooltip(string.Join('\n',timeline.Openings.Select(window=>$"{window.StartsAt.ToLocalTime():g}–{window.EndsAt.ToLocalTime():g}"))+"\n"+localization.Get("GatheringClock.Disclaimer"));
    }
}
