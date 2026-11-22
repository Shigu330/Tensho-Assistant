using TenshoAssistant.Chat;

// X-558 (0.1775, the user's request of 17/11; definition « no frame over 16 ms due to the chat at the load, measured in the log »): after the
// history is read back (ChatStore.Restore makes every tab's view again) the shown tab takes its messages first and the others within 3 ms a
// frame, round, nothing lost nor reordered; each chat frame of 16 ms or more in the first minute after the load is said, then the minute's line.
internal static class ChatLoad1775Tests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 3, 0, 0, TimeSpan.Zero);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat load 1775: the refresh order: the shown tab first, then the others from the next one, round, each once", () =>
        {
            Check(ChatLoading.RefreshOrder(4, 2, 0).SequenceEqual([2, 0, 1, 3]), "shown first, then from 0");
            Check(ChatLoading.RefreshOrder(4, 2, 3).SequenceEqual([2, 3, 0, 1]), "then from the next one, round");
            Check(ChatLoading.RefreshOrder(4, -1, 5).SequenceEqual([1, 2, 3, 0]), "none shown, the next one taken round (5 of 4 is 1)");
            Check(ChatLoading.RefreshOrder(3, 7, 0).SequenceEqual([0, 1, 2]), "a shown place out of the tabs: ignored");
            Check(ChatLoading.RefreshOrder(0, 0, 0).Count == 0 && ChatLoading.RefreshOrder(1, 0, 9).SequenceEqual([0]), "no tab, one tab");
            Check(ChatLoading.RefreshMore(2.9) && !ChatLoading.RefreshMore(3), "under 3 ms another tab, not at 3");
        }),
        ("Chat load 1775: behaviour: the history read back, tabs refreshed a few a frame end with every message, in order, as all at once", () =>
        {
            var store = new ChatStore();
            for (var index = 0; index < 50; index++) store.Add(Start.AddSeconds(1000 + index), index % 2 == 0 ? 10 : 11, "", [new ChatSegment($"now {index}")], 5000);
            ChatTabSettings[] tabs = [new() { Name = "Say", Channels = [ChatChannel.Say] }, new() { Name = "Shout", Channels = [ChatChannel.Shout] }, new() { Name = "Both", Channels = [ChatChannel.Say, ChatChannel.Shout] }];
            foreach (var tab in tabs) store.ViewOf(tab).Refresh(store, tab, 5000);
            var older = Enumerable.Range(0, 400).Select(index => new ChatRecord(0, Start.AddSeconds(index), index % 2 == 0 ? 10 : 11, ChatChannels.From(index % 2 == 0 ? 10 : 11), "", [new ChatSegment($"old {index}")])).ToArray();
            Check(store.Restore(older, 5000) == 400, "the history put back");
            // Two tabs a frame (the budget spent after the second), the second one shown: after enough frames, every view whole.
            var next = 0;
            for (var frame = 0; frame < 3; frame++)
            {
                var done = 0;
                foreach (var index in ChatLoading.RefreshOrder(tabs.Length, 1, next))
                {
                    store.ViewOf(tabs[index]).Refresh(store, tabs[index], 5000);
                    if (index != 1) next = index + 1;
                    if (++done == 2) break;
                }
                if (frame == 0) Check(store.ViewOf(tabs[1]).Count == 225 && store.ViewOf(tabs[2]).Count == 0, "the first frame: the shown tab whole, a tab left for later");
            }
            var reference = new ChatStore();
            foreach (var record in older.Concat(Enumerable.Range(0, 50).Select(index => new ChatRecord(0, Start.AddSeconds(1000 + index), index % 2 == 0 ? 10 : 11, ChatChannels.From(index % 2 == 0 ? 10 : 11), "", [new ChatSegment($"now {index}")]))))
                reference.Add(record.Time, record.Kind, record.Sender, record.Body, 5000);
            foreach (var tab in tabs)
            {
                var view = store.ViewOf(tab);
                var whole = new ChatTabView();
                whole.Refresh(reference, tab, 5000);
                Check(view.Count == whole.Count, $"{tab.Name}: {view.Count} lines, {whole.Count} all at once");
                for (var at = 0; at < view.Count; at++)
                    if (ChatFormatText(view.RecordAt(at)) != ChatFormatText(whole.RecordAt(at))) throw new Exception($"{tab.Name}: line {at} not in its place");
                Check(view.Unread == 0, $"{tab.Name}: taken as read");
            }
        }),
        ("Chat load 1775: behaviour: the first minute's frames: each of 16 ms or more said (8 at most), then one line with the count and the longest", () =>
        {
            var frames = new ChatLoadFrames();
            Check(frames.Add(1000, 3, 0.5).Count == 0, "a frame under 16 ms: nothing");
            var slow = frames.Add(9500, 50.3, 41.2);
            Check(slow is [var said] && said.Contains("frame of 50.3 ms 9.5 s after the load", StringComparison.Ordinal) && said.Contains("refresh 41.2 ms", StringComparison.Ordinal), "a long frame said with its refresh");
            for (var index = 0; index < 20; index++) frames.Add(10_000 + index, 20, 1);
            var end = frames.Add(60_000, 1, 0);
            Check(end is [var minute] && minute.Contains("22 frames, the longest 50.3 ms, 21 of 16 ms or more", StringComparison.Ordinal), "the minute's line: " + string.Join("|", end));
            Check(frames.Add(61_000, 99, 0).Count == 0, "nothing after the minute");
            var quiet = new ChatLoadFrames();
            quiet.Add(800, 2.5, 0.3);
            Check(quiet.Add(60_001, 1, 0) is [var none] && none.Contains("1 frames, the longest 2.5 ms, 0 of 16 ms or more", StringComparison.Ordinal), "a quiet minute: 0 over");
        }),
        ("Chat load 1775: wiring: the window refreshes in that order within the budget and writes the minute's lines", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("foreach(var index in ChatLoading.RefreshOrder(tabs.Count,shown,chatRefreshNext))", StringComparison.Ordinal) && window.Contains("if(!ChatLoading.RefreshMore(System.Diagnostics.Stopwatch.GetElapsedTime(refreshStarted).TotalMilliseconds))break;", StringComparison.Ordinal), "the order and the budget");
            Check(window.Contains("foreach(var line in chatLoadFrames.Add(WarmupSinceMade,", StringComparison.Ordinal) && window.Contains("log.Information(\"{0}\",line);", StringComparison.Ordinal), "the minute's lines in the log");
        }),
    ];

    private static string ChatFormatText(ChatRecord record) => record.Kind + ":" + string.Concat(record.Body.Select(segment => segment.Text));
}
