using TenshoAssistant.Discoveries;

// 0.1611 (X-591, external audit of 18/11, texts and tests). A13: SECURITY and the setting's tip say the automatic exchange's real pace
// (GitHubExchange.Every, 20 hours), in English and French. A23: TESTING no longer says « 87 files » nor « all in Program.cs ». A25: a test
// skipped on this system is counted apart (SKIP), never as passed: behaviour of the runner on a group made here.
internal static class AuditTexts1611Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Audit texts 1611: A13, the exchange's pace said as it is, in English and French",()=>
        {
            Check(GitHubExchange.Every==TimeSpan.FromHours(20),"the pace is 20 hours");
            var security=Read("SECURITY.md");
            Check(security.Contains("at most once every 20 hours") && security.Contains("au plus une fois toutes les 20 heures") && !security.Contains("once a day at most") && !security.Contains("une fois par jour au plus"),"SECURITY");
            Check(Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("\"AutoExchangeTip\": \"At most once every 20 hours:")
                && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\"AutoExchangeTip\": \"Au plus une fois toutes les 20 heures :"),"the setting's tip");
        }),
        ("Audit texts 1611: A23, TESTING describes the tests as they are found",()=>
        {
            var testing=Read("docs/project/TESTING.md");
            Check(!testing.Contains("87 fichiers") && !testing.Contains("Tous sont enregistrés dans `Program.cs`") && testing.Contains("trouvée seule par réflexion"),"TESTING");
        }),
        ("Audit texts 1611: A25, a skipped test is counted apart, never passed nor failed",()=>
        {
            (string,Action)[] tests=[("ran",()=>{}),("skipped",()=>throw Skip.Because("not on this system")),("ran too",()=>{})];
            var run=ParallelTests.Run([new TestGroup("Sample",tests,false)],1,[],DateOnly.FromDateTime(DateTime.Now));
            Check(run.Passed==2 && run.Total==2 && run.Skipped==1 && run.Failed==0,$"passed {run.Passed}, total {run.Total}, skipped {run.Skipped}, failed {run.Failed}");
            Check(run.Results.Single(result=>result.Skipped) is { Passed:false, Error:null, Note:"SKIP skipped: not on this system" },"its note");
            Check(ParallelTests.CountLine(run)=="2/2 passed, 1 skipped (not run on this system, SKIP lines above)",ParallelTests.CountLine(run));
            var plain=ParallelTests.Run([new TestGroup("Sample",[("ran",()=>{})],false)],1,[],DateOnly.FromDateTime(DateTime.Now));
            Check(ParallelTests.CountLine(plain)=="1/1 passed","no skip: the line the scripts read, unchanged");
        }),
        ("Audit texts 1611: A25 wiring, no test returns early off Windows any more",()=>
        {
            foreach(var file in Directory.EnumerateFiles(Path.GetFullPath("tests/TenshoAssistant.Tests"),"*.cs"))
                Check(!File.ReadAllText(file).Contains("if(!OperatingSystem.IsWindows())"+"return;"),"returns as passed: "+Path.GetFileName(file));
        }),
    ];
}
