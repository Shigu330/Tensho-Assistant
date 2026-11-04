using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Leves;

/// <summary>
/// g, C-14 (0.363): copies the character's guildleves on the framework thread — completed (Dalamud IUnlockState.IsLeveCompleted, 2 ms
/// per frame at most), accepted (QuestManager.LeveQuests, plain memory) and allowances left — every 5 seconds while logged in and only
/// while the Guildleves page asked for it in the last 5 seconds. A failure leaves the state « unavailable » (R-14); logging out clears it.
/// </summary>
public sealed unsafe class LeveProgressReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IUnlockState unlocks;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly Func<IReadOnlyList<LeveDefinition>> catalog;
    private LeveProgressState? state;
    private IReadOnlyList<LeveDefinition>? pending;
    private HashSet<uint> building=[];
    private int cursor;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public LeveProgressState? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public LeveProgressReader(IFramework framework,IClientState client,IPlayerState player,IUnlockState unlocks,IDataManager data,IPluginLog log,Func<IReadOnlyList<LeveDefinition>> catalog)
    {
        this.framework=framework;this.client=client;this.player=player;this.unlocks=unlocks;this.data=data;this.log=log;this.catalog=catalog;
        framework.Update+=OnUpdate;
    }

    /// <summary>Called by the page while it shows the leves (UI thread): keeps the reads going for 5 seconds.</summary>
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        // R-14 (0.453): before the character is loaded the game answers « not done » to everything: unknown, not zero.
        if(!client.IsLoggedIn || !player.IsLoaded){pending=null;if(State is not null)Volatile.Write(ref state,null);return;}
        try
        {
            if(pending is null)
            {
                if(now<nextRead || now>new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero))return;
                pending=catalog();cursor=0;building=new(pending.Count);
            }
            var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.Leve>();
            var start=Stopwatch.GetTimestamp();
            while(cursor<pending.Count)
            {
                var leve=pending[cursor++];
                if(sheet.GetRowOrDefault(leve.Id) is { } row && unlocks.IsLeveCompleted(row))building.Add(leve.Id);
                if(Stopwatch.GetElapsedTime(start)>=TimeSpan.FromMilliseconds(2))return;
            }
            var accepted=new List<uint>();int? allowances=null;
            var quests=QuestManager.Instance();
            if(quests!=null)
            {
                foreach(ref var leve in quests->LeveQuests)
                    if(leve.LeveId!=0)accepted.Add(leve.LeveId);
                allowances=quests->NumLeveAllowances;
            }
            Volatile.Write(ref state,new LeveProgressState(building,accepted,allowances,now));
            building=[];pending=null;nextRead=now.AddSeconds(5);Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            pending=null;building=[];Unavailable=true;nextRead=now.AddSeconds(30);Volatile.Write(ref state,null);
            if(!warned){warned=true;log.Warning(error,"Guildleves could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
