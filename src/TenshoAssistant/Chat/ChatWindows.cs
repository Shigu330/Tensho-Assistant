namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B11 (0.1714, the user's choice of 15/11, as Chattynator and Chat 2): a tab detached into a window of its own, numbered 1 to
/// <see cref="Maximum"/> (each number keeps its own place and size); 0, or a number out of range: in the chat window. A number two tabs ask for
/// (a file edited by hand) belongs to the first; the other stays in the chat window. The chat window always keeps one tab. Pure.
/// </summary>
public static class ChatWindows
{
    public const int Maximum=4;

    /// <summary>The window number a tab asks for (0: the chat window).</summary>
    public static int Of(ChatTabSettings tab)=>tab.Window is >= 1 and <= Maximum ? tab.Window:0;

    /// <summary>The tab drawn in window <paramref name="window"/>: the first asking for it, else null.</summary>
    public static ChatTabSettings? In(IEnumerable<ChatTabSettings> tabs,int window)
    {
        if(window is < 1 or > Maximum)return null;
        foreach(var tab in tabs)if(Of(tab)==window)return tab;
        return null;
    }

    /// <summary>Whether a tab is drawn in a window of its own (it asks for one and is the first to).</summary>
    public static bool Detached(IEnumerable<ChatTabSettings> tabs,ChatTabSettings tab)=>Of(tab)!=0 && ReferenceEquals(In(tabs,Of(tab)),tab);

    /// <summary>Whether any tab is drawn in a window of its own.</summary>
    public static bool Any(IReadOnlyList<ChatTabSettings> tabs)=>tabs.Any(tab=>Detached(tabs,tab));

    /// <summary>The window a tab would go to: the first free number; 0 when it is not in the list, already detached, when none is free, or
    /// when it is the last tab left in the chat window. X-717 M2 (review of 0.1716): with <paramref name="shown"/> (the tabs the context shows),
    /// also when it is the last of those left in the chat window (a context's profile leaves the others out).</summary>
    public static int Free(IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab,IReadOnlyList<ChatTabSettings>? shown=null)
    {
        if(!tabs.Contains(tab) || Detached(tabs,tab) || tabs.Count(other=>!Detached(tabs,other))<=1)return 0;
        if(shown is not null && shown.Contains(tab) && shown.Count(other=>!Detached(tabs,other))<=1)return 0;
        for(var window=1;window<=Maximum;window++)if(In(tabs,window) is null)return window;
        return 0;
    }

    /// <summary>Detaches the tab into the first free window; false when it cannot (see <see cref="Free"/>).</summary>
    public static bool Detach(IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab,IReadOnlyList<ChatTabSettings>? shown=null)
    {
        var window=Free(tabs,tab,shown);
        if(window==0)return false;
        tab.Window=window;
        return true;
    }

    /// <summary>Puts the tab back in the chat window; false when it was there already.</summary>
    public static bool Attach(ChatTabSettings tab)
    {
        if(tab.Window==0)return false;
        tab.Window=0;
        return true;
    }

    /// <summary>The tabs the chat window shows, in their order: those of <paramref name="shown"/> not drawn in a window of their own (decided
    /// on <paramref name="all"/>, every tab whatever the context shows). X-717 M2 (review of 0.1716): when every tab shown has a window of its
    /// own (a context's profile showing only those), the first stays in the chat window (<see cref="Kept"/>): its bar is never empty.</summary>
    public static void InChatWindow(IReadOnlyList<ChatTabSettings> shown,IReadOnlyList<ChatTabSettings> all,List<ChatTabSettings> into)
    {
        into.Clear();
        foreach(var tab in shown)if(!Detached(all,tab))into.Add(tab);
        if(into.Count==0 && Kept(shown,all) is { } kept)into.Add(kept);
    }

    /// <summary>X-717 M2 (review of 0.1716): the tab drawn in the chat window although it asks for a window of its own: the first of
    /// <paramref name="shown"/> when every one of them is detached (its own window hides meanwhile, and it goes back there in a context showing
    /// another tab; its number is kept); else null.</summary>
    public static ChatTabSettings? Kept(IReadOnlyList<ChatTabSettings> shown,IReadOnlyList<ChatTabSettings> all)
    {
        if(shown.Count==0)return null;
        foreach(var tab in shown)if(!Detached(all,tab))return null;
        return shown[0];
    }

    /// <summary>X-717 M2 (review of 0.1716): a tab of the settings (<paramref name="tabs"/>) may be removed: never the only one, nor the last
    /// one left in the chat window, every other in a window of its own (decided on <paramref name="all"/>; a conversation's tab does not count,
    /// it closes); a detached tab may.</summary>
    public static bool Removable(IReadOnlyList<ChatTabSettings> tabs,IReadOnlyList<ChatTabSettings> all,ChatTabSettings tab)
        =>tabs.Count>1 && (Detached(all,tab) || tabs.Any(other=>!ReferenceEquals(other,tab) && !Detached(all,other)));
}
