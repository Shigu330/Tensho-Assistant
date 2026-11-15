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
        Opacity:()=>ChatOptions.BoundedOpacity/100f,Size:new Vector2(560,320));

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
        SliderSetting("ChatWindow.Opacity","ChatWindow.OpacityTip",()=>configuration.Chat.BoundedOpacity,value=>configuration.Chat.Opacity=value,10,100,defaults.Opacity,"%d %%");
        SliderSetting("ChatWindow.FontScale","ChatWindow.FontScaleTip",()=>configuration.Chat.BoundedFontScale,value=>configuration.Chat.FontScale=value,60,200,defaults.FontScale,"%d %%");
        IntSetting("ChatWindow.MaxLines","ChatWindow.MaxLinesTip",()=>configuration.Chat.BoundedMaxLines,value=>configuration.Chat.MaxLines=value,ChatStore.MinimumLines,ChatStore.MaximumLines,defaults.MaxLines);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionTabs")+"###ChatTabsSection"))DrawChatTabSettings(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatWindow.SectionColors")+"###ChatColorsSection"))DrawChatColorSettings(options);
        if(developerMode)DrawChatKinds();
        if(chatSettingsPending && !ImGui.IsAnyItemActive()){chatSettingsPending=false;TrySave("Chat window tabs and colours");}
    }

    private void DrawChatTabSettings(ChatSettings options)
    {
        var tabs=options.EnsureTabs(localization.Get("ChatWindow.TabAll"),localization.Get("ChatWindow.TabSocial"));
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
        var tabs=options.EnsureTabs(localization.Get("ChatWindow.TabAll"),localization.Get("ChatWindow.TabSocial"));
        if(!ImGui.BeginTabBar("###TenshoChatTabs",ImGuiTabBarFlags.FittingPolicyScroll))return;
        try
        {
            for(var index=0;index<tabs.Count;index++)
            {
                var tab=tabs[index];
                var view=ChatMessages.ViewOf(tab);
                view.Refresh(ChatMessages,tab,options.BoundedMaxLines);
                if(!ImGui.BeginTabItem(ChatFormat.TabLabel(tab.Name,tab.ShowUnread,view.Unread)+"###ChatTab"+index))continue;
                try{DrawChatLines(view,options);}
                finally{ImGui.EndTabItem();}
            }
        }
        finally{ImGui.EndTabBar();}
    }

    private void DrawChatLines(ChatTabView view,ChatSettings options)
    {
        var visible=ImGui.BeginChild("###TenshoChatLines",Vector2.Zero,false,ImGuiWindowFlags.NoNav);
        try
        {
            if(!visible)return;
            if(view.Count==0){EmptyState("ChatWindow.Empty");view.MarkSeen();return;}
            ImGui.SetWindowFontScale(options.BoundedFontScale/100f);
            var width=ImGui.GetContentRegionAvail().X;
            var key=ChatFormat.LayoutKey(options);
            if(!view.LaidOutFor(width,key))view.Relayout(width,key);
            for(var budget=ChatLayoutsPerFrame;budget>0;budget--)
            {
                var next=view.NextToLayOut();
                if(next<0)break;
                LayOutChatLine(view,next,options);
            }
            var height=ImGui.GetTextLineHeightWithSpacing();
            var top=ImGui.GetCursorStartPos().Y;
            var scroll=ImGui.GetScrollY();
            // At the bottom on the frame before: follow the new lines, and nothing is unread in this tab any more.
            var atBottom=scroll>=ImGui.GetScrollMaxY()-1;
            var end=scroll+ImGui.GetWindowHeight();
            for(var index=view.IndexAtRow((int)(scroll/height));index<view.Count;index++)
            {
                if(view.LinesAt(index) is null)LayOutChatLine(view,index,options);
                var row=view.RowOf(index);
                if(row*height>end)break;
                DrawChatRecord(view.RecordAt(index),view.LinesAt(index)!,top+row*height,height,options);
            }
            ImGui.SetCursorPosY(top+view.RowCount*height);
            ImGui.Dummy(new Vector2(1,1));
            if(atBottom){ImGui.SetScrollHereY(1);view.MarkSeen();}
        }
        finally{ImGui.EndChild();}
    }

    private void LayOutChatLine(ChatTabView view,int index,ChatSettings options)
        =>view.SetLines(index,ChatFormat.Wrap(ChatFormat.Runs(view.RecordAt(index),options,ChatChannelShort),view.Width,text=>ImGui.CalcTextSize(text).X));

    private void DrawChatRecord(ChatRecord record,IReadOnlyList<IReadOnlyList<ChatRun>> lines,float y,float height,ChatSettings options)
    {
        var color=ChatColor(options.ColorOf(record.Channel));
        for(var line=0;line<lines.Count;line++)
        {
            ImGui.SetCursorPosY(y+line*height);
            var first=true;
            foreach(var run in lines[line])
            {
                if(!first)ImGui.SameLine(0,0);
                first=false;
                ImGui.TextColored(run.Role==ChatRole.Timestamp ? Muted:color,run.Text);
            }
            if(first)ImGui.TextUnformatted(" ");
        }
    }
}
