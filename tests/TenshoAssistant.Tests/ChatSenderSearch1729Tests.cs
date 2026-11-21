using TenshoAssistant.Chat;

// X-730 M2 (0.1729, review of 0.1721 to 0.1726): the chat search finds a party line's sender as the line shows it (« [1] [SAG] Name ») and
// without its brackets (« 1 SAG Name »); capture mode: the stand-in, never the name. A sender without the game's number: as before.
internal static class ChatSenderSearch1729Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);
    // The game's number glyph (U+E090: 1), then the job's abbreviation and a space, then the name (the chat history of 21/11).
    private const string PartySender = "SAG Ann Doe";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat sender search 1729: a party line's sender: found as shown and without brackets, never glued", () =>
        {
            var record = new ChatRecord(1, Now, 14, ChatChannel.Party, PartySender, [new ChatSegment("hello")]);
            Check(ChatFormat.ShownSender(record, false) == "[1] [SAG] Ann Doe" && ChatFormat.SenderWords(record, false) == "1 SAG Ann Doe", ChatFormat.ShownSender(record, false));
            var settings = new ChatSettings { ShowTimestamps = false, ShowChannel = false, SenderBrackets = false };
            Check(ChatFormat.Plain(record, settings, _ => "").StartsWith(ChatFormat.ShownSender(record, false), StringComparison.Ordinal), "the line starts with what the search takes");
            foreach (var query in new[] { "[1]", "1 SAG", "[SAG] Ann", "sag ann doe", "1 sag ann", "Ann Doe" })
                Check(ChatSearch.Matches(record, query), "found: " + query);
            Check(!ChatSearch.Matches(record, "1SAG"), "never glued as it was");
            Check(!ChatSearch.Matches(record, "[2]"), "another number: not found");
        }),
        ("Chat sender search 1729: capture mode: the stand-in with the number and the job, never the name", () =>
        {
            var record = new ChatRecord(1, Now, 14, ChatChannel.Party, PartySender, [new ChatSegment("hello")]);
            var stand = ChatFormat.StandIn("Ann Doe");
            Check(ChatFormat.ShownSender(record, true) == "[1] [SAG] " + stand && ChatFormat.SenderWords(record, true) == "1 SAG " + stand, ChatFormat.ShownSender(record, true));
            Check(ChatSearch.Matches(record, "[1] [SAG] " + stand, capture: true) && ChatSearch.Matches(record, "1 SAG", capture: true), "found as shown");
            Check(!ChatSearch.Matches(record, "Ann", capture: true), "the name is never found in capture mode");
        }),
        ("Chat sender search 1729: a sender without the game's number, or a number without a job: as the line shows it", () =>
        {
            var say = new ChatRecord(2, Now, 10, ChatChannel.Say, "Ann Doe@Moogle", [new ChatSegment("hi")]);
            Check(ChatFormat.ShownSender(say, false) == "Ann Doe@Moogle" && ChatFormat.SenderWords(say, false) == "Ann Doe@Moogle", "no number: the name as before");
            var numbered = new ChatRecord(3, Now, 14, ChatChannel.Party, "Ab Cd", [new ChatSegment("x")]);
            Check(ChatFormat.ShownSender(numbered, false) == "[1] Ab Cd" && ChatFormat.SenderWords(numbered, false) == "1 Ab Cd", "a number without a job");
        }),
    ];
}
