using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-77 (0.882, user request and agreement « Oui, ce schéma », 2026-11-08: share through Git what the parsers learned and take what others found). Two ways,
/// each on a click only, no account and no token in the plugin (Dalamud rules: nothing sent without the player's gesture):
/// - take: the common file published in the repository (community-discoveries/discoveries.json on main) is downloaded and joined to the base as imported
///   (DiscoveryStore.Merge: nothing of yours is removed); the same file is joined once (its hash is kept);
/// - give: the facts you saw yourself (Seen &gt; 0) are written to a zip and GitHub's « new issue » page opens in the browser; the player attaches the zip there.
///   The maintainer checks and merges it into the common file (tools/discoveries-merge.py). Facts never hold a player's name, position or id.
/// </summary>
public static class CommunityDiscoveries
{
    public const string Address="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-discoveries/discoveries.json";
    public const string IssueAddress="https://github.com/Shigu330/Tensho-Assistant/issues/new?template=discoveries.yml";
    public const int MaxBytes=32*1024*1024;

    public static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static DiscoveryFile? Parse(string json)
    {
        var file=JsonSerializer.Deserialize<DiscoveryFile>(json);
        return file is { Version:DiscoveryFile.CurrentVersion, Facts:not null } ? file:null;
    }

    /// <summary>Only what you saw yourself: imported facts are not sent back.</summary>
    public static DiscoveryFile Mine(DiscoveryFile all)=>new(){Written=all.Written,Producer=all.Producer,
        Facts=all.Facts.Where(fact=>fact.Seen>0).Select(fact=>{var copy=fact.Copy();copy.Imported=0;return copy;}).ToList()};

    public static string ShareName(DateTimeOffset at)=>$"Tensho-discoveries-{at.ToUniversalTime():yyyyMMdd-HHmmss}.zip";

    public static void WriteShare(string path,DiscoveryFile file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+".tmp";
        using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
        {
            var entry=zip.CreateEntry("discoveries.json",CompressionLevel.Optimal);
            using var stream=entry.Open();
            JsonSerializer.Serialize(stream,file);
        }
        File.Move(temporary,path,true);
    }
}
