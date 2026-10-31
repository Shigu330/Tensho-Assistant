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
// ObjectiveId: the game's objective (quest) id of the marker, used to focus a quest opened from the journal.
// MarkerType/DataId (0.135, /xllog evidence): type 3 = quest with DataId = quest row − 65536 (71021 → data 24, 71151 → data 141).
public sealed record MapEventMarker(uint MapId,uint Icon,Vector3 Position,float Radius,string Tooltip,uint ObjectiveId=0,byte MarkerType=0,uint DataId=0);

// Nearby game objects shown on the map (coffers, gathering points); Level is the gathering level when known, else 0.
public enum MapObjectKind { Treasure, Gathering }
public sealed record MapObjectMarker(MapObjectKind Kind,string Name,Vector3 Position,int Level);

// FATE running in the current zone (IFateTable): FATEs have no static map position in the game data (0.114 probe), only live ones.
public sealed record MapFateMarker(uint Id,string Name,int Level,uint Icon,Vector3 Position,float Radius,int Progress,long SecondsLeft,string State);

// Flights: every territory that has aether currents, refreshed every two seconds (menu wings and the header symbol).
// Events: the game's quest/event markers, refreshed every half second. InCombat/InDuty drive the « hide » settings.
public sealed record MapLiveState(uint TerritoryId,uint MapId,Vector3 Position,float Rotation,DateTimeOffset ObservedAt,uint FlagMapId=0,Vector2? Flag=null,
    IReadOnlyDictionary<uint,MapFlightState>? Flights=null,IReadOnlyList<MapEventMarker>? Events=null,bool InCombat=false,bool InDuty=false,
    IReadOnlyList<MapObjectMarker>? Objects=null,IReadOnlyList<MapFateMarker>? Fates=null,IReadOnlyList<MapPartyMarker>? Party=null,
    IReadOnlySet<uint>? UnlockedCurrents=null,IReadOnlySet<uint>? Sightseen=null,int SightseeingUnlock=-1,int SightseeingUnlockEx=-1);

// Party member in the same territory as the player (not the player): job icon = 62100 + ClassJob row.
public sealed record MapPartyMarker(string Name,uint Job,int Level,Vector3 Position);

