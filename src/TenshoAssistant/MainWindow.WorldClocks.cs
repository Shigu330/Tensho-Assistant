using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;

namespace TenshoAssistant;

// g, X-10 (0.386): on the Alarms page, clocks for the time zones the player chose (6 at most), and a manual one-off alarm at a real
// date typed in local time (the tools already create the others).
internal sealed partial class MainWindow
{
    private string clockSearch="";
    private string alarmManualDate="";

    private void DrawWorldClocks()
    {
        var zones=configuration.WorldClocks ?? [];
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("WorldClocks.Title",zones.Count)+"###WorldClocks"))return;
        var now=DateTimeOffset.UtcNow;string? removed=null;
        foreach(var (id,name,time) in WorldClocks.Times(zones,now))
        {
            ImGui.PushID(id);
            try
            {
                ImGui.TextColored(Accent,time.ToString("t"));
                ImGui.SameLine();ImGui.TextUnformatted(name);
                ImGui.SameLine();
                if(DeleteButton("WorldClocks.Remove"))removed=id;
                Tip("WorldClocks.RemoveTip");
            }
            finally{ImGui.PopID();}
        }
        if(removed is not null){configuration.WorldClocks=zones.Where(zone=>zone!=removed).ToList();SaveClocks();}
        ImGui.BeginDisabled(zones.Count>=WorldClocks.Capacity);
        try
        {
            ImGui.SetNextItemWidth(320);
            if(ImGui.BeginCombo("##ClockZone",localization.Get("WorldClocks.Add")))
            {
                try
                {
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##ClockSearch",localization.Get("WorldClocks.Search"),ref clockSearch,64);
                    Tip("WorldClocks.SearchTip");
                    foreach(var zone in TimeZoneInfo.GetSystemTimeZones().Where(zone=>clockSearch.Length==0 || zone.DisplayName.Contains(clockSearch,StringComparison.CurrentCultureIgnoreCase)).Take(80))
                        if(ImGui.Selectable(zone.DisplayName+"##"+zone.Id))
                        {
                            var list=new List<string>(zones);
                            if(WorldClocks.Add(list,zone.Id)){configuration.WorldClocks=list;SaveClocks();}
                        }
                }
                finally{ImGui.EndCombo();}
            }
            Tip("WorldClocks.AddTip");
        }
        finally{ImGui.EndDisabled();}
    }

    private void SaveClocks()=>TrySave("Clocks");

    // Manual one-off alarm at a real local date, beside the manual ET alarm.
    // 0.577 (T0): how the alarm with a real date repeats.
    private int alarmManualRepeat;
    private static readonly AlarmRepeat[] ManualRepeats=[AlarmRepeat.Once,AlarmRepeat.RealDaily,AlarmRepeat.RealWeekly,AlarmRepeat.DailyReset,AlarmRepeat.WeeklyReset];
    private void DrawManualDateAlarm()
    {
        ImGui.SetNextItemWidth(170);
        ImGui.Combo("##AlarmManualRepeat",ref alarmManualRepeat,ManualRepeats.Select(repeat=>localization.Get("Alarms.Repeat"+repeat)).ToArray(),ManualRepeats.Length);
        Tip("Alarms.RepeatTip");
        ImGui.SameLine();
        var chosenRepeat=ManualRepeats[Math.Clamp(alarmManualRepeat,0,ManualRepeats.Length-1)];
        var byReset=chosenRepeat is AlarmRepeat.DailyReset or AlarmRepeat.WeeklyReset;
        ImGui.BeginDisabled(byReset);
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##AlarmDate",localization.Get("WorldClocks.DateHint"),ref alarmManualDate,16);
        Tip("WorldClocks.DateTip");
        ImGui.EndDisabled();
        ImGui.SameLine();
        var at=byReset ? DateTimeOffset.UtcNow:WorldClocks.ParseLocal(alarmManualDate,DateTimeOffset.UtcNow);
        ImGui.BeginDisabled(at is null);
        try
        {
            if(ImGui.SmallButton(localization.Get("WorldClocks.AddOnce")) && at is { } when)
            {
                // X-295 (0.1296): the block of a dated alarm is titled with its name; without a name, « Dated alarm · » and its date.
                var alarm=new TenshoAlarm{Group=localization.Get("WorldClocks.Group"),Source=AlarmSource.Other,Repeat=chosenRepeat,At=when,
                    LeadMinutes=AlarmSchedule.ClampLead(chosenRepeat,DefaultAlarmLead)};
                alarm.Label=alarmManualLabel.Trim().Length>0 ? alarmManualLabel.Trim():DatedAlarmFallback(alarm);
                AddAlarms([alarm]);
                alarmManualDate="";
            }
        }
        finally{ImGui.EndDisabled();}
        Tip("WorldClocks.AddOnceTip");
    }
}
