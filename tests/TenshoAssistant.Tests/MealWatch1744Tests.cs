using TenshoAssistant.Chat;

// X-760 (0.1744, review of 0.1742, M2): the meal's watch lasts 30 frames and 2 s at least: a meal put on over an older one past 30 frames is
// still the line's at any frame rate; the delay the game takes is written in the log (the longest seen).
internal static class MealWatch1744Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The reader's loop at `frameMs` a frame: the older meal `a` held at the line, `b` put on at frame `at` (0: never).
    private static (ushort Value, int Frame, string How) Watch(int frameMs, ushort a, ushort b, int at)
    {
        for (var frame = 0; ; frame++)
        {
            var read = ChatStatusWatch.Next(a, at > 0 && frame >= at ? b : a, frame, (long)frame * frameMs);
            if (read.Over) return (read.Value, frame, read.How);
            if (frame > 10_000) throw new Exception("the watch never ends");
        }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal watch 1744: behaviour: meal B put on over an older meal A past 30 frames: B at 144, 60 and 20 frames a second (was A past half a second at 60, a fifth at 144)", () =>
        {
            const ushort a = 10123, b = 456;
            foreach (var (frameMs, at) in new[] { (7, 60), (7, 250), (17, 40), (17, 110), (50, 35) })
            {
                var read = Watch(frameMs, a, b, at);
                Check(read.Value == b && read.Frame == at && read.How.StartsWith("changed", StringComparison.Ordinal), $"{frameMs} ms a frame, B at frame {at} ({at * frameMs} ms): {read}");
            }
        }),
        ("Meal watch 1744: behaviour: the watch ends at the later of 30 frames and 2 s, never sooner (a frame of 3 s: 30 frames still)", () =>
        {
            const ushort a = 10123;
            Check(Watch(7, a, 0, 0) is { Value: a } fast && fast.Frame == (ChatStatusWatch.WatchMs + 6) / 7 && fast.How.StartsWith("held", StringComparison.Ordinal), "144 frames a second: 2 s");
            Check(Watch(100, a, 0, 0).Frame == ChatStatusWatch.Frames, "10 frames a second: 30 frames (3 s)");
            Check(ChatStatusWatch.Next(a, a, 1, 3_000) is { Over: false } && ChatStatusWatch.Next(0, 0, 29, 60_000) is { Over: false }, "a frame that took 3 s (a hitch): not over before 30 frames");
            Check(ChatStatusWatch.Next(0, 0, 30, 1_999) is { Over: false } && ChatStatusWatch.Next(0, 0, 30, 2_000) is { Over: true, Value: 0, How: "not found" }, "not over before 2 s");
            Check(ChatStatusWatch.WatchMs == 2_000 && ChatStatusWatch.Frames == 30, "2 s and 30 frames");
        }),
        ("Meal watch 1744: wiring: the reader gives the time since the line and logs the longest delay seen, each time it grows", () =>
        {
            var players = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(players.Contains("=>ReadStatus(param,status,name,world,0,0,Environment.TickCount64);", StringComparison.Ordinal)
                && players.Contains("var elapsed=Environment.TickCount64-lineAt;", StringComparison.Ordinal)
                && players.Contains("framework.RunOnTick(()=>ReadStatus(param,status,name,world,frame+1,atLine,lineAt),delayTicks:1);", StringComparison.Ordinal), "the time since the line");
            Check(players.Contains("if(read.Value!=0 && read.Value!=atLine)StatusLate(frame,elapsed);return;}", StringComparison.Ordinal)
                && players.Contains("if(elapsed<=statusLateMs)return;", StringComparison.Ordinal) && players.Contains("the longest seen since the plugin loaded", StringComparison.Ordinal), "the longest delay, once per new longest");
        }),
    ];
}
