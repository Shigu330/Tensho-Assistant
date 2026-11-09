using TenshoAssistant.Community;
using TenshoAssistant.Gathering;

// 0.396 (g, R-02): next real fish windows — ET window, weather and previous weather together, windows over midnight joined, unknown
// weather = unknown answer, searched far ahead; checked against a step-by-step scan of the same weather tables.
internal static class FishWindows396Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GatherBuddyFish Fish(int? start,int? end,uint[] weathers,uint[] previous)=>new(1,[],[],null,null,start,end,weathers,previous,null,[],false);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fish windows: conditions held together, checked minute by minute against the weather tables",() =>
        {
            var catalog = new WeatherCatalog([new(1,"Clair",1),new(2,"Pluie",2),new(3,"Brume",3)],[new(10,1,[new(1,40),new(2,40),new(3,20)])],"test");
            var now = new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var fish = Fish(22*60,2*60,[1],[2]);
            var windows = FishWindows.Next(fish,catalog,10,now,3) ?? throw new Exception("unknown");
            Check(windows.Count>0,"some window");
            foreach (var window in windows)
            {
                // Inside: weather clear, previous rain, ET between 22:00 and 02:00; just before the start: one condition fails.
                foreach (var at in new[]{window.StartsAt.AddSeconds(1),window.EndsAt.AddSeconds(-1)})
                {
                    var period = catalog.Forecast(10,at,1)[0];var before = catalog.Forecast(10,period.StartsAt.AddSeconds(-1400),1)[0];
                    var minute = (int)(at.ToUnixTimeSeconds()*60/175%1440);
                    Check(period.Weather!.Id==1 && before.Weather!.Id==2 && (minute>=22*60 || minute<2*60),$"inside {at:u}");
                }
                var outside = window.StartsAt.AddSeconds(-3);
                var p = catalog.Forecast(10,outside,1)[0];var b = catalog.Forecast(10,p.StartsAt.AddSeconds(-1400),1)[0];
                var m = (int)(outside.ToUnixTimeSeconds()*60/175%1440);
                Check(!(p.Weather!.Id==1 && b.Weather!.Id==2 && (m>=22*60 || m<2*60)) || window.StartsAt<=now,"closed just before");
            }
            Check(windows.Zip(windows.Skip(1)).All(pair=>pair.First.EndsAt<pair.Second.StartsAt),"separate, in order");
        }),
        ("Fish windows: no condition = always; unknown weather = unknown, never « none »",() =>
        {
            var catalog = new WeatherCatalog([new(1,"Clair",1)],[new(10,1,[new(1,100)],true)],"test");
            Check(FishWindows.Next(Fish(null,null,[],[]),catalog,10,DateTimeOffset.UtcNow)!.Count==0,"always");
            Check(FishWindows.Next(Fish(null,null,[1],[]),catalog,10,DateTimeOffset.UtcNow) is null,"individual weather: unknown");
            // 0.1019 (test audit #13): a fixed time outside the window (10:00 ET), so the count and the length are always checked.
            var tenEt = DateTimeOffset.FromUnixTimeSeconds(1_700_002_150);
            Check((int)(tenEt.ToUnixTimeSeconds()*60/175%1440)==600,"fixed time is 10:00 ET");
            var timeOnly = FishWindows.Next(Fish(60,120,[],[]),catalog,10,tenEt,2)!;
            Check(timeOnly.Count==2,"two windows: "+timeOnly.Count);
            Check(timeOnly.All(window=>Math.Abs((window.EndsAt-window.StartsAt).TotalSeconds-175)<1),"one ET hour = 175 s");
            Check(timeOnly[0].StartsAt>tenEt && Math.Abs((timeOnly[0].StartsAt-tenEt).TotalSeconds-15*175)<1,"next 1:00 ET, 15 ET hours later");
        }),
    ];
}
