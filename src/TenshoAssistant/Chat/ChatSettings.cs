namespace TenshoAssistant.Chat;

/// <summary>One tab of the chat window: its name and the channels it shows.</summary>
public sealed class ChatTabSettings
{
    public string Name{get;set;}="";
    public List<ChatChannel> Channels{get;set;}=[];
    public bool ShowUnread{get;set;}=true;
    /// <summary>X-478 (the user's request of 15/11): the colour of the tab's name (0xRRGGBB); null: the theme's text colour.</summary>
    public uint? NameColor{get;set;}
    public bool Shows(ChatChannel channel)=>Channels?.Contains(channel)==true;
}

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the Tensho chat window's settings, read through bounded values so a hand-edited configuration stays in range.
/// Off by default: while the window is off, no message is copied. Settings only: the messages themselves are never saved.
/// </summary>
public sealed class ChatSettings
{
    public const int MaximumTabs=12,MaximumTabName=24;
    public bool WindowOpen{get;set;}
    public bool HideInCombat{get;set;}
    public bool ShowTimestamps{get;set;}=true;
    public bool Clock24{get;set;}=true;
    public bool ShowSeconds{get;set;}
    public bool ShowChannel{get;set;}=true;
    public int Opacity{get;set;}=85;
    public int FontScale{get;set;}=100;
    public int MaxLines{get;set;}=2000;
    /// <summary>X-448 (0.1454): no title bar nor frame; the buttons at the end of the tab bar stay.</summary>
    public bool Borderless{get;set;}
    /// <summary>X-448 (0.1454): the window is neither moved nor resized.</summary>
    public bool Locked{get;set;}
    /// <summary>X-485 (0.1479): what the window does while a window of the game covers it: it hides (the default, as the panels), it fades
    /// (no background, text faint, clicks go through), or it stays.</summary>
    public ChatCovered WhenCovered{get;set;}=ChatCovered.Hide;
    public List<ChatTabSettings>? Tabs{get;set;}
    /// <summary>X-445 (0.1458): <see cref="CurrentTabs"/> once the tabs follow the game's default tabs (tabs saved before get Battle and Event
    /// once).</summary>
    public int TabsVersion{get;set;}
    /// <summary>X-464 (0.1466): 2, the Battle and Event tabs count no unread line; the General tab saved before 0.1458 leaves the NPC dialogue to Event.</summary>
    public const int CurrentTabs=2;
    public Dictionary<ChatChannel,uint>? Colors{get;set;}
    /// <summary>X-448 (0.1454): down to 0 (transparent; was 10).</summary>
    public int BoundedOpacity=>Math.Clamp(Opacity,0,100);
    public int BoundedFontScale=>Math.Clamp(FontScale,60,200);
    public int BoundedMaxLines=>ChatStore.Bounded(MaxLines);
    public uint ColorOf(ChatChannel channel)=>Colors?.TryGetValue(channel,out var color)==true ? color&0xFFFFFF:ChatChannels.DefaultColor(channel);
    /// <summary>The tabs, made at first use. X-445 (0.1458): the game's three default log tabs (General: everything but the combat lines and the
    /// NPC dialogue; Battle: the combat lines; Event: the NPC dialogue), then Social (only what people say to you or your groups). Tabs saved
    /// before get Battle and Event once, after the first tab, unless a tab already shows that channel alone; the others stay as they are.</summary>
    public List<ChatTabSettings> EnsureTabs(string generalName,string battleName,string eventName,string socialName)
    {
        if(Tabs is { Count:>0 })
        {
            if(TabsVersion<1)
            {
                var at=1;
                foreach(var (name,channel) in new[]{(battleName,ChatChannel.Combat),(eventName,ChatChannel.Npc)})
                {
                    if(Tabs.Count>=MaximumTabs || Tabs.Any(tab=>tab.Channels is [var only] && only==channel))continue;
                    Tabs.Insert(Math.Min(at,Tabs.Count),new(){Name=name,Channels=[channel],ShowUnread=false});
                    at++;
                }
            }
            if(TabsVersion<2)
            {
                // X-464 M2 (0.1466): the Battle and Event tabs made in 0.1458 (one channel) count no unread line, as the game counts none.
                foreach(var tab in Tabs)
                    if(tab.Channels is [var only] && only is ChatChannel.Combat or ChatChannel.Npc)tab.ShowUnread=false;
                // X-464 M3 (0.1466): the General tab saved before 0.1458 (every channel but Combat, exactly) leaves the NPC dialogue to Event.
                if(Tabs.Any(tab=>tab.Channels is [ChatChannel.Npc]))
                {
                    var oldGeneral=ChatChannels.All.Where(channel=>channel!=ChatChannel.Combat).ToHashSet();
                    foreach(var tab in Tabs)
                        if(tab.Channels is { Count:>1 } channels && channels.ToHashSet().SetEquals(oldGeneral))channels.Remove(ChatChannel.Npc);
                }
            }
            TabsVersion=CurrentTabs;
            return Tabs;
        }
        Tabs=
        [
            new(){Name=generalName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.General).ToList()},
            new(){Name=battleName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Battle).ToList(),ShowUnread=false},
            new(){Name=eventName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Event).ToList(),ShowUnread=false},
            new(){Name=socialName,Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()},
        ];
        TabsVersion=CurrentTabs;
        return Tabs;
    }
}
