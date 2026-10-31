using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

// Aether currents of one territory: Total 0 means flying is not governed by aether currents there (or unknown).
public sealed record MapFlightState(uint TerritoryId,int Total,int Unlocked,bool Complete,bool MountAllowed);

// Player position, the game's map flag and the flight state of the watched territory, copied on the framework thread.
// Flag is a world X/Z position on FlagMapId, or null when no flag is set.
// Game event marker (quest objectives and similar) copied from AgentMap.EventMarkers: world position, optional area radius.
public sealed record MapEventMarker(uint MapId,uint Icon,Vector3 Position,float Radius,string Tooltip);

// Nearby game objects shown on the map (coffers, gathering points); Level is the gathering level when known, else 0.
public enum MapObjectKind { Treasure, Gathering }
public sealed record MapObjectMarker(MapObjectKind Kind,string Name,Vector3 Position,int Level);

// FATE running in the current zone (IFateTable): FATEs have no static map position in the game data (0.114 probe), only live ones.
public sealed record MapFateMarker(uint Id,string Name,int Level,uint Icon,Vector3 Position,float Radius,int Progress,long SecondsLeft,string State);

// Flights: every territory that has aether currents, refreshed every two seconds (menu wings and the header symbol).
// Events: the game's quest/event markers, refreshed every half second. InCombat/InDuty drive the « hide » settings.
public sealed record MapLiveState(uint TerritoryId,uint MapId,Vector3 Position,float Rotation,DateTimeOffset ObservedAt,uint FlagMapId=0,Vector2? Flag=null,
    IReadOnlyDictionary<uint,MapFlightState>? Flights=null,IReadOnlyList<MapEventMarker>? Events=null,bool InCombat=false,bool InDuty=false,
    IReadOnlyList<MapObjectMarker>? Objects=null,IReadOnlyList<MapFateMarker>? Fates=null);

