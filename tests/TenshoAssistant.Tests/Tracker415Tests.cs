using TenshoAssistant.Alarms;

// R-13 (0.415): pinned tracking window — enabled item alarms, soonest first, bounded.
internal static class Tracker415Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Tracker: enabled item alarms only, soonest first, weather flagged, bounded",() =>
        {
            var now=new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            TenshoAlarm Once(string label,uint item,int minutes,bool enabled=true,bool weather=false)
                =>new(){Label=label,ItemId=item,Repeat=AlarmRepeat.Once,At=now.AddMinutes(minutes),Enabled=enabled,Weathers=weather ? [2u]:[]};
            var alarms=new List<TenshoAlarm>
            {
                Once("Late",1,90),Once("Soon",2,5,weather:true),Once("Off",3,1,enabled:false),Once("NoItem",0,2),Once("Past",4,-10),
            };
            var rows=TrackedAlarms.Rows(alarms,now);
            Check(rows.Select(row=>row.Label).SequenceEqual(["Soon","Late"]),"enabled item alarms with a next time, soonest first");
            Check(rows[0].Weather && !rows[1].Weather && rows[0].At==now.AddMinutes(5),"weather and time");
            var many=Enumerable.Range(1,30).Select(index=>Once($"A{index}",(uint)index,index)).ToList();
            Check(TrackedAlarms.Rows(many,now).Count==TrackedAlarms.MaxRows && TrackedAlarms.Rows(many,now,3).Count==3 && TrackedAlarms.Rows(null,now).Count==0,"bounded");
        }),
        ("Tracker: off by default, closed by its cross, hidden with the game interface",() =>
        {
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.TrackerWindow.cs"));
            Check(ui.Contains("()=>configuration.TrackerWindow,()=>!windowDisposed && !gameGui.GameUiHidden") && ui.Contains("configuration.TrackerWindow=false;")
                && ui.Contains("trackerRefresh=now.AddSeconds(1)"),"window");
            var configuration=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs"));
            Check(configuration.Contains("public bool TrackerWindow { get; set; }") && configuration.Contains("\"TrackerWindow\","),"setting off by default and listed");
        }),
    ];
}
