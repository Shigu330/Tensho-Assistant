using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Maps;

/// <summary>
/// When enabled, the game's map window (AreaMap) is closed the frame it appears and the Tensho map shows the same map.
/// Polling the addon each framework tick replaced the 0.107 lifecycle listener, which did not stop the game map in practice.
/// The AltGr + M shortcut (0.108–0.109) was removed in 0.110: it never opened the game map in game.
/// Since 0.151 the game map is hidden off screen rather than closed (see Update).
/// </summary>
internal sealed class NativeMapReplacer : IDisposable
{
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IClientState client;private readonly IPluginLog log;
    private readonly Func<MapViewSettings> settings;private readonly Action<GameMapRequest> open;
    private readonly Func<bool> tenshoOpen;private readonly Action closeTensho;
    private bool wasVisible;private bool keepGameMap;private bool disposed;private DateTimeOffset retryAt;
    private int openRequest;private bool bypass;private DateTimeOffset bypassUntil;
    // 0.151: the game map is kept open off screen while the Tensho map shows (as Mappy does) instead of being closed, so the
    // map agent stays active and keeps the quest and leve markers up to date; closing either window closes the other.
    private bool hosted;private string lastRequest="";
    /// <summary>Opens the game map without replacing it (tool button); done on the next framework tick.</summary>
    public void OpenGameMap()=>Interlocked.Exchange(ref openRequest,1);
    public NativeMapReplacer(IFramework framework,IGameGui gui,IClientState client,Func<MapViewSettings> settings,Action<GameMapRequest> open,
        Func<bool> tenshoOpen,Action closeTensho,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.client=client;this.settings=settings;this.open=open;this.tenshoOpen=tenshoOpen;this.closeTensho=closeTensho;this.log=log;
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
            var unit=addon.IsNull ? null:(AtkUnitBase*)addon.Address;
            var visible=unit!=null && addon.IsVisible;
            if(visible && !wasVisible)
            {
                // Decided once, when the game map appears: kept for the whole time it stays open.
                keepGameMap=!options.ReplaceGameMap || bypass;
                bypass=false;
                if(!keepGameMap)
                {
                    hosted=true;
                    var (request,signature)=ReadRequest(true);lastRequest=signature;
                    open(request);
                }
            }
            if(hosted)
            {
                if(!visible){hosted=false;Restore(unit);closeTensho();}
                else if(!tenshoOpen()){hosted=false;Restore(unit);AgentMap.Instance()->Hide();visible=false;}
                else
                {
                    // Off screen every frame (the game moves it back when it animates); a new request (journal, link) is relayed.
                    if(unit->RootNode!=null)unit->RootNode->SetPositionFloat(-9000,-9000);
                    var (request,signature)=ReadRequest(false);
                    if(signature!=lastRequest){lastRequest=signature;if(request.Pointed)open(request);}
                }
            }
            if(!visible)keepGameMap=false;
            wasVisible=visible;
        }
        catch(Exception error){retryAt=now.AddSeconds(5);log.Warning(error,"Could not replace the game map; it stays open.");}
    }
    private static unsafe void Restore(AtkUnitBase* unit)
    {
        if(unit!=null && unit->RootNode!=null)unit->RootNode->SetPositionFloat(unit->X,unit->Y);
    }
    // What the game asked (CurrentOpenMapInfo, temporary target), with a signature to notice a new request while hosted.
    // A quest, flag or link asks for a map through CurrentOpenMapInfo; at the first frame SelectedMapId can still be the world map.
    private unsafe (GameMapRequest Request,string Signature) ReadRequest(bool log)
    {
        var agent=AgentMap.Instance();
        uint requested=0,territory=0,selected=0,quest=0,aetheryte=0,fate=0;var type="";var questLog=false;System.Numerics.Vector2? target=null;uint targetIcon=0;
        if(agent!=null)
        {
            requested=agent->CurrentOpenMapInfo.MapId;territory=agent->CurrentOpenMapInfo.TerritoryId;
            selected=agent->SelectedMapId;type=agent->CurrentOpenMapInfo.Type.ToString();var kind=agent->CurrentOpenMapInfo.Type;questLog=kind is MapType.QuestLog or MapType.GatheringLog;
            if(questLog && agent->TempMapMarkerCount>0)
            {
                var marker=agent->TempMapMarkers[0].MapMarker;
                target=new System.Numerics.Vector2(marker.X/16f,marker.Y/16f);targetIcon=marker.IconId;
            }
            quest=agent->CurrentOpenMapInfo.QuestId;aetheryte=agent->CurrentOpenMapInfo.AetheryteId;fate=agent->CurrentOpenMapInfo.FateId;
        }
        if(log)this.log.Information("Game map hosted: type {0}, map {1}, territory {2}, selected {3}, quest {4}, aetheryte {5}, fate {6}, target {7}.",type,requested,territory,selected,quest,aetheryte,fate,target?.ToString() ?? "none");
        return (new(selected,requested,territory,client.MapId,quest,aetheryte,fate,questLog,target,targetIcon),$"{type}|{requested}|{territory}|{quest}|{aetheryte}|{fate}|{target}");
    }
    public unsafe void Dispose()
    {
        if(disposed)return;disposed=true;framework.Update-=Update;
        // Never leave the game map stuck off screen after an unload.
        try{if(hosted && gui.GetAddonByName("AreaMap") is { IsNull:false } addon)Restore((AtkUnitBase*)addon.Address);}catch(Exception error){log.Warning(error,"Could not restore the game map position.");}
    }
}
