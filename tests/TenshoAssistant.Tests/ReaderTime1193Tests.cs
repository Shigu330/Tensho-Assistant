using TenshoAssistant.Competitive;

// 0.1193 (X-177 C4.4, analysis of the slow frames of 12/11): the time a subscription spends inside a function of the game it calls on the
// player's request (the gear set reader's EquipGearset: 354 ms once) is not counted as the reader's own in the « Slow reader » line.
[SerialTests("CompetitiveGate's static measure of the game's own time (MeasureStart, GameCallEnded) is shared by the whole process")]
internal static class ReaderTime1193Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reader time 1193: behaviour: a subscription that waits 80 ms inside a function of the game is not slow by itself; the same wait outside is its own", () =>
        {
            var started = CompetitiveGate.MeasureStart();
            var call = CompetitiveGate.GameCallStarted();
            try { Thread.Sleep(80); }
            finally { CompetitiveGate.GameCallEnded(call); }
            var whole = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var own = CompetitiveGate.MeasureOwn(started);
            Check(whole >= 60 && own <= whole - 60, $"the game's time is left out: own {own:0.0} ms of {whole:0.0} ms");

            started = CompetitiveGate.MeasureStart();
            Thread.Sleep(80);
            own = CompetitiveGate.MeasureOwn(started);
            Check(own >= 60, $"the reader's own wait stays its own: {own:0.0} ms");
        }),
        ("Reader time 1193: behaviour: each measure starts from nothing: a game call noted outside a measure, or in the one before, is not taken from the next", () =>
        {
            var call = CompetitiveGate.GameCallStarted();
            Thread.Sleep(40);
            CompetitiveGate.GameCallEnded(call);
            var started = CompetitiveGate.MeasureStart();
            Thread.Sleep(40);
            var own = CompetitiveGate.MeasureOwn(started);
            Check(own >= 30, $"a call before the measure is forgotten: {own:0.0} ms");
            started = CompetitiveGate.MeasureStart();
            Thread.Sleep(40);
            Check(CompetitiveGate.MeasureOwn(started) >= 30, "and nothing is carried over after a measure");
            started = CompetitiveGate.MeasureStart();
            Check(CompetitiveGate.MeasureOwn(started) >= 0, "never negative");
        }),
        ("Reader time 1193: wiring: the gate measures each reader's own time; the gear set reader marks the game's EquipGearset, in a finally", () =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(gate.Contains("var started=MeasureStart();\n        handler(running);\n        var spent=MeasureOwn(started);", StringComparison.Ordinal), "the gate's measure");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gear/GameGearsetReader.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(reader.Contains("var call=TenshoAssistant.Competitive.CompetitiveGate.GameCallStarted();\n                    try{gearsets->EquipGearset(wanted,0);}\n                    finally{TenshoAssistant.Competitive.CompetitiveGate.GameCallEnded(call);}", StringComparison.Ordinal), "the game's call marked");
            Check(reader.Split("EquipGearset(wanted,0)").Length == 2, "one call to the game's function, the marked one");
        }),
    ];
}
