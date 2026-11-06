using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Gathering;

/// <summary>
/// g, X-06 (0.391): every frame on the framework thread while the GP tick bar is on, copies the gatherer's GP (miner, botanist, fisher)
/// and follows its regeneration tick (RegenTickTracker). Nothing is published for another job or in combat (DEC-009: combat is out of
/// scope); the tracker is reset then.
/// </summary>
public sealed class RegenTickReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IObjectTable objects;
    private readonly ICondition condition;
    private readonly Func<bool> enabled;
    private readonly IPluginLog log;
    private readonly RegenTickTracker tracker=new();
    // 0.403 (g, R-08): GP alert at the threshold, said through Reached (framework thread) while Alert gives a threshold (null = off).
    private readonly GpAlertTracker alert=new();
    public Func<int?>? Alert { get; set; }
    public Action<uint,uint>? Reached { get; set; }
    private RegenTickState? state;
    private bool disposed,warned;
    public RegenTickState? State=>Volatile.Read(ref state);

    public RegenTickReader(IFramework framework,IObjectTable objects,ICondition condition,Func<bool> enabled,IPluginLog log)
    {
        this.framework=framework;this.objects=objects;this.condition=condition;this.enabled=enabled;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        try
        {
            var local=objects.LocalPlayer;
            var threshold=Alert?.Invoke();
            // 0.656: a change of zone is not a fall of GP: the alert keeps what it knew (docs/evidence/0.656-gp-alert-teleport.md).
            if(local is null || condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])
            {tracker.Reset();alert.Away(DateTimeOffset.UtcNow);Volatile.Write(ref state,null);return;}
            if(!enabled() && threshold is null || local is null || local.ClassJob.RowId is not (16 or 17 or 18) || condition[ConditionFlag.InCombat])
            {tracker.Reset();alert.Reset();Volatile.Write(ref state,null);return;}
            if(threshold is { } target && alert.Evaluate(local.CurrentGp,local.MaxGp,target,DateTimeOffset.UtcNow))Reached?.Invoke(local.CurrentGp,local.MaxGp);
            else if(threshold is null)alert.Reset();
            Volatile.Write(ref state,enabled() ? tracker.Observe(local.CurrentGp,local.MaxGp,DateTimeOffset.UtcNow) with {JobId=local.ClassJob.RowId}:null);
            warned=false;
        }
        catch(Exception error)
        {
            tracker.Reset();Volatile.Write(ref state,null);
            if(!warned){warned=true;log.Warning(error,"The GP tick could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
