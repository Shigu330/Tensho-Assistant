using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TenshoAssistant.Discoveries;

/// <summary>X-174 (0.1137, DEC-052): one kind file of a v3 set, as its index lists it: name, kind, facts, compressed size and SHA-256 of the compressed bytes.</summary>
public sealed class ExchangeV3Entry
{
    public string Name { get; set; }="";
    public DiscoveryKind Kind { get; set; }
    public int Count { get; set; }
    public long Bytes { get; set; }
    public string Sha256 { get; set; }="";
}

/// <summary>X-174: the index of a v3 set (index.json): the format, its version, when it was written and its kind files.</summary>
public sealed class ExchangeV3Index
{
    public string Format { get; set; }=ExchangeV3.FormatName;
    public int Version { get; set; }=ExchangeV3.Version;
    public DateTimeOffset Written { get; set; }
    public List<ExchangeV3Entry> Files { get; set; }=[];
}

/// <summary>X-174: the first line of a kind file.</summary>
public sealed class ExchangeV3Header
{
    public string Format { get; set; }="";
    public int Version { get; set; }
    public DiscoveryKind Kind { get; set; }
    public int Count { get; set; }
    public string Producer { get; set; }="";
    public DateTimeOffset Written { get; set; }
}

/// <summary>X-174: the facts of one kind file that hold, and how many lines were refused.</summary>
public sealed record ExchangeV3Kind(DiscoveryKind Kind,List<DiscoveryFact> Facts,int Refused);

/// <summary>X-174: a v3 set built from facts: index.json and its kind files (gzip bytes, by file name).</summary>
public sealed record ExchangeV3Set(string Index,IReadOnlyDictionary<string,byte[]> Files);

/// <summary>
/// X-174 (0.1137, DEC-052, user choice of 12/11 « option 3 »: formats changed before the public announcement): the exchange format v3 of the shared discoveries.
/// A set is <c>index.json</c> and one <c>kind-NN.jsonl.gz</c> per kind present: gzip of UTF-8 lines, line 1 a header (format, version 3, kind, count, producer,
/// written), then one fact per line with the names of <see cref="DiscoveryFact"/>, optional fields left out when empty, never <c>Own</c>, <c>Key</c> nor the
/// imported counts. Read as a stream and bounded: the compressed bytes (within the size the index gives, at most <see cref="MaxCompressedBytes"/>) checked
/// against the index's SHA-256 before use, then decompressed line by line (<see cref="MaxDecompressedBytes"/>, <see cref="MaxLineBytes"/>, at most
/// <see cref="DiscoveryStore.MaxFacts"/> facts); a bad line is refused and counted, a file whose refusals pass 5 % is refused whole, as is an unknown version
/// or a header of another kind. Written here for the gist (GitHubExchange) and by tools/discoveries-merge.py for the repository (community-discoveries/v3/).
/// </summary>
public static class ExchangeV3
{
    public const string FormatName="tensho-discoveries",IndexName="index.json",GistSuffix=".b64";
    public const int Version=3;
    public const int MaxCompressedBytes=32*1024*1024,MaxIndexBytes=1024*1024,MaxLineBytes=64*1024;
    public const long MaxDecompressedBytes=256L*1024*1024;

