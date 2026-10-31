namespace TenshoAssistant.Monsters;

public enum MonsterHoverStatus { None, Reading, Waiting, Loading, Ready, Unavailable }
// Document may be present while Loading or Unavailable: an older copy stays visible during a refresh or after a failure.
public sealed record MonsterHoverView(uint ItemId, MonsterHoverStatus Status, WikiMonsterDocument? Document, bool Stale);

/// <summary>
/// Hover-only lookup of wiki drop tables: the cached copy is read at once; the network is used only after a stable hover,
/// one request at a time, through the wiki's shared cadence. Callers pass only monster-drop candidates. Never blocks.
/// </summary>
public sealed class MonsterHoverSources : IDisposable
{
    public static readonly TimeSpan NetworkDelay=TimeSpan.FromMilliseconds(1500);
    public static readonly TimeSpan ErrorBackoff=TimeSpan.FromMinutes(1);
    public static readonly TimeSpan CadenceBackoff=WikiMonsterSources.RequestInterval;
    public const int Capacity=128;
    private sealed record Entry(WikiMonsterDocument? Document,bool DiskChecked,DateTimeOffset? RetryAt,DateTimeOffset SeenAt);
    private readonly object gate=new();
    private readonly WikiMonsterSources wiki;
    private readonly TimeProvider clock;
    private readonly CancellationTokenSource lifetime=new();
    private readonly Dictionary<uint,Entry> entries=[];
    private uint diskItem;
    private uint networkItem;
    private bool disposed;
    // The wiki service is borrowed; the plugin disposes it after this service.
    public MonsterHoverSources(WikiMonsterSources wiki,TimeProvider? clock=null){this.wiki=wiki;this.clock=clock ?? TimeProvider.System;}

    public MonsterHoverView Observe(uint itemId,TimeSpan hoveredFor,bool allowNetwork)
    {
        lock(gate)
        {
            if(disposed || itemId==0)return new(itemId,MonsterHoverStatus.None,null,false);
            var now=clock.GetUtcNow();
            var entry=entries.GetValueOrDefault(itemId);
            if(entry is not null)entries[itemId]=entry=entry with{SeenAt=now};
            var document=entry?.Document;
            if(document is not null && wiki.IsFresh(document))return new(itemId,MonsterHoverStatus.Ready,document,false);
            var stale=document is not null;
            if(entry is not { DiskChecked:true })
            {
                if(diskItem==0)StartDisk(itemId,now);
                return new(itemId,MonsterHoverStatus.Reading,document,stale);
            }
            if(networkItem==itemId)return new(itemId,MonsterHoverStatus.Loading,document,stale);
            if(!allowNetwork)return new(itemId,stale ? MonsterHoverStatus.Ready:MonsterHoverStatus.None,document,stale);
            if(entry.RetryAt is { } retry && now<retry)return new(itemId,stale ? MonsterHoverStatus.Ready:MonsterHoverStatus.Unavailable,document,stale);
            if(hoveredFor<NetworkDelay || networkItem!=0)return new(itemId,MonsterHoverStatus.Waiting,document,stale);
            StartNetwork(itemId);
            return new(itemId,MonsterHoverStatus.Loading,document,stale);
        }
    }

    private void Store(uint itemId,Func<Entry?,Entry> update)
    {
        var next=update(entries.GetValueOrDefault(itemId));
        if(!entries.ContainsKey(itemId) && entries.Count>=Capacity)entries.Remove(entries.MinBy(pair=>pair.Value.SeenAt).Key);
        entries[itemId]=next;
    }

    private void StartDisk(uint itemId,DateTimeOffset now)
    {
        diskItem=itemId;var token=lifetime.Token;
        _=Task.Run(async()=>
        {
            WikiMonsterDocument? document=null;
            try{document=await wiki.ReadCachedAsync(itemId,token).ConfigureAwait(false);}
            catch(Exception){document=null;} // An unreadable copy counts as absent; the network may replace it.
            lock(gate)
            {
                if(disposed)return;
                diskItem=0;
                Store(itemId,entry=>new(document ?? entry?.Document,true,entry?.RetryAt,entry?.SeenAt ?? now));
            }
        });
    }

    private void StartNetwork(uint itemId)
    {
        networkItem=itemId;var token=lifetime.Token;
        _=Task.Run(async()=>
        {
            WikiMonsterDocument? document=null;DateTimeOffset? retry=null;
            try{document=await wiki.FetchAsync(itemId,token).ConfigureAwait(false);}
            catch(InvalidOperationException){retry=clock.GetUtcNow()+CadenceBackoff;} // Shared cadence: another wiki request just ran.
            catch(Exception){retry=clock.GetUtcNow()+ErrorBackoff;}
            lock(gate)
            {
                if(disposed)return;
                networkItem=0;
                var now=clock.GetUtcNow();
                Store(itemId,entry=>document is not null ? new(document,true,null,entry?.SeenAt ?? now):new(entry?.Document,true,retry,entry?.SeenAt ?? now));
            }
        });
    }

    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;
            disposed=true;lifetime.Cancel();entries.Clear();
        }
        lifetime.Dispose();
    }
}
