using TenshoAssistant.Caching;
using TenshoAssistant.Chat;

// 0.1466 (X-464): the minor findings of the review of 0.1455 to 0.1461. M1: a background build still running after Stop's wait is said
// (Stop false, logged), not claimed stopped. M2: the Battle and Event tabs count no unread line (new or saved in 0.1458). M3: the General tab
// saved before 0.1458 leaves the NPC dialogue to Event. M4: the combat lines have their own limit: they no longer push the spoken lines out.
// X-574 (0.1793, the user's choice of 17/11): the one-time corrections of tabs saved before 0.1466 (M2 on saved tabs, M3) are gone: tabs saved
// stay as they are.
internal static class ChatReview1466Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static List<ChatTabSettings> Tabs(ChatSettings settings) => settings.EnsureTabs("General", "Battle", "Event", "Social");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1466 M2: new Battle and Event tabs count no unread line; General and Social do", () =>
        {
            var tabs = Tabs(new ChatSettings());
            Check(tabs[0].ShowUnread && !tabs[1].ShowUnread && !tabs[2].ShowUnread && tabs[3].ShowUnread, string.Join(",", tabs.Select(tab => tab.ShowUnread)));
        }),
        ("Chat review 1466 M2, M3: tabs saved are kept as they are, the player's own tab and unread counts included (X-574: no correction)", () =>
        {
            var oldGeneral = ChatChannels.All.Where(channel => channel != ChatChannel.Combat).ToList();
            var mine = new List<ChatChannel> { ChatChannel.Say, ChatChannel.Npc };
            var settings = new ChatSettings
            {
                Tabs = [new() { Name = "General", Channels = [.. oldGeneral] }, new() { Name = "Battle", Channels = [ChatChannel.Combat] },
                        new() { Name = "Event", Channels = [ChatChannel.Npc] }, new() { Name = "Mine", Channels = [.. mine] }],
            };
            var tabs = Tabs(settings);
            Check(ReferenceEquals(tabs, settings.Tabs) && tabs.Count == 4 && tabs.All(tab => tab.ShowUnread), "the saved tabs, unread counts as saved");
            Check(tabs[0].Shows(ChatChannel.Npc) && tabs[0].Channels.Count == oldGeneral.Count, "the saved General as it was");
            Check(tabs[3].Channels.SequenceEqual(mine), "the player's own tab unchanged");
            tabs[0].Channels.Remove(ChatChannel.Npc); tabs[1].ShowUnread = false;   // the player's choice afterwards stays
            Tabs(settings);
            Check(!tabs[0].Shows(ChatChannel.Npc) && !tabs[1].ShowUnread, "a choice made afterwards is kept");
        }),
        ("Chat review 1466 M3: without an Event tab, the old General keeps the NPC dialogue (nowhere else to see it)", () =>
        {
            var oldGeneral = ChatChannels.All.Where(channel => channel != ChatChannel.Combat).ToList();
            var settings = new ChatSettings { Tabs = [new() { Name = "General", Channels = [.. oldGeneral] }] };
            Check(Tabs(settings)[0].Shows(ChatChannel.Npc), "kept");
        }),
        ("Chat review 1466 M3: tabs saved before 0.1458 stay as they are (X-574: no Battle nor Event added, the old General keeps the dialogue)", () =>
        {
            var oldGeneral = ChatChannels.All.Where(channel => channel != ChatChannel.Combat).ToList();
            var tabs = Tabs(new ChatSettings { Tabs = [new() { Name = "General", Channels = [.. oldGeneral] }] });
            Check(tabs.Count == 1 && tabs[0].Shows(ChatChannel.Npc), string.Join(" | ", tabs.Select(tab => tab.Name)));
        }),
        ("Chat review 1466 M4: an alliance raid's combat lines no longer push the spoken lines out (each its own limit)", () =>
        {
            var store = new ChatStore();
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < 50; index++) store.Add(now, 10, "Ann", [new ChatSegment("hello " + index)], 100);
            var combatKind = Enumerable.Range(1, 5000).First(kind => ChatChannels.From(kind) == ChatChannel.Combat);
            for (var index = 0; index < 500; index++) store.Add(now, combatKind, "", [new ChatSegment("blow " + index)], 100);
            var all = store.After(0);
            Check(all.Count(record => record.Channel == ChatChannel.Say) == 50, "the 50 spoken lines kept");
            Check(all.Count(record => record.Channel == ChatChannel.Combat) == 100, "the last 100 combat lines");
            Check(all.Where(record => record.Channel == ChatChannel.Combat).First().Body[0].Text == "blow 400", "the oldest combat lines went");
            var tab = new ChatTabSettings { Name = "General", Channels = [ChatChannel.Say] };
            var view = new ChatTabView();
            view.Refresh(store, tab, 100);
            Check(view.Count == 50, "General still has its 50 lines");
            var lastSay = all.Last(record => record.Channel == ChatChannel.Say).Id;
            Check(store.After(lastSay).Count == 100 && store.After(all[^1].Id).Count == 0 && store.After(lastSay + 1).Count == 100, "the lines after a number, with gaps");
            for (var index = 0; index < 120; index++) store.Add(now, 10, "Ann", [new ChatSegment("again " + index)], 100);
            Check(store.After(0).Count(record => record.Channel == ChatChannel.Say) == 100 && store.Count == 200, "each kind at its limit");
        }),
        ("Chat review 1466 M1: a build still running after the wait is said, not claimed stopped", () =>
        {
            using var release = new ManualResetEventSlim(false);
            var build = new BackgroundBuild<string>(() => { release.Wait(TimeSpan.FromSeconds(10)); return "late"; }, _ => { });
            build.Start();
            Thread.Sleep(50);
            Check(!build.Stop(TimeSpan.FromMilliseconds(50)), "still running: Stop says false");
            release.Set();
            Check(build.Get() is null, "its result dropped");
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs")).Replace("\r\n", "\n");
            Check(watcher.Contains("var ended=rulesBuild.Stop(TimeSpan.FromSeconds(2));\n        if(!ended)log.Information(", StringComparison.Ordinal), "the watcher logs it");
        }),
    ];
}
