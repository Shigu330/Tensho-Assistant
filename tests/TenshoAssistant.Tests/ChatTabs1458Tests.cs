using TenshoAssistant.Chat;

// 0.1458 (X-445): the default tabs of Tensho's chat window are the game's three (General, Battle, Event; LogFilter sheet) plus Social; tabs saved
// before got Battle and Event once (X-574, 0.1793: no longer; tabs saved stay as they are); combat lines are copied and shown as the game shows
// them, never read nor counted.
internal static class ChatTabs1458Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat tabs 1458: General, Battle, Event and Social by default, the combat lines and the NPC dialogue apart from General", () =>
        {
            var tabs = new ChatSettings().EnsureTabs("General", "Battle", "Event", "Social");
            Check(tabs.Select(tab => tab.Name).SequenceEqual(["General", "Battle", "Event", "Social"]), "four tabs in the game's order");
            Check(!tabs[0].Shows(ChatChannel.Combat) && !tabs[0].Shows(ChatChannel.Npc) && tabs[0].Shows(ChatChannel.Say) && tabs[0].Shows(ChatChannel.System)
                  && tabs[0].Shows(ChatChannel.Announcement) && tabs[0].Shows(ChatChannel.Loot) && tabs[0].Shows(ChatChannel.Other), "General: everything else");
            Check(tabs[1].Channels.SequenceEqual([ChatChannel.Combat]) && tabs[2].Channels.SequenceEqual([ChatChannel.Npc]), "Battle and Event");
            Check(tabs[3].Shows(ChatChannel.Party) && !tabs[3].Shows(ChatChannel.Combat), "Social");
            Check(ChatChannels.From(58) == ChatChannel.Combat && ChatChannels.From(68) == ChatChannel.Announcement && ChatChannels.From(61) == ChatChannel.Npc
                  && ChatChannels.From(41) == ChatChannel.Combat && ChatChannels.From(49) == ChatChannel.Combat, "kinds filed as the game's default tabs");
        }),
        ("Chat tabs 1458: tabs saved stay as they are, the user's own tabs included (X-574: Battle and Event no longer added to tabs saved before)", () =>
        {
            var mine = new ChatTabSettings { Name = "Mine", Channels = [ChatChannel.FreeCompany, ChatChannel.Ls1], ShowUnread = false };
            var saved = new ChatSettings
            {
                Tabs = [new() { Name = "Général", Channels = ChatChannels.All.Where(channel => channel != ChatChannel.Combat).ToList() },
                        new() { Name = "Social", Channels = ChatChannels.All.Where(ChatChannels.Spoken).ToList() }, mine],
            };
            var tabs = saved.EnsureTabs("Général", "Combat", "Événement", "Social");
            Check(tabs.Select(tab => tab.Name).SequenceEqual(["Général", "Social", "Mine"]), "nothing added: " + string.Join(", ", tabs.Select(tab => tab.Name)));
            Check(ReferenceEquals(tabs[2], mine) && mine.Channels.SequenceEqual([ChatChannel.FreeCompany, ChatChannel.Ls1]) && !mine.ShowUnread, "the user's tab untouched");
            tabs.RemoveAt(1);
            Check(saved.EnsureTabs("Général", "Combat", "Événement", "Social").Count == 2, "a tab removed by the user does not come back");
            var full = new ChatSettings { Tabs = Enumerable.Range(0, ChatSettings.MaximumTabs).Select(index => new ChatTabSettings { Name = "T" + index }).ToList() };
            Check(full.EnsureTabs("G", "B", "E", "S").Count == ChatSettings.MaximumTabs, "never past the most tabs");
        }),
        ("Chat tabs 1458: a combat line is copied as the game wrote it, shown in Battle only; nothing else reads the combat lines", () =>
        {
            var store = new ChatStore();
            var tabs = new ChatSettings().EnsureTabs("General", "Battle", "Event", "Social");
            const string hit = "You hit the Shadow Lord for 9999 damage. www.example.org";
            store.Add(DateTimeOffset.UtcNow, 41, "", ChatPieces.Segments([new ChatPiece(hit), new ChatPiece(null, End: true)]), 2000);
            var general = store.ViewOf(tabs[0]); general.Refresh(store, tabs[0], 2000);
            var battle = store.ViewOf(tabs[1]); battle.Refresh(store, tabs[1], 2000);
            Check(general.Count == 0 && battle.Count == 1, "in Battle only");
            var record = battle.RecordAt(0);
            Check(record.Channel == ChatChannel.Combat && string.Concat(record.Body.Select(segment => segment.Text)) == hit, "the text as written");
            var source = Path.GetFullPath("src/TenshoAssistant");
            var readers = Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Replace('\\', '/').Contains("/Chat/", StringComparison.Ordinal) && File.ReadAllText(file).Contains("ChatChannel.Combat", StringComparison.Ordinal)).ToList();
            Check(readers.Count == 0, "nothing outside the chat window looks at the combat lines: " + string.Join(", ", readers));
        }),
    ];
}
