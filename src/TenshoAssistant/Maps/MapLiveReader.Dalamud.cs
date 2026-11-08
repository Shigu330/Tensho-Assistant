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
public sealed record MapEventMarker(uint MapId,uint Icon,Vector3 Position,float Radius,string Tooltip,uint ObjectiveId=0,byte MarkerType=0,uint DataId=0,bool IsLeve=false);

// Nearby game objects shown on the map (coffers, gathering points); Level is the gathering level when known, else 0.
// N-09 (0.441): HuntMark = a living hunt mark (NotoriousMonster by BNpcBase); Level holds its rank (1 B, 2 A, 3 S).
// 0.901 (X-83): IslandAnimal: a wild animal of the Island Sanctuary that can be caught (MJIAnimals, by BNpcBase), Icon = its icon in the sheet.
public enum MapObjectKind { Treasure, Gathering, HuntMark, IslandAnimal }
public sealed record MapObjectMarker(MapObjectKind Kind,string Name,Vector3 Position,int Level,uint NameId=0,uint BaseId=0,uint Icon=0);
/// <summary>0.813 (X-49): one point of an accepted quest as the game's own Map holds it (MarkerInfo.ObjectiveId, MapMarkerData map and place).</summary>
public sealed record MapQuestPoint(uint Quest,uint MapId,float X,float Z);
/// <summary>0.795 (X-34): a living enemy the game streams around the player: its name row, base row, name, place and level. Information only.</summary>
public sealed record MapMonster(uint NameId,uint BaseId,string Name,Vector3 Position,int Level,uint FateId=0,uint EventId=0,float Height=0);

// FATE running in the current zone (IFateTable): FATEs have no static map position in the game data (0.114 probe), only live ones.
public sealed record MapFateMarker(uint Id,string Name,int Level,uint Icon,Vector3 Position,float Radius,int Progress,long SecondsLeft,string State,bool HasBonus=false);

