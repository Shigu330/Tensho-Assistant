using TenshoAssistant.Chat;

// X-762 (review of 0.1746, I2; D-208, the owner's choice of 21/11 at about 8:15 pm): another player's meal status is read half a second at
// most after its line, at any frame rate and through a hitch; past it, nothing is read, and no bonus rather than an older meal. Your own
// meal keeps its watch of 2 s (X-760).
internal static class MealOther1748Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The reader's loop for another player (ChatPlayerActions.ReadStatus): frame k comes `time(k)` ms after the line; the older meal `a` is
    // held at the line (0: none), `b` is put on from `bAt` ms on (-1: never). The time of every read is kept.
    private static (ushort Value, string How, List<long> Reads) Other(Func<int, long> time, ushort a, ushort b, long bAt)
    {
        var reads = new List<long>();
        ushort atLine = 0;
        for (var frame = 0; ; frame++)
        {
            var elapsed = time(frame);
            if (!ChatStatusWatch.OtherMayRead(elapsed)) { var end = ChatStatusWatch.OtherEnd(atLine); return (end.Value, end.How, reads); }
            reads.Add(elapsed);
            var seen = bAt >= 0 && elapsed >= bAt ? b : a;
            if (frame == 0) atLine = seen;
            var read = ChatStatusWatch.Other(atLine, seen, elapsed);
            if (read.Over) return (read.Value, read.How, reads);
            if (frame > 100_000) throw new Exception("the watch never ends");
        }
    }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal other 1748: behaviour: another player's status is never read past half a second, at any frame rate and through a hitch (was 2 s and 30 frames)", () =>
        {
            const ushort a = 10123;
            Check(ChatStatusWatch.OtherMs == 500, "half a second");
            foreach (var frameMs in new[] { 1, 7, 17, 50, 100, 499, 501, 3_000 })
            {
                var watch = Other(frame => (long)frame * frameMs, a, 0, -1);
                Check(watch.Reads.Count >= 1 && watch.Reads.All(at => at <= 500), $"{frameMs} ms a frame: reads at {string.Join(",", watch.Reads)}");
                Check(watch.Value == 0 && watch.How.Contains("no bonus", StringComparison.Ordinal), $"{frameMs} ms a frame: no bonus for a meal unchanged since the line: {watch}");
            }
            var hitch = Other(frame => frame switch { 0 => 0, 1 => 400, _ => 900 }, a, 0, -1);
            Check(hitch.Reads.SequenceEqual([0L, 400L]) && hitch.Value == 0, "a hitch past half a second: the late frame reads nothing: " + string.Join(",", hitch.Reads));
            Check(ChatStatusWatch.OtherMayRead(500) && !ChatStatusWatch.OtherMayRead(501), "the limit");
        }),
        ("Meal other 1748: behaviour: meal B put on within half a second is B; past it, no bonus, never the older meal A; none at all: not found", () =>
        {
            const ushort a = 10123, b = 456;
            foreach (var (frameMs, held, bAt, expected, how) in new (int, ushort, long, ushort, string)[]
            {
                (17, a, 300, b, "changed"), (7, a, 490, b, "changed"), (17, 0, 300, b, "put on after"),
                (17, a, 700, 0, "no bonus"), (7, a, 505, 0, "no bonus"), (17, 0, 700, 0, "not found"), (17, 0, -1, 0, "not found"),
            })
            {
                var watch = Other(frame => (long)frame * frameMs, held, b, bAt);
                Check(watch.Value == expected && watch.How.Contains(how, StringComparison.Ordinal) && watch.Value != a, $"{frameMs} ms a frame, A held: {held != 0}, B at {bAt} ms: {watch.Value} « {watch.How} »");
                Check(watch.Reads.All(at => at <= 500), "never read past half a second");
            }
        }),
        ("Meal other 1748: behaviour: your own meal keeps its watch of 2 s and 30 frames (X-760 unchanged)", () =>
        {
            const ushort a = 10123, b = 456;
            Check(ChatStatusWatch.Next(a, a, 30, 1_999) is { Over: false } && ChatStatusWatch.Next(a, a, 30, 2_000) is { Over: true, Value: a }, "your own unchanged meal: the line's, after 2 s");
            Check(ChatStatusWatch.Next(a, b, 90, 1_500) is { Over: true, Value: b }, "your own meal put on after 1.5 s: found");
        }),
        ("Meal other 1748: wiring: the reader stops another player's watch before reading anything past half a second; rule 5 and the evidence say how long, both halves", () =>
        {
            var players = Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            var stop = players.IndexOf("if(name is not null && !ChatStatusWatch.OtherMayRead(elapsed)){StatusSaid(ChatStatusWatch.OtherEnd(atLine).How);return;}", StringComparison.Ordinal);
            var look = players.IndexOf("var character=name is null ? objects.LocalPlayer", StringComparison.Ordinal);
            Check(stop > 0 && look > stop, "the limit checked before the player is looked for");
            Check(players.Contains("var read=name is null ? ChatStatusWatch.Next(atLine,seen,frame,elapsed):ChatStatusWatch.Other(atLine,seen,elapsed);", StringComparison.Ordinal), "another player's own watch");
            var publishing = Read("docs/project/PUBLISHING.md");
            Check(publishing.Contains("reads that player's status when the line arrives and for half a second at most after it (D-208,", StringComparison.Ordinal)
                && publishing.Contains("lit le statut de ce joueur à l'arrivée de la ligne et une demi-seconde au plus après elle (D-208,", StringComparison.Ordinal), "rule 5, en and fr");
            var evidence = Read("docs/evidence/0.1739-meal-status-gained.md");
            Check(evidence.Contains("another player's status is read half a second at most after the line", StringComparison.Ordinal)
                && evidence.Contains("le statut d'un autre joueur est lu une\n  demi-seconde au plus après la ligne", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
