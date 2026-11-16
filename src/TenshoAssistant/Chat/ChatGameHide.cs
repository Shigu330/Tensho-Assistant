namespace TenshoAssistant.Chat;

/// <summary>X-467 (0.1504): one of the game's message panels (ChatLogPanel_0 to _3) as the hider reads it in a frame: ready, shown.</summary>
public readonly record struct ChatPanelState(int Index,bool Ready,bool Visible);

/// <summary>
/// X-467 (0.1504, D-181 of 16/11, DEC-053 completed): « Hide the game chat's messages », off by default. Only the messages (the four panels,
/// ChatLogPanel_0 to _3) are hidden, never the game's typing box (ChatLog). What is hidden is given back as soon as it is no longer wanted
/// (the option off, the Tensho chat window closed or not shown, « both chats », competitive content, Tensho unloaded). Pure: it decides which
/// panels to hide and which to show again in a frame; the native side (GameChatHider) does it. It only ever shows again a panel it hid itself.
/// </summary>
public sealed class ChatGameHide
{
    private readonly SortedSet<int> hidden=[];

    /// <summary>The panels hidden by Tensho, to give back.</summary>
    public IReadOnlyCollection<int> Hidden=>hidden;

    /// <summary>One frame: <paramref name="wanted"/> says whether the messages are to be hidden now. Hides each ready panel the game shows;
    /// when no longer wanted, shows again each panel Tensho hid (a panel no longer ready was rebuilt by the game: nothing to give back).</summary>
    public (IReadOnlyList<int> Hide,IReadOnlyList<int> Show) Step(bool wanted,IReadOnlyList<ChatPanelState> panels)
    {
        var hide=new List<int>();var show=new List<int>();
        if(wanted)
        {
            foreach(var panel in panels)
                if(panel.Ready && panel.Visible){hide.Add(panel.Index);hidden.Add(panel.Index);}
            return (hide,show);
        }
        if(hidden.Count==0)return (hide,show);
        foreach(var index in hidden)
            if(panels.Any(panel=>panel.Index==index && panel.Ready))show.Add(index);
        hidden.Clear();
        return (hide,show);
    }

    /// <summary>Everything to give back now (Tensho unloaded): the panels hidden by Tensho; forgets them.</summary>
    public IReadOnlyList<int> GiveBack()
    {
        var all=hidden.ToList();
        hidden.Clear();
        return all;
    }
}

/// <summary>
/// X-467 (0.1504): where the Tensho chat window goes when it takes the place of the game's messages: the box of the game's message panel
/// (a window's own box, read from X, Y and GetSize), down to the top of the game's typing box when that box is plausible (inside ChatLog and in
/// the panel's lower half), so the game's typing row stays visible under it. The boxes of nested nodes are not verified (X-525 M2,
/// docs/evidence/0.1502-game-chat-input.md): a typing box out of those bounds is not used, and the window keeps the panel's whole box. Pure.
/// </summary>
public static class ChatFollow
{
    public const float MinimumSide=40;

    public static ChatLogBox? Box(IReadOnlyList<ChatLogPanelBox> panels,ChatLogBox window,ChatLogBox? input)
    {
        if(panels.Count==0)return null;
        var panel=panels.FirstOrDefault(each=>each.Index==0,panels[0]).Box;
        if(panel.Width<MinimumSide || panel.Height<MinimumSide)return null;
        var bottom=panel.Y+panel.Height;
        if(input is { } typing && typing.Inside(window) && typing.Y>panel.Y+panel.Height/2 && typing.Y<bottom)bottom=typing.Y;
        return new ChatLogBox(panel.X,panel.Y,panel.Width,bottom-panel.Y);
    }
}
