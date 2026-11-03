using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// Eorzea clock and gathering windows: the current ET time (the PC time stays in the header) and, for a timed schedule, whether it is open,
// the next opening or closing with a countdown and the next openings in the tooltip (Gathering/GatheringTimeline.cs). Timelines are
// cached and recomputed at most once a second.
internal sealed partial class MainWindow
{
    private long gatheringTimeSecond=-1;
    private readonly Dictionary<string,GatheringTimeline> gatheringTimeCache=[];
    private void DrawGatheringClock()
    {
        var now=DateTimeOffset.UtcNow;var minute=GatheringTime.MinuteOfDay(now);
        // Eorzea time only (0.206): the PC time is shown once, in the header next to the version.
        ImGui.TextColored(Accent,localization.Format("GatheringClock.Time",minute/60,minute%60));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GatheringClock.Disclaimer"));
    }
    // 0.330: text of a schedule for the UI and the copies: its windows (language-neutral) or the translated text of its kind.
    private string ScheduleText(GatheringSchedule schedule)=>schedule.TextKey is { } key ? localization.Get(key):schedule.Label;
    private void DrawGatheringTime(GatheringSchedule schedule)
    {
        ImGui.TextWrapped(ScheduleText(schedule));
        if(schedule.Kind!=GatheringScheduleKind.Limited)return;
        var now=DateTimeOffset.UtcNow;var second=now.ToUnixTimeSeconds();
        if(second!=gatheringTimeSecond){gatheringTimeSecond=second;gatheringTimeCache.Clear();}
        if(!gatheringTimeCache.TryGetValue(schedule.Key,out var timeline) || timeline.Openings.FirstOrDefault() is { } cached && cached.EndsAt<=now)
        {timeline=GatheringTime.Next(schedule,now);gatheringTimeCache[schedule.Key]=timeline;}
        if(timeline.Openings.FirstOrDefault() is not { } first)return;
        var current=first.StartsAt<=now && now<first.EndsAt;
        ImGui.TextColored(current ? Success:Muted,localization.Format(current ? "LocalClock.Current":"LocalClock.Next",current ? first.EndsAt.ToLocalTime().ToString("HH:mm:ss"):first.StartsAt.ToLocalTime().ToString("g")));
        var remaining=(current ? first.EndsAt:first.StartsAt)-now;
        ImGui.TextDisabled(localization.Format(current ? "LocalClock.CountdownClose":"LocalClock.CountdownOpen",Math.Max(0,(int)remaining.TotalHours),remaining.Minutes,remaining.Seconds));
        if(ImGui.IsItemHovered())WrappedTooltip(string.Join('\n',timeline.Openings.Select(window=>$"{window.StartsAt.ToLocalTime():g}–{window.EndsAt.ToLocalTime():g}"))+"\n"+localization.Get("GatheringClock.Disclaimer"));
    }
}
