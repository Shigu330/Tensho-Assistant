using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Hunting;

/// <summary>
/// g, C-12 (0.364): copies the twelve hunting logs (MonsterNoteManager.RankData: current rank, 10 entries × 4 kill counts) on the framework
/// thread, every 2 seconds while logged in and only while the Hunting log page asked for it in the last 5 seconds; plain memory, no
/// function called. A failure leaves the state « unavailable » (R-14); logging out clears it. Removed on dispose.
/// </summary>
public sealed unsafe class HuntingLogReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private IReadOnlyList<HuntSlot>? state;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public IReadOnlyList<HuntSlot>? State=>Volatile.Read(ref state);
    // 0.393: the character's Grand Company (PlayerState.GrandCompany: 0 none, 1 Maelstrom, 2 Twin Adder, 3 Immortal Flames); -1 = not read.
    private int grandCompany=-1;
    public uint? GrandCompany=>Volatile.Read(ref grandCompany) is >= 0 and var company ? (uint)company:null;
    public bool Unavailable { get; private set; }

    public HuntingLogReader(IFramework framework,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;
        framework.Update+=OnUpdate;
    }

    /// <summary>Called by the page while it shows the log (UI thread): keeps the reads going for 5 seconds.</summary>
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(!client.IsLoggedIn){if(State is not null)Volatile.Write(ref state,null);Volatile.Write(ref grandCompany,-1);return;}
        if(now<nextRead || now>new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero))return;
        nextRead=now.AddSeconds(2);
        try
        {
            var manager=MonsterNoteManager.Instance();
            if(manager==null){Unavailable=true;return;}
            var slots=new List<HuntSlot>();
            foreach(ref var info in manager->RankData)
            {
                var entries=new List<IReadOnlyList<byte>>();
                foreach(ref var entry in info.RankData)entries.Add(entry.Counts.ToArray());
                slots.Add(new HuntSlot(info.Rank,entries));
            }
            Volatile.Write(ref state,slots);
            var player=FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
            Volatile.Write(ref grandCompany,player!=null && player->IsLoaded ? player->GrandCompany:-1);
            Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            Unavailable=true;nextRead=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"The hunting log could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
