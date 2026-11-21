using TenshoAssistant.Chat;
using TenshoAssistant.Npcs;

// X-757 (0.1740, review of 0.1738, minor findings M2 to M4): the vendors on no map counted as vendors; the chat put back on its history never
// brings a faded window back; the developer's line of a fade says the tab and the alpha.
internal static class ReviewMinors1740Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000);
    private static ChatRecord Add(ChatStore store, int seconds, int kind, string sender, string text) => store.Add(Start.AddSeconds(seconds), kind, sender, [new ChatSegment(text)], 500, 0);
    private static ChatRecord Old(int seconds, int kind, string sender, string text) => new(0, Start.AddSeconds(seconds), kind, ChatChannels.From(kind), sender, [new ChatSegment(text)]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1740: M2: behaviour: a vendor of two shops counts once", () =>
        {
            (uint Npc, string Shop)[] offers = [(7, "furnishings"), (7, "dyes"), (9, "furnishings")];
            Check(NpcVendorRows.Vendors(offers, offer => offer.Npc) == 2, "two vendors, three offers");
            Check(NpcVendorRows.Vendors(Array.Empty<(uint, string)>(), offer => offer.Item1) == 0, "none");
            var page = Read("MainWindow.NpcVendors.cs");
            Check(page.Contains("NpcVendorRows.Vendors(unplaced,offer=>offer.Vendor.NpcId))+\"##Unplaced\"", StringComparison.Ordinal) && !page.Contains("\"NpcUi.UnplacedClosed\",unplaced.Count)", StringComparison.Ordinal), "the row counts the vendors");
        }),
        ("Review minors 1740: M3: behaviour: faded out, the chat put back on its history (every line numbered again, old tells among them) stays faded; a new message then brings it back", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Channels = [ChatChannel.Say, ChatChannel.TellIn] };
            var settings = new ChatSettings { FadeAfter = 10, FadeBackOnTell = true };
            Add(store, 100, 10, "Ann Doe", "hello");
            var clock = new ChatFadeClock(0);
            var wakeChecked = store.LastId;
            clock.Restored(store.Restores);
            // One frame of the window's fade, as MainWindow.ChatFade does it.
            bool Frame(long at)
            {
                var wake = false;
                if (clock.Restored(store.Restores))
                {
                    clock.Renumbered(ChatFadeClock.Newest(store, tab, settings, null));
                    wakeChecked = store.LastId;
                }
                var last = store.LastId;
                wake = settings.FadeBackOnTell && last > wakeChecked && ChatFadeClock.Wakes(store.After(wakeChecked), [], null);
                wakeChecked = last;
                clock.Note(ChatFadeClock.Newest(store, tab, settings, null), wake, at);
                return clock.Alpha(1f, 10, at, false) is <= 0f;
            }
            Check(!Frame(0) && Frame(60_000), "faded out after the delay");
            var newestBefore = ChatFadeClock.Newest(store, tab, settings, null);
            var lastBefore = store.LastId;
            var put = store.Restore([Old(10, 13, "Bob Roe", "an old tell"), Old(20, 10, "Bob Roe", "an old say")], 500);
            Check(put == 2 && ChatFadeClock.Newest(store, tab, settings, null) != newestBefore, "the restore numbers the lines again: the tab's newest has another number (the review's case)");
            Check(ChatFadeClock.Wakes(store.After(lastBefore), [], null), "the old tell put back has a number above the last one looked at (the review's case)");
            Check(Frame(61_000), "put back on its history: still faded (no new message, no tell)");
            Check(Frame(70_000), "still faded later");
            Add(store, 200, 10, "Cid Poe", "a new say");
            Check(!Frame(71_000), "a new message of the tab: back");
            Check(Frame(71_000 + 30_000), "faded again");
            Add(store, 300, 13, "Cid Poe", "a new tell");
            Check(!Frame(102_000), "a new tell: back");
        }),
        ("Review minors 1740: M3 and M4: wiring: the restore looked at before the content draws; the developer's line says the tab, its newest and the alpha", () =>
        {
            var chat = Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("if(chatFadeClock.Restored(ChatMessages.Restores))", StringComparison.Ordinal)
                && chat.Contains("chatWakeChecked=ChatMessages.LastId;\n            chatFadeClock.Renumbered(chatShownNewest);", StringComparison.Ordinal), "the restore: newest and last message taken again");
            Check(chat.IndexOf("if(chatFadeClock.Restored(ChatMessages.Restores))", StringComparison.Ordinal) < chat.IndexOf("if(chatIdleOut && chatShownTab is { } shownTab)", StringComparison.Ordinal), "before the faded window's reading");
            Check(chat.Contains("log.Information(\"Chat window, faded out, back: {0}; tab « {1} », its newest message {2}; alpha {3} then {4}.\",", StringComparison.Ordinal)
                && chat.Contains("chatShownTab?.Name ?? \"\",newest,ChatFadeText(chatFadeAlpha),ChatFadeText(alpha));", StringComparison.Ordinal) && chat.Contains("chatFadeAlpha=alpha;", StringComparison.Ordinal), "M4: tab, newest, alpha before and after");
        }),
    ];
}
