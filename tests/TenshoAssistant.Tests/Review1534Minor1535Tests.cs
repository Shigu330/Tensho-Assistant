using TenshoAssistant.Chat;
using TenshoAssistant.Diagnostics;

// 0.1535 (X-563, review of block 0.1527 to 0.1534): M1, the chat lines stay in place in the frame a packet of older lines is laid out above;
// M2, the early phase of the warm-up is measured and written in the log; M3, the beast panel's fight is said to be still confirmed in game.
internal static class Review1534Minor1535Tests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 11, 0, 0, TimeSpan.Zero);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly IReadOnlyList<IReadOnlyList<ChatRun>> TwoRows = [[], []];

    internal static class Early1535
    {
        public static int A() => 1;
        public static int B() => 2;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review 1534 minor 1535: M1, behaviour: in the frame a packet is laid out above, every line in view keeps its place on the screen", () =>
        {
            var store = new ChatStore();
            for (var index = 0; index < 1000; index++) store.Add(Start.AddSeconds(index), 10, "Cloud Strife", [new ChatSegment($"line {index}")], 5000);
            var view = new ChatTabView();
            view.Refresh(store, new ChatTabSettings { Name = "Say", Channels = [ChatChannel.Say] }, 5000);
            view.Relayout(300, "k");
            for (var lay = view.NextToLayOut(ChatLoading.Eager); lay >= 0; lay = view.NextToLayOut(ChatLoading.Eager)) view.SetLines(lay, TwoRows);
            const float height = 20f, top = 4f;
            var first = 700;
            var read = view.RowOf(first) * height + 7f; // ImGui's scroll this frame, part of a line in
            var before = Enumerable.Range(first, 30).Select(index => top + view.RowOf(index) * height - read).ToArray();
            var firstRow = view.RowOf(first);
            for (var older = view.OlderToLayOut(first, ChatLoading.Packet); older >= 0; older = view.OlderToLayOut(first, ChatLoading.Packet)) view.SetLines(older, TwoRows);
            var added = view.RowOf(first) - firstRow;
            var shift = ChatLoading.Shift(read, added, height);
            Check(added == ChatLoading.Packet && shift == added * height, $"the packet's rows: {added}, {shift}");
            // The frame draws at top - shift with ImGui's scroll unchanged (read); the next frame at the kept scroll, without the shift.
            var now = Enumerable.Range(first, 30).Select(index => top - shift + view.RowOf(index) * height - read).ToArray();
            var next = Enumerable.Range(first, 30).Select(index => top + view.RowOf(index) * height - ChatLoading.Kept(read, added, height)).ToArray();
            Check(before.SequenceEqual(now) && now.SequenceEqual(next), "no jump: the same place before, in that frame and after");
            Check(view.IndexAtRow((int)((read + shift) / height)) == first, "the first line in view is still the same");
            Check(ChatLoading.Shift(10, -5, 20) == -10 && ChatLoading.Shift(100, 0, 20) == 0, "never above the top; nothing added, nothing moved");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("shift=ChatLoading.Shift(ImGui.GetScrollY(),added,height);") && window.Contains("var drawn=scroll+shift;") && window.Contains("top-=shift;")
                  && window.Contains("var end=drawn+shown;"), "the window draws the lines at the next frame's scroll");
            Check(window.Contains("var atBottom=ChatScroll.AtBottom(scroll,view.RowsShown,height,view.HeightShown);") && window.Contains("var button=atBottom ? null:ChatBottomButton(view,scroll,shown);"),
                "« at the bottom » and the button keep ImGui's scroll");
        }),
        ("Review 1534 minor 1535: M2, behaviour: the early phase's end is measured, also when every method is early; the log says it", () =>
        {
            using (var warmup = JitWarmup.Start(() => [typeof(Early1535)], TimeSpan.Zero, early: method => method.Name == "A"))
            {
                Check(warmup.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
                Check(warmup.EarlyReadyAfter >= 0 && warmup.EarlyPrepared >= 1 && warmup.EarlyPrepared < warmup.Prepared, $"measured when the early part ends ({warmup.EarlyReadyAfter}, {warmup.EarlyPrepared}/{warmup.Prepared})");
            }
            using (var all = JitWarmup.Start(() => [typeof(Early1535)], TimeSpan.Zero, early: _ => true))
            {
                Check(all.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
                Check(all.EarlyReadyAfter >= 0 && all.EarlyPrepared == all.Prepared, $"every method early: measured at the end ({all.EarlyReadyAfter}, {all.EarlyPrepared}/{all.Prepared})");
            }
            using (var none = JitWarmup.Start(() => [typeof(Early1535)], TimeSpan.Zero))
            {
                Check(none.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
                Check(none.EarlyReadyAfter < 0, "no early phase: nothing measured");
            }
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("ready {done.EarlyReadyAfter:F0} ms after the load."), "written in the warm-up's log line");
        }),
        ("Review 1534 minor 1535: M3, the fight shown by the game's board window is said to be still confirmed in game, in both languages", () =>
        {
            static string Value(string language)
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                var at = text.IndexOf("\"FightNow\": \"", StringComparison.Ordinal);
                Check(at >= 0, "FightNow in " + language);
                var start = at + "\"FightNow\": \"".Length;
                return text[start..text.IndexOf('"', start)];
            }
            Check(Value("en").Contains("to be confirmed in game", StringComparison.Ordinal), "English: " + Value("en"));
            Check(Value("fr").Contains("à confirmer en jeu", StringComparison.Ordinal), "French: " + Value("fr"));
        }),
    ];
}
