using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenshoAssistant.Discoveries;

/// <summary>X-43 (0.807): what a fact is about. Numbers are kept in files: never reuse one.</summary>
// X-72 C (0.856, user choice « ABC »): hunt marks' spawn places (B, A and S ranks: in no game sheet, only in community data) and the FATEs seen (place, level,
// when). HuntMarkPlace: Subject = BNpcName row, Detail = BNpcBase row, levels = the rank (1 B, 2 A, 3 S). FateSeen: Subject = Fate row, levels = the FATE's level.
// X-74 (0.860, the night's parsers, ItemDiscoveryWatcher): where items come from. Subject = the item got; Detail = its source (source item, RetainerTask row, CFC row,
// Fate row, BNpcName row, bait item, weather row); levels = the quantities seen (FishCondition: the Eorzea minutes seen). Places only for MonsterDrop and FishCatch.
public enum DiscoveryKind { MonsterPlace=1, HuntMarkPlace=2, FateSeen=3, Desynthesis=4, Venture=5, DutyChest=6, FateReward=7, MonsterDrop=8, ItemFromItem=9, FishCatch=10, FishCondition=11,
    // X-73.4 (0.861): the Fashion Report's judgement: Subject = the item worn, Detail = the slot's category row, Territory = the week's theme row, levels = the raw
    // evaluation byte (its meaning is not proved yet: kept raw, read later).
    FashionEvaluation=12,
    // X-73.8 (0.862): submersible voyages: VoyageDrop: Subject = item, Detail = SubmarineExploration row (sector), levels = quantities; VoyageUnlock: Subject = the
    // sector unlocked, Detail = the sector whose voyage unlocked it.
    VoyageDrop=13, VoyageUnlock=14,
    // X-73.9 (0.863): BlueSpellLearned: Subject = AozAction row, Detail = the BNpcName it is put down to (0: none), with its place when known; BeastTamed: Subject = the
    // Beastmaster's beast id, the same Detail and place.
    BlueSpellLearned=15, BeastTamed=16,
    // X-73.12 (0.864): FestivalSeen: Subject = Festival row, Detail = phase, levels = the first and last days seen (days since 2013-08-27); VistaSeen: Subject = the
    // vista's row (as the map reads it), Detail = the weather when registered, levels = the Eorzea minutes.
    FestivalSeen=17, VistaSeen=18,
    // X-73.16 (0.872): NpcPlace: Subject = the ENpc (ENpcResident row) whose shop was opened, with its place; Detail 0. For the vendors no game sheet places.
    NpcPlace=19,
    // X-75.3 (0.877): FishLure: Subject = fish, Detail = 1 Ambitious / 2 Modest lure on the cast that caught it; FishTransition: Subject = fish, Detail = the
    // weather of the previous period (the game's forecast 1400 s earlier), levels = Eorzea minutes. Seen with, never « required ».
    FishLure=20, FishTransition=21,
    // X-75.4 (0.878): BlueSpellCaster: Subject = AozAction row, Detail = the BNpcName seen casting its action (cast bar), with its place, levels = the caster's level.
    // « Seen casting », not « teaches »: some casters do not teach it.
    BlueSpellCaster=22,
    // X-73.18 (0.887, docs/evidence/0.887-fishing-bite.md): FishBite: Subject = fish, Detail = bite strength (1 !, 2 !!, 3 !!!, 0 unknown), place = where it was
    // caught, levels = the bite time in tenths of a second from the cast (casts under Chum are left out).
    FishBite=23,
    // X-73.18 (0.888, docs/evidence/0.888-aetherial-reduction.md): Reduction: Subject = item obtained, Detail = the collectable reduced, levels = quantities.
    Reduction=24,
    // X-73.18 (0.889, docs/evidence/0.889-hand-in.md): QuestItemAsked: Subject = item handed in, Detail = the Quest row whose step changed just after, levels =
    // the quantities asked. 0.911 (X-76): an HQ demand is kept under the item's HQ id (Subject = 1 000 000 + item, the game's own convention, QuestItemAsks), so
    // it is its own fact; facts written before 0.911 hold the plain item, HQ or not. A key item (EventItem row, 2 000 000 and up) is kept as its own id.
    QuestItemAsked=25,
    // 26 was 0.890's spearfishing shadow, taken from the shadows left in sight: wrong (docs/evidence/0.891-spearfishing-slots.md); its facts are refused when read.
    // 27 was 0.891's: the item was read a moment too early (it lags the catch count) and the speed could be the doubled one (docs/evidence/0.896-spearfishing-lag.md);
    // its facts are refused when read.
    // X-73.18 (0.896): SpearShadow: Subject = fish speared, Detail = the shadow's size (1 small, 2 average, 3 large), levels = its speed when it appeared, place =
    // where it was speared. The shadow is the slot that left the window with its size and speed kept, just before the catch.
    SpearShadow=28,
    // X-76 P6 (0.933): FishBiteBait: Subject = fish, Detail = the bait of the cast (the fish before for a mooch), levels = the bite time in tenths of a second, place =
    // where it was caught (the same cast as FishBite, keyed by bait). IntuitionStart: Subject = the fish caught just before Fisher's Intuition (status 568) began
    // (the catch that started it: a predator candidate), levels = the intuition's length in seconds, place. IntuitionCatch: Subject = a fish caught while the
    // intuition lasted, Detail = the fish that started it (0 unknown). Seen with, not proven to be required.
    FishBiteBait=29, IntuitionStart=30, IntuitionCatch=31,
    // X-76 P7 (0.934): VoyageVisit: Subject = the sector (SubmarineExploration row) a submersible explored, Detail = 1 when the game gave a second find there
    // (DoubleDip), else 0, levels = the vessel's surveillance at that voyage. One per sector of a voyage: the count of visits behind the drop rates (P8).
    VoyageVisit=32,
    // X-76 P8 (0.935): the counts behind a drop rate. MonsterKill: Subject = BNpcName of a targeted enemy seen dying (as MonsterDrop credits its drops), levels = its
    // level. DutyChestOpened: Subject = the duty (ContentFinderCondition row) whose loot window listed a new chest (ChestObjectId; whether that id stays the same
    // for one chest is not proven), counted once per chest.
    MonsterKill=33, DutyChestOpened=34,
    // X-85 V4 (0.962): SpectralBefore: Subject = the fish you caught last before a spectral current began at an ocean stop (within 90 s), Detail = the bait of
    // that cast (or the fish mooched), territory = the voyage's. Seen with, not proven to trigger it: another player's catch can start the current.
    SpectralBefore=35,
    // X-85 V2 (0.964): FishPoints: Subject = an ocean fish, levels = the points one catch of it brought (the stop's FishData points grew by that much; bonuses
    // of the moment included), territory = the voyage's.
    FishPoints=36,
    // X-73 (0.1003, user request « the parsers we lack to complete what we still take from the community »): AirshipDrop: Subject = an item an airship
    // brought back, Detail = the AirshipExplorationPoint row (sector) it came from, levels = the quantity. The sector comes from the voyage log's route
    // (T26), paired with the stops in order: noted only with a single airship and as many stops with loot as sectors in the route.
    AirshipDrop=37 }

