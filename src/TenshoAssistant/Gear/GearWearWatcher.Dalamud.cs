using Dalamud.Plugin.Services;

namespace TenshoAssistant.Gear;

/// <summary>
/// g, M-06 (0.376): every 5 seconds on the framework thread, while logged in and the wear alert is on, checks the worn gear copied by
/// GearLoader against the thresholds and calls <see cref="Crossed"/> once when a repair or an extraction becomes due.
/// </summary>
public sealed class GearWearWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly Func<IReadOnlyList<EquippedPiece>> equipped;
    private readonly Func<GearWearSettings> settings;
    private readonly IPluginLog log;
    private readonly GearWearAlertTracker tracker=new();
    private DateTimeOffset nextRead;
    private bool disposed,warned;
    /// <summary>Framework thread: (repair due, extraction due, the state that made it).</summary>
    public Action<bool,bool,GearWearState>? Crossed { get; set; }

    public GearWearWatcher(IFramework framework,IClientState client,Func<IReadOnlyList<EquippedPiece>> equipped,Func<GearWearSettings> settings,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.equipped=equipped;this.settings=settings;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(5);
        var options=settings();
        if(!client.IsLoggedIn || !options.Alert){tracker.Reset();return;}
        try
        {
            var state=GearWear.Evaluate(equipped(),options);
            var (repair,extract)=tracker.Evaluate(state);
            if(repair || extract)Crossed?.Invoke(repair,extract,state);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"Gear wear could not be checked.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
