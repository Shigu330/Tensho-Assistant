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
    // 0.328 (R-14): whether the Tensho map can draw now (catalogue ready, not hidden in combat or a duty); when it cannot, M keeps
    // the game map on screen instead of hosting it off screen with no map shown at all.
    private readonly Func<bool> canShow;
    private bool moveWarned;private bool wasVisible;private bool keepGameMap;private bool disposed;private DateTimeOffset retryAt;
    private int openRequest;private bool bypass;private DateTimeOffset bypassUntil;
    // 0.151: the game map is kept open off screen while the Tensho map shows (as Mappy does) instead of being closed, so the
    // map agent stays active and keeps the quest and leve markers up to date; closing either window closes the other.
    private bool hosted;private string lastRequest="";
    // 0.484 (user report: after a reload the Tensho map needed M twice): the game map stays open across a reload, and it appeared while the catalogue was still loading, so it was kept for good. For a
    // short time after loading, the decision waits for the Tensho map to be able to draw.
    private readonly DateTimeOffset startingUntil=DateTimeOffset.UtcNow.AddSeconds(15);
    /// <summary>Opens the game map without replacing it (tool button); done on the next framework tick.</summary>
    public void OpenGameMap()=>Interlocked.Exchange(ref openRequest,1);
    // 0.625: the game closes its map when a talk or an event starts: it comes back at the end.
    private readonly MapResume resume=new();
    private string lastPointed="";
    private readonly MapClickMemory clicks=new();
    private bool refocus;
    private ICondition? occupiedFlags;
    private bool Occupied()
    {
        if(occupiedFlags is not { } flags)return false;
        return flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied] || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInEvent]
            || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent] || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent]
            || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied30] || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied33]
            || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied38] || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]
            || flags[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell];
    }
    public NativeMapReplacer(IFramework framework,IGameGui gui,IClientState client,ICondition condition,Func<MapViewSettings> settings,Action<GameMapRequest> open,
        Func<bool> tenshoOpen,Func<bool> canShow,Action closeTensho,IPluginLog log)
    {occupiedFlags=condition;
        this.framework=framework;this.gui=gui;this.client=client;this.settings=settings;this.open=open;this.tenshoOpen=tenshoOpen;this.canShow=canShow;this.closeTensho=closeTensho;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.AddAlways(framework,Update);
    }
    private unsafe void Update(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        // DEC-038: the game map is given back during a competitive cut; the next tick after it hides it again if the setting says so.
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)
        {
            if(!hosted)return;
            hosted=false;
            try{if(gui.GetAddonByName("AreaMap") is { IsNull:false } shown)Restore((AtkUnitBase*)shown.Address);}catch(Exception){}
            return;
        }
        var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
        try
        {
            var options=settings();
            // 0.629: the map key and a click ask for the same thing in the agent: only the mouse tells them apart.
            if(FFXIVClientStructs.FFXIV.Client.UI.UIInputData.Instance() is var input && input!=null && (input->CursorInputs.MouseButtonReleasedFlags & FFXIVClientStructs.FFXIV.Client.System.Input.MouseButtonFlags.LBUTTON)!=0)clicks.Clicked(now);
            var addon=gui.GetAddonByName("AreaMap");
            var unit=addon.IsNull ? null:(AtkUnitBase*)addon.Address;
            var visible=unit!=null && addon.IsVisible;
            if(resume.ShouldReopen(Occupied(),visible,client.TerritoryType) && AgentMap.Instance() is var again && again!=null)
            {
                log.Information("The event is over: the map opens again.");
                again->Show();
            }
            // 0.328: handled once the addon is read. A hosted game map is brought back on screen and no longer hosted (Show did
            // nothing on a map already open, and bypass is only read when the map appears); the Tensho map stays open.
            if(Interlocked.Exchange(ref openRequest,0)==1)
            {
                if(hosted && visible){hosted=false;keepGameMap=true;Restore(unit);}
                else
                {
                    var agent=AgentMap.Instance();
                    if(agent!=null){bypass=true;bypassUntil=now.AddSeconds(2);agent->Show();}
                }
            }
            if(bypass && now>bypassUntil)bypass=false;
            var waiting=visible && !wasVisible && now<startingUntil && options.ReplaceGameMap && !bypass && !canShow();
            if(visible && !wasVisible && !waiting)
            {
                // Decided once, when the game map appears: kept for the whole time it stays open, and kept too when the Tensho
                // map cannot draw then (canShow, 0.328).
                // R-14 (0.454): hosting needs the game's node move: when it did not resolve the game map simply stays (logged once), instead of a
                // replacement half-run again every 5 s.
                var canMove=(nint)AtkResNode.MemberFunctionPointers.SetPositionFloat!=0;
                if(!canMove && !moveWarned){moveWarned=true;log.Warning("The game map cannot be replaced: its node move did not resolve (perhaps after a game update).");}
                keepGameMap=!options.ReplaceGameMap || bypass || !canShow() || !canMove;
                bypass=false;
                if(!keepGameMap)
                {
                    hosted=true;
                    var (request,signature)=ReadRequest(true);lastRequest=signature;
                    // 0.627: the same request as last time is what the game kept, not a new one.
                    // 0.629 (user report): unless the mouse just clicked: the same quest was asked again.
                    if(request.Pointed)
                    {
                        var repeated=signature==lastPointed;
                        var clicked=refocus || clicks.Take(now);
                        refocus=false;
                        if(repeated)log.Information("Same map request again: clicked {0}.",clicked);
                        if(repeated && !clicked)request=request.Plain();
                        else lastPointed=signature;
                    }
                    open(request);
                }
            }
            if(hosted)
            {
                if(!visible)
                {
                    hosted=false;Restore(unit);
                    // 0.629 (user report, log): clicking the quest already shown makes the game close its map: the click asked for the quest, so it opens again on it.
                    if(!Occupied() && lastPointed.Length>0 && lastPointed==lastRequest && clicks.Take(now) && AgentMap.Instance() is var shown && shown!=null)
                    {
                        log.Information("The game closed its map on a click on the same quest: it stays open on it.");
                        refocus=true;
                        shown->Show();
                    }
                    else
                    {
                        resume.GameClosedMap(Occupied(),client.TerritoryType);
                        log.Information("The game closed its map window: the Tensho map closes with it; occupied: {0}.",Occupied());
                        closeTensho();
                    }
                }
                else if(!tenshoOpen()){hosted=false;Restore(unit);AgentMap.Instance()->Hide();visible=false;}
                else
                {
                    // Off screen every frame (the game moves it back when it animates); a new request (journal, link) is relayed.
                    if(unit->RootNode!=null)unit->RootNode->SetPositionFloat(-9000,-9000);
                    var (request,signature)=ReadRequest(false);
                    if(signature!=lastRequest){lastRequest=signature;if(request.Pointed){lastPointed=signature;open(request);}}
                }
            }
            if(!visible)keepGameMap=false;
            wasVisible=visible && !waiting;
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
        if(log && questLog && agent!=null)LogTargetProbe(agent);
        return (new(selected,requested,territory,client.MapId,quest,aetheryte,fate,questLog,target,targetIcon),$"{type}|{requested}|{territory}|{quest}|{aetheryte}|{fate}|{target}");
    }
    // 0.630 (temporary, read only): the game shows a quest beyond a zone-change NPC on that NPC (user capture): where does it keep that place?
    private unsafe void LogTargetProbe(AgentMap* agent)
    {
        var title=MapLiveReader.TooltipText(&agent->CurrentOpenMapInfo.TitleString);
        log.Information("Map target probe: title {0}, player map {1}, temp markers {2}.",title,client.MapId,agent->TempMapMarkerCount);
        for(var index=0;index<agent->TempMapMarkerCount && index<agent->TempMapMarkers.Length;index++)
        {
            var temp=agent->TempMapMarkers[index];
            log.Information("Map target probe, temp {0}: raw {1}, {2}, icon {3}, type {4}, style {5}.",index,temp.MapMarker.X,temp.MapMarker.Y,temp.MapMarker.IconId,temp.Type,temp.StyleFlags);
        }
        if(title.Length==0)return;
        var events=agent->EventMarkers.LongCount;
        if(events is >=0 and <=1024)
        {
            var seen=0;
            foreach(var marker in agent->EventMarkers)
            {
                if(++seen>1024)break;
                if(!MapLiveReader.TooltipText(marker.TooltipString).Contains(title,StringComparison.Ordinal))continue;
                log.Information("Map target probe, event: map {0}, territory {1}, level {2}, objective {3}, position {4}, icon {5}, type {6}, data {7}.",marker.MapId,marker.TerritoryTypeId,marker.LevelId,marker.ObjectiveId,marker.Position,marker.IconId,marker.MarkerType,marker.DataId);
            }
        }
        var game=FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance();
        if(game==null)return;
        var quests=game->QuestMarkers;
        for(var index=0;index<quests.Length;index++)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&quests[index])
            {
                if(info->ObjectiveId==0 || !MapLiveReader.TooltipText(&info->Label).Contains(title,StringComparison.Ordinal))continue;
                var markers=info->MarkerData.LongCount;
                log.Information("Map target probe, quest slot {0}: objective {1}, markers {2}.",index,info->ObjectiveId,markers);
                if(markers is <0 or >64)continue;
                foreach(var marker in info->MarkerData)
                    log.Information("Map target probe, quest marker: map {0}, territory {1}, level {2}, position {3}, icon {4}, type {5}, data {6}.",marker.MapId,marker.TerritoryTypeId,marker.LevelId,marker.Position,marker.IconId,marker.MarkerType,marker.DataId);
            }
        }
    }
    public unsafe void Dispose()
    {
        if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.RemoveAlways(framework,Update);
        // Never leave the game map stuck off screen after an unload.
        try{if(hosted && gui.GetAddonByName("AreaMap") is { IsNull:false } addon)Restore((AtkUnitBase*)addon.Address);}catch(Exception error){log.Warning(error,"Could not restore the game map position.");}
    }
}
