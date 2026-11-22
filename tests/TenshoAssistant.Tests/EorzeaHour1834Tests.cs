using System.IO.Compression;
using System.Text;
using System.Text.Json;

// 0.1834 (X-841, review of 0.1832 I2): X-829 brought the shares read after 0.1831 to the Eorzea hour, but the facts merged before kept the Eorzea
// minute (VistaSeen 18, FishCondition 11, FishTransition 21) in the repository's common set and player file: 157 facts, 49 seen once with their
// day. The merge tool's rebuild now brings every player file it reads to the hour (and so the common set built from it); the repository's data were
// rebuilt. Here: the repository's own files hold the hour only, and the tool's case is in its selftest (run by Anonymous1832Tests).
internal static class EorzeaHour1834Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly int[] TimeKinds={11,18,21};

    // The facts of a gzip file of the common set or a player file (a header line, then one fact per line).
    private static List<JsonElement> Facts(string path)
    {
        using var zip=new GZipStream(File.OpenRead(path),CompressionMode.Decompress);
        using var reader=new StreamReader(zip,Encoding.UTF8);
        reader.ReadLine();
        var facts=new List<JsonElement>();
        for(var line=reader.ReadLine();line is not null;line=reader.ReadLine())
            if(line.Length>0)facts.Add(JsonDocument.Parse(line).RootElement.Clone());
        return facts;
    }

    // A minute of the Eorzea clock left in a fact of a time kind: a level or a value that is not a whole hour (a multiple of 60).
    private static string? Minute(JsonElement fact)
    {
        if(!fact.TryGetProperty("Kind",out var kind) || Array.IndexOf(TimeKinds,kind.GetInt32())<0)return null;
        foreach(var name in new[]{"LevelMin","LevelMax"})
            if(fact.TryGetProperty(name,out var level) && level.GetInt32()%60!=0)return name+" "+level.GetInt32();
        if(fact.TryGetProperty("Values",out var values))
            foreach(var value in values.EnumerateObject())
                if(int.Parse(value.Name,System.Globalization.CultureInfo.InvariantCulture)%60!=0)return "value "+value.Name;
        return null;
    }

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Eorzea hour 1834: the repository's common set and player files keep the Eorzea hour, never the minute (X-841)",() =>
        {
            var files=TimeKinds.Select(kind=>Path.GetFullPath($"community-discoveries/v3/kind-{kind:00}.jsonl.gz")).Where(File.Exists)
                .Concat(Directory.GetFiles(Path.GetFullPath("community-discoveries/players"),"*.jsonl.gz")).ToList();
            Check(files.Count>=2,"the common set's time kinds and a player file are read: "+files.Count);
            var timed=0;
            foreach(var path in files)
                foreach(var fact in Facts(path))
                {
                    var minute=Minute(fact);
                    Check(minute is null,Path.GetFileName(path)+": a fact keeps an Eorzea minute ("+minute+"): "+fact.GetRawText());
                    if(Array.IndexOf(TimeKinds,fact.GetProperty("Kind").GetInt32())>=0)timed++;
                }
            Check(timed>0,"facts of the time kinds were read");
        }),
        ("Eorzea hour 1834: rebuild brings a player file it reads to the hour, its selftest case kept (X-841)",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("hourly=[eorzea_hour(dict(f)) for f in player[\"Facts\"]]",StringComparison.Ordinal)
                && tool.Contains("kept=[eorzea_hour(f) for f in kept]",StringComparison.Ordinal)
                && tool.Contains("given=[eorzea_hour(dict(f)) for f in pending[name][1]]",StringComparison.Ordinal),"every player fact rebuild reads goes through eorzea_hour");
            Check(tool.Contains("X-841: a player file with the Eorzea minute: the file and the common set brought to the hour, the rest unchanged",StringComparison.Ordinal)
                && tool.Contains("X-841: rebuilt again at the hour: nothing written",StringComparison.Ordinal),"the selftest's behaviour case");
            Check(tool.Contains("X-838: the repository and the temporary folder on two drives: rebuild still stops on Lossy",StringComparison.Ordinal)
                && !System.Text.RegularExpressions.Regex.IsMatch(tool,@"\{os\.path\.relpath\("),"X-838: no message builds its path with os.path.relpath");
        }),
    ];
}
