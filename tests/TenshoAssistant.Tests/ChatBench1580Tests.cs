using TenshoAssistant.Chat;

// 0.1580 (X-633 to X-638, findings of the reviewer bench X-631 on the reviews of 0.1441 and 0.1520): the chat window's folded repeats count,
// the alert found once per message, an unreadable link ends the open one, a link acts on release without drag, tooltips made on hover, HQ said.
internal static class ChatBench1580Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start = new(2026, 11, 20, 7, 0, 0, TimeSpan.Zero);

    private static ChatTabSettings Tab(bool fold) => new() { Name = "t", Channels = [ChatChannel.Shout, ChatChannel.Say], FoldRepeats = fold };

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat bench 1580 X-633: a folded repeat counts as unread and as an alert, like a new message", () =>
        {
            var store = new ChatStore();
            var settings = new ChatSettings { AlertWords = "gil" };
            store.Add(Start, 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            var view = new ChatTabView();
            view.Refresh(store, Tab(true), 100, null, settings);
            Check(view.Unread == 0 && view.Alerts == 0, "the lines kept before the view are read");
            store.Add(Start.AddSeconds(5), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            view.Refresh(store, Tab(true), 100, null, settings);
            Check(view.Count == 1 && view.RepeatsAt(0) == 1, "folded into the first");
            Check(view.Unread == 1 && view.Alerts == 1, $"the repeat counts: unread {view.Unread}, alerts {view.Alerts}");
            view.MarkSeen();
            store.Add(Start.AddSeconds(10), 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            view.Refresh(store, Tab(true), 100, null, settings);
            Check(view.Unread == 1 && view.Alerts == 1 && view.RepeatsAt(0) == 2, "a tab already seen gets its « ! » back on a word said again");
        }),
        ("Chat bench 1580 X-634: the alert of a message is found once per message, again when the words change; Genres made once", () =>
        {
            var store = new ChatStore();
            store.Add(Start, 11, "Cloud Strife", [new ChatSegment("Selling gil!")], 100);
            store.Add(Start.AddSeconds(1), 10, "Tifa Lockhart", [new ChatSegment("Hello")], 100);
            var view = new ChatTabView();
            view.Refresh(store, Tab(false), 100);
            Check(view.AlertAt(0, "gil") && !view.AlertAt(1, "gil"), "gil: the first only");
            Check(!view.AlertAt(0, "hello") && view.AlertAt(1, "HELLO"), "other words: looked again (capitals ignored)");
            Check(!view.AlertAt(0, null) && !view.AlertAt(1, ""), "no words: no alert");
            var table = new ChatFilterTable([new ChatFilterRow(1, 41, 2, ChatFilterTable.Anyone, 4, 1, 0, "Damage")]);
            Check(ReferenceEquals(table.Genres(), table.Genres()), "the same list at each frame");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("view.AlertAt(index,chatAlertWords)") && !window.Contains("ChatAlert.Holds("), "the drawing asks the view, never looks itself");
        }),
        ("Chat bench 1580 X-635: an unreadable map link ends the link still open: its text stays plain", () =>
        {
            var item = new ChatLink(ChatSegmentKind.ItemLink, ItemId: 5);
            var segments = ChatPayloads.Segments([
                new ChatPayload(ChatPayloadKind.Item, "Item", Link: item),
                new ChatPayload(ChatPayloadKind.Text, "Text", Text: "Potion"),
                new ChatPayload(ChatPayloadKind.Map, "MapLink", Link: null),
                new ChatPayload(ChatPayloadKind.Text, "Text", Text: "Limsa (10, 11)"),
                new ChatPayload(ChatPayloadKind.LinkEnd, "LinkEnd"),
                new ChatPayload(ChatPayloadKind.Text, "Text", Text: " after"),
            ]);
            var potion = segments.Single(segment => segment.Text == "Potion");
            var place = segments.Single(segment => segment.Text == "Limsa (10, 11)");
            Check(potion.Link == item, "the item link keeps its text");
            Check(place.Link is null && place.Kind == ChatSegmentKind.Text, "the unreadable link's text is plain, not the item's");
            Check(segments.Single(segment => segment.Text == " after").Link is null, "the text after is plain");
        }),
        ("Chat bench 1580 X-636: a link acts on the release over it, pressed on it, without a drag", () =>
        {
            Check(ChatLinkPress.Acts("1_0_2", "1_0_2", hovered: true, dragged: false), "a click on the link");
            Check(!ChatLinkPress.Acts("1_0_2", "1_0_2", hovered: true, dragged: true), "a drag of the window from the link");
            Check(!ChatLinkPress.Acts("1_0_3", "1_0_2", hovered: true, dragged: false), "pressed on another link");
            Check(!ChatLinkPress.Acts(null, "1_0_2", hovered: true, dragged: false), "pressed elsewhere, released over the link");
            Check(!ChatLinkPress.Acts("1_0_2", "1_0_2", hovered: false, dragged: false), "released away from the link");
            Check(!ChatLinkPress.Dragged(new(100, 100), new(103, 104)) && ChatLinkPress.Dragged(new(100, 100), new(100, 106)), "drag past six pixels");
            var window = Read("MainWindow.ChatWindow.cs");
            var link = window[window.IndexOf("private void DrawChatLink(", StringComparison.Ordinal)..];
            link = link[..link.IndexOf("\n    }\n", StringComparison.Ordinal)];
            Check(!link.Contains("IsItemClicked(ImGuiMouseButton.Left)"), "no link acts on the press any more");
            Check(link.Split("ChatLinkClicked(id)").Length - 1 == 5, "Dalamud, web, map, quest and item links");
        }),
        ("Chat bench 1580 X-637 and X-638: link tooltips made only on hover; a high-quality item link says so", () =>
        {
            var window = Read("MainWindow.ChatWindow.cs");
            var link = window[window.IndexOf("private void DrawChatLink(", StringComparison.Ordinal)..];
            link = link[..link.IndexOf("\n    }\n", StringComparison.Ordinal)];
            Check(!link.Contains("TipText(localization.Format(") && link.Contains("TipText(()=>localization.Format(\"ChatWindow.MapLinkTip\",Place()));"), "built inside the hover");
            Check(link.Contains("link.Hq ? \"ChatWindow.ItemLinkTipHq\":\"ChatWindow.ItemLinkTip\""), "HQ chosen by the link");
            foreach (var lang in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{lang}.json"));
                Check(text.Contains("    \"ItemLinkTipHq\": \"{0} (HQ)\\n"), lang + ": ItemLinkTipHq");
            }
        }),
    ];
}
