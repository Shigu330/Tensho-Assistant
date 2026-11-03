using TenshoAssistant.Community;

namespace TenshoAssistant.Gathering;

/// <summary>A real-time window when a fish can bite: its ET hours, its weather and the previous weather all hold.</summary>
public sealed record FishWindow(DateTimeOffset StartsAt,DateTimeOffset EndsAt);

/// <summary>
/// g, R-02 (0.396): the next real windows of a fish from GatherBuddy's conditions (ET minutes, weather ids, previous weather ids;
/// Apache-2.0 data already used since 0.227) and the zone's weather from the game's tables (WeatherCatalog). Weather periods last 8 ET
/// hours (1 400 real seconds); one ET minute is 175/60 real seconds. A period whose weather cannot be known makes the answer unknown
/// (null), never « no window ». Searched up to 35 days ahead (rare fish).
/// </summary>
public static class FishWindows
{
    public const int MaximumPeriods=2160;
    private const double SecondsPerEtMinute=175.0/60;

    public static IReadOnlyList<FishWindow>? Next(GatherBuddyFish fish,WeatherCatalog catalog,uint territory,DateTimeOffset now,int count=5,int maxPeriods=MaximumPeriods)
    {
        var needsWeather=fish.Weathers.Count>0 || fish.Transitions.Count>0;
        var timed=fish.StartMinute is not null && fish.EndMinute is not null;
        if(!needsWeather && !timed)return [];
        var windows=new List<FishWindow>();
        var start=DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds()/1400*1400);
        WeatherPeriod? previous=needsWeather ? catalog.Forecast(territory,start.AddSeconds(-1400),1)[0]:null;
        for(var done=0;done<maxPeriods && windows.Count<=count;done+=24)
        {
            foreach(var period in catalog.Forecast(territory,start.AddSeconds(done*1400L),24))
            {
                var before=previous;previous=period;
                if(needsWeather)
                {
                    if(fish.Weathers.Count>0 && period.Weather is null || fish.Transitions.Count>0 && before?.Weather is null)return null;
                    if(fish.Weathers.Count>0 && !fish.Weathers.Contains(period.Weather!.Id))continue;
                    if(fish.Transitions.Count>0 && !fish.Transitions.Contains(before!.Weather!.Id))continue;
                }
                var first=period.EtHour*60;
                foreach(var (from,to) in Minutes(fish))
                {
                    var open=Math.Max(from,first);var close=Math.Min(to,first+480);
                    if(open>=close)continue;
                    var window=new FishWindow(period.StartsAt.AddSeconds((open-first)*SecondsPerEtMinute),period.StartsAt.AddSeconds((close-first)*SecondsPerEtMinute));
                    if(window.EndsAt<=now)continue;
                    // Joined to the previous window when they touch (same window across two weather periods).
                    if(windows.Count>0 && (window.StartsAt-windows[^1].EndsAt).Duration()<TimeSpan.FromSeconds(1))windows[^1]=windows[^1] with{EndsAt=window.EndsAt};
                    else windows.Add(window);
                }
            }
        }
        return windows.Take(count).ToArray();
    }

    // The fish's ET minutes of the day as one or two ranges (a window over midnight is split); no window = the whole day.
    private static IEnumerable<(int From,int To)> Minutes(GatherBuddyFish fish)
    {
        if(fish.StartMinute is not { } start || fish.EndMinute is not { } end){yield return (0,1440);yield break;}
        start=((start%1440)+1440)%1440;end=end%1440==0 && end!=0 ? 1440:((end%1440)+1440)%1440;
        if(start<end){yield return (start,end);yield break;}
        yield return (start,1440);
        if(end>0)yield return (0,end);
    }
}
