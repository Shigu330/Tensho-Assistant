using TenshoAssistant.Fields;

// 0.1241 (X-205, user request of 2026-11-12, K-35): Eureka's notorious monsters that need a weather or the night. The table (community,
// DEC-044, docs/evidence/0.1241-eureka-notorious.md), the next windows from 8-hour weather periods (unknown weather never a window, the
// night inside a period, two nights' halves joined), the alarm minutes, the check against the game's files, and the page's wiring.
internal static class EurekaNotorious1241Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    // Eight Eorzea hours last 1,400 real seconds (175 s an hour).
    private static readonly DateTimeOffset Origin=new(2026,11,13,0,0,0,TimeSpan.Zero);
    private static (DateTimeOffset,DateTimeOffset,int,uint?) Period(int index,uint? weather)
        =>(Origin.AddSeconds(1400*index),Origin.AddSeconds(1400*(index+1)),index%3*8,weather);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Eureka notorious 1241: the table (21 rows, one per FATE, the four zones, a condition each, weathers of the probe)",() =>
        {
            var all=EurekaNotoriousBoard.All;
            Check(all.Count==21 && all.Select(monster=>monster.Fate).Distinct().Count()==21,"21 FATEs, once each");
            Check(all.All(monster=>monster.Weather is not null || monster.Night),"each has a condition");
            Check(all.Select(monster=>monster.Territory).Distinct().Order().SequenceEqual(new uint[]{732,763,795,827}),"the four zones");
            Check(all.Where(monster=>monster.Weather is not null).Select(monster=>monster.Weather!.Value).Distinct().Order().SequenceEqual(new uint[]{4,6,9,14,16,49}),"weathers of PROBE_EUREKANM");
            var pazuzu=all.Single(monster=>monster.Fate==1329);
            Check(pazuzu is { Weather:6, WeatherOn:EurekaConditionOn.Monster, Night:true },"Wail in the Willows: Gales and night");
            Check(all.Single(monster=>monster.Fate==1365) is { Weather:16, Night:false } && all.Single(monster=>monster.Fate==1355).WeatherOn==EurekaConditionOn.Spawner,"Cassie, Melting Point");
            var evidence=Read("docs/evidence/0.1241-eureka-notorious.md");
            foreach(var monster in all)Check(evidence.Contains($"| {monster.Fate} |"),"cited: "+monster.Fate);
        }),
        ("Eureka notorious 1241: windows from the forecast: weather periods, unknown weather never, night parts, nights joined",() =>
        {
            var cassie=EurekaNotoriousBoard.All.Single(monster=>monster.Fate==1365);
            var periods=new[]{Period(0,16),Period(1,16),Period(2,2),Period(3,null),Period(4,16)};
            var windows=EurekaNotoriousBoard.Windows(cassie,periods);
            Check(windows.Count==2 && windows[0]==new EurekaWindow(Origin,Origin.AddSeconds(2800)) && windows[1].Start==Origin.AddSeconds(5600),"two Blizzards periods joined, unknown weather left out: "+string.Join(",",windows));
            var night=EurekaNotoriousBoard.All.Single(monster=>monster.Fate==1418);
            var nights=EurekaNotoriousBoard.Windows(night,new[]{Period(0,null),Period(1,null),Period(2,null),Period(3,null)});
            // 00:00–06:00, then 19:00–24:00 joined to the next 00:00–06:00.
            Check(nights.Count==2 && nights[0]==new EurekaWindow(Origin,Origin.AddSeconds(6*175)),"first night's end: "+string.Join(",",nights));
            Check(nights[1]==new EurekaWindow(Origin.AddSeconds(19*175),Origin.AddSeconds(24*175+6*175)),"19:00 to the next 06:00, one window: "+nights[1]);
            var pazuzu=EurekaNotoriousBoard.All.Single(monster=>monster.Fate==1329);
            var both=EurekaNotoriousBoard.Windows(pazuzu,new[]{Period(0,2),Period(1,6),Period(2,6),Period(3,2)});
            Check(both.Count==1 && both[0]==new EurekaWindow(Origin.AddSeconds(19*175),Origin.AddSeconds(24*175)),"Gales and night: 19:00–24:00 only (the next period is not Gales): "+string.Join(",",both));
            Check(EurekaNotoriousBoard.Windows(cassie,[]).Count==0,"no forecast: none");
        }),
        ("Eureka notorious 1241: alarm minutes, and the check against the game's files",() =>
        {
            var all=EurekaNotoriousBoard.All;
            Check(EurekaNotoriousBoard.AlarmMinutes(all.Single(monster=>monster.Fate==1365)).SequenceEqual([0,480,960]),"weather alone: each period's start");
            Check(EurekaNotoriousBoard.AlarmMinutes(all.Single(monster=>monster.Fate==1418)).SequenceEqual([19*60]),"night alone: its start");
            Check(EurekaNotoriousBoard.AlarmMinutes(all.Single(monster=>monster.Fate==1329)).SequenceEqual([0,19*60]),"both: the two periods a night spans");
            Check(EurekaNotoriousBoard.Mismatches((_,_)=>true,(_,_)=>true).Count==0,"all found: nothing said");
            var wrong=EurekaNotoriousBoard.Mismatches((_,fate)=>fate!=1403,(territory,weather)=>!(territory==763 && weather==4));
            Check(wrong.Count==3 && wrong.Contains("fate 1403: not a FATE of territory 795") && wrong.Count(line=>line.Contains("weather 4 not in territory 763"))==2,"said: "+string.Join("; ",wrong));
        }),
        ("Eureka notorious 1241: the page's tab, hidden by « game data only », forecast cached, alarms, texts, self-check",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.EurekaNotorious.cs");
            Check(page.Contains("if(configuration.GameDataOnly){ImGui.TextColored(Muted,localization.Get(\"FieldContent.NotoriousOff\"));return;}"),"community conditions hidden by « game data only » (DEC-044)");
            Check(page.Contains("if(now-eurekaForecastAt>TimeSpan.FromMinutes(1)){eurekaForecast.Clear();eurekaForecastAt=now;}"),"the forecast at most once a minute, not in every frame");
            Check(page.Contains("AddAlarms(AlarmSchedule.ForWindows(name,EurekaNotoriousBoard.AlarmMinutes(monster),DefaultAlarmLead,monster.Territory,0,") && page.Contains("Tip(\"FieldContent.NotoriousAlarmTip\");"),"alarms like the vistas'");
            Check(Read("src/TenshoAssistant/MainWindow.FieldContent.cs").Contains("tabs.Add((\"Notorious\",localization.Get(\"FieldContent.TabNotorious\"),()=>DrawFieldNotorious(catalog)));"),"a tab of Eureka and Bozja");
            Check(Read("src/TenshoAssistant/MainWindow.SelfCheckPages.cs").Contains("Page(\"Eureka notorious monsters\""),"checked in the game");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"TabNotorious","NotoriousHint","NotoriousOff","NotoriousLoading","ColumnCondition","ColumnWindow","NotoriousSpawnerWeather","NotoriousNight","NotoriousNow","NotoriousNext","NotoriousNoWindow","NotoriousAlarm","NotoriousAlarmTip"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text in both languages: "+key);
            Check(en.Contains("come from the community (Eureka Helper)") && fr.Contains("viennent de la communauté (Eureka Helper)"),"the source said as such");
        }),
    ];
}
