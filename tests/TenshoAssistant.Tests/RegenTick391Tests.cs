using TenshoAssistant.Gathering;

// 0.391 (g, X-06): GP tick — a rise while not full is a tick, the bar runs on a 3-second cycle, forgotten after a minute; gatherers only,
// never in combat (DEC-009), off by default.
internal static class RegenTick391Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GP tick: a rise while not full sets the tick, 3-second cycle, full or old ticks not counted; gatherers only, not in combat",() =>
        {
            var start = new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var tracker = new RegenTickTracker();
            Check(tracker.Observe(500,700,start).Progress is null,"no tick seen yet");
            var after = tracker.Observe(507,700,start.AddSeconds(1));
            Check(after.Progress==0 && after.SecondsLeft==3,"tick at the rise");
            var later = tracker.Observe(507,700,start.AddSeconds(5.5));
            Check(Math.Abs(later.SecondsLeft!.Value-1.5)<1e-9,"cycle of 3 seconds (4.5 s after the tick: 1.5 s left)");
            Check(tracker.Observe(507,700,start.AddMinutes(2)).Progress is null,"forgotten after a minute");
            var full = new RegenTickTracker();
            full.Observe(700,700,start);full.Observe(710,710,start.AddSeconds(1));
            Check(full.Observe(710,710,start.AddSeconds(2)).Progress is null,"a rise from full is not a tick");
            Check(Read("Gathering/RegenTickReader.Dalamud.cs").Contains("local.ClassJob.RowId is not (16 or 17 or 18) || condition[ConditionFlag.InCombat]",StringComparison.Ordinal),"gatherers, not in combat");
            Check(!new TenshoAssistant.Configuration().GpTickBar && TenshoAssistant.ConfigurationReset.Reset.Contains("GpTickBar"),"off by default");
        }),
    ];
}
