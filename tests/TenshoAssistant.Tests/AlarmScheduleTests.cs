using TenshoAssistant.Alarms;

internal static class AlarmScheduleTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
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
        ("Fashion Report weeks start on Tuesday 08:00 UTC and the slots follow the game window",()=>
        {
            var start=new DateTimeOffset(2018,1,30,8,0,0,TimeSpan.Zero);
            Check(TenshoAssistant.Fashion.FashionWeek.Number(start)==0 && TenshoAssistant.Fashion.FashionWeek.Number(start.AddDays(7).AddSeconds(-1))==0,"first week");
            Check(TenshoAssistant.Fashion.FashionWeek.Number(start.AddDays(7))==1 && TenshoAssistant.Fashion.FashionWeek.ThemeRow(start.AddDays(7))==10,"second week, theme row + 9");
            var (begin,next)=TenshoAssistant.Fashion.FashionWeek.Bounds(start.AddDays(10));
            Check(begin==start.AddDays(7) && next==start.AddDays(14),"week bounds");
            Check(TenshoAssistant.Fashion.FashionWeek.Slots.Length==11 && TenshoAssistant.Fashion.FashionWeek.Slots[0]==TenshoAssistant.Items.EquipmentSlots.MainHand
                && TenshoAssistant.Fashion.FashionWeek.Slots[10]==TenshoAssistant.Items.EquipmentSlots.Ring,"11 slots, weapon first, rings last");
        }),
        ("Bountiful II estimate: +3 from 110 % of the base, +2 from 90 %, else +1, unknown without rating or base",()=>
        {
            // Base 3420 = ItemLevel.Gathering of a level 90 ore (docs/evidence/0.224-bountiful-base.md): steps at 3078 and 3762.
            Check(TenshoAssistant.Gathering.BountifulEstimate.Bonus(3762,3420)==3 && TenshoAssistant.Gathering.BountifulEstimate.Bonus(3761,3420)==2,"+3 step");
            Check(TenshoAssistant.Gathering.BountifulEstimate.Bonus(3078,3420)==2 && TenshoAssistant.Gathering.BountifulEstimate.Bonus(3077,3420)==1,"+2 step");
            Check(TenshoAssistant.Gathering.BountifulEstimate.Bonus(0,3420) is null && TenshoAssistant.Gathering.BountifulEstimate.Bonus(4000,0) is null,"unknown");
            Check(TenshoAssistant.Gathering.BountifulEstimate.NextStep(3000,3420)==3078 && TenshoAssistant.Gathering.BountifulEstimate.NextStep(3500,3420)==3762
                && TenshoAssistant.Gathering.BountifulEstimate.NextStep(4000,3420) is null,"next step");
        }),
    ];
}