/// <summary>
/// X-43 (0.807, user request): one thing seen in game, kept locally, with where it came from: seen by the player (Seen, how many separate times), imported
/// (Imported, how many files had it). Only game rows and places: never a player's name, position or id.
/// MonsterPlace: Subject = BNpcName row, Detail = BNpcBase row, the territory and map, the 50-yalm cell (CellSize), the first exact place, the levels seen.
/// </summary>
public sealed class DiscoveryFact
{
    public DiscoveryKind Kind { get; set; }
    public uint Subject { get; set; }
    public uint Detail { get; set; }
    public uint Territory { get; set; }
    public uint Map { get; set; }
    public int CellX { get; set; }
    public int CellZ { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    public int Seen { get; set; }
    public int Imported { get; set; }
    public DateTimeOffset First { get; set; }
    public DateTimeOffset Last { get; set; }
    // X-76 E1 (0.916, DEC-042, file version 2, docs/evidence/0.916-discoveries-format.md): optional, left out of the file when empty, so that a version 1 file
    // reads as it is. Like Seen and Imported, yours and imported ones are kept apart: only yours are shared (CommunityDiscoveries.Mine), never echoed back.
    /// <summary>How many of your sightings were HQ (an item got: catches, drops, rewards, chests); never more than the sightings counted in <see cref="Values"/>.</summary>
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)]
    public int Hq { get; set; }
    /// <summary>The value seen at each of your sightings (level, quantity, bite time…): value → count, at most <see cref="ValueCounts.MaxValues"/> values.</summary>
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<int,int>? Values { get; set; }
    /// <summary>The imported HQ sightings: the largest number an imported file gave (a file read again is not counted twice).</summary>
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)]
    public int ImportedHq { get; set; }
    /// <summary>The imported values: per value, the largest count an imported file gave.</summary>
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<int,int>? ImportedValues { get; set; }
    public string Key=>$"{(int)Kind}:{Subject}:{Detail}:{Territory}:{Map}:{CellX}:{CellZ}";
    public DiscoveryFact Copy()
    {
        var copy=(DiscoveryFact)MemberwiseClone();
        copy.Values=Values is null ? null:new(Values);copy.ImportedValues=ImportedValues is null ? null:new(ImportedValues);
        return copy;
    }
    /// <summary>X-76 (0.908): how many times a fact was seen in game, yours and imported together (static: nothing more is written in the files).</summary>
    public static int TimesOf(DiscoveryFact fact)=>fact.Seen+fact.Imported;
}

/// <summary>
/// X-76 E1 (0.916): a bounded histogram of the values seen (value → count). A new value when <see cref="MaxValues"/> are already kept is counted under the
/// nearest value kept (the lower one on a tie): no sighting is lost, the shape stays close. Counts stop at <see cref="MaxCount"/>.
/// </summary>
public static class ValueCounts
{
    public const int MaxValues=32,MaxValue=100_000,MaxCount=100_000_000;

    /// <summary>Count added under the value, or under the nearest value kept when the histogram is full.</summary>
    public static void Add(ref Dictionary<int,int>? values,int value,int count=1)
    {
        if(count<=0)return;
        values??=[];
        var key=values.ContainsKey(value) || values.Count<MaxValues ? value:Nearest(values,value);
        values[key]=(int)Math.Min((long)values.GetValueOrDefault(key)+count,MaxCount);
    }

    /// <summary>The value kept closest to this one, the lower one on a tie.</summary>
    public static int Nearest(IReadOnlyDictionary<int,int> values,int value)=>
        values.Keys.OrderBy(key=>Math.Abs((long)key-value)).ThenBy(key=>key).First();

    /// <summary>Both histograms added (values of the second taken in increasing order).</summary>
    public static Dictionary<int,int>? Sum(Dictionary<int,int>? first,Dictionary<int,int>? second)
    {
        var result=first is null ? null:new Dictionary<int,int>(first);
        if(second is not null)foreach(var (value,count) in second.OrderBy(pair=>pair.Key))Add(ref result,value,count);
        return result;
    }

