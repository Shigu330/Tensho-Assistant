using TenshoAssistant.Diagnostics;

// 0.494 (user request): the self-check says what is read without calling a gap a fault, and a probe that throws does not stop the pass.
internal static class SelfCheckTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Code(string file) => File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Self-check: statuses of a catalogue, a built list, and the line format",() =>
        {
            Check(SelfCheck.Catalog("items",100,0,0,null).Status==SelfCheckStatus.Ok,"rows");
            Check(SelfCheck.Catalog("items",0,0,0,null).Status==SelfCheckStatus.Suspect,"empty is suspect");
            Check(SelfCheck.Catalog("items",null,5,9,null) is {Status:SelfCheckStatus.NotRead,Detail:"loading 5/9"},"still loading is not a fault");
            Check(SelfCheck.Catalog("items",100,0,0,"broken").Status==SelfCheckStatus.Failed,"an error wins");
            Check(SelfCheck.Built("gear",false,true).Status==SelfCheckStatus.Failed && SelfCheck.Built("gear",true,false).Status==SelfCheckStatus.Ok && SelfCheck.Built("gear",false,false).Status==SelfCheckStatus.NotRead,"built");
            var format=SelfCheck.Ok("quests","12 done").Format();
            Check(format.StartsWith("Selftest quests",StringComparison.Ordinal) && format.Contains(" OK ",StringComparison.Ordinal) && format.EndsWith("12 done",StringComparison.Ordinal),"format");
        }),
        ("Self-check: a probe that throws is its own line, the pass goes on and is counted",() =>
        {
            var run=new SelfCheckRun([("a",()=>SelfCheck.Ok("a","fine")),("b",()=>throw new InvalidOperationException("lost")),("c",()=>SelfCheck.NotRead("c","closed"))]);
            Check(!run.Done && run.Step()!.Status==SelfCheckStatus.Ok,"first");
            var failed=run.Step()!;
            Check(failed.Status==SelfCheckStatus.Failed && failed.Domain=="b" && failed.Detail=="InvalidOperationException: lost","thrown");
            Check(run.Step()!.Status==SelfCheckStatus.NotRead && run.Done && run.Step() is null,"last");
            Check(SelfCheck.Summary("auto",run.Lines)=="Selftest done (auto): 1 ok, 0 seen earlier, 1 not read, 0 suspect, 1 failed.","summary");
        }),
        ("Self-check: a window is OK only when really read (first pass in game counted Waiting as OK)",() =>
        {
            Check(SelfCheck.Window("synthesis",true,"Observed","closed").Status==SelfCheckStatus.Ok,"read");
            foreach(var status in new[]{"Waiting","Offline","NotReady","Disabled"})Check(SelfCheck.Window("synthesis",true,status,"closed") is {Status:SelfCheckStatus.NotRead} line && line.Detail==status,status);
            Check(SelfCheck.Window("synthesis",true,"AddonAbsent","no synthesis running").Detail=="no synthesis running","closed");
            Check(SelfCheck.Window("synthesis",true,"Error","closed").Status==SelfCheckStatus.Failed,"error");
            Check(SelfCheck.Window("synthesis",false,"Observed","closed").Status==SelfCheckStatus.NotRead,"off");
        }),
        ("Self-check memory: an OK stays after the window closes, a live answer wins, another game version forgets",() =>
        {
            var at=new DateTimeOffset(2026,11,4,11,0,0,TimeSpan.Zero);
            var memory=SelfCheckMemory.Empty("v1");
            Check(memory.Record(SelfCheck.Ok("synthesis","Observed"),at),"first OK reaches the disk");
            Check(!memory.Record(SelfCheck.Ok("synthesis","Observed"),at.AddSeconds(1)),"same OK a second later does not");
            Check(memory.Record(SelfCheck.Ok("synthesis","Observed"),at.AddMinutes(6)),"its time, every few minutes");
            Check(!memory.Record(SelfCheck.NotRead("gathering node","no node open"),at) && memory.Count==1,"only OK lines");
            var closed=memory.Apply(SelfCheck.NotRead("synthesis","no synthesis running"));
            Check(closed.Status==SelfCheckStatus.Seen && closed.Detail.StartsWith("Observed at ",StringComparison.Ordinal) && closed.Detail.Contains("no synthesis running",StringComparison.Ordinal),"seen earlier, and says what is true now");
            Check(memory.Apply(SelfCheck.Failed("synthesis","lost")).Status==SelfCheckStatus.Failed,"a failure is never hidden");
            Check(memory.Apply(SelfCheck.Suspect("synthesis","odd")).Status==SelfCheckStatus.Suspect,"nor a doubt");
            Check(memory.Apply(SelfCheck.Ok("synthesis","Live")).Detail=="Live","a live OK stays itself");
            Check(memory.Apply(SelfCheck.NotRead("triad match","no match running")).Status==SelfCheckStatus.NotRead,"never seen stays not read");
            Check(SelfCheckLine.Label(SelfCheckStatus.Seen)=="OK_SEEN","label");
            var saved=memory.Save();
            Check(SelfCheckMemory.Load(saved,"v1").Count==1,"survives a reload");
            Check(SelfCheckMemory.Load(saved,"v2").Count==0,"another version of the game's data forgets");
            Check(SelfCheckMemory.Load("{broken",  "v1").Count==0 && SelfCheckMemory.Load(null,"v1").Count==0,"a damaged or missing file is empty");
        }),
        ("Self-check: an error is one line, bounded, without paths",() =>
        {
            var clean=SelfCheck.Clean("Could not open C:\\Users\\Someone\\file.json\nsecond line");
            Check(clean=="Could not open <path> second line","path and newline");
            Check(SelfCheck.Clean(new string('x',500)).Length==161,"bounded");
            Check(SelfCheck.Clean(null)=="unknown error","no message");
        }),
        ("Self-check: read-only, framework thread, developer mode on its own, numbers only",() =>
        {
            var runner=Code("Diagnostics/SelfCheckRunner.Dalamud.cs");
            Check(runner.Contains("framework.IsInFrameworkUpdateThread") && runner.Contains("framework.Update -= OnUpdate") && runner.Contains("developerMode()"),"thread, cleanup, auto only in developer mode");
            var probes=Code("MainWindow.SelfCheck.cs");
            foreach(var word in new[]{"HttpClient","Equip(","stats.Name","CharacterId,","ContentId"})Check(!probes.Contains(word),"not in the probes: "+word);
            Check(Code("Plugin.cs").Contains("is \"selftest\""),"the command");
            Check(probes.Contains("(\"crafting log\", SelfCheckCraftingLog)") && probes.Contains("\"no synthesis running\"") && probes.Contains("\"no node open\"") && !probes.Contains("window closed"),"in-game report: lines named after what they watch");
            Check(Code("MainWindow.Diagnostics.cs").Contains("DrawSelfCheck();"),"the Diagnostics section");
        }),
    ];
}
