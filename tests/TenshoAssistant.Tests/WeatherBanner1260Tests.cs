using TenshoAssistant.UI;

// 0.1260 (X-155, pilot of the visual plan, first screen: Weather). A banner of three blocks (now, next, after) above the table, the time
// left ticking each second, the alarm button of the table; the brief fade when the period changes is cut by Dalamud's « reduced motion »
// preference (read for the first time) and absent in the desktop application.
internal static class WeatherBanner1260Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Between(string text,string from,string to)
    {
        var start=text.IndexOf(from,StringComparison.Ordinal);
        if(start<0)throw new Exception("not found: "+from);
        var end=text.IndexOf(to,start+from.Length,StringComparison.Ordinal);
        return end<0 ? text[start..]:text[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Weather banner 1260: a highlight fades within its duration and never shows with reduced motion",() =>
        {
            Check(Motion.Pulse(1000,1000,150,false)==1,"full at the start");
            Check(Math.Abs(Motion.Pulse(1000,1075,150,false)-0.5f)<0.001f,"half way");
            Check(Motion.Pulse(1000,1150,150,false)==0 && Motion.Pulse(1000,99_000,150,false)==0,"over at the end and after");
            Check(Motion.Pulse(1000,900,150,false)==0,"nothing before the start");
            Check(Motion.Pulse(1000,1000,150,true)==0 && Motion.Pulse(1000,1075,150,true)==0,"reduced motion: none");
            Check(Motion.Pulse(1000,1000,0,false)==0 && Motion.Pulse(1000,1000,-5,false)==0,"no duration: none");
            // A start far in the past, as the fields begin (Timers828Tests: never long.MinValue).
            Check(Motion.Pulse(-100_000,5,150,false)==0,"an old start is over");
        }),
        ("Weather banner 1260: the countdown is « mm:ss », rounded up to the second, never negative",() =>
        {
            Check(Motion.Countdown(TimeSpan.FromSeconds(292))=="04:52","4 min 52 s");
            Check(Motion.Countdown(TimeSpan.FromSeconds(291.2))=="04:52","rounded up: a period never starts while a second is still shown");
            Check(Motion.Countdown(TimeSpan.FromSeconds(1400))=="23:20","a whole weather period");
            Check(Motion.Countdown(TimeSpan.FromSeconds(3892))=="1:04:52","from one hour");
            Check(Motion.Countdown(TimeSpan.Zero)=="00:00" && Motion.Countdown(TimeSpan.FromSeconds(-30))=="00:00","passed");
        }),
        ("Weather banner 1260: three blocks above the table, the current one marked, computed forecast said, motion from Dalamud's preference",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Weather.cs");
            Check(page.IndexOf("DrawWeatherBanner(now);",StringComparison.Ordinal) is var banner and >0 && banner<page.IndexOf("BeginDataTable(\"WeatherForecast\",5)",StringComparison.Ordinal),"the banner comes before the table, which stays");
            Check(page.Contains("()=>WeatherPeriodAlarm(entry,named));"),"the table and the banner make the same alarm");
            var code=Read("src/TenshoAssistant/MainWindow.WeatherBanner.cs");
            var draw=Between(code,"private void DrawWeatherBanner(","\n    }");
            Check(draw.Contains("var count=Math.Min(3,weatherForecast.Count);"),"now, next, after");
            Check(draw.Contains("Motion.Pulse(weatherBannerChangedAt,Environment.TickCount64,WeatherBannerFadeMs,ReducedMotion)") && code.Contains("private const int WeatherBannerFadeMs=150;"),"a brief fade of the current block, cut by reduced motion");
            Check(draw.Contains("if(weatherBannerPeriod>=0 && weatherBannerTerritory==weatherTerritory)weatherBannerChangedAt=Environment.TickCount64;"),"the highlight starts on a change of period, not of zone");
            Check(code.Contains("weatherBannerChangedAt=-100_000;"),"no highlight at the first draw");
            Check(draw.Contains("SelectionMark.SideMark(") && draw.Contains("localization.Get(\"Weather.BannerComputed\")"),"the current block carries the selection mark; the forecast is said computed");
            Check(draw.Contains("var stacked=available<ImGui.GetFontSize()*30;"),"a narrow window stacks the blocks");
            var block=Between(code,"private void DrawWeatherBannerBlock(","\n    }");
            Check(block.Contains("localization.Format(\"Weather.BannerIn\",Motion.Countdown(entry.StartsAt-now))") && block.Contains("AlarmButton($\"Banner{entry.StartsAt.ToUnixTimeSeconds()}\"")
                && block.Contains("localization.Get(\"Weather.BannerUnavailable\")"),"time left, alarm, and « forecast unavailable »");
            Check(code.Contains("private Func<bool> reducedMotion=()=>true;") && code.Contains("catch(Exception){return true;}"),"without the preference (the desktop application) or on a failure: no motion");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.ConfigureMotion(()=>pluginInterface.UiBuilder.ShouldUseReducedMotion);"),"Dalamud's preference is read");
            foreach(var file in new[]{"en","fr"})
            {
                var weather=Between(Read($"src/TenshoAssistant/Localization/Languages/{file}.json"),"\n  \"Weather\": {","\n  },");
                foreach(var key in new[]{"BannerNow","BannerNext","BannerAfter","BannerIn","BannerEt","BannerEtPc","BannerUnavailable","BannerComputed"})Check(weather.Contains($"    \"{key}\": \""),$"{file}: Weather.{key}");
            }
        }),
    ];
}
