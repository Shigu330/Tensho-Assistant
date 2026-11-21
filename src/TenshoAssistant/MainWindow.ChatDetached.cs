using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Chat;

namespace TenshoAssistant;

// X-447 B11 (0.1714, the user's choice of 15/11, as Chattynator and Chat 2): a chat tab detached into a window of its own, beside the chat
// window: its pins and its lines, its own place and size, the chat window's look (background, frame, lock; the tab's own text size). A right
// click on a tab detaches it; the button at its top, its name's menu or its cross put it back. B12 (WIM's « Exposé »): every Tensho chat window
// hidden for a moment, by « /tensho chat hide » or a tab's menu; « /tensho chat » or the same again shows them back, nothing is closed meanwhile
// (the messages are still received). A detached window hides as the chat window (competitive cut, the game's interface hidden, combat,
// cutscene, duty, a context's profile without its tab), and steps aside behind a game window as Tensho's other panels.
internal sealed partial class MainWindow
{
    // X-447 B12 (0.1714): every chat window hidden for a moment (never saved).
    private volatile bool chatAllHidden;

    /// <summary>X-447 B12 (0.1714): « /tensho chat hide »: every Tensho chat window hidden, or shown back.</summary>
    internal void ToggleChatWindowsHidden()=>chatAllHidden=!chatAllHidden;

    // X-447 B11 (0.1714): each detached window's state between frames, and every tab (the settings' and the conversations'), filled again on use.
    private readonly ChatPane[] chatPanes=[.. Enumerable.Range(0,ChatWindows.Maximum).Select(_=>new ChatPane())];
    private readonly List<ChatTabSettings> chatAllTabs=[],chatMainTabs=[];

    /// <summary>X-447 B11 (0.1714): the panels of the detached windows, one per number (created once, shown while a tab asks for it).</summary>
    internal PanelDefinition[] ChatDetachedPanels()=>[.. Enumerable.Range(1,ChatWindows.Maximum).Select(ChatDetachedPanel)];

    private PanelDefinition ChatDetachedPanel(int window)=>new("Chat "+window,"###TenshoChatWindow"+window,new Vector2(620,420-window*40),
        ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoBringToFrontOnFocus,
        ()=>ChatOptions.WindowOpen && !inDesktopApp && ChatDetachedTab(window) is not null,()=>ChatWindowShown() && !chatAllHidden,()=>DrawChatDetached(window),
        ()=>AttachChatWindow(window),()=>ChatDetachedTitle(window),Opacity:()=>ChatOptions.BoundedOpacity/100f,Size:new Vector2(420,240),
        MoreFlags:ChatDetachedFlags,NoBorder:()=>ChatOptions.Borderless,Fade:()=>ChatDetachedFade(window),Padding:new Vector2(ChatLook.Padding.X,ChatLook.Padding.Y),
        Frame:ChatFrame,Early:()=>WarmupChatDone);

    /// <summary>X-447 B11 (0.1714): every tab, the settings' then the conversations', whatever the context shows (a window number is decided on
    /// all of them).</summary>
    private List<ChatTabSettings> ChatAllTabs(ChatSettings options)
    {
        chatAllTabs.Clear();
        chatAllTabs.AddRange(ChatTabs(options));
        if(options.Conversations)chatAllTabs.AddRange(chatConversations.Tabs);
        return chatAllTabs;
    }

    /// <summary>X-447 B11 (0.1714): the tab drawn in window n, when the context shows it (a context's profile can leave it out); else null.
    /// X-717 M2 (review of 0.1716): null too while the chat window keeps it, every tab the context shows being detached (ChatWindows.Kept).</summary>
    private ChatTabSettings? ChatDetachedTab(int window)
    {
        var options=ChatOptions;
        var all=ChatAllTabs(options);
        if(ChatWindows.In(all,window) is not { } tab
            || tab.Conversation is null && !ChatContexts.ShowsTab(ChatContexts.ProfileOf(options,chatContextNow),tab))return null;
        return ReferenceEquals(ChatWindows.Kept(ChatWindowTabs(options),all),tab) ? null:tab;
    }

