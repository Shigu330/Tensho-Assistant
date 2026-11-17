using TenshoAssistant.Chat;

// 0.1532 (X-558, the user's request of 17/11): the newest lines laid out first, within a time budget; only the newest ahead of time; the older
// ones by packets above the view when one scrolls back; nothing lost nor reordered.
internal static class ChatLoading1532Tests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 11, 0, 0, TimeSpan.Zero);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly IReadOnlyList<IReadOnlyList<ChatRun>> TwoRows = [[], []];

    private static (ChatStore Store, ChatTabView View, ChatTabSettings Tab) Long(int count)
    {
        var store = new ChatStore();
        for (var index = 0; index < count; index++) store.Add(Start.AddSeconds(index), 10, "Cloud Strife", [new ChatSegment($"line {index}")], 5000);
        var tab = new ChatTabSettings { Name = "Say", Channels = [ChatChannel.Say] };
        var view = new ChatTabView();
        view.Refresh(store, tab, 5000);
        return (store, view, tab);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat loading 1532: a long history (reload): the newest first, only the newest ahead, in order, none unread", () =>
        {
            var (_, view, _) = Long(1000);
            Check(view.Count == 1000 && view.Unread == 0, "every line kept, taken as read");
            view.Relayout(300, "k");
            var order = new List<int>();
            for (var next = view.NextToLayOut(ChatLoading.Eager); next >= 0; next = view.NextToLayOut(ChatLoading.Eager)) { order.Add(next); view.SetLines(next, TwoRows); }
            Check(order.Count == ChatLoading.Eager && order[0] == 999 && order[^1] == 1000 - ChatLoading.Eager, $"the {ChatLoading.Eager} newest, newest first: {order.Count}, {order[0]}, {order[^1]}");
            Check(order.SequenceEqual(Enumerable.Range(1000 - ChatLoading.Eager, ChatLoading.Eager).Reverse()), "one after the other, none skipped");
            Check(view.LinesAt(699) is null && view.LinesAt(700) is not null, "the older ones wait");
            Check(view.RowCount == 700 + 2 * ChatLoading.Eager, $"rows: a line not laid out counts one: {view.RowCount}");
            for (var index = 0; index < view.Count; index++) if (view.RecordAt(index).Id != index + 1) throw new Exception($"order at {index}");
        }),
        ("Chat loading 1532: scrolled back, the packet above the view, nearest first; the rows added above keep the view on the same line", () =>
        {
            var (_, view, _) = Long(1000);
            view.Relayout(300, "k");
            for (var next = view.NextToLayOut(ChatLoading.Eager); next >= 0; next = view.NextToLayOut(ChatLoading.Eager)) view.SetLines(next, TwoRows);
            // The view's first line is 700 (just above the newest laid out): the packet above it is 699 down to 660.
            var first = 700;
            var before = view.RowOf(first);
            var packet = new List<int>();
            for (var older = view.OlderToLayOut(first, ChatLoading.Packet); older >= 0; older = view.OlderToLayOut(first, ChatLoading.Packet)) { packet.Add(older); view.SetLines(older, TwoRows); }
            Check(packet.Count == ChatLoading.Packet && packet[0] == 699 && packet[^1] == 700 - ChatLoading.Packet, $"the packet: {packet.Count}, {packet[0]}, {packet[^1]}");
            Check(view.LinesAt(659) is null, "nothing beyond the packet: older lines only when one scrolls back to them");
            var added = view.RowOf(first) - before;
            Check(added == ChatLoading.Packet, $"one more row each: {added}");
            Check(ChatLoading.Kept(before * 20f, added, 20f) == view.RowOf(first) * 20f, "the scroll follows the line");
            Check(view.OlderToLayOut(0, 40) == -1, "nothing above the first line");
            Check(ChatLoading.Kept(10, -5, 20) == 0, "never above the top");
        }),
        ("Chat loading 1532: the budget: under the count and the time; new lines still come at the bottom, unread", () =>
        {
            Check(ChatLoading.More(0, 0) && ChatLoading.More(99, 2.9) && !ChatLoading.More(100, 0) && !ChatLoading.More(5, ChatLoading.BudgetMs), "count and time");
            var (store, view, tab) = Long(500);
            store.Add(Start.AddSeconds(600), 10, "Tifa Lockhart", [new ChatSegment("new")], 5000);
            view.Refresh(store, tab, 5000);
            Check(view.Count == 501 && view.Unread == 1 && view.RecordAt(500).Id == 501, "the new line last, unread");
            Check(view.NextToLayOut(ChatLoading.Eager) == 500, "laid out first");
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("while(ChatLoading.More(laid,System.Diagnostics.Stopwatch.GetElapsedTime(layoutStarted).TotalMilliseconds))") && code.Contains("view.NextToLayOut(ChatLoading.Eager)")
                  && code.Contains("view.OlderToLayOut(first,ChatLoading.Packet)") && code.Contains("ImGui.SetScrollY(ChatLoading.Kept(ImGui.GetScrollY(),added,height))"), "the window uses it");
            Check(code.Contains("\"Chat layout\"") && code.Contains("\"Chat refresh\""), "measured apart in developer mode");
        }),
    ];
}
