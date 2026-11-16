using TenshoAssistant.Chat;

// 0.1460 (X-457): the chat window that jumps between two scroll positions says how in the log, in developer mode: a back-and-forth (layouts
// redone, rows and scroll limit changing frame after frame) gives one line a second at most; a window at rest or one new line gives none.
internal static class ChatScrollWatch1460Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat scroll watch 1460: a back-and-forth of the layout and the scroll limit is said, once a second at most", () =>
        {
            var watch = new ChatScrollWatch();
            var said = new List<string>();
            for (var frame = 0; frame < 120; frame++)
            {
                var odd = frame % 2 == 1;   // two layouts in turn: a scroll bar shown, then not
                var line = watch.Frame(new(frame / 60.0, odd ? 540 : 556, odd ? 31 : 29, odd ? 400 : 380, odd ? 400 : 380, true, true));
                if (line is not null) said.Add(line);
            }
            Check(said.Count == 2, "once a second over two seconds: " + said.Count);
            Check(said[0].StartsWith("Chat window jumps: ", StringComparison.Ordinal) && said[0].Contains("widths 556/540", StringComparison.Ordinal)
                  && said[0].Contains("rows changed", StringComparison.Ordinal) && said[0].Contains("relaid", StringComparison.Ordinal), said[0]);
        }),
        ("Chat scroll watch 1460: a window at rest, or following a new line now and then, says nothing", () =>
        {
            var watch = new ChatScrollWatch();
            var rows = 40;
            for (var frame = 0; frame < 600; frame++)
            {
                if (frame % 90 == 0) rows++;   // a new line every second and a half: the limit moves once
                Check(watch.Frame(new(frame / 60.0, 556, rows, rows * 20 - 300, rows * 20 - 300, true, frame == 0)) is null, "quiet at frame " + frame);
            }
        }),
        ("Chat scroll watch 1460: watched in developer mode only, after the lines are drawn", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            // 0.1462 (X-457): the bottom followed by ChatScroll.Bottom, no longer by SetScrollHereY.
            var follow = window.IndexOf("if(atBottom){ImGui.SetScrollY(ChatScroll.Bottom(view.RowCount,height,shown));view.MarkSeen();}", StringComparison.Ordinal);
            Check(follow > 0 && window.IndexOf("if(developerMode && chatScrollWatch.Frame(", follow, StringComparison.Ordinal) > follow, "after the bottom is followed, developer mode only");
            Check(window.Contains("var relaid=!view.LaidOutFor(width,key);", StringComparison.Ordinal), "a layout redone is known");
        }),
    ];
}
