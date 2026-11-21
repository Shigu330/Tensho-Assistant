namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B10 (0.1654, the user's choice of 15/11, after WIM): private conversations: each player's tells, received and sent, in a tab of their
/// own, opened by the first tell that comes (or from a player's menu) and closed by the player. In memory only, for the session: these tabs are
/// never saved and leave at a logout (a player's name is kept nowhere, R04). A tab shows the tells the window already keeps, so it opens with
/// the session's tells with that player (and the older ones the chat history brings back, when the player turned it on). Pure (tests); used on
/// the draw thread.
/// </summary>
public sealed class ChatConversations
{
    /// <summary>The most conversation tabs at once; past it, the least recent one with nothing not seen (and not shown) leaves its place.</summary>
    public const int Maximum=6;
    private static readonly IReadOnlyList<(ChatTabSettings Tab,long From)> None=[];
    private readonly List<ChatTabSettings> tabs=[];
    private readonly Dictionary<ChatTabSettings,DateTimeOffset> latest=[];
    private long lastId,clears=-1,restores;
    private bool started;

    public IReadOnlyList<ChatTabSettings> Tabs=>tabs;

    /// <summary>A sender as a conversation's key: the game's icons (the other world's mark…) and the spaces around taken off.</summary>
    public static string Key(string? sender)=>new string((sender ?? "").Where(letter=>!ChatGlyphs.IsGlyph(letter)).ToArray()).Trim();

    /// <summary>The other player of a tell: the one who wrote it, or the one it was sent to (the game names them as the sender of both, the
    /// window writes « &gt;&gt; Name: »); empty for any other message.</summary>
    public static string PartnerOf(ChatRecord record)=>record.Channel is ChatChannel.TellIn or ChatChannel.TellOut ? Key(record.Sender):"";

    /// <summary>The message is a tell with <paramref name="partner"/> (capitals aside).</summary>
    public static bool With(ChatRecord record,string? partner)
        =>!string.IsNullOrEmpty(partner) && string.Equals(PartnerOf(record),partner,StringComparison.OrdinalIgnoreCase);

    /// <summary>A tab's name as shown: a conversation's is the player's stand-in in capture mode (R25), as their name on the lines.</summary>
    public static string Label(ChatTabSettings tab,bool capture)=>tab.Conversation is not null && capture ? ChatFormat.StandIn(tab.Name):tab.Name;

    public ChatTabSettings? Find(string? partner)
    {
        var key=Key(partner);
        return key.Length==0 ? null:tabs.Find(tab=>string.Equals(tab.Conversation,key,StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The tells kept since the call before: a tab opened for each new player. Only the tells that come after the first call open one:
    /// those kept before it, and those the chat history puts back (numbered again after the last number), never do. A player hidden
    /// (<paramref name="hiddenPlayers"/>, the setting's text) gets none. A logout (the store emptied) closes them all. <paramref name="busy"/>: a tab
    /// that is never replaced (shown, or holding something not seen). Returns the tabs opened, each with the number of the tell that opened it
    /// (counted unread there: <see cref="ChatTabView.CountFrom"/>).</summary>
    public IReadOnlyList<(ChatTabSettings Tab,long From)> Update(ChatStore store,Func<ChatTabSettings,bool> busy,string? hiddenPlayers,uint color,Action<ChatTabSettings> closed)
    {
        if(store.Clears!=clears){Clear(closed);clears=store.Clears;}
        if(!started){started=true;lastId=store.LastId;restores=store.Restores;return None;}
        if(store.Restores!=restores){restores=store.Restores;lastId=store.LastId;return None;}
        List<(ChatTabSettings,long)>? opened=null;
        IReadOnlyList<string>? hidden=null;
        foreach(var record in store.After(lastId))
        {
            lastId=record.Id;
            var partner=PartnerOf(record);
            if(partner.Length==0)continue;
            if(Find(partner) is { } known){latest[known]=record.Time;continue;}
            if(ChatTextFilter.IsPlayer(partner,hidden??=ChatTextFilter.Words(hiddenPlayers)))continue;
            if(Open(partner,record.Time,busy,color,closed) is { } tab)(opened??=[]).Add((tab,record.Id));
        }
        return opened ?? None;
    }

    /// <summary>The conversation with this player: the tab already open, or a new one at the end (past <see cref="Maximum"/>, the least recent
    /// tab not busy leaves its place: <paramref name="closed"/> is told; every tab busy: none). Its name is the player's without their world.</summary>
    public ChatTabSettings? Open(string? sender,DateTimeOffset now,Func<ChatTabSettings,bool> busy,uint color,Action<ChatTabSettings> closed)
    {
        var partner=Key(sender);
        if(partner.Length==0)return null;
        if(Find(partner) is { } known)return known;
        if(tabs.Count>=Maximum)
        {
            ChatTabSettings? oldest=null;
            foreach(var tab in tabs)
                if(!busy(tab) && (oldest is null || latest.GetValueOrDefault(tab)<latest.GetValueOrDefault(oldest)))oldest=tab;
            if(oldest is null)return null;
            Close(oldest);
            closed(oldest);
        }
        var opened=new ChatTabSettings{Name=ChatTextFilter.PlayerName(partner),Channels=[ChatChannel.TellIn,ChatChannel.TellOut],Conversation=partner,NameColor=color};
        tabs.Add(opened);
        latest[opened]=now;
        return opened;
    }

    /// <summary>Closes a conversation's tab (the caller drops its view and pins); false when it is not one of them.</summary>
    public bool Close(ChatTabSettings tab){latest.Remove(tab);return tabs.Remove(tab);}

    /// <summary>Every conversation closed (the setting turned off, a logout); the next update starts again from the tells kept then.</summary>
    public void Clear(Action<ChatTabSettings> closed)
    {
        foreach(var tab in tabs.ToArray()){Close(tab);closed(tab);}
        started=false;
    }
}
