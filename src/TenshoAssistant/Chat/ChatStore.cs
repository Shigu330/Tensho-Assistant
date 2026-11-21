namespace TenshoAssistant.Chat;

/// <remarks>X-474 (0.1477): DalamudLink, a link of Dalamud or of a plugin (its plugin and command).</remarks>
public enum ChatSegmentKind { Text,MapLink,ItemLink,QuestLink,WebLink,DalamudLink,StatusLink,ActionName }
/// <summary>X-403 phase 2 (0.1441): what a link of a message points to: a place (territory, map, coordinate, place name) or an item (id, high
/// quality); 0.1442: or a quest (its row in the game's Quest sheet).</summary>
/// <param name="Url">X-403 phase 2 (0.1453): a web address's absolute form (ChatWeb.Address), for a WebLink.</param>
/// <param name="Plugin">X-474 (0.1477): a DalamudLink's plugin (« Dalamud » for Dalamud's own), with <paramref name="CommandId"/> its command.</param>
/// <param name="Data">X-496 M1 (0.1478): a DalamudLink's own string, encoded: its payloads from the link to its end (the link terminator
/// included), as Dalamud passes it to the link's handler; null when not read (the handler then gets the link's text).</param>
/// <param name="StatusId">X-473 (0.1715): a StatusLink's status (its row in the game's Status sheet).</param>
/// <param name="Action">X-473 (0.1720): an ActionName's name as the combat line writes it (no link of the game; X-734, 0.1734: found in the
/// line's text by the game's template, ChatActionLines).</param>
/// <param name="Param">X-733 (0.1734): a meal's or a medicine's status: its Param on the one who gained it (ChatFoodTip), read at the line's
/// arrival; null for any other status.</param>
public sealed record ChatLink(ChatSegmentKind Kind,uint TerritoryId=0,uint MapId=0,float X=0,float Y=0,string Place="",uint ItemId=0,bool Hq=false,uint QuestId=0,string Url="",string Plugin="",uint CommandId=0,byte[]? Data=null,uint StatusId=0,string Action="",ChatStatusParam? Param=null);
public sealed record ChatSegment(string Text,ChatSegmentKind Kind=ChatSegmentKind.Text,ChatLink? Link=null);
/// <summary>A copy of one chat message, in plain managed data; nothing of the game's memory is kept.</summary>
/// <param name="Source">X-484 (0.1482): who does it, Dalamud's XivChatRelationKind (0 none), and <paramref name="Target"/> to whom: the
/// game's log filters tell the combat lines apart by them.</param>
/// <param name="Job">X-723 (0.1724): the sender's job the game showed when the line arrived (ClassJob row; 0: not known), kept with the line
/// (in the chat history too when it is on): its role colour and job icon stay after the party, and after a reload.</param>
public sealed record ChatRecord(long Id,DateTimeOffset Time,int Kind,ChatChannel Channel,string Sender,IReadOnlyList<ChatSegment> Body,byte Source=0,byte Target=0,uint Job=0);

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the messages of this session, in memory only (other players' names and words are in them: never written
/// anywhere; X-475: except, when the player turns it on with their consent, the chat history on this PC, ChatHistory), the oldest dropped past the bounded cap. Written from the game's chat event, read by the window, under one lock. It also holds each tab's
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
    /// <summary>X-447 B10 (0.1654): how many times messages were put back from the history (<see cref="Restore"/>: every line numbered again).</summary>
    public long Restores{get{lock(gate)return restores;}}
    private long restores;
    /// <summary>X-679 (0.1658): after <see cref="Restore"/>, this session's lines by their number before and after (called outside the store's lock):
    /// what keeps a line's number elsewhere follows it (ChatSources: the senders for a player's menu).</summary>
    public event Action<IReadOnlyDictionary<long,long>>? Renumbered;
    private string? ownName;
    /// <summary>X-447 B7 (0.1646): your character's name, as the reader last saw it (the tabs alert on your first name with the setting on); null
    /// before any message. Your own name: not another player's data.</summary>
    public string? OwnName{get{lock(gate)return ownName;}set{lock(gate)ownName=value;}}
    public long LastId{get{lock(gate)return nextId-1;}}
    public int Count{get{lock(gate)return records.Count;}}

    public static int Bounded(int cap)=>Math.Clamp(cap,MinimumLines,MaximumLines);

    public ChatRecord Add(DateTimeOffset time,int kind,string sender,IReadOnlyList<ChatSegment> body,int cap,byte source=0,byte target=0,uint job=0)
    {
        lock(gate)
        {
            var record=new ChatRecord(nextId++,time,kind,ChatChannels.From(kind),sender,body,source,target,job);
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
    public void Restart(ChatTabSettings tab){lock(gate){views.Remove(tab);DropSearch();}}

    // X-447 B8 (0.1638): the search's view of the tab shown (one at a time), kept here so a clear drops it with the other copies.
    // X-670 M1 (review of 0.1638, R07): filled off the game's thread. Each letter typed made a new view and went through every line kept on the
    // game's thread, with a comparison by culture: 11 to 17 ms a letter at 10 000 lines, 42 to 62 ms the first time.
    private ChatTabView? searchView;
    private bool searchReady,searchStarted;
    private ChatTabSettings? searchTab;
    private string searchKey="";
    private long searchGeneration;

    /// <summary>X-447 B8, X-670 M1: the view of <paramref name="tab"/>'s messages a search keeps (<paramref name="key"/>: what the view depends
    /// on, the text typed and how the sender is shown). At a new key or tab, a new view is filled by <paramref name="fill"/> on a task, off the
    /// game's thread; its second argument tells it a newer key, a clear or the search closed made it useless (it then stops, and that view is
    /// never shown). Returns the view to show and whether it is this key's (Ready): while the new one is filled, the last key's view of the same
    /// tab as it was (not to be refreshed), or null (another tab, or none yet); once filled, that view, which the caller refreshes with the new
    /// lines only. A clear, the tab restarted, the history read back or <see cref="EndSearch"/> drops it.</summary>
    public (ChatTabView? View,bool Ready) Searched(ChatTabSettings tab,string key,Action<ChatTabView,Func<bool>> fill)
    {
        lock(gate)
        {
            if(!searchStarted || !ReferenceEquals(searchTab,tab) || !string.Equals(searchKey,key,StringComparison.Ordinal))
            {
                if(!ReferenceEquals(searchTab,tab))searchView=null;
                var generation=Interlocked.Increment(ref searchGeneration);
                (searchTab,searchKey,searchReady,searchStarted)=(tab,key,false,true);
                var view=new ChatTabView();
                bool Stale()=>Interlocked.Read(ref searchGeneration)!=generation;
                _=Task.Run(()=>
                {
                    try{fill(view,Stale);}
                    catch(OperationCanceledException){return;}
                    lock(gate)if(!Stale())(searchView,searchReady)=(view,true);
                });
            }
            return (searchView,searchReady);
        }
    }

    /// <summary>X-447 B9 (0.1642): the most messages pinned in one tab.</summary>
    public const int MaximumPins=5;
    // X-447 B9: the messages pinned per tab, oldest pinned first.
    private readonly Dictionary<ChatTabSettings,List<ChatRecord>> pins=[];

    /// <summary>X-447 B9 (0.1642, the user's choice of 15/11): the messages pinned at the top of <paramref name="tab"/>, oldest pinned first:
    /// copies in memory only (other players' words, never written: R04), kept while the session lasts even once the tab's lines have moved past
    /// them, dropped by a clear (logout, unload, window turned off) or the tab cleared.</summary>
    public IReadOnlyList<ChatRecord> Pins(ChatTabSettings tab){lock(gate)return pins.TryGetValue(tab,out var list) ? list.ToArray():[];}

    /// <summary>X-447 B9: <paramref name="record"/> is pinned in <paramref name="tab"/>.</summary>
    public bool Pinned(ChatTabSettings tab,ChatRecord record){lock(gate)return pins.TryGetValue(tab,out var list) && list.Contains(record);}

    /// <summary>X-447 B9: pins <paramref name="record"/> at the top of <paramref name="tab"/>, after the others; false when it already is, or when
    /// the tab holds <see cref="MaximumPins"/>.</summary>
    public bool Pin(ChatTabSettings tab,ChatRecord record)
    {
        lock(gate)
        {
            if(!pins.TryGetValue(tab,out var list))pins[tab]=list=[];
            if(list.Count>=MaximumPins || list.Contains(record))return false;
            list.Add(record);
            return true;
        }
    }

    /// <summary>X-447 B9: <paramref name="record"/> unpinned from <paramref name="tab"/>; null: every message of the tab.</summary>
    public void Unpin(ChatTabSettings tab,ChatRecord? record=null)
    {
        lock(gate)
        {
            if(!pins.TryGetValue(tab,out var list))return;
            if(record is null)list.Clear();else list.Remove(record);
            if(list.Count==0)pins.Remove(tab);
        }
    }

    /// <summary>X-670 M1: the search closed: its view dropped (no copy of the messages left), a view being filled told to stop.</summary>
    public void EndSearch(){lock(gate)DropSearch();}

    private void DropSearch(){Interlocked.Increment(ref searchGeneration);searchStarted=false;searchReady=false;searchView=null;searchTab=null;searchKey="";}

    /// <summary>X-475: messages kept on disk from before (oldest first), put before this session's lines: only those older than the oldest
    /// line kept (a line of this session is never twice), the bounds kept as for new lines. Every line is numbered again after the last
    /// number given, and each tab's view is made again from them (taken as read). Returns how many were put back.
    /// X-515 M4: a message's time is to the second; those of the same second as the oldest line kept (a fight or a loot at the reload) are put
    /// back too, except the ones this session holds already (same time, kind, sender and text, counted: two equal lines stay two).</summary>
    public int Restore(IReadOnlyList<ChatRecord> older,int cap)
    {
        var put=Put(older,cap,out var ids);
        if(ids.Count>0)Renumbered?.Invoke(ids);
        return put;
    }

    private int Put(IReadOnlyList<ChatRecord> older,int cap,out Dictionary<long,long> ids)
    {
        ids=[];
        lock(gate)
        {
            var first=records.Count>0 ? records[0].Time:DateTimeOffset.MaxValue;
            var held=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var record in records)
            {
                if(record.Time!=first)continue;
                var key=Same(record);held[key]=held.GetValueOrDefault(key)+1;
            }
            var back=older.Where(record=>record.Time<first || record.Time==first && !Held(held,record)).ToList();
            if(back.Count==0)return 0;
            restores++;
            var all=new List<ChatRecord>(back.Count+records.Count);
            all.AddRange(back);all.AddRange(records);
            records.Clear();combat=0;
            // X-673 M1 (review of 0.1643): this session's lines by their number before, for the pins to follow them.
            var moved=new Dictionary<long,ChatRecord>();
            for(var at=0;at<all.Count;at++)
            {
                var record=all[at];
                var numbered=record with{Id=nextId++,Channel=ChatChannels.From(record.Kind)};
                records.Add(numbered);
                if(at>=back.Count){moved[record.Id]=numbered;ids[record.Id]=numbered.Id;}
                if(numbered.Channel==ChatChannel.Combat)combat++;
            }
            // X-673 M1: a pinned message takes its new number (a click goes to it, its menu knows it pinned and unpins it, never pins it twice);
            // a pin whose line had already left the store keeps its own.
            foreach(var list in pins.Values)
                for(var at=0;at<list.Count;at++)
                    if(moved.TryGetValue(list[at].Id,out var numbered))list[at]=numbered;
            var limit=Bounded(cap);
            while(combat>limit)RemoveOldest(ChatChannel.Combat,true);
            while(records.Count-combat>limit)RemoveOldest(ChatChannel.Combat,false);
            views.Clear();DropSearch();
            return back.Count;
        }
    }

    // X-515 M4: what tells a message read back from one of this session (the link pieces by their text: a link read back may be text now).
    private static string Same(ChatRecord record)
        =>string.Join('\u001f',record.Kind,record.Sender,record.Source,record.Target,string.Concat(record.Body.Select(segment=>segment.Text)));

    // One of this session's lines of that second matches: counted off (so a second equal line read back is still put back).
    private static bool Held(Dictionary<string,int> held,ChatRecord record)
    {
        var key=Same(record);
        if(held.GetValueOrDefault(key)<=0)return false;
        held[key]--;
        return true;
    }

    public void Clear()
    {
        lock(gate){records.Clear();views.Clear();DropSearch();pins.Clear();clears++;combat=0;}
    }

    public IReadOnlyList<KeyValuePair<int,int>> KindCounts(){lock(gate)return kinds.OrderBy(row=>row.Key).ToArray();}
}
