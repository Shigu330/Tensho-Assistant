using TenshoAssistant.Fields;
using TenshoAssistant.Gathering;

// 0.1318 (X-323, user report of 2026-11-14, Eureka and Bozja › Weather and night, plugin 0.1317): every row said « none in the next
// 28 hours », night monsters included. Cause: the page asked WeatherCatalog.Forecast for 72 periods at once, the catalogue throws above
// 24, the page caught it and had no forecast. EurekaNotoriousBoard.Forecast asks 24 at a time; tested on a known forecast (the game's
// weather roll, WeatherCatalog.Target, on a made-up table).
internal static class EurekaForecast1318Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private const uint Pagos=763,Hydatos=827,Blizzards=16,Clear=2;
    private static readonly DateTimeOffset Now=new(2026,11,14,10,30,17,TimeSpan.Zero);

    // Pagos: Blizzards on a roll under 30, Clear weather otherwise; Hydatos: never Blizzards.
    private static WeatherCatalog Catalog()=>new(
        [new(Blizzards,"Blizzards",0,"Blizzards"),new(Clear,"Clear Skies",0,"Clear Skies")],
        [new(Pagos,1,[new(Blizzards,30),new(Clear,70)]),new(Hydatos,2,[new(Clear,100)])],"test");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Eureka forecast 1318: the cause: the catalogue gives 24 periods at most, the board's forecast gives 72, joined, from now's period",() =>
        {
            var catalog=Catalog();
            var threw=false;
            try{catalog.Forecast(Pagos,Now,72);}catch(ArgumentOutOfRangeException){threw=true;}
            Check(threw,"the catalogue still refuses 72 periods at once (the page's former call)");
            var periods=EurekaNotoriousBoard.Forecast(catalog,Pagos,Now);
            Check(periods.Count==EurekaNotoriousBoard.ForecastPeriods && EurekaNotoriousBoard.ForecastPeriods==72,"72 periods: "+periods.Count);
            Check(periods[0].Start<=Now && periods[0].End>Now,"the first holds now");
            for(var i=1;i<periods.Count;i++)Check(periods[i].Start==periods[i-1].End && periods[i].End-periods[i].Start==TimeSpan.FromSeconds(1400),"contiguous, 1,400 s each: "+i);
            foreach(var period in periods)
            {
                var expected=WeatherCatalog.Target(period.Start)<30 ? Blizzards:Clear;
                Check(period.Weather==expected && period.EtHour==(int)(period.Start.ToUnixTimeSeconds()/175%24),$"the game's roll at {period.Start:O}: {period.Weather} vs {expected}");
            }
            Check(EurekaNotoriousBoard.Forecast(catalog,Pagos,Now,30).Count==30 && EurekaNotoriousBoard.Forecast(catalog,Pagos,Now,0).Count==0,"other counts");
        }),
        ("Eureka forecast 1318: each night monster has its next window within 70 real minutes; weather monsters when their weather comes",() =>
        {
            var catalog=Catalog();
            foreach(var monster in EurekaNotoriousBoard.All.Where(monster=>monster is { Night:true, Weather:null }))
            {
                var next=EurekaNotoriousBoard.Windows(monster,EurekaNotoriousBoard.Forecast(catalog,monster.Territory,Now)).First(window=>window.End>Now);
                Check(next.Start<=Now.AddSeconds(4200),$"fate {monster.Fate}: next night window at {next.Start:O}");
            }
            var cassie=EurekaNotoriousBoard.All.Single(monster=>monster.Fate==1365);
            Check(cassie.Territory==Pagos && cassie.Weather==Blizzards,"Cassie needs Blizzards in Pagos");
            var forecast=EurekaNotoriousBoard.Forecast(catalog,Pagos,Now);
            var firstBlizzards=forecast.First(period=>period.Weather==Blizzards&&period.End>Now);
            var window=EurekaNotoriousBoard.Windows(cassie,forecast).First(window=>window.End>Now);
            Check(window.Start==(firstBlizzards.Start>Now ? firstBlizzards.Start:forecast[0].Start),"Cassie's window is the first Blizzards period: "+window);
            var noBlizzards=EurekaNotoriousBoard.Windows(cassie with { Territory=Hydatos },EurekaNotoriousBoard.Forecast(catalog,Hydatos,Now));
            Check(noBlizzards.Count==0,"a weather that never comes: none (the page's « none in the next 28 hours » is right then)");
            var (withWindow,wrong)=EurekaNotoriousBoard.ForecastCheck(catalog,Now);
            Check(wrong.Count==0 && withWindow>=EurekaNotoriousBoard.All.Count(monster=>monster is { Night:true, Weather:null }),$"self-check: {withWindow}, {string.Join("; ",wrong)}");
        }),
        ("Eureka forecast 1318: the page and the self-check use the board's forecast, not 72 periods at once",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.EurekaNotorious.cs"));
            Check(page.Contains("EurekaNotoriousBoard.Forecast(catalog,territory,now)") && !page.Contains("Forecast(territory,now,72)"),"the page asks the board");
            var self=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SelfCheckPages.cs"));
            Check(self.Contains("Fields.EurekaNotoriousBoard.ForecastCheck(weathers,DateTimeOffset.UtcNow)"),"the self-check computes the windows in the game");
        }),
    ];
}
