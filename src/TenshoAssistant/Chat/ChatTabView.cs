namespace TenshoAssistant.Chat;

/// <summary>
/// X-403 phase 1 (D-166): one tab of the chat window: its lines, laid out once per message and width (newest first, a few per frame), the row each
/// message starts at (to draw only the rows in view), and its unread count. A line that arrives in a tab not shown counts as unread; showing the tab
/// at its bottom clears the count (<see cref="MarkSeen"/>). A view made while lines are already kept takes them as read. Pure; used on the draw thread.
/// </summary>
public sealed class ChatTabView
{
    private static int lastSerial;
    /// <summary>X-504 M2 (review of 0.1486): the view's own number, given once (a view per tab, ChatStore.ViewOf): it names the tab's lines'
    /// area, so the tab keeps its scroll when a tab before it is removed (named by its place, it took the scroll of the tab before).</summary>
    public int Serial{get;}=Interlocked.Increment(ref lastSerial);
    private readonly List<ChatRecord> records=[];
    private readonly List<IReadOnlyList<IReadOnlyList<ChatRun>>?> lines=[];
    private readonly List<int> rowStarts=[];
    // X-484 (0.1519): how many times each message came again right after itself (tabs folding repeats), and when the last one came.
    private readonly List<int> repeats=[];
    // X-634 (bench X-631, review of 0.1520, M2): whether each message holds an alert word, found once per message and list of words (null:
    // not looked yet), not at each frame by the drawing.
    private readonly List<bool?> alertMarks=[];
    private string? alertKey;
    private IReadOnlyList<string> alertWords=[];
    private DateTimeOffset lastSeen;
    private int rows;
    private bool rowsDirty;
    private long lastId;
    private bool started;
    private int layoutCursor=-1;
    public int Unread{get;private set;}
    /// <summary>X-484 (0.1520): the messages holding an alert word not seen yet (cleared with <see cref="Unread"/>).</summary>
    public int Alerts{get;private set;}
    public int Count=>records.Count;
    public float Width{get;private set;}=-1;
    /// <summary>X-457 (0.1462): the rows and the visible height of the window at the end of the frame before (« at the bottom » is read
    /// against them).</summary>
    public int RowsShown{get;set;}
    public float HeightShown{get;set;}
    public string LayoutKey{get;private set;}="";

    /// <summary>Takes the store's new lines this tab shows, oldest dropped past the cap; true when the tab changed.</summary>
    /// <param name="table">X-484 (0.1482): the game's log filters (null: not read; the tab's channels decide), with <paramref name="settings"/>
    /// for the players hidden.</param>
    /// <param name="also">X-447 B8 (0.1638): a further test each message must pass (the search's text); null: none.</param>
    public bool Refresh(ChatStore store,ChatTabSettings tab,int cap,ChatFilterTable? table=null,ChatSettings? settings=null,Func<ChatRecord,bool>? also=null)
    {
        var changed=false;
        ChatTabFilter? filter=null;
        IReadOnlyList<string>? alerts=null;
        foreach(var record in store.After(lastId))
        {
            lastId=record.Id;
            filter??=new(tab,table,settings);
            if(!filter.Shows(record) || also?.Invoke(record)==false)continue;
            if(tab.FoldRepeats && records.Count>0 && ChatRepeat.Repeats(records[^1],lastSeen,record))
            {
                repeats[^1]++;lastSeen=record.Time;changed=true;
                // X-556 M3 (review of 0.1520): the « ×n » after the message takes room on its last line: laid out again with the count.
                lines[^1]=null;layoutCursor=records.Count-1;
                // X-633 (bench X-631, review of 0.1520, M1): a repeat folded counts as the new message it is: unread, and an alert when it
                // holds a word (before, a word said again within the minute left a tab already seen without « ! »).
                Count(record);
                continue;
            }
            records.Add(record);lines.Add(null);repeats.Add(0);alertMarks.Add(null);lastSeen=record.Time;
            if(rowsDirty)rowStarts.Add(0);else{rowStarts.Add(rows);rows++;}
            layoutCursor=records.Count-1;
            Count(record);
            changed=true;
        }
        started=true;
        void Count(ChatRecord record)
        {
            if(started && ChatChannels.CountsAsUnread(record.Channel) && !ChatAlert.Sent(record))Unread++;
            if(started && ChatAlert.Holds(record,alerts??=ChatTextFilter.Words(settings?.AlertWords)))Alerts++;
        }
        var excess=records.Count-ChatStore.Bounded(cap);
        if(excess>0)
        {
            records.RemoveRange(0,excess);lines.RemoveRange(0,excess);rowStarts.RemoveRange(0,excess);repeats.RemoveRange(0,excess);alertMarks.RemoveRange(0,excess);
            rowsDirty=true;layoutCursor=Math.Max(-1,layoutCursor-excess);
        }
        return changed;
    }

