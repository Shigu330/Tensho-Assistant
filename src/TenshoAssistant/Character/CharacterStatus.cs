using System.Globalization;
using System.Text.Json;

namespace TenshoAssistant.Character;

/// <summary>One retainer's venture as last read (Done = when it ends; null = no venture).</summary>
public sealed record VentureState(string Retainer,DateTimeOffset? Done);

/// <summary>
/// g, M-04 (0.374): what Tensho last read for one of the player's characters (local character id, never an Account ID): retainer
/// ventures, next treasure map, Wondrous Tails, hunt bills, daily roulettes, and the Reminders page's « done » boxes. A null value is
/// unknown (never read), never zero.
/// </summary>
public sealed record CharacterStatus
{
    public ulong ContentId { get; init; }
    public string Name { get; init; } = "";
    public DateTimeOffset SeenAt { get; init; }
    public VentureState[]? Ventures { get; init; }
    public DateTimeOffset? NextMap { get; init; }
    public bool? BingoHeld { get; init; }
    public int? BingoStickers { get; init; }
    public DateTimeOffset? BingoExpires { get; init; }
    public int? HuntBills { get; init; }
    public int? RoulettesDone { get; init; }
    public int? RoulettesTotal { get; init; }
    public DateTimeOffset? RoulettesAt { get; init; }
    public Dictionary<string,DateTimeOffset>? Done { get; init; }
}

/// <summary>
/// g, M-04 (0.374): merging a fresh read into the kept status, the overview figures, and the files (one per character,
/// Characters/&lt;local character id&gt;.json, user's choice: one file per character, like I-03).
/// </summary>
public static class CharacterStatuses
{
    public const int MaximumFileBytes=256*1024,MaximumVentures=10,MaximumDone=64;

    /// <summary>The fresh read wins where it knows a value; what it does not know (retainer list not opened…) stays as last read.</summary>
    public static CharacterStatus Merge(CharacterStatus? old,CharacterStatus fresh)
    {
        if(old is null || old.ContentId!=fresh.ContentId)return fresh;
        return fresh with
        {
            Name=fresh.Name.Length>0 ? fresh.Name:old.Name,
            Ventures=fresh.Ventures ?? old.Ventures,
            NextMap=fresh.NextMap ?? old.NextMap,
            BingoHeld=fresh.BingoHeld ?? old.BingoHeld,
            BingoStickers=fresh.BingoHeld is null ? old.BingoStickers:fresh.BingoStickers,
            BingoExpires=fresh.BingoHeld is null ? old.BingoExpires:fresh.BingoExpires,
            HuntBills=fresh.HuntBills ?? old.HuntBills,
            RoulettesDone=fresh.RoulettesDone is null ? old.RoulettesDone:fresh.RoulettesDone,
            RoulettesTotal=fresh.RoulettesDone is null ? old.RoulettesTotal:fresh.RoulettesTotal,
            RoulettesAt=fresh.RoulettesDone is null ? old.RoulettesAt:fresh.RoulettesAt,
            Done=fresh.Done ?? old.Done,
        };
    }

    /// <summary>True when the two statuses differ in anything but the time they were seen (a save is then due).</summary>
    public static bool Changed(CharacterStatus? old,CharacterStatus fresh)
    {
        if(old is null)return true;
        var a=old with{SeenAt=default,Ventures=null,Done=null,RoulettesAt=null};var b=fresh with{SeenAt=default,Ventures=null,Done=null,RoulettesAt=null};
        return a!=b || !Same(old.Ventures,fresh.Ventures) || !Same(old.Done,fresh.Done);
    }
    private static bool Same(VentureState[]? a,VentureState[]? b)=>a is null ? b is null:b is not null && a.SequenceEqual(b);
    private static bool Same(Dictionary<string,DateTimeOffset>? a,Dictionary<string,DateTimeOffset>? b)
        =>a is null ? b is null:b is not null && a.Count==b.Count && a.All(pair=>b.TryGetValue(pair.Key,out var at) && at==pair.Value);

    /// <summary>Ventures done, ventures running, and the next end (null when none runs). Unknown (never read) = null.</summary>
    public static (int Ready,int Running,DateTimeOffset? Next)? Ventures(CharacterStatus status,DateTimeOffset now)
    {
        if(status.Ventures is not { } ventures)return null;
        var ready=ventures.Count(venture=>venture.Done<=now);
        var running=ventures.Where(venture=>venture.Done>now).Select(venture=>venture.Done!.Value).ToArray();
        return (ready,running.Length,running.Length>0 ? running.Min():null);
    }

    /// <summary>Daily roulettes done today, or null when the last read was before the daily reset (the count then means nothing).</summary>
    public static (int Done,int Total)? RoulettesToday(CharacterStatus status,DateTimeOffset lastDailyReset)
        =>status is { RoulettesDone:{ } done,RoulettesTotal:{ } total,RoulettesAt:{ } at } && at>=lastDailyReset ? (done,total):null;

    public static string FileName(ulong contentId)=>contentId.ToString(CultureInfo.InvariantCulture)+".json";

    private static readonly JsonSerializerOptions Json=new(){MaxDepth=8};
    private sealed record FileDto(int Schema,CharacterStatus Status);

    /// <summary>Reads one character's file; a file too large, of another schema, or holding another character throws.</summary>
    public static CharacterStatus Read(string path,ulong expected)
    {
        var info=new FileInfo(path);
        if(info.Length>MaximumFileBytes)throw new InvalidDataException("Character file too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty character file.");
        if(dto.Schema!=1 || dto.Status is null)throw new InvalidDataException("Unknown character file schema.");
        if(dto.Status.ContentId!=expected)throw new InvalidDataException("Character file holds another character.");
        return Bound(dto.Status);
    }

    /// <summary>Writes one character's file through a temporary file (a crash never leaves half a file).</summary>
    public static void Write(string folder,CharacterStatus status)
    {
        if(status.ContentId==0)throw new ArgumentException("No character.",nameof(status));
        Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,FileName(status.ContentId));
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllBytes(temporary,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,Bound(status)),Json));
        File.Move(temporary,path,true);
    }

    /// <summary>Every character file of the folder; a bad file is reported and skipped (and never written over by another character).</summary>
    public static List<CharacterStatus> ReadAll(string folder,Action<string,Exception> failed)
    {
        var all=new List<CharacterStatus>();
        if(!Directory.Exists(folder))return all;
        foreach(var path in Directory.EnumerateFiles(folder,"*.json").Take(200))
        {
            if(!ulong.TryParse(Path.GetFileNameWithoutExtension(path),NumberStyles.None,CultureInfo.InvariantCulture,out var id) || id==0)continue;
            try{all.Add(Read(path,id));}
            catch(Exception error){failed(path,error);}
        }
        return all;
    }

    private static CharacterStatus Bound(CharacterStatus status)=>status with
    {
        Name=status.Name.Length>80 ? status.Name[..80]:status.Name,
        Ventures=status.Ventures?.Take(MaximumVentures).ToArray(),
        Done=status.Done is { Count:>MaximumDone } done ? done.Take(MaximumDone).ToDictionary():status.Done,
    };
}
