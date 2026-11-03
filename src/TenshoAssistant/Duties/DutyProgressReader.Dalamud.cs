using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Duties;

/// <summary>
/// g, C-01 (0.355): copies the status of every catalog duty — completed (UIState.IsInstanceContentCompleted, by InstanceContent row) or
/// unlocked (Dalamud IUnlockState.IsInstanceContentUnlocked) — on the framework thread, 2 ms per frame at most, every 5 seconds while
/// logged in and only while the Duty loot page asked for it in the last 5 seconds. The native function is called only once its address
/// is resolved; a failure leaves the state « unavailable » (R-14). Logging out clears it (another character may log in). Removed on dispose.
/// </summary>
public sealed unsafe class DutyProgressReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IUnlockState unlocks;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly Func<DutyCatalog?> catalog;
    private DutyProgressState? state;
    private IReadOnlyList<DutyDefinition>? pending;
    private Dictionary<uint,DutyStatus> building=[];
    private int cursor;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public DutyProgressState? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public DutyProgressReader(IFramework framework,IClientState client,IUnlockState unlocks,IDataManager data,IPluginLog log,Func<DutyCatalog?> catalog)
    {
        this.framework=framework;this.client=client;this.unlocks=unlocks;this.data=data;this.log=log;this.catalog=catalog;
        framework.Update+=OnUpdate;
    }

    /// <summary>Called by the page while it shows the duty list (UI thread): keeps the reads going for 5 seconds.</summary>
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(!client.IsLoggedIn){pending=null;if(State is not null)Volatile.Write(ref state,null);return;}
        try
        {
            if(pending is null)
            {
                if(now<nextRead || now>new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero) || catalog() is not { } duties)return;
                if((nint)UIState.MemberFunctionPointers.IsInstanceContentCompleted==0)
                {Unavailable=true;nextRead=now.AddSeconds(30);if(!warned){warned=true;log.Warning("Duty completion is unavailable: function not resolved.");}return;}
                pending=duties.Entries;cursor=0;building=new(duties.Entries.Count);
            }
            var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.InstanceContent>();
            var start=Stopwatch.GetTimestamp();
            while(cursor<pending.Count)
            {
                var duty=pending[cursor++];
                if(sheet.GetRowOrDefault(duty.InstanceId) is { } row)
                    building[duty.InstanceId]=UIState.IsInstanceContentCompleted(duty.InstanceId) ? DutyStatus.Completed
                        :unlocks.IsInstanceContentUnlocked(row) ? DutyStatus.Unlocked:DutyStatus.Locked;
                if(Stopwatch.GetElapsedTime(start)>=TimeSpan.FromMilliseconds(2))return;
            }
            Volatile.Write(ref state,new DutyProgressState(building,now));
            building=[];pending=null;nextRead=now.AddSeconds(5);Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            pending=null;building=[];Unavailable=true;nextRead=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"Duty completion could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
