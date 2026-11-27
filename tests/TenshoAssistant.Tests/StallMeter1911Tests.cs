// 0.1911 (X-889 M4, review of 0.1909): the test run measures what stops it (StallMeter, TestRunner.cs): the longest late wake of a thread of
// its own (a stop of the whole process or of the PC, with the garbage collector's pauses inside it) and the longest wait of a work on the
// pool (the pool starved by tests that block its threads). One line, « STALLS … », after the slowest tests, kept with each run of a sweep under load.
internal static class StallMeter1911Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Stall meter 1911: the longest late wake and the longest pool wait are kept with their moment, the pool and the collector's pauses then", () =>
        {
            var record = new StallRecord();
            record.Wake(S(0.1), S(0), S(10), 0, 40);
            record.Wake(S(24.3), S(21.7), S(61.2), 12, 64);
            record.Wake(S(0.5), S(0.4), S(70), 3, 64);
            record.Pool(S(0.2), S(5), 0);
            record.Pool(S(3.4), S(62), 9);
            record.Pool(S(1), S(80), 1);
            var line = record.Line();
            Check(line == "STALLS the watch thread woke at most 24.3 s late (at 61.2 s of the run; garbage collection pauses in it 21.7 s; the pool then: 12 work(s) waiting, 64 thread(s)); "
                + "a work on the pool waited at most 3.4 s (at 62.0 s; 9 waiting before it); 3 wakes, 3 probes.", line);
            record.Failed("ThreadInterruptedException");
            Check(record.Line().EndsWith("3 wakes, 3 probes; the meter stopped on ThreadInterruptedException.", StringComparison.Ordinal), record.Line());
        }),
        ("Stall meter 1911: behaviour: a real meter wakes and puts works on the pool while it runs, then stops", () =>
        {
            var meter = StallMeter.Start();
            var waited = System.Diagnostics.Stopwatch.StartNew();
            while ((meter.Record.Wakes < 3 || meter.Record.Probes < 1) && waited.Elapsed < S(60)) Thread.Sleep(50);
            // X-892 M3 (review of 0.1912): the thread's end is checked (Dispose joins it 2 s only; past that under load, it woke once more
            // during the sleep below and « stopped » failed).
            Check(meter.Stop(S(60)), "the meter's thread ended");
            Check(meter.Record.Wakes >= 3 && meter.Record.Probes >= 1, "woke and probed: " + meter.Record.Line());
            var wakes = meter.Record.Wakes;
            Thread.Sleep(600);
            Check(meter.Record.Wakes == wakes, "stopped");
            Check(meter.Record.Line().StartsWith("STALLS the watch thread woke at most ", StringComparison.Ordinal), meter.Record.Line());
        }),
        ("Stall meter 1911: wiring: the run starts the meter before the tests, stops it after, says its line after the slowest tests and before the count", () =>
        {
            var program = File.ReadAllText("tests/TenshoAssistant.Tests/Program.cs").ReplaceLineEndings("\n");
            var start = program.IndexOf("var stalls = StallMeter.Start();\nvar run = ParallelTests.Run(", StringComparison.Ordinal);
            var stop = program.IndexOf("GroupFilter.Full(args));\nstalls.Dispose();\n", StringComparison.Ordinal);
            var slowest = program.IndexOf("Console.WriteLine(SlowestTests.Line(run.Times, run.Whole));\nConsole.WriteLine(stalls.Record.Line());\nConsole.WriteLine(ParallelTests.CountLine(run));", StringComparison.Ordinal);
            Check(start > 0 && stop > start && slowest > stop, $"wiring: {start} {stop} {slowest}");
        }),
    ];
}
