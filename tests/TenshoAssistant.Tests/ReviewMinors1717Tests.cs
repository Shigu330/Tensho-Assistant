using TenshoAssistant.Chat;

// X-717 (0.1717, review of block 0.1713 to 0.1716): M1, the players put forward or hidden are a set cut once (a lookup per line drawn, not a
// walk through up to 250 names three times per line); M2, the chat window keeps a tab in every context (a profile showing only detached tabs
// keeps the first there), a tab is not detached when it is the last the context shows there, and the settings' « Remove » never takes the
// last tab left in it; M3, the « list full » line goes when the text typed or the list changes.
internal static class ReviewMinors1717Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatTabSettings Tab(string name, int window = 0) => new() { Name = name, Window = window };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1717: M1, the set answers as the list, and is cut again only when the text changes", () =>
        {
            const string list = "Ann Doe, Bob Roe@Moogle, cid, Dee@";
            var names = ChatTextFilter.Words(list);
            var set = new ChatPlayerSet(list);
            foreach (var sender in new[] { "Ann Doe", "ann doe@Moogle", "ANN DOE@Cactuar", "Bob Roe@Moogle", "bob roe@moogle", "Bob Roe", "Bob Roe@Cactuar",
                " Cid ", "cid@Moogle", "Ann Doer", "Ann", "", " ", "@Ann Doe", "Dee@", "Dee", "Dee@World" })
                Check(set.Holds(sender) == ChatTextFilter.IsPlayer(sender, names) && ChatTextFilter.IsPlayer(sender, set) == set.Holds(sender), "the same answer as the list for « " + sender + " »");
            Check(set.Holds("ann doe@Moogle") && set.Holds("Bob Roe@Moogle") && !set.Holds("Bob Roe") && !set.Holds("Ann Doer"), "the name before the world, or the whole sender");
            Check(!set.Update(list) && !set.Update(new string(list.ToCharArray())), "the same text: not cut again");
            Check(set.Update("Eve Poe") && set.Holds("Eve Poe@Moogle") && !set.Holds("Ann Doe") && set.Count == 1, "another text: cut again");
            Check(set.Update(null) && set.Count == 0 && !set.Holds("Eve Poe") && !set.Update(""), "emptied: nobody; null and empty the same");
            var many = string.Join(", ", Enumerable.Range(0, 250).Select(i => "Player" + i.ToString("000") + " Name"));
            var big = new ChatPlayerSet(many);
            Check(many.Length <= ChatTextFilter.ListMaximum && big.Count == 250 && big.Holds("player249 name@World") && !big.Holds("Player250 Name"), "a long list: the last name found, one more not");
        }),
        ("Review minors 1717: M2, a context showing only detached tabs keeps the first in the chat window; the last it shows there stays", () =>
        {
            var tabs = new List<ChatTabSettings> { Tab("General"), Tab("Battle", 1), Tab("Event", 2) };
            var main = new List<ChatTabSettings>();
            var shown = new List<ChatTabSettings> { tabs[1], tabs[2] };
            ChatWindows.InChatWindow(shown, tabs, main);
            Check(main.Count == 1 && main[0] == tabs[1], "the first shown stays in the chat window (the review's case: an empty bar before)");
            Check(ChatWindows.Kept(shown, tabs) == tabs[1] && tabs[1].Window == 1 && ChatWindows.Detached(tabs, tabs[1]), "kept there in this context, its window number unchanged");
            Check(ChatWindows.Kept(tabs, tabs) is null && ChatWindows.Kept([], tabs) is null, "a tab left in the chat window, or nothing shown: none kept");
            ChatWindows.InChatWindow(tabs, tabs, main);
            Check(main.Count == 1 && main[0] == tabs[0], "every tab shown: as before");
            var four = new List<ChatTabSettings> { Tab("General"), Tab("Battle"), Tab("Event"), Tab("Social") };
            var context = new List<ChatTabSettings> { four[1], four[2] };
            Check(ChatWindows.Detach(four, four[1], context) && four[1].Window == 1, "two shown in the chat window: one may go");
            Check(ChatWindows.Free(four, four[2], context) == 0 && !ChatWindows.Detach(four, four[2], context) && four[2].Window == 0, "the last the context shows in the chat window stays");
            Check(ChatWindows.Free(four, four[2]) == 2 && ChatWindows.Free(four, four[3], context) == 2, "without the context, or a tab it does not show: the whole list decides, as before");
        }),
        ("Review minors 1717: M2, the settings' « Remove » never takes the last tab left in the chat window", () =>
        {
            var tabs = new List<ChatTabSettings> { Tab("General"), Tab("Battle", 1), Tab("Event", 2) };
            Check(!ChatWindows.Removable(tabs, tabs, tabs[0]), "the last left in the chat window: not removable (the review's case)");
            Check(ChatWindows.Removable(tabs, tabs, tabs[1]) && ChatWindows.Removable(tabs, tabs, tabs[2]), "a detached tab: removable");
            tabs[2].Window = 0;
            Check(ChatWindows.Removable(tabs, tabs, tabs[0]) && ChatWindows.Removable(tabs, tabs, tabs[2]), "two in the chat window: either");
            Check(!ChatWindows.Removable([tabs[0]], tabs, tabs[0]), "the only tab: never, as before");
            var settings = new List<ChatTabSettings> { Tab("General"), Tab("Battle", 1) };
            var all = settings.Append(new ChatTabSettings { Name = "Ann", Conversation = "Ann Doe@Moogle" }).ToList();
            Check(!ChatWindows.Removable(settings, all, settings[0]), "a conversation's tab in the chat window does not count (it closes)");
        }),
        ("Review minors 1717: M3, the « list full » line holds while the text and the list are those refused", () =>
        {
            const string list = "Ann Doe, Bob Roe";
            var (added, isFull) = ChatTextFilter.TryAdd(list, "Cid Long Name", 20);
            Check(added is null && isFull, "refused: full");
            var refused = ("Cid Long Name", list);
            Check(ChatTextFilter.StillRefused(refused, "Cid Long Name", list), "the same text and list: still said");
            Check(!ChatTextFilter.StillRefused(refused, "Cid Long Nam", list), "the text changed: gone (the review's case)");
            Check(!ChatTextFilter.StillRefused(refused, "Cid Long Name", ChatTextFilter.RemovePlayer(list, "Ann Doe@Moogle")), "the list emptied from a player's menu: gone (the review's case)");
            Check(ChatTextFilter.StillRefused(("x", ""), "x", null) && !ChatTextFilter.StillRefused(("x", ""), "x", "Ann"), "an empty list: null and empty the same");
        }),
        ("Review minors 1717: wiring: the set in the window and the filter, the kept tab, the detach and remove rules, the full line", () =>
        {
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("private readonly ChatPlayerSet chatHighlighted=new();", StringComparison.Ordinal) && window.Contains("        chatHighlighted.Update(options.HighlightedPlayers);", StringComparison.Ordinal)
                && !window.Contains("chatHighlighted=ChatTextFilter.Words(", StringComparison.Ordinal), "M1: the players put forward, a set updated once per frame");
            Check(Read("Chat/ChatFilters.cs").Contains("hidden=new(settings?.HiddenPlayers);", StringComparison.Ordinal), "M1: the players hidden, a set");
            var detached = Read("MainWindow.ChatDetached.cs");
            Check(detached.Contains("return ReferenceEquals(ChatWindows.Kept(ChatWindowTabs(options),all),tab) ? null:tab;", StringComparison.Ordinal), "M2: the kept tab's own window hides");
            Check(detached.Contains("var free=ChatWindows.Free(all,tab,ChatWindowTabs(ChatOptions))!=0;", StringComparison.Ordinal)
                && detached.Contains("if(!ChatWindows.Detach(ChatAllTabs(ChatOptions),tab,ChatWindowTabs(ChatOptions)))return;", StringComparison.Ordinal), "M2: detached on the context's tabs");
            Check(window.Contains("var removable=ChatWindows.Removable(tabs,ChatAllTabs(options),tab);", StringComparison.Ordinal) && window.Contains("ImGui.BeginDisabled(!removable);", StringComparison.Ordinal)
                && window.Contains("else Tip(\"ChatWindow.TabRemoveLastTip\");", StringComparison.Ordinal), "M2: « Remove » greyed with why");
            Check(Read("Localization/Languages/en.json").Contains("    \"TabRemoveLastTip\": ", StringComparison.Ordinal) && Read("Localization/Languages/fr.json").Contains("    \"TabRemoveLastTip\": ", StringComparison.Ordinal), "M2: text");
            Check(Read("UI/Components/SettingRow.cs").Contains("else listSettingFull.Remove(draftId);", StringComparison.Ordinal), "M3: the line goes once the text or the list changed");
        }),
    ];
}
