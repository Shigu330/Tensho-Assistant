using TenshoAssistant.Chat;

// X-558 (0.1779): the slow chat frames of 22/11 (35 to 49.5 ms, the tabs' refresh 0.0 to 0.1 ms) each held a pause of the runtime for a
// collection (dalamud.log, « Runtime suspended every managed thread 26 to 34 ms », the same frame): each slow frame now says the runtime's
// pauses and the compilations on the game's thread during it, and the chat's own time; the minute's line counts the frames over by that time.
internal static class ChatLoadFrames1779Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat load frames 1779: behaviour: a slow frame says the runtime's pauses, the compilations and the chat's own time; the minute counts the frames over by it apart", () =>
        {
            Check(ChatLoadFrames.Own(42, 34, 0) == 8 && ChatLoadFrames.Own(20, 30, 5) == 0 && ChatLoadFrames.Own(30, -1, -2) == 30, "the chat's own part, never below 0, a negative part ignored");
            var frames = new ChatLoadFrames();
            // The frame of 05:19:19 on 22/11: 42.0 ms, the refresh 0.0 ms, a pause of the runtime of 34 ms in it.
            var gc = frames.Add(10_300, 42, 0, 34, 0);
            Check(gc is [var line] && line.Contains("frame of 42.0 ms 10.3 s after the load", StringComparison.Ordinal) && line.Contains("refresh 0.0 ms; the runtime's pauses 34.0 ms, compilation on the game's thread 0.0 ms: the chat's own 8.0 ms.", StringComparison.Ordinal), "said with its parts: " + string.Join("|", gc));
            var jit = frames.Add(11_000, 25, 1, 0, 12);
            Check(jit is [var compiled] && compiled.Contains("compilation on the game's thread 12.0 ms: the chat's own 13.0 ms.", StringComparison.Ordinal), "a compilation on the game's thread");
            var own = frames.Add(12_000, 30, 20, 2, 1);
            Check(own is [var chat] && chat.Contains("the chat's own 27.0 ms.", StringComparison.Ordinal), "a frame over by the chat's own time");
            Check(frames.Add(13_000, 5, 0, 0, 0).Count == 0 && !frames.Done, "a frame under the budget: nothing");
            var end = frames.Add(60_000, 1, 0);
            Check(end is [var minute] && minute.Contains("4 frames, the longest 42.0 ms, 3 of 16 ms or more, 1 by the chat's own time (the runtime's pauses and the compilations left out).", StringComparison.Ordinal), "the minute's line: " + string.Join("|", end));
            Check(frames.Done && frames.Add(61_000, 99, 0, 0, 0).Count == 0, "done: nothing more");
        }),
        ("Chat load frames 1779: wiring: the window takes the runtime's pauses and its thread's compilations around the frame, and stops timing once the minute is done", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("if(chatLoadFrames.Done){DrawChatWindowTabsAndLines();return;}", StringComparison.Ordinal), "nothing timed after the minute");
            Check(window.Contains("var paused=GC.GetTotalPauseDuration();", StringComparison.Ordinal) && window.Contains("var compiled=System.Runtime.JitInfo.GetCompilationTime(true);", StringComparison.Ordinal), "taken before the frame");
            Check(window.Contains("(GC.GetTotalPauseDuration()-paused).TotalMilliseconds,(System.Runtime.JitInfo.GetCompilationTime(true)-compiled).TotalMilliseconds))log.Information(\"{0}\",line);", StringComparison.Ordinal), "and after it, given to the minute's measure");
        }),
    ];
}