// Flights: every territory that has aether currents, refreshed every two seconds (menu wings and the header symbol).
// Events: the game's quest/event markers, refreshed every half second. InCombat/InDuty drive the « hide » settings.
public sealed record MapLiveState(uint TerritoryId,uint MapId,Vector3 Position,float Rotation,DateTimeOffset ObservedAt,uint FlagMapId=0,Vector2? Flag=null,
    IReadOnlyDictionary<uint,MapFlightState>? Flights=null,IReadOnlyList<MapEventMarker>? Events=null,bool InCombat=false,bool InDuty=false,
    IReadOnlyList<MapObjectMarker>? Objects=null,IReadOnlyList<MapFateMarker>? Fates=null,IReadOnlyList<MapPartyMarker>? Party=null,
    IReadOnlySet<uint>? UnlockedCurrents=null,IReadOnlySet<uint>? Sightseen=null,int SightseeingUnlock=-1,int SightseeingUnlockEx=-1,string Area="",string SubArea="",
    IReadOnlyList<MapMonster>? Monsters=null,uint DiscoveryMapId=0,IReadOnlyList<int>? Undiscovered=null,IReadOnlyList<MapQuestPoint>? QuestPoints=null);

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
    private IReadOnlyList<MapObjectMarker> nearby=[];private IReadOnlyList<MapMonster> monsters=[];private IReadOnlyList<MapQuestPoint>? questPoints;
    // P11 (0.803): the map whose discovery the window wants, and what was read for it.
    private int discoveryWanted;private uint discoveryMap;private IReadOnlyList<int>? undiscovered;
    private Dictionary<uint,(uint Flag,int Index,bool Small)>? discoveryRows;
    public void WantDiscovery(uint mapId)=>Volatile.Write(ref discoveryWanted,(int)mapId);
    /// <summary>A game texture's pixels (BGRA), read off the frame; null when missing or unreadable.</summary>
    public (byte[] Bgra,int Width,int Height)? ReadTexture(string path)
    {
        try{return placeData.GetFile<Lumina.Data.Files.TexFile>(path) is { } tex ? (tex.ImageData,tex.Header.Width,tex.Header.Height):null;}
        catch(Exception error){log.Warning(error,"Map texture {0} unreadable.",path);return null;}
    }
    // P11 (0.803): the game keeps each region's state in two tables (16 or 32 regions a map); the Map sheet gives the table (DiscoveryArrayByte: the
    // 16 one, inferred from Central Shroud 13 regions / Heritage Found 30) and the row (DiscoveryIndex). Fields read, no function called.
    private unsafe IReadOnlyList<int>? ReadUndiscovered(uint mapId)
    {
        discoveryRows??=placeData.GetExcelSheet<Lumina.Excel.Sheets.Map>().Where(row=>row.DiscoveryFlag!=0)
            .ToDictionary(row=>row.RowId,row=>(row.DiscoveryFlag,(int)row.DiscoveryIndex,row.DiscoveryArrayByte));
        if(!discoveryRows.TryGetValue(mapId,out var row))return null;
        var manager=FFXIVClientStructs.FFXIV.Client.Game.MapDiscoveryManager.Instance();
        if(manager==null)return null;
        var found=new bool[32];var size=0;
        if(row.Small)
        {
            var tables=manager->MapsWithUpTo16Regions;
            if(row.Index<0 || row.Index>=tables.Length)return null;
            var table=tables[row.Index].Discoveries;size=table.Length;for(var region=0;region<size && region<32;region++)found[region]=table[region];
        }
        else
        {
            var tables=manager->MapsWithUpTo32Regions;
            if(row.Index<0 || row.Index>=tables.Length)return null;
            var table=tables[row.Index].Discoveries;size=table.Length;for(var region=0;region<size && region<32;region++)found[region]=table[region];
        }
        return MapDiscovery.Undiscovered(row.Flag,region=>region<size ? found[region]:null);
    }
    private readonly ICondition condition;private readonly IFateTable fateTable;private readonly IPartyList party;
    private IReadOnlyList<MapPartyMarker> partyMarkers=[];private IReadOnlySet<uint>? unlockedCurrents;private IReadOnlySet<uint>? sightseen;
    private readonly uint[] allCurrents;private readonly uint[] adventureRows;private int loggedVistas=-1,loggedNative=-1;private int logUnlock=-1,logUnlockEx=-1;
    private IReadOnlyList<MapFateMarker> fates=[];
    private readonly Dictionary<uint,int> gatheringLevels=[];
    // 0.621: where the player stands, as the game's map writes it. Names read again only when the place changes.
    private readonly IDataManager placeData;
    private uint areaId,subAreaId;private string areaName="",subAreaName="";
    private unsafe void ReadPlace()
    {
        var info=TerritoryInfo.Instance();
        if(info==null)return;
        string Name(uint id)=>id==0 ? "":placeData.GetExcelSheet<Lumina.Excel.Sheets.PlaceName>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
        if(info->AreaPlaceNameId!=areaId){areaId=info->AreaPlaceNameId;areaName=Name(areaId);}
        if(info->SubAreaPlaceNameId!=subAreaId){subAreaId=info->SubAreaPlaceNameId;subAreaName=Name(subAreaId);}
    }
    // N-09 (0.441): hunt marks by BNpcBase → rank (docs/evidence/0.441-hunt-marks.md).
    private readonly Dictionary<uint,int> huntMarks=[];
    // 0.901 (X-83, docs/evidence/0.901-island-animals.md): the island's animals that can be caught: BNpcBase → icon (MJIAnimals).
    private readonly Dictionary<uint,uint> islandAnimals=[];
    private readonly TeleportListGate teleportGate=new();
    private long teleport;private int costsRequest;private IReadOnlyDictionary<uint,uint>? teleportCosts;
    /// <summary>Queues a teleport to an aetheryte (game action, normal gil cost); sent on the next framework tick.</summary>
    public void Teleport(uint aetheryteId)=>Interlocked.Exchange(ref teleport,aetheryteId);
    /// <summary>
    /// Gil cost per attuned aetheryte from the game's teleport list (the price the game would charge now), or null before the
    /// first read. An aetheryte missing from it is not attuned (or not offered by the game): its cost is unknown, never zero.
    /// </summary>
    public IReadOnlyDictionary<uint,uint>? TeleportCosts=>Volatile.Read(ref teleportCosts);
    // 0.328: the last cost read failed (the popup says so; prices depend on where the player stands, an old copy would mislead).
    private volatile bool costsFailed;
    public bool TeleportCostsFailed=>costsFailed;
    /// <summary>Asks for a fresh copy of the teleport costs, read on the next framework tick.</summary>
    public void RefreshTeleportCosts()=>Interlocked.Exchange(ref costsRequest,1);
    public MapLiveState? State=>Volatile.Read(ref state);
    /// <summary>Queues the flag removal; it is written on the next framework tick, never from the UI thread.</summary>
    public void RemoveFlag()=>Interlocked.Exchange(ref removeFlag,1);

    public MapLiveReader(IFramework framework,IClientState client,IObjectTable objects,IDataManager data,ICondition condition,IFateTable fateTable,IPartyList party,IPluginLog log)
    {placeData=data;
        try
        {
            var keys=data.GetExcelSheet<Lumina.Excel.Sheets.Leve>().Select(row=>(row.RowId,Key:MapLeves.Key(row.Name.ExtractText()))).Where(row=>row.Key.Length>0).ToDictionary(row=>row.RowId,row=>row.Key);
            leveKeys=keys;leveNames=keys.Values.ToHashSet();leveMarkers.Clear();
        }
        catch(Exception error){leveKeys=null;leveNames=null;log.Warning(error,"The leve names could not be read.");}
        this.framework=framework;this.client=client;this.objects=objects;this.log=log;this.condition=condition;this.fateTable=fateTable;this.party=party;Log=log;
        uint[] currentIds=[],vistaIds=[];
        // 0.491 (R-14): one guard per sheet, so that one sheet does not cost the other.
        try{currentIds=data.GetExcelSheet<AetherCurrent>().Select(row=>row.RowId).ToArray();}
        catch(Exception error){log.Warning(error,"Aether current rows unavailable.");}
        try{vistaIds=data.GetExcelSheet<Adventure>().Select(row=>row.RowId).ToArray();}
        catch(Exception error){log.Warning(error,"Sightseeing rows unavailable.");}
        allCurrents=currentIds;adventureRows=vistaIds;
        // R-14 (0.507): one guard per sheet: a failing sheet no longer silently takes the others, hunt marks among them.
        try
        {
            // 0.586 (user report): each zone names its set in TerritoryType. All the old zones share set 19, which itself only names Mor Dhona (docs/evidence/0.586-flight-sets.md).
            var sets=new Dictionary<uint,uint[]>();
            foreach(var set in data.GetExcelSheet<AetherCurrentCompFlgSet>())
            {
                var ids=set.AetherCurrents.Select(current=>current.RowId).Where(id=>id!=0).ToArray();
                sets[set.RowId]=ids;
                if(set.Territory.RowId!=0)currents.TryAdd(set.Territory.RowId,(set.RowId,ids));
            }
            foreach(var territory in data.GetExcelSheet<TerritoryType>())
            {
                mounts[territory.RowId]=territory.Mount;
                var linked=territory.AetherCurrentCompFlgSet.RowId;
                if(linked!=0 && sets.TryGetValue(linked,out var shared) && shared.Length>0)currents.TryAdd(territory.RowId,(linked,shared));
            }
        }
        catch(Exception error){log.Warning(error,"Aether current data unavailable; flight state stays unknown.");}
        try
        {
            foreach(var point in data.GetExcelSheet<GatheringPoint>())
                if(point.GatheringPointBase.RowId!=0 && point.GatheringPointBase.IsValid)gatheringLevels[point.RowId]=point.GatheringPointBase.Value.GatheringLevel;
        }
        catch(Exception error){log.Warning(error,"Gathering point levels unavailable.");}
        try
        {
            foreach(var mark in data.GetExcelSheet<NotoriousMonster>())
                if(mark.BNpcBase.RowId!=0 && mark.Rank is >= 1 and <= 3)huntMarks.TryAdd(mark.BNpcBase.RowId,mark.Rank);
        }
        catch(Exception error){log.Warning(error,"Hunt mark data unavailable: marks are not shown on the map.");}
        try
        {
            foreach(var animal in data.GetExcelSheet<Lumina.Excel.Sheets.MJIAnimals>())
                if(animal.BNpcBase.RowId!=0 && animal.Icon>0)islandAnimals.TryAdd(animal.BNpcBase.RowId,(uint)animal.Icon);
        }
        catch(Exception error){log.Warning(error,"Island animal data unavailable: they are not shown on the map.");}
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
    }
    private bool teleportFailed;
    private DateTimeOffset partyAt;
    private void Update(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
        try
        {
            if(Interlocked.Exchange(ref removeFlag,0)==1)ClearFlag();
            // R-14 (0.507): a failed teleport no longer blanks the map for five seconds.
            if(Interlocked.Exchange(ref teleport,0) is var destination && destination>0)
                try{SendTeleport((uint)destination);}
                catch(Exception error){if(!teleportFailed){teleportFailed=true;log.Warning(error,"The teleport could not be sent.");}}
            var player=client.IsLoggedIn ? objects.LocalPlayer:null;
            // 0.662: called only with the player there for a moment: see TeleportListGate.
            var listAllowed=teleportGate.Allowed(player is not null,condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51],DateTimeOffset.UtcNow);
            if(listAllowed && Interlocked.Exchange(ref costsRequest,0)==1){try{Volatile.Write(ref teleportCosts,ReadTeleportCosts());costsFailed=false;}catch(Exception error){costsFailed=true;Failed("teleport costs",error);}}
            if(player is null){Volatile.Write(ref state,null);return;}
            var (flagMap,flag)=ReadFlag();
            // 0.328 (R-14): each secondary read (here and the teleport costs above) has its own guard; a failure keeps that part's
            // last copy and no longer removes the position or the flag. « flights is null || » was dropped from the test: with a
            // failing read it retried every frame; the first tick still reads, flightAt starts at its default.
            if(now>=flightAt)
            {
                try{flights=ReadFlights();Read("flights");}catch(Exception error){Failed("flights",error);}
                try{(unlockedCurrents,sightseen)=ReadCollected();Read("collected");}catch(Exception error){Failed("collected",error);}
                try{var wanted=(uint)Volatile.Read(ref discoveryWanted);undiscovered=wanted==0 ? null:ReadUndiscovered(wanted);discoveryMap=wanted;Read("discovery");}
                catch(Exception error){undiscovered=null;Failed("discovery",error);}
                flightAt=now.AddSeconds(2);
            }
            if(now>=eventsAt)
            {
                try{events=ReadEvents();Read("events");}catch(Exception error){Failed("events",error);}
                // Objects, FATEs and party carry no map id: on a failure they are emptied, never kept from another zone.
                try{nearby=ReadObjects();Read("objects");}catch(Exception error){nearby=[];Failed("objects",error);}
                try{questPoints=ReadQuestPoints();Read("quest points");}catch(Exception error){questPoints=null;Failed("quest points",error);}
                try{fates=ReadFates();Read("FATEs");}catch(Exception error){fates=[];Failed("FATEs",error);}
                eventsAt=now.AddMilliseconds(500);
            }
            // 0.525 (user report): the party is read more often than the rest; the map glides between positions (MapPartySmoother).
            if(now>=partyAt)
            {
                try{partyMarkers=ReadParty();Read("party");}catch(Exception error){partyMarkers=[];Failed("party",error);}
                // 0.799 (X-38): monsters move; their icons above the head follow at this pace.
                try{monsters=ReadMonsters();Read("monsters");}catch(Exception error){monsters=[];Failed("monsters",error);}
                partyAt=now.AddMilliseconds(100);
            }
            try{ReadPlace();Read("place");}catch(Exception error){areaName=subAreaName="";Failed("place",error);}
            var inCombat=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            var inDuty=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56];
            Volatile.Write(ref state,new MapLiveState(client.TerritoryType,client.MapId,player.Position,player.Rotation,now,flagMap,flag,flights,events,inCombat,inDuty,nearby,fates,partyMarkers,unlockedCurrents,sightseen,logUnlock,logUnlockEx,areaName,subAreaName,monsters,discoveryMap,undiscovered,questPoints));
        }
        catch(Exception error){Volatile.Write(ref state,null);retryAt=now.AddSeconds(5);log.Warning(error,"Map position unavailable; retry in five seconds.");}
    }
    // Parts whose read failed, logged once each (the reads repeat every half second or two seconds).
    private readonly HashSet<string> failedReads=[];
    private void Failed(string part,Exception error)
    {
        if(failedReads.Add(part)){log.Warning(error,"Map {0} unavailable; the last copy is kept, position and flag still update.",part);Volatile.Write(ref failedParts,failedReads.ToArray());}
    }
    // 0.569 (P15): a part that reads again is no longer said to be down.
    private void Read(string part)
    {
        if(failedReads.Remove(part))Volatile.Write(ref failedParts,failedReads.ToArray());
    }
    private string[] failedParts=[];
    /// <summary>The parts whose last read failed, for the map's line and Diagnostics.</summary>
    public IReadOnlyList<string> FailedParts=>Volatile.Read(ref failedParts);
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
    // 0.795 (X-34, user request): the living enemies streamed around the player, for information layers only (what drops, what is missing, what teaches):
    // name, place and level, nothing about the fight. At most 100.
    // 0.797 (X-37): with the FATE it belongs to and its event, read from the game object (FFXIVClientStructs GameObject.FateId, EventId.Id).
    private unsafe IReadOnlyList<MapMonster> ReadMonsters()
    {
        var result=new List<MapMonster>();
        foreach(var item in objects)
        {
            if(result.Count>=100)break;
            if(item is not Dalamud.Game.ClientState.Objects.Types.IBattleNpc { CurrentHp:>0, NameId:not 0 } npc || npc.BattleNpcKind!=Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Combatant || !npc.IsTargetable)continue;
            var native=(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)npc.Address;
            // 0.799 (X-38): its height, for an icon above its head (a field, no game function called).
            var height=native==null || !float.IsFinite(native->Height) ? 0:Math.Clamp(native->Height,0,50);
            result.Add(new(npc.NameId,npc.BaseId,npc.Name.TextValue,npc.Position,npc.Level,native==null ? 0u:native->FateId,native==null ? 0u:native->EventId.Id,height));
        }
        return result.AsReadOnly();
    }
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
                Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc when huntMarks.ContainsKey(item.BaseId)
                    && item is Dalamud.Game.ClientState.Objects.Types.IBattleNpc { CurrentHp:>0 }=>MapObjectKind.HuntMark,
                Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc when islandAnimals.ContainsKey(item.BaseId)
                    && item is Dalamud.Game.ClientState.Objects.Types.IBattleNpc { CurrentHp:>0 }=>MapObjectKind.IslandAnimal,
                _=>(MapObjectKind?)null,
            };
            if(kind is null || !item.IsTargetable)continue;
            // 0.856 (X-72 C): a hunt mark keeps its name and base rows, for the discoveries base.
            result.Add(new(kind.Value,item.Name.TextValue,item.Position,kind==MapObjectKind.Gathering ? gatheringLevels.GetValueOrDefault(item.BaseId)
                :kind==MapObjectKind.HuntMark ? huntMarks.GetValueOrDefault(item.BaseId):0,
                kind==MapObjectKind.HuntMark && item is Dalamud.Game.ClientState.Objects.Types.IBattleNpc mark ? mark.NameId:0,kind==MapObjectKind.HuntMark ? item.BaseId:0,
                kind==MapObjectKind.IslandAnimal ? islandAnimals.GetValueOrDefault(item.BaseId):0));
        }
        return result.AsReadOnly();
    }
    // Unlocked aether currents and discovered vistas, copied as sets. Sightseeing index = Adventure row − first row
    // (rows are contiguous from 0x210000 in the probe); to confirm in game against the sightseeing log.
    // 0.491 (R-14): null = unknown ([?] on the map): no player state, a sheet not read, or more vistas than the game's bit array. Never an empty set, which reads as « nothing collected ».
    private unsafe (IReadOnlySet<uint>?,IReadOnlySet<uint>?) ReadCollected()
    {
        var player=PlayerState.Instance();
        if(player==null)return (null,null);
        HashSet<uint>? unlocked=null;
        if(allCurrents.Length>0){unlocked=[];foreach(var id in allCurrents)if(player->IsAetherCurrentUnlocked(id))unlocked.Add(id);}
        // Discovered vistas: the raw bit array (bit i = Adventure row i, ClientStructs: bitCount = Adventure row count) is used;
        // the native IsAdventureComplete is only compared and logged (0.128: the user saw completed vistas not hidden).
        var seen=new HashSet<uint>();var native=0;
        // ClientStructs: 0 = log not unlocked, 1 = ARR part 1, 2 = ARR part 2; Ex: 3 = « Sights of the North » done.
        logUnlock=player->SightseeingLogUnlockState;logUnlockEx=player->SightseeingLogUnlockStateEx;
        var bits=player->CompletedAdventures;
        if(adventureRows.Length==0 || adventureRows.Length>bits.Length*8)return (unlocked,null);
        for(var index=0;index<adventureRows.Length;index++)
        {
            if((bits[index>>3]>>(index&7)&1)!=0)seen.Add(adventureRows[index]);
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
            // A member in view has an exact position; the party list's comes from the server, from time to time.
            result.Add(new(member.Name.TextValue,member.ClassJob.RowId,member.Level,member.GameObject?.Position ?? member.Position));
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
                float.IsFinite(fate.Radius) ? Math.Max(0,fate.Radius):0,fate.Progress,Math.Max(0,fate.TimeRemaining),fate.State.ToString(),fate.HasBonus));
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
        // 0.453 (R-14): unreadable throws (TeleportCostsFailed), an empty list read « none attuned ».
        if(telepo==null)throw new InvalidOperationException("Teleport module unavailable.");
        var list=telepo->UpdateAetheryteList();
        // 0.328 (R-14): length checked before the walk; a shifted layout would read past valid memory (not catchable).
        if(list==null || list->LongCount is <0 or >512)throw new InvalidOperationException("Teleport list unreadable.");
        foreach(var entry in *list)
            if(entry.AetheryteId!=0 && entry.SubIndex==0 && entry.Ward==0 && entry.Plot==0)result.TryAdd(entry.AetheryteId,entry.GilCost);
        return result;
    }
    private static readonly HashSet<uint> loggedEventIcons=[];
    // P17 (0.652): the names of the Leve sheet, in the game's language, and what was already decided for a tooltip.
    private static IReadOnlySet<string>? leveNames;
    private static IReadOnlyDictionary<uint,string>? leveKeys;
    private static readonly LeveAreaMemory<MapEventMarker> leveAreas=new();
    /// <summary>0.657 (user report): the journal's click gives the leve's area, from anywhere (docs/evidence/0.657-leve-area-from-journal.md).</summary>
    internal static void SeedLeveArea(uint map,string leve,string title,Vector2 at,int radius,uint icon)=>leveAreas.Seed(map,leve,new MapEventMarker(map,icon,new Vector3(at.X,0,at.Y),radius,title,IsLeve:true));
    /// <summary>0.655: the accepted leves, as read with the markers (null = not read).</summary>
    internal static IReadOnlySet<string>? AcceptedLeves { get; private set; }
    /// <summary>0.655: the key of a leve's name, or null when this text names none.</summary>
    internal static string? LeveKeyOf(string title)=>MapLeves.Key(title) is var key && leveNames?.Contains(key)==true ? key:null;
    /// <summary>0.654: the areas of the leves still accepted, remembered; those of a leve no longer accepted go. Unknown (no reader): the markers as read.</summary>
    private static unsafe IReadOnlyList<MapEventMarker> WithLeveAreas(IReadOnlyList<MapEventMarker> markers)
    {
        var quests=FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance();
        if(quests==null || leveKeys is null || leveNames is null)return markers;
        var accepted=new HashSet<string>();
        foreach(ref var leve in quests->LeveQuests)if(leve.LeveId!=0 && leveKeys.TryGetValue(leve.LeveId,out var name))accepted.Add(name);
        AcceptedLeves=accepted;
        var named=markers.Where(marker=>marker.IsLeve).Select(marker=>(Marker:marker,Key:MapLeves.Key(marker.Tooltip))).Where(entry=>leveNames.Contains(entry.Key)).ToArray();
        var extras=leveAreas.Update(named.Select(entry=>(entry.Marker.MapId,entry.Key,entry.Marker)),accepted);
        var gone=named.Where(entry=>!accepted.Contains(entry.Key)).Select(entry=>entry.Marker).ToHashSet();
        return extras.Count==0 && gone.Count==0 ? markers:[..markers.Where(marker=>!gone.Contains(marker)),..extras];
    }
    private static readonly Dictionary<(uint Icon,string Tooltip),bool> leveMarkers=[];
    private static bool IsLeve(uint icon,byte type,string tooltip)
    {
        if(leveMarkers.TryGetValue((icon,tooltip),out var known))return known;
        var leve=MapLeves.Is(icon,type,tooltip,leveNames);
        if(leveMarkers.Count>=512)leveMarkers.Clear();
        leveMarkers[(icon,tooltip)]=leve;
        return leve;
    }
    private static IPluginLog? Log;
    // Quest and event markers of the game map, copied (strings included); at most 256 to bound the work per refresh.
    private static unsafe IReadOnlyList<MapEventMarker> ReadEvents()
    {
        var agent=AgentMap.Instance();
        if(agent==null)return [];
        // 0.328 (R-14): after a game update First/Last can be garbage and an access violation is not catchable: the vector
        // length is checked first, and every visited entry counts (skipped ones too), not only the kept ones.
        var count=agent->EventMarkers.LongCount;
        if(count is <0 or >1024)return [];
        var result=new List<MapEventMarker>();var visited=0;
        foreach(var marker in agent->EventMarkers)
        {
            if(++visited>1024 || result.Count>=256)break;
            if(marker.IconId==0 || marker.MapId==0 || !float.IsFinite(marker.Position.X) || !float.IsFinite(marker.Position.Z))continue;
            var tooltip=TooltipText(marker.TooltipString);
            // Evidence for the labels (0.134): once per icon, what the game gives (objective, type, readable tooltip).
            if(loggedEventIcons.Count<64 && loggedEventIcons.Add(marker.IconId))
                Log?.Information("Map event marker icon {0} on map "+marker.MapId+": objective {1}, type {2}, data {3}, tooltip {4} (length {5}, used {6}, size {7}), radius {8}, text \"{9}\".",marker.IconId,marker.ObjectiveId,marker.MarkerType,marker.DataId,
                    tooltip.Length>0 ? "read":"unread",marker.TooltipString==null ? -1:marker.TooltipString->StringLength,marker.TooltipString==null ? -1:marker.TooltipString->BufUsed,marker.TooltipString==null ? -1:marker.TooltipString->BufSize,marker.Radius,tooltip);
            result.Add(new(marker.MapId,marker.IconId,marker.Position,float.IsFinite(marker.Radius) ? Math.Max(0,marker.Radius):0,tooltip,marker.ObjectiveId,marker.MarkerType,marker.DataId,IsLeve(marker.IconId,marker.MarkerType,tooltip)));
        }
        // 0.653: the game empties this list during a journal request; what was read just before stays.
        var journalRequest=agent->CurrentOpenMapInfo.Type is FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType.QuestLog or FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType.GatheringLog;
        if(!MapEventHold.Holds(result.Count,journalRequest,agent->CurrentMapId,lastEventsMap)){lastEvents=result.AsReadOnly();lastEventsMap=agent->CurrentMapId;}
        // 0.848 (X-70): the game's quest list, current at each step, replaces the window's markers of the same quests.
        var current=MapQuestEvents.Merge(lastEvents,ReadGameQuests(),marker=>(marker.MarkerType==3,marker.ObjectiveId & 0xFFFF));
        return WithLeveAreas(MapGameLeves.Merge(current,ReadGameLeves(),marker=>(marker.MapId,marker.Icon,marker.Position.X,marker.Position.Z)));
    }
    /// <summary>0.848 (X-70): the markers of the quests the game renders, from its own Map (kept current with or without its map window); bounded.</summary>
    private static unsafe IReadOnlyList<MapEventMarker> ReadGameQuests()
    {
        var map=FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance();
        if(map==null)return [];
        var result=new List<MapEventMarker>();
        var quests=map->QuestMarkers;
        for(var index=0;index<quests.Length && result.Count<128;index++)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&quests[index])
            {
                if(info->ObjectiveId==0 || !info->ShouldRender)continue;
                var count=info->MarkerData.LongCount;
                if(count is <=0 or >32)continue;
                var label=TooltipText(&info->Label);
                foreach(var data in info->MarkerData)
                {
                    if(data.IconId==0 || data.MapId==0 || !float.IsFinite(data.Position.X) || !float.IsFinite(data.Position.Z) || result.Count>=128)continue;
                    var text=label.Length>0 ? label:TooltipText(data.TooltipString);
                    result.Add(new(data.MapId,data.IconId,data.Position,float.IsFinite(data.Radius) ? Math.Max(0,data.Radius):0,text,info->ObjectiveId,3,data.DataId,false));
                }
            }
        }
        return result;
    }
    // 0.813 (X-49): the points of the accepted quests, from the game's Map (docs/evidence/0.793-game-leve-markers.md shows its quest list); bounded.
    private static unsafe IReadOnlyList<MapQuestPoint>? ReadQuestPoints()
    {
        var map=FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance();
        if(map==null)return null;
        var result=new List<MapQuestPoint>();
        var quests=map->QuestMarkers;
        for(var index=0;index<quests.Length;index++)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&quests[index])
            {
                if(info->ObjectiveId==0)continue;
                var count=info->MarkerData.LongCount;
                if(count is <=0 or >32)continue;
                foreach(var data in info->MarkerData)
                    if(data.MapId!=0 && float.IsFinite(data.Position.X) && float.IsFinite(data.Position.Z) && result.Count<256)
                        result.Add(new(info->ObjectiveId,data.MapId,data.Position.X,data.Position.Z));
            }
        }
        return result;
    }
    // 0.793 (X-32): the leve areas the game keeps itself, with or without its map open: each accepted leve's area, and the points of the one started.
    private static unsafe IReadOnlyList<MapEventMarker> ReadGameLeves()
    {
        var map=FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance();
        if(map==null)return [];
        var result=new List<MapEventMarker>();
        void From(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info)
        {
            // Length checked before the walk, as for the map window's list (R-14).
            var count=info->MarkerData.LongCount;
            if(count is <=0 or >32)return;
            var label=TooltipText(&info->Label);
            foreach(var data in info->MarkerData)
            {
                if(result.Count>=64)return;
                if(data.IconId==0 || data.MapId==0 || !float.IsFinite(data.Position.X) || !float.IsFinite(data.Position.Z))continue;
                var text=label.Length>0 ? label:TooltipText(data.TooltipString);
                result.Add(new(data.MapId,data.IconId,data.Position,float.IsFinite(data.Radius) ? Math.Max(0,data.Radius):0,text,data.ObjectiveId,data.MarkerType,data.DataId,true));
            }
        }
        var leves=map->LevequestMarkers;
        for(var index=0;index<leves.Length;index++)fixed(FFXIVClientStructs.FFXIV.Client.Game.UI.MarkerInfo* info=&leves[index])From(info);
        From(&map->ActiveLevequestMarker);
        return result;
    }
    private static IReadOnlyList<MapEventMarker> lastEvents=[];private static uint lastEventsMap;
    // Crash 2026-10-31 08:19 (dalamud.crashhandler.log): Utf8String.ToString() on an event marker tooltip read freed memory
    // with an unbounded length. Only a short string (1–256 bytes, within its buffer) is copied, with its own length; anything
    // else gives "". 0.136: the length is BufUsed − 1, as Utf8String.ToString uses it (StringLength reads 0 in game).
    internal static unsafe string TooltipText(FFXIVClientStructs.FFXIV.Client.System.String.Utf8String* text)
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
    public void Dispose(){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref state,null);}
}
