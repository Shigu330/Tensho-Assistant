using System.Numerics;
using System.Text.Json;

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
    // X-73.18 (0.889, docs/evidence/0.889-hand-in.md): QuestItemAsked: Subject = item handed in, Detail = the Quest row whose step changed just after (HQ adds
    // 1 000 000 to nothing: the HQ demand is kept as LevelMax = 1), LevelMin = quantity asked.
    QuestItemAsked=25,
    // 26 was 0.890's spearfishing shadow, taken from the shadows left in sight: wrong (docs/evidence/0.891-spearfishing-slots.md); its facts are refused when read.
    // 27 was 0.891's: the item was read a moment too early (it lags the catch count) and the speed could be the doubled one (docs/evidence/0.896-spearfishing-lag.md);
    // its facts are refused when read.
    // X-73.18 (0.896): SpearShadow: Subject = fish speared, Detail = the shadow's size (1 small, 2 average, 3 large), levels = its speed when it appeared, place =
    // where it was speared. The shadow is the slot that left the window with its size and speed kept, just before the catch.
    SpearShadow=28 }

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
    public string Key=>$"{(int)Kind}:{Subject}:{Detail}:{Territory}:{Map}:{CellX}:{CellZ}";
    public DiscoveryFact Copy()=>(DiscoveryFact)MemberwiseClone();
    /// <summary>X-76 (0.908): how many times a fact was seen in game, yours and imported together (static: nothing more is written in the files).</summary>
    public static int TimesOf(DiscoveryFact fact)=>fact.Seen+fact.Imported;
}

/// <summary>The file: a version, who wrote it, when, and the facts.</summary>
public sealed class DiscoveryFile
{
    public const int CurrentVersion=1;
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
    public const int MaxFacts=200_000;
    /// <summary>A second sighting of the same place counts only after this long: standing beside a monster is one sighting.</summary>
    public static readonly TimeSpan SightingGap=TimeSpan.FromMinutes(10);
    private readonly object gate=new();
    private readonly Dictionary<string,DiscoveryFact> facts=new(StringComparer.Ordinal);
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
        =>See(DiscoveryKind.FateSeen,fateId,0,territory,map,position,level,at,FateGap);

    /// <summary>X-74 (0.860): an item got from a source; every event counts once (the watcher sends each once); a place only when a map is given.</summary>
    public bool SeeSource(DiscoveryKind kind,uint subject,uint detail,int value,DateTimeOffset at,uint territory=0,uint map=0,Vector3? place=null)
    {
        var position=map==0 ? Vector3.Zero:place ?? Vector3.Zero;
        if(subject==0 || value<0 || !float.IsFinite(position.X) || !float.IsFinite(position.Z))return false;
        var (cellX,cellZ)=Cell(position.X,position.Z);
        var fact=new DiscoveryFact{Kind=kind,Subject=subject,Detail=detail,Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=position.X,Z=position.Z,
            LevelMin=value,LevelMax=value,Seen=1,First=at,Last=at};
        lock(gate)
        {
            if(facts.TryGetValue(fact.Key,out var known))
            {
                known.LevelMin=Math.Min(known.LevelMin,value);known.LevelMax=Math.Max(known.LevelMax,value);known.Seen++;
                if(at>known.Last)known.Last=at;
                Dirty=true;revision++;
                return false;
            }
            if(facts.Count>=MaxFacts)return false;
            facts[fact.Key]=fact;Dirty=true;revision++;
            return true;
        }
    }

