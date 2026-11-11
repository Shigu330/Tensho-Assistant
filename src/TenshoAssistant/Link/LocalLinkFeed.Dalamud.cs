using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Link;

/// <summary>
/// X-119 step 2, part 1 (0.1081): the plugin's side of the local link (LocalLink.cs). Once a second on the framework thread, through the
/// competitive gate (DEC-038: nothing read during a cut): the setting is read (off by default), the pipe opened or closed to match, and
/// the snapshot (logged in, territory, its name) read, copied and handed to the server's worker, which writes it off the thread (R07).
/// When a cut starts, a « paused » snapshot without game data replaces the last one. Owner: Plugin.cs, through its registrations.
/// Protocol 2 (0.1114): the small live states (job, level, GP, the four bags' free slots) read with the snapshot, and, while the link is on,
/// two file watchers (the settings file, the plugin's folder) whose events only add a name to a LinkChangeLog (any thread).
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
    private readonly IGameInventory inventory;
    private readonly IObjectTable objects;
    private readonly string settingsFile,folder;
    private readonly LinkChangeLog changes=new();
    private static readonly GameInventoryType[] Bags=[GameInventoryType.Inventory1,GameInventoryType.Inventory2,GameInventoryType.Inventory3,GameInventoryType.Inventory4];
    private FileSystemWatcher? settingsWatcher,folderWatcher;
    private bool liveFailed;
    private LocalLinkServer? server;
    // Timers828Tests: never from long.MinValue.
    private long next=-100_000;
    private long sequence;
    private bool disposed;

    public LocalLinkFeed(IFramework framework,IClientState client,IPlayerState player,IGameInventory inventory,IObjectTable objects,string settingsFile,string folder,
        Configuration configuration,Func<uint,string?> zoneName,string version,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.configuration=configuration;this.zoneName=zoneName;this.version=version;this.log=log;
        this.inventory=inventory;this.objects=objects;this.settingsFile=settingsFile;this.folder=folder;
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
        if(server is null){server=new LocalLinkServer(LocalLinkProtocol.PipeName(),message=>log.Information(message));Watch();}
        var loggedIn=client.IsLoggedIn && player.IsLoaded;
        var territory=loggedIn ? (uint)client.TerritoryType : 0;
        var zone=territory==0 ? "" : zoneName(territory) ?? "";
        if(zone.Length>LocalLinkProtocol.MaxZone)zone=zone[..LocalLinkProtocol.MaxZone];
        server.Publish(new LinkSnapshot(LocalLinkProtocol.Version,version,loggedIn,false,territory,zone,++sequence,ReadLive(loggedIn),changes.Snapshot()));
    }

    /// <summary>The small live states, on the framework thread, copied (R07); null without a character or after a reading failed once (R08).</summary>
    private LinkLive? ReadLive(bool loggedIn)
    {
        if(!loggedIn || liveFailed)return null;
        try
        {
            var job=player.ClassJob.Value.Abbreviation.ExtractText();
            if(job.Length>LocalLinkProtocol.MaxJob)job=job[..LocalLinkProtocol.MaxJob];
            int gp=-1,maxGp=-1;
            if(objects.LocalPlayer is { } local && local.MaxGp>0 && local.CurrentGp<=local.MaxGp){gp=(int)local.CurrentGp;maxGp=(int)local.MaxGp;}
            int free=0,size=0;
            foreach(var bag in Bags)
            {
                var slots=inventory.GetInventoryItems(bag);
                size+=slots.Length;
                foreach(ref readonly var slot in slots)if(slot.IsEmpty || slot.Quantity==0)free++;
            }
            return new(job,player.Level,gp,maxGp,size==0 ? -1 : free,size==0 ? -1 : size);
        }
        catch(Exception error)
        {
            liveFailed=true;
            log.Warning(error,"Local link: live states unavailable after a game update; the link goes on without them.");
            return null;
        }
    }

    /// <summary>The settings file and the plugin's folder watched while the link is on; a failure leaves the link without change notices.</summary>
    private void Watch()
    {
        try
        {
            if(Path.GetDirectoryName(settingsFile) is { } settingsFolder && Directory.Exists(settingsFolder))settingsWatcher=Watcher(settingsFolder,Path.GetFileName(settingsFile),false);
            if(Directory.Exists(folder))folderWatcher=Watcher(folder,"*",true);
        }
        catch(Exception error) when(error is IOException or ArgumentException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            Unwatch();
            log.Warning(error,"Local link: saved files not watched; the desktop app will not read them again by itself.");
        }
    }

    private FileSystemWatcher Watcher(string path,string filter,bool subfolders)
    {
        var watcher=new FileSystemWatcher(path,filter){IncludeSubdirectories=subfolders,NotifyFilter=NotifyFilters.FileName|NotifyFilters.LastWrite|NotifyFilters.Size};
        watcher.Changed+=OnFile;watcher.Created+=OnFile;watcher.Renamed+=OnFile;
        watcher.EnableRaisingEvents=true;
        return watcher;
    }

    // A watcher's thread: only a name added to the log (thread-safe); nothing of the game is touched.
    private void OnFile(object? sender,FileSystemEventArgs change)
    {
        if(LocalLinkProtocol.ChangeName(settingsFile,folder,change.FullPath) is { } name)changes.Add(name);
    }

    private void Unwatch()
    {
        settingsWatcher?.Dispose();settingsWatcher=null;
        folderWatcher?.Dispose();folderWatcher=null;
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
        Unwatch();
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
        Unwatch();
    }
}