internal sealed class MapLiveReader : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IObjectTable objects;private readonly IPluginLog log;
    private readonly Dictionary<uint,(uint Set,uint[] Currents)> currents=[];
    private readonly Dictionary<uint,bool> mounts=[];
    private MapLiveState? state;private bool disposed;private DateTimeOffset retryAt;private DateTimeOffset flightAt;
    private IReadOnlyDictionary<uint,MapFlightState>? flights;private int removeFlag;
    private IReadOnlyList<MapEventMarker> events=[];private DateTimeOffset eventsAt;
    private IReadOnlyList<MapObjectMarker> nearby=[];
    private readonly ICondition condition;private readonly IFateTable fateTable;
    private IReadOnlyList<MapFateMarker> fates=[];
    private readonly Dictionary<uint,int> gatheringLevels=[];
    private long teleport;private int costsRequest;private IReadOnlyDictionary<uint,uint>? teleportCosts;
    /// <summary>Queues a teleport to an aetheryte (game action, normal gil cost); sent on the next framework tick.</summary>
    public void Teleport(uint aetheryteId)=>Interlocked.Exchange(ref teleport,aetheryteId);
    /// <summary>
    /// Gil cost per attuned aetheryte from the game's teleport list (the price the game would charge now), or null before the
    /// first read. An aetheryte missing from it is not attuned (or not offered by the game): its cost is unknown, never zero.
    /// </summary>
    public IReadOnlyDictionary<uint,uint>? TeleportCosts=>Volatile.Read(ref teleportCosts);
    /// <summary>Asks for a fresh copy of the teleport costs, read on the next framework tick.</summary>
    public void RefreshTeleportCosts()=>Interlocked.Exchange(ref costsRequest,1);
    public MapLiveState? State=>Volatile.Read(ref state);
    /// <summary>Queues the flag removal; it is written on the next framework tick, never from the UI thread.</summary>
    public void RemoveFlag()=>Interlocked.Exchange(ref removeFlag,1);

    public MapLiveReader(IFramework framework,IClientState client,IObjectTable objects,IDataManager data,ICondition condition,IFateTable fateTable,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.log=log;this.condition=condition;this.fateTable=fateTable;
        try
        {
            foreach(var set in data.GetExcelSheet<AetherCurrentCompFlgSet>())
                if(set.Territory.RowId!=0)currents.TryAdd(set.Territory.RowId,(set.RowId,set.AetherCurrents.Select(current=>current.RowId).Where(id=>id!=0).ToArray()));
            foreach(var territory in data.GetExcelSheet<TerritoryType>())mounts[territory.RowId]=territory.Mount;
            foreach(var point in data.GetExcelSheet<GatheringPoint>())
                if(point.GatheringPointBase.RowId!=0 && point.GatheringPointBase.IsValid)gatheringLevels[point.RowId]=point.GatheringPointBase.Value.GatheringLevel;
        }
        catch(Exception error){log.Warning(error,"Aether current data unavailable; flight state stays unknown.");}
        framework.Update+=Update;
    }
    private void Update(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
        try
        {
            if(Interlocked.Exchange(ref removeFlag,0)==1)ClearFlag();
            if(Interlocked.Exchange(ref teleport,0) is var destination && destination>0)SendTeleport((uint)destination);
            if(Interlocked.Exchange(ref costsRequest,0)==1)Volatile.Write(ref teleportCosts,ReadTeleportCosts());
            var player=client.IsLoggedIn ? objects.LocalPlayer:null;
            if(player is null){Volatile.Write(ref state,null);return;}
            var (flagMap,flag)=ReadFlag();
            if(flights is null || now>=flightAt){flights=ReadFlights();flightAt=now.AddSeconds(2);}
            if(now>=eventsAt){events=ReadEvents();nearby=ReadObjects();fates=ReadFates();eventsAt=now.AddMilliseconds(500);}
            var inCombat=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            var inDuty=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56];
            Volatile.Write(ref state,new MapLiveState(client.TerritoryType,client.MapId,player.Position,player.Rotation,now,flagMap,flag,flights,events,inCombat,inDuty,nearby,fates));
        }
        catch(Exception error){Volatile.Write(ref state,null);retryAt=now.AddSeconds(5);log.Warning(error,"Map position unavailable; retry in five seconds.");}
    }
    private unsafe IReadOnlyDictionary<uint,MapFlightState> ReadFlights()
    {
        var player=PlayerState.Instance();var result=new Dictionary<uint,MapFlightState>();
        if(player==null)return result;
        foreach(var (territory,set) in currents)
            result[territory]=new(territory,set.Currents.Length,set.Currents.Count(id=>player->IsAetherCurrentUnlocked(id)),
                player->IsAetherCurrentZoneComplete(set.Set),mounts.GetValueOrDefault(territory));
        return result;
    }
    // Targetable coffers and gathering points in the object table (what the game currently streams around the player).
    private IReadOnlyList<MapObjectMarker> ReadObjects()
    {
        var result=new List<MapObjectMarker>();
        foreach(var item in objects)
        {
            if(result.Count>=200)break;
            var kind=item.ObjectKind switch
            {
                Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure=>MapObjectKind.Treasure,
                Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint=>MapObjectKind.Gathering,
                _=>(MapObjectKind?)null,
            };
            if(kind is null || !item.IsTargetable)continue;
            result.Add(new(kind.Value,item.Name.TextValue,item.Position,kind==MapObjectKind.Gathering ? gatheringLevels.GetValueOrDefault(item.BaseId):0));
        }
        return result.AsReadOnly();
    }
    // FATEs the game currently lists for this zone, copied (names included); at most 64.
    private IReadOnlyList<MapFateMarker> ReadFates()
    {
        var result=new List<MapFateMarker>();
        foreach(var fate in fateTable)
        {
            if(result.Count>=64)break;
            if(fate is null || !float.IsFinite(fate.Position.X) || !float.IsFinite(fate.Position.Z))continue;
            result.Add(new(fate.FateId,fate.Name.TextValue,fate.Level,fate.MapIconId!=0 ? fate.MapIconId:fate.IconId,fate.Position,
                float.IsFinite(fate.Radius) ? Math.Max(0,fate.Radius):0,fate.Progress,Math.Max(0,fate.TimeRemaining),fate.State.ToString()));
        }
        return result.AsReadOnly();
    }
    private unsafe void SendTeleport(uint aetheryteId)
    {
        var telepo=Telepo.Instance();
        if(telepo==null || !telepo->Teleport(aetheryteId,0))log.Information("Teleport to aetheryte {0} refused by the game.",aetheryteId);
    }
    // Same list as the game's Teleport window (UpdateAetheryteList); housing entries (estate, sub-index) are left out.
    private static unsafe IReadOnlyDictionary<uint,uint> ReadTeleportCosts()
    {
        var telepo=Telepo.Instance();var result=new Dictionary<uint,uint>();
        if(telepo==null)return result;
        var list=telepo->UpdateAetheryteList();
        if(list==null)return result;
        foreach(var entry in *list)
            if(entry.AetheryteId!=0 && entry.SubIndex==0 && entry.Ward==0 && entry.Plot==0)result.TryAdd(entry.AetheryteId,entry.GilCost);
        return result;
    }
    // Quest and event markers of the game map, copied (strings included); at most 256 to bound the work per refresh.
    private static unsafe IReadOnlyList<MapEventMarker> ReadEvents()
    {
        var agent=AgentMap.Instance();
        if(agent==null)return [];
        var result=new List<MapEventMarker>();
        foreach(var marker in agent->EventMarkers)
        {
            if(result.Count>=256)break;
            if(marker.IconId==0 || marker.MapId==0 || !float.IsFinite(marker.Position.X) || !float.IsFinite(marker.Position.Z))continue;
            var tooltip=marker.TooltipString!=null ? marker.TooltipString->ToString():"";
            result.Add(new(marker.MapId,marker.IconId,marker.Position,float.IsFinite(marker.Radius) ? Math.Max(0,marker.Radius):0,tooltip));
        }
        return result.AsReadOnly();
    }
    // Values are copied out of the agent here; no pointer survives this call.
    private static unsafe (uint MapId,Vector2? Flag) ReadFlag()
    {
        var agent=AgentMap.Instance();
        if(agent==null || agent->FlagMarkerCount==0)return (0,null);
        var marker=agent->FlagMapMarkers[0];
        return marker.MapId==0 || !float.IsFinite(marker.XFloat) || !float.IsFinite(marker.YFloat) ? (0,null):(marker.MapId,new Vector2(marker.XFloat,marker.YFloat));
    }
    // The game shows the flag while FlagMarkerCount is non-zero; resetting it removes the flag from the map and minimap.
    private static unsafe void ClearFlag()
    {
        var agent=AgentMap.Instance();
        if(agent!=null)agent->FlagMarkerCount=0;
    }
    public void Dispose(){if(disposed)return;disposed=true;framework.Update-=Update;Volatile.Write(ref state,null);}
}
