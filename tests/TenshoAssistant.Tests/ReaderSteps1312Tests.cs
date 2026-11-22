using TenshoAssistant.Competitive;

// 0.1312 (X-01): a reader names its steps (CompetitiveGate.Step); a slow pass's line says its costliest one. Counted only on the thread
// that started the pass; nothing said when no step was named.
internal static class ReaderSteps1312Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reader steps 1312: the costliest named step is said, then nothing is counted until the next pass",() =>
        {
            CompetitiveGate.StepsStart();
            CompetitiveGate.Step("quick");
            Thread.Sleep(200);   // 0.1478: 40 ms lost once to a pause before « quick » (44 ms) under the parallel tests
            CompetitiveGate.Step("slow");
            CompetitiveGate.Step("quick again");
            var text = CompetitiveGate.StepsEnd();
            Check(text.StartsWith(" [costliest step slow ",StringComparison.Ordinal) && text.EndsWith(" ms]",StringComparison.Ordinal),"the slow step: " + text);
            CompetitiveGate.Step("after the end");
            Check(CompetitiveGate.StepsEnd() == "","nothing after the end");
        }),
        ("Reader steps 1312: no step named, or steps named on another thread: nothing said",() =>
        {
            CompetitiveGate.StepsStart();
            Exception? kept = null;   // 0.1796 (X-783): kept for the test, never left to end the test process
            var other = new Thread(() => { try { Thread.Sleep(30); CompetitiveGate.Step("other thread"); } catch (Exception error) { kept = error; } });
            other.Start();
            other.Join();
            Check(kept is null,"the other thread: " + kept);
            Check(CompetitiveGate.StepsEnd() == "","another thread's step is not this pass's");
        }),
        ("Reader steps 1312: wiring: the gate starts and ends the steps around each measured reader; the item watcher names its steps",() =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(gate.Contains("StepsStart();\n        var started=MeasureStart();\n        handler(running);\n        var spent=MeasureOwn(started);\n        var steps=StepsEnd();",StringComparison.Ordinal),"around the call");
            Check(gate.Contains("CompilerState?.Waiting ?? 0)+steps,spent,worst);",StringComparison.Ordinal),"in the line");
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("Step(\"weather\")",StringComparison.Ordinal) && watcher.Contains("Step(\"counts\")",StringComparison.Ordinal),"the item watcher's steps");
        }),
    ];
}
