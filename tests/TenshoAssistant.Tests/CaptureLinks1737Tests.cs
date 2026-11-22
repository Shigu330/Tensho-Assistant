using TenshoAssistant.Chat;

// X-754 (0.1737, idea I-050 chosen by the user on 21/11, R25): the chat map links' tooltip in capture mode: the chat window's stand-in, never the name.
internal static class CaptureLinks1737Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatRecord Said(string sender) => new(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 10, ChatChannel.Say, sender, [new ChatSegment("here")]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Capture links 1737: capture mode: the link's sender is the chat window's stand-in for that player, never the name", () =>
        {
            const string sender = "Ann Doe@Ultros";
            var shown = ChatFormat.LinkSender(sender, capture: true);
            Check(shown.StartsWith('#') && shown.Length == 6 && !shown.Contains("Ann", StringComparison.OrdinalIgnoreCase) && !shown.Contains("Ultros", StringComparison.Ordinal), "a code, no name nor world: " + shown);
            Check(shown == ChatFormat.ShownSender(Said(sender), capture: true), "the chat window's code for the same player");
            Check(ChatFormat.LinkSender("ann doe", capture: true) == shown, "the same player however written (capitals, without the world)");
            Check(ChatFormat.LinkSender("Bob Roe@Ultros", capture: true) != shown, "another player: another code");
            Check(ChatFormat.LinkSender(sender, capture: false) == "Ann Doe@Ultros", "off: the name and world, as before");
        }),
        ("Capture links 1737: a party member's number and job stay, as in the chat window", () =>
        {
            var party = "\uE090SAG Ann Doe";
            var shown = ChatFormat.LinkSender(party, capture: true);
            Check(shown.StartsWith("[1] [SAG] #", StringComparison.Ordinal) && !shown.Contains("Ann", StringComparison.Ordinal), shown);
            Check(shown == ChatFormat.ShownSender(Said(party), capture: true), "as the chat window");
            Check(ChatFormat.LinkSender("", capture: true) == "", "no sender: empty (the page writes its own word)");
        }),
        ("Capture links 1737: wiring: the watcher keeps the sender as the chat reader does; the page writes it by the capture rule", () =>
        {
            Check(Read("Maps/ChatMapLinkWatcher.Dalamud.cs").Contains("Chat.ChatPayloads.Text(message.Sender.Payloads.Select(Chat.ChatReader.Payload)).Trim()", StringComparison.Ordinal), "the sender with its world, as the chat window reads it");
            Check(Read("MainWindow.ChatMapLinks.cs").Contains("Chat.ChatFormat.LinkSender(link.Sender,ChatOptions.CaptureNames)", StringComparison.Ordinal), "the tooltip");
        }),
    ];
}
