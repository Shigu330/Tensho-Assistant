using TenshoAssistant.Chat;

// 0.1463 (X-461): in the Tensho chat window an item link in the middle of a message vanished (« test <item> test » shown « test test », 11/15,
// test T-0.1441-3). The pieces as the reader makes them from the game's payloads (ChatReader: ItemPayload → a link's start, the link's texts,
// RawPayload.LinkTerminator → its end): with the name as text, and without (only the game's link glyph, or nothing between start and end).
// Either way the link stays in its place, between the texts, underlined (role Link) and clickable (its target kept), in the lines laid out.
internal static class ChatLinks1463Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly ChatLink Potion = new(ChatSegmentKind.ItemLink, ItemId: 4554);
    private static readonly ChatLink Quest = new(ChatSegmentKind.QuestLink, QuestId: 65564);
    private static readonly DateTimeOffset Noon = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static float Measure(string text) => text.Length * 7f;

    private static string Names(ChatLink link) => link.Kind switch { ChatSegmentKind.ItemLink => "Hi-Potion", ChatSegmentKind.QuestLink => "Close to Home", _ => "" };

    private static List<ChatRun> Laid(IReadOnlyList<ChatPiece> pieces, Func<ChatLink, string?>? names = null)
    {
        var record = new ChatRecord(1, Noon, 10, ChatChannel.Say, "Ann", ChatPieces.Segments(pieces));
        var runs = ChatFormat.Runs(record, new ChatSettings { ShowTimestamps = false, ShowChannel = false }, _ => "", names);
        return [.. ChatFormat.Wrap(runs, 600, Measure).SelectMany(line => line)];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat links 1463: « test, link, test » with the item's name as text: the link in its place", () =>
        {
            // ItemPayload, the link glyph and the name (two texts), LinkTerminator, as the game and Dalamud's item links send them
            var runs = Laid([new("test "), new(null, Potion), new("\uE0BB"), new("Hi-Potion"), new(null, End: true), new(" test")]);
            Check(string.Concat(runs.Select(run => run.Text)) == "Ann: test Hi-Potion test", "the whole message: " + string.Concat(runs.Select(run => run.Text)));
            var link = runs.Single(run => run.Role == ChatRole.Link);
            Check(link.Text == "Hi-Potion" && link.Link == Potion, "underlined and clickable: " + link);
        }),
        ("Chat links 1463: « test, link, test » with the link glyph only: the item's name written in its place", () =>
        {
            var runs = Laid([new("test "), new(null, Potion), new("\uE0BB"), new(null, End: true), new(" test")], Names);
            Check(string.Concat(runs.Select(run => run.Text)) == "Ann: test Hi-Potion test", "the whole message: " + string.Concat(runs.Select(run => run.Text)));
            Check(runs.Single(run => run.Role == ChatRole.Link).Link == Potion, "the link kept");
        }),
        ("Chat links 1463: a link with nothing between its start and its end, or left open at the end, keeps its place", () =>
        {
            var runs = Laid([new("do "), new(null, Quest), new(null, End: true), new(" first, then "), new(null, Potion)], Names);
            Check(string.Concat(runs.Select(run => run.Text)) == "Ann: do Close to Home first, then Hi-Potion", string.Concat(runs.Select(run => run.Text)));
            Check(runs.Count(run => run.Role == ChatRole.Link) == 2, "two links");
        }),
        ("Chat links 1463: a link whose target is not known is never empty (« … » without a name, the window's word with one)", () =>
        {
            var runs = Laid([new("a "), new(null, Potion), new(null, End: true), new(" b")]);
            Check(runs.Single(run => run.Role == ChatRole.Link).Text == "…", "a mark where the link is");
            var named = Laid([new("a "), new(null, Potion), new(null, End: true), new(" b")], _ => "[link]");
            Check(named.Single(run => run.Role == ChatRole.Link).Text == "[link]", "the window's word");
        }),
        ("Chat links 1463: a link with its own text is written once (the name not added on top)", () =>
        {
            var segments = ChatPieces.Segments([new(null, Potion), new("\uE0BB"), new("Hi-Potion"), new(null, End: true)]);
            Check(segments.Count == 2 && segments.All(segment => segment.Text.Length > 0), "no empty place-holder when the link has text");
            var runs = Laid([new(null, Potion), new("\uE0BB"), new("Hi-Potion"), new(null, End: true)], _ => "SHOULD NOT SHOW");
            Check(!runs.Any(run => run.Text.Contains("SHOULD NOT SHOW", StringComparison.Ordinal)), "the name written once");
        }),
        ("Chat links 1463: the copy of a message has the link's name too", () =>
        {
            var record = new ChatRecord(1, Noon, 10, ChatChannel.Say, "Ann", ChatPieces.Segments([new("test "), new(null, Potion), new(null, End: true), new(" test")]));
            Check(ChatFormat.Plain(record, new ChatSettings { ShowTimestamps = false, ShowChannel = false }, _ => "", Names) == "Ann: test Hi-Potion test", "copied whole");
        }),
        ("Chat links 1463: the window names a link without text, and developer mode says the kinds of pieces, never the words", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            Check(window.Contains("ChatFormat.Runs(view.RecordAt(index),options,ChatChannelShort,ChatLinkName)", StringComparison.Ordinal)
                  && window.Contains("private string ChatLinkName(ChatLink link)=>link.Kind switch", StringComparison.Ordinal), "the window's names");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs")).Replace("\r\n", "\n");
            Check(reader.Contains("piecesSaid<PiecesSaidMax && developer?.Invoke()==true", StringComparison.Ordinal)
                  && reader.Contains("ITextProvider text=>$\"{payload.GetType().Name} ({text.Text?.Length ?? 0} characters", StringComparison.Ordinal)
                  && !reader.Contains("PieceKind(Payload payload)=>payload switch\n    {\n        ITextProvider text=>text.Text", StringComparison.Ordinal),
                  "kinds and lengths only");
        }),
    ];
}
