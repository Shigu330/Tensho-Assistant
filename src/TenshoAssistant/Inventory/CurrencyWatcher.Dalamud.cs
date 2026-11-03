using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Inventory;

/// <summary>What the Currencies tab and the server bar show: the last read, the currencies at or over their threshold, or why not.</summary>
public sealed record CurrencyWatchState(CurrencyReading? Reading,IReadOnlyList<CurrencyBalance> Over,bool Unavailable);

/// <summary>
/// g, I-02 (0.344): reads the tracked currencies on the framework thread every 5 seconds, while logged in, when the alerts or the server
/// bar are on or the Currencies tab is open. Balances: Grand Company seals and their rank cap (InventoryManager.GetCompanySeals /
/// GetMaxCompanySeals, current company only), the CurrencyManager for the currencies it holds (GetItemCount / GetItemMaxCount), else
/// InventoryManager.GetInventoryItemCount with the item's stack size as cap; this week's limited tomestones. Every native function is
/// used only when the game resolved it (pointer checked), each read guarded on its own (R-14): a failure leaves that balance unknown.
/// New threshold crossings go to <see cref="Crossed"/> (framework thread). Nothing is written to the inventory snapshot. Removed on dispose.
/// </summary>
public sealed unsafe class CurrencyWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IDataManager data;
    private readonly Configuration configuration;
    private readonly IPluginLog log;
    private readonly CurrencyAlertTracker tracker=new();
    private IReadOnlyList<CurrencyDefinition>? fixedCurrencies;
    private CurrencyWatchState state=new(null,[],false);
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    // Whole-read failure (cleared by a good read) and single-value failures (logged once per session: a value failing the same way at
    // every read must not fill the log, 0.344 review).
    private bool disposed,warned,valueWarned;

    public CurrencyWatchState State=>Volatile.Read(ref state);
    /// <summary>Called on the framework thread with the currencies that just crossed their threshold (alerts on only).</summary>
    public Action<IReadOnlyList<CurrencyBalance>>? Crossed { get; set; }

    public CurrencyWatcher(IFramework framework,IClientState client,IPlayerState player,IDataManager data,Configuration configuration,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.data=data;this.configuration=configuration;this.log=log;
        framework.Update+=OnUpdate;
    }

    /// <summary>Called by the Currencies tab while shown (UI thread): reads keep going for 5 seconds even with alerts and bar off.</summary>
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(5);
        var alerts=configuration.CurrencyAlerts;
        if(!alerts && !configuration.CurrencyServerBar && now.UtcTicks>Volatile.Read(ref wantedUntilTicks))return;
        if(!client.IsLoggedIn || !player.IsLoaded || player.ContentId==0){tracker.Reset();Volatile.Write(ref state,new(null,[],false));return;}
        try
        {
            var reading=Read(now);
            var (crossed,over)=tracker.Evaluate(reading,configuration.CurrencyThresholds);
            Volatile.Write(ref state,new(reading,over,false));
            warned=false;
            if(alerts && crossed.Count>0)Crossed?.Invoke(crossed);
        }
        catch(Exception error)
        {
            Volatile.Write(ref state,new(null,[],true));
            if(!warned){warned=true;log.Warning(error,"Currencies could not be read.");}
        }
    }

    private CurrencyReading Read(DateTimeOffset now)
    {
        fixedCurrencies??=BuildCurrencies();
        var inventory=InventoryManager.Instance();
        var currency=CurrencyManager.Instance();
        // 0.344 (review): until the game has loaded the Currency container, counts come back 0: every balance is unknown then.
        var loaded=inventory!=null && Guard(()=>inventory->GetInventoryContainer(InventoryType.Currency) is var container && container!=null && container->IsLoaded ? 1:0)==1;
        var balances=new List<CurrencyBalance>();
        // Grand Company seals: the current company only, with its rank's cap (the others have no cap: never alerted).
        var gc=player.GrandCompany.RowId;
        if(CurrencyDefaults.SealItem(gc) is var sealItem and not 0 && Item(sealItem,CurrencyKind.Seals) is { } seals)
            balances.Add(new(seals,
                loaded ? Guard(()=>inventory!=null && Resolved((nint)InventoryManager.MemberFunctionPointers.GetCompanySeals) ? inventory->GetCompanySeals((byte)gc):null):null,
                Guard(()=>inventory!=null && Resolved((nint)InventoryManager.MemberFunctionPointers.GetMaxCompanySeals) ? inventory->GetMaxCompanySeals((byte)gc):null)));
        foreach(var definition in fixedCurrencies)
        {
            long? amount=null,cap=definition.StackCap>0 ? definition.StackCap:null;
            var managed=Guard(()=>currency!=null && Resolved((nint)CurrencyManager.MemberFunctionPointers.HasItem) && currency->HasItem(definition.ItemId) ? 1:0)==1;
            if(!loaded){balances.Add(new(definition,null,cap));continue;}
            if(managed)
            {
                amount=Guard(()=>Resolved((nint)CurrencyManager.MemberFunctionPointers.GetItemCount) ? currency->GetItemCount(definition.ItemId):null);
                if(Guard(()=>Resolved((nint)CurrencyManager.MemberFunctionPointers.GetItemMaxCount) ? currency->GetItemMaxCount(definition.ItemId):null) is > 0 and var max)cap=max;
            }
            else amount=Guard(()=>inventory!=null && Resolved((nint)InventoryManager.MemberFunctionPointers.GetInventoryItemCount)
                ? inventory->GetInventoryItemCount(definition.ItemId,false,false,false,0):null);
            // A count out of range is a read error, not a balance.
            if(amount is < 0 || amount is { } value && cap is { } limit && value>limit*10L)amount=null;
            balances.Add(new(definition,amount,cap));
        }
        // GetWeeklyAcquiredTomestoneCount reads the manager's own field (no game function to resolve). A limit of 0 = no limited
        // tomestone at the moment (kept as 0: the tab says so), not « unknown ».
        int? weekly=loaded && Guard(()=>inventory!=null ? inventory->GetWeeklyAcquiredTomestoneCount():null) is { } got && got>=0 ? (int)got:null;
        int? limitWeekly=Guard(()=>Resolved((nint)InventoryManager.MemberFunctionPointers.GetLimitedTomestoneWeeklyLimit) ? InventoryManager.GetLimitedTomestoneWeeklyLimit():null) is { } cap2 && cap2>=0 ? (int)cap2:null;
        return new(player.ContentId,now,balances,weekly,limitWeekly);
    }

    private static bool Resolved(nint pointer)=>pointer!=0;

    // One read on its own: a failure leaves this value unknown, the others are still read.
    private long? Guard(Func<long?> read)
    {
        try{return read();}
        catch(Exception error){if(!valueWarned){valueWarned=true;log.Warning(error,"A currency could not be read.");}return null;}
    }

    // Default currencies from the game data, plus the permanent, standard and limited tomestones (TomestonesItem: Tomestones rows 1, 2, 3).
    private IReadOnlyList<CurrencyDefinition> BuildCurrencies()
    {
        var list=new List<CurrencyDefinition>();
        foreach(var tome in data.GetExcelSheet<TomestonesItem>())
            if(tome.Tomestones.RowId is >= 1 and <= 3 && tome.Item.RowId!=0 && Item(tome.Item.RowId,CurrencyKind.Tomestone) is { } definition && list.All(known=>known.ItemId!=definition.ItemId))
                list.Add(definition);
        foreach(var id in CurrencyDefaults.Items)
            if(Item(id,CurrencyKind.Other) is { } definition)list.Add(definition);
        return list;
    }

    private CurrencyDefinition? Item(uint id,CurrencyKind kind)
        =>data.GetExcelSheet<Item>().GetRowOrDefault(id) is { } row && !row.Name.IsEmpty ? new(id,row.Name.ExtractText(),row.Icon,kind,row.StackSize):null;

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
