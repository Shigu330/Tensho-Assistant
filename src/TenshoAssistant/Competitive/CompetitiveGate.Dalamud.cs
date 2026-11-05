using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Competitive;

/// <summary>
/// DEC-038: the one switch of the competitive cut. Every framework subscription of the plugin goes through <see cref="Add"/>: while <see cref="Cut"/>
/// is true, none runs, so nothing is read from the game. A test refuses a subscription that goes around it. <see cref="AddAlways"/> is only for
/// what must undo something when the cut starts: the server bar entries and the game map.
/// </summary>
internal static class CompetitiveGate
{
    private static readonly object Gate=new();
    private static readonly Dictionary<IFramework.OnUpdateDelegate,IFramework.OnUpdateDelegate> Wrappers=[];
    private static volatile bool cut;
    public static bool Cut=>cut;
    /// <summary>On the framework thread, when the cut starts (true) or ends (false).</summary>
    public static event Action<bool>? Changed;

    public static void Add(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        void Guarded(IFramework running){if(!cut)handler(running);}
        lock(Gate)Wrappers[handler]=Guarded;
        framework.Update+=Guarded;
    }
    public static void Remove(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        IFramework.OnUpdateDelegate? guarded;
        lock(Gate){if(!Wrappers.Remove(handler,out guarded))return;}
        framework.Update-=guarded;
    }
    public static void AddAlways(IFramework framework,IFramework.OnUpdateDelegate handler)=>framework.Update+=handler;
    public static void RemoveAlways(IFramework framework,IFramework.OnUpdateDelegate handler)=>framework.Update-=handler;

    internal static void Set(bool value,bool notify)
    {
        if(cut==value)return;
        cut=value;
        if(notify)Changed?.Invoke(value);
    }
    internal static void ClearSubscribers()=>Changed=null;
}

/// <summary>Reads the zones once, decides before any other service starts, then checks at each tick. Reads only what Dalamud's client state already holds.</summary>
internal sealed class CompetitiveWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IReadOnlySet<uint>? zones;

    public CompetitiveWatcher(IFramework framework,IClientState client,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;
        try
        {
            zones=CompetitiveZones.Build(data.GetExcelSheet<TerritoryType>().Select(row=>(row.RowId,row.IsPvpZone)),
                data.GetExcelSheet<ContentFinderCondition>().Select(row=>(row.TerritoryType.RowId,row.PvP,row.ContentType.RowId)));
        }
        // In doubt, stopped: after a game update that breaks these sheets Tensho stays cut rather than running in PvP.
        catch(Exception error){zones=null;log.Warning(error,"Competitive zones unavailable: Tensho stays stopped while logged in.");}
        CompetitiveGate.Set(Decide(),false);
        framework.Update+=Tick;
    }
    private bool Decide()=>CompetitiveZones.IsCut(client.IsLoggedIn,(uint)client.TerritoryType,client.IsPvP,zones,CompetitiveGate.Cut);
    private void Tick(IFramework _)=>CompetitiveGate.Set(Decide(),true);
    public void Dispose()
    {
        framework.Update-=Tick;
        CompetitiveGate.ClearSubscribers();
        CompetitiveGate.Set(false,false);
    }
}
