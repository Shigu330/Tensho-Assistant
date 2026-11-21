using TenshoAssistant.Chat;

// 0.1482 (X-484, the user's request of 15/11: « filters like the game's, and more »): the game's log filters (LogFilter sheet: kind, who, to
// whom, docs/evidence/0.1482-chat-log-filters.md), words kept or hidden, players hidden or put forward.
internal static class ChatFilters1482Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    // Rows of the evidence, as the sheet gives them.
    private static readonly ChatFilterRow Tell = new(3, 12, 65535, 65535, 1, 4, 1, "Tell");
    private static readonly ChatFilterRow Party = new(4, 14, 65535, 65535, 1, 5, 1, "Party");
    private static readonly ChatFilterRow Say = new(1, 10, 65535, 65535, 1, 1, 1, "Say");
    private static readonly ChatFilterRow OwnLoot = new(30, 65, 2, 65535, 2, 30, 1, "Own Loot Messages");
    private static readonly ChatFilterRow OthersLoot = new(31, 65, 65532, 65535, 2, 31, 1, "Others' Loot Messages");
    private static readonly ChatFilterRow ByYou = new(51, 41, 2, 65535, 4, 1, 2, "Damage dealt by you.");
    private static readonly ChatFilterRow OnYou = new(58, 41, 65535, 2, 4, 8, 2, "Damage you are dealt.");
    private static readonly ChatFilterRow ByOthers = new(91, 41, 16, 65535, 7, 1, 0, "Damage dealt by others.");
    private static readonly ChatFilterRow ByYourPet = new(191, 41, 256, 65535, 11, 1, 2, "Damage dealt by your pets and companions.");
    private static readonly ChatFilterRow Empty = new(22, 0, 0, 0, 0, 0, 0, "");

    private static ChatFilterTable Table() => new([OthersLoot, ByYou, Say, Tell, Party, OwnLoot, OnYou, ByOthers, ByYourPet, Empty]);

    private static ChatRecord Line(int kind, byte source = 0, byte target = 0, string sender = "", string text = "x")
        => new(1, DateTimeOffset.UnixEpoch, kind, ChatChannels.From(kind), sender, [new ChatSegment(text)], source, target);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat filters 1482: a filter matches the kind, who does it and to whom, as the game's masks say", () =>
        {
            // XivChatRelationKind: 1 you, 2 party, 4 other player, 5 engaged enemy, 8 your pet.
            Check(ChatFilterTable.Matches(ByYou, 41, 1, 5) && !ChatFilterTable.Matches(ByYou, 41, 2, 5), "dealt by you, not by a party member");
            Check(ChatFilterTable.Matches(OnYou, 41, 5, 1) && !ChatFilterTable.Matches(OnYou, 41, 1, 5), "dealt to you");
            Check(ChatFilterTable.Matches(ByOthers, 41, 4, 6) && ChatFilterTable.Matches(ByYourPet, 41, 8, 5), "others, your pet");
            Check(!ChatFilterTable.Matches(ByYou, 42, 1, 5), "another kind");
            Check(ChatFilterTable.Matches(OwnLoot, 65, 1, 0) && !ChatFilterTable.Matches(OthersLoot, 65, 1, 0) && ChatFilterTable.Matches(OthersLoot, 65, 4, 0), "own and others' loot (65532)");
            Check(ChatFilterTable.Matches(Say, 10, 0, 0) && ChatFilterTable.Matches(Tell, 13, 0, 0) && ChatFilterTable.Matches(Party, 32, 0, 0), "anyone; tells both ways; cross-world party with party");
            Check(!ChatFilterTable.Matches(ByYou, 41, 200, 5), "a relation out of range matches nothing");
        }),
        ("Chat filters 1482: the table keeps the game's order, drops empty rows; presets as the game's tabs; from channels", () =>
        {
            var table = Table();
            Check(table.Rows.Count == 9 && table.Rows[0].Id == 1 && table.Rows[^1].Id == 191, string.Join(",", table.Rows.Select(row => row.Id)));
            Check(table.Preset(ChatFilterTable.BattleTab).OrderBy(id => id).SequenceEqual(new uint[] { 51, 58, 191 }), "Battle's defaults");
            Check(!table.Preset(ChatFilterTable.GeneralTab).Contains(91u) && !table.Preset(ChatFilterTable.BattleTab).Contains(91u), "others' damage in no tab");
            Check(table.FromChannels([ChatChannel.Say, ChatChannel.Loot]).OrderBy(id => id).SequenceEqual(new uint[] { 1, 30, 31 }), "from channels");
        }),
        ("Chat filters 1482: a tab of filters shows what one chosen filter covers; « other messages » for what none covers", () =>
        {
            var table = Table();
            var tab = new ChatTabSettings { Filters = [51], Unlisted = false };
            var filter = new ChatTabFilter(tab, table, null);
            Check(filter.Shows(Line(41, 1, 5)) && !filter.Shows(Line(41, 2, 5)) && !filter.Shows(Line(10)), "only damage dealt by you");
            Check(!filter.Shows(Line(41, 0, 0)), "a combat line without subject is covered by no chosen filter");
            Check(!filter.Shows(Line(90)), "an unlisted kind, other messages off");
            tab.Unlisted = true;
            filter = new ChatTabFilter(tab, table, null);
            Check(filter.Shows(Line(90)) && filter.Shows(Line(41, 0, 0)) && !filter.Shows(Line(41, 4, 6)), "other messages: only what no filter covers");
            var channels = new ChatTabFilter(new ChatTabSettings { Channels = [ChatChannel.Say] }, table, null);
            Check(channels.Shows(Line(10)) && !channels.Shows(Line(41, 1, 5)), "no filters: the channels, as before");
            var unread = new ChatTabFilter(new ChatTabSettings { Filters = [51], Channels = [ChatChannel.Say] }, null, null);
            Check(unread.Shows(Line(10)) && !unread.Shows(Line(41, 1, 5)), "the game's filters not read: the channels");
        }),
        ("Chat filters 1482: words kept or hidden, capitals or not; players hidden from every tab or put forward", () =>
        {
            Check(ChatTextFilter.Words(" raid , , Hunt ").SequenceEqual(new[] { "raid", "Hunt" }) && ChatTextFilter.Words(null).Count == 0, "a list with commas");
            var tab = new ChatTabSettings { Channels = [ChatChannel.Say], KeepWords = "raid", HideWords = "wts" };
            var filter = new ChatTabFilter(tab, null, null);
            Check(filter.Shows(Line(10, text: "Raid tonight")) && !filter.Shows(Line(10, text: "hello")) && !filter.Shows(Line(10, text: "RAID WTS")), "keep, hide, any capitals");
            tab.WordsMatchCase = true;
            Check(!new ChatTabFilter(tab, null, null).Shows(Line(10, text: "Raid tonight")) && new ChatTabFilter(tab, null, null).Shows(Line(10, text: "raid tonight")), "capitals count");
            var settings = new ChatSettings { HiddenPlayers = "ann doe" };
            var hidden = new ChatTabFilter(new ChatTabSettings { Channels = [ChatChannel.Say] }, null, settings);
            Check(!hidden.Shows(Line(10, sender: "Ann Doe@Tonberry")) && hidden.Shows(Line(10, sender: "Bob Roe@Tonberry")), "a hidden player, whatever the world");
            Check(ChatTextFilter.IsPlayer("Ann Doe", ["ANN DOE"]) && !ChatTextFilter.IsPlayer("Ann Doer", ["Ann Doe"]) && !ChatTextFilter.IsPlayer("", ["Ann"]), "a whole name");
            var store = new ChatStore();
            var record = store.Add(DateTimeOffset.UnixEpoch, 41, "", [new ChatSegment("hit")], 100, 1, 5);
            Check(record.Source == 1 && record.Target == 5, "the store keeps who and to whom");
            var view = new ChatTabView();
            Check(view.Refresh(store, new ChatTabSettings { Filters = [51] }, 100, Table(), null) && view.Count == 1, "the view uses the filters");
        }),
        ("Chat filters 1482: wiring (reader, plugin, window); a change of filters builds the tab again", () =>
        {
            Check(Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs").Contains("(byte)message.SourceKind,sender,ownName?.Invoke());", StringComparison.Ordinal) && Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs").Contains("current.BoundedMaxLines,source,(byte)message.TargetKind,job);", StringComparison.Ordinal), "the reader keeps who and to whom");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("data.GetExcelSheet<Lumina.Excel.Sheets.LogFilter>()", StringComparison.Ordinal), "the game's sheet, client language");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("Refresh(ChatMessages,tab,options.BoundedMaxLines,table,options)", StringComparison.Ordinal), "views refreshed with the filters");
            Check(window.Contains("private void ChatTabChanged(ChatTabSettings tab){ChatMessages.Restart(tab);chatSettingsPending=true;}", StringComparison.Ordinal), "a change restarts the tab");
            Check(window.Contains("DrawChatPlayers(options);", StringComparison.Ordinal) && window.Contains("ChatTextFilter.IsPlayer(record.Sender,chatHighlighted) ? Warning", StringComparison.Ordinal), "players");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1482-chat-log-filters.md")), "evidence (R06)");
        }),
    ];
}
