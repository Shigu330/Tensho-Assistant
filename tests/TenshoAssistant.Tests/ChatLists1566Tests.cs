using TenshoAssistant.Chat;

// 0.1566 (X-508, the user's remark of 16/11: « Only with », « Hide with », hidden and highlighted players looked like one box for one name):
// each list is drawn line by line, with « remove » on each line and an « Add » box; still saved with commas, so older values come back as lines.
internal static class ChatLists1566Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat lists 1566: a value saved with commas comes back line by line", () =>
        {
            var lines = ChatTextFilter.Words("Ann Doe, Bob Roe ,,  Cid Moe");
            Check(lines.SequenceEqual(new[] { "Ann Doe", "Bob Roe", "Cid Moe" }), "older value not read line by line: " + string.Join("|", lines));
        }),
        ("Chat lists 1566: add a line, several at once, refused when empty, doubled or too long", () =>
        {
            Check(ChatTextFilter.Add(null, "Ann Doe", 600) == "Ann Doe", "first line");
            Check(ChatTextFilter.Add("Ann Doe", "Bob Roe", 600) == "Ann Doe, Bob Roe", "second line");
            Check(ChatTextFilter.Add("Ann Doe", "bob roe, cid moe", 600) == "Ann Doe, bob roe, cid moe", "two at once");
            Check(ChatTextFilter.Add("Ann Doe", "ann doe", 600) is null, "a double (capitals ignored) is not added");
            Check(ChatTextFilter.Add("Ann Doe", "  ", 600) is null, "an empty line is not added");
            Check(ChatTextFilter.Add("Ann Doe", "Bob Roe", 10) is null, "past the maximum length: refused, the list unchanged");
        }),
        ("Chat lists 1566: remove a line by its place or by its name", () =>
        {
            Check(ChatTextFilter.Remove("Ann Doe, Bob Roe, Cid Moe", 1) == "Ann Doe, Cid Moe", "middle line");
            Check(ChatTextFilter.Remove("Ann Doe", 0) == "", "last line");
            Check(ChatTextFilter.Remove("Ann Doe, Bob Roe", 5) == "Ann Doe, Bob Roe", "an index out of the list changes nothing");
            Check(ChatTextFilter.Remove("Ann Doe, Bob Roe", "bob roe") == "Ann Doe", "by name, capitals ignored");
        }),
        ("Chat lists 1566: the sender's name without its world, for the right-click menu", () =>
        {
            Check(ChatTextFilter.PlayerName("Ann Doe@Moogle") == "Ann Doe", "world kept");
            Check(ChatTextFilter.PlayerName(" Ann Doe ") == "Ann Doe", "spaces kept");
            Check(ChatTextFilter.PlayerName("") == "" && ChatTextFilter.PlayerName(null) == "", "no sender: no menu line");
            var added = ChatTextFilter.Add(null, ChatTextFilter.PlayerName("Ann Doe@Moogle"), 600);
            Check(ChatTextFilter.IsPlayer("Ann Doe@Moogle", ChatTextFilter.Words(added)), "a player hidden from the menu is then recognised");
        }),
        ("Chat lists 1566: the four lists and the two words of a tab are drawn as lists, the menu offers hide and put forward", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            foreach (var key in new[] { "KeepWords", "HideWords", "HiddenPlayers", "HighlightedPlayers", "AlertWords" })
            {
                Check(code.Contains("ListSetting(\"ChatWindow." + key + "\""), key + " is not drawn as a list");
                Check(!code.Contains("TextSetting(\"ChatWindow." + key + "\""), key + " is still one box of text");
            }
            Check(code.Contains("DrawChatSenderMenu(options,said);"), "the message's menu does not offer the sender's lines");
        }),
    ];
}