    /// <summary>X-447 B11 (0.1714): the window's title: its tab's name, its unread count and its alert mark, as on the tab.</summary>
    private string ChatDetachedTitle(int window)
    {
        if(ChatDetachedTab(window) is not { } tab)return localization.Get("ChatWindow.Title");
        var view=ChatMessages.ViewOf(tab);
        return ChatAlert.Mark(ChatFormat.TabLabel(ChatConversations.Label(tab,ChatOptions.CaptureNames),tab.ShowUnread,view.Unread),view.Alerts);
    }

    /// <summary>X-447 B11 (0.1714): the chat window's frame and lock settings, for a detached window too.</summary>
    private ImGuiWindowFlags ChatDetachedFlags()=>(ChatOptions.Borderless ? ImGuiWindowFlags.NoTitleBar:ImGuiWindowFlags.None)
        |(ChatOptions.Locked ? ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize:ImGuiWindowFlags.None);

    /// <summary>X-447 B11 (0.1714): the text's opacity while the mouse is not on the window, as the chat window's (ChatLook.Alpha).</summary>
    private float? ChatDetachedFade(int window)
    {
        var pane=chatPanes[window-1];
        var mouse=ImGui.GetMousePos();
        var hovered=pane.At is { } at && ChatLook.Under((mouse.X,mouse.Y),(at.X,at.Y),(pane.Size.X,pane.Size.Y));
        return ChatLook.Alpha(null,ChatOptions.BoundedTextOpacity,hovered || chatMenuOpen);
    }

    /// <summary>X-447 B11 (0.1714): a detached window: the button that puts its tab back and the tab's name (its menu on a right click), then the
    /// tab's pins and lines, drawn by the chat window's own code with this window's state swapped in.</summary>
    private void DrawChatDetached(int window)
    {
        if(!WarmupChatDone){EmptyState("ChatWindow.Loading");return;}
        var options=ChatOptions;
        if(ChatDetachedTab(window) is not { } tab)return;
        var pane=chatPanes[window-1];
        pane.At=ImGui.GetWindowPos();pane.Size=ImGui.GetWindowSize();pane.Tab=tab;
        var view=ChatMessages.ViewOf(tab);
        view.Refresh(ChatMessages,tab,options.BoundedMaxLines,ChatFilterTableNow(),options);
        if(ToolButton(Dalamud.Interface.FontAwesomeIcon.SignInAlt,"ChatWindow.AttachTip",true)){AttachChatWindow(window);return;}
        ImGui.SameLine();
        var coloured=PushChatTabColor(tab);
        try{ImGui.TextUnformatted(ChatDetachedTitle(window));}
        finally{if(coloured)ImGui.PopStyleColor();}
        if(ImGui.BeginPopupContextItem("###ChatDetachedMenu"))
        {
            try{DrawChatDetachedMenu(tab,window);}
            finally{ImGui.EndPopup();}
        }
        pane.Zones.Clear();
        SwapChatPane(pane);
        try{DrawChatPins(tab,options);DrawChatLines(view,options,0,view.Serial);}
        finally{SwapChatPane(pane);}
    }

    /// <summary>X-447 B11 (0.1714): the menu of a detached window's name: put back, clear or show again, close a conversation, hide every window.</summary>
    private void DrawChatDetachedMenu(ChatTabSettings tab,int window)
    {
        if(ImGui.Selectable(localization.Get("ChatWindow.Attach")))AttachChatWindow(window);
        if(ImGui.Selectable(localization.Get("ChatWindow.ClearTab")))ClearChatTab(tab,true);
        if(tab.ClearedAt is not null && ImGui.Selectable(localization.Get("ChatWindow.UnclearTab")))ClearChatTab(tab,false);
        if(tab.Conversation is not null && ImGui.Selectable(localization.Get("ChatWindow.CloseConversation")) && chatConversations.Close(tab))ChatConversationClosed(tab);
        if(ImGui.Selectable(localization.Get("ChatWindow.HideAll")))chatAllHidden=true;
        Tip("ChatWindow.HideAllTip");
    }

