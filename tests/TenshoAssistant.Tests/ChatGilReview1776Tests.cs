using TenshoAssistant.Chat;
using TenshoAssistant.Inventory;

// X-773 (review of 0.1773 to 0.1775, minor findings): M1 the messages come while a tab waits for its first refresh after the history is read
// back stay unread (ChatTabView.Again); M2 the character's retainers' names emptied at each change of character, the company's whole name kept
// out whole; M3 only the game's own kinds that may go with a gil change are kept in the « Other » context, at most 20 messages.
internal static class ChatGilReview1776Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 22, 4, 0, 0, TimeSpan.Zero);

    private static ChatRecord Add(ChatStore store, int second, string text, int kind = 10) => store.Add(Now.AddSeconds(second), kind, "Ann Example", [new ChatSegment(text)], 2000);

    private static ChatRecord Older(string text, int minutes) => new(0, Now.AddMinutes(-minutes), 10, ChatChannel.Say, "Ann Example", [new ChatSegment(text)]);

    private static ChatRecord Find(ChatStore store, string text) => store.After(0).Single(record => record.Body[0].Text == text);

    // Made-up names standing for the character's own (never a real one in the repository).
    private static readonly IReadOnlyList<string> Own = GilOtherContext.PersonalNames(["Lune Dorée", "Moogle", "TAG"], ["Les Chevaliers de la Lune"]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat gil review 1776: behaviour (M1): after the history is read back, a tab refreshed frames later counts the messages come meanwhile, keeps its unread count and its alerts", () =>
        {
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "Say", Channels = [ChatChannel.Say] };
            var settings = new ChatSettings { AlertWords = "gil" };
            Add(store, 0, "first");
            var view = store.ViewOf(tab);
            view.Refresh(store, tab, 2000, null, settings);
            Check(view.Unread == 0, "a new view takes what is kept as read");
            Add(store, 1, "who sells gil cheap");
            view.Refresh(store, tab, 2000, null, settings);
            Check(view.Unread == 1 && view.Alerts == 1, $"one unread, one alert: {view.Unread}, {view.Alerts}");
            Add(store, 2, "come before the read-back, not taken yet");
            Check(store.Restore([Older("yesterday", 24 * 60), Older("an hour ago", 60)], 2000) == 2, "the history put back");
            // The tab waits a few frames (ChatLoading.RefreshOrder): two messages come meanwhile.
            Add(store, 3, "after the read-back");
            Add(store, 4, "again after");
            var again = store.ViewOf(tab);
            Check(!ReferenceEquals(again, view), "the view made again");
            again.Refresh(store, tab, 2000, null, settings);
            Check(again.Count == 7, $"every line: {again.Count}");
            Check(again.Unread == 4, $"the unread one kept, the one not taken yet and the two after the read-back new (before X-773: 0): {again.Unread}");
            Check(again.Alerts == 1 && again.AlertIds.Single() == Find(store, "who sells gil cheap").Id, "the alert carried over, by the message's new number");
            again.MarkSeen();
            Add(store, 5, "later");
            again.Refresh(store, tab, 2000, null, settings);
            Check(again.Unread == 1, "then each new message as before");
        }),
        ("Chat gil review 1776: behaviour (M1): a view that had taken nothing counts from the read-back on; a tab first seen after it takes all as read; CountFrom kept", () =>
        {
            var store = new ChatStore();
            ChatTabSettings idle = new() { Name = "Idle", Channels = [ChatChannel.Say] }, later = new() { Name = "Later", Channels = [ChatChannel.Say] }, talk = new() { Name = "Talk", Channels = [ChatChannel.Say] };
            Add(store, 0, "first");
            store.ViewOf(idle);
            var tell = Add(store, 1, "the tell that opened the tab");
            store.ViewOf(talk).CountFrom(tell.Id);
            Add(store, 2, "before the read-back");
            store.Restore([Older("yesterday", 24 * 60)], 2000);
            Add(store, 3, "after the read-back");
            var view = store.ViewOf(idle);
            view.Refresh(store, idle, 2000);
            Check(view.Count == 5 && view.Unread == 1, $"made but never refreshed: only the message after the read-back is new: {view.Unread}");
            var conversation = store.ViewOf(talk);
            conversation.Refresh(store, talk, 2000);
            Check(conversation.Unread == 3, $"a conversation's tab still counts from its tell, by its new number: {conversation.Unread}");
            var fresh = store.ViewOf(later);
            fresh.Refresh(store, later, 2000);
            Check(fresh.Count == 5 && fresh.Unread == 0, "a tab with no view before: everything kept taken as read, as any new view");
        }),
        ("Chat gil review 1776: behaviour (M3, R25): only the game's own kinds are kept; another player's name in plain text in an echo or a plugin's line is left out", () =>
        {
            foreach (var kind in new[] { 57, 60, 62, 64, 71 }) Check(GilOtherContext.KindKept(kind), "the game's lines that may go with gil: " + kind);
            // 1 and 2: Dalamud's Print and PrintError; 3 notice; 56 echo; 10 to 37 players' channels; 41 and 58 combat (DEC-053); 65 rolls; 66, 67, 59
            // crafting and gathering; 69 and 70 the company's announcements; 68 NPC announcements; 101 a cross-world linkshell.
            foreach (var kind in new[] { 1, 2, 3, 56, 10, 11, 13, 14, 24, 28, 29, 37, 41, 58, 59, 65, 66, 67, 68, 69, 70, 101 })
                Check(!GilOtherContext.KindKept(kind), "left out: " + kind);
            var context = new GilOtherContext();
            Check(!context.Message(Now, 56, false, false, "Moonie Example a vendu 300 gils.", Own), "an echo naming another player in plain text: left out");
            Check(!context.Message(Now, 1, false, false, "[SomePlugin] Moonie Example: 300 gils.", Own), "another plugin's Print (no sender, no player payload): left out");
            Check(!context.Message(Now, 69, false, false, "Moonie Example a déposé 300 gils.", Own), "the company's announcement: left out");
            Check(context.Message(Now, 62, false, false, "Vous obtenez 300 gils.", Own), "the game's own line: kept");
            for (var index = 0; index < 30; index++) context.Message(Now.AddMilliseconds(index), 57, false, false, $"Line {index}.", Own);
            context.Other(Now, 300);
            var line = context.Due(Now.AddSeconds(3)).Single();
            Check(line.Split(" | ").Length == GilOtherContext.MaxMessages && GilOtherContext.MaxMessages == 20, "at most 20 messages: " + line);
            Check(!line.Contains("Moonie", StringComparison.Ordinal), "no other player's name in the line");
        }),
        ("Chat gil review 1776: behaviour (M2, R25): the company's whole name kept out whole (its common words not alone); the retainers' names emptied at each change of character", () =>
        {
            Check(Own.Contains("Les Chevaliers de la Lune") && !Own.Contains("de") && !Own.Contains("Les"), "the company's name whole only: " + string.Join(",", Own));
            Check(GilOtherContext.Safe(false, false, "Les Chevaliers de la Lune ont reçu 300 gils.", Own) is null, "a message naming the company: left out");
            Check(GilOtherContext.Safe(false, false, "Vous déposez les gils dans le coffre de la compagnie.", Own) is not null, "its common words alone leave nothing out");
            var watcher = Read("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs");
            var switched = watcher.IndexOf("private void Switch(ulong id,DateTimeOffset now)", StringComparison.Ordinal);
            Check(switched > 0 && watcher.IndexOf("retainerNames.Clear();", switched, StringComparison.Ordinal) is var cleared && cleared > switched && cleared < watcher.IndexOf("Saves.Switch();", switched, StringComparison.Ordinal), "emptied when the character changes");
            Check(watcher.Contains("GilOtherContext.PersonalNames((OwnNames?.Invoke() ?? []).Concat(retainerNames),[player.IsLoaded ? CompanyName():null])", StringComparison.Ordinal)
                && watcher.Contains("InfoProxyFreeCompany.Instance()", StringComparison.Ordinal) && watcher.Contains("game->UIModule==null", StringComparison.Ordinal), "the company's name read on the framework thread, the UI module checked first");
            Check(watcher.Contains("if(!KindKept(kind)", StringComparison.Ordinal) || Read("src/TenshoAssistant/Inventory/GilOtherContext.cs").Contains("if(!KindKept(kind) || Safe(", StringComparison.Ordinal), "the kind checked before the text");
        }),
    ];
}
