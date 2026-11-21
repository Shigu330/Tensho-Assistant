using TenshoAssistant.Chat;

// X-766 (review of 0.1751, X-764 M2; D-211, the owner's choice of 21/11/2026 at about 10:30 pm, R04): another player's Well Fed status held
// when its line arrives is the line's when its remaining time is the meal's full duration (ItemAction: 1800 s for every meal), put on just
// before the line; an older meal, a meal eaten again over the same one (its time added) or a meal-duration extension held: no bonus, as
// before (D-208). Still never read past half a second.
internal static class MealFull1757Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const int Full = 1800;

    // The reader's loop for another player's Well Fed status (ChatPlayerActions.ReadStatus): frame k comes `frameMs` × k ms after the line;
    // meal `a` is held at the line with `remaining` seconds left (0: none held), meal `b` is put on from `bAt` ms on (-1: never); `extended`:
    // a meal-duration extension held. The time of every read is kept.
    private static (ushort Value, string How, List<long> Reads) Watch(int frameMs, ushort a, float remaining, ushort b, long bAt, bool extended = false, Func<int, long>? time = null)
    {
        var reads = new List<long>();
        ushort atLine = 0;
        var fresh = false;
        for (var frame = 0; ; frame++)
        {
            var elapsed = time?.Invoke(frame) ?? (long)frame * frameMs;
            if (!ChatStatusWatch.OtherMayRead(elapsed)) { var end = ChatStatusWatch.OtherEnd(atLine, fresh); return (end.Value, end.How, reads); }
            reads.Add(elapsed);
            var seen = bAt >= 0 && elapsed >= bAt ? b : a;
            if (frame == 0) atLine = seen;
            if (frame == 0 && seen != 0) fresh = ChatStatusWatch.PutOnAtLine(seen == a ? remaining : Full, Full, extended);
            var read = ChatStatusWatch.Other(atLine, seen, elapsed, fresh);
            if (read.Over) return (read.Value, read.How, reads);
            if (frame > 100_000) throw new Exception("the watch never ends");
        }
    }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal full 1757: behaviour: a first meal put on just before its line (at its full duration) shows its bonus, at any frame rate and through a hitch", () =>
        {
            const ushort meal = 10123;
            foreach (var frameMs in new[] { 1, 7, 17, 100, 499, 501, 3_000 })
                foreach (var left in new[] { 1800f, 1799.9f, 1798.2f, 1797f })
                {
                    var watch = Watch(frameMs, meal, left, 0, -1);
                    Check(watch.Value == meal && watch.How.Contains("full duration", StringComparison.Ordinal), $"{frameMs} ms a frame, {left} s left: {watch}");
                    Check(watch.Reads.All(at => at <= ChatStatusWatch.OtherMs), "never read past half a second");
                }
            var hitch = Watch(0, meal, 1799.5f, 0, -1, time: frame => frame switch { 0 => 0, 1 => 400, _ => 900 });
            Check(hitch.Value == meal && hitch.Reads.SequenceEqual([0L, 400L]), "a hitch: the late frame reads nothing, the meal held at its full duration at the line is shown: " + hitch);
        }),
        ("Meal full 1757: behaviour: an older meal held at the line shows no bonus, never its own; the line's meal put on within half a second wins", () =>
        {
            const ushort older = 10123, meal = 456;
            foreach (var left in new[] { 1796.9f, 1500f, 900f, 12f, 0.5f })
            {
                var watch = Watch(17, older, left, meal, 700);
                Check(watch.Value == 0 && watch.How.Contains("no bonus", StringComparison.Ordinal), $"an older meal with {left} s left, the line's put on past half a second: {watch}");
            }
            var changed = Watch(17, older, 1500f, meal, 300);
            Check(changed.Value == meal && changed.How.Contains("changed", StringComparison.Ordinal), "the line's meal put on within half a second: " + changed);
            // An older meal eaten that very moment (at its full duration) and the line's put on within half a second: the change still wins.
            var both = Watch(17, older, 1799.8f, meal, 200);
            Check(both.Value == meal, "a change within half a second wins over a full duration at the line: " + both);
        }),
        ("Meal full 1757: behaviour: the same meal eaten again (its time added, up to 60 min) or a meal-duration extension held: no bonus, as before", () =>
        {
            const ushort meal = 10123;
            foreach (var left in new[] { 2400f, 3600f, 1801f })
            {
                var again = Watch(17, meal, left, 0, -1);
                Check(again.Value == 0 && again.How.Contains("no bonus", StringComparison.Ordinal), $"the same meal eaten again, {left} s left: {again}");
            }
            foreach (var left in new[] { 2100f, 2400f, 2700f, 1800f, 1799f })
            {
                var extended = Watch(17, meal, left, 0, -1, extended: true);
                Check(extended.Value == 0, $"a meal-duration extension held, {left} s left: never taken (an older meal could sit at 1800 s): {extended}");
            }
            Check(ChatStatusWatch.ExtendsMeals(360) && ChatStatusWatch.ExtendsMeals(1084) && !ChatStatusWatch.ExtendsMeals(48) && !ChatStatusWatch.ExtendsMeals(49), "Meat and Mead, Rationing");
            Check(!ChatStatusWatch.PutOnAtLine(1800f, 0, false), "an unknown duration: never");
            Check(ChatStatusWatch.PutOnAtLine(1800f - ChatStatusWatch.FreshSeconds, Full, false) && !ChatStatusWatch.PutOnAtLine(1800f - ChatStatusWatch.FreshSeconds - 0.01f, Full, false)
                && ChatStatusWatch.PutOnAtLine(1800.5f, Full, false) && !ChatStatusWatch.PutOnAtLine(1800.6f, Full, false), "the limits");
            Check(Watch(17, 0, 0f, 0, -1).How.Contains("not found", StringComparison.Ordinal), "none at all: not found");
        }),
        ("Meal full 1757: behaviour: the meals' durations by Param, NQ and HQ, from the ItemAction rows; a Param given two durations has none", () =>
        {
            var durations = ChatStatusWatch.MealDurations(
            [
                (48, 123, 1800, false), (48, 123, 1800, true), (48, 77, 1800, false), (48, 77, 1800, false),
                (48, 90, 1800, false), (48, 90, 900, false), (49, 200, 30, false), (48, 300, 0, false), (853, 400, 1800, false), (48, 0, 1800, false),
            ]);
            Check(durations.GetValueOrDefault((ushort)123) == 1800 && durations.GetValueOrDefault((ushort)10123) == 1800 && durations.GetValueOrDefault((ushort)77) == 1800, "NQ and HQ");
            Check(!durations.ContainsKey(90), "two durations: unknown");
            Check(!durations.ContainsKey(200) && !durations.ContainsKey(300) && !durations.ContainsKey(400) && !durations.ContainsKey(0) && durations.Count == 3, "only meals with a duration: " + string.Join(",", durations));
        }),
        ("Meal full 1757: wiring: the reader reads the remaining time and the extensions at the line, Well Fed only, durations from ItemAction; rule 5 and the evidence say it, both halves", () =>
        {
            var players = Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(players.Contains("if(seen==0 && held.StatusId==status && held.Param!=0){seen=held.Param;remaining=held.RemainingTime;}", StringComparison.Ordinal)
                && players.Contains("if(ChatStatusWatch.ExtendsMeals(held.StatusId))extended=true;", StringComparison.Ordinal), "the remaining time and the extensions");
            Check(players.Contains("if(frame==0 && name is not null && status==Craft.CraftConsumables.WellFed && seen!=0)fresh=MealPutOnAtLine(seen,remaining,extended);", StringComparison.Ordinal), "at the line, another player's Well Fed only");
            Check(players.Contains("data.GetExcelSheet<Lumina.Excel.Sheets.ItemAction>()", StringComparison.Ordinal) && players.Contains("return ChatStatusWatch.PutOnAtLine(remaining,full,extended);", StringComparison.Ordinal), "the durations from the game");
            var publishing = Read("docs/project/PUBLISHING.md");
            Check(publishing.Contains("its remaining time at the line (D-211,", StringComparison.Ordinal) && publishing.Contains("son temps restant à la ligne (D-211,", StringComparison.Ordinal), "rule 5, en and fr");
            var evidence = Read("docs/evidence/0.1739-meal-status-gained.md");
            Check(evidence.Contains("X-766", StringComparison.Ordinal) && evidence.Contains("447 meal actions", StringComparison.Ordinal) && evidence.Contains("447 actions de repas", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
