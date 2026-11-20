using System.Text.RegularExpressions;
using TenshoAssistant.Chat;

// 0.1468 (X-479, review of 0.1467 I1): the test of 0.1463 (X-461) built its pieces by hand. Here the pieces are the game's: the fifteen
// messages with a link the developer-mode line recorded on 11/15 (docs/evidence/0.1468-chat-link-pieces.md: kinds of payloads in order,
// item rows, text lengths), read back from that file, each text filled with letters of its recorded length, and replayed through the reader's
// own steps (ChatPayloads.Segments, ChatFormat.Runs and Wrap). Every link keeps its place with its whole name, and no text before or after it
// is lost. The line written by the reader is the one read here (Describe gives back each recorded line). The cap of the developer-mode line
// keeps room for a message said by a player after a run of loot lines (none was recorded on 11/15).
internal static class ChatLinkPieces1468Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Evidence = "docs/evidence/0.1468-chat-link-pieces.md";
    private static readonly Regex Recorded = new(@"^(\d\d:\d\d:\d\d) kind (\d+): (.+)$");
    private static readonly Regex Token = new(@"(?<name>\w+) \((?<len>\d+) characters(?<glyph>, game glyph)?\)|Raw \((?<size>\d+) bytes\)|Item (?<id>\d+)(?<hq> HQ)?|(?<other>\w+)");
    private static readonly ChatSettings Bare = new() { ShowTimestamps = false, ShowChannel = false, GameLinks = false };   // 0.1488 (X-505): links as before 0.1488 (ChatLook1488Tests: as in the game)
    private static readonly DateTimeOffset Noon = new(2026, 11, 15, 22, 0, 0, TimeSpan.Zero);

    private static List<(string Time, int Kind, string Line)> Lines()
    {
        var path = Path.GetFullPath(Evidence);
        Check(File.Exists(path), "the evidence: " + Evidence);
        var lines = File.ReadAllLines(path).Select(line => Recorded.Match(line)).Where(match => match.Success)
            .Select(match => (match.Groups[1].Value, int.Parse(match.Groups[2].Value), match.Groups[3].Value)).ToList();
        Check(lines.Count == 15, "the fifteen recorded messages: " + lines.Count);
        return lines;
    }

    /// <summary>A recorded line made into payloads: each text of its length (letters a, b, c… one per text; the link glyph first when recorded).</summary>
    private static List<ChatPayload> Payloads(string line, char? oneLetter = null)
    {
        var payloads = new List<ChatPayload>();
        var texts = 0;
        foreach (Match token in Token.Matches(line))
        {
            if (token.Groups["len"].Success)
            {
                var length = int.Parse(token.Groups["len"].Value);
                var letter = (char)('a' + texts++ % 26);   // each text its letter, in order (the glyph's text too)
                if (length == 1 && oneLetter is { } given) letter = given;
                var text = token.Groups["glyph"].Success ? "\uE0BB" + new string(letter, length - 1) : new string(letter, length);
                payloads.Add(new(ChatPayloadKind.Text, token.Groups["name"].Value, Text: text));
            }
            else if (token.Groups["size"].Success) payloads.Add(new(ChatPayloadKind.Raw, "Raw", Size: int.Parse(token.Groups["size"].Value)));
            else if (token.Groups["id"].Success)
                payloads.Add(new(ChatPayloadKind.Item, "Item", Link: new ChatLink(ChatSegmentKind.ItemLink, ItemId: uint.Parse(token.Groups["id"].Value), Hq: token.Groups["hq"].Success)));
            else payloads.Add(token.Groups["other"].Value switch
            {
                "LinkEnd" => new(ChatPayloadKind.LinkEnd, "LinkEnd"),
                "MapLink" => new(ChatPayloadKind.Map, "MapLink", Link: new ChatLink(ChatSegmentKind.MapLink, 820, 498, 10.5f, 11.2f)),
                "Quest" => new(ChatPayloadKind.Quest, "Quest", Link: new ChatLink(ChatSegmentKind.QuestLink, QuestId: 65564)),
                "IconPayload" => new(ChatPayloadKind.Icon, "IconPayload", Icon: 88),
                var name => new(ChatPayloadKind.Other, name),
            });
        }
        return payloads;
    }

    private static List<ChatRun> Laid(int kind, IReadOnlyList<ChatPayload> payloads, string sender = "", float width = 600)
    {
        var record = new ChatRecord(1, Noon, kind, ChatChannels.From(kind), sender, ChatPayloads.Segments(payloads));
        var runs = ChatFormat.Runs(record, Bare, _ => "", link => link.Kind == ChatSegmentKind.MapLink ? "Eulmore" : "Hi-Potion");
        return [.. ChatFormat.Wrap(runs, width, text => text.Length * 7f).SelectMany(line => line)];
    }

    /// <summary>Each link's own texts, cleaned of the game's glyphs (from its start to its end, the next start, or the end of the message).</summary>
    private static List<(ChatLink Link, string Name)> Named(IReadOnlyList<ChatPayload> payloads)
    {
        var named = new List<(ChatLink Link, string Name)>();
        foreach (var payload in payloads)
        {
            if (payload.Link is { } link && payload.Kind is ChatPayloadKind.Item or ChatPayloadKind.Map or ChatPayloadKind.Quest) named.Add((link, ""));
            else if (payload.Kind == ChatPayloadKind.LinkEnd && named.Count > 0 && named[^1].Name is not null) named[^1] = (named[^1].Link, named[^1].Name + "\0");
            else if (payload.Kind == ChatPayloadKind.Text && named.Count > 0 && !named[^1].Name.EndsWith('\0')) named[^1] = (named[^1].Link, named[^1].Name + ChatFormat.Clean(payload.Text));
        }
        return [.. named.Select(item => (item.Link, item.Name.TrimEnd('\0')))];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat link pieces 1468: the reader's line gives back each of the fifteen recorded lines (one format, read and written)", () =>
        {
            foreach (var (time, _, line) in Lines())
                Check(ChatPayloads.Describe(Payloads(line)) == line, $"{time}: written « {ChatPayloads.Describe(Payloads(line))} »");
        }),
        ("Chat link pieces 1468: every recorded message keeps every text, and each link its whole name in its place, underlined", () =>
        {
            foreach (var (time, kind, line) in Lines())
            {
                var payloads = Payloads(line);
                var runs = Laid(kind, payloads);
                // 0.1471 (X-472): the icons of the game's font are kept too, written readably (the world icon « @ » in kind 44)
                var all = ChatFormat.Clean(ChatPayloads.Text(payloads));
                Check(string.Concat(runs.Select(run => run.Text)) == all, $"{time}: nothing lost: « {string.Concat(runs.Select(run => run.Text))} »");
                foreach (var (link, name) in Named(payloads))
                {
                    var shown = string.Concat(runs.Where(run => run.Role == ChatRole.Link && Equals(run.Link, link)).Select(run => run.Text));
                    Check(name.Length > 0 && shown == name, $"{time}: the link to {link.Kind} {link.ItemId} shows « {shown} », expected « {name} »");
                }
            }
        }),
        ("Chat link pieces 1468: the name cut by a raw payload (kind 44) is one link; a player's name before it stays text", () =>
        {
            var (_, kind, line) = Lines().First(item => item.Line.Contains("Raw (4 bytes)", StringComparison.Ordinal));
            var runs = Laid(kind, Payloads(line));
            var link = runs.Where(run => run.Role == ChatRole.Link).ToList();
            Check(link.Count == 1 && link[0].Text.Length == 27 + 14 && link[0].Link?.ItemId == 21070, "one underlined run of 41 letters: " + string.Join("|", link));
            Check(runs[0].Role == ChatRole.Body && runs[0].Text.StartsWith(new string('a', 10), StringComparison.Ordinal), "the player's name, plain: " + runs[0]);
        }),
        ("Chat link pieces 1468: « test <item> test » said by a player, the item link as the game sends it (recorded shape), shows the link between the two words", () =>
        {
            // The item link of 22:56:28 as recorded (colours, Item 5711, glyph, name of 25 characters, LinkEnd), between « test » and « test ».
            var (_, _, line) = Lines()[0];
            var link = Payloads(line).Skip(1).Take(12).ToList();
            List<ChatPayload> said = [new(ChatPayloadKind.Text, "TextPayload", Text: "test "), .. link, new(ChatPayloadKind.Text, "TextPayload", Text: " test")];
            var runs = Laid(10, said, "Ann");
            var name = new string('c', 25);   // a: the text before (left out), b: the glyph's text, c: the name
            Check(string.Concat(runs.Select(run => run.Text)) == $"Ann: test {name} test", "the message: " + string.Concat(runs.Select(run => run.Text)));
            Check(runs.Single(run => run.Role == ChatRole.Link) is { Link.ItemId: 5711 } shown && shown.Text == name, "underlined, clickable");
            Check(ChatPayloads.Bare(ChatPayloads.Segments(said)) == 0, "a link with its name is not « without text of its own »");
        }),
        ("Chat link pieces 1468: the map link of 23:39 (one-character texts only): with spaces, its place is kept as the game's arrow (0.1487, X-506: no name)", () =>
        {
            var (_, kind, line) = Lines()[^1];
            var payloads = Payloads(line, ' ');
            var body = ChatPayloads.Segments(payloads);
            Check(ChatPayloads.Bare(body) == 0, "its glyph is shown: not « without text of its own »: " + ChatPayloads.Bare(body));
            var runs = Laid(kind, payloads, width: 2000);
            Check(runs.Any(run => run.Role == ChatRole.Link && run.Text.Contains('\u2192', StringComparison.Ordinal) && run.Link?.Kind == ChatSegmentKind.MapLink)
                  && !runs.Any(run => run.Text.Contains("Eulmore", StringComparison.Ordinal)), "the arrow, no place name: " + string.Join("|", runs));
            Check(runs.Single(run => run.Link?.Kind == ChatSegmentKind.ItemLink).Text == new string('e', 23), "the item after it keeps its name");
        }),
        ("Chat link pieces 1468: the developer line keeps room for a message said by a player after many loot lines (two per kind, thirty per session)", () =>
        {
            var kinds = Lines().Select(item => item.Kind).ToList();
            var loot = Enumerable.Repeat(kinds, 3).SelectMany(list => list).ToList();   // 45 messages with a link, kinds 62, 60, 57, 44, 1
            var log = new ChatPieceLog();
            var said = loot.Where(log.Take).ToList();
            Check(said.Count == 2 * kinds.Distinct().Count(), "two lines per kind of loot or system message: " + said.Count);
            Check(log.Take(10) && log.Take(10) && !log.Take(10), "then a message said by a player (Say, kind 10) is still written, twice");
            var before = new ChatPieceLog(perKind: int.MaxValue, total: 20);   // 0.1463: twenty per session, whatever the kind
            Check(loot.Count(before.Take) == 20 && !before.Take(10), "with the cap of 0.1463 the Say would not have been written");
            var full = new ChatPieceLog(perKind: 50, total: 3);
            Check(full.Take(1) && full.Take(2) && full.Take(3) && !full.Take(4), "thirty at most per session");
        }),
        ("Chat link pieces 1468: a raw payload or another payload is never a piece (0.1471, X-472: an icon is, written readably); the reader tells each payload apart (wiring)", () =>
        {
            Check(!ChatPayloads.Pieces([new(ChatPayloadKind.Raw, "Raw", Size: 4), new(ChatPayloadKind.Other, "PlayerPayload")]).Any(), "no piece");
            // X-635 (0.1580): a map link that could not be read has no start, and ends a link still open (ChatBench1580Tests).
            Check(ChatPayloads.Pieces([new(ChatPayloadKind.Map, "MapLink")]).Single() is { Start: null, End: true }, "an unreadable map link: an end, no start");
            Check(ChatPayloads.Pieces([new(ChatPayloadKind.Icon, "IconPayload", Icon: 88)]).Single() == new ChatPiece("@"), "the world icon: « @ »");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs"));
            Check(reader.Contains("ChatPayloads.Segments(payloads)", StringComparison.Ordinal) && reader.Contains("ChatPayloads.Describe(payloads)", StringComparison.Ordinal)
                  && reader.Contains("piecesSaid.Take(kind)", StringComparison.Ordinal) && reader.Contains("ChatPayloads.Bare(body)>0", StringComparison.Ordinal)
                  && !reader.Contains("PiecesSaidMax", StringComparison.Ordinal), "the reader uses the pure steps");
        }),
    ];
}
