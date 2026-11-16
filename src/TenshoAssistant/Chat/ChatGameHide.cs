namespace TenshoAssistant.Chat;

/// <summary>X-467 (0.1504): one of the game's message panels (ChatLogPanel_0 to _3) as the hider reads it in a frame: ready, shown.
/// X-528 M2 (0.1507): Under, the panel lies under Tensho's window (panel 0, or a docked tab at its place): only those are hidden.</summary>
public readonly record struct ChatPanelState(int Index,bool Ready,bool Visible,bool Under=true);

/// <summary>X-527 (0.1506, review of 0.1504, I1): what the hider does with the game's messages in a frame. Hide: hidden (Tensho's window in
/// their place). Hold: left exactly as they are, neither hidden more nor given back (the game hides its own interface: cutscene, GPose,
/// interface hidden, its chat not shown); before 0.1506 Tensho showed them again there, over the cutscene. Release: given back.</summary>
public enum ChatHideWant { Release, Hold, Hide }

/// <summary>X-527 (0.1506): the choice of each frame, from what Tensho knows. Pure.</summary>
public static class ChatHideWants
{
    /// <param name="wanted">The option on, « both chats » off, Tensho's chat window open, working and not unloading.</param>
    /// <param name="competitive">Player-versus-player or competitive content (DEC-038): always given back, the whole of Tensho is cut.</param>
    /// <param name="gameUiHidden">The game hides its interface (cutscene, GPose, interface hidden): the messages stay as they are.</param>
    /// <param name="combatHidden">« Hide in combat » on and in combat: Tensho's window goes, the game's messages come back as the chat of the fight.</param>
    public static ChatHideWant Of(bool wanted,bool competitive,bool gameUiHidden,bool combatHidden)=>Of(wanted,competitive,gameUiHidden,combatHidden,false);

    /// <param name="coveredHidden">X-528 M1 (0.1507): a game window covers Tensho's chat window and « Under a game window » hides it: the game's
    /// messages come back, drawn by the game under its own window, so the part it does not cover stays readable.</param>
    public static ChatHideWant Of(bool wanted,bool competitive,bool gameUiHidden,bool combatHidden,bool coveredHidden)
        =>!wanted || competitive ? ChatHideWant.Release
            : gameUiHidden ? ChatHideWant.Hold
            : combatHidden || coveredHidden ? ChatHideWant.Release
            : ChatHideWant.Hide;
}

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
        =>Step(wanted ? ChatHideWant.Hide:ChatHideWant.Release,panels);

    /// <summary>X-527 (0.1506): one frame with the three choices; Hold touches nothing and keeps the panels Tensho hid to give back later.</summary>
    public (IReadOnlyList<int> Hide,IReadOnlyList<int> Show) Step(ChatHideWant want,IReadOnlyList<ChatPanelState> panels)
    {
        var hide=new List<int>();var show=new List<int>();
        if(want==ChatHideWant.Hold)return (hide,show);
        if(want==ChatHideWant.Hide)
        {
            foreach(var panel in panels)
                if(panel.Ready && panel.Visible && panel.Under){hide.Add(panel.Index);hidden.Add(panel.Index);}
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

    /// <summary>X-528 M2 (0.1507): whether a message panel lies under Tensho's window: panel 0 always; another one when at least half of
    /// its box lies over panel 0's (the game keeps the docked tabs' panels at panel 0's place, docs/evidence/0.1502-game-chat-input.md); a tab
    /// detached elsewhere on the screen is not, and stays shown.</summary>
    public static bool Under(int index,ChatLogBox panel,ChatLogBox? main)
    {
        if(index==0)return true;
        if(main is not { } box || panel.Width<=0 || panel.Height<=0)return false;
        var width=Math.Min(panel.X+panel.Width,box.X+box.Width)-Math.Max(panel.X,box.X);
        var height=Math.Min(panel.Y+panel.Height,box.Y+box.Height)-Math.Max(panel.Y,box.Y);
        return width>0 && height>0 && width*height*2>=panel.Width*panel.Height;
    }

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
