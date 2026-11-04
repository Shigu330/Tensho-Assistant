using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Chat;

namespace TenshoAssistant;

// CHAT-001 (DEC-029), phase 1: a chat window that only reads: tabs by channel, colours, timestamps, unread counts. The game's chat stays as it is, and nothing is sent. Links and rules come in the
// next phases. The window draws only the lines in view; the lines are laid out once per message and width.
internal sealed partial class MainWindow
{
    internal ChatStore ChatMessages{get;}=new();
    internal ChatSettings ChatOptions=>configuration.Chat??=new();
    // C3: Chat 2 is told about, never blocked.
    internal Func<bool>? ChatTwoLoaded{get;set;}
    private sealed class ChatTabView
    {
        public readonly List<ChatRecord> Records=[];
        public readonly List<IReadOnlyList<IReadOnlyList<ChatRun>>?> Lines=[];
        public long LastId,Clears;
        public float Width=-1;
        public string LayoutKey="";
        public int Unread;
        public bool Following=true;
    }
    private readonly Dictionary<ChatTabSettings,ChatTabView> chatViews=[];
    private const int ChatLayoutsPerFrame=400;

    private void DrawChat()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("ChatUi.Title"),localization.Get("ChatUi.Help"));
        var options=ChatOptions;
        if(ChatTwoLoaded?.Invoke()==true)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("ChatUi.ChatTwoActive"));
        BoolSetting("ChatUi.Window","ChatUi.WindowTip",()=>options.WindowOpen,value=>{options.WindowOpen=value;if(!value)ChatMessages.Clear();},c=>c.Chat?.WindowOpen==true);
        ImGui.TextDisabled(localization.Get("ChatUi.Phase"));
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatUi.SectionDisplay"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            var defaults=new ChatSettings();
            BoolSetting("ChatUi.Timestamps","ChatUi.TimestampsTip",()=>options.ShowTimestamps,value=>options.ShowTimestamps=value,_=>defaults.ShowTimestamps);
            BoolSetting("ChatUi.Clock24","ChatUi.Clock24Tip",()=>options.Clock24,value=>options.Clock24=value,_=>defaults.Clock24);
            BoolSetting("ChatUi.Seconds","ChatUi.SecondsTip",()=>options.ShowSeconds,value=>options.ShowSeconds=value,_=>defaults.ShowSeconds);
            BoolSetting("ChatUi.Channel","ChatUi.ChannelTip",()=>options.ShowChannel,value=>options.ShowChannel=value,_=>defaults.ShowChannel);
            BoolSetting("ChatUi.Follow","ChatUi.FollowTip",()=>options.FollowNewMessages,value=>options.FollowNewMessages=value,_=>defaults.FollowNewMessages);
            BoolSetting("ChatUi.HideCombat","ChatUi.HideCombatTip",()=>options.HideInCombat,value=>options.HideInCombat=value,_=>defaults.HideInCombat);
            IntSetting("ChatUi.Opacity","ChatUi.OpacityTip",()=>options.BoundedOpacity,value=>options.Opacity=value,10,100,defaults.Opacity);
            IntSetting("ChatUi.FontScale","ChatUi.FontScaleTip",()=>options.BoundedFontScale,value=>options.FontScale=value,60,200,defaults.FontScale);
            IntSetting("ChatUi.MaxLines","ChatUi.MaxLinesTip",()=>options.BoundedMaxLines,value=>options.MaxLines=value,ChatStore.MinimumLines,ChatStore.MaximumLines,defaults.MaxLines);
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatUi.SectionTabs")))DrawChatTabSettings(options);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatUi.SectionColors")))DrawChatColors(options);
        if(developerMode && TenshoAssistant.UI.UiComponents.Header(localization.Get("ChatUi.SectionProbe")))DrawChatProbe();
    }

    private string ChatChannelName(ChatChannel channel)=>localization.Get($"ChatChannels.{channel}");
    private string ChatChannelShort(ChatChannel channel)=>localization.Get($"ChatChannelsShort.{channel}");

    private void DrawChatTabSettings(ChatSettings options)
    {
        var tabs=options.EnsureTabs(localization.Get("ChatUi.TabAll"),localization.Get("ChatUi.TabSocial"));
        for(var index=0;index<tabs.Count;index++)
        {
            var tab=tabs[index];
            ImGui.PushID(index);
            try
            {
                var name=tab.Name ?? "";
                ImGui.SetNextItemWidth(180);
                if(ImGui.InputText(localization.Get("ChatUi.TabName")+"###ChatTabName",ref name,ChatSettings.MaximumTabName)){tab.Name=name;SaveReminders();}
                Tip("ChatUi.TabNameTip");
                ImGui.SameLine();
                var unread=tab.ShowUnread;
                if(ImGui.Checkbox(localization.Get("ChatUi.TabUnread")+"###ChatTabUnread",ref unread)){tab.ShowUnread=unread;SaveReminders();}
                Tip("ChatUi.TabUnreadTip");
                if(tabs.Count>1)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("ChatUi.TabRemove"))){chatViews.Remove(tab);tabs.RemoveAt(index);SaveReminders();return;}
                }
                if(ImGui.TreeNode(localization.Format("ChatUi.TabChannels",tab.Channels?.Count ?? 0)+"###ChatTabChannels"))
                {
                    try
                    {
                        tab.Channels??=[];
                        var column=0;
                        foreach(var channel in ChatChannels.All)
                        {
                            if(column++%3!=0)ImGui.SameLine(ImGui.GetCursorStartPos().X+(column-1)%3*170);
                            var shown=tab.Shows(channel);
                            if(ImGui.Checkbox(ChatChannelName(channel)+$"###ChatChannel{channel}",ref shown))
                            {
                                if(shown)tab.Channels.Add(channel);else tab.Channels.Remove(channel);
                                chatViews.Remove(tab);SaveReminders();
                            }
                            Tip("ChatUi.ChannelBoxTip");
                        }
                    }
                    finally{ImGui.TreePop();}
                }
            }
            finally{ImGui.PopID();}
            ImGui.Separator();
        }
        if(tabs.Count<ChatSettings.MaximumTabs && ImGui.SmallButton(localization.Get("ChatUi.TabAdd")))
        {tabs.Add(new(){Name=localization.Format("ChatUi.TabNew",tabs.Count+1),Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()});SaveReminders();}
    }

    private static Vector4 ChatColor(uint rgb)=>new(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);

    private void DrawChatColors(ChatSettings options)
    {
        ImGui.TextDisabled(localization.Get("ChatUi.ColorsHelp"));
        foreach(var channel in ChatChannels.All)
        {
            ImGui.PushID((int)channel);
            try
            {
                var color=ChatColor(options.ColorOf(channel));var rgb=new Vector3(color.X,color.Y,color.Z);
                if(ImGui.ColorEdit3(ChatChannelName(channel)+"###ChatColor",ref rgb,ImGuiColorEditFlags.NoInputs))
                {
                    options.Colors??=[];
                    options.Colors[channel]=(uint)(rgb.X*255+.5f)<<16|(uint)(rgb.Y*255+.5f)<<8|(uint)(rgb.Z*255+.5f);
                    SaveReminders();
                }
                Tip("ChatUi.ColorTip");
                if(options.Colors?.ContainsKey(channel)==true)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton("↺")){options.Colors.Remove(channel);SaveReminders();}
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ChatUi.ColorDefaultTip"));
                }
            }
            finally{ImGui.PopID();}
        }
    }

    // Phase 0: what the game's chat holds, in counts only, to compare with the game's own chat.
    private void DrawChatProbe()
    {
        ImGui.TextWrapped(localization.Format("ChatUi.ProbeCount",ChatMessages.Count,ChatMessages.LastId));
        if(ImGui.SmallButton(localization.Get("ChatUi.ProbeLog")))
        {
            log.Information("Chat probe, kinds: {Kinds}",string.Join(", ",ChatMessages.KindCounts().Select(row=>$"{row.Key}={row.Value}")));
            log.Information("Chat probe, payloads: {Payloads}",string.Join(", ",ChatMessages.PayloadCounts().Select(row=>$"{row.Key}={row.Value}")));
        }
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ChatUi.ProbeLogTip"));
        foreach(var row in ChatMessages.KindCounts())ImGui.TextUnformatted($"{row.Key} · {ChatChannels.From(row.Key)} · {row.Value}");
    }

    internal void DrawChatWindow()
    {
        if(windowDisposed || configuration.Chat is not {WindowOpen:true} options)return;
        if(options.HideInCombat && mapLive?.State is {InCombat:true})return;
        var tabs=options.EnsureTabs(localization.Get("ChatUi.TabAll"),localization.Get("ChatUi.TabSocial"));
        UI.FfxivTheme.Push();
        try
        {
            ImGui.SetNextWindowSize(new Vector2(560,320),ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(options.BoundedOpacity/100f);
            var open=true;
            var visible=ImGui.Begin(localization.Get("ChatUi.WindowTitle")+"###TenshoChat",ref open,ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar);
            try
            {
                if(visible && ImGui.BeginTabBar("###TenshoChatTabs",ImGuiTabBarFlags.FittingPolicyScroll))
                {
                    try
                    {
                        foreach(var tab in tabs)
                        {
                            var view=ChatViewOf(tab);
                            RefreshChatView(view,tab,options);
                            var label=(tab.Name is {Length:>0} name ? name:"-")+(tab.ShowUnread && view.Unread>0 ? $" ({view.Unread})":"")+$"###ChatTab{tabs.IndexOf(tab)}";
                            if(!ImGui.BeginTabItem(label))continue;
                            try{DrawChatLines(view,options);}
                            finally{ImGui.EndTabItem();}
                        }
                    }
                    finally{ImGui.EndTabBar();}
                }
            }
            finally{ImGui.End();}
            if(!open){options.WindowOpen=false;ChatMessages.Clear();SaveReminders();}
        }
        finally{UI.FfxivTheme.Pop();}
    }

    private ChatTabView ChatViewOf(ChatTabSettings tab)
    {
        if(!chatViews.TryGetValue(tab,out var view))chatViews[tab]=view=new();
        return view;
    }

    private void RefreshChatView(ChatTabView view,ChatTabSettings tab,ChatSettings options)
    {
        if(view.Clears!=ChatMessages.Clears){view.Clears=ChatMessages.Clears;view.Records.Clear();view.Lines.Clear();view.LastId=0;view.Unread=0;}
        if(view.LastId==ChatMessages.LastId)return;
        foreach(var record in ChatMessages.After(view.LastId))
        {
            view.LastId=record.Id;
            if(!tab.Shows(record.Channel))continue;
            view.Records.Add(record);view.Lines.Add(null);
            if(record.Channel is not ChatChannel.TellOut)view.Unread++;
        }
        var excess=view.Records.Count-options.BoundedMaxLines;
        if(excess>0){view.Records.RemoveRange(0,excess);view.Lines.RemoveRange(0,excess);}
    }

    private void DrawChatLines(ChatTabView view,ChatSettings options)
    {
        var visible=ImGui.BeginChild("###TenshoChatLines",Vector2.Zero,false,ImGuiWindowFlags.NoNav);
        try
        {
            if(!visible)return;
            ImGui.SetWindowFontScale(options.BoundedFontScale/100f);
            // Any change of width or of what a line says lays the lines out again, the newest first and over several frames.
            var width=ImGui.GetContentRegionAvail().X;
            var key=$"{options.ShowTimestamps}{options.Clock24}{options.ShowSeconds}{options.ShowChannel}{options.BoundedFontScale}";
            if(Math.Abs(width-view.Width)>0.5f || key!=view.LayoutKey)
            {
                view.Width=width;view.LayoutKey=key;
                for(var index=0;index<view.Lines.Count;index++)view.Lines[index]=null;
            }
            var budget=ChatLayoutsPerFrame;
            for(var index=view.Lines.Count-1;index>=0 && budget>0;index--)
                if(view.Lines[index] is null){LayOutChatLine(view,index,options);budget--;}
            var height=ImGui.GetTextLineHeightWithSpacing();
            var total=0;
            foreach(var lines in view.Lines)total+=lines?.Count ?? 1;
            var scroll=ImGui.GetScrollY();var viewHeight=ImGui.GetWindowHeight();
            var row=0;
            for(var index=0;index<view.Records.Count;index++)
            {
                var count=view.Lines[index]?.Count ?? 1;
                if((row+count)*height>=scroll && row*height<=scroll+viewHeight)
                {
                    if(view.Lines[index] is null){LayOutChatLine(view,index,options);count=view.Lines[index]!.Count;}
                    DrawChatRecord(view.Records[index],view.Lines[index]!,row,height,options);
                }
                row+=count;
            }
            ImGui.SetCursorPosY(total*height);
            ImGui.Dummy(new Vector2(1,1));
            var atBottom=scroll>=ImGui.GetScrollMaxY()-height;
            if(atBottom)view.Unread=0;
            if(options.FollowNewMessages && view.Following && !atBottom)ImGui.SetScrollY(ImGui.GetScrollMaxY()+total*height);
            if(ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel!=0)view.Following=ImGui.GetIO().MouseWheel<0 && atBottom;
            else if(atBottom)view.Following=true;
        }
        finally{ImGui.EndChild();}
    }

    private void LayOutChatLine(ChatTabView view,int index,ChatSettings options)
        =>view.Lines[index]=ChatFormat.Wrap(ChatFormat.Runs(view.Records[index],options,ChatChannelShort),view.Width,text=>ImGui.CalcTextSize(text).X);

    private void DrawChatRecord(ChatRecord record,IReadOnlyList<IReadOnlyList<ChatRun>> lines,int firstRow,float height,ChatSettings options)
    {
        var color=ChatColor(options.ColorOf(record.Channel));
        for(var line=0;line<lines.Count;line++)
        {
            ImGui.SetCursorPos(new Vector2(0,(firstRow+line)*height));
            var first=true;
            foreach(var run in lines[line])
            {
                if(!first)ImGui.SameLine(0,0);
                first=false;
                ImGui.TextColored(run.Role==ChatRole.Timestamp ? UI.FfxivTheme.TextMuted:color,run.Text);
            }
            if(first)ImGui.TextUnformatted(" ");
        }
    }
}
