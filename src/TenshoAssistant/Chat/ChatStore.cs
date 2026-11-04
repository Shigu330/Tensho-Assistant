namespace TenshoAssistant.Chat;

public enum ChatSegmentKind { Text }
public sealed record ChatSegment(string Text,ChatSegmentKind Kind=ChatSegmentKind.Text);
/// <summary>A copy of one chat message; nothing of the game's memory is kept.</summary>
public sealed record ChatRecord(long Id,DateTimeOffset Time,int Kind,ChatChannel Channel,string Sender,IReadOnlyList<ChatSegment> Body);

/// <summary>
/// CHAT-001: the messages of this session, in memory, oldest dropped past the cap. Written from the game's chat event, read by the window under one lock. Also counts, for the probe, the chat
/// kinds and payload types seen (counts only, no text).
/// </summary>
public sealed class ChatStore
{
    public const int MinimumLines=100,MaximumLines=10000;
    private readonly object gate=new();
    private readonly Queue<ChatRecord> records=new();
    private readonly Dictionary<int,int> kinds=[];
    private readonly Dictionary<string,int> payloads=[];
    private long nextId=1;
    private long clears;
    public long Clears{get{lock(gate)return clears;}}
    public long LastId{get{lock(gate)return nextId-1;}}
    public int Count{get{lock(gate)return records.Count;}}
    public ChatRecord Add(DateTimeOffset time,int kind,string sender,IReadOnlyList<ChatSegment> body,int cap,IEnumerable<string>? payloadTypes=null)
    {
        lock(gate)
        {
            var record=new ChatRecord(nextId++,time,kind,ChatChannels.From(kind),sender,body);
            records.Enqueue(record);
            var limit=Math.Clamp(cap,MinimumLines,MaximumLines);
            while(records.Count>limit)records.Dequeue();
            kinds[kind]=kinds.GetValueOrDefault(kind)+1;
            if(payloadTypes is not null)foreach(var type in payloadTypes)payloads[type]=payloads.GetValueOrDefault(type)+1;
            return record;
        }
    }
    public IReadOnlyList<ChatRecord> After(long id)
    {
        lock(gate)return records.Where(record=>record.Id>id).ToArray();
    }
    public void Clear()
    {
        lock(gate){records.Clear();clears++;}
    }
    public IReadOnlyList<KeyValuePair<int,int>> KindCounts(){lock(gate)return kinds.OrderBy(row=>row.Key).ToArray();}
    public IReadOnlyList<KeyValuePair<string,int>> PayloadCounts(){lock(gate)return payloads.OrderBy(row=>row.Key,StringComparer.Ordinal).ToArray();}
}
