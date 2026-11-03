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
                if(ImGui.SmallButton(localization.Get("WorldClocks.Remove")))removed=id;
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

    private void SaveClocks()
    {
        try{save();saveError=null;}
        catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Clocks could not be saved.");}
    }

    // Manual one-off alarm at a real local date, beside the manual ET alarm.
    private void DrawManualDateAlarm()
    {
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##AlarmDate",localization.Get("WorldClocks.DateHint"),ref alarmManualDate,16);
        Tip("WorldClocks.DateTip");
        ImGui.SameLine();
        var at=WorldClocks.ParseLocal(alarmManualDate,DateTimeOffset.UtcNow);
        ImGui.BeginDisabled(at is null);
        try
        {
            if(ImGui.SmallButton(localization.Get("WorldClocks.AddOnce")) && at is { } when)
            {
                AddAlarms([new TenshoAlarm{Label=alarmManualLabel.Trim().Length>0 ? alarmManualLabel.Trim():localization.Get("Alarms.ManualDefault"),
                    Group=localization.Get("WorldClocks.Group"),Source=AlarmSource.Other,Repeat=AlarmRepeat.Once,At=when,
                    LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead)}]);
                alarmManualDate="";
            }
        }
        finally{ImGui.EndDisabled();}
        Tip("WorldClocks.AddOnceTip");
    }
}
