namespace TenshoAssistant.Chat;

public enum ChatSegmentKind { Text }
public sealed record ChatSegment(string Text,ChatSegmentKind Kind=ChatSegmentKind.Text);
/// <summary>A copy of one chat message, in plain managed data; nothing of the game's memory is kept.</summary>
public sealed record ChatRecord(long Id,DateTimeOffset Time,int Kind,ChatChannel Channel,string Sender,IReadOnlyList<ChatSegment> Body);

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the messages of this session, in memory only (other players' names and words are in them: never written
/// anywhere), the oldest dropped past the bounded cap. Written from the game's chat event, read by the window, under one lock. It also holds each tab's
/// view, so one clear (logout, unload, window turned off) drops every copy at once, the tabs' laid-out lines included. Counts the chat kinds seen, for
/// developer mode (counts only, no text).
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
            var excess=records.Count-Bounded(cap);
            if(excess>0)records.RemoveRange(0,excess);
            kinds[kind]=kinds.GetValueOrDefault(kind)+1;
            return record;
        }
    }

    /// <summary>The lines kept whose number is above <paramref name="id"/>, oldest first; nothing allocated when there is none.</summary>
    public IReadOnlyList<ChatRecord> After(long id)
    {
        lock(gate)
        {
            if(records.Count==0 || id>=nextId-1)return [];
            var skip=(int)Math.Clamp(id-records[0].Id+1,0,records.Count);
            return records.GetRange(skip,records.Count-skip);
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
        lock(gate){records.Clear();views.Clear();clears++;}
    }

    public IReadOnlyList<KeyValuePair<int,int>> KindCounts(){lock(gate)return kinds.OrderBy(row=>row.Key).ToArray();}
}
