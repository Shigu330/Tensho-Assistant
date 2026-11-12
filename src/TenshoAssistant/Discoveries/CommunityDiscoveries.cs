using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-77 (0.882): sharing through Git what the parsers learned. 0.885 (user request: keep only the automatic exchange): only the GitHub exchange remains (GitHubExchange: the common
/// file community-discoveries/discoveries.json of main joined as imported, your own sightings sent to your gist, on the opt-in automatic exchange or « Exchange now »);
/// the zip-and-issue way and its buttons are gone. This class keeps the shared helpers: the file's address, hash, parsing, own sightings, gist encoding.
/// </summary>
public static class CommunityDiscoveries
{
    public const string Address="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-discoveries/discoveries.json";
    public const int MaxBytes=32*1024*1024;

    public static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static DiscoveryFile? Parse(string json)
    {
        // X-76 E1 (0.916): versions 1 and 2 (DiscoveryFile.Readable).
        var file=JsonSerializer.Deserialize<DiscoveryFile>(json);
        return DiscoveryFile.Readable(file) ? file:null;
    }

    /// <summary>
    /// Only what you saw yourself: imported facts are not sent back. 0.883: dated by its newest fact, so that the same facts give the same file (and hash).
    /// X-76 E1 (0.916): nor the imported HQ sightings and values (written as version 2).
    /// </summary>
    public static DateTimeOffset Day(DateTimeOffset at)=>new(at.UtcDateTime.Date,TimeSpan.Zero);
    public static DiscoveryFile Mine(DiscoveryFile all)
    {
        // X-76 step 5 (0.949, user's go-ahead): what leaves the PC carries the day only, never the time of a sighting. X-167 (0.1134, audit R03): a fact
        // that mixes yours with an import's sends your own dates, levels and place only (DiscoveryFact.Own), never the import's; a copy that would not
        // hold (DiscoveryStore.Valid) is not sent.
        var facts=all.Facts.Where(fact=>fact.Seen>0).Select(fact=>
            {
                var copy=fact.Copy();
                if(copy.Own is { } own){copy.First=own.First;copy.Last=own.Last;copy.LevelMin=own.LevelMin;copy.LevelMax=own.LevelMax;copy.X=own.X;copy.Z=own.Z;}
                copy.Own=null;copy.Imported=0;copy.ImportedHq=0;copy.ImportedValues=null;
                return copy;
            })
            .Where(DiscoveryStore.Valid).Select(copy=>{copy.First=Day(copy.First);copy.Last=Day(copy.Last);return copy;})
            .OrderBy(fact=>fact.Key,StringComparer.Ordinal).ToList();
        return new(){Written=facts.Count==0 ? DateTimeOffset.UnixEpoch:facts.Max(fact=>fact.Last),Producer=all.Producer,Facts=facts};
    }

    /// <summary>X-77.1 (0.883): the file as one line of text for a gist: JSON, gzip, base64.</summary>
    public static string Encode(DiscoveryFile file)
    {
        using var memory=new MemoryStream();
        using(var gzip=new GZipStream(memory,CompressionLevel.Optimal,true))JsonSerializer.Serialize(gzip,file);
        return Convert.ToBase64String(memory.ToArray());
    }
    public static DiscoveryFile? Decode(string text)
    {
        using var memory=new MemoryStream(Convert.FromBase64String(text.Trim()));
        using var gzip=new GZipStream(memory,CompressionMode.Decompress);
        using var reader=new StreamReader(gzip);
        var buffer=new char[MaxBytes/2];var read=reader.ReadBlock(buffer,0,buffer.Length);
        if(!reader.EndOfStream)throw new InvalidDataException("Shared discoveries too large.");
        return Parse(new string(buffer,0,read));
    }
}
