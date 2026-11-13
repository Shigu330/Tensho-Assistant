using TenshoAssistant.Diagnostics;

// 0.1244 (X-165, slice 1, user request of 2026-11-12): Tensho's dashboard in developer mode reads measures already taken (nothing new
// measured) once a second while shown. The rules (HealthBoard): a failed self-check, save, cache or bank, or discoveries that cannot be
// written are a problem; a reader unavailable, a suspect self-check, discoveries waiting or a page over 4 ms usually are worth a look; an
// unknown value is said unknown, never 0 (R08).
internal static class Dashboard1244Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static HealthInputs Fine()=>new(200L*1048576,150L*1048576,"Home",0.4,2.5,[],null,null,false,false,null,
        new Dictionary<string,int>(),36,0,0,true);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dashboard 1244: all fine is Ok; each problem and each thing worth a look raises the overall state",() =>
        {
            var (overall,lines)=HealthBoard.Evaluate(Fine());
            Check(overall==HealthLevel.Ok && lines.All(line=>line.Level==HealthLevel.Ok) && lines.Count==9,"fine: "+overall);
            Check(lines[0].Key=="Dashboard.Memory" && (double)lines[0].Values[0]==200.0,"memory in MB");
            foreach(var (name,inputs) in new (string,HealthInputs)[]
            {
                ("failed self-check",Fine() with { SelfFailed=1 }),("save",Fine() with { SaveError="disk full" }),("cache",Fine() with { CacheError="locked" }),
                ("bank",Fine() with { BankError="unreadable" }),("discoveries blocked",Fine() with { DiscoveriesBlocked=true }),
            })
                Check(HealthBoard.Evaluate(inputs).Overall==HealthLevel.Problem,"a problem: "+name);
            foreach(var (name,inputs) in new (string,HealthInputs)[]
            {
                ("reader",Fine() with { Unavailable=["Hunt"] }),("suspect",Fine() with { SelfSuspect=2 }),("waiting",Fine() with { DiscoveriesUnsaved=true }),
                ("slow page",Fine() with { SlowestMedianMs=HealthBoard.SlowPageMs+0.1 }),
            })
                Check(HealthBoard.Evaluate(inputs).Overall==HealthLevel.Watch,"worth a look: "+name);
            Check(HealthBoard.Evaluate(Fine() with { SlowestMedianMs=HealthBoard.SlowPageMs,SlowestMaxMs=40 }).Overall==HealthLevel.Ok,"a spike alone is not slow: the median counts");
            Check(HealthBoard.Evaluate(Fine() with { SelfSuspect=1,SaveError="x" }).Overall==HealthLevel.Problem,"the worst wins");
        }),
        ("Dashboard 1244: unknown stays unknown, requests sorted, median and max of a ring",() =>
        {
            var unknown=HealthBoard.Evaluate(Fine() with { HeapBytes=null,SlowestPage=null,SlowestMedianMs=null,SlowestMaxMs=null,SelfRun=false }).Lines;
            Check(unknown.Any(line=>line.Key=="Dashboard.MemoryUnknown") && unknown.Any(line=>line.Key=="Dashboard.NoPageTimed") && unknown.Any(line=>line.Key=="Dashboard.SelfCheckNotRun"),"said unknown, never 0");
            var requests=HealthBoard.Evaluate(Fine() with { Requests=new Dictionary<string,int>{["b.org"]=2,["a.org"]=5,["c.org"]=2} }).Lines.Single(line=>line.Key=="Dashboard.Requests");
            Check((int)requests.Values[0]==9 && (string)requests.Values[1]=="a.org 5, b.org 2, c.org 2","total and hosts, most first: "+requests.Values[1]);
            Check(HealthBoard.MedianMax([5,1,3,0,0],3) is (3,5) && HealthBoard.MedianMax([1,2],0) is null,"ring: only the samples written");
            var readers=HealthBoard.Evaluate(Fine() with { Unavailable=["Hunt","Chocobo"] }).Lines.Single(line=>line.Key=="Dashboard.ReadersUnavailable");
            Check((int)readers.Values[0]==2 && (string)readers.Values[1]=="Hunt, Chocobo","readers named");
        }),
        ("Dashboard 1244: in Diagnostics, read once a second while shown, from measures already taken; texts in both languages",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(page.Contains("private long dashboardAt=-100_000;") && page.Contains("if(dashboard is null || now-dashboardAt>=1000){dashboard=HealthBoard.Evaluate(DashboardInputs());dashboardAt=now;}"),"once a second, clock from -100 000");
            var drawStart=page.IndexOf("private void DrawDashboard()",StringComparison.Ordinal);
            Check(page.IndexOf("if(!TenshoAssistant.UI.UiComponents.Header(",drawStart,StringComparison.Ordinal) < page.IndexOf("DashboardInputs()",drawStart,StringComparison.Ordinal),"nothing read while folded");
            Check(page.Contains("Community.BoundedHttp.Sent") && page.Contains("SelfCheck?.LastLines") && page.Contains("pageDrawMs") && page.Contains("FailedReadings()"),"existing measures");
            Check(Read("src/TenshoAssistant/MainWindow.Diagnostics.cs").Contains("        DrawDashboard();\n"),"in Diagnostics");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"Title","OverallOk","OverallWatch","OverallProblem","Memory","MemoryUnknown","SlowestPage","NoPageTimed","ReadersOk","ReadersUnavailable","CacheError","CacheOk","BankError","BankOk","DiscoveriesBlocked","DiscoveriesUnsaved","DiscoveriesOk","SaveError","SavesOk","SelfCheckNotRun","SelfCheck","NoRequests","Requests","Help"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text: "+key);
            Check(en.Contains("  \"Dashboard\": {") && fr.Contains("  \"Dashboard\": {"),"its own section");
        }),
    ];
}
