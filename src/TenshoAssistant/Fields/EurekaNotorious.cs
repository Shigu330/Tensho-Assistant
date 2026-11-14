namespace TenshoAssistant.Fields;

/// <summary>Who the condition is for: the notorious monster itself, or the monster whose defeat calls it (its « spawner »).</summary>
public enum EurekaConditionOn { Monster,Spawner }

/// <summary>
/// X-205 (0.1241, user request of 2026-11-12, K-35): one notorious monster of Eureka with a weather or night condition. Fate: the FATE row
/// (its name is the game's); Weather: the Weather row it needs (null: none), for the monster or its spawner; Night: its spawner appears at
/// night only. Community knowledge (DEC-044), cited in docs/evidence/0.1241-eureka-notorious.md.
/// </summary>
public sealed record EurekaNotorious(uint Fate,uint Territory,uint? Weather,EurekaConditionOn WeatherOn,bool Night);

/// <summary>One real-time window when a notorious monster's condition holds.</summary>
public readonly record struct EurekaWindow(DateTimeOffset Start,DateTimeOffset End);

/// <summary>
/// X-205 (0.1241): the notorious monsters of Eureka whose appearance depends on the weather or the night, and their next windows from the
/// zone's weather forecast. Conditions from Eureka Helper (snorux/EurekaHelper, Apache-2.0, commit 59fab9e, XIV/Zones/Eureka*.cs:
/// SpawnRequiredWeather, SpawnByRequiredWeather, SpawnByRequiredNight); FATE ids and weathers checked against the game's sheets
/// (PROBE_EUREKANM: each FATE's English name is the source's, each weather is in its zone's WeatherRate). The 21 others need neither.
/// </summary>
public static class EurekaNotoriousBoard
{
    public const uint Anemos=732,Pagos=763,Pyros=795,Hydatos=827;
    // Weather rows (PROBE_EUREKANM, English): 4 Fog, 6 Gales, 9 Thunder, 14 Heat Waves, 16 Blizzards, 49 Umbral Wind.
    private const uint Fog=4,Gales=6,Thunder=9,HeatWaves=14,Blizzards=16,UmbralWind=49;

    /// <summary>
    /// Night as the source counts it for a spawner (EorzeaTime.NextNightTime: from 19:00 to 06:00 Eorzea time). The source's day is
    /// 06:00 to 18:00, so 18:00 to 19:00 is left out: a window shown is inside both, never outside.
    /// </summary>
    public const int NightStart=19*60,NightEnd=6*60;

    public static readonly IReadOnlyList<EurekaNotorious> All=
    [
        new(1345,Anemos,Gales,EurekaConditionOn.Spawner,false),
        new(1336,Anemos,null,EurekaConditionOn.Monster,true),
        new(1343,Anemos,null,EurekaConditionOn.Monster,true),
        new(1331,Anemos,null,EurekaConditionOn.Monster,true),
        new(1338,Anemos,null,EurekaConditionOn.Monster,true),
        new(1329,Anemos,Gales,EurekaConditionOn.Monster,true),
        new(1369,Pagos,null,EurekaConditionOn.Monster,true),
        new(1355,Pagos,Fog,EurekaConditionOn.Spawner,false),
        new(1360,Pagos,Fog,EurekaConditionOn.Monster,false),
        new(1362,Pagos,Thunder,EurekaConditionOn.Monster,false),
        new(1359,Pagos,HeatWaves,EurekaConditionOn.Monster,false),
        new(1365,Pagos,Blizzards,EurekaConditionOn.Monster,false),
        new(1364,Pagos,null,EurekaConditionOn.Monster,true),
        new(1388,Pyros,null,EurekaConditionOn.Monster,true),
        new(1389,Pyros,Thunder,EurekaConditionOn.Spawner,false),
        new(1392,Pyros,UmbralWind,EurekaConditionOn.Monster,false),
        new(1393,Pyros,null,EurekaConditionOn.Monster,true),
        new(1399,Pyros,Thunder,EurekaConditionOn.Monster,false),
        new(1403,Pyros,Blizzards,EurekaConditionOn.Monster,false),
        new(1404,Pyros,HeatWaves,EurekaConditionOn.Monster,false),
        new(1418,Hydatos,null,EurekaConditionOn.Monster,true),
    ];

    /// <summary>The periods the page reads: 72 of 8 Eorzea hours, about 28 real hours.</summary>
    public const int ForecastPeriods=72;

