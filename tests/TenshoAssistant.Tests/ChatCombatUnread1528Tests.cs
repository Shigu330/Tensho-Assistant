using TenshoAssistant.Chat;

// 0.1528 (X-559, DEC-053: combat lines « never read nor counted »; X-562 M1): a combat line never enters a tab's unread count, so a tab that
// shows only combat stays at zero. X-560 (the user's report of 17/11): the chat settings' grids of boxes take the columns the width holds.
internal static class ChatCombatUnread1528Tests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat combat unread 1528: a tab showing only combat stays at zero, whatever comes; the « new below » count too", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "Battle", Channels = [ChatChannel.Combat], ShowUnread = true };
            var view = new ChatTabView();
            view.Refresh(store, tab, 100);
            foreach (var kind in new[] { 41, 42, 45, 49, 58 })
                store.Add(Start.AddSeconds(kind), kind, "", [new ChatSegment("The Shadow Lord hits you for 9999 damage.")], 100, 32, 2);
            view.Refresh(store, tab, 100);
            Check(view.Count == 5, $"the combat lines are shown: {view.Count}");
            Check(view.Unread == 0 && view.Alerts == 0, $"never counted: {view.Unread}, {view.Alerts}");
            Check(ChatFormat.TabLabel(tab.Name, tab.ShowUnread, view.Unread) == "Battle", "no number on the tab");
        }),
        ("Chat combat unread 1528: in a mixed tab only the other lines count; combat is the only channel left out besides what you send", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "All", Channels = [.. ChatChannels.All] };
            var view = new ChatTabView();
            view.Refresh(store, tab, 100);
            store.Add(Start, 41, "", [new ChatSegment("You use Fire.")], 100);
            store.Add(Start.AddSeconds(1), 10, "Cloud Strife", [new ChatSegment("hello")], 100);
            store.Add(Start.AddSeconds(2), 58, "", [new ChatSegment("You defeat the goblin.")], 100);
            store.Add(Start.AddSeconds(3), 57, "", [new ChatSegment("A system line.")], 100);
            view.Refresh(store, tab, 100);
            Check(view.Unread == 2, $"the spoken and the system lines: {view.Unread}");
            var left = ChatChannels.All.Where(channel => !ChatChannels.CountsAsUnread(channel)).ToArray();
            Check(left.Length == 2 && left.Contains(ChatChannel.Combat) && left.Contains(ChatChannel.TellOut), string.Join(",", left));
        }),
        ("Chat combat unread 1528: the grid of boxes takes the columns the width holds, never fewer than one nor more than the boxes", () =>
        {
            Check(ChatFormat.GridColumns(600, 190, 37) == 3, "600 wide, boxes of 190: three");
            Check(ChatFormat.GridColumns(560, 190, 37) == 2, "560 wide (a scroll bar taken off): two, the third no longer under the edge");
            Check(ChatFormat.GridColumns(150, 190, 37) == 1, "narrower than one box: one column (the name wrapped beside it)");
            Check(ChatFormat.GridColumns(4000, 190, 5) == 5 && ChatFormat.GridColumns(500, 100, 1) == 1 && ChatFormat.GridColumns(500, 100, 0) == 1, "at most one per box");
            Check(ChatFormat.GridColumns(float.NaN, 190, 10) == 1 && ChatFormat.GridColumns(600, 0, 10) == 1, "a width not known: one");
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(!code.Contains("BeginDataTable(\"###ChatTabChannelTable\",3)"), "no fixed three columns any more");
            Check(code.Contains("DrawChatCheckGrid(\"###ChatTabChannelTable\"") && code.Contains("DrawChatCheckGrid(\"###ChatFilterRows\"+id") && code.Contains("DrawChatCheckGrid(\"###ChatFilterGenres\""),
                "the channels, the game's filters and the kinds of combat line use the same grid");
            Check(code.Contains("ChatFormat.GridColumns(ImGui.GetContentRegionAvail().X,"), "the columns come from the width left (the scroll bar outside it)");
            Check(code.Contains("ImGui.SameLine();ImGui.TextWrapped(label);"), "a name wider than the window is wrapped, not cut");
        }),
    ];
}