    /// <summary>Per value, the largest count of the two (an imported file read again adds nothing).</summary>
    public static Dictionary<int,int>? Max(Dictionary<int,int>? first,Dictionary<int,int>? second)
    {
        var result=first is null ? null:new Dictionary<int,int>(first);
        if(second is null)return result;
        foreach(var (value,count) in second.OrderBy(pair=>pair.Key))
        {
            if(count<=0)continue;
            result??=[];
            var key=result.ContainsKey(value) || result.Count<MaxValues ? value:Nearest(result,value);
            result[key]=Math.Max(result.GetValueOrDefault(key),Math.Min(count,MaxCount));
        }
        return result;
    }

    public static long Total(IReadOnlyDictionary<int,int>? values)=>values?.Values.Sum(count=>(long)count) ?? 0;
    /// <summary>An HQ count held within the sightings of its histogram (a count stopped at <see cref="MaxCount"/> or folded by a largest-count join can be lower).</summary>
    public static int AtMostTotal(long hq,IReadOnlyDictionary<int,int>? values)=>(int)Math.Clamp(hq,0,Math.Min(Total(values),int.MaxValue));

    /// <summary>What a file may hold: at most <see cref="MaxValues"/> values from 0 to <see cref="MaxValue"/>, each counted 1 to <see cref="MaxCount"/> times.</summary>
    public static bool Valid(IReadOnlyDictionary<int,int>? values)=>values is null
        || values.Count<=MaxValues && values.All(pair=>pair.Key is >= 0 and <= MaxValue && pair.Value is >= 1 and <= MaxCount);
}

/// <summary>
/// The file: a version, who wrote it, when, and the facts. X-76 E1 (0.916): version 2 adds the optional Hq, Values, ImportedHq and ImportedValues of a fact;
/// version 1 files, which have none of them, are still read as they are.
/// </summary>
public sealed class DiscoveryFile
{
    public const int CurrentVersion=2,OldestVersion=1;
    /// <summary>A file this version reads: a known version and a list of facts.</summary>
    public static bool Readable(DiscoveryFile? file)=>file is { Version:>=OldestVersion and <=CurrentVersion, Facts:not null };
    public int Version { get; set; }=CurrentVersion;
    public string Producer { get; set; }="Tensho Assistant";
    public DateTimeOffset Written { get; set; }
    public List<DiscoveryFact> Facts { get; set; }=[];
}

/// <summary>X-43 (0.807): the local base of discoveries; every method is safe from any thread.</summary>
public sealed class DiscoveryStore
{
    // X-72 B (0.855, user choice after the parser review: 87 % of the 10-yalm places seen once, 640 facts instead of 1 469 at 50 yalms): 50-yalm cells; facts of
    // older cells are moved to theirs and joined when loaded or imported (Recell, Join).
    public const float CellSize=50;
    /// <summary>
    /// X-93 (0.939, user: never forget a discovery): the bound applies to the shared facts held in memory only (imported, not seen by you), per kind since 0.943. Your own sightings are
    /// never refused, and nothing already held is ever removed to make room; a shared fact left out is counted (<see cref="LeftOut"/>) and the page says so.
    /// </summary>
    public const int MaxFacts=200_000;
    /// <summary>
    /// X-93 (0.941): shared kinds loaded on demand. With a source set (<see cref="UseSharedSource"/>), a kind whose shared file exists is read the first time
    /// something asks for it (a search in the background: the answer comes a moment later, the page refreshes on the next revision; an import at once), so
    /// the shared facts of the kinds no page uses never take memory. Without a source (tests, tools), everything is held as before.
    /// </summary>
    private Func<DiscoveryKind,DiscoveryFile?>? sharedSource;
    private readonly HashSet<DiscoveryKind> sharedWaiting=[],sharedLoading=[];
    public void UseSharedSource(Func<DiscoveryKind,DiscoveryFile?> source,IEnumerable<DiscoveryKind> onDisk)
    {
        lock(gate){sharedSource=source;sharedWaiting.Clear();sharedWaiting.UnionWith(onDisk);}
    }
    /// <summary>The kinds whose shared facts are all in memory (their file may be written and removed); the others are left alone on disk.</summary>
    public IReadOnlySet<DiscoveryKind> SharedKindsHeld()
    {
        lock(gate)return Enum.GetValues<DiscoveryKind>().Where(kind=>!sharedWaiting.Contains(kind)).ToHashSet();
    }
    public int SharedKindsWaiting{get{lock(gate)return sharedWaiting.Count;}}
    /// <summary>X-93 (0.942): the Discoveries page shows every kind's totals: it asks for all the shared kinds still on disk.</summary>
    public void WantAll(){lock(gate)foreach(var kind in sharedWaiting.ToArray())Want(kind);}
    // Under the lock: asks for a kind in the background.
    private void Want(DiscoveryKind kind)
    {
        if(sharedSource is not { } source || !sharedWaiting.Contains(kind) || !sharedLoading.Add(kind))return;
        _=Task.Run(()=>
        {
            DiscoveryFile? file=null;
            try{file=source(kind);}catch(Exception){file=null;}
            lock(gate){if(file is not null)LoadLocked(file,false);sharedWaiting.Remove(kind);sharedLoading.Remove(kind);revision++;}
        });
    }
    /// <summary>Reads at once the shared kinds still on disk among <paramref name="kinds"/> (before an import joins facts to them); the file read outside the lock.</summary>
    private void NeedNow(IEnumerable<DiscoveryKind> kinds)
    {
        Func<DiscoveryKind,DiscoveryFile?>? source;DiscoveryKind[] wanted;
        lock(gate){source=sharedSource;wanted=kinds.Distinct().Where(kind=>sharedWaiting.Contains(kind)).ToArray();}
        if(source is null)return;
        foreach(var kind in wanted)
        {
            DiscoveryFile? file=null;
            try{file=source(kind);}catch(Exception){file=null;}
            lock(gate){if(sharedWaiting.Contains(kind)){if(file is not null)LoadLocked(file,false);sharedWaiting.Remove(kind);revision++;}}
        }
    }
    /// <summary>X-93 (0.939): shared facts left out since the start because the shared part was full.</summary>
    public int LeftOut { get; private set; }
    // X-93 (0.943, user's choice: « 200 000 par sorte »): the bound counts the shared facts of each kind apart.
    private Dictionary<DiscoveryKind,int> SharedHeld()=>facts.Values.Where(fact=>fact.Seen==0).GroupBy(fact=>fact.Kind).ToDictionary(group=>group.Key,group=>group.Count());
    /// <summary>A second sighting of the same place counts only after this long: standing beside a monster is one sighting.</summary>
    public static readonly TimeSpan SightingGap=TimeSpan.FromMinutes(10);
    private readonly object gate=new();
    private readonly Dictionary<string,DiscoveryFact> facts=new(StringComparer.Ordinal);
    /// <summary>
    /// X-93 (0.938, load test docs/evidence/0.938-discoveries-load.md): the facts by kind and subject, kept with every insertion, so a search reads only its kind
    /// (or its subject) instead of the whole base: a search took about 2 ms at 200 000 facts, and the fishing help makes several per fish on every frame.
    /// </summary>
    private readonly Dictionary<DiscoveryKind,Dictionary<uint,List<DiscoveryFact>>> index=[];
    private void Put(DiscoveryFact fact)
    {
        facts[fact.Key]=fact;
        if(!index.TryGetValue(fact.Kind,out var bySubject))index[fact.Kind]=bySubject=[];
        if(!bySubject.TryGetValue(fact.Subject,out var list))bySubject[fact.Subject]=list=[];
        list.Add(fact);
    }
    private int revision;
    public int Revision=>Volatile.Read(ref revision);
    public bool Dirty { get; private set; }

