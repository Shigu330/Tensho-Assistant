using TenshoAssistant.Gathering;

// 0.1685 (X-701, X-702, user requests of 21/11 at sea): the fishing stopwatch stops at the bite read in the game and stays frozen until the next
// cast; the fish whose bite window holds the frozen time are put forward; under Chum the windows are halved (the user's rule: the game and the
// source give no figure, docs/evidence/0.1685-fishing-bite-stopwatch.md).
internal static class FishingBite1685Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset T0 = new(2026, 11, 21, 3, 30, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Fishing bite 1685: the line's states make the last cast: cast, bite and strength, hook, caught or not, next cast", () =>
        {
            var line = new FishingLine();
            var chumAsked = 0;
            bool? chum = false;
            ushort timeline = 0;
            void See(int state, double seconds) => line.See(state, T0.AddSeconds(seconds), () => { chumAsked++; return chum; }, () => timeline);
            See(4, 0);
            Check(line.Last is null, "no cast yet: nothing");
            See(1, 1); See(12, 2);
            Check(line.Last is { Bite: null, Strength: 0, Chum: false } cast && cast.Cast == T0.AddSeconds(1) && chumAsked == 1, "the cast, Chum read once at the cast");
            See(9, 5); See(12, 6);
            Check(line.Last!.Bite is null && line.Last.Cast == T0.AddSeconds(1), "a lure does not restart the cast nor bite");
            // The bite: the animation comes a frame after the state (0.1095).
            See(5, 14.6);
            Check(line.Last!.Bite == T0.AddSeconds(14.6) && line.Last.Strength == 0, "the bite at state 5, its strength not shown yet");
            timeline = 293; See(5, 14.7);
            Check(line.Last!.Strength == 2 && line.Last.Bite == T0.AddSeconds(14.6), "strength read on the next frame (!!), the bite time kept");
            timeline = 0; See(6, 15); See(2, 16); See(4, 18);
            Check(line.Last is { Strength: 2 } held && held.Bite == T0.AddSeconds(14.6), "hooked and caught: the bite stays until the next cast");
            // Next cast under Chum: the bite gone, Chum read.
            chum = true; See(1, 20); See(12, 21);
            Check(line.Last is { Bite: null, Strength: 0, Chum: true } next && next.Cast == T0.AddSeconds(20) && chumAsked == 2, "the next cast clears the bite; Chum at its start");
            timeline = 294; See(5, 24); See(4, 26);
            Check(line.Last is { Strength: 3, Chum: true } legend && legend.Bite == T0.AddSeconds(24), "« !!! », nothing caught (back to PoleReady): the bite stays");
            chum = null; See(1, 30);
            Check(line.Last is { Chum: null, Bite: null }, "Chum not read: unknown, never guessed");
            Check(FishingLine.Strength(292) == 1 && FishingLine.Strength(277) == 0, "the strengths of FishingHookStrength only");
        }),
        ("Fishing bite 1685: the stopwatch runs, freezes at the bite of its cast, and restarts at the next cast as before", () =>
        {
            var since = T0;
            Check(FishingStopwatch.Elapsed(since, null, T0.AddSeconds(7.5)) == 7.5 && !FishingStopwatch.Frozen(since, null), "running");
            var bite = T0.AddSeconds(13.6);
            Check(FishingStopwatch.Frozen(since, bite) && FishingStopwatch.Elapsed(since, bite, T0.AddSeconds(40)) is { } frozen && Math.Abs(frozen - 13.6) < 1e-3, "frozen at the bite, whatever the time now");
            // A manual restart after the bite: running again from the restart.
            var restart = T0.AddSeconds(20);
            Check(!FishingStopwatch.Frozen(restart, bite) && FishingStopwatch.Elapsed(restart, bite, T0.AddSeconds(25)) == 5, "a restart after the bite runs");
            // The next cast restarts it (C10): the line's bite is cleared at that cast.
            var next = FishingStopwatch.FromCast(since, T0.AddSeconds(30));
            Check(next == T0.AddSeconds(30) && !FishingStopwatch.Frozen(next, null) && FishingStopwatch.Elapsed(next, null, T0.AddSeconds(31)) == 1, "the next cast: from zero");
            Check(FishingStopwatch.Elapsed(null, bite, T0) is null, "not fishing: nothing");
        }),
        ("Fishing bite 1685: a bite window of floored seconds holds [min, max + 1); halved under Chum; the frozen time picks the fish", () =>
        {
            Check(FishingStopwatch.Inside(4, 4, 8, false) && FishingStopwatch.Inside(8.99, 4, 8, false) && !FishingStopwatch.Inside(9, 4, 8, false) && !FishingStopwatch.Inside(3.99, 4, 8, false), "4–8 s: 4 to 9 excluded");
            Check(FishingStopwatch.Passed(9, 8, false) && !FishingStopwatch.Passed(8.5, 8, false), "passed after max + 1");
            Check(FishingStopwatch.Window(4, 8, true) == (2, 4.5) && FishingStopwatch.Inside(2, 4, 8, true) && FishingStopwatch.Inside(4.4, 4, 8, true) && !FishingStopwatch.Inside(4.5, 4, 8, true), "under Chum: 2 to 4.5");
            Check(FishingStopwatch.Passed(4.5, 8, true) && !FishingStopwatch.Passed(4.5, 8, false), "passed sooner under Chum");
            Check(FishingStopwatch.Shown(9, true) == 4.5 && FishingStopwatch.Shown(9, false) == 9 && FishingStopwatch.ChumFactor == 0.5, "the times shown");
            // Frozen at 13.6 s: of three fish, those whose window holds it.
            var fish = new[] { (Name: "a", Min: 4, Max: 8), (Name: "b", Min: 12, Max: 16), (Name: "c", Min: 13, Max: 13) };
            Check(fish.Where(f => FishingStopwatch.Inside(13.6, f.Min, f.Max, false)).Select(f => f.Name).SequenceEqual(["b", "c"]), "put forward: b and c");
            Check(fish.Where(f => FishingStopwatch.Inside(6.8, f.Min, f.Max, true)).Select(f => f.Name).SequenceEqual(["b", "c"]), "under Chum, 6.8 s: b (6–8.5) and c (6.5–7)");
        }),
        ("Fishing bite 1685: wiring: the reader feeds the line every frame while it bites, both panels draw the one clock and cell, display only", () =>
        {
            var reader = Read("src/TenshoAssistant/Gathering/FishingBaitReader.Dalamud.cs");
            Check(reader.Contains("if(state==lineState && state!=FishingLine.Bite)return;") && reader.Contains("line.See(state,DateTimeOffset.UtcNow,") && reader.Contains("status.StatusId==ChumStatus") && reader.Contains("ChumStatus=763"), "the line fed with the state, the strength and Chum");
            Check(reader.Contains("public DateTimeOffset? CastAt=>line.Last?.Cast;") && Read("src/TenshoAssistant/Plugin.Gathering.cs").Contains("mainWindow.FishingLastCast=()=>baitReader?.LastCast;"), "one source for the cast and the bite");
            var clock = Read("src/TenshoAssistant/MainWindow.FishingClock.cs");
            Check(clock.Contains("FishingStopwatch.Frozen(since,cast?.Bite)") && clock.Contains("localization.Format(\"FishingPanel.Frozen\",clock.Seconds,BiteMark(clock.Strength))") && clock.Contains("ImGui.TextColored(Selected,\"» \"+text)"), "frozen, its strength, the fish put forward");
            Check(!clock.Contains("ExecuteCommand") && !clock.Contains("UseAction") && !clock.Contains("SendChat"), "display only (R03.3)");
            foreach (var panel in new[] { "MainWindow.FishingPanel.cs", "MainWindow.OceanVoyagePanel.cs" })
            {
                var text = Read("src/TenshoAssistant/" + panel);
                Check(text.Contains("ReadFishingClock(") && text.Contains("DrawFishingClock(shownClock,ref ") && text.Contains("DrawBiteTimeCell(time,clock)") && !text.Contains("(int)(DateTimeOffset.UtcNow-since).TotalSeconds"), panel + ": the one clock and cell");
            }
            Check(clock.Contains("(chum ? \"\\n\"+localization.Get(\"FishingPanel.BiteTimeChum\"):\"\")"), "each cell of the bite time column says the rule under Chum");
            Check(Read("docs/evidence/0.1685-fishing-bite-stopwatch.md").Contains("ActionTransient 4104") && Read("docs/evidence/0.1685-fishing-bite-stopwatch.md").Contains("bite_time_per_fish_per_spot_per_bait"), "the evidence cites the game and the source");
        }),
    ];
}
