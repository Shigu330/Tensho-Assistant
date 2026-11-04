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
            Check(SelfCheck.Summary("auto",run.Lines)=="Selftest done (auto): 1 ok, 1 not read, 0 suspect, 1 failed.","summary");
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
            Check(Code("MainWindow.Diagnostics.cs").Contains("DrawSelfCheck();"),"the Diagnostics section");
        }),
    ];
}
