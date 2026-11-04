namespace TenshoAssistant.Chat;

/// <summary>One tab of the chat window: its name and the channels it shows.</summary>
public sealed class ChatTabSettings
{
    public string Name{get;set;}="";
    public List<ChatChannel> Channels{get;set;}=[];
    public bool ShowUnread{get;set;}=true;
    public bool Shows(ChatChannel channel)=>Channels?.Contains(channel)==true;
}

/// <summary>CHAT-001: the chat's settings, read through bounded values so a hand-edited configuration stays in range. Off by default: while the window is off, no message is copied.</summary>
public sealed class ChatSettings
{
    public const int MaximumTabs=12,MaximumTabName=24;
    public bool WindowOpen{get;set;}
    public bool HideInCombat{get;set;}
    public bool ShowTimestamps{get;set;}=true;
    public bool Clock24{get;set;}=true;
    public bool ShowSeconds{get;set;}
    public bool ShowChannel{get;set;}=true;
    public bool FollowNewMessages{get;set;}=true;
    public int Opacity{get;set;}=85;
    public int FontScale{get;set;}=100;
    public int MaxLines{get;set;}=2000;
    public List<ChatTabSettings>? Tabs{get;set;}
    public Dictionary<ChatChannel,uint>? Colors{get;set;}
    public int BoundedOpacity=>Math.Clamp(Opacity,10,100);
    public int BoundedFontScale=>Math.Clamp(FontScale,60,200);
    public int BoundedMaxLines=>Math.Clamp(MaxLines,ChatStore.MinimumLines,ChatStore.MaximumLines);
    public uint ColorOf(ChatChannel channel)=>Colors?.TryGetValue(channel,out var color)==true ? color&0xFFFFFF:ChatChannels.DefaultColor(channel);
    /// <summary>The tabs, created at first use: everything said and shown except combat lines, then only what people say to you or your groups.</summary>
    public List<ChatTabSettings> EnsureTabs(string allName,string socialName)
    {
        if(Tabs is {Count:>0})return Tabs;
        Tabs=
        [
            new(){Name=allName,Channels=ChatChannels.All.Where(channel=>channel!=ChatChannel.Combat).ToList()},
            new(){Name=socialName,Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()},
        ];
        return Tabs;
    }
}
