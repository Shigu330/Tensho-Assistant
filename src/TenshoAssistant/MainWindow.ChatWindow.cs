using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Chat;

namespace TenshoAssistant;

// X-403 phase 1 (D-166, 15/11; the 0.471 window, removed in 0.472 by DEC-030, brought back): the Tensho chat window, beside the game's chat. It only
// reads: tabs by channel, colours, time, unread counts. The game's chat stays as it is (never hidden), nothing is sent, nothing is written to disk
// (the messages stay in memory, Chat/ChatStore.cs). A panel window (PanelWindow.cs): not drawn during a competitive cut, hidden with the game's
// interface. It draws only the rows in view; each message is laid out once per width, the newest first and a few per frame.
internal sealed partial class MainWindow
{
    internal ChatStore ChatMessages{get;}=new();
    // X-447 B7 (0.1647): the alerts not seen in any tab (for the server bar, read on the game's thread), and a click on it waiting.
    private readonly HashSet<long> chatUnseen=[];
    private int chatAlertsUnseen;
    private bool chatShowAlert;
    /// <summary>X-447 B7 (0.1647): the messages with an alert not seen yet in the Tensho chat window, a message in two tabs counted once.</summary>
    internal int ChatAlertsUnseen=>Volatile.Read(ref chatAlertsUnseen);
    /// <summary>X-447 B7 (0.1647): the server bar's entry clicked: the first tab holding an alert not seen is shown at the next frame.</summary>
    internal void ShowChatAlert()=>Volatile.Write(ref chatShowAlert,true);
    /// <summary>X-475: the folder of the chat history files (said when the player is asked to turn it on); null outside the game.</summary>
    internal string? ChatHistoryFolder{get;set;}
    /// <summary>X-475: « Erase the history » (every character's file, and what is queued); its answer says whether every file went.</summary>
    internal Action<Action<bool>?>? EraseChatHistory{get;set;}
    /// <summary>X-513: the place the open character's history takes on disk, in bytes (-1: none open); null outside the game.</summary>
    internal Func<long>? ChatHistoryBytes{get;set;}
    // X-475: the player asked to turn the history on (the question opens next); the last « Erase »: 1 every file went, 2 one is still there.
    private bool chatHistoryAsk;
    private volatile int chatHistoryErased;
    /// <summary>X-484 (0.1482): the game's log filters (LogFilter sheet, client language), set by the plugin; read once, on first use.</summary>
    // X-564 (0.1634): the game's log filters read off the game's thread (its first frame with lines read them there: a sheet and the code to
    // read it, 20/11 18:00); 0.1637: by the warm-up's thread, after the chat's code (MainWindow, chatThen), no longer on a thread of their
    // own during the load (the chat part took 3.7 to 4.2 s with it, 2.0 to 2.9 s before).
    internal Func<IReadOnlyList<ChatFilterRow>>? ChatFilterSource{get=>chatFilterSource;set=>chatFilterSource=value;}
    private Func<IReadOnlyList<ChatFilterRow>>? chatFilterSource;
    private ChatFilterTable? chatFilterTable;
    private bool chatFilterTried;
    private readonly object chatFilterGate=new();
    // X-564 (0.1634): read once, by the first of the game's thread and the background reading (the other waits for it or finds it made).
    private ChatFilterTable? ChatFilterTableNow()
    {
        if(Volatile.Read(ref chatFilterTried))return chatFilterTable;
        lock(chatFilterGate)
        {
            if(chatFilterTried || chatFilterSource is not { } source)return chatFilterTable;
            try{chatFilterTable=new(source());}
            catch(Exception error){log.Warning(error,"The game's log filters could not be read: the chat tabs use their channels.");}
            Volatile.Write(ref chatFilterTried,true);
            return chatFilterTable;
        }
    }
    // X-484 (0.1482): the players whose name stands out, read once per frame.
    private IReadOnlyList<string> chatHighlighted=[];
    // X-484 (0.1520): the alert words. X-634 (review of 0.1520, M2): looked once per message by the tab's view (ChatTabView.AlertAt).
    private string? chatAlertWords;
    // X-636 (bench X-631, review of 0.1441, M3): the link the left button was pressed on (it acts on the release over it, without a drag).
    private string? chatLinkPressed;
    private Vector2 chatLinkPressedAt;
    private ChatSettings ChatOptions=>configuration.Chat;
    // Saved once no control is active any more (tab names, channels, colours: several edits per frame while typing or picking).
    private bool chatSettingsPending;
    private const int ChatLayoutsPerFrame=100;

    // X-485 (0.1479): never brought to the front (NoBringToFrontOnFocus) and put at the back when it appears: Tensho's other windows stay in
    // front of it. Its own setting decides what a game window over it does (StepsAside false: not the panels' setting).
    private PanelDefinition ChatPanelDefinition=>new("Chat","###TenshoChat",new Vector2(40,420),
        ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoBringToFrontOnFocus,
        ()=>ChatOptions.WindowOpen && !inDesktopApp,ChatWindowVisible,DrawChatWindowContents,CloseChatWindow,()=>localization.Get("ChatWindow.Title"),
        StepsAside:()=>false,Opacity:()=>ChatCover.Background(ChatOptions.WhenCovered,chatCovered,ChatOptions.BoundedOpacity),Size:new Vector2(560,320),
        MoreFlags:ChatFrameFlags,NoBorder:()=>ChatOptions.Borderless,Fade:ChatFade,
        Padding:new Vector2(ChatLook.Padding.X,ChatLook.Padding.Y),Place:ChatPlace,Frame:ChatFrame,Once:ChatPlaceOnce,Early:()=>WarmupChatDone);

    /// <summary>X-447 D6 (0.1618): faded under a game window, else the opacity chosen while the mouse is not on the window (ChatLook.Alpha).
    /// X-447 B1 (0.1622): then faded out after the delay without a new message in the tab shown (ChatLook.Idle); faded out, the clicks go through.</summary>
    private float? ChatFade()
    {
        var hovered=chatAt is { } at && ChatLook.Under((ImGui.GetMousePos().X,ImGui.GetMousePos().Y),(at.X,at.Y),(chatSize.X,chatSize.Y));
        if(chatShownNewest!=chatSeenNewest){chatSeenNewest=chatShownNewest;chatLastMessageAt=Environment.TickCount64;}
        var alpha=ChatLook.Idle(ChatBaseFade(hovered),ChatOptions.BoundedFadeAfter,(Environment.TickCount64-chatLastMessageAt)/1000d,hovered || chatMenuOpen);
        chatIdleOut=alpha is <= 0f;
        return alpha;
    }
    private float? ChatBaseFade(bool hovered)
        =>ChatLook.Alpha(ChatCover.Faded(ChatOptions.WhenCovered,chatCovered) ? ChatCover.FadedAlpha:null,ChatOptions.BoundedTextOpacity,hovered);
    // X-447 B1 (0.1622): the newest message of the tab shown (its id; -1: none), the one seen last, when it came, and the window faded out.
    private long chatShownNewest=-1,chatSeenNewest=-1,chatLastMessageAt=Environment.TickCount64;
    private bool chatIdleOut;

    /// <summary>X-447 D5 (0.1618): the window's margins, corners and frame thickness from its settings.</summary>
    private PanelFrame ChatFrame()=>new(new Vector2(ChatOptions.BoundedMarginX,ChatOptions.BoundedMarginY),ChatOptions.BoundedRounding,ChatOptions.BoundedBorder);

    /// <summary>X-447 D7 (0.1618): a place asked in the settings, given once to the window.</summary>
    private PanelPlace? chatPlaceAsked;
    private PanelPlace? ChatPlaceOnce(){var once=chatPlaceAsked;chatPlaceAsked=null;return once;}

    /// <summary>X-467 (0.1504): the box the game's messages leave free while they are hidden (GameChatHider.Place, set by the plugin).</summary>
    internal Func<ChatFollowPlace?>? ChatGamePlace{get;set;}
    /// <summary>X-533 (0.1508): the game's channel menu open while its messages are hidden (GameChatHider.ChannelMenuOpen, set by the plugin).</summary>
    internal Func<bool>? ChatGameChannelMenu{get;set;}
    // X-467 (0.1504): the window drawn over the game's messages this frame; reduced to its tabs (« both chats »), for this session.
    private bool chatFollowing,chatReduced;
    // X-467 (0.1504): the window failed (PanelWindow turned it off): the game's messages are never left hidden behind it.
    private volatile bool chatWindowFailed;

    /// <summary>X-467 (0.1504, D-181, DEC-053 completed): whether the game chat's messages are to be hidden now (read by GameChatHider on the
    /// game's thread): the option on and « both chats » off, Tensho's chat window open, shown and working. X-527 (0.1506): left as they are
    /// while the game hides its interface (cutscene, GPose, interface hidden: Hold); given back only when really no longer wanted. X-689 (0.1657,
    /// the user's report of 20/11): Tensho's window hiding itself (in combat, a duty or a cutscene, under a game window, faded) keeps the game's
    /// messages hidden: Tensho's chat still replaces the game's.</summary>
    internal ChatHideWant ChatGameMessagesWant()
        =>ChatHideWants.Of(ChatGameMessagesOption(),TenshoAssistant.Competitive.CompetitiveGate.Cut,gameGui.GameUiHidden);

    /// <summary>X-540 M1: the option wanted: on, « both chats » off, Tensho's chat window open, shown and working (the first state of
    /// ChatGameMessagesWant; the hider reads the game chat's place only then).</summary>
    internal bool ChatGameMessagesOption()
        =>!inDesktopApp && !windowDisposed && ChatPanelOpen && !chatWindowFailed && ChatOptions.WindowOpen && ChatOptions.HideGameMessages && !ChatOptions.BothChats;

    /// <summary>X-564 (0.1631): the chat window may be drawn: the panels' gate is open, or its own code is compiled (it opens before the
    /// other panels; the game's messages are hidden from the same moment).</summary>
    internal bool ChatPanelOpen=>Diagnostics.PanelGate.Lets(PanelsOpen,()=>WarmupChatDone);

    /// <summary>X-467 (0.1504): the game's messages hidden now, Tensho's window in their place.</summary>
    internal bool ChatHidesGameMessages()=>ChatGameMessagesWant()==ChatHideWant.Hide;

    /// <summary>X-467 (0.1504): the window's place given from outside: over the game's messages while they are hidden; reduced to its tabs
    /// when asked (its place kept); null otherwise (the window where the player put it).</summary>
    private PanelPlace? ChatPlace()
    {
        var follow=ChatHidesGameMessages() ? ChatGamePlace?.Invoke():null;
        // X-528 M4 (0.1507): « reduced » belongs to the window at its own place; it is cleared when the window takes the game's messages' place,
        // so it never comes back reduced after a pause of the following (combat, cutscene, covered) the player did not ask for.
        if(follow is not null)chatReduced=false;
        chatFollowing=follow is not null;
        if(follow is { Box:var box })return new PanelPlace(new Vector2(box.X,box.Y),new Vector2(box.Width,box.Height));
        if(!chatReduced)return null;
        var width=chatSize.X>0 ? chatSize.X:560;
        return new PanelPlace(null,new Vector2(width,ImGui.GetFrameHeightWithSpacing()+ChatOptions.BoundedMarginY*2+(ChatOptions.Borderless ? 0:ImGui.GetFrameHeight())));
    }

    /// <summary>X-448 (0.1454): what the two settings add to the window's flags: borderless, no title bar; locked, neither moved nor resized.
    /// X-485 (0.1479): faded under a game window, the clicks go through to the game.</summary>
    private ImGuiWindowFlags ChatFrameFlags()=>(ChatOptions.Borderless ? ImGuiWindowFlags.NoTitleBar:ImGuiWindowFlags.None)
        |(ChatOptions.Locked ? ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize:ImGuiWindowFlags.None)
        // X-467 (0.1504): over the game's messages, the game's chat decides its place and size (its own resize button, the HUD layout).
        |(chatFollowing ? ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize:ImGuiWindowFlags.None)
        |(ChatCover.Faded(ChatOptions.WhenCovered,chatCovered) ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None)
        // X-447 B1 (0.1622): faded out after the delay without message, the clicks go through to the game.
        |(chatIdleOut ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None)
        // X-447 D1 (0.1620): locked, the clicks go through to the game except on the zones drawn the frame before.
        |(ChatLook.Through(ChatOptions.MouseThrough,ChatOptions.Locked,chatMenuOpen,(ImGui.GetMousePos().X,ImGui.GetMousePos().Y),chatZones) ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None);

    // X-447 D1 (0.1620): the zones of the window that answer the mouse (screen space), drawn last frame and being drawn now; a chat menu open.
    private List<(float X,float Y,float Width,float Height)> chatZones=[],chatZonesNext=[];
    private bool chatMenuOpen;
    private void ChatZone(){var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();chatZonesNext.Add((min.X,min.Y,max.X-min.X,max.Y-min.Y));}

    /// <summary>X-447 D8 (0.1620): the moments the window is hidden in (set by the plugin: the game's conditions).</summary>
    internal Func<bool>? InCutsceneNow{get;set;}
    internal Func<bool>? InDutyNow{get;set;}
    /// <summary>X-447 B13 (0.1655): crafting (the game's crafting state), in a city (the zone's use, read once per zone): the chat's contexts.</summary>
    internal Func<bool>? InCraftingNow{get;set;}
    internal Func<bool>? InTownNow{get;set;}
    private bool ChatHiddenNow()=>ChatLook.HiddenNow(ChatOptions,InCombatNow?.Invoke()==true,InCutsceneNow?.Invoke()==true,InDutyNow?.Invoke()==true);

    // X-485 (0.1479): where the window was last drawn, whether a game window covers it there, and the frame it was last drawn in.
    private Vector2? chatAt;
    private Vector2 chatSize;
    private bool chatCovered;
    private int chatLastFrame=-10;

    private bool ChatWindowShown()
        =>!windowDisposed && !TenshoAssistant.Competitive.CompetitiveGate.Cut && !gameGui.GameUiHidden && !ChatHiddenNow();

    private bool ChatWindowVisible()
    {
        if(!ChatWindowShown())return false;
        // X-533 (0.1508): over the game's messages, the window steps aside while the game's channel menu is open (the menu opens upward over it).
        if(ChatFollow.StepsAside(ChatHidesGameMessages(),ChatGameChannelMenu?.Invoke()==true))return false;
        var at=chatAt ?? Vector2.Zero;
        var covered=ChatCover.Bounded(ChatOptions.WhenCovered)!=ChatCovered.Stay && chatAt is not null && BehindGame(at,chatSize,chat:true);
        if(covered!=chatCovered && developerMode)
            log.Information("Chat window {0} ({1}): at {2}, size {3}; windows: {4}.",covered ? "covered by a game window":"uncovered",ChatCover.Bounded(ChatOptions.WhenCovered),at,chatSize,
                string.Join(", ",GameWindowList.Where(window=>UI.GameWindows.Touch(at.X,at.Y,chatSize.X,chatSize.Y,window.X,window.Y,window.Width,window.Height)).Select(window=>window.Name)));
        chatCovered=covered;
        return !ChatCover.Hidden(ChatOptions.WhenCovered,covered);
    }

    private void CloseChatWindow()
    {
        if(!ChatOptions.WindowOpen)return;
        ChatOptions.WindowOpen=false;
        ChatMessages.Clear();
        // As the other panels: a closed window stays closed when saving fails (the player asked for it); the next save writes it.
        TrySave("Chat window");
    }

    /// <summary>« /tensho chat »: shows or hides the window, as its setting does.</summary>
    internal void ToggleChatWindow()
    {
        var open=!ChatOptions.WindowOpen;
        ChatOptions.WindowOpen=open;
        if(!open)ChatMessages.Clear();
        TrySave("Chat window");
    }

    private string ChatChannelName(ChatChannel channel)=>localization.Get("ChatChannels."+channel);

    /// <summary>X-545 (0.1517): a tooltip of the chat settings from its lines (keys and arguments, Chat/ChatHelp.cs), one line each.</summary>
    private string ChatHelpText(IEnumerable<(string Key,object[] Args)> lines)
        =>string.Join("\n",lines.Select(line=>line.Args.Length==0 ? localization.Get(line.Key):localization.Format(line.Key,line.Args)));
    private string ChatChannelShort(ChatChannel channel)=>localization.Get("ChatChannelsShort."+channel);
    private static Vector4 ChatColor(uint rgb){var (red,green,blue)=ChatChannels.Unpack(rgb);return new(red,green,blue,1);}

    // The whole settings of the chat window, for the window's own settings panel; the Settings page shows the same parts one category each.
    private void DrawChatWindowSettings()
    {
        var options=ChatOptions;
        DrawChatLookSettings();
        DrawChatHistorySettings(DefaultConfiguration.Chat);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionTabs")+"###ChatTabsSection"))DrawChatTabSettings(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionContexts")+"###ChatContextsSection"))DrawChatContextProfiles(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionColors")+"###ChatColorsSection"))DrawChatColorSettings(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionPlayers")+"###ChatPlayersSection"))DrawChatPlayers(options);
        if(developerMode)DrawChatKinds();
        SaveChatSettingsWhenReleased();
    }

    // X-449: the Settings page's categories of the chat window; each one writes the tabs and colours once no control is held any more.
    private void DrawChatLookCategory()
    {
        DrawChatLookSettings();
        SaveChatSettingsWhenReleased();
    }

    private void DrawChatHistoryCategory()
    {
        DrawChatHistorySettings(DefaultConfiguration.Chat);
        SaveChatSettingsWhenReleased();
    }

    private void DrawChatTabsCategory()
    {
        var options=ChatOptions;
        DrawChatTabSettings(options);
        DrawChatContextProfiles(options);
        if(developerMode)DrawChatKinds();
        SaveChatSettingsWhenReleased();
    }

    private void DrawChatColoursCategory()
    {
        var options=ChatOptions;
        DrawChatColorSettings(options);
        SaveChatSettingsWhenReleased();
    }

    private void DrawChatPlayersCategory()
    {
        var options=ChatOptions;
        DrawChatPlayers(options);
        SaveChatSettingsWhenReleased();
    }

    private void SaveChatSettingsWhenReleased()
    {
        if(chatSettingsPending && !ImGui.IsAnyItemActive()){chatSettingsPending=false;TrySave("Chat window tabs and colours");}
    }

