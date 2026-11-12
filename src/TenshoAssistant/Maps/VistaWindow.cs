namespace TenshoAssistant.Maps;

public enum VistaOpen { Open, Closed, TimeOnly }

/// <summary>
/// The state of a vista at a time: Open (time window and weather both right), Closed, or TimeOnly (time right, weather not known).
/// TimeOk and WeatherOk say what is missing (WeatherOk null: not known). Until: when an open window closes. Opens: when the next
/// window opens (null when none within the searched days, or not searched). SearchedDays: the length of that search.
/// </summary>
public sealed record VistaWindowState(VistaOpen State,bool TimeOk,bool? WeatherOk,DateTimeOffset? Until,DateTimeOffset? Opens,int SearchedDays);

/// <summary>
/// X-158 (0.1128, user report of 12/11: the list said « possible now » for Highbridge at 0:32 while the game refused, and accepted it at
/// 1:14, the « next window » shown): the one calculation behind « possible now », the list's colour and filter, the tooltip's line and
/// the next window. Time is counted in Eorzea minutes since the epoch (one Eorzea minute = 35/12 real seconds): the window of the vista
/// (MinTime–MaxTime, hhmm, MaxTime included, wrapping past midnight) and the weather periods (480 Eorzea minutes, starting at 00, 08 and
/// 16 ET) both change on whole Eorzea minutes, so the state is exact between two of these changes and the search jumps from one to the
/// next. Weathers: null = not known (no wiki row, or a weather name not proven for the zone), empty = any weather.
/// Evidence: docs/evidence/x158-highbridge-window.md.
/// </summary>
public static class VistaWindow
{
    public const int SearchDays=30;
    private const int Day=1440, Period=480;

    public static long EorzeaMinute(DateTimeOffset time)=>time.ToUnixTimeMilliseconds()*12/35000;
    /// <summary>The real time an Eorzea minute starts (rounded up to the millisecond, so it is never before it).</summary>
    public static DateTimeOffset RealTime(long eorzeaMinute)=>DateTimeOffset.FromUnixTimeMilliseconds((eorzeaMinute*35000+11)/12);

    public static VistaWindowState At(int minTime,int maxTime,IReadOnlyCollection<uint>? weathers,Func<DateTimeOffset,uint?> weatherAt,DateTimeOffset now,bool searchNext,int days=SearchDays)
    {
        ArgumentNullException.ThrowIfNull(weatherAt);
        var anyTime=minTime==0 && maxTime==0;
        var start=minTime/100*60+minTime%100;var end=(maxTime/100*60+maxTime%100+1)%Day;
        var known=new Dictionary<long,uint?>();
        bool TimeOk(long minute)
        {
            if(anyTime)return true;
            var inDay=(int)(minute%Day);
            return start<end ? inDay>=start && inDay<end:inDay>=start || inDay<end;
        }
        bool? WeatherOk(long minute)
        {
            if(weathers is null)return null;
            if(weathers.Count==0)return true;
            var period=minute/Period;
            if(!known.TryGetValue(period,out var id))known[period]=id=weatherAt(RealTime(period*Period));
            return id is { } found ? weathers.Contains(found):null;
        }
        // Open for the search: time right and weather proven right; with unknown weathers, the time alone (said TimeOnly).
        bool Open(long minute)=>TimeOk(minute) && (weathers is null || WeatherOk(minute)==true);
        long Next(long minute)
        {
            var candidates=new List<long>{(minute/Period+1)*Period};
            if(!anyTime)
            {
                var dayStart=minute/Day*Day;
                foreach(var at in new[]{start,end})
                {
                    var candidate=dayStart+at;
                    if(candidate<=minute)candidate+=Day;
                    candidates.Add(candidate);
                }
            }
            return candidates.Min();
        }
        var now0=EorzeaMinute(now);
        var timeOk=TimeOk(now0);var weatherOk=WeatherOk(now0);
        var state=!timeOk || weatherOk==false ? VistaOpen.Closed:weatherOk==true ? VistaOpen.Open:VistaOpen.TimeOnly;
        // Any time and any weather: always open, nothing to search.
        if(anyTime && weathers is { Count:0 })return new(VistaOpen.Open,true,true,null,null,searchNext ? days:0);
        var limit=now0+(long)days*86400*12/35;
        DateTimeOffset? until=null,opens=null;
        var minute=now0;
        if(Open(now0))
        {
            // Until the first change that closes it, looked for over three Eorzea days at most (a time window lasts one at most; a weather
            // that keeps matching longer is said « possible now » without an end).
            var bound=Math.Min(limit,now0+3*Day);
            while(minute<bound && Open(minute))minute=Next(minute);
            if(Open(minute))return new(state,timeOk,weatherOk,null,null,searchNext ? days:0);
            until=RealTime(minute);
        }
        if(searchNext)
        {
            while(minute<limit && !Open(minute))minute=Next(minute);
            if(Open(minute) && minute>now0)opens=RealTime(minute);
        }
        return new(state,timeOk,weatherOk,until,opens,searchNext ? days:0);
    }
}
