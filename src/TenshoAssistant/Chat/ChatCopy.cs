namespace TenshoAssistant.Chat;

/// <summary>
/// X-455 (0.1617, the user's request of 15/11 « copy paste and the rest » and their choice of E2, E3, E4, E7 the same evening): what the chat
/// window copies and what a cleared tab keeps. Pure (tests); MainWindow reads the keys and the menus.
/// </summary>
public static class ChatCopy
{
    /// <summary>E2: what Ctrl+C copies: the whole tab once Ctrl+A chose it, else the message under the mouse; null: nothing.</summary>
    public static IReadOnlyList<ChatRecord>? Chosen(bool wholeTab,ChatRecord? hovered,Func<IEnumerable<ChatRecord>> tab)
        =>wholeTab ? tab().ToArray():hovered is { } one ? [one]:null;

    /// <summary>E3: a player's name with their world as the game writes it outside the chat (« Name@World »); the name alone without one.</summary>
    public static string Player(string name,string? world)
        =>string.IsNullOrWhiteSpace(world) ? name.Trim():name.Trim()+"@"+world.Trim();

    /// <summary>E3: the world written after « @ » in a sender's name, or nothing.</summary>
    public static string WorldOf(string? sender)
    {
        if(string.IsNullOrWhiteSpace(sender))return "";
        var at=sender.IndexOf('@');
        return at>0 ? sender[(at+1)..].Trim():"";
    }

    /// <summary>E7: whether a message stays in a tab cleared at <paramref name="cleared"/> (the messages that came after; none cleared: all).</summary>
    public static bool AfterClear(ChatRecord record,DateTimeOffset? cleared)=>cleared is not { } at || record.Time>at;
}
