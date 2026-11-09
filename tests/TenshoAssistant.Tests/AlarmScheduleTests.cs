using TenshoAssistant.Alarms;
using TenshoAssistant.UI;

internal static class AlarmScheduleTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Daily ET alarms recur every 70 real minutes at the ET time and ring once per occurrence within their lead",()=>
        {
            // Unix 4200 × 1000 is the start of an ET day (00:00 ET); 08:00 ET is 480 ET minutes = 1400 s later.
            var dayStart = DateTimeOffset.FromUnixTimeSeconds(4200L * 1000);
            var alarm = new TenshoAlarm { Repeat = AlarmRepeat.EorzeaDaily, EtMinute = 480, LeadMinutes = 5 };
            Check(AlarmSchedule.Next(alarm,dayStart) == dayStart.AddSeconds(1400),"08:00 ET is 1400 s after midnight ET");
            Check(AlarmSchedule.Next(alarm,dayStart.AddSeconds(1400)) == dayStart.AddSeconds(1400 + 4200),"next ET day after the occurrence");
            Check(AlarmSchedule.Occurrences(alarm,dayStart,3).SequenceEqual([dayStart.AddSeconds(1400),dayStart.AddSeconds(5600),dayStart.AddSeconds(9800)]),"three ET days");
            Check(AlarmSchedule.Check(alarm,dayStart).Check == AlarmCheck.None,"too early");
            var (check,occurrence) = AlarmSchedule.Check(alarm,dayStart.AddSeconds(1400 - 240));
            Check(check == AlarmCheck.Fire && occurrence == dayStart.AddSeconds(1400),"rings 4 min before, within the 5 min lead");
            alarm.LastOccurrence = occurrence;
            Check(AlarmSchedule.Check(alarm,dayStart.AddSeconds(1400 - 60)).Check == AlarmCheck.None,"rings once per occurrence");
            Check(AlarmSchedule.ClampLead(AlarmRepeat.EorzeaDaily,500) == 60 && AlarmSchedule.ClampLead(AlarmRepeat.Once,500) == 500,"lead bounded by the ET day");
        }),
        ("Alarms with weather conditions skip occurrences without that weather and treat an unknown forecast as no match",()=>
        {
            var dayStart = DateTimeOffset.FromUnixTimeSeconds(4200L * 1000);
            var alarm = new TenshoAlarm { Repeat = AlarmRepeat.EorzeaDaily, EtMinute = 480, LeadMinutes = 1, Weathers = [2], PreviousWeathers = [7] };
            var now = dayStart.AddSeconds(1400 - 30);
            uint? Forecast(DateTimeOffset time) => time < dayStart.AddSeconds(1400) ? 7u : 2u;
            Check(AlarmSchedule.Check(alarm,now,Forecast).Check == AlarmCheck.Fire,"weather and previous weather match");
            Check(AlarmSchedule.Check(alarm,now,_ => 2u).Check == AlarmCheck.SkipWeather,"previous weather differs");
            Check(AlarmSchedule.Check(alarm,now,_ => null).Check == AlarmCheck.SkipWeather,"unknown forecast is not a match");
        }),
        ("Window alarms are one per ET start, single alarms expire, and the list is bounded",()=>
        {
            var created = AlarmSchedule.ForWindows("Point",[600,120,600,1500],10,1,2);
            Check(created.Select(alarm => alarm.EtMinute).SequenceEqual([60,120,600]) && created[0].Label == "Point · 01:00 ET","sorted distinct ET starts");
            var list = new List<TenshoAlarm>();
            Check(AlarmSchedule.Add(list,created) && list.Count == 3,"added");
            Check(AlarmSchedule.Add(list,AlarmSchedule.ForWindows("Point",[60],10,1,2)) && list.Count == 3,"duplicates skipped");
            var once = new TenshoAlarm { Label = "Once", At = DateTimeOffset.UnixEpoch.AddDays(1) };
            Check(AlarmSchedule.Next(once,DateTimeOffset.UnixEpoch.AddDays(2)) is null,"single alarm past");
            list.Add(once);
            Check(AlarmSchedule.RemoveExpired(list,DateTimeOffset.UnixEpoch.AddDays(2)) == 1 && list.Count == 3,"expired single alarm removed");
            Check(!AlarmSchedule.Add(list,Enumerable.Range(0,AlarmSchedule.Maximum).Select(index => new TenshoAlarm { Label = $"A{index}" })),"bounded");
            Check(AlarmSchedule.JobOf(0,false)==16 && AlarmSchedule.JobOf(1,false)==16 && AlarmSchedule.JobOf(2,false)==17 && AlarmSchedule.JobOf(3,false)==17
                && AlarmSchedule.JobOf(4,false)==18 && AlarmSchedule.JobOf(100,false)==18 && AlarmSchedule.JobOf(0,true)==18 && AlarmSchedule.JobOf(9,false)==0,"gathering type to job");
        }),
        ("Game alarm commands use the game's /alarm syntax with an ET daily time, a 0-60 reminder, a sound and a 20-character name",()=>
        {
            var alarm = new TenshoAlarm { Label = "Minerai \"rare\" de Sharlayan · 23:08 ET", Repeat = AlarmRepeat.EorzeaDaily, EtMinute = 23 * 60 + 8, LeadMinutes = 90 };
            var command = GameAlarmCommand.Build(alarm,0,true);
            Check(command == "/alarm \"Minerai rare de 2308\" et rp 2308 60 se00 mute","command: " + command);
            Check(GameAlarmCommand.Name("Cuivre",480) == "Cuivre 0800" && GameAlarmCommand.Name("",0) == "0000","names");
            Check(GameAlarmCommand.Build(new TenshoAlarm { Repeat = AlarmRepeat.Once },1,false) is null,"only daily ET alarms");
            Check(GameAlarmCommand.IsAlarmCommand(command!) && !GameAlarmCommand.IsAlarmCommand("/say hi") && !GameAlarmCommand.IsAlarmCommand("/alarm \"a\"\n/say"),"only alarm commands");
        }),
        ("0.556: an alarm can ring again on time, once, after really ringing ahead",() =>
        {
            var at=new DateTimeOffset(2026,11,5,12,0,0,TimeSpan.Zero);
            TenshoAssistant.Alarms.TenshoAlarm Alarm()=>new(){Repeat=TenshoAssistant.Alarms.AlarmRepeat.Once,At=at,LeadMinutes=5};
            var alarm=Alarm();
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at) is null,"it has not rung ahead");
            alarm.LastRung=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddMinutes(-1)) is null,"not before its time");
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddSeconds(5))==at,"at its time");
            alarm.LastOnTime=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(alarm,at.AddSeconds(6)) is null,"once");
            var noLead=Alarm();noLead.LeadMinutes=0;noLead.LastRung=at;
            Check(TenshoAssistant.Alarms.AlarmSchedule.OnTimeDue(noLead,at.AddSeconds(5)) is null,"no lead: the first ring was on time");
            Check(!new TenshoAssistant.Alarms.AlarmSettings().RingOnTime,"off by default");
        }),
        ("0.557: an alarm can be put off and edited; the original stays",() =>
        {
            var now=new DateTimeOffset(2026,11,5,12,0,0,TimeSpan.Zero);
            var alarm=new TenshoAssistant.Alarms.TenshoAlarm{Label="Mandragora",Group="G",ItemId=4,Repeat=TenshoAssistant.Alarms.AlarmRepeat.EorzeaDaily,EtMinute=60};
            var later=TenshoAssistant.Alarms.AlarmSchedule.Snooze(alarm,now,5);
            Check(later.Label=="Mandragora" && later.Group=="G" && later.ItemId==4 && later.Repeat==TenshoAssistant.Alarms.AlarmRepeat.Once && later.At==now.AddMinutes(5) && later.Id!=alarm.Id,"a new one-off alarm");
            Check(alarm.Repeat==TenshoAssistant.Alarms.AlarmRepeat.EorzeaDaily,"the original stays");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Snooze(alarm,now,0).At==now.AddMinutes(1),"at least a minute");
            Check(Read("MainWindow.Alarms.cs").Contains("if(alarm.AutoKey.Length==0 && ImGui.SmallButton(localization.Get(\"Alarms.Edit\")"),"an automatic alarm is not edited");
        }),
        ("0.577: alarms in real time repeat by day, week or game reset",() =>
        {
            var at=new DateTimeOffset(2026,11,5,20,0,0,TimeSpan.Zero);
            TenshoAssistant.Alarms.TenshoAlarm Alarm(TenshoAssistant.Alarms.AlarmRepeat repeat)=>new(){Repeat=repeat,At=at};
            var daily=Alarm(TenshoAssistant.Alarms.AlarmRepeat.RealDaily);
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at.AddHours(-1))==at,"before its first time");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at)==at.AddDays(1),"strictly after");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(daily,at.AddDays(3).AddHours(1))==at.AddDays(4),"days later");
            var weekly=Alarm(TenshoAssistant.Alarms.AlarmRepeat.RealWeekly);
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(weekly,at.AddDays(1))==at.AddDays(7),"a week on");
            var reset=TenshoAssistant.Alarms.AlarmSchedule.Next(Alarm(TenshoAssistant.Alarms.AlarmRepeat.DailyReset),at);
            Check(reset==TenshoAssistant.Reminders.ResetSchedule.NextDaily(at,TenshoAssistant.Reminders.ResetSchedule.DailyHour),"the daily reset");
            Check(TenshoAssistant.Alarms.AlarmSchedule.Next(Alarm(TenshoAssistant.Alarms.AlarmRepeat.WeeklyReset),at)>at,"the weekly reset");
        }),
    ];
}