    public static (int X,int Z) Cell(float x,float z)=>((int)MathF.Floor(x/CellSize),(int)MathF.Floor(z/CellSize));

    /// <summary>A monster seen at a place; true when the place is new for it.</summary>
    public bool SeeMonster(uint nameId,uint baseId,uint territory,uint map,Vector3 position,int level,DateTimeOffset at)
        =>See(DiscoveryKind.MonsterPlace,nameId,baseId,territory,map,position,level,at,SightingGap);
    /// <summary>X-72 C (0.856): a hunt mark (rank 1 B, 2 A, 3 S) seen at a place.</summary>
    public bool SeeHuntMark(uint nameId,uint baseId,uint territory,uint map,Vector3 position,int rank,DateTimeOffset at)
        =>See(DiscoveryKind.HuntMarkPlace,nameId,baseId,territory,map,position,rank,at,SightingGap);
    /// <summary>X-72 C (0.856): a FATE seen running; one more sighting after an hour, longer than any FATE lasts.</summary>
    public static readonly TimeSpan FateGap=TimeSpan.FromHours(1);
    public bool SeeFate(uint fateId,uint territory,uint map,Vector3 position,int level,DateTimeOffset at)
        =>position.X==0 && position.Z==0 ? false:See(DiscoveryKind.FateSeen,fateId,0,territory,map,position,level,at,FateGap);

    /// <summary>
    /// X-74 (0.860): an item got from a source; every event counts once (the watcher sends each once); a place only when a map is given. X-76 E1 (0.916): the
    /// value is also counted in the fact's Values, and « hq » (an HQ item got) in its Hq.
    /// </summary>
    public bool SeeSource(DiscoveryKind kind,uint subject,uint detail,int value,DateTimeOffset at,uint territory=0,uint map=0,Vector3? place=null,bool hq=false)
    {
        var position=map==0 ? Vector3.Zero:place ?? Vector3.Zero;
        if(subject==0 || value<0 || value>ValueCounts.MaxValue || !float.IsFinite(position.X) || !float.IsFinite(position.Z))return false;
        var (cellX,cellZ)=Cell(position.X,position.Z);
        var fact=new DiscoveryFact{Kind=kind,Subject=subject,Detail=detail,Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=position.X,Z=position.Z,
            LevelMin=value,LevelMax=value,Seen=1,First=at,Last=at,Hq=hq ? 1:0,Values=new(){[value]=1}};
        lock(gate)
        {
            if(facts.TryGetValue(fact.Key,out var known))
            {
                known.LevelMin=Math.Min(known.LevelMin,value);known.LevelMax=Math.Max(known.LevelMax,value);known.Seen++;
                var values=known.Values;ValueCounts.Add(ref values,value);known.Values=values;
                if(hq)known.Hq++;
                if(at>known.Last)known.Last=at;
                Dirty=true;revision++;
                return false;
            }
            // X-93 (0.939): your own sighting is never refused.
            Put(fact);Dirty=true;revision++;
            return true;
        }
    }

    /// <summary>
    /// X-76 (0.911): a festival running on a day (days since 2013-08-27): counted once a day, also across plugin reloads: nothing is added when your own fact of
    /// this festival and phase already reaches that day. True when the fact is new.
    /// </summary>
    public bool SeeFestival(uint festival,uint phase,int day,DateTimeOffset at)
    {
        lock(gate)
            if(facts.TryGetValue(new DiscoveryFact{Kind=DiscoveryKind.FestivalSeen,Subject=festival,Detail=phase}.Key,out var known) && known.Seen>0 && known.LevelMax>=day)
                return false;
        return SeeSource(DiscoveryKind.FestivalSeen,festival,phase,day,at);
    }

