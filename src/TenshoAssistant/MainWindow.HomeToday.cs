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
    private static readonly string[] HomeTodayLines=["Next","Reminders","Finished","Vessels","Quests","Alarms"];
    // X-155 (0.1261): the nearest deadline, first; a brief outline when another one takes its place (none with reduced motion).
    private const int HomeNextPulseMs=250;
    private HomeDeadline? homeNext;
    private (string Kind,string Label)? homeNextShown;
    private long homeNextChangedAt=-100_000;

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
        // X-155 (0.1261): the nearest of the next alarm, the next vessel back and the garden's next tending or harvest (when its reminders are on).
        var deadlines=new List<HomeDeadline>();
        if(NextAlarm() is { } nextAlarm)deadlines.Add(new("Alarms",nextAlarm.Alarm.Label,nextAlarm.At));
        if(Voyages.NextReturn(configuration.Voyages,now) is { } back)deadlines.Add(new("Vessels",localization.Get("Home.DeadlineVessel"),back));
        if(configuration.GardenAlerts && GardenJournal.Alarms(configuration.GardenBeds,now) is { Count:>0 } garden)
            deadlines.Add(new("Garden",localization.Format(garden[0].Kind==GardenState.Ripe ? "Home.DeadlineRipe":"Home.DeadlineTend",garden[0].Bed.Seed),garden[0].At));
        var next=HomeToday.Next(deadlines,now);
        (string,string)? shown=next is null ? null:(next.Kind,next.Label);
        if(shown!=homeNextShown)
        {
            if(homeNextShown is not null && shown is not null)homeNextChangedAt=Environment.TickCount64;
            homeNextShown=shown;
        }
        homeNext=next;
    }

    private void DrawHomeToday()
    {
        var now=DateTimeOffset.UtcNow;
        if(now-homeTodayAt>=TimeSpan.FromSeconds(1) || now<homeTodayAt)BuildHomeToday(now);
        TenshoAssistant.UI.UiComponents.SubTitle(localization.Get("Home.TodayTitle"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Home.Customize")))ImGui.OpenPopup("HomeCustomize");
        Tip("Home.CustomizeTip");
        DrawHomeCustomize();
        DrawHomeNextDeadline(now);
        foreach(var (key,text,target,good) in homeToday)
        {
            if(configuration.HomeTodayHidden?.Contains(key)==true)continue;
            ImGui.PushStyleColor(ImGuiCol.Text,good ? Success:UI.FfxivTheme.Text);
            try{if(ImGui.Selectable(text+"###Today"+key))page=target;}
            finally{ImGui.PopStyleColor();}
        }
    }

    /// <summary>X-155 (0.1261): « Next deadline: …, in … » first, one click to its tool; under an hour the time left ticks each second.</summary>
    private void DrawHomeNextDeadline(DateTimeOffset now)
    {
        if(homeNext is not { } next || next.At<=now || configuration.HomeTodayHidden?.Contains("Next")==true)return;
        var left=next.At-now;
        var when=left<TimeSpan.FromHours(1) ? TenshoAssistant.UI.Motion.Countdown(left):GardenLeftText(((int)left.TotalDays,left.Hours,left.Minutes));
        if(ImGui.Selectable(localization.Format("Home.NextDeadline",next.Label,when)+"###TodayNext"))page=next.Kind switch{"Alarms"=>Page.Alarms,"Vessels"=>Page.FreeCompany,_=>Page.Garden};
        Tip("Home.NextDeadlineTip");
        var pulse=TenshoAssistant.UI.Motion.Pulse(homeNextChangedAt,Environment.TickCount64,HomeNextPulseMs,ReducedMotion);
        if(pulse>0)ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),ImGui.GetColorU32(Accent with{W=Accent.W*pulse}));
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
