using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Collections;

/// <summary>
/// g, X-02 (0.370): the music playing now — the first BGMSystem scene with a playing BGM (scenes are kept by priority, highest first;
/// to confirm in game), read in memory on the framework thread once a second while the Orchestrion tab asked for it in the last
/// 5 seconds. No function is called and nothing is changed. Null = nothing read (logged out, no scene, failure).
/// </summary>
public sealed unsafe class BgmReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private int playing=-1;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public BgmReader(IFramework framework,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;
        framework.Update+=OnUpdate;
    }

    /// <summary>The BGM row playing now, or null.</summary>
    public ushort? Playing=>Volatile.Read(ref playing) is >= 0 and var id ? (ushort)id:null;
    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(now<nextRead || now>new DateTimeOffset(Volatile.Read(ref wantedUntilTicks),TimeSpan.Zero))return;
        nextRead=now.AddSeconds(1);
        if(!client.IsLoggedIn){Volatile.Write(ref playing,-1);return;}
        try
        {
            var system=BGMSystem.Instance();
            var found=-1;
            if(system!=null)
                foreach(ref var scene in system->Scenes)
                    if(scene.PlayingBgmId!=0){found=scene.PlayingBgmId;break;}
            Volatile.Write(ref playing,found);
            warned=false;
        }
        catch(Exception error)
        {
            Volatile.Write(ref playing,-1);nextRead=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"The music playing could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