    private bool See(DiscoveryKind kind,uint subject,uint detail,uint territory,uint map,Vector3 position,int level,DateTimeOffset at,TimeSpan gap)
    {
        if(subject==0 || map==0 || !float.IsFinite(position.X) || !float.IsFinite(position.Z))return false;
        var (cellX,cellZ)=Cell(position.X,position.Z);
        // X-76 E1 (0.916): the level counted in Values at each sighting counted in Seen; 0 (not known) is not counted.
        var counted=level is > 0 and <= ValueCounts.MaxValue;
        var fact=new DiscoveryFact{Kind=kind,Subject=subject,Detail=detail,Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=position.X,Z=position.Z,
            LevelMin=level,LevelMax=level,Seen=1,First=at,Last=at,Values=counted ? new(){[level]=1}:null};
        lock(gate)
        {
            if(facts.TryGetValue(fact.Key,out var known))
            {
                if(level>0){known.LevelMin=known.LevelMin==0 ? level:Math.Min(known.LevelMin,level);known.LevelMax=Math.Max(known.LevelMax,level);}
                if(at-known.Last>=gap)
                {
                    known.Seen++;
                    if(counted){var values=known.Values;ValueCounts.Add(ref values,level);known.Values=values;}
                }
                if(at>known.Last)known.Last=at;
                Dirty=true;
                return false;
            }
            // X-93 (0.939): your own sighting is never refused.
            Put(fact);Dirty=true;revision++;
            return true;
        }
    }

    /// <summary>
    /// The facts of an imported file joined to ours: the same fact counts one more import and keeps the widest dates and levels; a new one comes in as imported only.
    /// A fact that does not hold is left out and counted. Nothing of ours is removed.
    /// </summary>
    public (int Added,int Joined,int Refused) Merge(IEnumerable<DiscoveryFact> imported)
    {
        int added=0,joined=0,refused=0;
        // X-93 (0.941): the kinds of the file are read first, so a fact already shared is joined, never doubled.
        var list=imported as IReadOnlyCollection<DiscoveryFact> ?? imported.ToArray();
        NeedNow(list.Where(fact=>fact is not null).Select(fact=>fact.Kind));
        lock(gate)
        {
            var shared=SharedHeld();
            foreach(var original in list)
            {
                // 0.855: an older file's cells are moved to ours first.
                var source=original is null ? null:Recell(original);
                if(source is null || !Valid(source)){refused++;continue;}
                if(facts.TryGetValue(source.Key,out var known))
                {
                    known.Imported++;
                    if(source.LevelMin>0)known.LevelMin=known.LevelMin==0 ? source.LevelMin:Math.Min(known.LevelMin,source.LevelMin);
                    known.LevelMax=Math.Max(known.LevelMax,source.LevelMax);
                    if(source.First<known.First)known.First=source.First;
                    if(source.Last>known.Last)known.Last=source.Last;
                    // X-76 E1 (0.916): the file's HQ sightings and values (its own and its imported ones) kept apart from yours, the largest per value.
                    var (hq,values)=ImportedPart(source);
                    known.ImportedValues=ValueCounts.Max(known.ImportedValues,values);known.ImportedHq=ValueCounts.AtMostTotal(Math.Max(known.ImportedHq,hq),known.ImportedValues);
                    joined++;
                    continue;
                }
                if(shared.GetValueOrDefault(source.Kind)>=MaxFacts){LeftOut++;continue;}
                var copy=source.Copy();copy.Seen=0;copy.Imported=1;shared[source.Kind]=shared.GetValueOrDefault(source.Kind)+1;
                (copy.ImportedHq,copy.ImportedValues)=ImportedPart(source);copy.Hq=0;copy.Values=null;
                Put(copy);added++;
            }
            if(added+joined>0){Dirty=true;revision++;}
        }
        return (added,joined,refused);
    }

    /// <summary>X-72 B (0.855): a copy in the cell of its exact place (older files used 10-yalm cells).</summary>
    public static DiscoveryFact Recell(DiscoveryFact fact)
    {
        var copy=fact.Copy();
        if(float.IsFinite(copy.X) && float.IsFinite(copy.Z))(copy.CellX,copy.CellZ)=Cell(copy.X,copy.Z);
        return copy;
    }
    /// <summary>X-76 E1 (0.916): what an imported fact brings as imported: its HQ sightings and values, its own and its imported ones together.</summary>
    public static (int Hq,Dictionary<int,int>? Values) ImportedPart(DiscoveryFact source)
    {
        var values=ValueCounts.Sum(source.Values,source.ImportedValues);
        return (ValueCounts.AtMostTotal((long)source.Hq+source.ImportedHq,values),values);
    }

    /// <summary>
    /// X-72 B (0.855): two facts of one cell made one: sightings and imports added, the widest levels and dates, the first exact place kept. X-76 E1 (0.916):
    /// HQ sightings and values added, yours to yours, imported to imported.
    /// </summary>
    public static void Join(DiscoveryFact known,DiscoveryFact other)
    {
        known.Seen+=other.Seen;known.Imported+=other.Imported;
        known.Values=ValueCounts.Sum(known.Values,other.Values);known.ImportedValues=ValueCounts.Sum(known.ImportedValues,other.ImportedValues);
        known.Hq=ValueCounts.AtMostTotal((long)known.Hq+other.Hq,known.Values);known.ImportedHq=ValueCounts.AtMostTotal((long)known.ImportedHq+other.ImportedHq,known.ImportedValues);
        if(other.LevelMin>0)known.LevelMin=known.LevelMin==0 ? other.LevelMin:Math.Min(known.LevelMin,other.LevelMin);
        known.LevelMax=Math.Max(known.LevelMax,other.LevelMax);
        if(other.First<known.First){known.First=other.First;known.X=other.X;known.Z=other.Z;}
        if(other.Last>known.Last)known.Last=other.Last;
    }

