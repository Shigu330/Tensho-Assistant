namespace TenshoAssistant.Chat;

/// <remarks>X-474 (0.1477): DalamudLink, a link of Dalamud or of a plugin (its plugin and command).</remarks>
public enum ChatSegmentKind { Text,MapLink,ItemLink,QuestLink,WebLink,DalamudLink }
/// <summary>X-403 phase 2 (0.1441): what a link of a message points to: a place (territory, map, coordinate, place name) or an item (id, high
/// quality); 0.1442: or a quest (its row in the game's Quest sheet).</summary>
/// <param name="Url">X-403 phase 2 (0.1453): a web address's absolute form (ChatWeb.Address), for a WebLink.</param>
/// <param name="Plugin">X-474 (0.1477): a DalamudLink's plugin (« Dalamud » for Dalamud's own), with <paramref name="CommandId"/> its command.</param>
public sealed record ChatLink(ChatSegmentKind Kind,uint TerritoryId=0,uint MapId=0,float X=0,float Y=0,string Place="",uint ItemId=0,bool Hq=false,uint QuestId=0,string Url="",string Plugin="",uint CommandId=0);
public sealed record ChatSegment(string Text,ChatSegmentKind Kind=ChatSegmentKind.Text,ChatLink? Link=null);
/// <summary>A copy of one chat message, in plain managed data; nothing of the game's memory is kept.</summary>
public sealed record ChatRecord(long Id,DateTimeOffset Time,int Kind,ChatChannel Channel,string Sender,IReadOnlyList<ChatSegment> Body);

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the messages of this session, in memory only (other players' names and words are in them: never written
/// anywhere), the oldest dropped past the bounded cap. Written from the game's chat event, read by the window, under one lock. It also holds each tab's
/// view, so one clear (logout, unload, window turned off) drops every copy at once, the tabs' laid-out lines included. Counts the chat kinds seen, for
/// developer mode (counts only, no text). X-464 M4 (0.1466): the combat lines have their own limit (the same number): an alliance raid's lines no
/// longer push the spoken lines out; the numbers of the lines kept may then have gaps (<see cref="After"/> searches them).
/// </summary>
public sealed class ChatStore
{
    public const int MinimumLines=100,MaximumLines=10000;
    private readonly object gate=new();
    private readonly List<ChatRecord> records=[];
    private readonly Dictionary<int,int> kinds=[];
    private readonly Dictionary<ChatTabSettings,ChatTabView> views=[];
    private long nextId=1;
    private long clears;
    private int combat;
    public long Clears{get{lock(gate)return clears;}}
    public long LastId{get{lock(gate)return nextId-1;}}
    public int Count{get{lock(gate)return records.Count;}}

    public static int Bounded(int cap)=>Math.Clamp(cap,MinimumLines,MaximumLines);

    public ChatRecord Add(DateTimeOffset time,int kind,string sender,IReadOnlyList<ChatSegment> body,int cap)
    {
        lock(gate)
        {
            var record=new ChatRecord(nextId++,time,kind,ChatChannels.From(kind),sender,body);
            records.Add(record);
            if(record.Channel==ChatChannel.Combat)combat++;
            var limit=Bounded(cap);
            while(combat>limit)RemoveOldest(ChatChannel.Combat,true);
            while(records.Count-combat>limit)RemoveOldest(ChatChannel.Combat,false);
            kinds[kind]=kinds.GetValueOrDefault(kind)+1;
            return record;
        }
    }

    // The oldest line of the combat lines (combat true) or of the others.
    private void RemoveOldest(ChatChannel channel,bool same)
    {
        var index=records.FindIndex(record=>record.Channel==channel==same);
        if(index<0)return;
        if(records[index].Channel==ChatChannel.Combat)combat--;
        records.RemoveAt(index);
    }

    /// <summary>The lines kept whose number is above <paramref name="id"/>, oldest first; nothing allocated when there is none.</summary>
    public IReadOnlyList<ChatRecord> After(long id)
    {
        lock(gate)
        {
            if(records.Count==0 || id>=nextId-1)return [];
            int low=0,high=records.Count;
            while(low<high){var middle=(low+high)/2;if(records[middle].Id<=id)low=middle+1;else high=middle;}
            return low>=records.Count ? []:records.GetRange(low,records.Count-low);
        }
    }

    /// <summary>The view of a tab, made at its first use; a clear drops them all.</summary>
    public ChatTabView ViewOf(ChatTabSettings tab)
    {
        lock(gate)
        {
            if(!views.TryGetValue(tab,out var view))views[tab]=view=new();
            return view;
        }
    }

    /// <summary>A tab whose channels changed, or that was removed: its view is built again from the lines kept, as read.</summary>
    public void Restart(ChatTabSettings tab){lock(gate)views.Remove(tab);}

    public void Clear()
    {
        lock(gate){records.Clear();views.Clear();clears++;combat=0;}
    }

    public IReadOnlyList<KeyValuePair<int,int>> KindCounts(){lock(gate)return kinds.OrderBy(row=>row.Key).ToArray();}
}
