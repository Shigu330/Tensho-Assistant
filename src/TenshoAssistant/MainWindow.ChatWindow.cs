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
    private ChatSettings ChatOptions=>configuration.Chat;
    // Saved once no control is active any more (tab names, channels, colours: several edits per frame while typing or picking).
    private bool chatSettingsPending;
    private const int ChatLayoutsPerFrame=100;

    private PanelDefinition ChatPanelDefinition=>new("Chat","###TenshoChat",new Vector2(40,420),
        ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar,
        ()=>ChatOptions.WindowOpen && !inDesktopApp,ChatWindowVisible,DrawChatWindowContents,CloseChatWindow,()=>localization.Get("ChatWindow.Title"),
        Opacity:()=>ChatOptions.BoundedOpacity/100f,Size:new Vector2(560,320),MoreFlags:ChatFrameFlags,NoBorder:()=>ChatOptions.Borderless);

    /// <summary>X-448 (0.1454): what the two settings add to the window's flags: borderless, no title bar; locked, neither moved nor resized.</summary>
    private ImGuiWindowFlags ChatFrameFlags()=>(ChatOptions.Borderless ? ImGuiWindowFlags.NoTitleBar:ImGuiWindowFlags.None)
        |(ChatOptions.Locked ? ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize:ImGuiWindowFlags.None);

    private bool ChatWindowVisible()
        =>!windowDisposed && !TenshoAssistant.Competitive.CompetitiveGate.Cut && !gameGui.GameUiHidden && !(ChatOptions.HideInCombat && InCombatNow?.Invoke()==true);

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
    private string ChatChannelShort(ChatChannel channel)=>localization.Get("ChatChannelsShort."+channel);
    private static Vector4 ChatColor(uint rgb){var (red,green,blue)=ChatChannels.Unpack(rgb);return new(red,green,blue,1);}

    private void DrawChatWindowSettings()
    {
        var options=ChatOptions;var defaults=DefaultConfiguration.Chat;
        BoolSetting("ChatWindow.Show","ChatWindow.ShowTip",()=>configuration.Chat.WindowOpen,value=>{configuration.Chat.WindowOpen=value;if(!value)ChatMessages.Clear();},c=>c.Chat.WindowOpen);
        BoolSetting("ChatWindow.HideInCombat","ChatWindow.HideInCombatTip",()=>configuration.Chat.HideInCombat,value=>configuration.Chat.HideInCombat=value,c=>c.Chat.HideInCombat);
        BoolSetting("ChatWindow.Timestamps","ChatWindow.TimestampsTip",()=>configuration.Chat.ShowTimestamps,value=>configuration.Chat.ShowTimestamps=value,c=>c.Chat.ShowTimestamps);
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.Chat.ShowTimestamps);
        try
        {
            BoolSetting("ChatWindow.Clock24","ChatWindow.Clock24Tip",()=>configuration.Chat.Clock24,value=>configuration.Chat.Clock24=value,c=>c.Chat.Clock24);
            BoolSetting("ChatWindow.Seconds","ChatWindow.SecondsTip",()=>configuration.Chat.ShowSeconds,value=>configuration.Chat.ShowSeconds=value,c=>c.Chat.ShowSeconds);
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        BoolSetting("ChatWindow.Channel","ChatWindow.ChannelTip",()=>configuration.Chat.ShowChannel,value=>configuration.Chat.ShowChannel=value,c=>c.Chat.ShowChannel);
        SliderSetting("ChatWindow.Opacity","ChatWindow.OpacityTip",()=>configuration.Chat.BoundedOpacity,value=>configuration.Chat.Opacity=value,0,100,defaults.Opacity,"%d %%");
        // X-448 (0.1454): the frame and the lock, also on the buttons at the end of the window's tab bar.
        BoolSetting("ChatWindow.Borderless","ChatWindow.BorderlessTip",()=>configuration.Chat.Borderless,value=>configuration.Chat.Borderless=value,c=>c.Chat.Borderless);
        BoolSetting("ChatWindow.Locked","ChatWindow.LockedTip",()=>configuration.Chat.Locked,value=>configuration.Chat.Locked=value,c=>c.Chat.Locked);
        SliderSetting("ChatWindow.FontScale","ChatWindow.FontScaleTip",()=>configuration.Chat.BoundedFontScale,value=>configuration.Chat.FontScale=value,60,200,defaults.FontScale,"%d %%");
        IntSetting("ChatWindow.MaxLines","ChatWindow.MaxLinesTip",()=>configuration.Chat.BoundedMaxLines,value=>configuration.Chat.MaxLines=value,ChatStore.MinimumLines,ChatStore.MaximumLines,defaults.MaxLines);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionTabs")+"###ChatTabsSection"))DrawChatTabSettings(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionColors")+"###ChatColorsSection"))DrawChatColorSettings(options);
        if(developerMode)DrawChatKinds();
        if(chatSettingsPending && !ImGui.IsAnyItemActive()){chatSettingsPending=false;TrySave("Chat window tabs and colours");}
    }

    /// <summary>X-445 (0.1458): the tabs, the game's three default ones (names of the game's Addon sheet) and Social, made or completed once.</summary>
    private List<ChatTabSettings> ChatTabs(ChatSettings options)=>options.EnsureTabs(localization.Get("ChatWindow.TabAll"),localization.Get("ChatWindow.TabBattle"),
        localization.Get("ChatWindow.TabEvent"),localization.Get("ChatWindow.TabSocial"));

    private void DrawChatTabSettings(ChatSettings options)
    {
        var tabs=ChatTabs(options);
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
                ImGui.SameLine();
                var unread=tab.ShowUnread;
                if(ImGui.Checkbox(localization.Get("ChatWindow.TabUnread")+"###ChatTabUnread",ref unread)){tab.ShowUnread=unread;chatSettingsPending=true;}
                Tip("ChatWindow.TabUnreadTip");
                // X-478 (the user's request of 15/11): the colour of the tab's name; « ↺ » brings back the theme's text colour.
                ImGui.SameLine();
                var named=tab.NameColor is { } chosen ? ChatColor(chosen):ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Text));
                var nameRgb=new Vector3(named.X,named.Y,named.Z);
                if(ImGui.ColorEdit3(localization.Get("ChatWindow.TabColor")+"###ChatTabColor",ref nameRgb,ImGuiColorEditFlags.NoInputs))
                {
                    tab.NameColor=ChatChannels.Pack(nameRgb.X,nameRgb.Y,nameRgb.Z);
                    chatSettingsPending=true;
                }
                Tip("ChatWindow.TabColorTip");
                if(DefaultButton("ChatTabColor",tab.NameColor is not null)){tab.NameColor=null;chatSettingsPending=true;}
                if(tabs.Count>1)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("ChatWindow.TabRemove"))){ChatMessages.Restart(tab);tabs.RemoveAt(index);chatSettingsPending=true;return;}
                    Tip("ChatWindow.TabRemoveTip");
                }
                if(ImGui.TreeNode(localization.Format("ChatWindow.TabChannels",tab.Channels?.Count ?? 0)+"###ChatTabChannels"))
                {
                    try{DrawChatTabChannels(tab);}
                    finally{ImGui.TreePop();}
                }
            }
            finally{ImGui.PopID();}
            ImGui.Separator();
        }
        if(tabs.Count<ChatSettings.MaximumTabs)
        {
            if(ImGui.SmallButton(localization.Get("ChatWindow.TabAdd")))
            {
                tabs.Add(new(){Name=localization.Format("ChatWindow.TabNew",tabs.Count+1),Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()});
                chatSettingsPending=true;
            }
            Tip("ChatWindow.TabAddTip");
        }
    }

    private void DrawChatTabChannels(ChatTabSettings tab)
    {
        tab.Channels??=[];
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable("###ChatTabChannelTable",3))return;
        try
        {
            foreach(var channel in ChatChannels.All)
            {
                ImGui.TableNextColumn();
                var shown=tab.Shows(channel);
                if(ImGui.Checkbox(ChatChannelName(channel)+"###ChatChannel"+channel,ref shown))
                {
                    if(shown)tab.Channels.Add(channel);else tab.Channels.Remove(channel);
                    ChatMessages.Restart(tab);chatSettingsPending=true;
                }
                Tip("ChatWindow.ChannelBoxTip");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawChatColorSettings(ChatSettings options)
    {
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
            }
            finally{ImGui.PopID();}
        }
    }

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
        var options=ChatOptions;
        var tabs=ChatTabs(options);
        if(!ImGui.BeginTabBar("###TenshoChatTabs",ImGuiTabBarFlags.FittingPolicyScroll))return;
        try
        {
            for(var index=0;index<tabs.Count;index++)
            {
                var tab=tabs[index];
                var view=ChatMessages.ViewOf(tab);
                view.Refresh(ChatMessages,tab,options.BoundedMaxLines);
                bool open;
                var coloured=PushChatTabColor(tab);
                try{open=ImGui.BeginTabItem(ChatFormat.TabLabel(tab.Name,tab.ShowUnread,view.Unread)+"###ChatTab"+index);}
                finally{if(coloured)ImGui.PopStyleColor();}
                if(!open)continue;
                try{DrawChatLines(view,options);}
                finally{ImGui.EndTabItem();}
            }
            DrawChatFrameButtons(options);
        }
        finally{ImGui.EndTabBar();}
    }

    /// <summary>X-448 (0.1454): at the end of the tab bar, always visible (also without the frame): the frame on or off, the lock on or off.</summary>
    private void DrawChatFrameButtons(ChatSettings options)
    {
        // X-477 (0.1476): the icons are drawn centred in tab-high buttons (GlyphTabButton), no longer as the label of a button in the icon font.
        var frame=GlyphTabButton("###ChatFrame",options.Borderless ? Dalamud.Interface.FontAwesomeIcon.BorderAll:Dalamud.Interface.FontAwesomeIcon.BorderNone,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip(options.Borderless ? "ChatWindow.FrameOnTip":"ChatWindow.FrameOffTip");
        var lockButton=GlyphTabButton("###ChatLock",options.Locked ? Dalamud.Interface.FontAwesomeIcon.Lock:Dalamud.Interface.FontAwesomeIcon.LockOpen,ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip);
        Tip(options.Locked ? "ChatWindow.UnlockTip":"ChatWindow.LockTip");
        if(frame){options.Borderless=!options.Borderless;TrySave("Chat window");}
        if(lockButton){options.Locked=!options.Locked;TrySave("Chat window");}
    }

    /// <summary>X-478: pushes the colour of a tab's name when it has one (readable on the inactive and the active tab); true when pushed.</summary>
    private static bool PushChatTabColor(ChatTabSettings tab)
    {
        if(tab.NameColor is not { } chosen)return false;
        static Vector4 Style(ImGuiCol colour)=>ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(colour));
        ImGui.PushStyleColor(ImGuiCol.Text,ChatTabColors.Readable(ChatColor(chosen),Style(ImGuiCol.Text),Style(ImGuiCol.Tab),Style(ImGuiCol.TabActive)));
        return true;
    }

    // X-457 (0.1462): the height of a row in the lines' area (font size chosen in the settings), kept from the frame before: the lines' height is
    // declared before the area opens.
    private float chatLineHeight;

    private void DrawChatLines(ChatTabView view,ChatSettings options)
    {
        // X-457 (0.1462): the scroll bar always kept (one width for the layout, whatever the height), and the lines' height declared from the
        // tab's rows, not measured from what the frame before drew (Chat/ChatScroll.cs).
        var rowHeight=chatLineHeight>0 ? chatLineHeight:ImGui.GetTextLineHeightWithSpacing()*options.BoundedFontScale/100f;
        ImGui.SetNextWindowContentSize(new Vector2(0,ChatScroll.Content(view.RowCount,rowHeight)));
        // X-447 A4 (0.1467): the mouse wheel is read here (whole lines), not by ImGui.
        var visible=ImGui.BeginChild("###TenshoChatLines",Vector2.Zero,false,ImGuiWindowFlags.NoNav|ImGuiWindowFlags.AlwaysVerticalScrollbar|ImGuiWindowFlags.NoScrollWithMouse);
        try
        {
            if(!visible)return;
            if(view.Count==0){EmptyState("ChatWindow.Empty");view.MarkSeen();return;}
            ImGui.SetWindowFontScale(options.BoundedFontScale/100f);
            var width=ImGui.GetContentRegionAvail().X;
            // X-482 M1 (0.1472): laid out again once the lists naming the links are loaded.
            var key=ChatFormat.LayoutKey(options,ChatFormat.NamesKey(itemCatalog.State.Catalog is not null,quests.State.ById is not null,maps.State.Catalog is not null));
            var relaid=!view.LaidOutFor(width,key);
            if(relaid)view.Relayout(width,key);
            for(var budget=ChatLayoutsPerFrame;budget>0;budget--)
            {
                var next=view.NextToLayOut();
                if(next<0)break;
                LayOutChatLine(view,next,options);
            }
            var height=ImGui.GetTextLineHeightWithSpacing();
            chatLineHeight=height;
            // X-457 (0.1462): the top of the content, as SetCursorPosY counts it (GetCursorStartPos takes the scroll off, and SetCursorPosY
            // takes it off again: every line was drawn the scroll too high, the cause of the jumps and of the empty window).
            var top=ImGui.GetCursorPosY();
            var scroll=ImGui.GetScrollY();
            var shown=ImGui.GetWindowHeight();
            // At the bottom on the frame before (its rows): follow the new lines, and nothing is unread in this tab any more.
            var atBottom=ChatScroll.AtBottom(scroll,view.RowsShown,height,view.HeightShown);
            var end=scroll+shown;
            // X-482 M2 (0.1472): the « back to the bottom » button is placed before the lines: the links and lines under it do not answer the mouse.
            var button=atBottom ? null:ChatBottomButton(view,scroll,shown);
            var mouse=ImGui.GetMousePos()-ImGui.GetWindowPos()+new Vector2(0,scroll);
            chatLinesBlocked=button is { } placed && ImGui.IsWindowHovered() && ChatScroll.OnButton(mouse.X,mouse.Y,placed.Corner,placed.Size.X,placed.Size.Y);
            for(var index=view.IndexAtRow((int)(scroll/height));index<view.Count;index++)
            {
                if(view.LinesAt(index) is null)LayOutChatLine(view,index,options);
                var row=view.RowOf(index);
                if(row*height>end)break;
                DrawChatRecord(view.RecordAt(index),view.LinesAt(index)!,top+row*height,height,options);
            }
            // X-457 (0.1462): the last line at the bottom of the window, from the rows laid out in this frame (the next frame declares them).
            view.RowsShown=view.RowCount;view.HeightShown=shown;
            if(atBottom){ImGui.SetScrollY(ChatScroll.Bottom(view.RowCount,height,shown));view.MarkSeen();}
            else if(button is { } bottom)DrawChatBottomButton(view,bottom,shown,height);
            chatLinesBlocked=false;
            // X-447 A4 (0.1467): the mouse wheel moves whole lines, as the game's chat. X-482 M3 (0.1472): a fine wheel adds up to whole notches.
            if(ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel is var wheel and not 0)
                ImGui.SetScrollY(ChatScroll.Wheel(scroll,wheel,height,ImGui.GetScrollMaxY(),ref chatWheelRest));
            // X-457 (0.1460): developer mode: a window that jumps says how in the log (layouts, rows, scroll limit, « at the bottom »).
            if(developerMode && chatScrollWatch.Frame(new(ImGui.GetTime(),width,view.RowCount,scroll,ImGui.GetScrollMaxY(),atBottom,relaid)) is { } jumps)
                log.Information(jumps);
            // X-403 phase 2 (0.1441): the place menu of a map link, opened by a right click on it (the same entries as Tensho's other place menus).
            if(chatWindowPlace is { } place)
            {
                if(ImGui.BeginPopup("###TenshoChatWindowPlace"))
                {
                    try{MenuHeader(localization.Get("ChatLinks.MenuTitle"));PlaceEntries(place.Map,place.Coordinate,null,place.Place);}
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
                    }
                    finally{ImGui.EndPopup();}
                }
                else chatWindowLine=null;
            }
        }
        finally{ImGui.EndChild();}
    }

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
        Tip("ChatWindow.ToBottomTip");
    }

    // X-482 (0.1472): M2, the mouse is on the « back to the bottom » button (the lines under it do not answer it); M3, the wheel's part of a notch.
    private bool chatLinesBlocked;
    private float chatWheelRest;

    private void LayOutChatLine(ChatTabView view,int index,ChatSettings options)
        =>view.SetLines(index,ChatFormat.Wrap(ChatFormat.Runs(view.RecordAt(index),options,ChatChannelShort,ChatLinkName),view.Width,text=>ImGui.CalcTextSize(text).X));

    /// <summary>X-461 (0.1463): the name of what a link without text of its own points to: an item (Tensho's item list), a quest, a place; else
    /// a word for a link (never empty: the link keeps its place in the message).</summary>
    private string ChatLinkName(ChatLink link)=>link.Kind switch
    {
        ChatSegmentKind.ItemLink=>itemCatalog.State.Catalog?.Find(link.ItemId)?.Name,
        ChatSegmentKind.QuestLink=>quests.State.ById?.GetValueOrDefault(link.QuestId)?.Name,
        ChatSegmentKind.MapLink=>link.Place.Length>0 ? link.Place:maps.State.Catalog?.Find(link.MapId)?.Zone,
        _=>null,
    } is { Length:>0 } name ? name:localization.Get("ChatWindow.LinkNoName");

    private void DrawChatRecord(ChatRecord record,IReadOnlyList<IReadOnlyList<ChatRun>> lines,float y,float height,ChatSettings options)
    {
        var color=ChatColor(options.ColorOf(record.Channel));
        for(var line=0;line<lines.Count;line++)
        {
            ImGui.SetCursorPosY(y+line*height);
            var first=true;
            var index=0;
            foreach(var run in lines[line])
            {
                if(!first)ImGui.SameLine(0,0);
                first=false;
                if(run.Link is { } link && ChatLinkActive(link))DrawChatLink(run,link,$"{record.Id}_{line}_{index}");
                else
                {
                    ImGui.TextColored(run.Role==ChatRole.Timestamp ? Muted:color,run.Text);
                    if(!chatLinesBlocked && run.Link is { Kind:ChatSegmentKind.DalamudLink })TipText(localization.Get("ChatWindow.DalamudLinkGameTip"));
                    // 0.1442: a right click on a message's text: its menu (copy). X-482 M2: not under the « back to the bottom » button.
                    if(!chatLinesBlocked && ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowLine=record;ImGui.OpenPopup("###TenshoChatLine");}
                }
                index++;
            }
            if(first)ImGui.TextUnformatted(" ");
        }
    }

    /// <summary>X-474 (0.1477): whether Dalamud keeps a handler for a link of Dalamud or of a plugin (set by the plugin; null outside the game).</summary>
    internal Func<string,uint,bool>? DalamudLinkKnown{get;set;}
    /// <summary>X-474 (0.1477): runs that handler as a click in the game's chat does (plugin, command, the link's text); false when there is none.</summary>
    internal Func<string,uint,string,bool>? OpenDalamudLink{get;set;}

    /// <summary>X-474 (0.1477): a link drawn as a link: all of them, except a link of Dalamud or of a plugin that Dalamud has no handler for
    /// (a plugin unloaded since): plain text, its tooltip says to open it from the game's chat.</summary>
    private bool ChatLinkActive(ChatLink link)=>link.Kind!=ChatSegmentKind.DalamudLink || DalamudLinkKnown?.Invoke(link.Plugin,link.CommandId)==true;

    // X-403 phase 2 (0.1441): the place menu a right click on a map link opened (drawn by DrawChatLines).
    private (Maps.MapDefinition Map,Vector2 Coordinate,string Place)? chatWindowPlace;
    // 0.1442: the message whose menu a right click opened.
    private ChatRecord? chatWindowLine;
    // 0.1460 (X-457): the scrolling of the tab shown, frame by frame, in developer mode.
    private readonly ChatScrollWatch chatScrollWatch=new();
    // 0.1453: the web address whose menu a click opened.
    private string? chatWindowWeb;
    // 0.1456 (X-451): its site, read when the menu shows a new address.
    private WebSite? chatWindowWebSite;

    /// <summary>
    /// X-403 phase 2 (0.1441): a link of a message, underlined in the accent colour. Map link: left click places the flag and opens the map (as the
    /// Maps page's « open »), right click opens Tensho's place menu. Item link: left click opens the item in Tensho (its first page), right click
    /// opens Tensho's item menu. 0.1442: quest link: left click opens the quest on the Quests page. A target Tensho does not know (data not
    /// loaded, an item it does not list) is only said on hover. 0.1453: web address: the whole address on hover; a click opens its menu (open in
    /// the browser, copy), never the browser at once. 0.1456 (X-451): the tooltip and the menu say the site it leads to (its host as the browser
    /// reaches it) and warn when that name holds letters of other alphabets; the tooltip is made only under the mouse (X-453 M3).
    /// </summary>
    private void DrawChatLink(ChatRun run,ChatLink link,string id)
    {
        ImGui.TextColored(Accent,run.Text);
        var min=ImGui.GetItemRectMin();
        var max=ImGui.GetItemRectMax();
        ImGui.GetWindowDrawList().AddLine(new Vector2(min.X,max.Y-1),new Vector2(max.X,max.Y-1),ImGui.GetColorU32(Accent));
        // X-482 M2 (0.1472): under the « back to the bottom » button the link neither shows its tooltip nor opens (its menu already open stays).
        if(chatLinesBlocked){if(link.Kind==ChatSegmentKind.ItemLink && ImGui.IsPopupOpen("##ChatItem"+id))ItemLinksMenu("##ChatItem"+id,itemCatalog.State.Catalog?.Find(link.ItemId));return;}
        // X-474 (0.1477): a link of Dalamud or of a plugin: a click does what it does in the game's chat (its plugin's handler).
        if(link.Kind==ChatSegmentKind.DalamudLink)
        {
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            TipText(localization.Format("ChatWindow.DalamudLinkTip",link.Plugin));
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left) && OpenDalamudLink?.Invoke(link.Plugin,link.CommandId,run.Text)!=true)
                log.Information("Chat window: no handler any more for a link of {0} (command {1}).",link.Plugin,link.CommandId);
            return;
        }
        if(link.Kind==ChatSegmentKind.WebLink)
        {
            if(ImGui.IsItemHovered()){ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);TipText(WebLinkTip(link.Url));}
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowWeb=link.Url;ImGui.OpenPopup("###TenshoChatWeb");}
            return;
        }
        if(link.Kind==ChatSegmentKind.MapLink)
        {
            if(maps.State.Catalog?.Find(link.MapId) is not { } map || !Maps.MapCatalog.CoordinatesValid(link.X,link.Y)){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
            var coordinate=new Vector2(link.X,link.Y);
            var place=$"{(link.Place.Length>0 ? link.Place:map.Zone)}  ({Maps.MapProjection.CoordinateText(coordinate)})";
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            TipText(localization.Format("ChatWindow.MapLinkTip",place));
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left))OpenMapPointFromPage(map,link.X,link.Y);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowPlace=(map,coordinate,place);ImGui.OpenPopup("###TenshoChatWindowPlace");}
            return;
        }
        if(link.Kind==ChatSegmentKind.QuestLink)
        {
            // 0.1442: a quest link: left click opens it on Tensho's Quests page.
            if(quests.State.ById?.GetValueOrDefault(link.QuestId) is not { } quest){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
            if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            TipText(localization.Format("ChatWindow.QuestLinkTip",quest.Name));
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left)){SelectQuest(quest.Id);page=Page.Quests;IsOpen=true;}
            return;
        }
        if(itemCatalog.State.Catalog?.Find(link.ItemId) is not { } item){TipText(localization.Get("ChatWindow.LinkUnknownTip"));return;}
        if(ImGui.IsItemHovered())ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        TipText(localization.Format("ChatWindow.ItemLinkTip",item.Name));
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            SelectItem(item.Id);
            if(RelevantRoutes(item.Id) is { Count:>0 } routes)OpenRoute(routes[0],item.Id);
            IsOpen=true;
        }
        ItemLinksMenu("##ChatItem"+id,item);
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