    /// <summary>X-74 (0.860): the kinds that hold a place (a map); the others are about items and sources only.</summary>
    public static bool HasPlace(DiscoveryKind kind)=>kind is DiscoveryKind.MonsterPlace or DiscoveryKind.HuntMarkPlace or DiscoveryKind.FateSeen or DiscoveryKind.MonsterDrop or DiscoveryKind.FishCatch
        or DiscoveryKind.NpcPlace or DiscoveryKind.BlueSpellCaster or DiscoveryKind.FishBite or DiscoveryKind.SpearShadow or DiscoveryKind.FishBiteBait or DiscoveryKind.IntuitionStart;
    /// <summary>
    /// What an imported fact must hold: a known kind, a subject and a map, finite places in their cell, levels and dates that make sense. 0.911 (X-76): a FATE
    /// at exactly (0, 0) is the zero position the game gives for about a second (entering a zone, reloading): refused, read or imported. X-76 E1 (0.916): the
    /// histograms within their bounds (ValueCounts.Valid), never more HQ sightings than sightings counted in them.
    /// </summary>
    public static bool Valid(DiscoveryFact? fact)=>fact is not null && Enum.IsDefined(fact.Kind) && fact.Subject!=0 && (fact.Map!=0 || !HasPlace(fact.Kind))
        && !(fact.Kind==DiscoveryKind.FateSeen && fact.X==0 && fact.Z==0)
        && float.IsFinite(fact.X) && float.IsFinite(fact.Z) && Math.Abs(fact.X)<=10_000 && Math.Abs(fact.Z)<=10_000 && Cell(fact.X,fact.Z)==(fact.CellX,fact.CellZ)
        && fact.LevelMin is >= 0 and <= 100_000 && fact.LevelMax is >= 0 and <= 100_000 && fact.LevelMin<=Math.Max(fact.LevelMin,fact.LevelMax)
        && fact.Seen>=0 && fact.Imported>=0 && fact.First<=fact.Last && fact.Last<=DateTimeOffset.UtcNow.AddDays(1)
        && ValueCounts.Valid(fact.Values) && ValueCounts.Valid(fact.ImportedValues)
        && fact.Hq>=0 && fact.Hq<=ValueCounts.Total(fact.Values) && fact.ImportedHq>=0 && fact.ImportedHq<=ValueCounts.Total(fact.ImportedValues)
        && !(fact.Kind==DiscoveryKind.QuestItemAsked && fact.Last<QuestItemAsks.Fixed);

    public void Load(DiscoveryFile file){lock(gate)LoadLocked(file,true);}
    private void LoadLocked(DiscoveryFile file,bool count)
    {
        {
            // 0.855: each fact moved to its 50-yalm cell; those meeting in one are joined, and the base is written again, smaller.
            var read=0;var refused=0;var shared=SharedHeld();
            foreach(var original in file.Facts)
            {
                if(original is null)continue;
                var fact=Recell(original);
                if(!Valid(fact)){refused++;continue;}
                read++;
                if(facts.TryGetValue(fact.Key,out var known)){Join(known,fact);continue;}
                // X-93 (0.939): yours always; a shared fact while the shared part has room, else counted as left out.
                if(fact.Seen>0)Put(fact);
                else if(shared.GetValueOrDefault(fact.Kind)<MaxFacts){Put(fact);shared[fact.Kind]=shared.GetValueOrDefault(fact.Kind)+1;}
                else LeftOut++;
            }
            // 0.1005: a refused fact (one written before a fix) is gone from the file at the next save, not only from memory.
            if(count && (read>facts.Count || refused>0) || file.Facts.Any(fact=>fact is not null && Cell(fact.X,fact.Z)!=(fact.CellX,fact.CellZ)))Dirty=true;
            revision++;
        }
    }

    /// <summary>A copy of the base; « saving » marks it as written (an export does not).</summary>
    public DiscoveryFile Snapshot(DateTimeOffset at,bool saving=true)
    {
        lock(gate){if(saving)Dirty=false;return new DiscoveryFile{Written=at,Facts=facts.Values.Select(fact=>fact.Copy()).ToList()};}
    }

    public void Clear(){lock(gate){facts.Clear();index.Clear();sharedWaiting.Clear();Dirty=true;revision++;}}

    /// <summary>Per kind: the facts, those seen by the player, those only imported.</summary>
    public IReadOnlyList<(DiscoveryKind Kind,int Total,int Seen,int ImportedOnly)> Summary()
    {
        lock(gate)return facts.Values.GroupBy(fact=>fact.Kind).OrderBy(group=>group.Key)
            .Select(group=>(group.Key,group.Count(),group.Count(fact=>fact.Seen>0),group.Count(fact=>fact.Seen==0))).ToArray();
    }

    /// <summary>The facts of a kind whose subject passes the filter, most seen first.</summary>
    public IReadOnlyList<DiscoveryFact> Find(DiscoveryKind kind,Func<uint,bool> subject,int max)
    {
        lock(gate)
        {
            Want(kind);
            if(!index.TryGetValue(kind,out var bySubject))return [];
            return bySubject.Where(pair=>subject(pair.Key)).SelectMany(pair=>pair.Value).OrderByDescending(DiscoveryFact.TimesOf).ThenBy(fact=>fact.Subject)
                .Take(max).Select(fact=>fact.Copy()).ToArray();
        }
    }

    /// <summary>X-93 (0.938): the facts of a kind about one subject, most seen first, straight from the index.</summary>
    public IReadOnlyList<DiscoveryFact> Find(DiscoveryKind kind,uint subject,int max)
    {
        lock(gate)
        {
            Want(kind);
            if(!index.TryGetValue(kind,out var bySubject) || !bySubject.TryGetValue(subject,out var list))return [];
            return list.OrderByDescending(DiscoveryFact.TimesOf).Take(max).Select(fact=>fact.Copy()).ToArray();
        }
    }

