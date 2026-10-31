using TenshoAssistant.Gathering;

internal static class GatheringTimelineTests
{
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Gathering ET clock rational minute boundaries and real day wrap",()=>
        {
            var epoch=DateTimeOffset.UnixEpoch;
            Check(GatheringTime.MinuteOfDay(epoch.AddSeconds(175))==60 && GatheringTime.MinuteOfDay(epoch.AddSeconds(4200))==0,"hour and day");
            var boundary=(35L*TimeSpan.TicksPerSecond+11)/12;
            Check(GatheringTime.MinuteOfDay(epoch.AddTicks(boundary-1))==0 && GatheringTime.MinuteOfDay(epoch.AddTicks(boundary))==1,"first ET minute within one tick");
            Check(GatheringTime.MinuteOfDay(epoch.AddSeconds(1400).ToOffset(TimeSpan.FromHours(9)))==480,"UTC normalization");
        }),
        ("Gathering opening starts inclusive ends exclusive and recurs next ET day",()=>
        {
            var schedule=GatheringSchedule.FromEphemeral(100,200);var epoch=DateTimeOffset.UnixEpoch;
            var first=GatheringTime.Next(schedule,epoch.AddSeconds(175));Check(first.Openings[0].Current && first.Openings[0].StartsAt==epoch.AddSeconds(175) && first.Openings[0].EndsAt==epoch.AddSeconds(350),"one ET hour =175 seconds");
            var ended=GatheringTime.Next(schedule,epoch.AddSeconds(350));Check(!ended.Openings[0].Current && ended.Openings[0].StartsAt==epoch.AddSeconds(4375),"end switches to next day");
        }),
        ("Gathering split midnight windows merge across days without false interruption",()=>
        {
            var schedule=GatheringSchedule.FromEphemeral(2200,200);var epoch=DateTimeOffset.UnixEpoch;
            var current=GatheringTime.Next(schedule,epoch.AddSeconds(175));
            Check(current.Openings[0].Current && current.Openings[0].StartsAt==epoch.AddSeconds(-350) && current.Openings[0].EndsAt==epoch.AddSeconds(350),"prior-day22 through current-day02");
            Check(current.Openings[1].StartsAt==epoch.AddSeconds(3850) && current.Openings[1].EndsAt==epoch.AddSeconds(4550),"next contiguous midnight span");
        }),
        ("Gathering unknown unrestricted and no-window schedules stay distinct",()=>
        {
            foreach(var schedule in new[]{GatheringSchedule.Unknown,GatheringSchedule.FromEphemeral(ushort.MaxValue,ushort.MaxValue),GatheringSchedule.FromRare([]),GatheringSchedule.FromEphemeral(2500,100)})
            {
                var result=GatheringTime.Next(schedule,DateTimeOffset.UnixEpoch);Check(result.Kind==schedule.Kind && result.Openings.Count==0,"no fabricated opening");
            }
            Throws<ArgumentOutOfRangeException>(()=>GatheringTime.Next(GatheringSchedule.Unknown,DateTimeOffset.UnixEpoch,0));
            Throws<ArgumentOutOfRangeException>(()=>GatheringTime.MinuteOfDay(DateTimeOffset.UnixEpoch.AddSeconds(-1)));
        }),
        ("Gathering openings match independent minute membership through two ET days",()=>
        {
            var schedule=GatheringSchedule.FromRare([(100,200),(900,300),(2200,400)]);var epoch=DateTimeOffset.UnixEpoch;
            for(var minute=0;minute<2880;minute++)
            {
                var time=epoch.AddTicks((minute*35L*TimeSpan.TicksPerSecond+11)/12);var expected=minute%1440 is >=60 and <180 or >=540 and <720 or >=1320 or <120;
                var first=GatheringTime.Next(schedule,time,1).Openings[0];Check(first.Current==expected,"independent native HHmm membership");
            }
            var result=GatheringTime.Next(schedule,epoch);Throws<NotSupportedException>(()=>((IList<GatheringOpening>)result.Openings).Clear());
        })
    };
}
