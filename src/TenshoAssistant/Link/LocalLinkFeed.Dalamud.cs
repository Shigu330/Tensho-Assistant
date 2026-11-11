using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Link;

/// <summary>
/// X-119 step 2, part 1 (0.1081): the plugin's side of the local link (LocalLink.cs). Once a second on the framework thread, through the
/// competitive gate (DEC-038: nothing read during a cut): the setting is read (off by default), the pipe opened or closed to match, and
/// the snapshot (logged in, territory, its name) read, copied and handed to the server's worker, which writes it off the thread (R07).
/// When a cut starts, a « paused » snapshot without game data replaces the last one. Owner: Plugin.cs, through its registrations.
/// </summary>
internal sealed class LocalLinkFeed:IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly Configuration configuration;
    private readonly Func<uint,string?> zoneName;
    private readonly string version;
    private readonly IPluginLog log;
    private LocalLinkServer? server;
    // Timers828Tests: never from long.MinValue.
    private long next=-100_000;
    private long sequence;
    private bool disposed;

    public LocalLinkFeed(IFramework framework,IClientState client,IPlayerState player,Configuration configuration,Func<uint,string?> zoneName,string version,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.configuration=configuration;this.zoneName=zoneName;this.version=version;this.log=log;
        CompetitiveGate.Add(framework,OnUpdate);
        CompetitiveGate.Changed+=OnCut;
    }

    /// <summary>The link's state for the settings page (Off while the setting is off) and why, if it failed.</summary>
    public (LocalLinkState State,string? Problem) Status=>server is { } running ? (running.State,running.Problem) : (LocalLinkState.Off,null);

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        var now=Environment.TickCount64;
        if(now-next<1000)return;
        next=now;
        if(!configuration.DesktopLink){Stop();return;}
        server??=new LocalLinkServer(LocalLinkProtocol.PipeName(),message=>log.Information(message));
        var loggedIn=client.IsLoggedIn && player.IsLoaded;
        var territory=loggedIn ? (uint)client.TerritoryType : 0;
        var zone=territory==0 ? "" : zoneName(territory) ?? "";
        if(zone.Length>LocalLinkProtocol.MaxZone)zone=zone[..LocalLinkProtocol.MaxZone];
        server.Publish(new LinkSnapshot(LocalLinkProtocol.Version,version,loggedIn,false,territory,zone,++sequence));
    }

    private void OnCut(bool cut)
    {
        if(cut && !disposed)server?.Publish(new LinkSnapshot(LocalLinkProtocol.Version,version,false,true,0,"",++sequence));
    }

    private void Stop()
    {
        if(server is null)return;
        server.Dispose();
        server=null;
        log.Information("Local link: Off (setting turned off).");
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.Changed-=OnCut;
        CompetitiveGate.Remove(framework,OnUpdate);
        server?.Dispose();
        server=null;
    }
}
