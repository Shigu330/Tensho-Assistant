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
    /// <param name="competitive">Player-versus-player or competitive content (DEC-038): given back, even with the game's interface hidden. X-534 M2
    /// (0.1509): in the frame, a game chat window that is not shown holds first (InFrame): never shown again over a hidden chat, given back as
    /// soon as it shows again.</param>
    /// <param name="gameUiHidden">The game hides its interface (cutscene, GPose, interface hidden): the messages stay as they are.</param>
    /// <remarks>X-689 (0.1657, the user's report of 20/11: « each time, the official chat comes back »): while Tensho's chat replaces the game's,
    /// Tensho's window hiding itself (« Hide in combat », in a duty, in a cutscene; a game window over it with « Under a game window » set to
    /// « Hide »; faded) no longer gives the game's messages back, as 0.1506 and 0.1507 M1 did: they come back only when the option is off, Tensho's
    /// window closed or failed, « both chats », in a competitive cut that gives the hand back to the game (R03.2, DEC-038), or at unload. The
    /// journal of 20/11 22:58 showed why it mattered: the game's own item tooltip (ItemDetail), opened over Tensho's window, hid it and brought
    /// the game's chat back, again and again (docs/evidence/0.1657-game-chat-stays-hidden.md).</remarks>
    public static ChatHideWant Of(bool wanted,bool competitive,bool gameUiHidden)
        =>!wanted || competitive ? ChatHideWant.Release
            : gameUiHidden ? ChatHideWant.Hold
            : ChatHideWant.Hide;

    /// <summary>X-696 M2 (review of 0.1658): since X-689 the game's messages stay hidden while Tensho's window hides, so « Hide in combat » or
    /// « Hide in duties » with « Hide the game chat's messages » (not both chats) leaves no chat shown there: said under those settings, the
    /// user's choice kept.</summary>
    public static bool LeavesNoChat(bool hideGameMessages,bool bothChats,bool hideInCombat,bool hideInDuty)
        =>hideGameMessages && !bothChats && (hideInCombat || hideInDuty);

    /// <summary>X-534 M2 (0.1509, review of 0.1507): the choice the hider applies in a frame, the one place where it is written. A competitive
    /// cut gives back (DEC-038), whatever Tensho wished; but while the game's chat window itself is not shown (cutscene, opening scene of a
    /// PvP match, interface hidden), nothing is touched (Hold, X-527): the panels Tensho hid come back as soon as ChatLog shows again.</summary>
    public static ChatHideWant InFrame(ChatHideWant wished,bool competitiveCut,bool chatLogShown)
        =>!chatLogShown ? ChatHideWant.Hold
            : competitiveCut ? ChatHideWant.Release
            : wished;

    /// <summary>X-534 M1 (0.1509): the box of the game's messages is read in every frame the game's chat is shown, also while they are given
    /// back (a game window over Tensho's, combat): when Tensho's window takes their place again, the box is there in that very frame, and the
    /// window is never drawn for a frame at the player's own place (a blink, and a place another game window could cover).</summary>
    public static bool ReadsPlace(ChatHideWant now)=>now!=ChatHideWant.Hold;

    /// <summary>X-540 M1 (review of 0.1510): the place is read only while the option is wanted (« Hide the game chat's messages » on, Tensho's
    /// chat window open and working) and outside a competitive cut (DEC-038: all of Tensho stops): with the option off, its default, nothing
    /// is read, nothing is logged, and a reading error never stops the hider of a player who does not use it. While it is wanted but the
    /// messages are given back (a game window over Tensho's, combat), the place is still read (X-534 M1).</summary>
    public static bool OptionReadsPlace(bool optionWanted,bool competitiveCut)=>optionWanted && !competitiveCut;
}

/// <summary>
/// X-467 (0.1504, D-181 of 16/11, DEC-053 completed): « Hide the game chat's messages », off by default. Only the messages (the four panels,
/// ChatLogPanel_0 to _3) are hidden, never the game's typing box (ChatLog). What is hidden is given back as soon as it is no longer wanted
/// (the option off, the Tensho chat window closed, « both chats », competitive content, Tensho unloaded; X-689, 0.1657: no longer when Tensho's
/// window hides itself). Pure: it decides which
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

    public static ChatLogBox? Box(IReadOnlyList<ChatLogPanelBox> panels,ChatLogBox window,ChatLogBox? input)=>Box(panels,window,input,null);

    /// <summary>X-533 (0.1508, the user's request of 16/11): the frame of the game's messages and tabs, Tensho's window taking exactly their
    /// place. Top: ChatLog's own top when the game draws its tabs there, above the panel (within the panel's upper half); else the panel's.
    /// Bottom: the highest plausible top of the typing row, the typing box or the current channel's name (« Say » above the typing line, cut
    /// by Tensho's window on the capture of 16/11 23 h 10), each one used only inside ChatLog and in the panel's lower half.</summary>
    public static ChatLogBox? Box(IReadOnlyList<ChatLogPanelBox> panels,ChatLogBox window,ChatLogBox? input,ChatLogBox? channelName)
    {
        if(panels.Count==0)return null;
        var panel=panels.FirstOrDefault(each=>each.Index==0,panels[0]).Box;
        if(panel.Width<MinimumSide || panel.Height<MinimumSide)return null;
        var top=window.Y<panel.Y && panel.Y-window.Y<panel.Height/2 ? window.Y:panel.Y;
        var bottom=panel.Y+panel.Height;
        foreach(var row in new[]{input,channelName})
            if(row is { } typing && typing.Inside(window) && typing.Y>panel.Y+panel.Height/2 && typing.Y<bottom)bottom=typing.Y;
        return new ChatLogBox(panel.X,top,panel.Width,bottom-top);
    }

    /// <summary>X-533 (0.1508): the game's channel menu opens upward from the typing box, over the place of the messages, where Tensho's window
    /// is: a window drawn by Dalamud is always over the game's, so the menu would be behind it. While Tensho's window takes the messages' place
    /// and that menu is open, the window is not drawn (the game's messages stay hidden: nothing blinks); back when the menu closes.</summary>
    public static bool StepsAside(bool following,bool channelMenuOpen)=>following && channelMenuOpen;
}

/// <summary>X-533 (0.1508, the user's choice of 16/11 around 23 h 20): the parts of the game chat's tab bar Tensho hides with the messages (nodes of
/// ChatLog, FFXIVClientStructs' AddonChatLog): its five tabs (ChatTabs), the « add a tab » button, the bar's two end images and the settings
/// button. Its typing box and channel menu are never hidden. Each part is a slot of a ChatGameHide (the same rules: only what Tensho hid is
/// given back).</summary>
public static class ChatTabBar
{
    public const int Tabs=5,AddTab=5,BarStart=6,BarEnd=7,Settings=8,Count=9;

    /// <summary>A slot is read only when its node exists; a tab only below the game's tab count (the other tab nodes are the game's own,
    /// hidden by it).</summary>
    public static bool Ready(int slot,int tabCount,bool node)=>node && (slot>=Tabs || slot<tabCount);
}
