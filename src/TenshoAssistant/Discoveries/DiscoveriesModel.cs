using System.Numerics;
using System.Text.Json;

namespace TenshoAssistant.Discoveries;

/// <summary>X-43 (0.807): what a fact is about. Numbers are kept in files: never reuse one.</summary>
// X-72 C (0.856, user choice « ABC »): hunt marks' spawn places (B, A and S ranks: in no game sheet, only in community data) and the FATEs seen (place, level,
// when). HuntMarkPlace: Subject = BNpcName row, Detail = BNpcBase row, levels = the rank (1 B, 2 A, 3 S). FateSeen: Subject = Fate row, levels = the FATE's level.
public enum DiscoveryKind { MonsterPlace=1, HuntMarkPlace=2, FateSeen=3 }

/// <summary>
/// X-43 (0.807, user request): one thing seen in game, kept locally, with where it came from: seen by the player (Seen, how many separate times), imported
/// (Imported, how many files had it). Only game rows and places: never a player's name, position or id.
/// MonsterPlace: Subject = BNpcName row, Detail = BNpcBase row, the territory and map, the 10-yalm cell, the first exact place, the levels seen.
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

    /// <summary>What an imported fact must hold: a known kind, a subject and a map, finite places in their cell, levels and dates that make sense.</summary>
    public static bool Valid(DiscoveryFact? fact)=>fact is not null && Enum.IsDefined(fact.Kind) && fact.Subject!=0 && fact.Map!=0
        && float.IsFinite(fact.X) && float.IsFinite(fact.Z) && Math.Abs(fact.X)<=10_000 && Math.Abs(fact.Z)<=10_000 && Cell(fact.X,fact.Z)==(fact.CellX,fact.CellZ)
        && fact.LevelMin is >= 0 and <= 200 && fact.LevelMax is >= 0 and <= 200 && fact.LevelMin<=Math.Max(fact.LevelMin,fact.LevelMax)
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
        lock(gate)return facts.Values.Where(fact=>fact.Kind==kind && subject(fact.Subject)).OrderByDescending(fact=>fact.Seen+fact.Imported).ThenBy(fact=>fact.Subject)
            .Take(max).Select(fact=>fact.Copy()).ToArray();
    }
}

/// <summary>X-72 A (0.854): the places of a monster for the tools: per map, its most seen place; the most seen maps first.</summary>
public static class SeenPlaces
{
    public static DiscoveryFact[] ByMap(IEnumerable<DiscoveryFact> facts,int maps)=>
        facts.GroupBy(fact=>fact.Map).Select(group=>group.OrderByDescending(fact=>fact.Seen+fact.Imported).First())
            .OrderByDescending(fact=>fact.Seen+fact.Imported).Take(maps).ToArray();
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
