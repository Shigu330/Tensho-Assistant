using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gear;

/// <summary>
/// R-11 step 4 (0.340): copies the companion chocobo (CompanionInfo: name, rank, colour, barding worn) and the unlocked bardings
/// (Dalamud IUnlockState.IsBuddyEquipUnlocked) on the framework thread, every 2 seconds while logged in and only while the page asked
/// for it in the last 5 seconds. Guarded (R-14): a failure leaves the state « unavailable » until a read succeeds. Removed on dispose.
/// </summary>
public sealed unsafe class ChocoboReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IUnlockState unlocks;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private ChocoboState? state;
    private DateTimeOffset nextRead;
    private bool disposed,warned;

    public ChocoboState? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public ChocoboReader(IFramework framework,IClientState client,IUnlockState unlocks,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.unlocks=unlocks;this.data=data;this.log=log;
        framework.Update+=OnUpdate;
    }

    /// <summary>Called by the page while it shows the chocobo (UI thread): keeps the reads going for 5 seconds.</summary>
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);
    private long wantedUntilTicks;

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        var wantedUntil=new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero);
        if(disposed || now<nextRead || now>wantedUntil)return;
        nextRead=now.AddSeconds(2);
        if(!client.IsLoggedIn){Volatile.Write(ref state,null);return;}
        try
        {
            var ui=UIState.Instance();
            if(ui==null){Unavailable=true;return;}
            ref var info=ref ui->Buddy.CompanionInfo;
            var worn=info.BuddyEquipRowIds;
            var unlocked=new HashSet<uint>();
            foreach(var row in data.GetExcelSheet<BuddyEquip>())
                if(!row.Name.IsEmpty && unlocks.IsBuddyEquipUnlocked(row))unlocked.Add(row.RowId);
            Volatile.Write(ref state,new ChocoboState(info.NameString,info.Rank,info.CurrentColorStainId,
                worn.Length>0 ? worn[0]:0u,worn.Length>1 ? worn[1]:0u,worn.Length>2 ? worn[2]:0u,unlocked,now));
            Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            Unavailable=true;
            if(!warned){warned=true;log.Warning(error,"The companion chocobo could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
