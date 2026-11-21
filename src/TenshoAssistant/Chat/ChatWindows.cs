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
    /// when it is the last tab left in the chat window.</summary>
    public static int Free(IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab)
    {
        if(!tabs.Contains(tab) || Detached(tabs,tab) || tabs.Count(other=>!Detached(tabs,other))<=1)return 0;
        for(var window=1;window<=Maximum;window++)if(In(tabs,window) is null)return window;
        return 0;
    }

    /// <summary>Detaches the tab into the first free window; false when it cannot (see <see cref="Free"/>).</summary>
    public static bool Detach(IReadOnlyList<ChatTabSettings> tabs,ChatTabSettings tab)
    {
        var window=Free(tabs,tab);
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
    /// on <paramref name="all"/>, every tab whatever the context shows).</summary>
    public static void InChatWindow(IEnumerable<ChatTabSettings> shown,IReadOnlyList<ChatTabSettings> all,List<ChatTabSettings> into)
    {
        into.Clear();
        foreach(var tab in shown)if(!Detached(all,tab))into.Add(tab);
    }
}
