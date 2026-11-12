using TenshoAssistant.Diagnostics;

// 0.1190 (X-01, the steady state; analysis of the slow frames of 12/11, « what is missing »): one line a minute in developer mode says what
// each measured part of a frame costs on ordinary frames (count, average, a bound on the 95th percentile, worst), the costliest first.
internal static class SteadyFrames1190Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Steady frames 1190: no line before a minute has passed; then the frame first and the parts by total time, with average, 95th percentile bound and worst", () =>
        {
            var steady = new SteadyFrames();
            Check(steady.Take(1_000) is null, "the window starts at the first call");
            for (var frame = 0; frame < 100; frame++)
            {
                steady.Add("Tracker", frame < 96 ? 0.4 : 9);
                steady.Add("Map", 2.5);
                steady.Add("Garden", 0.05);
                steady.Add(SteadyFrames.Frame, frame < 96 ? 3.2 : 12.5);
            }
            Check(steady.Take(1_000 + SteadyFrames.WindowMs - 1) is null, "not yet");
            var line = steady.Take(1_000 + SteadyFrames.WindowMs);
            Check(line == "Steady frames over 60 s: frame 100× avg 3.57 ms, p95 ≤ 5 ms, max 12.5 ms; Map 100× avg 2.50 ms, p95 ≤ 3 ms, max 2.5 ms; Tracker 100× avg 0.74 ms, p95 ≤ 0.5 ms, max 9.0 ms; Garden 100× avg 0.05 ms, p95 ≤ 0.1 ms, max 0.1 ms", line ?? "null");
        }),
        ("Steady frames 1190: each line covers its own minute: the totals start again, a minute without a frame gives no line, the parts are bounded", () =>
        {
            var steady = new SteadyFrames();
            steady.Take(0);
            steady.Add(SteadyFrames.Frame, 1); steady.Add("A", 1);
            Check(steady.Take(SteadyFrames.WindowMs) is not null, "first minute");
            Check(steady.Take(2 * SteadyFrames.WindowMs) is null, "an empty minute: no line");
            for (var part = 0; part < 30; part++) steady.Add("P" + part.ToString("00"), part + 1);
            steady.Add(SteadyFrames.Frame, 200);
            var line = steady.Take(3 * SteadyFrames.WindowMs, 3)!;
            Check(line == "Steady frames over 60 s: frame 1× avg 200.00 ms, p95 > 100 ms, max 200.0 ms; P29 1× avg 30.00 ms, p95 ≤ 33 ms, max 30.0 ms; P28 1× avg 29.00 ms, p95 ≤ 33 ms, max 29.0 ms; P27 1× avg 28.00 ms, p95 ≤ 33 ms, max 28.0 ms", line);
            Check(!line.Contains(" A ", StringComparison.Ordinal), "the minute before is forgotten");
            steady.Add("B", double.NaN); steady.Add("B", -1); steady.Add(SteadyFrames.Frame, 1);
            Check(steady.Take(5 * SteadyFrames.WindowMs) == "Steady frames over 120 s: frame 1× avg 1.00 ms, p95 ≤ 1 ms, max 1.0 ms; ", "a time that is not one is left out: " + steady.Take(9 * SteadyFrames.WindowMs));
        }),
        ("Steady frames 1190: wiring: every part of a frame goes to the totals, the whole frame too, and the line is written in developer mode only", () =>
        {
            var slow = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/SlowFrame.cs"));
            Check(slow.Contains("Steady.Add(name,milliseconds);", StringComparison.Ordinal), "the parts");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            var developer = plugin.IndexOf("if(!mainWindow.DeveloperMode){DrawFrame();return;}", StringComparison.Ordinal);
            var add = plugin.IndexOf("mainWindow.SlowFrames.Steady.Add(Diagnostics.SteadyFrames.Frame,total);", StringComparison.Ordinal);
            var take = plugin.IndexOf("if(mainWindow.SlowFrames.Steady.Take(Environment.TickCount64) is { } steady)log.Information(steady);", StringComparison.Ordinal);
            Check(developer > 0 && add > developer && take > add, "after the developer mode check, the frame then the line");
        }),
    ];
}
