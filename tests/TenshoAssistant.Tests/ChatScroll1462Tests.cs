using TenshoAssistant.Chat;

// 0.1462 (X-457): the Tensho chat window, at the bottom, jumped between two scroll positions (0.1434 to 0.1461; measured in 0.1461: the scroll
// limit 0 then 31 and the width 704 then 720, frame after frame), and with a long history it showed an empty area with the bar at the bottom.
// A small model of Dear ImGui's scrolling, frame by frame, as ImGui does it: the limit of a frame comes from the lines' height known when the
// window begins (the frame before's measure, or the height declared); the vertical bar shows when that height is taller than the window
// (unless always kept) and takes its width from the lines; the scroll asked for is clamped at the next frame's limit; GetCursorStartPos()
// is the top of the content less the scroll, and SetCursorPosY(y) places at y from the top of the content less the scroll.
// The old way placed the lines from GetCursorStartPos(): the scroll taken off twice. The new one (ChatScroll) places them from the top of the
// content, declares the rows' height and keeps the bar: it stays put, follows new messages of one and several lines, and rests after a resize.
internal static class ChatScroll1462Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const float Row = 20f, Bar = 16f, Full = 720f;

    private sealed record Frame(float Width, float Scroll, float Limit, float FirstLineInView);

    /// <summary>Old way: top = GetCursorStartPos().Y (= −scroll), each row at SetCursorPosY(top + row·h), a one-pixel Dummy after the last row,
    /// the measure from those items, the bar from the measure of the frame before, the bottom followed by SetScrollHereY(1) (the end drawn).</summary>
    private static List<Frame> Old(int rows, float window, int frames, float startScroll = 0)
    {
        var list = new List<Frame>();
        float measured = startScroll > 0 ? rows * Row : 0, asked = startScroll;
        for (var frame = 0; frame < frames; frame++)
        {
            var width = Full - (measured > window ? Bar : 0);
            var limit = Math.Max(0, measured - window);
            var scroll = Math.Clamp(asked, 0, limit);
            var atBottom = scroll >= limit - 1;
            var top = -scroll;                          // GetCursorStartPos().Y
            var placedEnd = top + rows * Row;           // SetCursorPosY(top + rows·h): content position
            measured = placedEnd + 1;                   // the Dummy, measured from the top of the content
            var firstRow = (int)(scroll / Row);         // the first message drawn: IndexAtRow(scroll / h)
            var firstInView = top + firstRow * Row - scroll;   // where it shows in the window
            if (atBottom) asked = placedEnd + 1;        // SetScrollHereY(1)
            list.Add(new(width, scroll, limit, firstInView));
        }
        return list;
    }

    /// <summary>New way (ChatScroll): the bar always kept, the rows' height declared when the window begins, top = GetCursorPosY() (0),
    /// « at the bottom » read against the rows the frame before showed, the bottom asked for from the rows laid out in this frame.</summary>
    private static List<Frame> New(Func<int, (int Messages, int TwoLines, float Window)> at, int frames)
    {
        var list = new List<Frame>();
        float asked = 0;
        int laid = 0, seen = 0, shownRows = 0;
        float shownHeight = 0;
        for (var frame = 0; frame < frames; frame++)
        {
            var (messages, twoLines, window) = at(frame);
            var rowsAtBegin = laid + (messages - seen);   // a message just arrived counts one row until laid out
            seen = messages;
            const float width = Full - Bar;
            var limit = ChatScroll.Max(ChatScroll.Content(rowsAtBegin, Row), window);
            var scroll = Math.Clamp(asked, 0, limit);
            var atBottom = ChatScroll.AtBottom(scroll, shownRows, Row, shownHeight);
            laid = messages + twoLines;                   // the layout of this frame
            shownRows = laid; shownHeight = window;
            var firstRow = (int)(scroll / Row);
            var firstInView = 0 + firstRow * Row - scroll;
            if (atBottom) asked = ChatScroll.Bottom(laid, Row, window);
            list.Add(new(width, scroll, limit, firstInView));
        }
        return list;
    }

    private static int Changes(List<Frame> frames, int from)
    {
        var count = 0;
        for (var index = from + 1; index < frames.Count; index++)
            if (Math.Abs(frames[index].Width - frames[index - 1].Width) > 0.5f || Math.Abs(frames[index].Scroll - frames[index - 1].Scroll) > 0.5f
                || Math.Abs(frames[index].Limit - frames[index - 1].Limit) > 0.5f) count++;
        return count;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat scroll 1462: the old way goes back and forth with nothing arriving, as measured in 0.1461 (limit 0/31, two widths)", () =>
        {
            // 11 rows of 20 px in a window of 200 px: 231 px measured at scroll 0, 200 px at scroll 31
            var frames = Old(11, 200, 120);
            Check(Changes(frames, 10) >= 100, "the old way moves frame after frame: " + Changes(frames, 10));
            var tail = frames.Skip(60).ToList();
            Check(tail.Select(f => f.Limit).Distinct().Count() == 2 && tail.Select(f => f.Width).Distinct().Count() == 2,
                  "two limits and two widths in turn: " + string.Join(", ", tail.Take(4).Select(f => $"{f.Width}/{f.Scroll}/{f.Limit}")));
        }),
        ("Chat scroll 1462: the old way draws a long history far above the view (the empty window, the bar at the bottom)", () =>
        {
            var frames = Old(500, 300, 1, startScroll: 500 * Row - 300);
            Check(frames[0].FirstLineInView < -5000, "the first line drawn is far above the window: " + frames[0].FirstLineInView);
        }),
        ("Chat scroll 1462: the new way stays put at the bottom while nothing arrives, one width, no space under the last line", () =>
        {
            var frames = New(_ => (10, 1, 200), 120);
            Check(Changes(frames, 5) == 0, "nothing moves once settled: " + Changes(frames, 5));
            var last = frames[^1];
            Check(last.Width == Full - Bar && Math.Abs(last.Limit - (11 * Row - 200)) < 0.01f && Math.Abs(last.Scroll - last.Limit) < 0.01f,
                  $"the last line at the bottom, the limit the rows' height less the window: {last}");
            Check(last.FirstLineInView <= 0 && last.FirstLineInView > -Row, "the lines drawn where the window shows them: " + last.FirstLineInView);
        }),
        ("Chat scroll 1462: a long history is drawn in view at the bottom", () =>
        {
            var frames = New(_ => (500, 0, 300), 5);
            var last = frames[^1];
            Check(Math.Abs(last.Scroll - (500 * Row - 300)) < 0.01f && last.FirstLineInView <= 0 && last.FirstLineInView > -Row, $"{last}");
        }),
        ("Chat scroll 1462: new messages of one and several lines are followed, then the window rests", () =>
        {
            // a one-line message at frame 30, a two-line one (counted one row until laid out) at frame 60
            var frames = New(frame => (frame < 30 ? 10 : frame < 60 ? 11 : 12, frame < 60 ? 1 : 2, 200), 120);
            Check(Changes(frames, 70) == 0, "at rest after the last message: " + Changes(frames, 70));
            var last = frames[^1];
            Check(Math.Abs(last.Limit - (14 * Row - 200)) < 0.01f && Math.Abs(last.Scroll - last.Limit) < 0.01f, $"followed to the last line: {last}");
        }),
        ("Chat scroll 1462: after a resize the window follows the bottom and rests", () =>
        {
            var frames = New(frame => (10, 1, frame < 40 ? 200 : frame < 70 ? 140 : 180), 120);
            Check(Changes(frames, 75) == 0, "at rest after the resizes: " + Changes(frames, 75));
            var last = frames[^1];
            Check(Math.Abs(last.Scroll - (11 * Row - 180)) < 0.01f, $"at the bottom after the resizes: {last}");
        }),
        ("Chat scroll 1462: scrolled up, the window stays where the player put it when a message arrives", () =>
        {
            Check(!ChatScroll.AtBottom(100, 40, Row, 200), "40 rows, 200 px, scrolled to 100 of 600: not at the bottom");
            Check(ChatScroll.AtBottom(599.5f, 40, Row, 200), "at the bottom within a pixel");
            Check(ChatScroll.AtBottom(600, 40, Row, 200) && !ChatScroll.AtBottom(600, 42, Row, 200), "the rows shown before decide (2 rows more: scrolled up)");
        }),
        ("Chat scroll 1462: the pure rules: the lines' height is the rows', a short tab has no limit", () =>
        {
            Check(ChatScroll.Content(11, 20) == 220 && ChatScroll.Content(-1, 20) == 0, "rows times the row's height");
            Check(ChatScroll.Max(220, 200) == 20 && ChatScroll.Max(150, 200) == 0, "the limit: height less the window, never below 0");
            Check(ChatScroll.Bottom(11, 20, 200) == 20 && ChatScroll.Bottom(3, 20, 200) == 0, "the bottom: the last line at the bottom of the window");
            Check(ChatScroll.AtBottom(0, 0, 20, 200), "a first frame starts at the bottom");
        }),
        ("Chat scroll 1462: the window places the lines from the top of the content, keeps the bar and declares the lines' height", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            var declared = window.IndexOf("ImGui.SetNextWindowContentSize(new Vector2(0,ChatScroll.Content(view.RowCount,rowHeight)));", StringComparison.Ordinal);
            var child = window.IndexOf("ImGui.BeginChild(\"###TenshoChatLines\",new Vector2(0,linesHeight),false,ImGuiWindowFlags.NoNav|ImGuiWindowFlags.AlwaysVerticalScrollbar|ImGuiWindowFlags.NoScrollWithMouse);", StringComparison.Ordinal);   // 0.1467 (X-447 A4): the wheel read by the window
            Check(declared > 0 && child > declared, "declared just before the area, bar always kept");
            Check(window.Contains("var top=ImGui.GetCursorPosY();", StringComparison.Ordinal) && !window.Contains("var top=ImGui.GetCursorStartPos()", StringComparison.Ordinal),
                  "the lines placed from the top of the content (GetCursorStartPos takes the scroll off once more)");
            Check(window.Contains("var atBottom=ChatScroll.AtBottom(scroll,view.RowsShown,height,view.HeightShown);", StringComparison.Ordinal)
                  && window.Contains("view.RowsShown=view.RowCount;view.HeightShown=shown;", StringComparison.Ordinal), "« at the bottom » read against the rows shown before");
            Check(!window.Contains("SetScrollHereY(1)", StringComparison.Ordinal) && !window.Contains("ImGui.Dummy(new Vector2(1,1));", StringComparison.Ordinal),
                  "the old follow and the measured end are gone");
        }),
        ("Chat scroll 1462: a message read once gives one line in the tab (no copy of a message)", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "Combat", Channels = [.. ChatChannels.All] };
            var view = new ChatTabView();
            view.Refresh(store, tab, 100);
            store.Add(DateTimeOffset.UtcNow, 2091, "", [new ChatSegment("La roselette appelle à l'aide!")], 100);
            store.Add(DateTimeOffset.UtcNow, 2091, "", [new ChatSegment("3 ennemis se joignent au combat.")], 100);
            view.Refresh(store, tab, 100);
            view.Refresh(store, tab, 100);
            Check(view.Count == 2 && view.RowCount == 2, $"two messages, two rows: {view.Count}, {view.RowCount}");
        }),
    ];
}