    /// <summary>The tab is shown at its bottom: nothing unread any more.</summary>
    public void MarkSeen(){Unread=0;Alerts=0;}

    public ChatRecord RecordAt(int index)=>records[index];

    /// <summary>X-447 B9 (0.1642): the place of the message numbered <paramref name="id"/>, or of the one it was folded into (the last one not
    /// after it); -1 when every message of the tab is newer (it has left the tab).</summary>
    public int IndexOfId(long id)
    {
        int low=0,high=records.Count-1,found=-1;
        while(low<=high)
        {
            var middle=(low+high)/2;
            if(records[middle].Id<=id){found=middle;low=middle+1;}
            else high=middle-1;
        }
        return found;
    }
    /// <summary>X-484 (0.1519): how many times the message at this place came again right after itself (0: never, or the tab does not fold).</summary>
    public int RepeatsAt(int index)=>repeats[index];

    /// <summary>X-634 (bench X-631, review of 0.1520, M2): the message at this place holds one of <paramref name="words"/> (the setting's text,
    /// one word per line): looked once per message, again for all only when the words change.</summary>
    public bool AlertAt(int index,string? words)
    {
        if(!string.Equals(words,alertKey,StringComparison.Ordinal))
        {
            alertKey=words;alertWords=ChatTextFilter.Words(words);
            for(var at=0;at<alertMarks.Count;at++)alertMarks[at]=null;
        }
        return alertMarks[index]??=ChatAlert.Holds(records[index],alertWords);
    }
    public IReadOnlyList<IReadOnlyList<ChatRun>>? LinesAt(int index)=>lines[index];

    public bool LaidOutFor(float width,string key)=>Math.Abs(width-Width)<=0.5f && key==LayoutKey;

    /// <summary>A new width or a new way of writing the lines: every message is laid out again.</summary>
    public void Relayout(float width,string key)
    {
        Width=width;LayoutKey=key;
        for(var index=0;index<lines.Count;index++)lines[index]=null;
        rowsDirty=true;layoutCursor=lines.Count-1;
    }

    public void SetLines(int index,IReadOnlyList<IReadOnlyList<ChatRun>> laid)
    {
        var before=lines[index]?.Count ?? 1;
        lines[index]=laid;
        if(Math.Max(1,laid.Count)!=before)rowsDirty=true;
    }

    /// <summary>The newest message not laid out yet, or -1.</summary>
    public int NextToLayOut()
    {
        layoutCursor=Math.Min(layoutCursor,lines.Count-1);
        while(layoutCursor>=0 && lines[layoutCursor] is not null)layoutCursor--;
        return layoutCursor;
    }

    /// <summary>X-558 (0.1532): the newest message not laid out yet among the <paramref name="eager"/> newest, or -1: the older ones are laid out
    /// only when shown or near the view (<see cref="OlderToLayOut"/>), never ahead.</summary>
    public int NextToLayOut(int eager)
    {
        var next=NextToLayOut();
        return next>=0 && next>=lines.Count-Math.Max(0,eager) ? next:-1;
    }

    /// <summary>X-558 (0.1532): the nearest message above <paramref name="first"/> (the first one in view), within <paramref name="packet"/>
    /// messages, not laid out yet; -1 when they all are.</summary>
    public int OlderToLayOut(int first,int packet)
    {
        for(var index=Math.Min(first,lines.Count)-1;index>=0 && index>=first-packet;index--)if(lines[index] is null)return index;
        return -1;
    }

    /// <summary>Rows of the whole tab: a message not laid out yet counts one.</summary>
    public int RowCount{get{Rows();return rows;}}
    public int RowOf(int index){Rows();return rowStarts[index];}

    /// <summary>The message on a row (the last one starting at or above it); 0 for an empty tab.</summary>
    public int IndexAtRow(int row)
    {
        Rows();
        int low=0,high=rowStarts.Count-1,found=0;
        while(low<=high)
        {
            var middle=(low+high)/2;
            if(rowStarts[middle]<=row){found=middle;low=middle+1;}
            else high=middle-1;
        }
        return found;
    }

    private void Rows()
    {
        if(!rowsDirty)return;
        rows=0;
        for(var index=0;index<lines.Count;index++){rowStarts[index]=rows;rows+=Math.Max(1,lines[index]?.Count ?? 1);}
        rowsDirty=false;
    }
}