    public static string FileName(DiscoveryKind kind)=>$"kind-{(int)kind:00}.jsonl.gz";
    public static string Sha256(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));

    // A fact line holds the shared fields only: the computed Key, your own part (X-167) and the imported counts are not in its contract, so writing leaves
    // them out and a line that holds one (or any other unknown name) is refused when read.
    private static readonly string[] NotShared=["Key","Own","ImportedHq","ImportedValues"];
    private static readonly JsonSerializerOptions LineOptions=new()
    {
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver=new DefaultJsonTypeInfoResolver{Modifiers={info=>
        {
            if(info.Type!=typeof(DiscoveryFact))return;
            foreach(var property in info.Properties.Where(property=>NotShared.Contains(property.Name)).ToList())info.Properties.Remove(property);
        }}},
    };

    /// <summary>The set of these facts: one kind file per kind, facts in key order, dated by their newest day; the same facts give the same bytes.</summary>
    public static ExchangeV3Set Build(DiscoveryFile file)
    {
        var files=new SortedDictionary<string,byte[]>(StringComparer.Ordinal);
        var index=new ExchangeV3Index{Written=file.Written};
        foreach(var group in file.Facts.GroupBy(fact=>fact.Kind).OrderBy(group=>group.Key))
        {
            var facts=group.ToList();
            var bytes=WriteKind(group.Key,facts,file.Producer);
            var name=FileName(group.Key);files[name]=bytes;
            index.Files.Add(new(){Name=name,Kind=group.Key,Count=facts.Count,Bytes=bytes.Length,Sha256=Sha256(bytes)});
        }
        return new(JsonSerializer.Serialize(index),files);
    }

    /// <summary>The files of a set as a gist holds them (text only): index.json, and each kind file in base64 under its name and <see cref="GistSuffix"/>.</summary>
    public static SortedDictionary<string,string> GistFiles(ExchangeV3Set set)
    {
        var files=new SortedDictionary<string,string>(StringComparer.Ordinal){[IndexName]=set.Index};
        foreach(var (name,bytes) in set.Files)files[name+GistSuffix]=Convert.ToBase64String(bytes);
        return files;
    }

    /// <summary>One kind file: the header line, then each fact on its line, gzip.</summary>
    public static byte[] WriteKind(DiscoveryKind kind,IReadOnlyCollection<DiscoveryFact> facts,string producer)
    {
        var ordered=facts.OrderBy(fact=>fact.Key,StringComparer.Ordinal).ToList();
        var header=new ExchangeV3Header{Format=FormatName,Version=Version,Kind=kind,Count=ordered.Count,Producer=producer,
            Written=ordered.Count==0 ? DateTimeOffset.UnixEpoch:ordered.Max(fact=>fact.Last)};
        using var memory=new MemoryStream();
        using(var gzip=new GZipStream(memory,CompressionLevel.Optimal,true))
        {
            WriteLine(gzip,JsonSerializer.SerializeToUtf8Bytes(header));
            foreach(var fact in ordered)
            {
                var copy=fact.Copy();
                copy.Values=copy.Values?.OrderBy(pair=>pair.Key).ToDictionary(pair=>pair.Key,pair=>pair.Value);
                WriteLine(gzip,JsonSerializer.SerializeToUtf8Bytes(copy,LineOptions));
            }
        }
        return memory.ToArray();
    }
    private static void WriteLine(Stream stream,byte[] line){stream.Write(line);stream.WriteByte((byte)'\n');}

    /// <summary>
    /// The index of a set, or null when it is not one this version reads (another format or version, too large, a malformed entry). An entry of a kind this
    /// version does not know is left out (a newer set stays readable for the kinds known here).
    /// </summary>
    public static ExchangeV3Index? ParseIndex(string json)
    {
        try
        {
            if(Encoding.UTF8.GetByteCount(json)>MaxIndexBytes)return null;
            var index=JsonSerializer.Deserialize<ExchangeV3Index>(json);
            if(index is not { Format:FormatName, Version:Version, Files:not null })return null;
            var kinds=new HashSet<int>();var known=new List<ExchangeV3Entry>();
            foreach(var entry in index.Files)
            {
                if(entry is null || !kinds.Add((int)entry.Kind) || entry.Name!=FileName(entry.Kind) || entry.Count is < 0 or > DiscoveryStore.MaxFacts
                    || entry.Bytes is < 1 or > MaxCompressedBytes || entry.Sha256 is not { Length:64 } || !entry.Sha256.All(Uri.IsHexDigit))return null;
                if(Enum.IsDefined(entry.Kind))known.Add(entry);
            }
            index.Files=known;
            return index;
        }
        catch(JsonException){return null;}
    }

    /// <summary>
    /// One kind file read from <paramref name="compressed"/>: at most the size its index entry gives is read (more is refused at once, never read whole), its
    /// SHA-256 checked against the index, then its lines decompressed one at a time. Throws <see cref="InvalidDataException"/> when the file is refused.
    /// </summary>
    public static ExchangeV3Kind ReadKind(Stream compressed,ExchangeV3Entry entry,long maxDecompressed=MaxDecompressedBytes,int maxLine=MaxLineBytes)
    {
        var limit=Math.Min(entry.Bytes,MaxCompressedBytes);
        using var copy=new MemoryStream();
        var block=new byte[81920];int read;
        while((read=compressed.Read(block,0,block.Length))>0)
        {
            if(copy.Length+read>limit)throw new InvalidDataException("Shared discoveries file larger than its index says.");
            copy.Write(block,0,read);
        }
        var bytes=copy.ToArray();
        if(bytes.Length!=entry.Bytes)throw new InvalidDataException("Shared discoveries file of another size than its index says.");
        if(!string.Equals(Sha256(bytes),entry.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Shared discoveries file of another hash than its index says.");
        using var gzip=new GZipStream(new MemoryStream(bytes),CompressionMode.Decompress);
        return ReadLines(gzip,entry,maxDecompressed,maxLine);
    }

    private static ExchangeV3Kind ReadLines(Stream lines,ExchangeV3Entry entry,long maxDecompressed,int maxLine)
    {
        ExchangeV3Header? header=null;var facts=new List<DiscoveryFact>();int count=0,refused=0;
        void Take(byte[]? line)
        {
            if(header is null)
            {
                try{header=line is null ? null:JsonSerializer.Deserialize<ExchangeV3Header>(line);}catch(JsonException){header=null;}
                if(header is null || header.Format!=FormatName)throw new InvalidDataException("Shared discoveries file without its header.");
                if(header.Version!=Version)throw new InvalidDataException("Shared discoveries file of an unknown version.");
                if(header.Kind!=entry.Kind || header.Count!=entry.Count)throw new InvalidDataException("Shared discoveries file of another kind or count than its index says.");
                return;
            }
            if(++count>header.Count)throw new InvalidDataException("Shared discoveries file with more facts than its header says.");
            DiscoveryFact? fact;
            try{fact=line is null ? null:JsonSerializer.Deserialize<DiscoveryFact>(line,LineOptions);}catch(JsonException){fact=null;}
            if(fact is null || fact.Kind!=entry.Kind || !DiscoveryStore.Valid(fact)){refused++;return;}
            facts.Add(fact);
        }
        var buffer=new byte[64*1024];var current=new MemoryStream();var tooLong=false;long total=0;int read;
        while((read=lines.Read(buffer,0,buffer.Length))>0)
        {
            total+=read;
            if(total>maxDecompressed)throw new InvalidDataException("Shared discoveries file too large once decompressed.");
            var span=buffer.AsSpan(0,read);
            while(true)
            {
                var end=span.IndexOf((byte)'\n');
                var part=end<0 ? span:span[..end];
                if(!tooLong)
                {
                    if(current.Length+part.Length>maxLine){tooLong=true;current.SetLength(0);}
                    else current.Write(part);
                }
                if(end<0)break;
                Take(tooLong ? null:current.ToArray());
                current.SetLength(0);tooLong=false;
                span=span[(end+1)..];
            }
        }
        if(tooLong || current.Length>0)Take(tooLong ? null:current.ToArray());
        if(header is null)throw new InvalidDataException("Shared discoveries file without its header.");
        if(count!=header.Count)throw new InvalidDataException("Shared discoveries file with fewer facts than its header says.");
        if(refused*20>count)throw new InvalidDataException("Shared discoveries file with over 5 % of its lines refused.");
        return new(entry.Kind,facts,refused);
    }
}
