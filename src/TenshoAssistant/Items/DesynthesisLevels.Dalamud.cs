using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Items;

/// <summary>
/// X-197 (0.1761): your desynthesis level of each crafter (ClassJob 8 to 15), read every 5 seconds on the framework thread while logged in
/// (PlayerState.GetDesynthesisLevel, the player loaded) and copied; a level outside 0 to 1000 is unknown (ItemDesynth.Level). A failure is
/// said once and turns the reading off until the plugin reloads: the panel then says « your level unknown ». Cut in competitive content
/// like the rest.
/// </summary>
public sealed unsafe class DesynthesisLevels : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private IReadOnlyDictionary<uint,float> levels=new Dictionary<uint,float>();
    private long next=-100_000;
    private bool disposed,failed;

    public DesynthesisLevels(IFramework framework,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>Your desynthesis level of this crafter; null when unknown.</summary>
    public float? Of(uint job)=>Volatile.Read(ref levels).TryGetValue(job,out var level) ? level:null;

    private void OnUpdate(IFramework _)
    {
        var now=Environment.TickCount64;
        if(disposed || failed || now-next<5000)return;
        next=now;
        try
        {
            var state=PlayerState.Instance();
            if(!client.IsLoggedIn || state==null || !state->IsLoaded){Volatile.Write(ref levels,new Dictionary<uint,float>());return;}
            var read=new Dictionary<uint,float>();
            for(uint job=8;job<=15;job++)
                if(ItemDesynth.Level(state->GetDesynthesisLevel(job)) is { } level)read[job]=level;
            Volatile.Write(ref levels,read);
        }
        catch(Exception error)
        {
            failed=true;
            Volatile.Write(ref levels,new Dictionary<uint,float>());
            log.Warning(error,"Your desynthesis levels could not be read (maybe after a game update): the item panel says « unknown » until the plugin reloads.");
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
