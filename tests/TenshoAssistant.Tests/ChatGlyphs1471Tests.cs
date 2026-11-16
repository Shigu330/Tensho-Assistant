using TenshoAssistant.Chat;

// 0.1471 (X-472, user's capture of 11/15): the Tensho chat window left out the game's own glyphs and icons: « Name [world icon] World :
// THE EXAMPLE // … » (a shout, its words in the game's boxed letters) became « NameWorld:   // … ». The message here has the same shape,
// with invented words (never a player's name in the repository): the sender as the game sends a player from another world (PlayerPayload, the
// name, LinkEnd, the world icon, the world: the shape recorded on 11/15 in kind 44, docs/evidence/0.1468-chat-link-pieces.md), the text in
// SeIconChar's boxed letters (codes of Dalamud 15). Every glyph has a readable equivalent; only the link glyph is left out.
internal static class ChatGlyphs1471Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Noon = new(2026, 11, 15, 22, 40, 0, TimeSpan.Zero);
    private static readonly ChatSettings Shown = new() { ShowTimestamps = false, ShowChannel = false };

    /// <summary>Capital letters written as the game's boxed letters (SeIconChar.BoxedLetterA = U+E071 … Z = U+E08A); other letters as they are.</summary>
    private static string Boxed(string text) => string.Concat(text.Select(letter => letter is >= 'A' and <= 'Z' ? (char)('' + (letter - 'A')) : letter));

    private static ChatPayload Text(string text) => new(ChatPayloadKind.Text, "TextPayload", Text: text);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat glyphs 1471: a shout from another world in boxed letters reads « Name@World: THE EXAMPLE // November 22 », and its copy too", () =>
        {
            List<ChatPayload> sender = [new(ChatPayloadKind.Other, "PlayerPayload"), Text("Ann Example"), new(ChatPayloadKind.LinkEnd, "LinkEnd"),
                new(ChatPayloadKind.Icon, "IconPayload", Icon: ChatGlyphs.CrossWorld), Text("Alpha")];
            List<ChatPayload> body = [Text(Boxed("THE EXAMPLE") + " // November 22")];
            var record = new ChatRecord(1, Noon, 11, ChatChannels.From(11), ChatPayloads.Text(sender), ChatPayloads.Segments(body));
            var runs = ChatFormat.Runs(record, Shown, _ => "");
            Check(string.Concat(runs.Select(run => run.Text)) == "Ann Example@Alpha: THE EXAMPLE // November 22", "the window: " + string.Concat(runs.Select(run => run.Text)));
            Check(ChatFormat.Plain(record, Shown, _ => "") == "Ann Example@Alpha: THE EXAMPLE // November 22", "the copy: " + ChatFormat.Plain(record, Shown, _ => ""));
        }),
        ("Chat glyphs 1471: every boxed letter and number of the game reads as itself", () =>
        {
            for (var letter = 'A'; letter <= 'Z'; letter++)
                Check(ChatFormat.Clean(Boxed(letter.ToString())) == letter.ToString(), "boxed " + letter);
            for (var number = 0; number <= 31; number++)
                Check(ChatGlyphs.Readable((char)('' + number)) == number.ToString(System.Globalization.CultureInfo.InvariantCulture), "boxed number " + number);
            for (var digit = 0; digit <= 9; digit++)
                Check(ChatGlyphs.Readable((char)('' + digit)) == digit.ToString() && ChatGlyphs.Readable((char)('' + digit)) == digit.ToString(), "number " + digit);
            Check(ChatGlyphs.Readable('') == "I" && ChatGlyphs.Readable('') == "VI" && ChatGlyphs.Readable('') == "1" && ChatGlyphs.Readable('') == "9", "roman, instances");
            Check(ChatFormat.Clean("Potion") == "Potion HQ" && ChatFormat.Clean("Hello") == "«Hello»" && ChatFormat.Clean("AnnAlpha") == "Ann@Alpha", "HQ, auto-translate, world");
        }),
        ("Chat glyphs 1471: no glyph of the game's font is left out but the link glyph; an unknown one is marked « · »", () =>
        {
            for (var letter = ''; letter <= ''; letter++)
                Check(letter == ChatGlyphs.LinkMarker ? ChatGlyphs.Readable(letter) == "" : ChatGlyphs.Readable(letter).Trim().Length > 0, $"U+{(int)letter:X4}");
            Check(ChatFormat.Clean("") == "·" && ChatFormat.Clean("a\u0007b") == "ab" && ChatFormat.Clean("a\nb") == "a\nb", "the rest of the private area; control characters still out");
            Check(ChatGlyphs.Icon(ChatGlyphs.CrossWorld) == "@" && ChatGlyphs.Icon(54) == "«" && ChatGlyphs.Icon(77) == "·", "icons of the game's font");
        }),
        ("Chat glyphs 1471: the recorded message of a player from another world (kind 44, 11/15) shows the world icon between the name and the text", () =>
        {
            List<ChatPayload> payloads = [new(ChatPayloadKind.Other, "PlayerPayload"), Text(new string('a', 14)), new(ChatPayloadKind.LinkEnd, "LinkEnd"),
                new(ChatPayloadKind.Icon, "IconPayload", Icon: ChatGlyphs.CrossWorld), Text(new string('b', 20))];
            var record = new ChatRecord(1, Noon, 44, ChatChannels.From(44), "", ChatPayloads.Segments(payloads));
            Check(string.Concat(ChatFormat.Runs(record, Shown, _ => "").Select(run => run.Text)) == new string('a', 14) + "@" + new string('b', 20), "name@world");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs"));
            Check(reader.Contains("var sender=ChatPayloads.Text(message.Sender.Payloads.Select(Payload));", StringComparison.Ordinal)
                  && !reader.Contains("message.Sender.TextValue", StringComparison.Ordinal), "the sender's icon kept by the reader");
        }),
    ];
}
