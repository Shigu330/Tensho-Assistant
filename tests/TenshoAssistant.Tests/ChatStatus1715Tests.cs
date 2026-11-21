using TenshoAssistant.Chat;

// X-473 (0.1715, part 2): a status the game links in a message (StatusPayload) becomes a status link of the message: its text kept, its row
// known; the window draws it as text with the game's tooltip (icon, name, description), never a false one; the history keeps it as its text;
// developer mode describes the pieces of a combat line (kinds and lengths only) to learn how the actions come.
internal static class ChatStatus1715Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatPayload Text(string text) => new(ChatPayloadKind.Text, "TextPayload", Text: text);
    private static ChatPayload Status(uint id) => new(ChatPayloadKind.Status, "Status", Link: new ChatLink(ChatSegmentKind.StatusLink, StatusId: id));
    private static readonly ChatPayload End = new(ChatPayloadKind.LinkEnd, "LinkEnd");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat status 1715: a linked status is a link of the message: its text, its row; the text around stays", () =>
        {
            var payloads = new List<ChatPayload> { Text("You gain the effect of "), Status(48), Text("Well Fed"), End, Text(".") };
            var body = ChatPayloads.Segments(payloads);
            var linked = body.Where(segment => segment.Link is not null).ToList();
            Check(linked.Count == 1 && linked[0].Kind == ChatSegmentKind.StatusLink && linked[0].Link!.StatusId == 48 && linked[0].Text == "Well Fed", "the status's segment: " + string.Join("|", body.Select(segment => $"{segment.Kind}:{segment.Text}")));
            Check(string.Concat(body.Select(segment => segment.Text)) == "You gain the effect of Well Fed.", "the message's text whole");
            Check(ChatPayloads.HasLink(payloads), "counted as a link (developer mode describes it)");
            Check(ChatPayloads.Describe(payloads) == "TextPayload (23 characters), Status 48, TextPayload (8 characters), LinkEnd, TextPayload (1 characters)", "described without the words: " + ChatPayloads.Describe(payloads));
            var two = ChatPayloads.Segments([Status(48), Text("Well Fed"), Status(49), Text("Medicated"), End]);
            Check(two.Count(segment => segment.Kind == ChatSegmentKind.StatusLink) == 2 && two.Last().Link!.StatusId == 49, "a second link closes the first");
            Check(ChatPayloads.LinkShows([Status(48), Text("Well Fed"), End], 0) == ("Well Fed", true), "what it shows");
        }),
        ("Chat status 1715: the history keeps a status link as its text (its row is not in the format)", () =>
        {
            var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 2110, ChatChannels.From(2110), "",
                [new ChatSegment("You gain the effect of "), new ChatSegment("Well Fed", ChatSegmentKind.StatusLink, new ChatLink(ChatSegmentKind.StatusLink, StatusId: 48))]);
            var line = ChatHistory.Encode(record);
            Check(!line.Contains("StatusLink", StringComparison.Ordinal), "no status link written: " + line);
            var back = ChatHistory.Decode(line, 7);
            Check(back is not null && back.Body.All(segment => segment.Link is null) && string.Concat(back.Body.Select(segment => segment.Text)) == "You gain the effect of Well Fed", "read back as text");
            var place = ChatHistory.Encode(record with { Body = [new ChatSegment("Well Fed", ChatSegmentKind.MapLink, new ChatLink(ChatSegmentKind.MapLink, 1, 2, 10, 10, "Place"))] });
            Check(place.Contains("\"type\":\"MapLink\"", StringComparison.Ordinal), "a place link is written: " + place);
            var forged = place.Replace("\"type\":\"MapLink\"", "\"type\":\"StatusLink\"", StringComparison.Ordinal);
            Check(ChatHistory.Decode(forged, 8) is { } read && read.Body.All(segment => segment.Link is null) && read.Body[0].Text == "Well Fed", "a status link in a file is read as text too");
        }),
        ("Chat status 1715: wiring: the reader, the tooltip as the game's, drawn as text, the combat lines described", () =>
        {
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("StatusPayload status=>new(ChatPayloadKind.Status,\"Status\",Link:new ChatLink(ChatSegmentKind.StatusLink,StatusId:status.Status.RowId)),", StringComparison.Ordinal), "the game's status link read");
            Check(reader.Contains("log.Information(\"Chat window, pieces of a combat line (kind {0}): {1}.\",(int)message.LogKind,ChatPayloads.Describe(payloads));", StringComparison.Ordinal)
                && reader.Contains("private readonly ChatPieceLog combatSaid=new(1,30);", StringComparison.Ordinal), "the combat lines described, one per kind, thirty at most");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("ChatSegmentKind.StatusLink=>false,", StringComparison.Ordinal), "drawn as text, nothing to click");
            Check(window.Contains("run.Link is { Kind:ChatSegmentKind.StatusLink } statusLink && ImGui.IsItemHovered())DrawChatStatusTip(statusLink.StatusId);", StringComparison.Ordinal), "its tooltip on hover");
            Check(window.Contains("if(ChatPlayers?.Status(id) is not { } status)return;", StringComparison.Ordinal), "none for an unknown status");
            var data = Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(data.Contains("status.Name.ExtractText().Trim() is { Length:>0 } name", StringComparison.Ordinal) && data.Contains("if(id==0)return null;", StringComparison.Ordinal), "a row without a name: no tooltip");
        }),
    ];
}