    private void DrawChatLookSettings()
    {
        var defaults=DefaultConfiguration.Chat;
        // X-691 M1 and M2 (0.1668, review of 0.1655): the context followed here too, the window closed or hidden, and the profile worn said.
        DrawChatProfileWorn(ChatOptions);
        BoolSetting("ChatWindow.Show","ChatWindow.ShowTip",()=>configuration.Chat.WindowOpen,value=>{configuration.Chat.WindowOpen=value;if(!value)ChatMessages.Clear();},c=>c.Chat.WindowOpen);
        BoolSetting("ChatWindow.HideInCombat","ChatWindow.HideInCombatTip",()=>configuration.Chat.HideInCombat,value=>configuration.Chat.HideInCombat=value,c=>c.Chat.HideInCombat);
        // X-447 D8 (0.1620): hidden in a cutscene, in a duty.
        BoolSetting("ChatWindow.HideInCutscene","ChatWindow.HideInCutsceneTip",()=>configuration.Chat.HideInCutscene,value=>configuration.Chat.HideInCutscene=value,c=>c.Chat.HideInCutscene);
        BoolSetting("ChatWindow.HideInDuty","ChatWindow.HideInDutyTip",()=>configuration.Chat.HideInDuty,value=>configuration.Chat.HideInDuty=value,c=>c.Chat.HideInDuty);
        // X-467 (0.1504, D-181): the game chat's messages hidden, this window in their place above the game's typing box; both chats to compare.
        BoolSetting("ChatWindow.HideGameMessages","ChatWindow.HideGameMessagesTip",()=>configuration.Chat.HideGameMessages,value=>configuration.Chat.HideGameMessages=value,c=>c.Chat.HideGameMessages);
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.Chat.HideGameMessages);
        try{BoolSetting("ChatWindow.BothChats","ChatWindow.BothChatsTip",()=>configuration.Chat.BothChats,value=>configuration.Chat.BothChats=value,c=>c.Chat.BothChats);}
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        // X-696 M2: no chat at all in combat or in a duty with these settings together (the game's messages stay hidden since X-689).
        if(ChatHideWants.LeavesNoChat(configuration.Chat.HideGameMessages,configuration.Chat.BothChats,configuration.Chat.HideInCombat,configuration.Chat.HideInDuty))
        {
            ImGui.PushStyleColor(ImGuiCol.Text,Warning);
            ImGui.TextWrapped(localization.Get("ChatWindow.NoChatWarning"));
            ImGui.PopStyleColor();
        }
        BoolSetting("ChatWindow.Timestamps","ChatWindow.TimestampsTip",()=>configuration.Chat.ShowTimestamps,value=>configuration.Chat.ShowTimestamps=value,c=>c.Chat.ShowTimestamps);
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.Chat.ShowTimestamps);
        try
        {
            BoolSetting("ChatWindow.Clock24","ChatWindow.Clock24Tip",()=>configuration.Chat.Clock24,value=>configuration.Chat.Clock24=value,c=>c.Chat.Clock24);
            BoolSetting("ChatWindow.Seconds","ChatWindow.SecondsTip",()=>configuration.Chat.ShowSeconds,value=>configuration.Chat.ShowSeconds=value,c=>c.Chat.ShowSeconds);
            // X-505 (0.1488): the hour as the game writes it ([9:21]), or with a leading zero.
            BoolSetting("ChatWindow.HourLeadingZero","ChatWindow.HourLeadingZeroTip",()=>configuration.Chat.HourLeadingZero,value=>configuration.Chat.HourLeadingZero=value,c=>c.Chat.HourLeadingZero);
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        BoolSetting("ChatWindow.Channel","ChatWindow.ChannelTip",()=>configuration.Chat.ShowChannel,value=>configuration.Chat.ShowChannel=value,c=>c.Chat.ShowChannel);
        // X-447 B5 (0.1621): the players' names in brackets, in a colour of their own.
        BoolSetting("ChatWindow.SenderBrackets","ChatWindow.SenderBracketsTip",()=>configuration.Chat.SenderBrackets,value=>configuration.Chat.SenderBrackets=value,c=>c.Chat.SenderBrackets);
        BoolSetting("ChatWindow.NameColors","ChatWindow.NameColorsTip",()=>configuration.Chat.NameColors,value=>configuration.Chat.NameColors=value,c=>c.Chat.NameColors);
        // X-690 (0.1666): the role's colour and the job's icon, when the game shows the player's job.
        BoolSetting("ChatWindow.RoleColors","ChatWindow.RoleColorsTip",()=>configuration.Chat.RoleColors,value=>configuration.Chat.RoleColors=value,c=>c.Chat.RoleColors);
        BoolSetting("ChatWindow.JobIcons","ChatWindow.JobIconsTip",()=>configuration.Chat.JobIcons,value=>configuration.Chat.JobIcons=value,c=>c.Chat.JobIcons);
        // X-447 B15 (0.1623, R25): capture mode.
        BoolSetting("ChatWindow.CaptureNames","ChatWindow.CaptureNamesTip",()=>configuration.Chat.CaptureNames,value=>configuration.Chat.CaptureNames=value,c=>c.Chat.CaptureNames);
        SliderSetting("ChatWindow.Opacity","ChatWindow.OpacityTip",()=>configuration.Chat.BoundedOpacity,value=>configuration.Chat.Opacity=value,0,100,defaults.Opacity,"%d %%");
        // X-448 (0.1454): the frame and the lock, also on the buttons at the end of the window's tab bar.
        BoolSetting("ChatWindow.Borderless","ChatWindow.BorderlessTip",()=>configuration.Chat.Borderless,value=>configuration.Chat.Borderless=value,c=>c.Chat.Borderless);
        BoolSetting("ChatWindow.Locked","ChatWindow.LockedTip",()=>configuration.Chat.Locked,value=>configuration.Chat.Locked=value,c=>c.Chat.Locked);
        // X-447 D1 (0.1620): while locked, the clicks go through to the game.
        BoolSetting("ChatWindow.MouseThrough","ChatWindow.MouseThroughTip",()=>configuration.Chat.MouseThrough,value=>configuration.Chat.MouseThrough=value,c=>c.Chat.MouseThrough);
        // X-476 (0.1481): the tabs under the lines, and the letters' outline.
        BoolSetting("ChatWindow.TabsAtBottom","ChatWindow.TabsAtBottomTip",()=>configuration.Chat.TabsAtBottom,value=>configuration.Chat.TabsAtBottom=value,c=>c.Chat.TabsAtBottom);
        BoolSetting("ChatWindow.TextOutline","ChatWindow.TextOutlineTip",()=>configuration.Chat.TextOutline,value=>configuration.Chat.TextOutline=value,c=>c.Chat.TextOutline);
        // X-505 (0.1488): the rest of the game's look, each its own setting.
        BoolSetting("ChatWindow.ThinScrollbar","ChatWindow.ThinScrollbarTip",()=>configuration.Chat.ThinScrollbar,value=>configuration.Chat.ThinScrollbar=value,c=>c.Chat.ThinScrollbar);
        BoolSetting("ChatWindow.GameLinks","ChatWindow.GameLinksTip",()=>configuration.Chat.GameLinks,value=>configuration.Chat.GameLinks=value,c=>c.Chat.GameLinks);
        // X-447 B14 (0.1641): an item's icon before its link.
        BoolSetting("ChatWindow.ItemIcons","ChatWindow.ItemIconsTip",()=>configuration.Chat.ItemIcons,value=>configuration.Chat.ItemIcons=value,c=>c.Chat.ItemIcons);
        BoolSetting("ChatWindow.GameTabs","ChatWindow.GameTabsTip",()=>configuration.Chat.GameTabs,value=>configuration.Chat.GameTabs=value,c=>c.Chat.GameTabs);
        BoolSetting("ChatWindow.WrapIndent","ChatWindow.WrapIndentTip",()=>configuration.Chat.WrapIndent,value=>configuration.Chat.WrapIndent=value,c=>c.Chat.WrapIndent);
        // X-447 D4 to D7 (0.1618, the user's choices of 15/11): the window's own settings.
        BoolSetting("ChatWindow.HideTabs","ChatWindow.HideTabsSettingTip",()=>configuration.Chat.HideTabs,value=>configuration.Chat.HideTabs=value,c=>c.Chat.HideTabs);
        SliderSetting("ChatWindow.MarginX","ChatWindow.MarginXTip",()=>configuration.Chat.BoundedMarginX,value=>configuration.Chat.MarginX=value,0,24,defaults.MarginX,"%d px");
        SliderSetting("ChatWindow.MarginY","ChatWindow.MarginYTip",()=>configuration.Chat.BoundedMarginY,value=>configuration.Chat.MarginY=value,0,24,defaults.MarginY,"%d px");
        SliderSetting("ChatWindow.Rounding","ChatWindow.RoundingTip",()=>configuration.Chat.BoundedRounding,value=>configuration.Chat.Rounding=value,0,16,defaults.Rounding,"%d px");
        SliderSetting("ChatWindow.Border","ChatWindow.BorderTip",()=>configuration.Chat.BoundedBorder,value=>configuration.Chat.Border=value,0,4,defaults.Border,"%d px");
        SliderSetting("ChatWindow.TextOpacity","ChatWindow.TextOpacityTip",()=>configuration.Chat.BoundedTextOpacity,value=>configuration.Chat.TextOpacity=value,20,100,defaults.TextOpacity,"%d %%");
        DrawChatPlaceSettings();
        // X-485 (0.1479): what the window does under a window of the game.
        ComboSetting("ChatWindow.WhenCovered","ChatWindow.WhenCoveredTip",()=>ChatCover.Bounded(configuration.Chat.WhenCovered),value=>configuration.Chat.WhenCovered=value,
            [(ChatCovered.Hide,localization.Get("ChatWindow.CoveredHide")),(ChatCovered.Fade,localization.Get("ChatWindow.CoveredFade")),(ChatCovered.Stay,localization.Get("ChatWindow.CoveredStay"))],
            ChatCovered.Hide,220);
        SliderSetting("ChatWindow.FontScale","ChatWindow.FontScaleTip",()=>configuration.Chat.BoundedFontScale,value=>configuration.Chat.FontScale=value,60,200,defaults.FontScale,"%d %%");
        // X-447 B2 (0.1622): spacing between the lines; B1: the window fades out after a delay without message.
        SliderSetting("ChatWindow.LineSpacing","ChatWindow.LineSpacingTip",()=>configuration.Chat.BoundedLineSpacing,value=>configuration.Chat.LineSpacing=value,0,12,defaults.LineSpacing,"%d px");
        SliderSetting("ChatWindow.FadeAfter","ChatWindow.FadeAfterTip",()=>configuration.Chat.BoundedFadeAfter,value=>configuration.Chat.FadeAfter=value,0,600,defaults.FadeAfter,"%d s");
        IntSetting("ChatWindow.MaxLines","ChatWindow.MaxLinesTip",()=>configuration.Chat.BoundedMaxLines,value=>configuration.Chat.MaxLines=value,ChatStore.MinimumLines,ChatStore.MaximumLines,defaults.MaxLines);
        DrawChatLookCode();
    }

    // X-447 B16 (0.1648): what the last copy or paste of the look did.
    private string? chatLookMessage;

    /// <summary>X-447 B16 (0.1648, the user's choice of 15/11, after Chattynator and ElvUI): the window's look copied as a code, or a code pasted
    /// (ChatTheme: never the tabs, the players, the words nor the sounds).</summary>
    private void DrawChatLookCode()
    {
        if(ImGui.Button(localization.Get("ChatWindow.CopyLook"))){ImGui.SetClipboardText(ChatTheme.Share(configuration.Chat));chatLookMessage=localization.Get("ChatWindow.LookCopied");}
        Tip("ChatWindow.CopyLookTip");
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("ChatWindow.PasteLook")))
        {
            if(ChatTheme.Apply(ImGui.GetClipboardText(),configuration.Chat)){chatLookMessage=localization.Get("ChatWindow.LookPasted");TrySave("Chat window look");}
            else chatLookMessage=localization.Get("ChatWindow.LookNotACode");
        }
        Tip("ChatWindow.PasteLookTip");
        // X-447 B16 (0.1649): a ready-made look, applied at once (a complete look, the tabs, players, words and sounds kept).
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        if(ImGui.BeginCombo("###ChatReadyLooks",localization.Get("ChatWindow.Looks")))
        {
            try
            {
                foreach(var (key,_) in ChatTheme.Ready)
                {
                    if(!ImGui.Selectable(ChatLookName(key)) || ChatTheme.ReadyCode(key) is not { } code || !ChatTheme.Apply(code,configuration.Chat))continue;
                    chatLookMessage=localization.Format("ChatWindow.LookChosen",ChatLookName(key));
                    TrySave("Chat window look");
                }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("ChatWindow.LooksTip");
        if(chatLookMessage is { } said)ImGui.TextDisabled(said);
    }

    private string ChatLookName(string key)=>key switch
    {
        "Clear"=>localization.Get("ChatWindow.LookClear"),
        "Dark"=>localization.Get("ChatWindow.LookDark"),
        "Compact"=>localization.Get("ChatWindow.LookCompact"),
        _=>localization.Get("ChatWindow.LookGame"),
    };

    /// <summary>X-475 (the user's remark of 15/11, asked again on 16/11; X-447 B10): « Keep the chat history on this PC », off by default. Ticking it
    /// asks first, in plain words: what is kept (the tabs' messages, other players' names and words included), where (the folder, one file per
    /// character), how long (the bounds below) and how to erase it; only « Keep the history » turns it on. « Erase the history » asks too.</summary>
    private void DrawChatHistorySettings(ChatSettings defaults)
    {
        // X-515 M2: outside the game (the desktop app) nothing keeps nor erases the history: its settings are shown greyed, with why, and the
        // question (which names the folder) never opens there.
        var here=ChatHistoryFolder is not null && EraseChatHistory is not null;
        ImGui.BeginDisabled(!here);
        try{BoolSetting("ChatWindow.KeepHistory","ChatWindow.KeepHistoryTip",()=>configuration.Chat.KeepHistory,value=>{if(value)chatHistoryAsk=true;else configuration.Chat.KeepHistory=false;},c=>c.Chat.KeepHistory);}
        finally{ImGui.EndDisabled();}
        if(!here)
        {
            chatHistoryAsk=false;
            ImGui.TextColored(Muted,localization.Get("ChatWindow.HistoryGameOnly"));
        }
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.Chat.KeepHistory || !here);
        try
        {
            // X-513 (the user's request of 16/11, choice (a)): « No limit » beside each bound, apart; its number greyed while it is ticked.
            ImGui.BeginDisabled(configuration.Chat.HistoryMessagesUnlimited);
            try{IntSetting("ChatWindow.HistoryMessages","ChatWindow.HistoryMessagesTip",()=>ChatHistory.BoundedMessages(configuration.Chat.HistoryMessages),value=>configuration.Chat.HistoryMessages=value,ChatHistory.MinimumMessages,ChatHistory.MaximumMessages,defaults.HistoryMessages);}
            finally{ImGui.EndDisabled();}
            ImGui.SameLine();
            // X-518 M2 (review of 0.1494): unticking « No limit » while the history is kept asks first: what is past the bound is deleted.
            BoolSetting("ChatWindow.HistoryMessagesUnlimited","ChatWindow.HistoryMessagesUnlimitedTip",()=>configuration.Chat.HistoryMessagesUnlimited,
                value=>{if(ChatHistoryLimitGuard.Change(configuration.Chat,ChatHistoryBound.Messages,value) is var ask and not ChatHistoryBound.None)chatHistoryLimitAsk=ask;},c=>c.Chat.HistoryMessagesUnlimited);
            ImGui.BeginDisabled(configuration.Chat.HistoryDaysUnlimited);
            try{IntSetting("ChatWindow.HistoryDays","ChatWindow.HistoryDaysTip",()=>ChatHistory.BoundedDays(configuration.Chat.HistoryDays),value=>configuration.Chat.HistoryDays=value,ChatHistory.MinimumDays,ChatHistory.MaximumDays,defaults.HistoryDays);}
            finally{ImGui.EndDisabled();}
            ImGui.SameLine();
            BoolSetting("ChatWindow.HistoryDaysUnlimited","ChatWindow.HistoryDaysUnlimitedTip",()=>configuration.Chat.HistoryDaysUnlimited,
                value=>{if(ChatHistoryLimitGuard.Change(configuration.Chat,ChatHistoryBound.Days,value) is var ask and not ChatHistoryBound.None)chatHistoryLimitAsk=ask;},c=>c.Chat.HistoryDaysUnlimited);
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        // X-513: the place the character's history takes on disk, measured off the game's thread (none while no history is open).
        if(here && ChatHistoryBytes?.Invoke() is >=0 and var size)
        {
            var (amount,unit)=ChatHistory.Size(size);
            ImGui.TextColored(Muted,localization.Format("ChatWindow.HistorySize",localization.Format("ChatWindow.HistorySize"+unit,amount)));
        }
        ImGui.BeginDisabled(EraseChatHistory is null);
        try
        {
            if(ImGui.Button(localization.Get("ChatWindow.HistoryErase")+"###ChatHistoryErase"))ImGui.OpenPopup("###ChatHistoryEraseAsk");
            Tip("ChatWindow.HistoryEraseTip");
        }
        finally{ImGui.EndDisabled();}
        if(chatHistoryErased is 1 or 2)
        {
            ImGui.SameLine();
            ImGui.TextColored(chatHistoryErased==1 ? Success:Warning,localization.Get(chatHistoryErased==1 ? "ChatWindow.HistoryErased":"ChatWindow.HistoryEraseLeft"));
        }
        if(chatHistoryAsk){chatHistoryAsk=false;ImGui.OpenPopup("###ChatHistoryConsent");}
        if(chatHistoryLimitAsk!=ChatHistoryBound.None && !ImGui.IsPopupOpen("###ChatHistoryLimitAsk")){chatHistoryLimitAsking=chatHistoryLimitAsk;chatHistoryLimitAsk=ChatHistoryBound.None;ImGui.OpenPopup("###ChatHistoryLimitAsk");}
        DrawChatHistoryConsent();
        DrawChatHistoryEraseAsk();
        DrawChatHistoryLimitAsk();
    }

    // X-518 M2 (review of 0.1494): « No limit » unticked while the history is kept: the bound asked about before it applies (X-643: the
    // decision lives in ChatHistoryLimitGuard, tested).
    private ChatHistoryBound chatHistoryLimitAsk,chatHistoryLimitAsking;

    /// <summary>X-518 M2: the question before « No limit » is unticked: the messages past the number, or older than the days, are deleted from
    /// the history at its next trim (opening, or soon after), possibly months of them; « Cancel » leaves « No limit » ticked.</summary>
    private void DrawChatHistoryLimitAsk()
    {
        var open=true;
        if(!ImGui.BeginPopupModal(localization.Get("ChatWindow.HistoryLimitTitle")+"###ChatHistoryLimitAsk",ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            var days=chatHistoryLimitAsking==ChatHistoryBound.Days;
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*30);
            ImGui.TextUnformatted(days ? localization.Format("ChatWindow.HistoryLimitDaysText",ChatHistory.BoundedDays(configuration.Chat.HistoryDays))
                :localization.Format("ChatWindow.HistoryLimitMessagesText",ChatHistory.BoundedMessages(configuration.Chat.HistoryMessages)));
            ImGui.PopTextWrapPos();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryLimitYes")+"###ChatHistoryLimitYes"))
            {
                if(ChatHistoryLimitGuard.Answer(configuration.Chat,chatHistoryLimitAsking,true))TrySave("Chat history limit");
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryCancel")+"###ChatHistoryLimitNo"))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>X-475: the question before the history is turned on; « Keep the history » turns it on, anything else leaves it off.</summary>
    private void DrawChatHistoryConsent()
    {
        var open=true;
        if(!ImGui.BeginPopupModal(localization.Get("ChatWindow.HistoryConsentTitle")+"###ChatHistoryConsent",ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*34);
            // X-513: « No limit » said in words, a number as the language writes it.
            ImGui.TextUnformatted(localization.Format("ChatWindow.HistoryConsentText",ChatHistoryFolder ?? "-",
                ChatOptions.HistoryMessagesUnlimited ? localization.Get("ChatWindow.HistoryNoLimit"):localization.Format("ChatWindow.HistoryNumber",ChatOptions.BoundedHistoryMessages),
                ChatOptions.HistoryDaysUnlimited ? localization.Get("ChatWindow.HistoryNoLimit"):localization.Format("ChatWindow.HistoryNumber",ChatOptions.BoundedHistoryDays),
                // X-518 M1 (review of 0.1494): « older ones are removed » only when a bound is set; with both « No limit », nothing is removed.
                localization.Get(ChatHistory.RemovesOlder(ChatOptions.HistoryMessagesUnlimited,ChatOptions.HistoryDaysUnlimited) ? "ChatWindow.HistoryOlderRemoved":"ChatWindow.HistoryNothingRemoved")));
            ImGui.PopTextWrapPos();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryConsentYes")+"###ChatHistoryYes")){configuration.Chat.KeepHistory=true;TrySave("Chat history");ImGui.CloseCurrentPopup();}
            ImGui.SameLine();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryCancel")+"###ChatHistoryNo"))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>X-475: the question before « Erase the history »: every character's file, and the window's lines.</summary>
    private void DrawChatHistoryEraseAsk()
    {
        var open=true;
        if(!ImGui.BeginPopupModal(localization.Get("ChatWindow.HistoryEraseTitle")+"###ChatHistoryEraseAsk",ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*30);
            ImGui.TextUnformatted(localization.Get("ChatWindow.HistoryEraseText"));
            ImGui.PopTextWrapPos();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryEraseYes")+"###ChatHistoryEraseYes"))
            {
                chatHistoryErased=0;
                EraseChatHistory?.Invoke(all=>chatHistoryErased=all ? 1:2);
                ChatMessages.Clear();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if(ImGui.Button(localization.Get("ChatWindow.HistoryCancel")+"###ChatHistoryEraseNo"))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>X-445 (0.1458): the tabs, the game's three default ones (names of the game's Addon sheet) and Social, made or completed once.</summary>
    private List<ChatTabSettings> ChatTabs(ChatSettings options)
    {
        var names=ChatTabNames();
        return options.EnsureTabs(names.General,names.Battle,names.Event,names.Social);
    }

    // X-447 B10 (0.1654): the conversations' tabs (memory only, never in the settings' tabs: R04), and the window's tabs: the settings' ones,
    // then the conversations' (one list kept, filled again at each frame). A conversation opened from a player's menu, shown at the next frame.
    private readonly ChatConversations chatConversations=new();
    private readonly List<ChatTabSettings> chatWindowTabs=[];
    private ChatTabSettings? chatSelectTab;

    private List<ChatTabSettings> ChatWindowTabs(ChatSettings options)
    {
        chatWindowTabs.Clear();
        // X-447 B13 (0.1655): the tabs the context's profile shows (every tab without one).
        ChatContexts.Shown(ChatTabs(options),ChatContexts.ProfileOf(options,chatContextNow),chatWindowTabs);
        if(options.Conversations)chatWindowTabs.AddRange(chatConversations.Tabs);
        return chatWindowTabs;
    }

    // X-447 B13 (0.1655): the context the window follows (a duty, crafting, a city).
    private ChatContext chatContextNow;

    /// <summary>X-447 B13 (0.1655, D-198 a): the window's look and tabs follow the context; the tab shown stays the same when the new context
    /// shows it too. Only the display changes (R03.1).</summary>
    private void FollowChatContext(ChatSettings options)
    {
        var now=ChatContexts.Now(InDutyNow?.Invoke()==true,InCraftingNow?.Invoke()==true,InTownNow?.Invoke()==true);
        if(now!=chatContextNow)
        {
            chatContextNow=now;
            if(options.ContextProfiles)chatSelectTab=chatShownTab;
        }
        if(ChatContexts.Follow(options,now))TrySave("Chat window context");
    }

    /// <summary>
    /// X-691 M1 (0.1668, review of 0.1655): the context was followed only while the chat window drew its lines: the window closed or hidden in a
    /// duty, then out of it, a change of the look went into the duty's profile and the look of before came back with the window. The settings
    /// that change the look follow the context first. M2: while a profile is worn, the sections of the look and the colours say which one,
    /// and that a change there goes to it.
    /// </summary>
    private void DrawChatProfileWorn(ChatSettings options)
    {
        FollowChatContext(options);
        if(ChatContexts.Worn(options) is not { } context)return;
        ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("ChatWindow.ProfileWorn",localization.Get("ChatWindow.Context"+context)));
        TipText(localization.Get("ChatWindow.ProfileWornTip"));
    }

    /// <summary>X-447 B13 (0.1655, D-198 a): each context's look and tabs, under the tabs' settings.</summary>
    private void DrawChatContextProfiles(ChatSettings options)
    {
        BoolSetting("ChatWindow.ContextProfiles","ChatWindow.ContextProfilesTip",()=>configuration.Chat.ContextProfiles,value=>configuration.Chat.ContextProfiles=value,c=>c.Chat.ContextProfiles);
        if(!options.ContextProfiles)return;
        var tabs=ChatTabs(options);
        foreach(var context in ChatContexts.Chosen)
        {
            ImGui.PushID("ChatContext"+context);
            try
            {
                ImGui.TextColored(Accent,localization.Get("ChatWindow.Context"+context)+(context==chatContextNow ? " "+localization.Get("ChatWindow.ContextNow"):""));
                var profile=options.Profiles?.GetValueOrDefault(context);
                var ownLook=profile?.Look is { Length:>0 };
                ImGui.TextColored(Muted,localization.Get(ownLook ? "ChatWindow.ContextLookOwn":"ChatWindow.ContextLookWindow"));
                if(SettingButton("ChatWindow.ContextUseLook","ChatWindow.ContextUseLookTip")){ChatContexts.Edit(options,context).Look=ChatTheme.Share(options);chatSettingsPending=true;}
                if(ownLook && profile is not null)
                {
                    ImGui.SameLine();
                    if(SettingButton("ChatWindow.ContextNoLook","ChatWindow.ContextNoLookTip")){profile.Look=null;chatSettingsPending=true;}
                }
                DrawChatCheckGrid("###ChatContextTabs"+context,tabs,tab=>tab.Name,tab=>"ChatContextTab"+context+tab.Name,tab=>ChatContexts.ShowsTab(profile,tab),
                    (tab,shown)=>{ChatContexts.SetTab(ChatContexts.Edit(options,context),tabs,tab,shown);chatSettingsPending=true;},_=>Tip("ChatWindow.ContextTabTip"));
            }
            finally{ImGui.PopID();}
        }
    }

    /// <summary>X-447 B10 (0.1654): a conversation's tab never replaced while shown or holding something not seen.</summary>
    private bool ChatConversationBusy(ChatTabSettings tab)=>ReferenceEquals(tab,chatShownTab) || ChatMessages.ViewOf(tab) is { Unread:>0 } or { Alerts:>0 };

    /// <summary>X-447 B10 (0.1654): a conversation closed: its view and its pins dropped (nothing of it left).</summary>
    private void ChatConversationClosed(ChatTabSettings tab){ChatMessages.Restart(tab);ChatMessages.Unpin(tab);}

    /// <summary>X-447 B10 (0.1654): the conversations follow the tells kept (a tab for each new player); turned off, they all close.</summary>
    private void UpdateChatConversations(ChatSettings options)
    {
        if(!options.Conversations)
        {
            if(chatConversations.Tabs.Count>0)chatConversations.Clear(ChatConversationClosed);
            return;
        }
        foreach(var (opened,from) in chatConversations.Update(ChatMessages,ChatConversationBusy,options.HiddenPlayers,options.ColorOf(ChatChannel.TellIn),ChatConversationClosed))
            ChatMessages.ViewOf(opened).CountFrom(from);
    }

    private void DrawChatTabSettings(ChatSettings options)
    {
        var tabs=ChatTabs(options);
        DrawChatTabUndo(tabs);
        for(var index=0;index<tabs.Count;index++)
        {
            var tab=tabs[index];
            ImGui.PushID(index);
            try
            {
                var name=tab.Name ?? "";
                ImGui.SetNextItemWidth(180);
                if(ImGui.InputText(localization.Get("ChatWindow.TabName")+"###ChatTabName",ref name,ChatSettings.MaximumTabName)){tab.Name=name;chatSettingsPending=true;}
                Tip("ChatWindow.TabNameTip");
                // X-565 (0.1565): each control of the tab's line goes to the next line when the width left does not hold it (narrow window).
                var style=ImGui.GetStyle();
                var boxed=ImGui.GetFrameHeight()+style.ItemInnerSpacing.X;
                SameLineIfFits(boxed+ImGui.CalcTextSize(localization.Get("ChatWindow.TabUnread")).X);
                var unread=tab.ShowUnread;
                if(ImGui.Checkbox(localization.Get("ChatWindow.TabUnread")+"###ChatTabUnread",ref unread)){tab.ShowUnread=unread;chatSettingsPending=true;}
                Tip("ChatWindow.TabUnreadTip");
                // X-478 (the user's request of 15/11): the colour of the tab's name; « ↺ » brings back the theme's text colour.
                SameLineIfFits(boxed+ImGui.CalcTextSize(localization.Get("ChatWindow.TabColor")).X+(tab.NameColor is null ? 0:ImGui.GetFrameHeight()+style.ItemSpacing.X));
                var named=tab.NameColor is { } chosen ? ChatColor(chosen):ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Text));
                var nameRgb=new Vector3(named.X,named.Y,named.Z);
                if(ImGui.ColorEdit3(localization.Get("ChatWindow.TabColor")+"###ChatTabColor",ref nameRgb,ImGuiColorEditFlags.NoInputs))
                {
                    tab.NameColor=ChatChannels.Pack(nameRgb.X,nameRgb.Y,nameRgb.Z);
                    chatSettingsPending=true;
                }
                Tip("ChatWindow.TabColorTip");
                if(DefaultButton("ChatTabColor",tab.NameColor is not null)){tab.NameColor=null;chatSettingsPending=true;}
                // X-447 B2 (0.1622): the tab's own text size.
                SameLineIfFits(boxed+ImGui.CalcTextSize(localization.Get("ChatWindow.TabFontOwn")).X);
                var own=tab.FontScale is not null;
                if(ImGui.Checkbox(localization.Get("ChatWindow.TabFontOwn")+"###ChatTabFontOwn",ref own)){tab.FontScale=own ? options.BoundedFontScale:null;chatSettingsPending=true;}
                Tip("ChatWindow.TabFontOwnTip");
                if(tab.FontScale is not null)
                {
                    SameLineIfFits(120*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                    ImGui.SetNextItemWidth(120*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                    var size=tab.ScaleIn(options);
                    if(ImGui.SliderInt("###ChatTabFont",ref size,60,200,"%d %%")){tab.FontScale=size;chatSettingsPending=true;}
                    Tip("ChatWindow.TabFontTip");
                }
                // X-687 (0.1663): the tab one place to the left or right, also without the mouse's drag and with the tabs' names hidden.
                if(index>0)
                {
                    var left=localization.Get("ChatWindow.TabMoveLeft");
                    SameLineIfFits(ImGui.CalcTextSize(left).X+style.FramePadding.X*2);
                    if(ImGui.SmallButton(left+"###ChatTabLeft") && ChatTabOrder.Move(tabs,index,-1)){chatSettingsPending=true;return;}
                    Tip("ChatWindow.TabMoveTip");
                }
                if(index<tabs.Count-1)
                {
                    var right=localization.Get("ChatWindow.TabMoveRight");
                    SameLineIfFits(ImGui.CalcTextSize(right).X+style.FramePadding.X*2);
                    if(ImGui.SmallButton(right+"###ChatTabRight") && ChatTabOrder.Move(tabs,index,1)){chatSettingsPending=true;return;}
                    Tip("ChatWindow.TabMoveTip");
                }
                if(tabs.Count>1)
                {
                    SameLineIfFits(ImGui.CalcTextSize(localization.Get("ChatWindow.TabRemove")).X+style.FramePadding.X*2);
                    // X-686: the tab is kept a few seconds, the time to undo it (the line above the tabs)
                    if(ImGui.SmallButton(localization.Get("ChatWindow.TabRemove")) && chatTabUndo.Remove(tabs,index,Environment.TickCount64))
                    {
                        ChatMessages.Restart(tab);
                        chatSettingsPending=true;
                        return;
                    }
                    Tip("ChatWindow.TabRemoveTip");
                }
                // X-520 (0.1537): the desktop application's picture of the chat's tabs shows the first tab's channels (X-560's grid).
                if(index==0 && desktopChatChannelsOpen){ImGui.SetNextItemOpen(true);desktopChatChannelsOpen=false;}
                if(TenshoAssistant.UI.UiComponents.TreeNode(localization.Format("ChatWindow.TabChannels",tab.Channels?.Count ?? 0)+"###ChatTabChannels"))
                {
                    try{DrawChatTabFilters(tab);}
                    finally{ImGui.TreePop();}
                }
            }
            finally{ImGui.PopID();}
            ImGui.Separator();
        }
        if(tabs.Count<ChatSettings.MaximumTabs)
        {
            if(ImGui.SmallButton(localization.Get("ChatWindow.TabAdd"))){AddChatTab(tabs);chatSettingsPending=true;}
            Tip("ChatWindow.TabAddTip");
        }
        DrawChatTabDefaults(options,tabs);
    }

    // X-686 (the user's report of 20/11: a tab removed by mistake at the first click, and no way to get the default tabs back): « Remove »
    // keeps the tab a few seconds to undo it; a default tab removed comes back alone; all of them come back after a question.
    private readonly ChatTabUndo chatTabUndo=new();

    private (string General,string Battle,string Event,string Social) ChatTabNames()
        =>(localization.Get("ChatWindow.TabAll"),localization.Get("ChatWindow.TabBattle"),localization.Get("ChatWindow.TabEvent"),
           localization.Get("ChatWindow.TabSocial"));

    private void DrawChatTabUndo(List<ChatTabSettings> tabs)
    {
        var now=Environment.TickCount64;
        if(chatTabUndo.Waiting(now) is not { } removed)return;
        ImGui.TextColored(Warning,localization.Format("ChatWindow.TabRemoved",removed.Name,chatTabUndo.SecondsLeft(now)));
        var undo=localization.Get("ChatWindow.TabUndo");
        SameLineIfFits(ImGui.CalcTextSize(undo).X+ImGui.GetStyle().FramePadding.X*2);
        if(ImGui.SmallButton(undo+"###ChatTabUndo") && chatTabUndo.Undo(tabs,now))
        {
            ChatMessages.Restart(removed);
            chatSettingsPending=true;
        }
        Tip("ChatWindow.TabUndoTip");
    }

    private void DrawChatTabDefaults(ChatSettings options,List<ChatTabSettings> tabs)
    {
        var names=ChatTabNames();
        foreach(var (place,missing) in ChatSettings.MissingDefaults(tabs,names.General,names.Battle,names.Event,names.Social))
        {
            if(tabs.Count>=ChatSettings.MaximumTabs)break;
            var label=localization.Format("ChatWindow.TabAddDefault",missing.Name);
            SameLineIfFits(ImGui.CalcTextSize(label).X+ImGui.GetStyle().FramePadding.X*2);
            if(ImGui.SmallButton(label+"###ChatTabAddDefault"+place) && ChatSettings.AddDefault(tabs,place,missing))
            {
                chatTabUndo.Replaced(missing);   // X-696 M1: the removed tab it stands for no longer comes back as a second one
                chatSettingsPending=true;
            }
            Tip("ChatWindow.TabAddDefaultTip");
        }
        if(ImGui.SmallButton(localization.Get("ChatWindow.TabsReset")+"###ChatTabsReset"))ImGui.OpenPopup("###ChatTabsResetAsk");
        Tip("ChatWindow.TabsResetTip");
        var open=true;
        if(!ImGui.BeginPopupModal(localization.Get("ChatWindow.TabsResetTitle")+"###ChatTabsResetAsk",ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*30);
            ImGui.TextUnformatted(localization.Get("ChatWindow.TabsResetText"));
            ImGui.PopTextWrapPos();
            if(ImGui.Button(localization.Get("ChatWindow.TabsResetYes")+"###ChatTabsResetYes"))
            {
                foreach(var tab in tabs)ChatMessages.Restart(tab);
                options.ResetTabs(names.General,names.Battle,names.Event,names.Social);
                chatTabUndo.Forget();
                chatSettingsPending=true;
                ImGui.CloseCurrentPopup();
            }
            var cancel=localization.Get("ChatWindow.HistoryCancel");
            SameLineIfFits(ImGui.CalcTextSize(cancel).X+ImGui.GetStyle().FramePadding.X*2);
            if(ImGui.Button(cancel+"###ChatTabsResetNo"))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>X-565 (0.1565): the next control on the same line when the width left holds it, else on the next line.</summary>
    private static void SameLineIfFits(float width)
    {
        ImGui.SameLine();
        if(!Chat.ChatFormat.FitsOnLine(ImGui.GetContentRegionAvail().X,width))ImGui.NewLine();
    }

    private void ChatTabChanged(ChatTabSettings tab){ChatMessages.Restart(tab);chatSettingsPending=true;}

    /// <summary>X-484 (0.1482): what a tab shows. By channels (as before), with a button to the game's own filters; or by the game's log filters
    /// (LogFilter sheet, names in the client's language): Discussion, Announcements and the combat lines by subject, each filter a kind with
    /// who does it and to whom; presets as the game's tabs, all, none. Then, beyond the game, words to keep or hide.</summary>
    private void DrawChatTabFilters(ChatTabSettings tab)
    {
        var table=ChatFilterTableNow();
        if(tab.Filters is null || table is not { Rows.Count:>0 })
        {
            if(tab.Filters is not null)EmptyState("ChatWindow.FiltersUnavailable");
            DrawChatTabChannels(tab);
            if(tab.Filters is null && table is { Rows.Count:>0 })
            {
                if(SettingButton("ChatWindow.FiltersUse","ChatWindow.FiltersUseTip")){tab.Filters=table.FromChannels(tab.Channels ?? []).ToList();tab.Unlisted=true;ChatTabChanged(tab);}
            }
            DrawChatTabWords(tab);
            return;
        }
        var filters=tab.Filters;
        foreach(var (key,ids) in new (string,Func<IEnumerable<uint>>)[]
        {
            ("ChatWindow.PresetGeneral",()=>table.Preset(ChatFilterTable.GeneralTab)),("ChatWindow.PresetBattle",()=>table.Preset(ChatFilterTable.BattleTab)),
            ("ChatWindow.PresetEvent",()=>table.Preset(ChatFilterTable.EventTab)),("ChatWindow.PresetAll",()=>table.AllIds),("ChatWindow.PresetNone",()=>[]),
        })
        {
            if(ImGui.SmallButton(localization.Get(key))){tab.Filters=ids().ToList();ChatTabChanged(tab);return;}
            Tip(key+"Tip");
            ImGui.SameLine();
        }
        if(ImGui.SmallButton(localization.Get("ChatWindow.FiltersChannels"))){tab.Filters=null;ChatTabChanged(tab);return;}
        Tip("ChatWindow.FiltersChannelsTip");
        CheckSetting("ChatWindow.Unlisted","ChatWindow.UnlistedTip",()=>tab.Unlisted,value=>{tab.Unlisted=value;ChatTabChanged(tab);});
        DrawChatFilterGroup(tab,filters,table.Rows.Where(row=>row.Category==ChatFilterTable.Discussion),localization.Get("ChatWindow.FilterDiscussion"),"Discussion");
        DrawChatFilterGroup(tab,filters,table.Rows.Where(row=>row.Category==ChatFilterTable.Announcements),localization.Get("ChatWindow.FilterAnnouncements"),"Announcements");
        var combat=table.Rows.Where(row=>ChatFilterTable.IsCombat(row.Category)).ToList();
        if(combat.Count>0 && TenshoAssistant.UI.UiComponents.TreeNode(ChatFilterLabel(localization.Get("ChatWindow.FilterCombat"),combat,filters)+"###ChatFilterCombat"))
        {
            try
            {
                // X-484 (0.1518): the game's two ways in: each subject with its kinds of line, and each kind of line for every subject.
                if(TenshoAssistant.UI.UiComponents.TreeNode(ChatFilterLabel(localization.Get("ChatWindow.FilterBySubject"),combat,filters)+"###ChatFilterBySubject"))
                {
                    try
                    {
                        foreach(var subject in combat.GroupBy(row=>row.Category))
                            DrawChatFilterGroup(tab,filters,subject,localization.Get("ChatWindow.FilterSubject"+subject.Key),"Subject"+subject.Key);
                    }
                    finally{ImGui.TreePop();}
                }
                if(TenshoAssistant.UI.UiComponents.TreeNode(ChatFilterLabel(localization.Get("ChatWindow.FilterByGenre"),combat,filters)+"###ChatFilterByGenre"))
                {
                    try{DrawChatFilterGenres(tab,filters,table);}
                    finally{ImGui.TreePop();}
                }
            }
            finally{ImGui.TreePop();}
        }
        // Other groups the game may add one day: under their number, never lost.
        foreach(var other in table.Rows.Where(row=>row.Category is not ChatFilterTable.Discussion and not ChatFilterTable.Announcements && !ChatFilterTable.IsCombat(row.Category)).GroupBy(row=>row.Category))
            DrawChatFilterGroup(tab,filters,other,localization.Format("ChatWindow.FilterGroup",other.Key),"Group"+other.Key);
        DrawChatTabWords(tab);
    }

    private static string ChatFilterLabel(string name,IEnumerable<ChatFilterRow> rows,List<uint> filters)
    {
        var list=rows as IReadOnlyCollection<ChatFilterRow> ?? rows.ToList();
        return $"{name} ({list.Count(row=>filters.Contains(row.Id))}/{list.Count})";
    }

    private void DrawChatFilterGroup(ChatTabSettings tab,List<uint> filters,IEnumerable<ChatFilterRow> rows,string name,string id)
    {
        var list=rows.ToList();
        if(list.Count==0 || !TenshoAssistant.UI.UiComponents.TreeNode(ChatFilterLabel(name,list,filters)+"###ChatFilter"+id))return;
        try
        {
            // The whole group on or off at once, then each filter of the game.
            if(ImGui.SmallButton(localization.Get("ChatWindow.GroupAll"))){foreach(var row in list)if(!filters.Contains(row.Id))filters.Add(row.Id);ChatTabChanged(tab);}
            Tip("ChatWindow.GroupAllTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("ChatWindow.GroupNone"))){filters.RemoveAll(chosen=>list.Any(row=>row.Id==chosen));ChatTabChanged(tab);}
            Tip("ChatWindow.GroupNoneTip");
            DrawChatCheckGrid("###ChatFilterRows"+id,list,row=>row.Name,row=>"ChatFilterRow"+row.Id,row=>filters.Contains(row.Id),(row,shown)=>
            {
                if(shown)filters.Add(row.Id);else filters.Remove(row.Id);
                ChatTabChanged(tab);
            },row=>{TipText(()=>ChatHelpText(Chat.ChatHelp.FilterTip(row,localization.Get)));});
        }
        finally{ImGui.TreePop();}
    }

    /// <summary>X-484 (0.1518): one box per kind of combat line (damage dealt, damage taken…), for every subject at once; ticked when all of
    /// them are, its label counting them.</summary>
    private void DrawChatFilterGenres(ChatTabSettings tab,List<uint> filters,ChatFilterTable table)
    {
        DrawChatCheckGrid("###ChatFilterGenres",table.Genres(),genre=>
        {
            var key=ChatFilterTable.GenreKey(genre.Genre);
            return ChatFilterLabel(localization.Has(key) ? localization.Get(key):genre.Rows[0].Name,genre.Rows,filters);
        },genre=>"ChatFilterGenre"+ChatFilterTable.GenreKey(genre.Genre),genre=>genre.Rows.All(row=>filters.Contains(row.Id)),(genre,all)=>
        {
            var rows=genre.Rows;
            filters.RemoveAll(chosen=>rows.Any(row=>row.Id==chosen));
            if(all)filters.AddRange(rows.Select(row=>row.Id));
            ChatTabChanged(tab);
        },genre=>{TipText(()=>ChatGenreTip(genre.Rows.Count(row=>filters.Contains(row.Id)),genre.Rows.Count));});
    }

    private string ChatGenreTip(int on,int count)=>localization.Format("ChatWindow.GenreTip",on,count);

    /// <summary>X-484 (0.1482, beyond the game): words kept or hidden in this tab.</summary>
    private void DrawChatTabWords(ChatTabSettings tab)
    {
        ListSetting("ChatWindow.KeepWords","ChatWindow.KeepWordsTip",()=>tab.KeepWords,value=>{tab.KeepWords=value;ChatTabChanged(tab);},300,()=>tab.WordsMatchCase);
        ListSetting("ChatWindow.HideWords","ChatWindow.HideWordsTip",()=>tab.HideWords,value=>{tab.HideWords=value;ChatTabChanged(tab);},300,()=>tab.WordsMatchCase);
        CheckSetting("ChatWindow.WordsMatchCase","ChatWindow.WordsMatchCaseTip",()=>tab.WordsMatchCase,value=>{tab.WordsMatchCase=value;ChatTabChanged(tab);});
        CheckSetting("ChatWindow.FoldRepeats","ChatWindow.FoldRepeatsTip",()=>tab.FoldRepeats,value=>{tab.FoldRepeats=value;ChatTabChanged(tab);});
    }

    /// <summary>X-484 (0.1482, beyond the game): players hidden from every tab, players whose name stands out.</summary>
    private void DrawChatPlayers(ChatSettings options)
    {
        ListSetting("ChatWindow.HiddenPlayers","ChatWindow.HiddenPlayersTip",()=>options.HiddenPlayers,value=>{options.HiddenPlayers=value;RestartChatTabs(options);},600);
        ListSetting("ChatWindow.HighlightedPlayers","ChatWindow.HighlightedPlayersTip",()=>options.HighlightedPlayers,value=>{options.HighlightedPlayers=value;chatSettingsPending=true;},600);
        ListSetting("ChatWindow.AlertWords","ChatWindow.AlertWordsTip",()=>options.AlertWords,value=>{options.AlertWords=value;chatSettingsPending=true;},600);
        // X-447 B7 (0.1643): the tab holding an alert not seen blinks.
        BoolSetting("ChatWindow.BlinkAlerts","ChatWindow.BlinkAlertsTip",()=>configuration.Chat.BlinkAlerts,value=>configuration.Chat.BlinkAlerts=value,c=>c.Chat.BlinkAlerts);
        // X-447 B7 (0.1646, after Prat): your character's first name said by a player alerts as the words do.
        BoolSetting("ChatWindow.AlertOwnName","ChatWindow.AlertOwnNameTip",()=>configuration.Chat.AlertOwnName,value=>configuration.Chat.AlertOwnName=value,c=>c.Chat.AlertOwnName);
        // X-447 B7 (0.1646, after Prat and WIM): the game's sound a message with an alert plays (0: none), to listen to.
        IntSetting("ChatWindow.AlertSound","ChatWindow.AlertSoundTip",()=>configuration.Chat.AlertSound,value=>configuration.Chat.AlertSound=value,0,Alarms.MacroSound.Last,0);
        ImGui.SameLine();
        ImGui.BeginDisabled(!Alarms.MacroSound.Valid(configuration.Chat.AlertSound));
        try{if(ImGui.SmallButton(localization.Get("Alarms.Try")+"##ChatAlertSoundTry"))alarmService?.PreviewChatSound(configuration.Chat.AlertSound);}
        finally{ImGui.EndDisabled();}
        Tip("Advanced.TryTip");
        // X-447 B7 (0.1647, after WIM): the alerts not seen counted in the game's server bar.
        BoolSetting("ChatWindow.ServerBarAlerts","ChatWindow.ServerBarAlertsTip",()=>configuration.Chat.ServerBarAlerts,value=>configuration.Chat.ServerBarAlerts=value,c=>c.Chat.ServerBarAlerts);
        // X-447 B10 (0.1654, after WIM): each player's tells in a tab of their own (memory only).
        BoolSetting("ChatWindow.Conversations","ChatWindow.ConversationsTip",()=>configuration.Chat.Conversations,value=>configuration.Chat.Conversations=value,c=>c.Chat.Conversations);
    }

    private void SetHiddenPlayers(ChatSettings options,string value)
    {
        options.HiddenPlayers=value;
        RestartChatTabs(options);
    }

    private void RestartChatTabs(ChatSettings options)
    {
        foreach(var tab in ChatTabs(options))ChatMessages.Restart(tab);
        foreach(var tab in chatConversations.Tabs)ChatMessages.Restart(tab);
        chatSettingsPending=true;
    }

    /// <summary>X-508 (0.1566): the menu of a message, for its sender: hide this player, put them forward (or no longer).</summary>
    private void DrawChatSenderMenu(ChatSettings options,ChatRecord said)
    {
        var player=ChatTextFilter.PlayerName(said.Sender);
        if(player.Length==0)return;
        // X-669 M2 (review of 0.1627, R25): in capture mode the entries name the player by their stand-in, as the line does (a menu open in a
        // picture showed the real name); what they do is unchanged (the real name copied, hidden or put forward).
        var shown=ChatMenuName(player,options);
        ImGui.Separator();
        // X-455 E3 (0.1617): the player's name with their world, copied (nothing kept, R04).
        if(ImGui.Selectable(localization.Format("ChatWindow.MenuCopyPlayer",shown)))
            ImGui.SetClipboardText(ChatCopy.Player(player,ChatSenders?.For(said.Id) is { World:>0 } source && ChatPlayers is { } players ? players.World(source.World):ChatCopy.WorldOf(said.Sender)));
        if(ImGui.Selectable(localization.Format("ChatWindow.MenuHidePlayer",shown)) && ChatTextFilter.Add(options.HiddenPlayers,player,600) is { } hidden)
            SetHiddenPlayers(options,hidden);
        if(ChatTextFilter.IsPlayer(said.Sender,ChatTextFilter.Words(options.HighlightedPlayers)))
        {
            if(ImGui.Selectable(localization.Format("ChatWindow.MenuUnhighlightPlayer",shown))){options.HighlightedPlayers=ChatTextFilter.RemovePlayer(options.HighlightedPlayers,said.Sender);chatSettingsPending=true;}
        }
        else if(ImGui.Selectable(localization.Format("ChatWindow.MenuHighlightPlayer",shown)) && ChatTextFilter.Add(options.HighlightedPlayers,player,600) is { } forward)
        {
            options.HighlightedPlayers=forward;chatSettingsPending=true;
        }
        // X-447 B10 (0.1654): the conversation with this player, opened (or the one open) and shown; a player's own lines open none.
        if(options.Conversations && ChatSpoken(said) && ImGui.Selectable(localization.Format("ChatWindow.MenuConversation",shown))
            && chatConversations.Open(said.Sender,DateTimeOffset.Now,ChatConversationBusy,options.ColorOf(ChatChannel.TellIn),ChatConversationClosed) is { } conversation)
            chatSelectTab=conversation;
        DrawChatPlayerGameEntries(said);
    }

    /// <summary>X-447 B10 (0.1654): a message a player said (a spoken channel), not one of yours (no conversation with yourself).</summary>
    private static bool ChatSpoken(ChatRecord said)=>ChatChannels.Spoken(said.Channel) && said.Channel!=ChatChannel.TellOut && !ChatAlert.Sent(said)
        || said.Channel==ChatChannel.TellOut;

    /// <summary>X-669 M2 (review of 0.1627): the player's name as a message's menu writes it: their stand-in in capture mode.</summary>
    private static string ChatMenuName(string player,ChatSettings options)=>ChatCopy.MenuName(player,options.CaptureNames);

    /// <summary>X-654 (0.1602): the senders of the messages and the game's entries of a player's menu (null: none, as in the desktop application).</summary>
    internal Chat.ChatSources? ChatSenders{get;set;}
    internal Chat.ChatPlayerActions? ChatPlayers{get;set;}

    /// <summary>
    /// X-654 (0.1602, the user's request of 20/11, choice « by the game's menu »): after Tensho's entries, the game's own for this player, in the
    /// client's words (ChatPlayerMenu); an entry that needs the game's identifiers of the message is greyed without them (a message from the
    /// history on disk, or not given by the game). Each one only on the player's click (R03.3).
    /// </summary>
    /// <remarks>X-679 (0.1660, the user's report of 20/11: none of the game's entries, even for a new message): the sender is found by the
    /// message, else by the player (the identifiers are the character's), else by the name its text gives (ChatSources.Find); without one the
    /// entries are greyed with the reason in their tooltip, never left out. Your own lines and what no player says have none.</remarks>
    private void DrawChatPlayerGameEntries(ChatRecord said)
    {
        if(ChatPlayers is not { Available:true } actions || ChatSenders is not { } senders)return;
        if(said.Channel!=ChatChannel.TellOut && Chat.ChatAlert.Sent(said))return;
        var (name,worldName)=Chat.ChatPlayerMenu.Named(said.Sender);
        var spoken=ChatChannels.Spoken(said.Channel);
        var (source,how)=senders.Find(said.Id,spoken ? name:"",worldName.Length>0 ? actions.WorldId(worldName):actions.CurrentWorldId());
        if(source is null && !spoken)return;
        actions.SaidFound(how,said.Kind);
        var inRange=source is not null && actions.InRange(source) is not null;
        var entries=Chat.ChatPlayerMenu.Entries(source,said.Channel,inRange,source is not null && actions.CanInvite(source));
        if(entries.Count==0)return;
        ImGui.Separator();
        // X-680 (0.1662): the game's « Blocking functions » submenu holds its entries (consecutive in the list); its text missing: drawn flat.
        var blockLabel=actions.Text(Chat.ChatPlayerMenu.BlockRow);
        bool blockStarted=false,blockOpen=false;
        foreach(var entry in entries)
        {
            if(entry.Block && !blockStarted && blockLabel.Length>0){blockStarted=true;blockOpen=ImGui.BeginMenu(blockLabel+"###ChatPlayerBlock");}
            else if(!entry.Block && blockOpen){ImGui.EndMenu();blockOpen=false;}
            if(entry.Block && blockStarted && !blockOpen)continue;
            var text=entry.AddonRow!=0 ? actions.Text(entry.AddonRow):localization.Get("ChatWindow.MenuGameMenu");
            if(text.Length==0)continue;
            if(ImGui.Selectable(text+"###ChatPlayer"+entry.Action,false,entry.Enabled ? ImGuiSelectableFlags.None:ImGuiSelectableFlags.Disabled)
               && entry.Enabled && source is not null)
                actions.Run(entry.Action,source,said.Channel);
            if(ChatPlayerWhyKey(entry.Why) is { Length:>0 } why)Tip(why);
        }
        if(blockOpen)ImGui.EndMenu();
    }

    /// <summary>X-679 (0.1660): the tooltip of a greyed entry, saying why.</summary>
    private static string ChatPlayerWhyKey(Chat.ChatPlayerWhy why)=>why switch
    {
        Chat.ChatPlayerWhy.NoSender=>"ChatWindow.PlayerWhyNoSender",
        Chat.ChatPlayerWhy.NoIdentifiers=>"ChatWindow.PlayerWhyNoIdentifiers",
        Chat.ChatPlayerWhy.NoAccount=>"ChatWindow.PlayerWhyNoAccount",
        Chat.ChatPlayerWhy.NotInRange=>"ChatWindow.PlayerWhyNotInRange",
        Chat.ChatPlayerWhy.GameOnly=>"ChatWindow.PlayerWhyGameOnly",
        Chat.ChatPlayerWhy.NoReplyMode=>"ChatWindow.PlayerWhyNoReplyMode",
        _=>"",
    };

    /// <summary>X-476 (0.1481): a new tab with what people say, from the settings or from the « + » at the end of the window's tabs.</summary>
    private void AddChatTab(List<ChatTabSettings> tabs)
        =>tabs.Add(new(){Name=localization.Format("ChatWindow.TabNew",tabs.Count+1),Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()});

    private void DrawChatTabChannels(ChatTabSettings tab)
    {
        tab.Channels??=[];
        DrawChatCheckGrid("###ChatTabChannelTable",ChatChannels.All,ChatChannelName,channel=>"ChatChannel"+channel,tab.Shows,(channel,shown)=>
        {
            if(shown)tab.Channels.Add(channel);else tab.Channels.Remove(channel);
            ChatMessages.Restart(tab);chatSettingsPending=true;
        },channel=>{TipText(()=>ChatHelpText(Chat.ChatHelp.ChannelTip(channel)));});
    }

    /// <summary>
    /// X-560 (0.1528, the user's report of 17/11: « chat settings still not framed », the third column under the window's right edge and the
    /// names cut): a grid of boxes with as many columns as the width left holds (the scroll bar outside it, ImGui's content region), each as
    /// wide as the widest name, so no name is cut; a name wider than the window itself is written beside its box, wrapped.
    /// </summary>
    private void DrawChatCheckGrid<T>(string id,IReadOnlyList<T> items,Func<T,string> name,Func<T,string> key,Func<T,bool> get,Action<T,bool> set,Action<T> drawTip)
    {
        if(items.Count==0)return;
        var style=ImGui.GetStyle();
        var box=ImGui.GetFrameHeight()+style.ItemInnerSpacing.X;
        var widest=0f;
        foreach(var item in items)widest=Math.Max(widest,ImGui.CalcTextSize(name(item)).X);
        var columns=ChatFormat.GridColumns(ImGui.GetContentRegionAvail().X,box+widest+style.CellPadding.X*2+style.ItemSpacing.X,items.Count);
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable(id,columns))return;
        try
        {
            foreach(var item in items)
            {
                ImGui.TableNextColumn();
                var label=name(item);
                var fits=box+ImGui.CalcTextSize(label).X<=ImGui.GetContentRegionAvail().X;
                var shown=get(item);
                if(ImGui.Checkbox((fits ? label:"")+"###"+key(item),ref shown))set(item,shown);
                drawTip(item);
                if(fits)continue;
                ImGui.SameLine();ImGui.TextWrapped(label);
                drawTip(item);
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawChatColorSettings(ChatSettings options)
    {
        DrawChatProfileWorn(options);
        EmptyState("ChatWindow.ColorsHelp");
        foreach(var channel in ChatChannels.All)
        {
            ImGui.PushID((int)channel);
            try
            {
                var (red,green,blue)=ChatChannels.Unpack(options.ColorOf(channel));var rgb=new Vector3(red,green,blue);
                if(ImGui.ColorEdit3(ChatChannelName(channel)+"###ChatColor",ref rgb,ImGuiColorEditFlags.NoInputs))
                {
                    options.Colors??=[];
                    options.Colors[channel]=ChatChannels.Pack(rgb.X,rgb.Y,rgb.Z);
                    chatSettingsPending=true;
                }
                Tip("ChatWindow.ColorTip");
                if(DefaultButton("ChatColor",options.Colors?.ContainsKey(channel)==true)){options.Colors?.Remove(channel);chatSettingsPending=true;}
                // X-447 B4 (0.1621): the channel's short name, for the channels whose messages show it.
                if(ChatChannels.Spoken(channel))
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(70*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                    var shortName=options.ChannelNames?.GetValueOrDefault(channel) ?? "";
                    if(ImGui.InputTextWithHint("###ChatChannelName",ChatChannelShort(channel),ref shortName,ChatSettings.MaximumChannelName))
                    {
                        options.ChannelNames??=[];
                        if(shortName.Trim().Length==0)options.ChannelNames.Remove(channel);else options.ChannelNames[channel]=shortName;
                        chatSettingsPending=true;
                    }
                    Tip("ChatWindow.ChannelNameTip");
                    // X-447 B7 (0.1646, after Prat): the channel's sound (none by default); what you send plays none.
                    if(channel!=ChatChannel.TellOut)DrawChatChannelSound(options,channel);
                }
            }
            finally{ImGui.PopID();}
        }
    }

    /// <summary>X-447 B7 (0.1646, after Prat): the sound a spoken channel's messages play: none, or one of the game's sixteen (the macros'
    /// &lt;se.N&gt;); a sound picked is played once, to hear it.</summary>
    private void DrawChatChannelSound(ChatSettings options,ChatChannel channel)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        var sound=options.SoundOf(channel);
        if(ImGui.BeginCombo("###ChatChannelSound",ChatSoundName(sound)))
        {
            try
            {
                for(var choice=0;choice<=Alarms.MacroSound.Last;choice++)
                {
                    if(!ImGui.Selectable(ChatSoundName(choice),choice==sound))continue;
                    options.Sounds??=[];
                    if(choice==0)options.Sounds.Remove(channel);
                    else{options.Sounds[channel]=choice;alarmService?.PreviewChatSound(choice);}
                    chatSettingsPending=true;
                }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("ChatWindow.ChannelSoundTip");
    }

    private string ChatSoundName(int sound)=>sound==0 ? localization.Get("ChatWindow.NoSound"):localization.Format("ChatWindow.SoundNumber",sound);

    // Developer mode: the chat kinds seen (counts only, no text), to find the kinds still grouped under « Other ».
    private void DrawChatKinds()
    {
        ImGui.TextDisabled(localization.Format("ChatWindow.Kept",ChatMessages.Count,ChatMessages.LastId));
        if(ImGui.SmallButton(localization.Get("ChatWindow.LogKinds")))
            log.Information("Chat window, kinds seen: {0}.",string.Join(", ",ChatMessages.KindCounts().Select(row=>$"{row.Key} {ChatChannels.From(row.Key)} {row.Value}")));
        Tip("ChatWindow.LogKindsTip");
    }

    private void DrawChatWindowContents()
    {
        // X-485 (0.1479): put at the back of the windows when it appears (it is never brought to the front afterwards).
        var frameNow=ImGui.GetFrameCount();
        if(frameNow!=chatLastFrame+1)ImGuiP.BringWindowToDisplayBack(ImGuiP.GetCurrentWindow());
        chatLastFrame=frameNow;
        chatAt=ImGui.GetWindowPos();chatSize=ImGui.GetWindowSize();
        // X-498 M1 (0.1480): faded, the whole window is (the alpha pushed before Begin by PanelWindow, Fade), its frame and title bar too.
        chatZonesNext.Clear();
        try{DrawChatWindowBody();}
        finally
        {
            // X-447 D1 (0.1620): the zones drawn now decide the next frame's flags; a menu open (the chat's, or another) takes the clicks.
            (chatZones,chatZonesNext)=(chatZonesNext,chatZones);
            chatMenuOpen=ImGui.IsPopupOpen("",ImGuiPopupFlags.AnyPopupId|ImGuiPopupFlags.AnyPopupLevel);
        }
    }

    // X-476 (0.1481): the tab chosen in the tab bar (its lines are drawn outside the bar, over or under it).
    private int chatTabShown;

    /// <summary>X-476 (0.1481): the tab bar over the lines (as before) or under them (setting); the bar's items are empty, the chosen tab's lines
    /// are drawn after the bar, or before it with the bar's height kept free at the bottom.</summary>
    private void DrawChatWindowBody()
    {
        // X-558 (0.1533): after a load, the chat's code is compiled ahead; the window waits for it instead of compiling it in one frame
        // (« Chat 62 ms » on the reload of 17/11 13:05). X-564 (0.1616): its own part, first, when the window is open at the load (it waited
        // for the whole early phase, 8 to 25 s).
        if(!WarmupChatDone){EmptyState("ChatWindow.Loading");return;}
        // X-564 (0.1631): when the lines are first drawn after the load, said once (the measure of X-564).
        if(!chatFirstLinesLogged)
        {
            chatFirstLinesLogged=true;
            log.Information(FormattableString.Invariant($"Chat window: lines first drawn {WarmupSinceMade:F0} ms after the load (X-564), the panels' gate {(PanelsOpen ? "open":"still closed")}."));
            // X-564 (0.1632): that frame's time, and how much of it the game's thread spent compiling (34 ms of chat on 20/11 17:50).
            // X-564 (0.1633): in developer mode, the names of the methods compiled meanwhile on this thread (the runtime's JIT events).
            var names=developerMode ? new Diagnostics.JitNames():null;
            names?.Start();
            var jitBefore=System.Runtime.JitInfo.GetCompilationTime(true);
            var methodsBefore=System.Runtime.JitInfo.GetCompiledMethodCount(true);
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            try{DrawChatWindowLines();}
            finally
            {
                names?.Stop();
                log.Information(FormattableString.Invariant($"Chat window: its first frame with lines {System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms{Diagnostics.JitWarmup.CompiledText((System.Runtime.JitInfo.GetCompilationTime(true)-jitBefore).TotalMilliseconds,System.Runtime.JitInfo.GetCompiledMethodCount(true)-methodsBefore)} (X-564)."));
                if(names is not null)LogChatFrameJit(names,Diagnostics.ThreadClock.SystemId());
            }
            return;
        }
        DrawChatWindowLines();
    }

    /// <summary>X-564 (0.1633): the methods the chat's first frame compiled, named in the log 2 s later (the runtime's events come late), then
    /// the listener let go; off the game's thread.</summary>
    private void LogChatFrameJit(Diagnostics.JitNames names,long thread)
    {
        _=Task.Delay(2000).ContinueWith(_=>
        {
            try{log.Information("Chat window: methods compiled on the game's thread in its first frame with lines (X-564){0}: {1}.",names.Heard(thread) ? "":" (window not heard whole)",string.Join(", ",names.Names(thread)) is { Length:>0 } list ? list:"none");}
            catch(Exception error){log.Warning("Chat window: the first frame's compilations could not be named: {0}",error.Message);}
            finally{names.Dispose();}
        },TaskScheduler.Default);
    }

    /// <summary>X-564 (0.1632): the chat window's tabs and lines, once its code is compiled (the first time timed for the log).</summary>
    private void DrawChatWindowLines()
    {
        var options=ChatOptions;
        FollowChatContext(options);
        UpdateChatConversations(options);
        var tabs=ChatWindowTabs(options);
        var table=ChatFilterTableNow();
        // X-558 (0.1532): the tabs' new lines, measured apart in developer mode (the « Slow frame » line).
        var refreshStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        foreach(var tab in tabs)ChatMessages.ViewOf(tab).Refresh(ChatMessages,tab,options.BoundedMaxLines,table,options);
        // X-447 B7 (0.1647): the alerts not seen, for the server bar.
        Volatile.Write(ref chatAlertsUnseen,ChatAlert.Unseen(tabs.Select(ChatMessages.ViewOf),chatUnseen));
        if(developerMode)SlowFrames.Add(ImGui.GetFrameCount(),"Chat refresh",System.Diagnostics.Stopwatch.GetElapsedTime(refreshStarted).TotalMilliseconds);
        chatHighlighted=ChatTextFilter.Words(options.HighlightedPlayers);
        chatAlertWords=options.AlertWords;
        // X-467 (0.1504): reduced to its tabs, the lines are not drawn (never while it stands over the game's hidden messages).
        var reduced=chatReduced && !chatFollowing;
        if(options.TabsAtBottom && !reduced)DrawChatShownTab(tabs,options,-ImGui.GetFrameHeightWithSpacing());
        DrawChatTabBar(tabs,options);
        if(!options.TabsAtBottom && !reduced)DrawChatShownTab(tabs,options,0);
    }

    private void DrawChatTabBar(List<ChatTabSettings> tabs,ChatSettings options)
    {
        // X-447 D1 (0.1620): the whole bar answers the mouse (its tabs and the buttons at its end).
        var barAt=ImGui.GetCursorScreenPos();
        chatZonesNext.Add((barAt.X,barAt.Y,ImGui.GetContentRegionAvail().X,ImGui.GetFrameHeightWithSpacing()));
        // X-505 (0.1488): the game's tabs: rounded, grey, the one shown marked by a dot.
        var game=options.GameTabs;
        if(game)
        {
            static Vector4 Colour((float R,float G,float B,float A) c)=>new(c.R,c.G,c.B,c.A);
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding,ChatLook.TabRounding);
            ImGui.PushStyleColor(ImGuiCol.Tab,Colour(ChatLook.Tab));
            ImGui.PushStyleColor(ImGuiCol.TabHovered,Colour(ChatLook.TabHovered));
            ImGui.PushStyleColor(ImGuiCol.TabActive,Colour(ChatLook.TabShown));
        }
        try{DrawChatTabItems(tabs,options);}
        finally{if(game){ImGui.PopStyleColor(3);ImGui.PopStyleVar();}}
    }

    private void DrawChatTabItems(List<ChatTabSettings> tabs,ChatSettings options)
    {
        // X-687 (0.1663): the tabs dragged with the mouse (Reorderable); each tab's id is its own (not its place), so ImGui keeps it while the
        // order changes. The order the bar shows is read after it (ChatTabsAfterDrag).
        if(!ImGui.BeginTabBar("###TenshoChatTabs"+chatTabBarGeneration,ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.Reorderable))return;
        chatTabPlaces.Clear();
        var dragged=false;
        var fresh=chatTabBarFresh;
        chatTabBarFresh=false;
        try
        {
            // X-447 B7 (0.1647): the server bar's entry clicked: the first tab holding an alert not seen is selected.
            var alertTab=-1;
            // X-676 (0.1652): with the tabs' names hidden no tab item takes SetSelected: the tab holding the alert is shown directly.
            if(Volatile.Read(ref chatShowAlert))
            {
                Volatile.Write(ref chatShowAlert,false);
                alertTab=ChatAlert.FirstWithAlert(tabs.Select(ChatMessages.ViewOf));
                chatTabShown=ChatAlert.ShownAfterClick(chatTabShown,alertTab,options.HideTabs);
            }
            // X-447 B10 (0.1654): a conversation opened from a player's menu is shown, as the tab of a clicked alert.
            if(chatSelectTab is { } wanted)
            {
                chatSelectTab=null;
                alertTab=tabs.IndexOf(wanted);
                chatTabShown=ChatAlert.ShownAfterClick(chatTabShown,alertTab,options.HideTabs);
            }
            // X-447 D4 (0.1618): the tabs' names hidden, the tab shown kept; the buttons at the end stay (one shows the tabs again).
            for(var index=0;index<tabs.Count && !options.HideTabs;index++)
            {
                var tab=tabs[index];
                var view=ChatMessages.ViewOf(tab);
                bool open;
                var coloured=PushChatTabColor(tab);
                // X-447 B7 (0.1643): a tab not shown holding an alert not seen blinks: lit in the accent colour every other half second.
                var blink=options.BlinkAlerts && view.Alerts>0 && index!=chatTabShown && ChatLook.Blink(ImGui.GetTime());
                if(blink)ImGui.PushStyleColor(ImGuiCol.Tab,Accent with{W=0.75f});
                var select=index==alertTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
                // X-687 (0.1663): a bar made again in the list's order selects the tab shown.
                if(fresh && index==chatTabShown)select=ImGuiTabItemFlags.SetSelected;
                try{open=ImGui.BeginTabItem(ChatLook.TabLabel(ChatAlert.Mark(ChatFormat.TabLabel(ChatConversations.Label(tab,options.CaptureNames),tab.ShowUnread,view.Unread),view.Alerts),index==chatTabShown,options.GameTabs)+"###ChatTab"+ChatTabKey(tab),select);}
                finally{if(coloured)ImGui.PopStyleColor();if(blink)ImGui.PopStyleColor();}
                chatTabPlaces.Add((tab,ImGui.GetItemRectMin().X));
                if(ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))dragged=true;
                // X-455 E7 (0.1617): a right click on a tab: clear its messages (the other tabs keep theirs), or show them again.
                if(ImGui.BeginPopupContextItem("###ChatTabMenu"+index))
                {
                    try
                    {
                        if(ImGui.Selectable(localization.Get("ChatWindow.ClearTab")))ClearChatTab(tab,true);
                        if(tab.ClearedAt is not null && ImGui.Selectable(localization.Get("ChatWindow.UnclearTab")))ClearChatTab(tab,false);
                        // X-447 B10 (0.1654): a conversation's tab closed (its messages stay in the other tabs).
                        if(tab.Conversation is not null && ImGui.Selectable(localization.Get("ChatWindow.CloseConversation")) && chatConversations.Close(tab))
                            ChatConversationClosed(tab);
                        // X-687 (0.1663): the tab one place to the left or right (a settings' tab; a conversation's stays after them).
                        var all=ChatTabs(options);
                        var at=all.IndexOf(tab);
                        // X-697 M2 (0.1665): in a context with a profile, the move is that profile's order only.
                        if(ChatContexts.ProfileOf(options,chatContextNow) is { } profile)
                        {
                            var own=tabs.Where(all.Contains).ToList();
                            var place=own.IndexOf(tab);
                            if(place>0 && ImGui.Selectable(localization.Get("ChatWindow.TabMoveLeft")) && ChatContexts.Move(profile,all,tab,-1))ChatTabsMoved(tabs);
                            if(place>=0 && place<own.Count-1 && ImGui.Selectable(localization.Get("ChatWindow.TabMoveRight")) && ChatContexts.Move(profile,all,tab,1))ChatTabsMoved(tabs);
                        }
                        else
                        {
                        if(at>0 && ImGui.Selectable(localization.Get("ChatWindow.TabMoveLeft")) && ChatTabOrder.Move(all,at,-1))ChatTabsMoved(tabs);
                        if(at>=0 && at<all.Count-1 && ImGui.Selectable(localization.Get("ChatWindow.TabMoveRight")) && ChatTabOrder.Move(all,at,1))ChatTabsMoved(tabs);
                        }
                    }
                    finally{ImGui.EndPopup();}
                }
                if(!open)continue;
                chatTabShown=index;
                ImGui.EndTabItem();
            }
            DrawChatFrameButtons(options);
        }
        finally{ImGui.EndTabBar();}
        ChatTabsAfterDrag(tabs,options,dragged);
    }

    // X-687 (0.1663): the tabs as the bar placed them in this frame, the bar's number (a new one shows the list's order), each tab's own id.
    private readonly List<(ChatTabSettings Tab,float X)> chatTabPlaces=[];
    private int chatTabBarGeneration;
    private bool chatTabBarFresh,chatTabDragging;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ChatTabSettings,object> chatTabKeys=new();
    private int chatTabKeyNext;
    private int ChatTabKey(ChatTabSettings tab)=>(int)chatTabKeys.GetValue(tab,_=>++chatTabKeyNext);

    /// <summary>
    /// X-687 (0.1663): when a drag ends, the order the bar shows becomes the settings' order (ChatTabOrder.Apply), saved, the tab shown kept;
    /// otherwise the list's order is the truth: a bar showing another order (a tab put back, moved by its menu, a context's tabs) is made
    /// again in the list's order, the tab shown selected.
    /// </summary>
    private void ChatTabsAfterDrag(List<ChatTabSettings> tabs,ChatSettings options,bool dragged)
    {
        if(dragged){chatTabDragging=true;return;}
        var shown=chatTabPlaces.OrderBy(place=>place.X).Select(place=>place.Tab).ToList();
        var same=shown.SequenceEqual(chatTabPlaces.Select(place=>place.Tab));
        if(chatTabDragging)
        {
            chatTabDragging=false;
            // X-697 M2 (0.1665): a drag in a context with a profile changes that profile's order only (the settings' and the others' kept).
            if(!same && ChatContexts.ProfileOf(options,chatContextNow) is { } profile){if(ChatContexts.SetOrder(profile,ChatTabs(options),shown))ChatTabsMoved(tabs);return;}
            if(!same && ChatTabOrder.Apply(ChatTabs(options),shown))ChatTabsMoved(tabs);
            return;
        }
        if(!same){chatTabBarGeneration++;chatTabBarFresh=true;}
    }

    /// <summary>X-687 (0.1663): the order changed: saved, the tab shown kept (selected again at the next frame).</summary>
    private void ChatTabsMoved(List<ChatTabSettings> tabs)
    {
        if(chatTabShown>=0 && chatTabShown<tabs.Count)chatSelectTab=tabs[chatTabShown];
        chatSettingsPending=true;
    }

    private void DrawChatShownTab(List<ChatTabSettings> tabs,ChatSettings options,float height)
    {
        if(tabs.Count==0)return;
        var shown=Math.Clamp(chatTabShown,0,tabs.Count-1);
        chatShownTab=tabs[shown];
        // X-504 M2 (review of 0.1486): the area named after the tab's view (its own number), not the tab's place.
        var view=ChatMessages.ViewOf(tabs[shown]);
        // X-447 B1 (0.1622): the tab shown's newest message (another tab chosen counts as new).
        chatShownNewest=view.Count>0 ? view.RecordAt(view.Count-1).Id:-1;
        // X-447 B8 (0.1638): the search line, then only the tab's messages holding the text typed (a view of their own).
        // X-447 B9 (0.1642): the messages pinned at the top of the tab, then the search line.
        DrawChatPins(tabs[shown],options);
        if(chatSearchOpen)view=DrawChatSearchLine(tabs[shown],view,options);
        DrawChatLines(view,options,height,view.Serial);
    }

    // X-447 B9 (0.1642): the message a pin clicked asks the lines to scroll to (-1: none).
    private long chatScrollTo=-1;

    /// <summary>X-447 B9 (0.1642, the user's choice of 15/11): the messages pinned at the top of the tab shown, one line each (cut with « … »), in
    /// their channel's colour; a click scrolls the tab to the message (a bookmark), the cross unpins it; the whole message on hover. In memory
    /// only (ChatStore.Pins).</summary>
    private void DrawChatPins(ChatTabSettings tab,ChatSettings options)
    {
        var pinned=ChatMessages.Pins(tab);
        for(var at=0;at<pinned.Count;at++)
        {
            var record=pinned[at];
            ImGui.PushID(at);
            try
            {
                if(ToolButton(Dalamud.Interface.FontAwesomeIcon.Times,"ChatWindow.UnpinTip",true))ChatMessages.Unpin(tab,record);
                ImGui.SameLine();
                var text=ChatFormat.Plain(record,options,ChatChannelShort,ChatLinkName).ReplaceLineEndings(" ");
                ImGui.TextColored(ChatColor(options.ColorOf(record.Channel)),ChatLook.Shorten(text,ImGui.GetContentRegionAvail().X,line=>ImGui.CalcTextSize(line).X));
                if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                TipText(()=>localization.Format("ChatWindow.PinnedTip",text));
                if(ImGui.IsItemClicked(ImGuiMouseButton.Left))chatScrollTo=record.Id;
            }
            finally{ImGui.PopID();}
        }
    }

    // X-447 B8 (0.1638): the search line open, its text (in memory only), and the box to focus once opened.
    private bool chatSearchOpen,chatSearchFocus;
    private string chatSearchText="";

    /// <summary>X-447 B8 (0.1638): the search box over the lines; with text typed, the view of the tab's messages holding it (kept by the
    /// store) and how many they are; without, the tab's own view. X-670 M1 (review of 0.1638, R07): the lines kept are gone through off the
    /// game's thread (ChatStore.Searched); here, each frame, only the new ones. While a text is being searched, the last text's results as
    /// they were (or the tab itself) and « Searching… ». X-670 M3: in capture mode, the senders by their stand-ins, as shown.</summary>
    private ChatTabView DrawChatSearchLine(ChatTabSettings tab,ChatTabView view,ChatSettings options)
    {
        ImGui.SetNextItemWidth(Math.Min(ImGui.GetContentRegionAvail().X*0.6f,260*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale));
        if(chatSearchFocus){ImGui.SetKeyboardFocusHere();chatSearchFocus=false;}
        ImGui.InputTextWithHint("###ChatSearchText",localization.Get("ChatWindow.SearchHint"),ref chatSearchText,ChatSearch.MaximumText);
        Tip("ChatWindow.SearchBoxTip");
        var query=ChatSearch.Query(chatSearchText);
        if(query.Length==0)return view;
        var capture=options.CaptureNames;
        var cap=options.BoundedMaxLines;
        var table=ChatFilterTableNow();
        bool Holds(ChatRecord record)=>ChatSearch.Matches(record,query,capture);
        var (found,ready)=ChatMessages.Searched(tab,(capture ? "capture\u001f":"\u001f")+query,
            (fresh,stale)=>fresh.Refresh(ChatMessages,tab,cap,table,options,record=>stale() ? throw new OperationCanceledException():Holds(record)));
        if(ready && found is not null)found.Refresh(ChatMessages,tab,cap,table,options,Holds);
        ImGui.SameLine();
        ImGui.TextDisabled(ready && found is not null ? localization.Format("ChatWindow.SearchFound",found.Count):localization.Get("ChatWindow.Searching"));
        return found ?? view;
    }

    /// <summary>X-448 (0.1454): at the end of the tab bar, always visible (also without the frame): the frame on or off, the lock on or off.</summary>
    private void DrawChatFrameButtons(ChatSettings options)
    {
        // X-476 (0.1481): « + » adds a tab (as the settings' button), the gear opens the chat's own settings: the same ones as on the Settings
        // page, drawn by the same method from the same configuration (a change on one side shows on the other).
        var tabs=ChatTabs(options);
        if(tabs.Count<ChatSettings.MaximumTabs)
        {
            if(GlyphTabButton("###ChatAddTab",Dalamud.Interface.FontAwesomeIcon.Plus,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip)){AddChatTab(tabs);TrySave("Chat window tabs");}
            Tip("ChatWindow.AddTabTip");
        }
        // X-447 B8 (0.1638): the search in the tab shown, opened or closed (closed, its text is forgotten).
        if(GlyphTabButton("###ChatSearch",Dalamud.Interface.FontAwesomeIcon.Search,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip))
        {
            chatSearchOpen=!chatSearchOpen;chatSearchFocus=chatSearchOpen;
            // X-670 M1: closed, its text forgotten and its view dropped (no copy of the messages left).
            if(!chatSearchOpen){chatSearchText="";ChatMessages.EndSearch();}
        }
        Tip(chatSearchOpen ? "ChatWindow.SearchCloseTip":"ChatWindow.SearchTip");
        var settingsButton=GlyphTabButton("###ChatSettingsButton",Dalamud.Interface.FontAwesomeIcon.Cog,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip("ChatWindow.SettingsTip");
        if(settingsButton)ImGui.OpenPopup("###TenshoChatSettings");
        DrawChatSettingsPopup();
        // X-477 (0.1476): the icons are drawn centred in tab-high buttons (GlyphTabButton), no longer as the label of a button in the icon font.
        var frame=GlyphTabButton("###ChatFrame",options.Borderless ? Dalamud.Interface.FontAwesomeIcon.BorderAll:Dalamud.Interface.FontAwesomeIcon.BorderNone,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip(options.Borderless ? "ChatWindow.FrameOnTip":"ChatWindow.FrameOffTip");
        var lockButton=GlyphTabButton("###ChatLock",options.Locked ? Dalamud.Interface.FontAwesomeIcon.Lock:Dalamud.Interface.FontAwesomeIcon.LockOpen,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip(options.Locked ? "ChatWindow.UnlockTip":"ChatWindow.LockTip");
        // X-447 D4 (0.1618): the tabs' names hidden or shown again.
        var tabsButton=GlyphTabButton("###ChatTabsShown",options.HideTabs ? Dalamud.Interface.FontAwesomeIcon.Eye:Dalamud.Interface.FontAwesomeIcon.EyeSlash,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip(options.HideTabs ? "ChatWindow.ShowTabsTip":"ChatWindow.HideTabsTip");
        if(tabsButton){options.HideTabs=!options.HideTabs;TrySave("Chat window");}
        // X-467 (0.1504, D-181): with the game's messages hidden by the option, both chats on or off; out of it, this window reduced or not.
        var both=false;
        if(options.HideGameMessages)
        {
            both=GlyphTabButton("###ChatBoth",options.BothChats ? Dalamud.Interface.FontAwesomeIcon.WindowRestore:Dalamud.Interface.FontAwesomeIcon.Clone,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
            Tip(options.BothChats ? "ChatWindow.BothChatsOffTip":"ChatWindow.BothChatsOnTip");
        }
        var reduce=false;
        if(!chatFollowing)
        {
            reduce=GlyphTabButton("###ChatReduce",chatReduced ? Dalamud.Interface.FontAwesomeIcon.WindowMaximize:Dalamud.Interface.FontAwesomeIcon.WindowMinimize,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
            Tip(chatReduced ? "ChatWindow.ExpandTip":"ChatWindow.ReduceTip");
        }
        if(both){options.BothChats=!options.BothChats;if(!options.BothChats)chatReduced=false;TrySave("Chat window");}
        if(reduce)chatReduced=!chatReduced;
        if(frame){options.Borderless=!options.Borderless;TrySave("Chat window");}
        if(lockButton){options.Locked=!options.Locked;TrySave("Chat window");}
    }

    // X-447 D7 (0.1618): the place and size typed (X, Y, width, height, from the screen's corner), read from the window when null.
    private int[]? chatPlaceTyped;

    /// <summary>X-447 D7 (0.1618): the window's place and size typed to the pixel, or brought back to the screen's centre; kept inside the
    /// screen (ChatLook.Inside); a window partly out of the screen is said.</summary>
    private void DrawChatPlaceSettings()
    {
        var viewport=ImGui.GetMainViewport();
        var at=(chatAt ?? viewport.Pos)-viewport.Pos;
        var size=chatSize.X>0 ? chatSize:new Vector2(560,320);
        chatPlaceTyped??=[(int)at.X,(int)at.Y,(int)size.X,(int)size.Y];
        ImGui.TextUnformatted(localization.Get("ChatWindow.Place"));
        string[] keys=["ChatWindow.PlaceX","ChatWindow.PlaceY","ChatWindow.PlaceWidth","ChatWindow.PlaceHeight"];
        for(var index=0;index<keys.Length;index++)
        {
            if(index>0)ImGui.SameLine();
            ImGui.SetNextItemWidth(90*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
            var value=chatPlaceTyped[index];
            if(ImGui.InputInt(localization.Get(keys[index])+"###ChatPlace"+index,ref value,0))chatPlaceTyped[index]=value;
            Tip(keys[index]+"Tip");
        }
        if(ImGui.Button(localization.Get("ChatWindow.PlaceApply")))
        {
            var place=ChatLook.Inside(chatPlaceTyped[0],chatPlaceTyped[1],chatPlaceTyped[2],chatPlaceTyped[3],viewport.Size.X,viewport.Size.Y);
            chatPlaceAsked=new PanelPlace(new Vector2(place.X,place.Y),new Vector2(place.Width,place.Height));chatPlaceTyped=null;
        }
        Tip("ChatWindow.PlaceApplyTip");
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("ChatWindow.PlaceCenter")))
        {
            var place=ChatLook.Centered(size.X,size.Y,viewport.Size.X,viewport.Size.Y);
            chatPlaceAsked=new PanelPlace(new Vector2(place.X,place.Y),new Vector2(place.Width,place.Height));chatPlaceTyped=null;
        }
        Tip("ChatWindow.PlaceCenterTip");
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("ChatWindow.PlaceRead")))chatPlaceTyped=null;
        Tip("ChatWindow.PlaceReadTip");
        if(chatAt is not null && ChatLook.Outside(at.X,at.Y,size.X,size.Y,viewport.Size.X,viewport.Size.Y))ImGui.TextColored(Warning,localization.Get("ChatWindow.PlaceOutside"));
        // X-667 M3 (0.1619): placed from outside (over the game's messages, or reduced), a place asked now is not kept.
        if(chatFollowing||chatReduced)ImGui.TextColored(Warning,localization.Get("ChatWindow.PlaceFollowing"));
    }

    /// <summary>X-476 (0.1481): the chat's own settings, in a panel opened by the gear at the end of the tabs.</summary>
    private void DrawChatSettingsPopup()
    {
        if(!ImGui.BeginPopup("###TenshoChatSettings"))return;
        try
        {
            MenuHeader(localization.Get("ChatWindow.SettingsTitle"));
            if(ImGui.BeginChild("###TenshoChatSettingsScroll",new Vector2(520,420)))DrawChatWindowSettings();
            ImGui.EndChild();
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>X-478: pushes the colour of a tab's name when it has one (readable on the inactive and the active tab); true when pushed.
    /// X-495 (0.1478): also on the hovered tab and on the tabs of the window without the focus.</summary>
    private static bool PushChatTabColor(ChatTabSettings tab)
    {
        if(tab.NameColor is not { } chosen)return false;
        static Vector4 Style(ImGuiCol colour)=>ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(colour));
        ImGui.PushStyleColor(ImGuiCol.Text,ChatTabColors.Readable(ChatColor(chosen),Style(ImGuiCol.Text),Style(ImGuiCol.Tab),Style(ImGuiCol.TabActive),
            Style(ImGuiCol.TabHovered),Style(ImGuiCol.TabUnfocused),Style(ImGuiCol.TabUnfocusedActive)));
        return true;
    }

    // X-457 (0.1462): the height of a row in the lines' area (font size chosen in the settings), kept from the frame before: the lines' height is
    // declared before the area opens.
    private float chatLineHeight;

    /// <param name="linesHeight">X-476 (0.1481): the lines' height; 0, all that is left; below 0, all but that much (the tab bar under them).</param>
    /// <param name="tab">X-501: the number that names the tab's lines' area (its own scroll); X-504 M2: its view's own (ChatTabView.Serial).</param>
    private void DrawChatLines(ChatTabView view,ChatSettings options,float linesHeight,int tab)
    {
        // X-457 (0.1462): the scroll bar always kept (one width for the layout, whatever the height), and the lines' height declared from the
        // tab's rows, not measured from what the frame before drew (Chat/ChatScroll.cs).
        // X-447 B2 (0.1622): the tab's own text size, else the window's; the spacing added between the lines.
        var scale=chatShownTab?.ScaleIn(options) ?? options.BoundedFontScale;
        var rowHeight=chatLineHeight>0 ? chatLineHeight:ImGui.GetTextLineHeightWithSpacing()*scale/100f+options.BoundedLineSpacing;
        chatHoveredRecord=null;
        ImGui.SetNextWindowContentSize(new Vector2(0,ChatScroll.Content(view.RowCount,rowHeight)));
        // X-447 A4 (0.1467): the mouse wheel is read here (whole lines), not by ImGui.
        // X-505 (0.1488): a thin, discreet scroll bar, as the game's (its size and colours are taken when the area begins).
        var thin=options.ThinScrollbar;
        if(thin)
        {
            var text=ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Text));
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize,ChatLook.ScrollbarThin);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg,Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab,text with{W=ChatLook.ScrollbarGrab.Idle});
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered,text with{W=ChatLook.ScrollbarGrab.Hovered});
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive,text with{W=ChatLook.ScrollbarGrab.Held});
        }
        bool visible;
        // X-505 (0.1490, the user's capture of 11/16 09:58: background still opaque, with the opacity at 0 %): the lines' area has no background
        // of its own (the theme gives children its opaque panel colour, drawn over the window's): only the window's, with its opacity, shows.
        ImGui.PushStyleColor(ImGuiCol.ChildBg,Vector4.Zero);
        try{visible=ImGui.BeginChild(ChatFormat.LinesId(tab),new Vector2(0,linesHeight),false,ImGuiWindowFlags.NoNav|ImGuiWindowFlags.AlwaysVerticalScrollbar|ImGuiWindowFlags.NoScrollWithMouse);}
        finally{ImGui.PopStyleColor();if(thin){ImGui.PopStyleColor(4);ImGui.PopStyleVar();}}
        try
        {
            if(!visible)return;
            if(view.Count==0){EmptyState("ChatWindow.Empty");view.MarkSeen();return;}
            ImGui.SetWindowFontScale(scale/100f);
            var width=ImGui.GetContentRegionAvail().X;
            // X-482 M1 (0.1472): laid out again once the lists naming the links are loaded. X-447 B2 (0.1622): and with the tab's own size.
            var key=ChatFormat.LayoutKey(options,ChatFormat.NamesKey(itemCatalog.State.Catalog is not null,quests.State.ById is not null,maps.State.Catalog is not null))+"|"+scale;
            var relaid=!view.LaidOutFor(width,key);
            if(relaid)view.Relayout(width,key);
            var height=ImGui.GetTextLineHeightWithSpacing()+options.BoundedLineSpacing;
            // X-558 (0.1532): laid out within a time budget, the newest first, only the newest ahead (ChatLoading); scrolled back, a packet of
            // older lines above the view, the scroll moved by the rows they add so the view does not jump.
            var layoutStarted=System.Diagnostics.Stopwatch.GetTimestamp();
            var laid=0;
            while(ChatLoading.More(laid,System.Diagnostics.Stopwatch.GetElapsedTime(layoutStarted).TotalMilliseconds))
            {
                var next=view.NextToLayOut(ChatLoading.Eager);
                if(next<0)break;
                LayOutChatLine(view,next,options);laid++;
            }
            // X-563 M1: how far the scroll moves at the next frame because of the rows added above (0 when none).
            var shift=0f;
            if(!relaid && !ChatScroll.AtBottom(ImGui.GetScrollY(),view.RowsShown,height,view.HeightShown))
            {
                var first=view.IndexAtRow((int)(ImGui.GetScrollY()/height));
                var firstRow=view.RowOf(first);
                while(ChatLoading.More(laid,System.Diagnostics.Stopwatch.GetElapsedTime(layoutStarted).TotalMilliseconds))
                {
                    var older=view.OlderToLayOut(first,ChatLoading.Packet);
                    if(older<0)break;
                    LayOutChatLine(view,older,options);laid++;
                }
                if(view.RowOf(first)-firstRow is var added and not 0){ImGui.SetScrollY(ChatLoading.Kept(ImGui.GetScrollY(),added,height));shift=ChatLoading.Shift(ImGui.GetScrollY(),added,height);}
            }
            if(developerMode)SlowFrames.Add(ImGui.GetFrameCount(),"Chat layout",System.Diagnostics.Stopwatch.GetElapsedTime(layoutStarted).TotalMilliseconds);
            chatLineHeight=height;
            // X-457 (0.1462): the top of the content, as SetCursorPosY counts it (GetCursorStartPos takes the scroll off, and SetCursorPosY
            // takes it off again: every line was drawn the scroll too high, the cause of the jumps and of the empty window).
            var top=ImGui.GetCursorPosY();
            var scroll=ImGui.GetScrollY();
            var shown=ImGui.GetWindowHeight();
            // X-563 M1 (review of 0.1534): the scroll the lines are drawn at: ImGui's still has the frame before's (SetScrollY takes effect at
            // the next frame), so in the frame a packet was laid out above, the lines are drawn as the next frame will draw them (no jump of a
            // frame); the « back to the bottom » button, the mouse and « at the bottom » keep ImGui's scroll.
            var drawn=scroll+shift;
            top-=shift;
            // At the bottom on the frame before (its rows): follow the new lines, and nothing is unread in this tab any more.
            var atBottom=ChatScroll.AtBottom(scroll,view.RowsShown,height,view.HeightShown);
            var end=drawn+shown;
            // X-482 M2 (0.1472): the « back to the bottom » button is placed before the lines: the links and lines under it do not answer the mouse.
            var button=atBottom ? null:ChatBottomButton(view,scroll,shown);
            var mouse=ImGui.GetMousePos()-ImGui.GetWindowPos()+new Vector2(0,scroll);
            chatLinesBlocked=button is { } placed && ImGui.IsWindowHovered() && ChatScroll.OnButton(mouse.X,mouse.Y,placed.Corner,placed.Size.X,placed.Size.Y);
            for(var index=view.IndexAtRow((int)(drawn/height));index<view.Count;index++)
            {
                if(view.LinesAt(index) is null)LayOutChatLine(view,index,options);
                var row=view.RowOf(index);
                if(row*height>end)break;
                DrawChatRecord(view.RecordAt(index),view.LinesAt(index)!,top+row*height,height,options,view.RepeatsAt(index),view.AlertAt(index,chatAlertWords));
            }
            // X-457 (0.1462): the last line at the bottom of the window, from the rows laid out in this frame (the next frame declares them).
            view.RowsShown=view.RowCount;view.HeightShown=shown;
            if(atBottom){ImGui.SetScrollY(ChatScroll.Bottom(view.RowCount,height,shown));view.MarkSeen();}
            else if(button is { } bottom)DrawChatBottomButton(view,bottom,shown,height);
            // X-447 B9 (0.1642): a pinned message clicked: the tab scrolled to it (to its top when it has left the tab), after the bottom's.
            if(chatScrollTo>=0){var pinned=view.IndexOfId(chatScrollTo);chatScrollTo=-1;ImGui.SetScrollY(pinned<0 ? 0:view.RowOf(pinned)*height);}
            chatLinesBlocked=false;
            // X-447 A4 (0.1467): the mouse wheel moves whole lines, as the game's chat. X-482 M3 (0.1472): a fine wheel adds up to whole notches.
            // X-492 M1: the rest of a fine wheel belongs to one tab: another tab starts from nothing.
            if(!ReferenceEquals(view,chatWheelView)){chatWheelView=view;chatWheelRest=0;}
            if(ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel is var wheel and not 0)
                ImGui.SetScrollY(ChatScroll.Wheel(drawn,wheel,height,ImGui.GetScrollMaxY()+shift,ref chatWheelRest));
            // X-457 (0.1460): developer mode: a window that jumps says how in the log (layouts, rows, scroll limit, « at the bottom »).
            if(developerMode && chatScrollWatch.Frame(new(ImGui.GetTime(),width,view.RowCount,scroll,ImGui.GetScrollMaxY(),atBottom,relaid)) is { } jumps)
                log.Information(jumps);
            DrawChatCopyKeys(view,options);
            // X-403 phase 2 (0.1441): the place menu of a map link, opened by a right click on it (the same entries as Tensho's other place menus).
            if(chatWindowPlace is { } place)
            {
                if(ImGui.BeginPopup("###TenshoChatWindowPlace"))
                {
                    try
                    {
                        MenuHeader(localization.Get("ChatLinks.MenuTitle"));
                        // X-455 E4 (0.1617): the place with its coordinates, copied.
                        if(ImGui.Selectable(localization.Get("ChatWindow.CopyPlace")))ImGui.SetClipboardText(place.Place);
                        PlaceEntries(place.Map,place.Coordinate,null,place.Place);
                    }
                    finally{ImGui.EndPopup();}
                }
                else chatWindowPlace=null;
            }
            // X-403 phase 2 (0.1453): the menu of a web address, opened by a click on it: the address whole, then open it or copy it.
            if(chatWindowWeb is { } address)
            {
                if(ImGui.BeginPopup("###TenshoChatWeb"))
                {
                    try
                    {
                        // X-451 (0.1456): the site it leads to, read once per address opened; a host in other alphabets is said with how it reads.
                        if(chatWindowWebSite?.Url!=address)chatWindowWebSite=ChatWeb.Site(address);
                        ImGui.TextColored(Warning,localization.Get("ChatWindow.WebWarning"));
                        if(chatWindowWebSite is { } site)
                        {
                            ImGui.TextUnformatted(localization.Format("ChatWindow.WebSite",site.Host));
                            if(site.Foreign)ImGui.TextColored(Error,localization.Format("ChatWindow.WebForeign",site.Reads ?? site.Host));
                        }
                        ImGui.TextUnformatted(address);
                        ImGui.Separator();
                        if(ImGui.Selectable(localization.Get("ChatWindow.OpenWeb")))Dalamud.Utility.Util.OpenLink(address);
                        if(ImGui.Selectable(localization.Get("ChatWindow.CopyWeb")))ImGui.SetClipboardText(address);
                    }
                    finally{ImGui.EndPopup();}
                }
                else chatWindowWeb=null;
            }
            // X-403 phase 2 (0.1442): the menu of a message, opened by a right click on its text: copy it, or every message of this tab.
            if(chatWindowLine is { } said)
            {
                if(ImGui.BeginPopup("###TenshoChatLine"))
                {
                    try
                    {
                        if(ImGui.Selectable(localization.Get("ChatWindow.CopyLine")))ImGui.SetClipboardText(ChatFormat.Plain(said,options,ChatChannelShort,ChatLinkName));
                        if(ImGui.Selectable(localization.Get("ChatWindow.CopyTab")))
                            ImGui.SetClipboardText(ChatFormat.Plain(Enumerable.Range(0,view.Count).Select(view.RecordAt),options,ChatChannelShort,ChatLinkName));
                        if(chatShownTab is { } shownTab && ImGui.Selectable(localization.Get("ChatWindow.ClearTab")))ClearChatTab(shownTab,true);
                        // X-447 B9 (0.1642): pinned at the top of this tab (in memory only), or unpinned.
                        if(chatShownTab is { } pinTab)
                        {
                            if(ChatMessages.Pinned(pinTab,said)){if(ImGui.Selectable(localization.Get("ChatWindow.Unpin")))ChatMessages.Unpin(pinTab,said);}
                            else
                            {
                                ImGui.BeginDisabled(ChatMessages.Pins(pinTab).Count>=ChatStore.MaximumPins);
                                try{if(ImGui.Selectable(localization.Get("ChatWindow.Pin")))ChatMessages.Pin(pinTab,said);}
                                finally{ImGui.EndDisabled();}
                                Tip("ChatWindow.PinTip");
                            }
                        }
                        // X-455 E1 (0.1624): the tab as text to select with the mouse (ImGui draws no selectable text: a read-only box).
                        if(ImGui.Selectable(localization.Get("ChatWindow.SelectText")))
                            chatSelectText=ChatFormat.Plain(Enumerable.Range(0,view.Count).Select(view.RecordAt),options,ChatChannelShort,ChatLinkName);
                        DrawChatSenderMenu(options,said);
                    }
                    finally{ImGui.EndPopup();}
                }
                else chatWindowLine=null;
            }
            DrawChatSelectText();
            // X-455 E4 (0.1617): the menu of a quest link, opened by a right click on it: its name copied.
            if(chatWindowQuest is { } questName)
            {
                if(ImGui.BeginPopup("###TenshoChatQuest"))
                {
                    try{MenuHeader(questName);if(ImGui.Selectable(localization.Get("ChatWindow.CopyQuest")))ImGui.SetClipboardText(questName);}
                    finally{ImGui.EndPopup();}
                }
                else chatWindowQuest=null;
            }
        }
        finally{ImGui.EndChild();}
    }

    /// <summary>
    /// X-455 E2 (0.1617): with the chat window in front, Ctrl+A chooses the whole tab (a band over it says so; Esc or a click lets it go) and
    /// Ctrl+C copies it, or else the message under the mouse (ChatCopy.Chosen).
    /// </summary>
    private void DrawChatCopyKeys(ChatTabView view,ChatSettings options)
    {
        if(chatWholeTab==view.Serial && (ImGui.IsKeyPressed(ImGuiKey.Escape,false) || ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)))chatWholeTab=-1;
        if(ImGui.GetIO().KeyCtrl && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            if(ImGui.IsKeyPressed(ImGuiKey.A,false))chatWholeTab=view.Serial;
            if(ImGui.IsKeyPressed(ImGuiKey.C,false) && ChatCopy.Chosen(chatWholeTab==view.Serial,chatHoveredRecord,()=>Enumerable.Range(0,view.Count).Select(view.RecordAt)) is { Count:>0 } chosen)
                ImGui.SetClipboardText(ChatFormat.Plain(chosen,options,ChatChannelShort,ChatLinkName));
        }
        if(chatWholeTab!=view.Serial)return;
        var at=ImGui.GetWindowPos()+new Vector2(0,ImGui.GetScrollY());
        var draw=ImGui.GetWindowDrawList();
        draw.AddRectFilled(at,at+ImGui.GetWindowSize(),ImGui.GetColorU32(Selected with{W=0.18f}));
        draw.AddText(at+new Vector2(6,4),ImGui.GetColorU32(Accent),localization.Get("ChatWindow.WholeTabChosen"));
    }

    /// <summary>X-455 E7 (0.1617): a tab cleared (its messages until now hidden in it) or shown again; saved with the chat's settings.</summary>
    // X-447 B9 (0.1642): a tab cleared lets go of its pins too.
    private void ClearChatTab(ChatTabSettings tab,bool clear){tab.ClearedAt=clear ? DateTimeOffset.Now:null;if(clear)ChatMessages.Unpin(tab);ChatTabChanged(tab);}

    /// <summary>X-447 A4 (0.1467): while reading higher up, a button at the bottom right of the lines brings the window back down; it says how
    /// many messages arrived meanwhile. X-482 (0.1472): placed before the lines are drawn (M2); « 1 new » in the singular (M4).</summary>
    private (string Label,(float X,float Y) Corner,Vector2 Size)? ChatBottomButton(ChatTabView view,float scroll,float shown)
    {
        if(!ChatScroll.ShowsBottomButton(scroll,ImGui.GetScrollMaxY()))return null;
        var label=view.Unread switch{1=>localization.Get("ChatWindow.NewBelowOne"),>1=>localization.Format("ChatWindow.NewBelow",view.Unread),_=>localization.Get("ChatWindow.ToBottom")};
        var size=ImGui.CalcTextSize(label)+ImGui.GetStyle().FramePadding*2;
        return (label,ChatScroll.ButtonCorner(ImGui.GetWindowContentRegionMax().X,scroll,shown,size.X,size.Y),size);
    }

    private void DrawChatBottomButton(ChatTabView view,(string Label,(float X,float Y) Corner,Vector2 Size) button,float shown,float height)
    {
        ImGui.SetCursorPos(new Vector2(button.Corner.X,button.Corner.Y));
        if(ImGui.Button(button.Label+"###ChatToBottom"))ImGui.SetScrollY(ChatScroll.Bottom(view.RowCount,height,shown));
        ChatZone();
        Tip("ChatWindow.ToBottomTip");
    }

    // X-482 (0.1472): M2, the mouse is on the « back to the bottom » button (the lines under it do not answer it); M3, the wheel's part of a notch.
    private bool chatLinesBlocked;
    private float chatWheelRest;
    private object? chatWheelView;

    // X-556 M3 (review of 0.1520): a folded message is laid out narrower by its « ×n », so the count never runs past the window's edge
    // (ChatTabView lays it out again when the count grows).
    private void LayOutChatLine(ChatTabView view,int index,ChatSettings options)
        =>view.SetLines(index,ChatFormat.Wrap(ChatFormat.WithJobIcon(ChatFormat.Runs(view.RecordAt(index),options,ChatChannelShort,ChatLinkName,ChatLinkIcon),ChatJobIcon(view.RecordAt(index),options)),
            ChatLook.WidthBeside(view.Width,view.RepeatsAt(index)>0 ? ImGui.CalcTextSize(localization.Format("ChatWindow.Repeats",view.RepeatsAt(index)+1)).X:0),
            text=>ImGui.CalcTextSize(text).X,ChatLook.Indent(ImGui.GetFontSize(),options.WrapIndent),ChatLook.IconWidth(ImGui.GetTextLineHeight())));

    /// <summary>X-690 (0.1666): the job icon of a message's sender, the job the game showed when it arrived (0: not known, or the setting off).</summary>
    private uint ChatJobIcon(ChatRecord record,ChatSettings options)=>ChatSenders is { } senders ? Chat.ChatJobs.IconFor(options,record.Channel,senders.For(record.Id)?.Job ?? 0):0;

    /// <summary>X-690 (0.1666): the colour of a message's sender by role (red, green and blue), null when not known or the setting off.</summary>
    private (float R,float G,float B)? ChatRoleColour(ChatRecord record,ChatSettings options)
        =>ChatSenders is { } senders && senders.Jobs.ColourFor(options,record.Channel,senders.For(record.Id)?.Job ?? 0) is { } argb ? Chat.ChatJobs.Rgb(argb):null;

    /// <summary>X-447 B14 (0.1641): the icon of an item link's item, from Tensho's item list (0: not loaded, or not an item).</summary>
    private uint ChatLinkIcon(ChatLink link)=>link.Kind==ChatSegmentKind.ItemLink ? itemCatalog.State.Catalog?.Find(link.ItemId)?.IconId ?? 0:0;

    /// <summary>X-461 (0.1463): the name of what a link without text of its own points to: an item (Tensho's item list), a quest, a place; else
    /// a word for a link (never empty: the link keeps its place in the message).</summary>
    private string ChatLinkName(ChatLink link)=>link.Kind switch
    {
        ChatSegmentKind.ItemLink=>itemCatalog.State.Catalog?.Find(link.ItemId)?.Name,
        ChatSegmentKind.QuestLink=>quests.State.ById?.GetValueOrDefault(link.QuestId)?.Name,
        ChatSegmentKind.MapLink=>link.Place.Length>0 ? link.Place:maps.State.Catalog?.Find(link.MapId)?.Zone,
        _=>null,
    } is { Length:>0 } name ? name:localization.Get("ChatWindow.LinkNoName");

    private void DrawChatRecord(ChatRecord record,IReadOnlyList<IReadOnlyList<ChatRun>> lines,float y,float height,ChatSettings options,int repeats=0,bool alert=false)
    {
        var color=ChatColor(options.ColorOf(record.Channel));
        // X-505 (0.1488): the lines after the first indented, as laid out (LayOutChatLine).
        var left=ImGui.GetCursorPosX();
        // X-484 (0.1519, the user's request of 17/11: the player put forward more visible): a band in the warning colour behind the whole message.
        // X-484 (0.1520): a message holding an alert word on a band in the accent colour (a player put forward keeps the warning colour).
        var put=ChatTextFilter.IsPlayer(record.Sender,chatHighlighted);
        if(put || alert)
        {
            ImGui.SetCursorPosY(y);
            var top=ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(top-new Vector2(2,0),top+new Vector2(ImGui.GetContentRegionAvail().X+2,Math.Max(1,lines.Count)*height),
                ImGui.GetColorU32((put ? Warning:Accent) with {W=ChatLook.HighlightBandAlpha}),3f);
        }
        var indent=ChatLook.Indent(ImGui.GetFontSize(),options.WrapIndent);
        for(var line=0;line<lines.Count;line++)
        {
            ImGui.SetCursorPosY(y+line*height);
            if(line>0 && indent>0)ImGui.SetCursorPosX(left+indent);
            var first=true;
            var index=0;
            foreach(var run in lines[line])
            {
                if(!first)ImGui.SameLine(0,0);
                first=false;
                // X-690 (0.1666): the sender's job icon, drawn as an item's icon.
                if(run.Role==ChatRole.JobIcon)
                {
                    var side=ImGui.GetTextLineHeight();
                    DrawGameIcon(ChatJobIcon(record,options),new Vector2(side,side));
                    ImGui.SameLine(0,0);
                    ImGui.Dummy(new Vector2(ChatLook.IconWidth(side)-side,side));
                }
                // X-447 B14 (0.1641): an item's icon, a square as high as the line, then its gap.
                else if(run.Role==ChatRole.ItemIcon && run.Link is { } item)
                {
                    var side=ImGui.GetTextLineHeight();
                    DrawGameIcon(ChatLinkIcon(item),new Vector2(side,side));
                    ImGui.SameLine(0,0);
                    ImGui.Dummy(new Vector2(ChatLook.IconWidth(side)-side,side));
                }
                else if(run.Link is { } link && ChatLinkActive(link))DrawChatLink(run,link,$"{record.Id}_{line}_{index}",record);
                else
                {
                    if(options.TextOutline)ChatOutline(run.Text);
                    ImGui.TextColored(ChatRunColour(run,record,color,options),run.Text);
                    // X-473 (0.1627): a player's name: their world, and their job and level when the game shows them (never in capture mode).
                    if(!chatLinesBlocked && run.Role==ChatRole.Sender && !options.CaptureNames && ChatChannels.Spoken(record.Channel))TipText(()=>ChatSenderTip(record));
                    if(!chatLinesBlocked && run.Link is { Kind:ChatSegmentKind.DalamudLink })TipText(localization.Get("ChatWindow.DalamudLinkGameTip"));
                    // 0.1442: a right click on a message's text: its menu (copy). X-482 M2: not under the « back to the bottom » button.
                    if(!chatLinesBlocked && ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowLine=record;ImGui.OpenPopup("###TenshoChatLine");}
                }
                // X-455 E2 (0.1617): the message under the mouse, for Ctrl+C.
                if(!chatLinesBlocked && ImGui.IsItemHovered())chatHoveredRecord=record;
                index++;
            }
            if(first)ImGui.TextUnformatted(" ");
            // X-484 (0.1519): the repeats folded into this message, after its last line.
            if(line==lines.Count-1 && repeats>0)
            {
                ImGui.SameLine(0,0);
                ImGui.TextColored(Muted,localization.Format("ChatWindow.Repeats",repeats+1));
                TipText(()=>ChatRepeatsTip(repeats+1));
            }
        }
    }

    private string ChatRepeatsTip(int times)=>localization.Format("ChatWindow.RepeatsTip",times);

    /// <summary>X-473 (0.1627): a player's tooltip: name and world (the message's identifiers, else the name as written), job and level when the
    /// game shows them (ChatPlayerActions.Seen: the party or nearby).</summary>
    private string ChatSenderTip(ChatRecord record)
    {
        var source=ChatSenders?.For(record.Id);
        var name=source?.Name is { Length:>0 } known ? known:ChatCopy.NameOf(ChatFormat.Clean(record.Sender));
        var world=source is { World:>0 } withWorld && ChatPlayers is { } players ? players.World(withWorld.World):ChatCopy.WorldOf(ChatFormat.Clean(record.Sender));
        var seen=source is not null ? ChatPlayers?.Seen(source):null;
        return ChatCopy.SenderTip(name,world,seen?.Job,seen?.Level ?? 0,(who,where)=>localization.Format("ChatWindow.SenderTipWorld",who,where),
            (job,level)=>localization.Format("ChatWindow.SenderTipJob",job,level));
    }

    // X-455 E1 (0.1624): the tab's text made when « Select text » was chosen (null: closed); its box opened at the next frame.
    private string? chatSelectText;
    private bool chatSelectOpen;

    /// <summary>X-455 E1 (0.1624): the tab's messages in a read-only text box, the size of the window: a piece of one or several messages selected
    /// with the mouse (or Ctrl+A) and copied with Ctrl+C or the button. Made once when opened (never each frame); Échap or a click outside closes it.
    /// X-675 (0.1651): the box has a fixed size taken from the chat window (ChatCopy.SelectBox); the popup, which always fits its content, fits
    /// around it. A box of the space left (-1,-1) shrank with the popup frame after frame until it was gone (the user's report of 20/11).</summary>
    private void DrawChatSelectText()
    {
        if(chatSelectText is null)return;
        if(!chatSelectOpen){ImGui.OpenPopup("###TenshoChatSelect");chatSelectOpen=true;}
        var padding=ImGui.GetStyle().WindowPadding;
        var (boxWidth,boxHeight)=ChatCopy.SelectBox((chatSize.X,chatSize.Y),ImGui.GetTextLineHeightWithSpacing()+ImGui.GetFrameHeightWithSpacing(),padding.X,padding.Y);
        if(!ImGui.BeginPopup("###TenshoChatSelect")){chatSelectText=null;chatSelectOpen=false;return;}
        try
        {
            ImGui.TextColored(Muted,localization.Get("ChatWindow.SelectTextHelp"));
            var all=chatSelectText;
            CopyButton(localization.Get("ChatWindow.SelectTextCopyAll"),()=>all);
            Tip("ChatWindow.SelectTextCopyAllTip");
            var text=chatSelectText;
            ImGui.InputTextMultiline("###ChatSelectText",ref text,System.Text.Encoding.UTF8.GetByteCount(text)+1,new Vector2(boxWidth,boxHeight),ImGuiInputTextFlags.ReadOnly);
            Tip("ChatWindow.SelectTextTip");
        }
        finally{ImGui.EndPopup();}
    }

    /// <summary>A run's colour: the time muted, a player put forward in the warning colour, X-447 B5 (0.1621) a player's name in its own colour
    /// when chosen (spoken channels), else the channel's colour.</summary>
    private Vector4 ChatRunColour(ChatRun run,ChatRecord record,Vector4 color,ChatSettings options)
    {
        if(run.Role==ChatRole.Timestamp)return Muted;
        if(run.Role!=ChatRole.Sender)return color;
        // X-690 (0.1666): the role's colour first (a player put forward keeps the warning colour).
        if(ChatRoleColour(record,options) is var (red,green,blue))return ChatTextFilter.IsPlayer(record.Sender,chatHighlighted) ? Warning:new Vector4(red,green,blue,color.W);
        if(!options.NameColors || !ChatChannels.Spoken(record.Channel))return ChatTextFilter.IsPlayer(record.Sender,chatHighlighted) ? Warning:color;
        var (r,g,b)=ChatLook.SenderColour(record.Sender,options.CaptureNames);
        return ChatTextFilter.IsPlayer(record.Sender,chatHighlighted) ? Warning:new Vector4(r,g,b,color.W);
    }

    /// <summary>X-476 (0.1481, X-447 A3): the dark outline of the text about to be drawn at the cursor, as the game's chat.</summary>
    private static void ChatOutline(string text)
    {
        var at=ImGui.GetCursorScreenPos();
        var draw=ImGui.GetWindowDrawList();
        var font=ImGui.GetFont();var size=ImGui.GetFontSize();
        // X-502 (review of 0.1482): faded with the window, as the text itself (TextColored takes the style's alpha, a draw list does not).
        var colour=ChatLook.Outline(ImGui.GetStyle().Alpha);
        foreach(var (x,y) in ChatLook.OutlineShifts)draw.AddText(font,size,at+new Vector2(x,y),colour,text);
    }

    /// <summary>X-474 (0.1477): whether Dalamud keeps a handler for a link of Dalamud or of a plugin (set by the plugin; null outside the game).</summary>
    internal Func<string,uint,bool>? DalamudLinkKnown{get;set;}
    /// <summary>X-474 (0.1477): runs that handler as a click in the game's chat does (plugin, command, the link's own string, the link's text);
    /// false when there is none.</summary>
    internal Func<string,uint,byte[]?,string,bool>? OpenDalamudLink{get;set;}
    /// <summary>X-496 M1 (0.1478): the plugin whose handler failed on the last click (set on the game's thread), said on that plugin's links' tooltip.</summary>
    internal volatile string? DalamudLinkFailed;

    /// <summary>X-474 (0.1477): a link drawn as a link: all of them, except a link of Dalamud or of a plugin that Dalamud has no handler for
    /// (a plugin unloaded since): plain text, its tooltip says to open it from the game's chat.</summary>
    private bool ChatLinkActive(ChatLink link)=>link.Kind!=ChatSegmentKind.DalamudLink || DalamudLinkKnown?.Invoke(link.Plugin,link.CommandId)==true;

    // X-403 phase 2 (0.1441): the place menu a right click on a map link opened (drawn by DrawChatLines).
    private (Maps.MapDefinition Map,Vector2 Coordinate,string Place)? chatWindowPlace;
    // 0.1442: the message whose menu a right click opened.
    private ChatRecord? chatWindowLine;
    // X-455 (0.1617): the message under the mouse this frame (E2), the tab chosen whole by Ctrl+A (its view's number; -1: none), the tab shown
    // (E7, its messages' menu) and the quest of a quest link's menu (E4).
    private ChatRecord? chatHoveredRecord;
    private int chatWholeTab=-1;
    private ChatTabSettings? chatShownTab;
    private bool chatFirstLinesLogged;
    private string? chatWindowQuest;
    // 0.1460 (X-457): the scrolling of the tab shown, frame by frame, in developer mode.
    private readonly ChatScrollWatch chatScrollWatch=new();
    // 0.1453: the web address whose menu a click opened.
    private string? chatWindowWeb;
    // 0.1456 (X-451): its site, read when the menu shows a new address.
    private WebSite? chatWindowWebSite;

    /// <summary>X-636 (bench X-631, review of 0.1441, M3): a link acts on the left button's release over it, pressed on it, without a drag
    /// (the chat window moves by its whole background: pressing a link to move it placed the flag or opened the item). Pure rule:
    /// <see cref="TenshoAssistant.Chat.ChatLinkPress.Acts"/>.</summary>
    private bool ChatLinkClicked(string id)
    {
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left)){chatLinkPressed=id;chatLinkPressedAt=ImGui.GetMousePos();}
        if(!ImGui.IsMouseReleased(ImGuiMouseButton.Left))return false;
        var acts=TenshoAssistant.Chat.ChatLinkPress.Acts(chatLinkPressed,id,ImGui.IsItemHovered(),TenshoAssistant.Chat.ChatLinkPress.Dragged(chatLinkPressedAt,ImGui.GetMousePos()));
        if(chatLinkPressed==id)chatLinkPressed=null;
        return acts;
    }

    /// <summary>
    /// X-403 phase 2 (0.1441): a link of a message, underlined in the accent colour. Map link: left click places the flag and opens the map (as the
    /// Maps page's « open »), right click opens Tensho's place menu. Item link: left click opens the item in Tensho (its first page), right click
    /// opens Tensho's item menu. 0.1442: quest link: left click opens the quest on the Quests page. A target Tensho does not know (data not
    /// loaded, an item it does not list) is only said on hover. 0.1453: web address: the whole address on hover; a click opens its menu (open in
    /// the browser, copy), never the browser at once. 0.1456 (X-451): the tooltip and the menu say the site it leads to (its host as the browser
    /// reaches it) and warn when that name holds letters of other alphabets; the tooltip is made only under the mouse (X-453 M3).
    /// X-670 M2 (review of 0.1638): this summary stood above ChatLinkClicked, which has its own.
    /// </summary>
    private void DrawChatLink(ChatRun run,ChatLink link,string id,ChatRecord record)
    {
        if(ChatOptions.TextOutline)ChatOutline(run.Text);
        // X-505 (0.1488): with the game's look, the game's orange arrow then the text in the message's colour, not underlined; else underlined
        // in the accent colour, as before. X-510 (review of 0.1490): with the game's look, a link without the game's arrow (a web address, a link
        // named by Tensho, a link of Dalamud without arrow) is underlined in the accent colour too (ChatRun.Underlined): told apart from the text.
        var underline=!ChatOptions.GameLinks || run.Underlined;
        var colour=underline ? Accent:run.Role==ChatRole.LinkMark ? new Vector4(ChatLook.LinkMark.R,ChatLook.LinkMark.G,ChatLook.LinkMark.B,ChatLook.LinkMark.A)
            :ChatColor(ChatOptions.ColorOf(record.Channel));
        ImGui.TextColored(colour,run.Text);
        ChatZone();
        if(underline)
        {
            var min=ImGui.GetItemRectMin();
            var max=ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.X,max.Y-1),new Vector2(max.X,max.Y-1),ImGui.GetColorU32(Accent));
        }
        // X-482 M2 (0.1472): under the « back to the bottom » button the link neither shows its tooltip nor opens (its menu already open stays).
        if(chatLinesBlocked){if(link.Kind==ChatSegmentKind.ItemLink && ImGui.IsPopupOpen("##ChatItem"+id))ItemLinksMenu("##ChatItem"+id,itemCatalog.State.Catalog?.Find(link.ItemId));return;}
        // X-474 (0.1477): a link of Dalamud or of a plugin: a click does what it does in the game's chat (its plugin's handler).
        if(link.Kind==ChatSegmentKind.DalamudLink)
        {
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            TipText(()=>DalamudLinkFailed==link.Plugin ? localization.Format("ChatWindow.DalamudLinkFailedTip",link.Plugin):localization.Format("ChatWindow.DalamudLinkTip",link.Plugin));
            if(ChatLinkClicked(id))
            {
                DalamudLinkFailed=null;
                if(OpenDalamudLink?.Invoke(link.Plugin,link.CommandId,link.Data,run.Text)!=true)
                    log.Information("Chat window: no handler any more for a link of {0} (command {1}).",link.Plugin,link.CommandId);
            }
            // X-496 M2 (0.1478): a right click opens the message's menu (copy), as on its text (before 0.1477 the link was plain text).
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowLine=record;ImGui.OpenPopup("###TenshoChatLine");}
            return;
        }
        if(link.Kind==ChatSegmentKind.WebLink)
        {
            if(ImGui.IsItemHovered()){ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);TipText(WebLinkTip(link.Url));}
            if(ChatLinkClicked(id) || ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowWeb=link.Url;ImGui.OpenPopup("###TenshoChatWeb");}
            return;
        }
        if(link.Kind==ChatSegmentKind.MapLink)
        {
            if(maps.State.Catalog?.Find(link.MapId) is not { } map || !Maps.MapCatalog.CoordinatesValid(link.X,link.Y)){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
            var coordinate=new Vector2(link.X,link.Y);
            // X-637 (bench X-631, review of 0.1441, M4): the place and its tooltip written only under the mouse or on a click, not at each frame.
            string Place()=>$"{(link.Place.Length>0 ? link.Place:map.Zone)}  ({Maps.MapProjection.CoordinateText(coordinate)})";
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            // X-684 (0.1672): the one place tooltip, the click's effect under it.
            if(ImGui.IsItemHovered())DrawPlaceTooltip(map,coordinate,link.Place.Length>0 ? link.Place:null,localization.Get("ChatWindow.MapLinkTip"));
            if(ChatLinkClicked(id))OpenMapPointFromPage(map,link.X,link.Y);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowPlace=(map,coordinate,Place());ImGui.OpenPopup("###TenshoChatWindowPlace");}
            return;
        }
        if(link.Kind==ChatSegmentKind.QuestLink)
        {
            // 0.1442: a quest link: left click opens it on Tensho's Quests page.
            if(quests.State.ById?.GetValueOrDefault(link.QuestId) is not { } quest){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            // X-684 (0.1670): the one quest tooltip, the click's effect under it.
            if(ImGui.IsItemHovered())DrawQuestTooltip(quest,localization.Get("ChatWindow.QuestLinkTip"));
            if(ChatLinkClicked(id)){SelectQuest(quest.Id);page=Page.Quests;IsOpen=true;}
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowQuest=quest.Name;ImGui.OpenPopup("###TenshoChatQuest");}
            return;
        }
        if(itemCatalog.State.Catalog?.Find(link.ItemId) is not { } item){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
        if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        // X-638 (bench X-631, review of 0.1441, M5): a high-quality item link says so, as the game's link does.
        // X-473 (0.1627): as the game's tooltip: the item's icon, its name, its description (Item sheet), then what a click does.
        if(ImGui.IsItemHovered())
            using(TooltipScope.Begin())
            {
                // X-683 (0.1664, the user's request of 20/11 around 21:50, picture « Agrégat d'eau »): the name on top, the clicks' line at the
                // bottom; between them the game's description and the body of Tensho's common item tooltip, called as that tooltip calls it
                // (DrawItemInfoBody, the same code and caches: note, collection, stock, NPC price, usages, how to get it). The comparison with
                // the piece worn stays in the common tooltip only (X-294: one path for the gear part); the parts' settings are the panel's
                // under the game's tooltip only (HoverPanelParts).
                var (named,clicks)=ChatFormat.TipLines(localization.Format(link.Hq ? "ChatWindow.ItemLinkTipHq":"ChatWindow.ItemLinkTip",item.Name));
                DrawGameIcon(item.IconId,new Vector2(32,32)*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                ImGui.SameLine();
                ImGui.TextColored(Accent,named);
                ImGui.PushTextWrapPos(ImGui.GetFontSize()*28);
                try
                {
                    if(ChatPlayers?.Description(item.Id) is { Length:>0 } description)
                    {
                        ImGui.Separator();
                        ImGui.TextColored(Muted,description);
                    }
                    ImGui.Separator();
                    DrawItemInfoBody(item,item.Id);
                    if(clicks.Length>0){ImGui.Separator();ImGui.TextDisabled(clicks);}
                }
                finally{ImGui.PopTextWrapPos();}
            }
        if(ChatLinkClicked(id))
        {
            SelectItem(item.Id);
            if(RelevantRoutes(item.Id) is { Count:>0 } routes)OpenRoute(routes[0],item.Id);
            IsOpen=true;
        }
        ItemLinksMenu("##ChatItem"+id,item,copyName:true);
    }

    /// <summary>X-451 (0.1456): the tooltip of a web address: the address, the site it leads to, a warning when the site's name holds letters
    /// of other alphabets, then what a click does. Made only while the mouse is on it.</summary>
    private string WebLinkTip(string url)
    {
        var site=ChatWeb.Site(url);
        var tip=localization.Format("ChatWindow.WebLinkTip",url,site?.Host ?? url);
        return site is { Foreign:true } ? tip+"\n"+localization.Format("ChatWindow.WebForeign",site.Reads ?? site.Host):tip;
    }
}