    /// <summary>
    /// X-323 (0.1318, user report of 2026-11-14: every row « none in the next 28 hours », night monsters included): the zone's forecast
    /// over <paramref name="count"/> periods from the one holding <paramref name="now"/>, asked of the weather catalogue 24 periods at a
    /// time, the most it gives (WeatherCatalog.Forecast throws above 24: asking 72 at once threw, the page caught it and had no forecast).
    /// </summary>
    public static IReadOnlyList<(DateTimeOffset Start,DateTimeOffset End,int EtHour,uint? Weather)> Forecast(Gathering.WeatherCatalog catalog,uint territory,DateTimeOffset now,int count=ForecastPeriods)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var periods=new List<(DateTimeOffset Start,DateTimeOffset End,int EtHour,uint? Weather)>(Math.Max(count,0));
        while(periods.Count<count)
            foreach(var period in catalog.Forecast(territory,periods.Count==0 ? now:periods[^1].End,Math.Min(24,count-periods.Count)))
                periods.Add((period.StartsAt,period.EndsAt,period.EtHour,period.Weather?.Id));
        return periods.AsReadOnly();
    }

    /// <summary>
    /// X-323 (0.1318), for the self-check in the game: from the catalogue read, how many monsters have a window in the forecast, and each
    /// monster needing the night alone whose next window does not open within one Eorzea day (70 real minutes), which cannot be
    /// (« fate N: no night window »).
    /// </summary>
    public static (int WithWindow,IReadOnlyList<string> Wrong) ForecastCheck(Gathering.WeatherCatalog catalog,DateTimeOffset now)
    {
        var withWindow=0;var wrong=new List<string>();
        foreach(var zone in All.GroupBy(monster=>monster.Territory))
        {
            var forecast=Forecast(catalog,zone.Key,now);
            foreach(var monster in zone)
            {
                var next=Windows(monster,forecast).Where(window=>window.End>now).Select(window=>(EurekaWindow?)window).FirstOrDefault();
                if(next is not null)withWindow++;
                if(monster is { Night:true, Weather:null } && (next is not { } night || night.Start>now.AddSeconds(4200)))wrong.Add($"fate {monster.Fate}: no night window");
            }
        }
        return (withWindow,wrong);
    }

    /// <summary>
    /// The windows when the condition holds, in order, adjacent ones joined, from 8-hour weather periods (start, end, Eorzea hour of the
    /// start: 0, 8 or 16; weather row, null when unknown). A period whose weather is unknown is never a window when a weather is needed.
    /// </summary>
    public static IReadOnlyList<EurekaWindow> Windows(EurekaNotorious monster,IEnumerable<(DateTimeOffset Start,DateTimeOffset End,int EtHour,uint? Weather)> periods)
    {
        var windows=new List<EurekaWindow>();
        foreach(var period in periods.OrderBy(period=>period.Start))
        {
            if(monster.Weather is { } needed && period.Weather!=needed)continue;
            foreach(var (from,to) in Hours(monster.Night,period.EtHour))
            {
                // Exact in ticks (a period lasts 1,400 real seconds, 175 an Eorzea hour): no rounding of a minute's length.
                var ticks=(period.End-period.Start).Ticks;
                var start=period.Start+TimeSpan.FromTicks(ticks*(from-period.EtHour*60)/(8*60));
                var end=period.Start+TimeSpan.FromTicks(ticks*(to-period.EtHour*60)/(8*60));
                if(windows.Count>0 && windows[^1].End>=start)windows[^1]=windows[^1] with { End=end>windows[^1].End ? end:windows[^1].End };
                else windows.Add(new(start,end));
            }
        }
        return windows;
    }

    // The Eorzea minutes of a period (8 hours from etHour) when the condition's time part holds: all of it, or its night part.
    private static IEnumerable<(int From,int To)> Hours(bool night,int etHour)
    {
        var from=etHour*60;var to=from+8*60;
        if(!night){yield return (from,to);yield break;}
        if(from<NightEnd)yield return (from,Math.Min(to,NightEnd));
        if(to>NightStart)yield return (Math.Max(from,NightStart),to);
    }

    /// <summary>
    /// R06, for the self-check in the game: each monster whose FATE is not among its zone's FATEs, or whose weather is not one of its zone's
    /// weathers, as « fate N: why » (empty: the table agrees with the game's files read).
    /// </summary>
    public static IReadOnlyList<string> Mismatches(Func<uint,uint,bool> fateInZone,Func<uint,uint,bool> weatherInZone)
        =>[.. All.Where(monster=>!fateInZone(monster.Territory,monster.Fate)).Select(monster=>$"fate {monster.Fate}: not a FATE of territory {monster.Territory}")
            .Concat(All.Where(monster=>monster.Weather is { } weather && !weatherInZone(monster.Territory,weather)).Select(monster=>$"fate {monster.Fate}: weather {monster.Weather} not in territory {monster.Territory}"))];

    /// <summary>
    /// The Eorzea minutes an alarm rings at (with the weather, if any, checked on the period of that minute, AlarmSchedule.ForWindows): each
    /// weather period's start for a weather alone, the night's start for the night alone, the night's start and midnight for both (the two
    /// periods a night spans).
    /// </summary>
    public static IReadOnlyList<int> AlarmMinutes(EurekaNotorious monster)=>(monster.Weather,monster.Night) switch
    {
        (not null,false)=>[0,8*60,16*60],
        (null,true)=>[NightStart],
        _=>[0,NightStart],
    };
}
