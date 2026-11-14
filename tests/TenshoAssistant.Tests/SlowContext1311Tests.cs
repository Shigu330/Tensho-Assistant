using TenshoAssistant.Diagnostics;

// 0.1311 (X-01): a reader took 187 to 430 ms of one frame 15 to 25 s after each load made by Verify, a different one each time, nothing
// compiled on the game's thread. The slow pass's line now says what the other threads did meanwhile (compiling, lock waits, collections,
// readers' classes waiting): at 4:12 on 14/11 nothing was compiled anywhere while the readers' compiler had work, the whole game waiting
// for the processor taken by Verify's tests and desktop pages. Verify.ps1 now runs below normal priority (the game keeps the processor).
internal static class SlowContext1311Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Slow context 1311: nothing said when nothing happened; each part named, in a fixed order, invariant digits",() =>
        {
            Check(JitWarmup.ContextText(0,0,0,0,0,0,0) == "","nothing");
            Check(JitWarmup.ContextText(12.4,0,0,0,0,0,0) == "","time without methods: nothing compiled");
            Check(JitWarmup.ContextText(177.4,52,0,0,0,0,89) == " [other threads compiled 52 methods in 177 ms; 89 reader classes waiting]","compiling and waiting");
            Check(JitWarmup.ContextText(0,0,3,1,1,0,0) == " [3 lock waits; collections 1/1/0]","lock waits and collections");
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                Check(JitWarmup.ContextText(1234.6,1500,0,0,0,0,0) == " [other threads compiled 1500 methods in 1235 ms]","no thousands separator, invariant");
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        }),
        ("Slow context 1311: wiring: a slow pass's line carries the other threads' part, measured around the reader's call",() =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).ReplaceLineEndings("\n");
            var before = gate.IndexOf("var allMethodsBefore=System.Runtime.JitInfo.GetCompiledMethodCount(false);",StringComparison.Ordinal);
            var call = gate.IndexOf("handler(running);\n        var spent=MeasureOwn(started);",StringComparison.Ordinal);
            Check(before > 0 && call > before,"counted before the call");
            Check(gate.Contains("Monitor.LockContentionCount-contentionsBefore",StringComparison.Ordinal) && gate.Contains("GC.CollectionCount(2)-gen2Before",StringComparison.Ordinal)
                && gate.Contains("+Diagnostics.JitWarmup.ContextText(",StringComparison.Ordinal),"lock waits, collections and the text");
        }),
        ("Slow context 1311: Verify.ps1 runs below normal priority before it builds (the build, the tests alongside and the desktop pages inherit it)",() =>
        {
            var verify = File.ReadAllText(Path.GetFullPath("scripts/Verify.ps1")).ReplaceLineEndings("\n");
            var priority = verify.IndexOf("(Get-Process -Id $PID).PriorityClass = [System.Diagnostics.ProcessPriorityClass]::BelowNormal",StringComparison.Ordinal);
            var tests = verify.IndexOf("$taskTests = Start-Process",StringComparison.Ordinal);
            var headless = verify.IndexOf("--headless",StringComparison.Ordinal);
            var build = verify.IndexOf("'build'",StringComparison.Ordinal);
            Check(priority > 0 && priority < tests && priority < headless && (build < 0 || priority < build),"set first: " + priority + " / " + tests + " / " + headless + " / " + build);
        }),
    ];
}