internal sealed class MapLiveReader : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IObjectTable objects;private readonly IPluginLog log;
    private readonly Dictionary<uint,(uint Set,uint[] Currents)> currents=[];
    private readonly Dictionary<uint,bool> mounts=[];
    private MapLiveState? state;private bool disposed;private DateTimeOffset retryAt;private DateTimeOffset flightAt;
    private IReadOnlyDictionary<uint,MapFlightState>? flights;private int removeFlag;
    private IReadOnlyList<MapEventMarker> events=[];private DateTimeOffset eventsAt;
    private IReadOnlyList<MapObjectMarker> nearby=[];
    private readonly ICondition condition;private readonly IFateTable fateTable;private readonly IPartyList party;
    private IReadOnlyList<MapPartyMarker> partyMarkers=[];private IReadOnlySet<uint>? unlockedCurrents;private IReadOnlySet<uint>? sightseen;
    private readonly uint[] allCurrents;private readonly uint[] adventureRows;private int loggedVistas=-1,loggedNative=-1;private int logUnlock=-1,logUnlockEx=-1;
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

    public MapLiveReader(IFramework framework,IClientState client,IObjectTable objects,IDataManager data,ICondition condition,IFateTable fateTable,IPartyList party,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.log=log;this.condition=condition;this.fateTable=fateTable;this.party=party;Log=log;
        uint[] currentIds=[],vistaIds=[];
        try
        {
            currentIds=data.GetExcelSheet<AetherCurrent>().Select(row=>row.RowId).ToArray();
            vistaIds=data.GetExcelSheet<Adventure>().Select(row=>row.RowId).ToArray();
        }
        catch(Exception error){log.Warning(error,"Aether current or sightseeing rows unavailable.");}
        allCurrents=currentIds;adventureRows=vistaIds;
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
            if(flights is null || now>=flightAt){flights=ReadFlights();(unlockedCurrents,sightseen)=ReadCollected();flightAt=now.AddSeconds(2);}
            if(now>=eventsAt){events=ReadEvents();nearby=ReadObjects();fates=ReadFates();partyMarkers=ReadParty();eventsAt=now.AddMilliseconds(500);}
            var inCombat=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            var inDuty=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56];
            Volatile.Write(ref state,new MapLiveState(client.TerritoryType,client.MapId,player.Position,player.Rotation,now,flagMap,flag,flights,events,inCombat,inDuty,nearby,fates,partyMarkers,unlockedCurrents,sightseen,logUnlock,logUnlockEx));
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
    // Unlocked aether currents and discovered vistas, copied as sets. Sightseeing index = Adventure row − first row
    // (rows are contiguous from 0x210000 in the probe); to confirm in game against the sightseeing log.
    private unsafe (IReadOnlySet<uint>,IReadOnlySet<uint>) ReadCollected()
    {
        var player=PlayerState.Instance();
        if(player==null)return (new HashSet<uint>(),new HashSet<uint>());
        var unlocked=new HashSet<uint>();foreach(var id in allCurrents)if(player->IsAetherCurrentUnlocked(id))unlocked.Add(id);
        // Discovered vistas: the raw bit array (bit i = Adventure row i, ClientStructs: bitCount = Adventure row count) is used;
        // the native IsAdventureComplete is only compared and logged (0.128: the user saw completed vistas not hidden).
        var seen=new HashSet<uint>();var native=0;
        // ClientStructs: 0 = log not unlocked, 1 = ARR part 1, 2 = ARR part 2; Ex: 3 = « Sights of the North » done.
        logUnlock=player->SightseeingLogUnlockState;logUnlockEx=player->SightseeingLogUnlockStateEx;
        var bits=player->CompletedAdventures;
        for(var index=0;index<adventureRows.Length;index++)
        {
            if(index>>3<bits.Length && (bits[index>>3]>>(index&7)&1)!=0)seen.Add(adventureRows[index]);
            if(player->IsAdventureComplete((uint)index))native++;
        }
        if(seen.Count!=loggedVistas || native!=loggedNative)
        {
            loggedVistas=seen.Count;loggedNative=native;
            log.Information("Sightseeing: {0} discovered by the bit array, {1} by IsAdventureComplete, of {2} vistas.",seen.Count,native,adventureRows.Length);
        }
        return (unlocked,seen);
    }
    private IReadOnlyList<MapPartyMarker> ReadParty()
    {
        var result=new List<MapPartyMarker>();
        var self=objects.LocalPlayer?.EntityId ?? 0;
        foreach(var member in party)
        {
            if(member is null || member.EntityId==self || member.Territory.RowId!=client.TerritoryType || !float.IsFinite(member.Position.X))continue;
            result.Add(new(member.Name.TextValue,member.ClassJob.RowId,member.Level,member.Position));
            if(result.Count>=8)break;
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
    private static readonly HashSet<uint> loggedEventIcons=[];
    private static IPluginLog? Log;
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
            var tooltip=TooltipText(marker.TooltipString);
            // Evidence for the labels (0.134): once per icon, what the game gives (objective, type, readable tooltip).
            if(loggedEventIcons.Count<64 && loggedEventIcons.Add(marker.IconId))
                Log?.Information("Map event marker icon {0}: objective {1}, type {2}, data {3}, tooltip {4} (length {5}, used {6}, size {7}).",marker.IconId,marker.ObjectiveId,marker.MarkerType,marker.DataId,
                    tooltip.Length>0 ? "read":"unread",marker.TooltipString==null ? -1:marker.TooltipString->StringLength,marker.TooltipString==null ? -1:marker.TooltipString->BufUsed,marker.TooltipString==null ? -1:marker.TooltipString->BufSize);
            result.Add(new(marker.MapId,marker.IconId,marker.Position,float.IsFinite(marker.Radius) ? Math.Max(0,marker.Radius):0,tooltip,marker.ObjectiveId,marker.MarkerType,marker.DataId));
        }
        return result.AsReadOnly();
    }
    // Crash 2026-10-31 08:19 (dalamud.crashhandler.log): Utf8String.ToString() on an event marker tooltip read freed memory
    // with an unbounded length. Only a short string (1–256 bytes, within its buffer) is copied, with its own length; anything
    // else gives "". 0.136: the length is BufUsed − 1, as Utf8String.ToString uses it (StringLength reads 0 in game).
    private static unsafe string TooltipText(FFXIVClientStructs.FFXIV.Client.System.String.Utf8String* text)
    {
        if(text==null)return "";
        // /xllog 0.135: StringLength stays 0 in map markers; BufUsed = text bytes + terminator (e.g. 24 for 23 bytes).
        var used=text->BufUsed;var size=text->BufSize;var length=used-1;
        if(length<=0 || length>256 || size<used || size>65536)return "";
        var pointer=text->StringPtr.Value;
        if(pointer==null)return "";
        try{return System.Text.Encoding.UTF8.GetString(pointer,(int)length);}
        catch(ArgumentException){return "";}
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
