using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

// 0.1128 (X-158, user report of 12/11): one calculation for « possible now », the list's colour and filter, the tooltip and the next
// window. The Highbridge case with the game's own data (docs/evidence/x158-highbridge-window.md): refused at 0:32 (23:32 UTC), opens at
// 00:14:35 UTC, accepted at 1:14 and open until 00:23:20 UTC; weather from the real Eastern Thanalan table through WeatherCatalog.
internal static class VistaWindow1128Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n","\n");
    private static DateTimeOffset Utc(string text)=>DateTimeOffset.Parse(text+"Z",System.Globalization.CultureInfo.InvariantCulture);

    private const uint EasternThanalan=145;
    private static readonly WeatherCatalog Catalog=new(
        [new(1,"Clear Skies",60201,"Clear Skies"),new(2,"Fair Skies",60202,"Fair Skies"),new(3,"Clouds",60203,"Clouds"),new(4,"Fog",60204,"Fog"),new(7,"Rain",60207,"Rain"),new(8,"Showers",60208,"Showers")],
        [new(EasternThanalan,11,[new(1,40),new(2,20),new(3,10),new(4,10),new(7,5),new(8,15)])],"test");
    private static uint? WeatherAt(DateTimeOffset time)=>Catalog.Forecast(EasternThanalan,time,1)[0].Weather?.Id;
    private static readonly uint[] Highbridge=[1,2];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Vista window: Highbridge refused at 0:32, next window 1:14, accepted then and open until 1:23",() =>
        {
            Check(Catalog.WeatherBySourceName("Clear Skies",EasternThanalan)?.Id==1 && Catalog.WeatherBySourceName("Fair Skies",EasternThanalan)?.Id==2,"the wiki's names resolve in the zone");
            var refused=VistaWindow.At(500,759,Highbridge,WeatherAt,Utc("2026-11-11T23:32:00"),true);
            Check(refused.State==VistaOpen.Closed && !refused.TimeOk && refused.WeatherOk==true,"0:32: closed by the time only, the weather was right: "+refused);
            Check(refused.Opens is { } opens && opens>=Utc("2026-11-12T00:14:35") && opens<Utc("2026-11-12T00:14:36"),"next window 00:14:35 UTC: "+refused.Opens?.ToString("O"));
            var listed=VistaWindow.At(500,759,Highbridge,WeatherAt,Utc("2026-11-11T23:32:00"),false);
            Check(listed.State==VistaOpen.Closed && listed.Opens is null && listed.SearchedDays==0,"the list's « now » is the same answer, without the search");
            var accepted=VistaWindow.At(500,759,Highbridge,WeatherAt,Utc("2026-11-12T00:14:40"),true);
            Check(accepted.State==VistaOpen.Open && accepted.TimeOk && accepted.WeatherOk==true,"1:14: open");
            Check(accepted.Until is { } until && until>=Utc("2026-11-12T00:23:20") && until<Utc("2026-11-12T00:23:21"),"until 08:00 ET, 00:23:20 UTC: "+accepted.Until?.ToString("O"));
            Check(accepted.Opens is { } after && after>accepted.Until,"and the window after that one");
            var justBefore=VistaWindow.At(500,759,Highbridge,WeatherAt,Utc("2026-11-12T00:14:30"),false);
            Check(justBefore.State==VistaOpen.Closed,"five seconds before 05:00 ET: still closed");
        }),
        ("Vista window: weather missing, weather unknown, any weather, wrap past midnight, never an empty answer",() =>
        {
            // The Invisible City (08:00–11:59, Clouds 10 %): a next window is found within the searched days.
            var city=VistaWindow.At(800,1159,[3u],WeatherAt,Utc("2026-11-12T00:14:40"),true);
            Check(city.State==VistaOpen.Closed && city.Opens is not null && city.SearchedDays==VistaWindow.SearchDays,"a rare weather is found: "+city);
            var unknown=VistaWindow.At(500,759,null,WeatherAt,Utc("2026-11-12T00:14:40"),true);
            Check(unknown.State==VistaOpen.TimeOnly && unknown.WeatherOk is null && unknown.Until is not null && unknown.Opens is not null,"unknown weather: time only, with its next time window");
            var closedUnknown=VistaWindow.At(500,759,null,WeatherAt,Utc("2026-11-11T23:32:00"),true);
            Check(closedUnknown.State==VistaOpen.Closed && closedUnknown.WeatherOk is null && closedUnknown.Opens is not null,"outside the time, weather unknown: closed, next time window");
            var any=VistaWindow.At(0,0,[],WeatherAt,Utc("2026-11-12T00:14:40"),true);
            Check(any.State==VistaOpen.Open && any.Until is null && any.Opens is null,"no time, no weather: always open");
            var night=VistaWindow.At(1800,459,[],WeatherAt,Utc("2026-11-11T23:32:00"),false);
            Check(night.State==VistaOpen.Closed,"14:24 ET is outside 18:00–04:59");
            var nightOpen=VistaWindow.At(1800,459,[],WeatherAt,Utc("2026-11-12T00:14:00"),false);
            Check(nightOpen.State==VistaOpen.Open && nightOpen.Until is not null,"04:48 ET is inside 18:00–04:59, until 05:00");
            var never=VistaWindow.At(500,759,[99u],WeatherAt,Utc("2026-11-12T00:14:40"),true,2);
            Check(never.State==VistaOpen.Closed && never.Opens is null && never.SearchedDays==2,"no window: said with the days searched");
            var forecastUnknown=VistaWindow.At(500,759,Highbridge,_=>null,Utc("2026-11-12T00:14:40"),true,1);
            Check(forecastUnknown.State==VistaOpen.TimeOnly && forecastUnknown.Opens is null,"a forecast that cannot be read is never « possible »");
        }),
        ("Vista window: the list, the colour, the filter, the tooltip and the next window use it, off the frame for the search",() =>
        {
            var log=Read("MainWindow.VistaLog.cs");
            Check(log.Contains("Now:VistaStateAt(vista,catalog?.Find(vista.MapId)?.TerritoryId ?? 0,utc,false)") && log.Contains("row.Now.State!=VistaOpen.Closed")
                && log.Contains("possible.State==VistaOpen.Open ? UI.FfxivTheme.Success"),"list, filter and colour");
            Check(log.Contains("var search=Task.Run(()=>VistaWindow.At(min,max,weathers,") && log.Contains("localization.Get(\"MapView.VistaSearching\")"),"the search is off the frame and says so meanwhile");
            Check(log.Contains("localization.Format(\"MapView.VistaNoWindow\",next.SearchedDays)"),"never an empty line");
            var map=Read("MainWindow.MapView.cs");
            Check(map.Contains("var stateNow=VistaStateAt(vista,territory,DateTimeOffset.UtcNow,false);") && map.Contains("stateNow.TimeOk ?") && map.Contains("stateNow.WeatherOk is { } weatherOk")
                && !map.Contains("vista.InWindow(now)"),"tooltip lines from the same state");
            Check(!Read("MainWindow.Alarms.cs").Contains("AlarmSchedule.Occurrences(new TenshoAlarm{Repeat=AlarmRepeat.EorzeaDaily,EtMinute=start},DateTimeOffset.UtcNow.AddMinutes(-1),40)"),"the old 40-day search is gone");
        }),
    ];
}
