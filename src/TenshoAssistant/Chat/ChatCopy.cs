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

    /// <summary>X-675 (0.1651): the smallest « Select text » box: it never disappears, whatever the chat window's size.</summary>
    public const float MinimumSelectWidth=240f,MinimumSelectHeight=120f;

    /// <summary>X-675 (0.1651, the user's report of 20/11): the size of the « Select text » box, fixed from the chat window's size, never from
    /// the space left in its popup (an ImGui popup always resizes to its content: a box of the space left, (-1,-1), and the popup shrank each
    /// other frame after frame until the box was gone). As wide as the chat window (320 at least) and as high (200 to 600), less the help
    /// line, the button and the popup's padding; never under <see cref="MinimumSelectWidth"/> × <see cref="MinimumSelectHeight"/>. A size not
    /// known yet (0, not a number) takes the window's first size, 560 × 320.</summary>
    public static (float Width,float Height) SelectBox((float X,float Y) chat,float header,float paddingX,float paddingY)
    {
        static float Known(float value,float fallback)=>float.IsFinite(value) && value>0 ? value:fallback;
        static float Margin(float value)=>float.IsFinite(value) && value>0 ? value:0;
        var width=Math.Max(Known(chat.X,560f),320f)-2*Margin(paddingX);
        var height=Math.Clamp(Known(chat.Y,320f),200f,600f)-Margin(header)-2*Margin(paddingY);
        return (Math.Max(width,MinimumSelectWidth),Math.Max(height,MinimumSelectHeight));
    }

    /// <summary>E3: a player's name with their world as the game writes it outside the chat (« Name@World »); the name alone without one.</summary>
    public static string Player(string name,string? world)
        =>string.IsNullOrWhiteSpace(world) ? name.Trim():name.Trim()+"@"+world.Trim();

    /// <summary>X-473 (0.1627): a sender's name without its world (« Name@World » as the game writes it elsewhere).</summary>
    public static string NameOf(string? sender)
    {
        var name=(sender ?? "").Trim();
        var at=name.IndexOf('@');
        return at>0 ? name[..at].Trim():name;
    }

    /// <summary>X-473 (0.1627): the lines of a player's tooltip: the name and its world (when known), then the job and level when the game shows
    /// them (in the party or nearby; null otherwise: nothing guessed).</summary>
    public static string SenderTip(string name,string world,string? job,int level,Func<string,string,string> withWorld,Func<string,int,string> withJob)
    {
        var first=world.Length>0 ? withWorld(name,world):name;
        return job is { Length:>0 } && level>0 ? first+"\n"+withJob(job,level):first;
    }

    /// <summary>X-669 M2 (review of 0.1627, R25): a player's name as the menus of the chat window write it: their stand-in in capture mode
    /// (<see cref="ChatFormat.StandIn(string?)"/>, as the line), else the name.</summary>
    public static string MenuName(string player,bool capture)=>capture ? ChatFormat.StandIn(player):player;

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
