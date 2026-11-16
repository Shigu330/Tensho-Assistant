using System.Text.RegularExpressions;
using TenshoAssistant.Chat;

// 0.1487 (X-479, X-506): the payloads the game sent on 11/16 (docs/evidence/0.1487-chat-link-said.md), replayed through the reader's own steps
// (ChatPayloads.Segments, ChatFormat.Runs). X-479: « test <item> test » said by the player carries the item's name as text inside the link: the
// link shows between the two words with its own name, nothing written in its place. X-506: the place link of Tensho's alarms has only the game's
// arrow: it shows as an arrow, and the zone's name is no longer glued to the item's (« Thanalan occidentalTerreau thanalanais III »).
internal static class ChatLinks1487Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Evidence = "docs/evidence/0.1487-chat-link-said.md";
    private static readonly Regex Recorded = new(@"^(\d\d:\d\d:\d\d) kind (\d+): (.+)$");
    private static readonly Regex Token = new(@"(?<name>\w+) \((?<len>\d+) characters(?<glyph>, game glyph)?\)|Raw \((?<size>\d+) bytes\)|Item (?<id>\d+)|(?<other>\w+)");
    private static readonly ChatSettings Bare = new() { ShowTimestamps = false, ShowChannel = false };
    private static readonly DateTimeOffset Morning = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly ChatLink Place = new(ChatSegmentKind.MapLink, 140, 20, 26.1f, 24.4f);

    private static (int Kind, string Line) Line(int kind)
    {
        var path = Path.GetFullPath(Evidence);
        Check(File.Exists(path), "the evidence: " + Evidence);
        var match = File.ReadAllLines(path).Select(line => Recorded.Match(line)).First(item => item.Success && item.Groups[2].Value == kind.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return (kind, match.Groups[3].Value);
    }

    /// <summary>A recorded line made into payloads: texts of their length (the given words first, then letters; one-character texts as spaces, the glyph's text the link glyph).</summary>
    private static List<ChatPayload> Payloads(string line, params string[] words)
    {
        var payloads = new List<ChatPayload>();
        var texts = 0;
        foreach (Match token in Token.Matches(line))
        {
            if (token.Groups["len"].Success)
            {
                var length = int.Parse(token.Groups["len"].Value);
                string text;
                if (token.Groups["glyph"].Success) text = ChatGlyphs.LinkMarker.ToString();
                else if (length == 1) text = " ";
                else if (texts < words.Length && words[texts].Length == length) text = words[texts++];
                else text = new string((char)('k' + texts++ % 10), length);
                payloads.Add(new(ChatPayloadKind.Text, token.Groups["name"].Value, Text: text));
            }
            else if (token.Groups["size"].Success) payloads.Add(new(ChatPayloadKind.Raw, "Raw", Size: int.Parse(token.Groups["size"].Value)));
            else if (token.Groups["id"].Success) payloads.Add(new(ChatPayloadKind.Item, "Item", Link: new ChatLink(ChatSegmentKind.ItemLink, ItemId: uint.Parse(token.Groups["id"].Value))));
            else payloads.Add(token.Groups["other"].Value switch
            {
                "LinkEnd" => new(ChatPayloadKind.LinkEnd, "LinkEnd"),
                "MapLink" => new(ChatPayloadKind.Map, "MapLink", Link: Place),
                var name => new(ChatPayloadKind.Other, name),
            });
        }
        return payloads;
    }

    private static string Names(ChatLink link) => link.Kind == ChatSegmentKind.MapLink ? "Thanalan occidental" : "Terreau thanalanais III";

    private static (List<ChatRun> Runs, IReadOnlyList<ChatSegment> Body) Laid(int kind, string sender, List<ChatPayload> payloads)
    {
        var body = ChatPayloads.Segments(payloads);
        var record = new ChatRecord(1, Morning, kind, ChatChannels.From(kind), sender, body);
        return ([.. ChatFormat.Runs(record, Bare, _ => "", Names)], body);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat links 1487: the recorded lines are read back as the reader writes them", () =>
        {
            foreach (var kind in new[] { 10, 1 })
            {
                var (_, line) = Line(kind);
                Check(ChatPayloads.Describe(Payloads(line)) == line, $"kind {kind}: « {ChatPayloads.Describe(Payloads(line))} »");
            }
        }),
        ("Chat links 1487: « test <item> test » said by the player (kind 10, 11/16) shows the item's own name between the two words (X-479)", () =>
        {
            var (kind, line) = Line(10);
            var (runs, body) = Laid(kind, "Ann", Payloads(line, "test ", "Item's name", " test"));
            var item = runs.Single(run => run.Role == ChatRole.Link);
            Check(item.Link?.ItemId == 20253 && item.Text.Length == 11, "the item link with its own name (11 characters): " + item);
            Check(ChatPayloads.Bare(body) == 0, "a link with text of its own: nothing written in its place");
            var all = string.Concat(runs.Select(run => run.Text));
            Check(all.StartsWith("Ann: test ", StringComparison.Ordinal) && all.EndsWith(item.Text + " test", StringComparison.Ordinal), "between the two words: " + all);
            Check(!all.Contains("Terreau", StringComparison.Ordinal), "no name added on top");
        }),
        ("Chat links 1487: Tensho's alarm (kind 1, 11/16): the place link shows the game's arrow, the item's name follows, no zone name (X-506)", () =>
        {
            var (kind, line) = Line(1);
            var (runs, body) = Laid(kind, "", Payloads(line));
            var all = string.Concat(runs.Select(run => run.Text));
            Check(!all.Contains("Thanalan occidental", StringComparison.Ordinal), "no zone name: " + all);
            Check(ChatPayloads.Bare(body) == 0, "the place link is not « without text of its own » any more");
            var place = runs.Where(run => run.Link == Place).ToList();
            Check(place.Any(run => run.Text == ChatGlyphs.LinkArrow), "the arrow, clickable (opens the place): " + string.Join("|", place));
            var item = runs.Single(run => run.Link?.Kind == ChatSegmentKind.ItemLink);
            Check(item.Text.Length == 23, "the item link keeps its own name: " + item);
            var arrow = all.IndexOf(ChatGlyphs.LinkArrow, StringComparison.Ordinal);
            Check(arrow > 0 && all.IndexOf(item.Text, StringComparison.Ordinal) > arrow, "the arrow, then the item, as the game's chat");
        }),
        ("Chat links 1487: the target's name is written only for a link that would otherwise show nothing", () =>
        {
            var none = ChatPieces.Segments([new("go "), new(null, Place), new(null, End: true), new(" now")]);
            var record = new ChatRecord(1, Morning, 10, ChatChannel.Say, "Ann", none);
            Check(ChatFormat.Plain(record, Bare, _ => "", Names) == "Ann: go Thanalan occidental now", "nothing at all: the name");
            var arrow = ChatPieces.Segments([new("go "), new(null, Place), new(ChatGlyphs.LinkMarker.ToString()), new(null, End: true), new(" now")]);
            Check(ChatFormat.Plain(record with { Body = arrow }, Bare, _ => "", Names) == "Ann: go \u2192 now", "the glyph only: the arrow");
            var named = ChatPieces.Segments([new(null, Place), new(ChatGlyphs.LinkMarker.ToString()), new("Ul'dah"), new(null, End: true)]);
            Check(ChatFormat.Plain(record with { Body = named }, Bare, _ => "", Names) == "Ann: Ul'dah", "its own text: shown alone, the glyph left out");
        }),
    ];
}
