using TenshoAssistant.Chat;

// X-756 (0.1739, review of 0.1738, I1): a meal's line names the meal it gained, compared with the one held at the line; only the lines of a
// buff gained (kind 46).
internal static class MealStatus1739Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    // The reader's loop: frame 0 at the line, then one read a frame; the Param seen at each frame (the last one stays after the list).
    // X-760 (0.1744): at 60 frames a second (17 ms a frame); the watch ends at the later of 30 frames and 2 s (End).
    private static (ushort Value, int Frame, string How) Watch(params ushort[] seen)
    {
        var atLine = seen[0];
        for (var frame = 0; ; frame++)
        {
            var read = ChatStatusWatch.Next(atLine, seen[Math.Min(frame, seen.Length - 1)], frame, frame * 17L);
            if (read.Over) return (read.Value, frame, read.How);
            if (frame > 1000) throw new Exception("the watch never ends");
        }
    }
    private static readonly int End = Math.Max(ChatStatusWatch.Frames, (ChatStatusWatch.WatchMs + 16) / 17);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal status 1739: behaviour: Well Fed from meal A, meal B eaten, the line printed before the status: B, never A (the review's case)", () =>
        {
            const ushort a = 10123, b = 456;
            var read = Watch(a, a, b);
            Check(read.Value == b && read.Frame == 2 && read.How.StartsWith("changed", StringComparison.Ordinal), $"B as soon as it is put on: {read}");
            Check(ChatStatusWatch.Next(a, a, 0, 0) is { Over: false }, "nothing taken at the line while an older meal is held (was: A at once)");
        }),
        ("Meal status 1739: behaviour: status put on before the line, same meal again, put on after, lost then gained, never", () =>
        {
            const ushort a = 10123, b = 456;
            var before = Watch(b);
            Check(before.Value == b && before.Frame == End && before.How.StartsWith("held", StringComparison.Ordinal), $"put on before the line: B after the watch: {before}");
            Check(Watch(a, a, a).Value == a, "the same meal eaten again: its own Param");
            Check(Watch(0, 0, 0, b) is { Value: b, Frame: 3 } after && after.How.StartsWith("put on", StringComparison.Ordinal), "not held at the line: the first Param seen");
            Check(Watch(a, 0, 0, b).Value == b, "A gone, then B: B");
            Check(Watch(0) is { Value: 0 } none && none.How == "not found" && none.Frame == End, "never: not found, after the watch");
            Check(Watch(a, 0).Value == 0, "A lost and nothing gained: not found, never A");
            var frames = ChatStatusWatch.Frames;
            Check(frames >= 10 && frames <= 60, "about half a second at 60 frames a second");
        }),
        ("Meal status 1739: behaviour: only the lines of a buff gained (kind 46), never a status lost (48) nor a harmful one (47, 49)", () =>
        {
            Check(ChatStatusWatch.Reads(46) && !ChatStatusWatch.Reads(48) && !ChatStatusWatch.Reads(47) && !ChatStatusWatch.Reads(49) && !ChatStatusWatch.Reads(0), "kind 46 only");
        }),
        ("Meal status 1739: wiring: the reader keeps the Param held at the line and asks the watch; only kind 46; PUBLISHING rule 5 says it, both halves", () =>
        {
            var players = Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(players.Contains("if(frame==0)atLine=seen;", StringComparison.Ordinal) && players.Contains("var read=name is null ? ChatStatusWatch.Next(atLine,seen,frame,elapsed):ChatStatusWatch.Other(atLine,seen,elapsed);", StringComparison.Ordinal)
                && players.Contains("if(read.Over){if(read.Value!=0)param.Value=read.Value;StatusSaid(read.How);", StringComparison.Ordinal), "the watch decides");
            Check(!players.Contains("\"at once\"", StringComparison.Ordinal), "no Param taken at once");
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("if(!ChatStatusWatch.Reads((int)message.LogKind))return body;", StringComparison.Ordinal), "a status lost keeps its description");
            var publishing = File.ReadAllText(Path.GetFullPath("docs/project/PUBLISHING.md"));
            Check(publishing.Contains("reads that player's status when the line arrives", StringComparison.Ordinal) && publishing.Contains("lit le statut de ce joueur à l'arrivée de la ligne", StringComparison.Ordinal), "rule 5, en and fr");
        }),
    ];
}
