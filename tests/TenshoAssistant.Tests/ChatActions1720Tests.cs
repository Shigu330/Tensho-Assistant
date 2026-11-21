using TenshoAssistant.Chat;

// X-473 (0.1720): an action's coloured name in a combat line (the payloads read on 21/11: no link of the game, a colour on, the game's glow,
// the text, the glow, the colour off) becomes an action's name; its tooltip only for a name that is one action for sure
// (docs/evidence/0.1720-chat-action-names.md).
internal static class ChatActions1720Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatPayload Text(string text) => new(ChatPayloadKind.Text, "TextPayload", Text: text);
    private static ChatPayload Other(string name) => new(ChatPayloadKind.Other, name);
    private static ChatPayload On => new(ChatPayloadKind.Colour, "UIForegroundPayload", Size: 500);
    private static ChatPayload Off => new(ChatPayloadKind.Colour, "UIForegroundPayload");
    private static ChatPayload Glow => Other("UIGlowPayload");
    private static readonly ChatPayload End = new(ChatPayloadKind.LinkEnd, "LinkEnd");
    private static string Shape(IEnumerable<ChatSegment> body) => string.Join("|", body.Select(segment => $"{segment.Kind}:{segment.Text}"));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat actions 1720: the coloured name of a combat line (kind 44 of 21/11) is an action's name; elsewhere it stays text", () =>
        {
            var payloads = new List<ChatPayload> { Other("PlayerPayload"), Text("Ann Doe"), End, Text(" uses "), On, Glow, Text("Fire"), Glow, Off, Text(".") };
            var body = ChatPayloads.Segments(payloads, combat: true);
            Check(Shape(body) == "Text:Ann Doe|Text: uses |ActionName:Fire|Text:.", "the action's name: " + Shape(body));
            Check(body[2].Link is { Kind: ChatSegmentKind.ActionName, Action: "Fire" }, "its name kept");
            Check(Shape(ChatPayloads.Segments(payloads)) == "Text:Ann Doe|Text: uses |Text:Fire|Text:.", "not a combat line: text, as before");
            Check(ChatPayloads.Describe(payloads) == "PlayerPayload, TextPayload (7 characters), LinkEnd, TextPayload (6 characters), UIForegroundPayload, UIGlowPayload, TextPayload (4 characters), UIGlowPayload, UIForegroundPayload, TextPayload (1 characters)", "the developer's line unchanged: " + ChatPayloads.Describe(payloads));
        }),
        ("Chat actions 1720: a status's own colours, an icon inside, a colour never ended, no letter: no action", () =>
        {
            var status = new ChatPayload(ChatPayloadKind.Status, "Status", Link: new ChatLink(ChatSegmentKind.StatusLink, StatusId: 18));
            var linked = ChatPayloads.Segments([Text("You gain "), status, On, Glow, Text(""), Glow, Off, Text(" "), On, Text("Protect"), Off, End, Text(".")], combat: true);
            Check(!linked.Any(segment => segment.Kind == ChatSegmentKind.ActionName) && linked.Any(segment => segment.Kind == ChatSegmentKind.StatusLink), "inside a status link: its colours are not an action: " + Shape(linked));
            var icon = ChatPayloads.Segments([Text("You use "), On, new ChatPayload(ChatPayloadKind.Icon, "IconPayload", Icon: 51), Text("Potion"), Off], combat: true);
            Check(!icon.Any(segment => segment.Kind == ChatSegmentKind.ActionName) && string.Concat(icon.Select(segment => segment.Text)).EndsWith("Potion", StringComparison.Ordinal), "an icon inside: text");
            Check(!ChatPayloads.Segments([On, Text("Fire")], combat: true).Any(segment => segment.Kind == ChatSegmentKind.ActionName), "a colour never ended: text");
            Check(!ChatPayloads.Segments([On, Text("12345"), Off], combat: true).Any(segment => segment.Kind == ChatSegmentKind.ActionName), "no letter (a number): text");
            Check(ChatPayloads.ColouredName([On, Glow, Text(" Fire "), Glow, Off], 0) == ("Fire", 4), "the name trimmed, its end");
        }),
        ("Chat actions 1720: a tooltip only for a name that is one action for sure; a player's line among the player's actions", () =>
        {
            var tips = ChatActionTips.Build([
                new(1, "Fire", 10, true, false, "Deals fire damage."), new(2, "Fire", 99, false, false, ""),
                new(3, "Cure", 20, true, false, "Restores HP."), new(4, "Cure", 21, true, true, "PvP cure."),
                new(5, "Boss Slam", 30, false, false, ""), new(6, "Boss Slam", 31, false, false, ""),
                new(7, " ", 1, true, false, "x"), new(8, "Sprint", 40, true, false, "Run."), new(9, "Sprint", 40, true, false, "Run."),
                new(10, "Tail Swipe", 50, false, false, ""),
            ]);
            Check(ChatActionTips.Find(tips, "Fire", true) == new ChatStatusTip(10, "Fire", "Deals fire damage."), "a player's line: the player's Fire");
            Check(ChatActionTips.Find(tips, "Fire", false) is null, "an enemy's line: Fire is two different actions: none");
            Check(ChatActionTips.Find(tips, "Cure", true)?.Icon == 20 && ChatActionTips.Find(tips, "Cure", false) is null, "PvP left out for a player; mixed for others: none");
            Check(ChatActionTips.Find(tips, "Boss Slam", false) is null && ChatActionTips.Find(tips, "Boss Slam", true) is null, "rows that differ: none");
            Check(ChatActionTips.Find(tips, " Sprint ", false)?.Description == "Run." && ChatActionTips.Find(tips, "Tail Swipe", true)?.Icon == 50, "rows that agree, or one row (a player's line falls back on every action)");
            Check(ChatActionTips.Find(tips, "Unknown", true) is null && !tips.All.ContainsKey(""), "unknown or empty: none");
            Check(ChatActionTips.ByPlayer(1) && ChatActionTips.ByPlayer(4) && !ChatActionTips.ByPlayer(0) && !ChatActionTips.ByPlayer(5), "the players' subjects");
        }),
        ("Chat actions 1720: the history keeps an action's name as text", () =>
        {
            var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 2091, ChatChannels.From(2091), "",
                [new ChatSegment("You use "), new ChatSegment("Fire", ChatSegmentKind.ActionName, new ChatLink(ChatSegmentKind.ActionName, Action: "Fire"))]);
            var line = ChatHistory.Encode(record);
            Check(!line.Contains("ActionName", StringComparison.Ordinal), "no action written as a link: " + line);
            var back = ChatHistory.Decode(line, 7);
            Check(back is not null && back.Body.All(segment => segment.Link is null) && string.Concat(back.Body.Select(segment => segment.Text)) == "You use Fire", "read back as text");
        }),
        ("Chat actions 1720: wiring: the colour read, combat lines only, drawn as text, its tooltip, the names made once apart", () =>
        {
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("UIForegroundPayload colour=>new(ChatPayloadKind.Colour,payload.GetType().Name,Size:colour.ColorKey),", StringComparison.Ordinal)
                && reader.Contains("var body=ChatPayloads.Segments(payloads,ChatChannels.From((int)message.LogKind)==ChatChannel.Combat);", StringComparison.Ordinal), "the reader");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("ChatSegmentKind.ActionName=>false,", StringComparison.Ordinal) && window.Contains("DrawChatActionTip(actionName.Action,ChatActionTips.ByPlayer(record.Source));", StringComparison.Ordinal)
                && window.Contains("if(ChatPlayers?.Action(name,byPlayer) is not { } action)return;", StringComparison.Ordinal), "drawn as text, its tooltip, none when unsure");
            var players = Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(players.Contains("actionTips??=Task.Run(ActionTips);", StringComparison.Ordinal) && players.Contains("transient.GetRowOrDefault(row.RowId)?.Description.ExtractText().Trim()", StringComparison.Ordinal), "made once on a thread of its own, the game's description");
        }),
    ];
}
