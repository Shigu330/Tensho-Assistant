// 0.906 (X-76): one Eorzea minute formula, one « HH:mm » text and one open/left helper for a gathering opening.
using TenshoAssistant.Gathering;
internal static class EorzeaTime906Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Eorzea clock text: two digits for the hour and the minute",() =>
        {
            Check(GatheringTime.Clock(0)=="00:00","midnight");
            Check(GatheringTime.Clock(61)=="01:01","one past one");
            Check(GatheringTime.Clock(1439)=="23:59","last minute");
        }),
        ("Gathering opening: open or not at a time, with the time left until it closes or opens",() =>
        {
            var day=new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero);
            var opening=new GatheringOpening(day.AddHours(10),day.AddHours(12),false);
            Check(opening.At(day.AddHours(9))==(false,TimeSpan.FromHours(1)),"closed, opens in one hour");
            Check(opening.At(day.AddHours(11))==(true,TimeSpan.FromHours(1)),"open, closes in one hour");
        }),
        ("Eorzea minute of the day: the shared formula equals the old millisecond one",() =>
        {
            DateTimeOffset[] dates=[DateTimeOffset.UnixEpoch,DateTimeOffset.UnixEpoch.AddSeconds(175),new(2026,11,8,13,37,42,123,TimeSpan.Zero),
                new(2024,2,29,23,59,59,999,TimeSpan.Zero),new(2030,1,1,0,0,0,1,TimeSpan.FromHours(9))];
            foreach(var date in dates)
                Check((int)(date.ToUnixTimeMilliseconds()*3600/175/1000/60%1440)==GatheringTime.MinuteOfDay(date),"same minute at "+date.ToString("O"));
        }),
    ];
}
