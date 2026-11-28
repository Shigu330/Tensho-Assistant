using System.Text.RegularExpressions;
using TenshoAssistant.Chat;

// 0.1491 (review of 0.1490). X-509 (I1): the place link of Tensho's alarms showed no name in the game's own chat because the ring sent the
// link's start alone (SeStringBuilder.AddMapLink, Dalamud 15.0.4.0: docs/evidence/0.1491-alarm-place-link.md); the ring now sends Dalamud's
// whole place link (arrow, place and coordinates, end), and the glyph Dalamud's item link puts before the item is that item's arrow in the
// Tensho window, as in the game. X-510 (I2): with the game's look, a link without the game's arrow (a web address, a link named by Tensho, a link
// of Dalamud) is underlined in the accent colour, told apart from the text. X-511: M1 set aside with its reason (pinned below), M2 the recorded
// ring replayed with the default look, M3 the 1481 test's name.
internal static class ChatReview1491Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private const string Evidence = "docs/evidence/0.1487-chat-link-said.md";
    private static readonly Regex Recorded = new(@"^(\d\d:\d\d:\d\d) kind (\d+): (.+)$");
    private static readonly Regex Token = new(@"(?<name>\w+) \((?<len>\d+) characters(?<glyph>, game glyph)?\)|Raw \((?<size>\d+) bytes\)|Item (?<id>\d+)|(?<other>\w+)");
    private static readonly DateTimeOffset Morning = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly ChatLink Place = new(ChatSegmentKind.MapLink, 140, 20, 26.1f, 24.4f);
    private static readonly ChatSettings Game = new() { ShowTimestamps = false, ShowChannel = false };
    private static readonly ChatSettings Before = new() { ShowTimestamps = false, ShowChannel = false, GameLinks = false };
    private const string Where = "Thanalan occidental ( 26.1 , 24.4 )";
    private const string Soil = "Terreau thanalanais III";
    private static readonly string Glyph = ChatGlyphs.LinkMarker.ToString();

    private static ChatPayload Text(string text) => new(ChatPayloadKind.Text, "TextPayload", Text: text);
    private static ChatPayload Other(string name) => new(ChatPayloadKind.Other, name);
    private static readonly ChatPayload End = new(ChatPayloadKind.LinkEnd, "LinkEnd");

    /// <summary>The ring recorded on 11/16 at 05:29:36 (kind 1), made into payloads: texts of their length (the item's name, letters for the
    /// others; one-character texts as spaces, the glyph's text the link glyph).</summary>
    private static List<ChatPayload> RecordedRing()
    {
        var path = Path.GetFullPath(Evidence);
        Check(File.Exists(path), "the evidence: " + Evidence);
        var line = File.ReadAllLines(path).Select(item => Recorded.Match(item)).First(item => item.Success && item.Groups[2].Value == "1").Groups[3].Value;
        var payloads = new List<ChatPayload>();
        foreach (Match token in Token.Matches(line))
        {
            if (token.Groups["len"].Success)
            {
                var length = int.Parse(token.Groups["len"].Value);
                payloads.Add(Text(token.Groups["glyph"].Success ? Glyph : length == 1 ? " " : length == Soil.Length ? Soil : new string('k', length)));
            }
            else if (token.Groups["size"].Success) payloads.Add(new(ChatPayloadKind.Raw, "Raw", Size: int.Parse(token.Groups["size"].Value)));
            else if (token.Groups["id"].Success) payloads.Add(new(ChatPayloadKind.Item, "Item", Link: new ChatLink(ChatSegmentKind.ItemLink, ItemId: uint.Parse(token.Groups["id"].Value))));
            else payloads.Add(token.Groups["other"].Value switch
            {
                "LinkEnd" => End,
                "MapLink" => new(ChatPayloadKind.Map, "MapLink", Link: Place),
                var name => Other(name),
            });
        }
        return payloads;
    }

    /// <summary>The same ring with the place link Dalamud's SeString.CreateMapLink makes (Dalamud 15.0.4.0, read on 11/16): the start, the game's
    /// arrow in its colours (with the space of a French client), the place and its coordinates, the end.</summary>
    private static List<ChatPayload> WholeRing()
    {
        var payloads = RecordedRing();
        var at = payloads.FindIndex(payload => payload.Kind == ChatPayloadKind.Map);
        payloads.InsertRange(at + 1, [Other("UIForegroundPayload"), Other("UIGlowPayload"), Text(Glyph + " "), Other("UIGlowPayload"), Other("UIForegroundPayload"), Text(Where), End]);
        return payloads;
    }

    private static List<ChatRun> Runs(ChatSettings settings, IReadOnlyList<ChatSegment> body)
        => [.. ChatFormat.Runs(new ChatRecord(1, Morning, 1, ChatChannels.From(1), "", body), settings, _ => "", link => link.Kind == ChatSegmentKind.MapLink ? "Thanalan occidental" : Soil)];

    private static List<ChatRun> Said(ChatSettings settings, List<ChatPiece> pieces)
        => [.. ChatFormat.Runs(new ChatRecord(1, Morning, 10, ChatChannel.Say, "Ann", ChatPieces.Segments(pieces)), settings, _ => "", _ => "Hi-Potion")];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1491: the ring sends Dalamud's whole place link, never AddMapLink's bare start; the self-check reads it (X-509)", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs") + Read("src/TenshoAssistant/Plugin.Alarms.cs");
            Check(plugin.Contains("Alarms.AlarmPlaceLink.Build(place.TerritoryId,place.MapId,place.X,place.Y,log) is { } placeLink", StringComparison.Ordinal)
                  && plugin.Contains(".Append(placeLink);", StringComparison.Ordinal) && !plugin.Contains(".AddMapLink(", StringComparison.Ordinal), "the ring: the whole link");
            var link = Read("src/TenshoAssistant/Alarms/AlarmPlaceLink.Dalamud.cs");
            Check(link.Contains("SeString.CreateMapLink(territory,map,x,y,0.05f)", StringComparison.Ordinal) && link.Contains("return null;", StringComparison.Ordinal),
                  "Dalamud's CreateMapLink; none (never a bare one) when it cannot be made");
            var self = Read("src/TenshoAssistant/MainWindow.SelfCheck.cs");
            Check(self.Contains("(\"alarm place link\", SelfCheckAlarmPlaceLink)", StringComparison.Ordinal) && self.Contains("TenshoAssistant.Alarms.AlarmPlaceLink.Build(", StringComparison.Ordinal)
                  && self.Contains("ChatPayloads.PlaceLinkFault(payloads)", StringComparison.Ordinal) && self.Contains("TenshoAssistant.Chat.ChatReader.Payload", StringComparison.Ordinal),
                  "the self-check makes the link as the ring does and reads it as the window does");
            Check(Read("docs/evidence/0.1491-alarm-place-link.md").Contains("SeStringBuilder::AddMapLink (5 params)", StringComparison.Ordinal), "the evidence");
            Check(Read(Evidence).Contains("Correction (0.1491, X-509", StringComparison.Ordinal), "the 0.1487 reading corrected where it was written");
        }),
        ("Chat review 1491: the recorded ring fails the place-link check (no place, no end); the whole link passes it (X-509)", () =>
        {
            var recorded = RecordedRing();
            Check(ChatPayloads.Describe(recorded).StartsWith("TextPayload (89 characters), MapLink, TextPayload (1 characters), Raw (9 bytes)", StringComparison.Ordinal), "the recorded ring: " + ChatPayloads.Describe(recorded));
            Check(ChatPayloads.PlaceLinkFault(recorded) is { } fault && fault.Contains("0 characters", StringComparison.Ordinal), "the bare start: " + ChatPayloads.PlaceLinkFault(recorded));
            var whole = WholeRing();
            Check(ChatPayloads.PlaceLinkFault(whole) is null, "the whole link: " + ChatPayloads.PlaceLinkFault(whole));
            Check(ChatPayloads.LinkShows(whole, whole.FindIndex(payload => payload.Kind == ChatPayloadKind.Map)) == (Where, true), "it shows the place and its coordinates, then ends");
            Check(ChatPayloads.PlaceLinkFault([new(ChatPayloadKind.Map, "MapLink", Link: Place), Text(Glyph), End]) is not null, "the arrow alone is not a place");
            Check(ChatPayloads.PlaceLinkFault([new(ChatPayloadKind.Map, "MapLink", Link: Place), Text("Ul'dah"), End]) is not null, "a name without coordinates is not enough");
            Check(ChatPayloads.PlaceLinkFault([new(ChatPayloadKind.Map, "MapLink", Link: Place), Text(Where)]) == "the place link has no end", "a link left open");
            Check(ChatPayloads.PlaceLinkFault([Text(Where)]) == "no place link", "no link at all");
        }),
        ("Chat review 1491: the whole ring laid out with the default look: the place's arrow, place and coordinates, then the item's own arrow and name, as the game's chat (X-509)", () =>
        {
            var body = ChatPayloads.Segments(WholeRing());
            Check(ChatPayloads.Bare(body) == 0, "no link without text of its own");
            var runs = Runs(Game, body);
            var place = runs.Where(run => run.Link == Place).ToList();
            Check(place.Count >= 2 && place[0] is { Role: ChatRole.LinkMark, Text: "\u2192" }, "the place link opens with the game's arrow: " + string.Join("|", place));
            Check(string.Concat(place.Select(run => run.Text)) == "\u2192 " + Where && place.All(run => !run.Underlined), "then the place and its coordinates, as a game link (not underlined)");
            var item = runs.Where(run => run.Link?.Kind == ChatSegmentKind.ItemLink).ToList();
            Check(item.Count == 2 && item[0] is { Role: ChatRole.LinkMark, Text: "\u2192" } && item[1] is { Role: ChatRole.Link, Text: Soil } && item.All(run => !run.Underlined),
                  "the item link: its arrow (the glyph before it), then its own name: " + string.Join("|", item));
            var all = string.Concat(runs.Select(run => run.Text));
            var at = all.IndexOf(Where, StringComparison.Ordinal);
            Check(at > 0 && all.IndexOf("\u2192 " + Soil, at + Where.Length, StringComparison.Ordinal) > at, "the place, then the item with its arrow, apart: " + all);
            Check(all.IndexOf("Thanalan occidentalTerreau", StringComparison.Ordinal) < 0, "nothing glued");
            var before = ChatFormat.Plain(new ChatRecord(1, Morning, 1, ChatChannels.From(1), "", body), Before, _ => "", _ => "");
            Check(before.Contains(Where, StringComparison.Ordinal) && before.Contains(Soil, StringComparison.Ordinal) && !before.Contains('\u2192'), "without the game's look: the place and the item, underlined, no arrow: " + before);
        }),
        ("Chat review 1491: the recorded ring (bare place link) with the default look: the arrow opens the place, the item keeps its name, underlined (M2)", () =>
        {
            var body = ChatPayloads.Segments(RecordedRing());
            var runs = Runs(Game, body);
            var all = string.Concat(runs.Select(run => run.Text));
            Check(!all.Contains("Thanalan occidental", StringComparison.Ordinal) && ChatPayloads.Bare(body) == 0, "no zone name added: " + all);
            Check(runs.Where(run => run.Link == Place).Any(run => run is { Role: ChatRole.LinkMark, Text: "\u2192" }), "the glyph inside the open place link: its arrow");
            var item = runs.Single(run => run.Link?.Kind == ChatSegmentKind.ItemLink);
            Check(item is { Role: ChatRole.Link, Text: Soil, Underlined: true }, "the item: its own name, underlined (no arrow of its own in that message): " + item);
            Check(all.IndexOf(Soil, StringComparison.Ordinal) > all.IndexOf('\u2192'), "the arrow, then the item");
        }),
        ("Chat review 1491: with the game's look, a link without the game's arrow is underlined; one with it is not (X-510)", () =>
        {
            var web = Said(Game, [new("see https://example.org now")]);
            Check(web.Single(run => run.Link?.Kind == ChatSegmentKind.WebLink) is { Role: ChatRole.Link, Underlined: true }, "a web address found by Tensho");
            var potion = new ChatLink(ChatSegmentKind.ItemLink, ItemId: 4554);
            var named = Said(Game, [new("go "), new(null, potion), new(null, End: true), new(" now")]);
            Check(named.Single(run => run.Link == potion) is { Text: "Hi-Potion", Underlined: true }, "a link named by Tensho (no text in the message)");
            var plugin = new ChatLink(ChatSegmentKind.DalamudLink, Plugin: "Dalamud", CommandId: 1);
            Check(Said(Game, [new(null, plugin), new("Learn more"), new(null, End: true)]).Single(run => run.Link == plugin).Underlined, "a link of Dalamud without arrow");
            var game = Said(Game, [new(null, potion), new(Glyph), new("Hi-"), new("Potion"), new(null, End: true)]);
            Check(game.Where(run => run.Link == potion).All(run => !run.Underlined) && game.Count(run => run.Role == ChatRole.LinkMark) == 1, "a game link with its arrow, its name cut in two: not underlined");
            Check(Said(Before, [new("see https://example.org now")]).All(run => !run.Underlined), "without the game's look the flag is not set (every link is underlined there)");
            var wrapped = ChatFormat.Wrap(web, 60, text => text.Length * 7f).SelectMany(line => line).Where(run => run.Link?.Kind == ChatSegmentKind.WebLink).ToList();
            Check(wrapped.Count >= 2 && wrapped.All(run => run.Underlined), "laid out on several lines, every piece stays underlined");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("var underline=!ChatOptions.GameLinks || run.Underlined;", StringComparison.Ordinal) && window.Contains("var colour=underline ? Accent:", StringComparison.Ordinal)
                  && window.Contains("if(underline)", StringComparison.Ordinal), "the window draws the flag: accent colour and the line under it");
        }),
        ("Chat review 1491: faded, no zone of the outline is darker than the whole outline faded; where the four copies meet, exactly that (M1 set aside, with its reason)", () =>
        {
            // Reason (X-511 M1): the four copies overlap (one copy along a straight edge, two at a corner, four under a letter). No opacity of
            // each copy makes every zone equal to a fade of the whole outline at once, and ImGui has no layer to fade whole. 0.1489 bounds the
            // darkest zone, under the letters, seen through the faded text, where the review of 0.1486 found the halo darker than the text: every
            // zone is now at most as dark as the faded outline, and the edges are fainter (a third at 30 %), on a background at 15 %.
            var full = ChatLook.OutlineOpacity;
            foreach (var alpha in new[] { 0.3f, ChatCover.FadedAlpha, 0.5f, 0.8f })
            {
                var each = (ChatLook.Outline(alpha) >> 24) / 255d;
                for (var copies = 1; copies <= ChatLook.OutlineShifts.Length; copies++)
                {
                    var zone = 1 - Math.Pow(1 - each, copies);
                    var layer = alpha * (1 - Math.Pow(1 - full, copies));
                    Check(zone <= layer + 0.01, $"{alpha}, {copies} copies: {zone:0.000} against the faded outline {layer:0.000}");
                }
                Check(Math.Abs(1 - Math.Pow(1 - each, 4) - alpha * ChatLook.Together(full)) <= 0.01, $"{alpha}: four copies, the faded outline");
            }
            var edge = (ChatLook.Outline(0.3f) >> 24) / 255d;
            Check(edge > 0.08 && edge < 0.09, $"the review's figure: one copy at 30 % is {edge:0.000} (22/255)");
        }),
        ("Chat review 1491: the 1481 test's name says the defaults it checks (M3)", () =>
        {
            var test = Read("tests/TenshoAssistant.Tests/ChatLook1481Tests.cs");
            Check(test.Contains("defaults (tabs under the lines, outline on, background at 15 %)", StringComparison.Ordinal), "the name");
            Check(!test.Contains("defaults (tabs over", StringComparison.Ordinal) && !test.Contains("light background); time", StringComparison.Ordinal), "no more the look before 0.1488");
        }),
    ];
}
