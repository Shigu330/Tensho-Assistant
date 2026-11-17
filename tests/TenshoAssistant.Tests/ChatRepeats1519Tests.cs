using TenshoAssistant.Chat;

// 0.1519 (X-484, beyond the game; the user's request of 17/11): a tab can fold a message repeated right after itself (the « duplicates » of the
// chat study), and the player put forward stands out on a band.
internal static class ChatRepeats1519Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    private static ChatTabSettings Tab(bool fold) => new() { Name = "t", Channels = [ChatChannel.Shout, ChatChannel.Say], FoldRepeats = fold };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat repeats 1519: a message repeated right after itself is folded into the first, counted; another message breaks the run", () =>
        {
            var store = new ChatStore();
            store.Add(Start, 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            store.Add(Start.AddSeconds(20), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            store.Add(Start.AddSeconds(50), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            store.Add(Start.AddSeconds(55), 10, "Tifa Lockhart", [new ChatSegment("Hello")], 100);
            store.Add(Start.AddSeconds(56), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            var folded = new ChatTabView();
            folded.Refresh(store, Tab(true), 100);
            Check(folded.Count == 3 && folded.RepeatsAt(0) == 2 && folded.RepeatsAt(1) == 0 && folded.RepeatsAt(2) == 0,
                $"folded: {folded.Count} messages, repeats {string.Join(",", Enumerable.Range(0, folded.Count).Select(folded.RepeatsAt))}");
            var plain = new ChatTabView();
            plain.Refresh(store, Tab(false), 100);
            Check(plain.Count == 5 && Enumerable.Range(0, 5).All(index => plain.RepeatsAt(index) == 0), "a tab that does not fold shows every message");
        }),
        ("Chat repeats 1519: not a repeat past a minute, from another sender, on another channel or with another text", () =>
        {
            var first = new ChatRecord(1, Start, 11, ChatChannel.Shout, "Cloud Strife", [new ChatSegment("Selling gil!")]);
            Check(ChatRepeat.Repeats(first, Start, first with { Id = 2, Time = Start.AddSeconds(60) }), "within the minute");
            Check(!ChatRepeat.Repeats(first, Start, first with { Id = 2, Time = Start.AddSeconds(61) }), "past the minute");
            Check(!ChatRepeat.Repeats(first, Start, first with { Id = 2, Sender = "Tifa Lockhart" }), "another sender");
            Check(!ChatRepeat.Repeats(first, Start, first with { Id = 2, Kind = 10, Channel = ChatChannel.Say }), "another channel");
            Check(!ChatRepeat.Repeats(first, Start, first with { Id = 2, Body = [new ChatSegment("Selling gil?")] }), "another text");
            Check(!ChatRepeat.Repeats(first, Start, first with { Id = 2, Time = Start.AddSeconds(-5) }), "an older message is not a repeat");
        }),
        ("Chat repeats 1519: a run of repeats is timed from its last one, not its first", () =>
        {
            var store = new ChatStore();
            for (var i = 0; i < 4; i++) store.Add(Start.AddSeconds(i * 50), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            var view = new ChatTabView();
            view.Refresh(store, Tab(true), 100);
            Check(view.Count == 1 && view.RepeatsAt(0) == 3, "each repeat within a minute of the one before, 150 s in all, one message ×4");
        }),
        ("Chat repeats 1519: the settings and the window use the fold and the band", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("CheckSetting(\"ChatWindow.FoldRepeats\"") && code.Contains("view.RepeatsAt(index)"), "the tab's setting and the count drawn");
            Check(code.Contains("ChatTextFilter.IsPlayer(record.Sender,chatHighlighted)") && code.Contains("ChatLook.HighlightBandAlpha"), "the band of a player put forward");
        }),
    ];
}
