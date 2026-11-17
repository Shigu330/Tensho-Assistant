namespace TenshoAssistant.Chat;

/// <summary>
/// X-545 (0.1517): the tooltip of each channel box and of each game filter of the chat settings, one explanation per channel instead of one
/// shared sentence. The channels' texts come from the game's own help: the TextCommand sheet (each chat command's description: who sees the
/// message, its short form) and the LogFilter sheet (the filters and their examples), read with tools/XbmProbe on 17/11, client 7.58
/// (docs/evidence/0.1517-chat-channel-help.md). Pure: the lines are localisation keys with their arguments, MainWindow formats them.
/// </summary>
public static class ChatHelp
{
    /// <summary>The key of a channel's explanation (section ChatChannelTips) and its argument: the slot number of a linkshell, else none.</summary>
    public static (string Key,object[] Args) Explanation(ChatChannel channel)=>channel switch
    {
        >=ChatChannel.Ls1 and <=ChatChannel.Ls8=>("ChatChannelTips.Linkshell",[channel-ChatChannel.Ls1+1]),
        >=ChatChannel.Cwls1 and <=ChatChannel.Cwls8=>("ChatChannelTips.CrossWorldLinkshell",[channel-ChatChannel.Cwls1+1]),
        _=>("ChatChannelTips."+channel,[]),
    };

    /// <summary>The lines of a channel box's tooltip: its full name (the box's label may be cut), what the channel carries, what ticking it does.</summary>
    public static IReadOnlyList<(string Key,object[] Args)> ChannelTip(ChatChannel channel)=>
        [("ChatChannels."+channel,[]),Explanation(channel),("ChatWindow.ChannelBoxTip",[])];

    /// <summary>The lines of a game filter's tooltip: its name in the game's language, the game's own example line when it has one, the game's
    /// tabs that show it by default (LogFilter's preset: 1 General, 2 Battle, 4 Event), and what ticking it does.</summary>
    public static IReadOnlyList<(string Key,object[] Args)> FilterTip(ChatFilterRow row,Func<string,string> text)
    {
        var lines=new List<(string Key,object[] Args)>{("ChatWindow.FilterTip",[row.Name])};
        if(!string.IsNullOrWhiteSpace(row.Example))lines.Add(("ChatWindow.FilterTipExample",[row.Example.Trim()]));
        var tabs=new List<string>();
        if((row.Preset&ChatFilterTable.GeneralTab)!=0)tabs.Add(text("ChatWindow.TabAll"));
        if((row.Preset&ChatFilterTable.BattleTab)!=0)tabs.Add(text("ChatWindow.TabBattle"));
        if((row.Preset&ChatFilterTable.EventTab)!=0)tabs.Add(text("ChatWindow.TabEvent"));
        lines.Add(tabs.Count==0 ? ("ChatWindow.FilterTipNoTab",[]):("ChatWindow.FilterTipTabs",[string.Join(", ",tabs)]));
        return lines;
    }
}
