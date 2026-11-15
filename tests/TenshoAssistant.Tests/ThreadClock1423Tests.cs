// 0.1423, X-433 step 2 (measure): the slow line gives the game thread's own processor time (working or waiting), and for two minutes after
// a load the runtime's long type loads are noted with their thread and the type's name.
using System.Diagnostics;
using TenshoAssistant.Diagnostics;

internal static class ThreadClock1423Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTime At = new(2026, 11, 15, 14, 23, 12, DateTimeKind.Utc);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Thread clock 1423: the calling thread's processor time grows while it works, not while it sleeps", () =>
        {
            if (!OperatingSystem.IsWindows()) { Check(ThreadClock.ProcessorMs() is null, "unknown off Windows"); return; }
            Check(ThreadClock.SystemId() > 0, "a system id");
            var before = ThreadClock.ProcessorMs();
            Thread.Sleep(150);
            var slept = ThreadClock.ProcessorMs();
            // 0.1459: work until the thread's own clock has moved 40 ms, at most 5 s of wall time: a fixed 300 ms of wall time gave only 31 ms
            // of processor time on a loaded machine (the game running, 32 test threads), and the test failed for that alone.
            var watch = Stopwatch.StartNew();
            var spin = 0L;
            var worked = slept;
            while (watch.ElapsedMilliseconds < 5000 && (worked is null || slept is null || worked.Value - slept.Value < 40))
            {
                spin++;
                if ((spin & 0xFFFF) == 0) worked = ThreadClock.ProcessorMs();
            }
            worked = ThreadClock.ProcessorMs();
            Check(before is not null && slept is not null && worked is not null, "known on Windows");
            Check(slept!.Value - before!.Value < 100, "a sleep of 150 ms costs almost nothing: " + (slept - before));
            Check(worked!.Value - slept.Value >= 40, "work counts (clock ticks of about 16 ms): " + (worked - slept) + " ms in " + watch.ElapsedMilliseconds + " ms, " + spin);
            Check(ThreadClock.ProcessorText(10, 42) == " (on the processor 32 ms)" && ThreadClock.ProcessorText(null, 42) == "" && ThreadClock.ProcessorText(50, 42) == "", "the slow line's part");
        }),
        ("Thread clock 1423: a long type load gives a note naming its thread and type; the game thread's time summed", () =>
        {
            var types = new TypeLoadTracker(30, 1);
            types.Start(7, At, 1234);
            types.Start(8, At, 99);
            var note = types.Stop(7, At.AddMilliseconds(120), "Lumina.Excel.ExcelSheet`1[Weather]", 1234);
            Check(note is not null && note.Contains("120 ms on the game's thread", StringComparison.Ordinal) && note.EndsWith("Lumina.Excel.ExcelSheet`1[Weather].", StringComparison.Ordinal), "the game's thread: " + note);
            Check(types.Stop(8, At.AddMilliseconds(80), "Other", 1234) is null, "at most MaximumNotes notes");
            Check(types.Stop(9, At, "Never started", 1234) is null && types.Count == 2, "a stop without its start is not counted");
            Check(Math.Abs(types.GameThreadMs - 120) < 0.01 && Math.Abs(types.LongestMs - 120) < 0.01 && types.Longest == "Lumina.Excel.ExcelSheet`1[Weather]", "sums");
            Check(types.Summary() == "Type loads in the same time: 2, 120 ms on the game's thread (nested ones counted in their outer one too), the longest 120 ms: Lumina.Excel.ExcelSheet`1[Weather].", types.Summary());
            var other = new TypeLoadTracker(30, 5);
            other.Start(1, At, 99);
            Check(other.Stop(1, At.AddMilliseconds(40), "T", 0)!.Contains("on thread 99", StringComparison.Ordinal) && other.GameThreadMs == 0, "the game thread unknown: another thread");
        }),
        ("Thread clock 1423: wired: the slow line gives the processor time, the listener follows the type loads, the summary says them", () =>
        {
            var gate = Read("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs");
            Check(gate.IndexOf("var processorBefore=Diagnostics.ThreadClock.ProcessorMs();", StringComparison.Ordinal) < gate.IndexOf("StepsStart();\n        var started=MeasureStart();\n        handler(running);", StringComparison.Ordinal)
                && gate.Contains("Diagnostics.ThreadClock.ProcessorText(processorBefore,processorAfter)", StringComparison.Ordinal), "the slow line");
            var listener = Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs");
            Check(listener.Contains("(EventKeywords)(GcKeyword|TieredCompilationKeyword|TypeDiagnosticKeyword)", StringComparison.Ordinal) && RuntimeSuspensionListener.TypeDiagnosticKeyword == 0x8000000000, "the type loads followed");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("log.Information(suspensions.Types.Summary());", StringComparison.Ordinal), "summed at the end of the watch");
        }),
    ];
}
