using TenshoAssistant.Chat;

// X-759 (0.1743, review of 0.1742, I1, R04): what PUBLISHING rule 5 says of another player's meal is what the code does, and the owner's
// choice it cites (D-207, 21/11 about 7:10 pm) was asked on that behaviour: read when the line arrives, kept in memory with the line, never
// on disk, nothing read on hover.
internal static class MealConsent1743Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    // The row of rule 5 in one half of PUBLISHING.md (« | 5 | » after the heading of the rules, one by one).
    private static string Row(string text, string heading)
    {
        var start = text.IndexOf(heading, StringComparison.Ordinal);
        Check(start >= 0, "heading: " + heading);
        var row = text.IndexOf("\n| 5 | ", start, StringComparison.Ordinal);
        Check(row >= 0, "rule 5 under " + heading);
        return text[(row + 1)..text.IndexOf('\n', row + 1)];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Meal consent 1743: behaviour: the meal's number read at the line is never written with the chat's history", () =>
        {
            var param = new ChatStatusParam { Value = 10123 };
            var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 2110, ChatChannels.From(2110), "",
                [new ChatSegment("Somebody gains the effect of "), new ChatSegment("Well Fed", ChatSegmentKind.StatusLink, new ChatLink(ChatSegmentKind.StatusLink, StatusId: 48, Param: param))]);
            var line = ChatHistory.Encode(record);
            Check(!line.Contains("10123", StringComparison.Ordinal) && !line.Contains("Param", StringComparison.Ordinal), "no meal's number on disk: " + line);
            var back = ChatHistory.Decode(line, 7);
            Check(back is not null && back.Body.All(segment => segment.Link is null), "read back as text, no Param");
        }),
        ("Meal consent 1743: wiring: read when the line arrives (the reader), the tooltip draws the number kept with the line and reads nothing", () =>
        {
            var reader = Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("if(lineData is not null)body=WithStatusParams(body,message,lineData);", StringComparison.Ordinal)
                && reader.Contains("data.StatusParam(param,link.StatusId,player?.PlayerName,(ushort)(player?.World.RowId ?? 0));", StringComparison.Ordinal), "read when the line arrives");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("ImGui.IsItemHovered())DrawChatStatusTip(statusLink.StatusId,statusLink.Param);", StringComparison.Ordinal), "the tooltip gets the Param kept with the line");
            var start = window.IndexOf("private void DrawChatStatusTip(", StringComparison.Ordinal);
            var body = window[start..window.IndexOf("\n    }\n", start, StringComparison.Ordinal)];
            Check(!body.Contains("StatusList", StringComparison.Ordinal) && !body.Contains("StatusParam(", StringComparison.Ordinal) && !body.Contains("objects", StringComparison.Ordinal), "nothing read on hover: " + body);
        }),
        ("Meal consent 1743: PUBLISHING rule 5 and the evidence cite the owner's choice made on this behaviour (D-207), both halves", () =>
        {
            var publishing = Read("docs/project/PUBLISHING.md");
            var en = Row(publishing, "## Dalamud rules, one by one");
            var fr = Row(publishing, "## Règles de Dalamud, une par une");
            foreach (var (row, said) in new[] { (en, new[] { "when the line arrives", "kept in memory with the line", "never on disk", "D-207", "it corrects D-205" }), (fr, new[] { "à l'arrivée de la ligne", "gardé en mémoire avec la ligne", "jamais sur le disque", "D-207", "il corrige D-205" }) })
                foreach (var words in said) Check(row.Contains(words, StringComparison.Ordinal), "rule 5 says « " + words + " »: " + row);
            Check(!en.Contains("(the owner's choice of 21/11/2026) |", StringComparison.Ordinal) && !fr.Contains("(choix du propriétaire du 21/11/2026) |", StringComparison.Ordinal), "no longer the choice that said otherwise");
            var evidence = Read("docs/evidence/0.1739-meal-status-gained.md");
            Check(evidence.Contains("(D-207, the owner's choice of 21/11", StringComparison.Ordinal) && evidence.Contains("(D-207, choix du propriétaire du 21/11", StringComparison.Ordinal), "the evidence cites D-207, both halves");
            Check(!evidence.Contains("(D-205, the owner's choice", StringComparison.Ordinal) && !evidence.Contains("(D-205, choix du propriétaire", StringComparison.Ordinal), "D-205 is no longer the choice cited");
        }),
    ];
}