    private bool See(DiscoveryKind kind,uint subject,uint detail,uint territory,uint map,Vector3 position,int level,DateTimeOffset at,TimeSpan gap)
    {
        if(subject==0 || map==0 || !float.IsFinite(position.X) || !float.IsFinite(position.Z))return false;
        var (cellX,cellZ)=Cell(position.X,position.Z);
        var fact=new DiscoveryFact{Kind=kind,Subject=subject,Detail=detail,Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=position.X,Z=position.Z,
            LevelMin=level,LevelMax=level,Seen=1,First=at,Last=at};
        lock(gate)
        {
            if(facts.TryGetValue(fact.Key,out var known))
            {
                if(level>0){known.LevelMin=known.LevelMin==0 ? level:Math.Min(known.LevelMin,level);known.LevelMax=Math.Max(known.LevelMax,level);}
                if(at-known.Last>=gap)known.Seen++;
                if(at>known.Last)known.Last=at;
                Dirty=true;
                return false;
            }
            if(facts.Count>=MaxFacts)return false;
            facts[fact.Key]=fact;Dirty=true;revision++;
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
        lock(gate)
        {
            foreach(var original in imported)
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
                    joined++;
                    continue;
                }
                if(facts.Count>=MaxFacts){refused++;continue;}
                var copy=source.Copy();copy.Seen=0;copy.Imported=1;
                facts[copy.Key]=copy;added++;
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
    /// <summary>X-72 B (0.855): two facts of one cell made one: sightings and imports added, the widest levels and dates, the first exact place kept.</summary>
    public static void Join(DiscoveryFact known,DiscoveryFact other)
    {
        known.Seen+=other.Seen;known.Imported+=other.Imported;
        if(other.LevelMin>0)known.LevelMin=known.LevelMin==0 ? other.LevelMin:Math.Min(known.LevelMin,other.LevelMin);
        known.LevelMax=Math.Max(known.LevelMax,other.LevelMax);
        if(other.First<known.First){known.First=other.First;known.X=other.X;known.Z=other.Z;}
        if(other.Last>known.Last)known.Last=other.Last;
    }

    /// <summary>X-74 (0.860): the kinds that hold a place (a map); the others are about items and sources only.</summary>
    public static bool HasPlace(DiscoveryKind kind)=>kind is DiscoveryKind.MonsterPlace or DiscoveryKind.HuntMarkPlace or DiscoveryKind.FateSeen or DiscoveryKind.MonsterDrop or DiscoveryKind.FishCatch
        or DiscoveryKind.NpcPlace or DiscoveryKind.BlueSpellCaster or DiscoveryKind.FishBite or DiscoveryKind.SpearShadow;
    /// <summary>What an imported fact must hold: a known kind, a subject and a map, finite places in their cell, levels and dates that make sense.</summary>
    public static bool Valid(DiscoveryFact? fact)=>fact is not null && Enum.IsDefined(fact.Kind) && fact.Subject!=0 && (fact.Map!=0 || !HasPlace(fact.Kind))
        && float.IsFinite(fact.X) && float.IsFinite(fact.Z) && Math.Abs(fact.X)<=10_000 && Math.Abs(fact.Z)<=10_000 && Cell(fact.X,fact.Z)==(fact.CellX,fact.CellZ)
        && fact.LevelMin is >= 0 and <= 100_000 && fact.LevelMax is >= 0 and <= 100_000 && fact.LevelMin<=Math.Max(fact.LevelMin,fact.LevelMax)
        && fact.Seen>=0 && fact.Imported>=0 && fact.First<=fact.Last && fact.Last<=DateTimeOffset.UtcNow.AddDays(1);

    public void Load(DiscoveryFile file)
    {
        lock(gate)
        {
            // 0.855: each fact moved to its 50-yalm cell; those meeting in one are joined, and the base is written again, smaller.
            var read=0;
            foreach(var original in file.Facts)
            {
                if(original is null)continue;
                var fact=Recell(original);
                if(!Valid(fact))continue;
                read++;
                if(facts.TryGetValue(fact.Key,out var known)){Join(known,fact);continue;}
                if(facts.Count<MaxFacts)facts[fact.Key]=fact;
            }
            if(read>facts.Count || file.Facts.Any(fact=>fact is not null && Cell(fact.X,fact.Z)!=(fact.CellX,fact.CellZ)))Dirty=true;
            revision++;
        }
    }

    /// <summary>A copy of the base; « saving » marks it as written (an export does not).</summary>
    public DiscoveryFile Snapshot(DateTimeOffset at,bool saving=true)
    {
        lock(gate){if(saving)Dirty=false;return new DiscoveryFile{Written=at,Facts=facts.Values.Select(fact=>fact.Copy()).ToList()};}
    }

    public void Clear(){lock(gate){facts.Clear();Dirty=true;revision++;}}

    /// <summary>Per kind: the facts, those seen by the player, those only imported.</summary>
    public IReadOnlyList<(DiscoveryKind Kind,int Total,int Seen,int ImportedOnly)> Summary()
    {
        lock(gate)return facts.Values.GroupBy(fact=>fact.Kind).OrderBy(group=>group.Key)
            .Select(group=>(group.Key,group.Count(),group.Count(fact=>fact.Seen>0),group.Count(fact=>fact.Seen==0))).ToArray();
    }

    /// <summary>The facts of a kind whose subject passes the filter, most seen first.</summary>
    public IReadOnlyList<DiscoveryFact> Find(DiscoveryKind kind,Func<uint,bool> subject,int max)
    {
        lock(gate)return facts.Values.Where(fact=>fact.Kind==kind && subject(fact.Subject)).OrderByDescending(DiscoveryFact.TimesOf).ThenBy(fact=>fact.Subject)
            .Take(max).Select(fact=>fact.Copy()).ToArray();
    }

    /// <summary>X-76 (0.908): the spearfishing shadow (size, speed) seen in game most often for a fish (yours or shared), if any.</summary>
    public (int Size,int Speed)? MostSeenSpearShadow(uint fish)=>
        Find(DiscoveryKind.SpearShadow,subject=>subject==fish,8).Select(fact=>((int,int)?)((int)fact.Detail,fact.LevelMin)).FirstOrDefault();
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
        return file is { Version:DiscoveryFile.CurrentVersion, Facts:not null } ? file:null;
    }

    public static void Write(string path,DiscoveryFile file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+".tmp";
        File.WriteAllText(temporary,JsonSerializer.Serialize(file,Options));
        File.Move(temporary,path,true);
    }

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
