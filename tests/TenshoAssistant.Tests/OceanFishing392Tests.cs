using TenshoAssistant.Gathering;

// 0.392 (g, R-01): ocean fishing timetable — loop slot k leaves at the loop start + 2 h × k, boarding open 15 minutes, route filter,
// moments of the day (IKDTimeDefine 1 night, 2 day, 3 sunset), a voyage named after its last stop.
internal static class OceanFishing392Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static OceanRoute Route(uint id,OceanTime last)=>new(id,"R"+id,[new("A",OceanTime.Night),new("B",OceanTime.Day),new("C",last)]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean fishing: departures every 2 hours from the loop start, boarding 15 minutes, filter by voyage, loop wraps",() =>
        {
            var routes = new Dictionary<uint,OceanRoute>{[7]=Route(7,OceanTime.Sunset),[10]=Route(10,OceanTime.Sunset),[1]=Route(1,OceanTime.Sunset),[19]=Route(19,OceanTime.Sunset),[13]=Route(13,OceanTime.Sunset)};
            var schedule = new OceanSchedule([(7,19),(10,13),(1,19)],routes);
            Check(OceanSchedule.LoopStart==new DateTimeOffset(1970,1,9,16,0,0,TimeSpan.Zero),"loop start");
            var slot = OceanSchedule.LoopStart+TimeSpan.FromHours(2)*3000;
            var next = schedule.Next(slot+TimeSpan.FromMinutes(5),3);
            Check(next[0].At==slot && next[0].Aldenard!.Id==7 && next[1].At==slot+TimeSpan.FromHours(2) && next[1].Othard!.Id==13,"boarding still open, loop index 3000 % 3 = 0");
            Check(schedule.Next(slot+TimeSpan.FromMinutes(16),1)[0].At==slot+TimeSpan.FromHours(2),"boarding closed after 15 minutes");
            var filtered = schedule.Next(slot+TimeSpan.FromMinutes(16),2,1);
            Check(filtered.All(departure=>departure.Aldenard!.Id==1) && filtered[0].At==slot+TimeSpan.FromHours(4) && filtered[1].At==slot+TimeSpan.FromHours(10),"filter by voyage");
            Check(OceanSchedule.TimeOf(1)==OceanTime.Night && OceanSchedule.TimeOf(2)==OceanTime.Day && OceanSchedule.TimeOf(3)==OceanTime.Sunset && OceanSchedule.TimeOf(9)==OceanTime.Unknown,"moments");
            Check(routes[7].Label==OceanTime.Sunset,"named after the last stop");
        }),
    ];
}
