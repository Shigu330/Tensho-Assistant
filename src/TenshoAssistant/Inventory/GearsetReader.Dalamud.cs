using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Inventory;

/// <summary>The item ids (HQ folded) used by the character's gear sets, and how many sets; null until read.</summary>
public sealed record GearsetItems(ulong Character,int Sets,IReadOnlySet<uint> Items,DateTimeOffset ReadAt);

/// <summary>
/// g, I-10 (0.348): copies the items of every gear set (RaptureGearsetModule) on the framework thread, at most every 10 seconds while the
/// storage report asked for it in the last 5 seconds and the character is logged in; guarded (R-14): a failure leaves it unknown.
/// Removed on dispose.
/// </summary>
public sealed unsafe class GearsetReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private GearsetItems? state;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public GearsetItems? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public GearsetReader(IFramework framework,IPlayerState player,IPluginLog log)
    {
        this.framework=framework;this.player=player;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntilTicks))return;
        nextRead=now.AddSeconds(10);
        if(!player.IsLoaded || player.ContentId==0){Volatile.Write(ref state,null);return;}
        try
        {
            var module=RaptureGearsetModule.Instance();
            if(module==null){Unavailable=true;return;}
            var items=new HashSet<uint>();var sets=0;
            // A set exists when its Exists flag is set (a memory read: no game function is called).
            for(var index=0;index<module->Entries.Length;index++)
            {
                ref var entry=ref module->Entries[index];
                if(!entry.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))continue;
                sets++;
                foreach(var item in entry.Items)
                    if(item.ItemId!=0)items.Add(item.ItemId%500_000);
            }
            Volatile.Write(ref state,new GearsetItems(player.ContentId,sets,items,now));
            Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            Unavailable=true;
            if(!warned){warned=true;log.Warning(error,"Gear sets could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
