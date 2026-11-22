using TenshoAssistant.Chat;

// X-732 (0.1736, the user's report of 21/11 around 15:20): a faded-out chat window comes back when a message arrives, its content not drawn.
internal static class ChatFade1736Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000);
    private static ChatRecord Add(ChatStore store, int seconds, int kind, string sender, string text, byte source = 0) => store.Add(Start.AddSeconds(seconds), kind, sender, [new ChatSegment(text)], 500, source);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat fade 1736: behaviour: faded out after 10 s, a new message of the tab shown, added without any drawing, brings the window back", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Channels = [ChatChannel.Say] };
            var settings = new ChatSettings { FadeAfter = 10 };
            Add(store, 1, 10, "Ann Doe", "hello");
            var clock = new ChatFadeClock(0);
            clock.Note(ChatFadeClock.Newest(store, tab, settings, null), false, 0);
            Check(clock.Alpha(1f, 10, 5_000, false) == 1f, "kept up before the delay");
            Check(clock.Alpha(1f, 10, 60_000, false) is <= 0f, "faded out a while after the delay");
            Add(store, 70, 11, "Bob Roe", "a shout, not in the tab");
            clock.Note(ChatFadeClock.Newest(store, tab, settings, null), false, 61_000);
            Check(clock.Alpha(1f, 10, 61_000, false) is <= 0f, "a message the tab does not show: still faded");
            Add(store, 71, 10, "Bob Roe", "a say, in the tab");
            clock.Note(ChatFadeClock.Newest(store, tab, settings, null), false, 62_000);
            Check(clock.Alpha(1f, 10, 62_000, false) == 1f, "the tab's new message (read from the data, nothing drawn): back");
            Check(clock.Alpha(1f, 10, 62_000 + 30_000, false) is <= 0f && clock.Alpha(1f, 10, 92_000, kept: true) == 1f, "faded again later; the mouse or a menu keeps it");
            Check(clock.Alpha(1f, 0, 1_000_000, false) == 1f, "0: never fades");
        }),
        ("Chat fade 1736: with the setting on, a tell received or an alert brings it back; never what you send, never a combat line", () =>
        {
            var store = new ChatStore();
            var said = Add(store, 1, 11, "Bob Roe", "nothing for you");
            Check(!ChatFadeClock.Wakes([said], ["dragon"], "Ann Doe"), "a shout without an alert: no");
            Check(ChatFadeClock.Wakes([Add(store, 2, 13, "Bob Roe", "hi")], [], null), "a tell received: yes");
            Check(!ChatFadeClock.Wakes([Add(store, 3, 12, "Bob Roe", "hi")], [], null), "a tell you sent: no");
            Check(ChatFadeClock.Wakes([Add(store, 4, 11, "Bob Roe", "a DRAGON here")], ["dragon"], null), "an alert word: yes");
            Check(ChatFadeClock.Wakes([Add(store, 5, 10, "Bob Roe", "hello Ann")], [], "Ann Doe"), "your first name said by a player: yes");
            Check(!ChatFadeClock.Wakes([Add(store, 6, 41, "", "dragon")], ["dragon"], null), "a combat line: never read");
            var clock = new ChatFadeClock(0);
            clock.Note(5, false, 0);
            clock.Note(5, true, 50_000);
            Check(clock.Alpha(1f, 10, 50_000, false) == 1f, "a wake with the tab's newest unchanged: back");
        }),
        ("Chat fade 1736: wiring: the fade reads the data while faded out; the setting, its tooltip and the texts", () =>
        {
            var chat = Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("if(chatIdleOut && chatShownTab is { } shownTab)", StringComparison.Ordinal)
                && chat.Contains("chatShownNewest=ChatFadeClock.Newest(ChatMessages,shownTab,options,ChatFilterTableNow());", StringComparison.Ordinal), "faded out: the newest from the data");
            Check(chat.Contains("wake=options.FadeBackOnTell && last>chatWakeChecked", StringComparison.Ordinal) && chat.Contains("else chatWakeChecked=ChatMessages.LastId;", StringComparison.Ordinal),
                "only the messages after the fade looked at, and only with the setting on");
            Check(chat.Contains("BoolSetting(\"ChatWindow.FadeBackOnTell\",\"ChatWindow.FadeBackOnTellTip\",()=>configuration.Chat.FadeBackOnTell,value=>configuration.Chat.FadeBackOnTell=value,c=>c.Chat.FadeBackOnTell);", StringComparison.Ordinal), "the setting");
            Check(!new ChatSettings().FadeBackOnTell, "off by default");
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"FadeBackOnTell\": \"Revenir aussi pour un message privé ou une alerte\"", StringComparison.Ordinal) && fr.Contains("un nouveau message de l'onglet affiché la font revenir (avec la case suivante", StringComparison.Ordinal), "fr");
            Check(en.Contains("\"FadeBackOnTell\": \"Also come back for a tell or an alert\"", StringComparison.Ordinal) && en.Contains("a new message in the tab shown brings it back (with the next box", StringComparison.Ordinal), "en");
        }),
    ];
}
