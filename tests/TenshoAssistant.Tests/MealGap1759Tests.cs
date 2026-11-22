using TenshoAssistant.Chat;

// X-768 (review of 0.1758, M1 and M2). M1: the log line meant to measure the margin of 3 s (ChatStatusWatch.FreshSeconds) wrote only the
// smallest gap seen since the plugin loaded: a first meal past the margin (no bonus) was never written. MealGapLog says the largest gap taken
// and each near one not taken. M2 (D-208, never an older meal): an older meal A eaten less than 3 s before the line of a meal B is still at its
// full duration at B's line; B put on past half a second, A was shown for B's line. A status at its full duration that is the meal shown for
// the same player's line of the 4 s before is now that older meal.
internal static class MealGap1759Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const int Full = 1800;

    // The reader's loop for another player's Well Fed line (ChatPlayerActions.ReadStatus and MealPutOnAtLine): frame k comes 17 ms × k after
    // the line; `held` is held at the line with `remaining` seconds left, `put` is put on from `putAt` ms on (-1: never); `earlier`: the meals
    // shown for the same player's lines before, with their line's time; the line arrives at `lineAt`.
    private static (ushort Value, string How) Watch(ushort held, float remaining, ushort put, long putAt, IEnumerable<(ushort, long)> earlier, long lineAt)
    {
        ushort atLine = 0;
        var fresh = false;
        for (var frame = 0; ; frame++)
        {
            var elapsed = frame * 17L;
            if (!ChatStatusWatch.OtherMayRead(elapsed)) { var end = ChatStatusWatch.OtherEnd(atLine, fresh); return (end.Value, end.How); }
            var seen = put != 0 && putAt >= 0 && elapsed >= putAt ? put : held;
            if (frame == 0) atLine = seen;
            if (frame == 0 && seen != 0)
                fresh = !ChatStatusWatch.ShownJustBefore(seen, earlier, lineAt) && ChatStatusWatch.PutOnAtLine(seen == held ? remaining : Full, Full, false);
            var read = ChatStatusWatch.Other(atLine, seen, elapsed, fresh);
            if (read.Over) return (read.Value, read.How);
        }
    }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal gap 1759: behaviour: the log says the largest gap taken, and each near one not taken (a first meal past the margin), never an older meal", () =>
        {
            var log = new MealGapLog();
            var first = log.Say(1799.8f, Full, false, false);
            Check(first is not null && first.Contains("0.2 s from its full duration (1800 s)", StringComparison.Ordinal) && first.Contains("taken as put on just before its line", StringComparison.Ordinal), "a first meal at 0.2 s: " + first);
            Check(log.Say(1799.9f, Full, false, false) is null, "a smaller gap taken: nothing new");
            // The case of the review: after a first meal at 0.2 s, another first meal at 3.5 s (no bonus) was never written.
            var missed = log.Say(1796.5f, Full, false, false);
            Check(missed is not null && missed.Contains("3.5 s from its full duration", StringComparison.Ordinal) && missed.Contains("past the 3 s margin, not taken", StringComparison.Ordinal), "a first meal at 3.5 s: " + missed);
            Check(log.Say(1796.5f, Full, false, false) is not null, "each near one is said, not only the farthest");
            var larger = log.Say(1797.6f, Full, false, false);
            Check(larger is not null && larger.Contains("2.4 s", StringComparison.Ordinal) && larger.Contains("the largest gap taken", StringComparison.Ordinal), "the largest gap taken grows: " + larger);
            Check(log.Say(1797.6f, Full, false, false) is null, "the same largest gap: nothing new");
            Check(log.Say(1750f, Full, false, false) is null && log.Say(900f, Full, false, false) is null, "an older meal, farther than 30 s: nothing");
            Check(log.Say(1770f, Full, false, false) is not null, "30 s: still near");
            Check(log.Say(2400f, Full, false, false) is null && log.Say(1801f, Full, false, false) is null, "past the full duration (eaten again): nothing");
            Check(log.Say(1799f, Full, true, false) is null && log.Say(1799f, 0, false, false) is null, "an extension held, or an unknown duration: nothing");
            var older = log.Say(1799f, Full, false, true);
            Check(older is not null && older.Contains("the meal shown for that player's line of the 4 s before", StringComparison.Ordinal), "the meal of the line just before: " + older);
        }),
        ("Meal gap 1759: behaviour: the near lines are bounded (20 since the plugin loaded), the last one says so; the largest gap taken is still said", () =>
        {
            var log = new MealGapLog();
            var said = Enumerable.Range(0, 30).Select(_ => log.Say(1795f, Full, false, false)).ToList();
            Check(said.Take(MealGapLog.NearMost).All(line => line is not null) && said.Skip(MealGapLog.NearMost).All(line => line is null), "20 at most: " + said.Count(line => line is not null));
            Check(said[MealGapLog.NearMost - 1]!.Contains("the next ones are not written", StringComparison.Ordinal) && !said[0]!.Contains("not written", StringComparison.Ordinal), "the last one says so");
            Check(log.Say(1798f, Full, false, false) is not null, "the largest gap taken is not bounded by the near ones");
        }),
        ("Meal gap 1759: behaviour: the meal shown for the same player's line of the 4 s before is an older meal, not the line's", () =>
        {
            const ushort a = 10123, b = 456;
            Check(ChatStatusWatch.ShownJustBefore(a, [(a, 1_000)], 3_000) && ChatStatusWatch.ShownJustBefore(a, [(b, 2_500), (a, 1_000)], 5_000), "within 4 s");
            Check(!ChatStatusWatch.ShownJustBefore(a, [(a, 1_000)], 5_001), "past 4 s: no longer");
            Check(!ChatStatusWatch.ShownJustBefore(a, [(b, 1_000)], 2_000) && !ChatStatusWatch.ShownJustBefore(a, [(0, 1_000)], 2_000), "another meal, or a line still being read (0)");
            Check(!ChatStatusWatch.ShownJustBefore(0, [(0, 1_000)], 2_000) && !ChatStatusWatch.ShownJustBefore(a, [(a, 3_000)], 2_000) && !ChatStatusWatch.ShownJustBefore(a, [], 2_000), "none held, a later line, none before");
            Check(ChatStatusWatch.EarlierMealMs >= (ChatStatusWatch.FreshSeconds + 0.5f) * 1000 + ChatStatusWatch.OtherMs, "4 s covers 3 s of margin, the half second above the full duration and half a second of watch");
        }),
        ("Meal gap 1759: behaviour: meal A shown for its line, meal B eaten 2 s after: B past the half second shows no bonus (was A's), B within it or before its line shows B's", () =>
        {
            const ushort a = 10123, b = 456;
            // A's line at 0 ms: A put on just before it, shown (its full duration).
            var lineA = Watch(a, 1800f, 0, -1, [], 0);
            Check(lineA.Value == a, "A's line shows A: " + lineA);
            IEnumerable<(ushort, long)> earlier = [(lineA.Value, 0L)];
            // B's line 2 s later: A still at 1798 s.
            var late = Watch(a, 1798f, b, 700, earlier, 2_000);
            Check(late.Value == 0 && late.How.Contains("no bonus", StringComparison.Ordinal), "B put on past the half second: no bonus, never A: " + late);
            var within = Watch(a, 1798f, b, 300, earlier, 2_000);
            Check(within.Value == b, "B put on within the half second: B: " + within);
            var before = Watch(b, 1800f, 0, -1, earlier, 2_000);
            Check(before.Value == b && before.How.Contains("full duration", StringComparison.Ordinal), "B put on just before its line: B: " + before);
            // Before X-768, the same line showed A (the older meal), what D-208 excluded.
            var old = Watch(a, 1798f, b, 700, [], 2_000);
            Check(old.Value == a, "without the line before (another player, or none seen): as 0.1757: " + old);
            // A's line 4.5 s before: no longer compared (A is then past the margin anyway).
            Check(Watch(a, 1795.5f, b, 700, [(a, 0L)], 4_500).Value == 0, "4.5 s after: past the margin, no bonus");
        }),
        ("Meal gap 1759: wiring: the reader compares with the same player's lines of the 4 s before, records each line's box after, says the gap by MealGapLog; rule 5 and the evidence say it, both halves", () =>
        {
            var players = Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(players.Contains("fresh=MealPutOnAtLine(seen,remaining,extended,name,world,lineAt);\n            if(frame==0 && name is not null && status==Craft.CraftConsumables.WellFed)MealLine(param,name,world,lineAt);", StringComparison.Ordinal), "checked, then recorded");
            Check(players.Contains("ChatStatusWatch.ShownJustBefore(param,mealLines.Where(line=>line.Name==name && line.World==world)", StringComparison.Ordinal) && players.Contains("if(earlier)return false;", StringComparison.Ordinal), "the same player's lines");
            Check(players.Contains("mealLines.RemoveAll(line=>lineAt-line.LineAt>ChatStatusWatch.EarlierMealMs);", StringComparison.Ordinal), "dropped past 4 s");
            Check(players.Contains("if(mealGaps.Say(remaining,full,extended,earlier) is { } said)", StringComparison.Ordinal) && !players.Contains("mealGapSaid", StringComparison.Ordinal), "the gap said by MealGapLog");
            var publishing = Read("docs/project/PUBLISHING.md");
            Check(publishing.Contains("the meal shown for that player's line of the 4 seconds before is that older meal: no bonus, X-768", StringComparison.Ordinal)
                && publishing.Contains("le repas montré pour la ligne de ce joueur des 4 secondes d'avant est cet ancien repas : aucun bonus, X-768", StringComparison.Ordinal), "rule 5, en and fr");
            var evidence = Read("docs/evidence/0.1739-meal-status-gained.md");
            Check(evidence.Contains("An older meal eaten just before (0.1759, X-768", StringComparison.Ordinal) && evidence.Contains("Un ancien repas pris juste avant (0.1759, X-768", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
