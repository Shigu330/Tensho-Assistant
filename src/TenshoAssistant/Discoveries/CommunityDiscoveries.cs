using System.Security.Cryptography;
using System.Text;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-77 (0.882): sharing through Git what the parsers learned. 0.885 (user request: keep only the automatic exchange): only the GitHub exchange remains (GitHubExchange: the common
/// set community-discoveries/v3/ of main joined as imported, your own sightings sent to your gist, on the opt-in automatic exchange or « Exchange now »);
/// the zip-and-issue way and its buttons are gone. This class keeps the shared helpers: the set's address, hash, own sightings.
/// X-357 (0.1349, DEC-052): the v2 common file discoveries.json is gone. X-574 (0.1793, the user's choice of 17/11): so are Parse, Encode and
/// Decode, kept until then for the files of versions 1 and 2 (no longer read anywhere since 0.1349).
/// </summary>
public static class CommunityDiscoveries
{
    /// <summary>X-174 (0.1137, DEC-052): the folder of the v3 set (index.json and one kind-NN.jsonl.gz per kind, ExchangeV3), the only common file read since X-357 (0.1349).</summary>
    public const string AddressV3="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-discoveries/v3/";

    public static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

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
}