    /// <summary>X-76 (0.908): the spearfishing shadow (size, speed) seen in game most often for a fish (yours or shared), if any.</summary>
    public (int Size,int Speed)? MostSeenSpearShadow(uint fish)=>
        Find(DiscoveryKind.SpearShadow,subject=>subject==fish,8).Select(fact=>((int,int)?)((int)fact.Detail,fact.LevelMin)).FirstOrDefault();
}

/// <summary>
/// X-76 (0.911): the subject of a QuestItemAsked fact: the item, or its HQ id (1 000 000 + item) when the quest asks for HQ. A key item (EventItem row,
/// 2 000 000 and up) is kept as it is and is never HQ.
/// </summary>
public static class QuestItemAsks
{
    public const uint HqOffset=1_000_000;
    /// <summary>
    /// 0.1005 (docs/evidence/0.1004-discoveries-check.md, the user's choice): 0.911 was published at 2026-11-08 16:42 (UTC+1); a QuestItemAsked fact last seen
    /// before it may hold a key item without its 2 000 000 (seen: 2002169 kept as 2169) or an HQ item without its offset: refused when read, imported or merged.
    /// </summary>
    public static readonly DateTimeOffset Fixed=new(2026,11,8,15,42,29,TimeSpan.Zero);
    public static uint Subject(uint item,bool hq)=>ItemGains.BaseItem(item) is var plain && (plain==0 || plain>=ItemGains.KeyItemStart || !hq) ? plain:plain+HqOffset;
    public static uint Item(uint subject)=>ItemGains.BaseItem(subject);
    public static bool Hq(uint subject)=>subject is >= HqOffset and < ItemGains.KeyItemStart;
    /// <summary>True for the facts about this item, HQ demanded or not.</summary>
    public static bool IsOf(uint subject,uint item)=>item!=0 && subject!=0 && Item(subject)==ItemGains.BaseItem(item);
}

/// <summary>X-72 A (0.854): the places of a monster for the tools: per map, its most seen place; the most seen maps first.</summary>
public static class SeenPlaces
{
    public static DiscoveryFact[] ByMap(IEnumerable<DiscoveryFact> facts,int maps)=>
        facts.GroupBy(fact=>fact.Map).Select(group=>group.OrderByDescending(DiscoveryFact.TimesOf).First())
            .OrderByDescending(DiscoveryFact.TimesOf).Take(maps).ToArray();
}

