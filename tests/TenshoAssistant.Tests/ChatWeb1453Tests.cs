using TenshoAssistant.Chat;

// 0.1453 (X-403 phase 2, end): the web addresses of a message's plain text, found by Tensho's chat window (the game sends them as text).
internal static class ChatWeb1453Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Show(IReadOnlyList<(string Text, string? Url)> parts) => string.Join(" | ", parts.Select(part => part.Url is null ? "[" + part.Text + "]" : "<" + part.Text + "=" + part.Url + ">"));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat web 1453: addresses found with their text around; trailing punctuation and unopened brackets left out", () =>
        {
            Check(Show(ChatWeb.Split("see https://example.com/a?b=1, ok")) == "[see ] | <https://example.com/a?b=1=https://example.com/a?b=1> | [, ok]", Show(ChatWeb.Split("see https://example.com/a?b=1, ok")));
            Check(Show(ChatWeb.Split("go www.example.org.")) == "[go ] | <www.example.org=https://www.example.org/> | [.]", Show(ChatWeb.Split("go www.example.org.")));
            Check(Show(ChatWeb.Split("(https://en.wikipedia.org/wiki/Foo_(bar))")) == "[(] | <https://en.wikipedia.org/wiki/Foo_(bar)=https://en.wikipedia.org/wiki/Foo_(bar)> | [)]", Show(ChatWeb.Split("(https://en.wikipedia.org/wiki/Foo_(bar))")));
            Check(Show(ChatWeb.Split("two: http://a.fr and HTTPS://B.ORG/x!")) == "[two: ] | <http://a.fr=http://a.fr/> | [ and ] | <HTTPS://B.ORG/x=https://b.org/x> | [!]", Show(ChatWeb.Split("two: http://a.fr and HTTPS://B.ORG/x!")));
            Check(Show(ChatWeb.Split("« www.site.jp »")) == "[« ] | <www.site.jp=https://www.site.jp/> | [ »]", Show(ChatWeb.Split("« www.site.jp »")));
        }),
        ("Chat web 1453: only http and https with a host; inside a word, alone or too long: text", () =>
        {
            foreach (var text in new[] { "javascript:alert(1)", "ftp://files.example.com", "file:///c:/x", "http://nohost", "abchttps://x.org", "https://", "www.", "http://.org", "mailto:a@b.fr", "https://" + new string('a', 2050) + ".com" })
                Check(ChatWeb.Split(text).All(part => part.Url is null), "text: " + text);
            Check(ChatWeb.Split("").Count == 0 && ChatWeb.Split("plain").Single() == ("plain", null), "plain text whole");
            Check(ChatWeb.Address("https://ex ample.com") is null, "a space: not an address");
        }),
        ("Chat web 1453: a message's plain text gets its web links; a game link's text keeps its link", () =>
        {
            var place = new ChatLink(ChatSegmentKind.MapLink, 1, 2, 3, 4, "here");
            var body = ChatPieces.Segments([new ChatPiece("look https://x.org now "), new ChatPiece(null, place), new ChatPiece("www.inside.link"), new ChatPiece(null, End: true)]);
            Check(body.Count == 4, "segments: " + body.Count);
            Check(body[0] == new ChatSegment("look ") && body[1].Kind == ChatSegmentKind.WebLink && body[1].Text == "https://x.org" && body[1].Link!.Url == "https://x.org/", "the address");
            Check(body[2] == new ChatSegment(" now ") && body[3].Kind == ChatSegmentKind.MapLink && body[3].Text == "www.inside.link", "the game link's text untouched");
            var record = new ChatRecord(1, DateTimeOffset.UtcNow, 10, ChatChannel.Say, "Someone", body);
            var runs = ChatFormat.Runs(record, new ChatSettings { ShowTimestamps = false }, _ => "");
            Check(runs.Any(run => run.Role == ChatRole.Link && run.Link?.Kind == ChatSegmentKind.WebLink), "drawn as a link");
        }),
        ("Chat web 1453: the window opens the browser only from the address's menu", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            Check(window.Split("Dalamud.Utility.Util.OpenLink(").Length == 2, "one place opens a web page");
            var menu = window.IndexOf("ImGui.BeginPopup(\"###TenshoChatWeb\")", StringComparison.Ordinal);
            Check(menu > 0 && window.IndexOf("Dalamud.Utility.Util.OpenLink(address)", menu, StringComparison.Ordinal) > menu, "in the menu, after the address is shown");
            Check(window.Contains("{chatWindowWeb=link.Url;ImGui.OpenPopup(\"###TenshoChatWeb\");}", StringComparison.Ordinal), "a click opens the menu");
        }),
    ];
}
