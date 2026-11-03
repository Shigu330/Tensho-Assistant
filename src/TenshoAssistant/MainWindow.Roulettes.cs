using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// g, C-02 (0.356): under Reminders, the daily duty roulettes — done today or not, and the role that gets the bonus — read with the
// Calendar counters (Reminders/GameTimers). Rows and names from the game data (ContentRoulette with a completion index, set by Plugin.cs).
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<RouletteRow>>? DailyRoulettes { get; set; }
    // 0.382 (g, M-11): the challenge log's entries (ContentsNote rows with a name), set by Plugin.cs.
    internal Func<IReadOnlyList<int>>? ChallengeRows { get; set; }
    internal Func<byte,string?>? RouletteName { get; set; }

    private void DrawDailyRoulettes(GameTimerSnapshot? timers)
    {
        var rows=DailyRoulettes?.Invoke();
        if(rows is not { Count:>0 })return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Roulettes.Title")+"###DailyRoulettes",ImGuiTreeNodeFlags.DefaultOpen))return;
        var read=timers?.Roulettes;
        var done=read?.Count(roulette=>roulette.Completed==true) ?? 0;
        var known=read?.Count(roulette=>roulette.Completed is not null) ?? 0;
        var reset=ResetSchedule.NextDaily(DateTimeOffset.UtcNow,ResetSchedule.DailyHour);
        ImGui.TextDisabled(known==0 ? localization.Get("Roulettes.Unknown")
            :localization.Format("Roulettes.Count",done,known,reset.ToLocalTime().ToString("t")));
        if(!ImGui.BeginTable("DailyRoulettes",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var key in new[]{"Roulette","Done","Bonus"})ImGui.TableSetupColumn(localization.Get("Roulettes.Column"+key));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                var state=read?.FirstOrDefault(roulette=>roulette.Id==row.Id);
                ImGui.TableNextRow();ImGui.TableNextColumn();
                ImGui.TextUnformatted(RouletteName?.Invoke(row.Id) ?? $"#{row.Id}");
                ImGui.TableNextColumn();
                if(state?.Completed==true)ImGui.TextColored(Success,localization.Get("Roulettes.Done"));
                else if(state?.Completed==false)ImGui.TextColored(Accent,localization.Get("Roulettes.ToDo"));
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(state?.Bonus is { } role)ImGui.TextColored(Accent,localization.Get("Roulettes.Role"+role));
                else ImGui.TextDisabled(row.BonusRow==0 ? "":"—");
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Get("Roulettes.Help"));
    }
}
