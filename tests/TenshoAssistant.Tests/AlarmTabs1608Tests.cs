// 0.1608 (X-602, user request of 18/11, D-186): the Alarms page in tabs, in the Storage style (DetailTabs), one tab per block that
// followed one another (clocks, alarms by job, other alarms, personal alarm, game alarms, settings); the alarms' tabs carry their count;
// nothing left out: every alarm the page lists falls in one of the two lists, every block of before is drawn by a tab.
internal static class AlarmTabs1608Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Source()=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Alarms.cs")).Replace("\r\n","\n");
    private static string Page()
    {
        var source=Source();
        var start=source.IndexOf("private void DrawAlarmsPage()",StringComparison.Ordinal);
        var end=source.IndexOf("private bool DrawAlarmList(",start,StringComparison.Ordinal);
        Check(start>0 && end>start,"DrawAlarmsPage found");
        return source[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm tabs 1608: the page uses the tab template, one tab per block, in the order the user listed, the settings last",()=>
        {
            var page=Page();
            Check(page.Contains("DetailTabs(\"AlarmsTabs\","),"the common tab template");
            var keys=new[]{"(\"Clocks\",","(\"Jobs\",","(\"Other\",","(\"Personal\",","(\"Game\",","(\"Settings\","};
            var at=keys.Select(key=>page.IndexOf(key,StringComparison.Ordinal)).ToArray();
            Check(at.All(index=>index>0) && at.Zip(at.Skip(1)).All(pair=>pair.First<pair.Second),"six tabs, in order: "+string.Join(",",at));
            Check(!page.Contains("UiComponents.Header(") && !Source().Contains("ImGui.TreeNode(localization.Get(\"Alarms.GameTitle\")"),"no folding block left on the page");
        }),
        ("Alarm tabs 1608: no alarm and no block lost: both lists split the page's alarms, each former block drawn by a tab, saved once",()=>
        {
            var page=Page();
            Check(page.Contains("foreach(var alarm in shown)(AlarmJob(alarm)!=0 ? byJob:other).Add(alarm);"),"every listed alarm in exactly one list");
            Check(page.Contains("DrawAlarmList(byJob,now,remove,added,true)") && page.Contains("DrawAlarmList(other,now,remove,added,false)"),"both lists drawn");
            foreach(var block in new[]{"DrawGatheringClock();DrawWorldClocks();","DrawManualAlarm)","DrawGameAlarms)","DrawAlarmSettings)"})
                Check(page.Contains(block),"block drawn by a tab: "+block);
            Check(Source().Contains("private void DrawManualAlarm()") && Source().Contains("DrawManualDateAlarm();"),"the personal alarm keeps its dated form");
            Check(page.Contains("foreach(var alarm in remove){alarms.Remove(alarm);changed=true;}") && page.Contains("if(added.Count>0)AddAlarms(added);") && page.Contains("try{save();}"),"removals, additions and the save after the tabs");
        }),
        ("Alarm tabs 1608: counts in the alarms' tabs, a stable tab id, the labels in both languages",()=>
        {
            var page=Page();
            Check(page.Contains("$\"{localization.Get(\"Alarms.TabJobs\")} ({byJob.Count})\"") && page.Contains("$\"{localization.Get(\"Alarms.TabOther\")} ({other.Count})\""),"counts");
            Check(page.Contains("gameCount is { } count ? $\" ({count})\":\"\""),"the game's count when read");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"TabClocks","TabJobs","TabOther","TabGame","TabSettings"})Check(text.Contains($"\"{key}\":"),language+" "+key);
            }
        }),
    ];
}
