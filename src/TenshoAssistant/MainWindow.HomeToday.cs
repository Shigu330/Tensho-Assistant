using Dalamud.Bindings.ImGui;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// T10 (0.599): Home › Today: what counts now, each line hidden from « Customize » with the tiles. Nothing is read from the game here: the rows and
// timers the server bar already copied (ReminderCounts, TickRemindersFromGame), the saved vessels, the followed quests and the alarms. Built once
// a second, not every frame. A line with nothing to say is left out.
internal sealed partial class MainWindow
{
    private GameTimerSnapshot? homeTimers;
    private DateTimeOffset homeTodayAt;
    private (string Key,string Text,Page Target,bool Good)[] homeToday=[];
    private static readonly string[] HomeTodayLines=["Reminders","Finished","Vessels","Quests","Alarms"];

    /// <summary>Called with each copy of the game's timers, on the framework thread.</summary>
    private void KeepHomeTimers(GameTimerSnapshot timers)=>Volatile.Write(ref homeTimers,timers);

    private void BuildHomeToday(DateTimeOffset now)
    {
        var lines=new List<(string,string,Page,bool)>();
        var todo=ReminderTodoLines(now);
        lines.Add(("Reminders",todo.Count==0 ? localization.Get("Home.TodayNoReminder"):localization.Format("Home.TodayReminders",HomeToday.Short(todo,4,localization.Get("Home.TodayMore"))),Page.Reminders,todo.Count==0));
        var timers=Volatile.Read(ref homeTimers);
        var finished=ReminderCounts.Finished(timers?.Ventures?.Select(venture=>venture.Done),configuration.Voyages,
            timers?.SquadronMission is { } mission ? (mission.Id,mission.Done):null,timers?.SquadronTraining is { } training ? (training.Id,training.Done):null,configuration.GardenBeds,now);
        if(finished.Count>0)lines.Add(("Finished",string.Join(" · ",finished.Select(item=>localization.Format("Reminders.Bar"+item.Kind,item.Count))),Page.Reminders,true));
        if(Voyages.NextReturn(configuration.Voyages,now) is { } vessel && vessel>now)lines.Add(("Vessels",localization.Format("Home.TodayVessel",vessel.ToLocalTime().ToString("g")),Page.FreeCompany,false));
        var followed=(configuration.FollowedQuests ?? []).Select(id=>QuestOf(id)?.Name).OfType<string>().Where(name=>name.Length>0).ToArray();
        if(followed.Length>0)lines.Add(("Quests",localization.Format("Home.TodayQuests",HomeToday.Short(followed,3,localization.Get("Home.TodayMore"))),Page.Quests,false));
        var alarms=(configuration.Alarms ?? []).Where(alarm=>alarm.Enabled).Select(alarm=>(alarm.Label,At:NextAlarm(alarm,now))).Where(row=>row.At is not null).OrderBy(row=>row.At!.Value).Take(3)
            .Select(row=>localization.Format("Home.TodayAlarm",row.Label,row.At!.Value.ToLocalTime().ToString("t"))).ToArray();
        if(alarms.Length>0)lines.Add(("Alarms",string.Join(" · ",alarms),Page.Alarms,false));
        homeToday=lines.ToArray();homeTodayAt=now;
    }

    private void DrawHomeToday()
    {
        var now=DateTimeOffset.UtcNow;
        if(now-homeTodayAt>=TimeSpan.FromSeconds(1) || now<homeTodayAt)BuildHomeToday(now);
        ImGui.TextColored(Accent,localization.Get("Home.TodayTitle"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Home.Customize")))ImGui.OpenPopup("HomeCustomize");
        Tip("Home.CustomizeTip");
        DrawHomeCustomize();
        foreach(var (key,text,target,good) in homeToday)
        {
            if(configuration.HomeTodayHidden?.Contains(key)==true)continue;
            ImGui.PushStyleColor(ImGuiCol.Text,good ? Success:UI.FfxivTheme.Text);
            try{if(ImGui.Selectable(text+"###Today"+key))page=target;}
            finally{ImGui.PopStyleColor();}
        }
    }

    private void DrawHomeCustomize()
    {
        if(!ImGui.BeginPopup("HomeCustomize"))return;
        try
        {
            ImGui.TextDisabled(localization.Get("Home.TodayTitle"));
            foreach(var key in HomeTodayLines)
            {
                var shown=configuration.HomeTodayHidden?.Contains(key)!=true;
                if(ImGui.Checkbox(localization.Get("Home.TodayLine"+key)+"###TodayLine"+key,ref shown)){configuration.HomeTodayHidden=HomeToday.Toggle(configuration.HomeTodayHidden,key,shown);SaveHome();}
                Tip("Home.TodayLineTip");
            }
            ImGui.Separator();
            ImGui.TextDisabled(localization.Get("Home.TilesTitle"));
            foreach(var (_,name,_,target) in HomeTiles())
            {
                var key=target.ToString();var shown=configuration.HomeTilesHidden?.Contains(key)!=true;
                if(ImGui.Checkbox(name+"###TileShown"+key,ref shown)){configuration.HomeTilesHidden=HomeToday.Toggle(configuration.HomeTilesHidden,key,shown);SaveHome();}
                Tip("Home.TileShownTip");
            }
        }
        finally{ImGui.EndPopup();}
    }

    private void SaveHome()=>TrySave("Home choices");
}
