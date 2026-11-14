using System.IO.Compression;
using System.Text;
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
}