/// <summary>X-43 (0.807): the files: the base, the exports (a dated copy) and the imports (every .json of the import folder, read once, then renamed).</summary>
public static class DiscoveryFiles
{
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=false};
    public const long MaxBytes=64L*1024*1024;

    public static DiscoveryFile? Read(string path)
    {
        if(!File.Exists(path) || new FileInfo(path).Length>MaxBytes)return null;
        var file=JsonSerializer.Deserialize<DiscoveryFile>(File.ReadAllText(path),Options);
        return DiscoveryFile.Readable(file) ? file:null;
    }

    /// <summary>
    /// X-93 part 2 (0.1039): the base read at load. A file that is there but cannot be used (over <paramref name="max"/>, damaged, unknown version) is set
    /// aside under a dated name that never replaces an earlier one, and reported through <paramref name="broken"/>: the next save starts a new file instead of
    /// writing an empty base over it. <c>Unsafe</c> is true when it could not even be moved: then nothing may be written this session.
    /// </summary>
    public static (DiscoveryFile? File,string? SetAside,bool Unsafe) ReadBase(string path,DateTimeOffset now,Action<string,Exception> broken,long max=MaxBytes)
    {
        if(!File.Exists(path))return (null,null,false);
        DiscoveryFile? file=null;Exception? error=null;
        try
        {
            if(new FileInfo(path).Length>max)throw new InvalidDataException($"Discoveries file over {max:N0} bytes.");
            file=JsonSerializer.Deserialize<DiscoveryFile>(File.ReadAllText(path),Options);
            if(!DiscoveryFile.Readable(file))throw new InvalidDataException("Unknown discoveries file.");
        }
        catch(Exception caught){error=caught;}
        if(error is null)return (file,null,false);
        var aside=SetAsideName(path,now);
        broken(aside,error);
        try{File.Move(path,aside,false);return (null,aside,false);}
        catch(Exception){return (null,aside,true);}
    }

    /// <summary>X-93 part 2 (0.1039): « Discoveries.json.broken-yyyyMMdd-HHmmss », with « -2 », « -3 »… when that name is taken.</summary>
    public static string SetAsideName(string path,DateTimeOffset now)
    {
        var name=$"{path}.broken-{now.ToUniversalTime():yyyyMMdd-HHmmss}";
        var aside=name;
        for(var number=2;File.Exists(aside);number++)aside=$"{name}-{number}";
        return aside;
    }

    public static void Write(string path,DiscoveryFile file)
    {
        AtomicFile.Write(path,JsonSerializer.Serialize(file,Options));
    }

    /// <summary>
    /// X-93 (0.940): the shared facts (imported, not seen by you) are kept apart from yours: <c>Shared/kind-NN.json.gz</c>, one gzip file per kind, in the same
    /// format as an exported file; the main file keeps your facts only (with their imported counts). A kind file is written again only when its content changed
    /// (<paramref name="written"/> holds the last content written per kind, for this session), and removed when the kind has no shared fact any more.
    /// </summary>
    public static void WriteSplit(string mainPath,DiscoveryFile all,Dictionary<DiscoveryKind,string> written,IReadOnlySet<DiscoveryKind>? held=null)
    {
        // X-93 (0.941): the shared files first, then yours: a stop between the two never loses a shared fact. Kinds still on disk (not held) are left alone.
        var mine=all.Facts.Where(fact=>fact.Seen>0).ToList();
        var folder=SharedFolder(mainPath);
        var byKind=all.Facts.Where(fact=>fact.Seen==0).GroupBy(fact=>fact.Kind).ToDictionary(group=>group.Key,group=>group.OrderBy(fact=>fact.Key,StringComparer.Ordinal).ToList());
        foreach(var (kind,facts) in byKind)
        {
            if(held is not null && !held.Contains(kind))continue;
            var json=JsonSerializer.Serialize(new DiscoveryFile{Version=all.Version,Producer=all.Producer,Written=all.Written,Facts=facts},Options);
            var content=json[(json.IndexOf("\"Facts\"",StringComparison.Ordinal) is var at and >= 0 ? at:0)..];
            if(written.TryGetValue(kind,out var last) && last==content && File.Exists(SharedPath(folder,kind)))continue;
            using(var buffer=new MemoryStream())
            {
                using(var gzip=new System.IO.Compression.GZipStream(buffer,System.IO.Compression.CompressionLevel.Optimal,true))
                    gzip.Write(System.Text.Encoding.UTF8.GetBytes(json));
                AtomicFile.Write(SharedPath(folder,kind),buffer.ToArray());
            }
            written[kind]=content;
        }
        if(Directory.Exists(folder))
            foreach(var path in Directory.GetFiles(folder,"kind-*.json.gz"))
                if(KindOf(path) is { } kind && !byKind.ContainsKey(kind) && (held is null || held.Contains(kind))){File.Delete(path);written.Remove(kind);}
        Write(mainPath,new DiscoveryFile{Version=all.Version,Producer=all.Producer,Written=all.Written,Facts=mine});
    }

    /// <summary>X-93 (0.941): the shared kinds that have a file next to the main file.</summary>
    public static IReadOnlyList<DiscoveryKind> SharedKindsOnDisk(string mainPath)
        =>Directory.Exists(SharedFolder(mainPath)) ? Directory.GetFiles(SharedFolder(mainPath),"kind-*.json.gz").Select(KindOf).OfType<DiscoveryKind>().Distinct().ToArray():[];
    /// <summary>X-93 (0.941): one shared kind file (null when missing; an unreadable one reported and set aside).</summary>
    public static DiscoveryFile? ReadSharedKind(string mainPath,DiscoveryKind kind,Action<string,Exception> broken)
    {
        var path=SharedPath(SharedFolder(mainPath),kind);
        return File.Exists(path) ? ReadSharedFile(path,broken):null;
    }

    /// <summary>X-93 (0.940): every shared kind file next to the main file (unreadable ones reported through <paramref name="broken"/> and set aside).</summary>
    public static IEnumerable<DiscoveryFile> ReadShared(string mainPath,Action<string,Exception> broken)
    {
        var folder=SharedFolder(mainPath);
        if(!Directory.Exists(folder))yield break;
        foreach(var path in Directory.GetFiles(folder,"kind-*.json.gz").Order(StringComparer.Ordinal))
            if(ReadSharedFile(path,broken) is { } file)yield return file;
    }
    private static DiscoveryFile? ReadSharedFile(string path,Action<string,Exception> broken)
    {
        {
            DiscoveryFile? file=null;
            try
            {
                if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("Shared discoveries file too large.");
                using var stream=File.OpenRead(path);
                using var gzip=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionMode.Decompress);
                using var bounded=new MemoryStream();
                var chunk=new byte[81920];int read;long total=0;
                while((read=gzip.Read(chunk,0,chunk.Length))>0){total+=read;if(total>MaxBytes*4)throw new InvalidDataException("Shared discoveries file too large.");bounded.Write(chunk,0,read);}
                file=JsonSerializer.Deserialize<DiscoveryFile>(bounded.ToArray(),Options);
                if(!DiscoveryFile.Readable(file))throw new InvalidDataException("Unknown shared discoveries file.");
            }
            catch(Exception error){broken(path,error);try{File.Move(path,path+".broken",true);}catch(Exception){}return null;}
            return file;
        }
    }

    public static string SharedFolder(string mainPath)=>Path.Combine(Path.GetDirectoryName(mainPath) ?? "","Shared");
    public static string SharedPath(string folder,DiscoveryKind kind)=>Path.Combine(folder,$"kind-{(int)kind:00}.json.gz");
    private static DiscoveryKind? KindOf(string path)
        =>Path.GetFileName(path) is var name && name.Length>=12 && int.TryParse(name.AsSpan(5,2),out var number) && Enum.IsDefined((DiscoveryKind)number) ? (DiscoveryKind)number:null;

    public static string ExportName(DateTimeOffset at)=>$"Discoveries-{at.ToUniversalTime():yyyyMMdd-HHmmss}.json";

    /// <summary>Every file of the import folder joined to the store; each one read is renamed « .imported » so that it is not joined twice.</summary>
    public static (int Files,int Added,int Joined,int Refused,int Unreadable) Import(string folder,DiscoveryStore store)
    {
        int files=0,added=0,joined=0,refused=0,unreadable=0;
        if(!Directory.Exists(folder))return (0,0,0,0,0);
        foreach(var path in Directory.GetFiles(folder,"*.json").Order(StringComparer.Ordinal))
        {
            DiscoveryFile? file;
            try{file=Read(path);}catch(Exception){file=null;}
            if(file is null){unreadable++;continue;}
            var result=store.Merge(file.Facts);
            files++;added+=result.Added;joined+=result.Joined;refused+=result.Refused;
            File.Move(path,path+".imported",true);
        }
        return (files,added,joined,refused,unreadable);
    }
}
