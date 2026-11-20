using TenshoAssistant.Chat;

// 0.1520 (X-484, beyond the game; the « alerts » of the chat study, X-447): alert words make a message stand out and mark its tab until seen.
internal static class ChatAlerts1520Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start = new(2026, 11, 17, 11, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat alerts 1520: a message holding an alert word, capitals ignored, in its text and not its sender's name; never your own tells", () =>
        {
            var words = ChatTextFilter.Words("savage, Raid ");
            var said = new ChatRecord(1, Start, 11, ChatChannel.Shout, "Cloud Strife", [new ChatSegment("LF2M for a "), new ChatSegment("RAID tonight")]);
            Check(ChatAlert.Holds(said, words), "a word cut across two pieces of the message, capitals ignored");
            Check(!ChatAlert.Holds(said with { Sender = "Savage Cloud", Body = [new ChatSegment("hello")] }, words), "the sender's name does not alert");
            Check(!ChatAlert.Holds(said with { Kind = 12, Channel = ChatChannel.TellOut }, words), "what you send yourself never alerts");
            Check(!ChatAlert.Holds(said, ChatTextFilter.Words("")), "no word, no alert");
        }),
        ("Chat alerts 1520: a tab counts its alerts not seen, marks its name with « ! », and showing it clears them", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "Shout", Channels = [ChatChannel.Shout] };
            var settings = new ChatSettings { AlertWords = "raid" };
            var view = new ChatTabView();
            store.Add(Start, 11, "Cloud Strife", [new ChatSegment("raid before the view")], 100);
            view.Refresh(store, tab, 100, null, settings);
            Check(view.Alerts == 0, "the lines already kept when the view is made are taken as read");
            store.Add(Start.AddSeconds(1), 11, "Cloud Strife", [new ChatSegment("Raid at 9")], 100);
            store.Add(Start.AddSeconds(2), 11, "Tifa Lockhart", [new ChatSegment("hello")], 100);
            view.Refresh(store, tab, 100, null, settings);
            Check(view.Alerts == 1 && view.Unread == 2, $"one alert among two new lines: {view.Alerts}, {view.Unread}");
            Check(ChatAlert.Mark("Shout (2)", view.Alerts) == "! Shout (2)" && ChatAlert.Mark("Shout", 0) == "Shout", "the tab's mark");
            view.MarkSeen();
            Check(view.Alerts == 0 && view.Unread == 0, "seen: cleared");
        }),
        ("Chat alerts 1520: the settings and the window use the alert words", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("ListSetting(\"ChatWindow.AlertWords\"") && code.Contains("chatAlertWords=options.AlertWords;")
                  && code.Contains("view.AlertAt(index,chatAlertWords)") && code.Contains("if(put || alert)") && code.Contains("ChatAlert.Mark(ChatFormat.TabLabel("), "setting, band and tab mark");
        }),
    ];
}
