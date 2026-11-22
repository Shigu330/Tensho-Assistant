using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// X-357 (0.1349): the repository's common discoveries, read from the v3 set community-discoveries/v3/ (the v2 discoveries.json is gone).
// Not a test class (no Cases): the helper of the tests that read the published common facts.
internal static class RepositoryCommon
{
    private static string Folder=>Path.GetFullPath("community-discoveries/v3");
    private static ExchangeV3Index Index()=>ExchangeV3.ParseIndex(File.ReadAllText(Path.Combine(Folder,ExchangeV3.IndexName)))
        ?? throw new InvalidDataException("the repository's v3 index does not read");

    /// <summary>Every fact of the set, read as the plugin reads it (hash, size and bounds checked); a refused line throws.</summary>
    public static List<DiscoveryFact> Facts()
    {
        var facts=new List<DiscoveryFact>();
        foreach(var entry in Index().Files)
        {
            using var stream=File.OpenRead(Path.Combine(Folder,entry.Name));
            var kind=ExchangeV3.ReadKind(stream,entry);
            if(kind.Refused>0)throw new InvalidDataException(entry.Name+": "+kind.Refused+" lines refused");
            facts.AddRange(kind.Facts);
        }
        return facts;
    }

    /// <summary>Every fact line as written in the files (the header line left out), to check the names and texts as published.</summary>
    public static List<(string File,string Line)> FactLines()
    {
        var lines=new List<(string,string)>();
        foreach(var entry in Index().Files)
        {
            using var reader=new StreamReader(new GZipStream(File.OpenRead(Path.Combine(Folder,entry.Name)),CompressionMode.Decompress),Encoding.UTF8);
            reader.ReadLine();
            for(var line=reader.ReadLine();line is not null;line=reader.ReadLine())if(line.Length>0)lines.Add((entry.Name,line));
        }
        return lines;
    }

    /// <summary>X-772 (0.1801, D-217): the player files of community-discoveries/players, as the merge tool reads them: <c>&lt;issue&gt;.jsonl.gz</c>
    /// (gzip of a header line, then one fact per line, without place nor day), or an <c>&lt;issue&gt;.json</c> of before (v2 JSON, its facts with
    /// place and days): (file, header or null for a file of before, the facts as JSON).</summary>
    /// X-826 (0.1827): folder: another players/ folder (files the merge tool has just written), the repository's by default.
    public static List<(string File,JsonElement? Header,List<JsonElement> Facts)> Players(string? folder=null)
    {
        var players=new List<(string,JsonElement?,List<JsonElement>)>();
        foreach(var path in Directory.GetFiles(folder ?? Path.GetFullPath("community-discoveries/players")).Order(StringComparer.Ordinal))
        {
            var name=Path.GetFileName(path);
            if(name.EndsWith(".jsonl.gz",StringComparison.Ordinal))
            {
                using var reader=new StreamReader(new GZipStream(File.OpenRead(path),CompressionMode.Decompress),Encoding.UTF8);
                var header=JsonDocument.Parse(reader.ReadLine() ?? "null").RootElement.Clone();
                var facts=new List<JsonElement>();
                for(var line=reader.ReadLine();line is not null;line=reader.ReadLine())if(line.Length>0)facts.Add(JsonDocument.Parse(line).RootElement.Clone());
                players.Add((name,header,facts));
            }
            else if(name.EndsWith(".json",StringComparison.Ordinal))
                players.Add((name,null,JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("Facts").EnumerateArray().Select(fact=>fact.Clone()).ToList()));
        }
        return players;
    }
}