    /// <summary>X-447 B11 (0.1714): the entry of a tab's menu that detaches it; greyed (and said why) when no window is free, or for the last tab
    /// of the chat window. B12: with a tab in a window of its own, every chat window hidden for a moment (« /tensho chat » shows them back).</summary>
    private void DrawChatDetachEntry(ChatTabSettings tab)
    {
        var all=ChatAllTabs(ChatOptions);
        // X-717 M2 (review of 0.1716): nor the last tab the context shows in the chat window.
        var free=ChatWindows.Free(all,tab,ChatWindowTabs(ChatOptions))!=0;
        var any=ChatWindows.Any(all);
        ImGui.BeginDisabled(!free);
        try{if(ImGui.Selectable(localization.Get("ChatWindow.Detach")))DetachChatTab(tab);}
        finally{ImGui.EndDisabled();}
        if(free)Tip("ChatWindow.DetachTip");
        else TipText(()=>localization.Format("ChatWindow.DetachNoneTip",ChatWindows.Maximum));
        if(!any)return;
        if(ImGui.Selectable(localization.Get("ChatWindow.HideAll")))chatAllHidden=true;
        Tip("ChatWindow.HideAllTip");
    }

    /// <summary>X-447 B11 (0.1714): a tab of the chat window into the first free window of its own; saved (a conversation's tab never is).</summary>
    private void DetachChatTab(ChatTabSettings tab)
    {
        if(!ChatWindows.Detach(ChatAllTabs(ChatOptions),tab,ChatWindowTabs(ChatOptions)))return;
        if(tab.Conversation is null)TrySave("Chat window tabs");
    }

    /// <summary>X-447 B11 (0.1714): the tab of window n back in the chat window, shown there; saved (a conversation's tab never is).</summary>
    private void AttachChatWindow(int window)
    {
        if(ChatWindows.In(ChatAllTabs(ChatOptions),window) is not { } tab || !ChatWindows.Attach(tab))return;
        chatSelectTab=tab;
        if(tab.Conversation is null)TrySave("Chat window tabs");
    }

    /// <summary>
    /// X-447 B11 (0.1714): what the lines of one window keep between frames: its size, its line height, the wheel's rest, its tab, the menus
    /// opened on its lines and their site, the text to select, the pin clicked, the developer's scroll watch, the zones answering the mouse. The
    /// chat window's live in its own fields; a detached window's are swapped in while its lines are drawn, then swapped back, so one window's
    /// menu is never closed by the other's frame.
    /// </summary>
    private sealed class ChatPane
    {
        public Vector2? At;
        public Vector2 Size;
        public float LineHeight;
        public object? WheelView;
        public float WheelRest;
        public ChatTabSettings? Tab;
        public ChatRecord? Line;
        public (Maps.MapDefinition Map,Vector2 Coordinate,string Place)? Place;
        public string? Web;
        public WebSite? Site;
        public string? Quest;
        public string? SelectText;
        public bool SelectOpen;
        public long ScrollTo=-1;
        public ChatScrollWatch ScrollWatch=new();
        public List<(float X,float Y,float Width,float Height)> Zones=[];
    }

    /// <summary>X-447 B11 (0.1714): the pane's state and the chat window's fields exchanged (twice: back as they were).</summary>
    private void SwapChatPane(ChatPane pane)
    {
        (chatSize,pane.Size)=(pane.Size,chatSize);
        (chatLineHeight,pane.LineHeight)=(pane.LineHeight,chatLineHeight);
        (chatWheelView,pane.WheelView)=(pane.WheelView,chatWheelView);
        (chatWheelRest,pane.WheelRest)=(pane.WheelRest,chatWheelRest);
        (chatShownTab,pane.Tab)=(pane.Tab,chatShownTab);
        (chatWindowLine,pane.Line)=(pane.Line,chatWindowLine);
        (chatWindowPlace,pane.Place)=(pane.Place,chatWindowPlace);
        (chatWindowWeb,pane.Web)=(pane.Web,chatWindowWeb);
        (chatWindowWebSite,pane.Site)=(pane.Site,chatWindowWebSite);
        (chatWindowQuest,pane.Quest)=(pane.Quest,chatWindowQuest);
        (chatSelectText,pane.SelectText)=(pane.SelectText,chatSelectText);
        (chatSelectOpen,pane.SelectOpen)=(pane.SelectOpen,chatSelectOpen);
        (chatScrollTo,pane.ScrollTo)=(pane.ScrollTo,chatScrollTo);
        (chatScrollWatch,pane.ScrollWatch)=(pane.ScrollWatch,chatScrollWatch);
        (chatZonesNext,pane.Zones)=(pane.Zones,chatZonesNext);
    }
}
