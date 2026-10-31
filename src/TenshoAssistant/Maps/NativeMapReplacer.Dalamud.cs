using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Maps;

/// <summary>
/// When enabled, the game's map window (AreaMap) is closed the frame it appears and the Tensho map shows the same map.
/// Polling the addon each framework tick replaced the 0.107 lifecycle listener, which did not stop the game map in practice.
/// The AltGr + M shortcut (0.108–0.109) was removed in 0.110: it never opened the game map in game.
/// </summary>
internal sealed class NativeMapReplacer : IDisposable
{
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IClientState client;private readonly IPluginLog log;
    private readonly Func<MapViewSettings> settings;private readonly Action<GameMapRequest> open;
    private bool wasVisible;private bool keepGameMap;private bool disposed;private DateTimeOffset retryAt;
    private int openRequest;private bool bypass;private DateTimeOffset bypassUntil;
    /// <summary>Opens the game map without replacing it (tool button); done on the next framework tick.</summary>
    public void OpenGameMap()=>Interlocked.Exchange(ref openRequest,1);
    public NativeMapReplacer(IFramework framework,IGameGui gui,IClientState client,Func<MapViewSettings> settings,Action<GameMapRequest> open,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.client=client;this.settings=settings;this.open=open;this.log=log;
        framework.Update+=Update;
    }
    private unsafe void Update(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
        try
        {
            var options=settings();
            if(Interlocked.Exchange(ref openRequest,0)==1)
            {
                var agent=AgentMap.Instance();
                if(agent!=null){bypass=true;bypassUntil=now.AddSeconds(2);agent->Show();}
            }
            if(bypass && now>bypassUntil)bypass=false;
            var addon=gui.GetAddonByName("AreaMap");
            var visible=!addon.IsNull && addon.IsVisible;
            if(visible && !wasVisible)
            {
                // Decided once, when the game map appears: kept for the whole time it stays open.
                keepGameMap=!options.ReplaceGameMap || bypass;
                bypass=false;
                if(!keepGameMap)
                {
                    // A quest, flag or link asks for a map through CurrentOpenMapInfo; at that frame SelectedMapId can still be
                    // the world map (0.111 report: a quest opened the world). The window picks: a zone selection, else the request.
                    var agent=AgentMap.Instance();
                    uint requested=0,territory=0,selected=0;var type="";
                    if(agent!=null)
                    {
                        requested=agent->CurrentOpenMapInfo.MapId;territory=agent->CurrentOpenMapInfo.TerritoryId;
                        selected=agent->SelectedMapId;type=agent->CurrentOpenMapInfo.Type.ToString();
                    }
                    ((AtkUnitBase*)addon.Address)->Close(true);
                    log.Debug("Game map replaced: type {0}, map {1}, territory {2}, selected {3}.",type,requested,territory,selected);
                    open(new(selected,requested,territory,client.MapId));
                    visible=false;
                }
            }
            if(!visible)keepGameMap=false;
            wasVisible=visible;
        }
        catch(Exception error){retryAt=now.AddSeconds(5);log.Warning(error,"Could not replace the game map; it stays open.");}
    }
    public void Dispose(){if(disposed)return;disposed=true;framework.Update-=Update;}
}
