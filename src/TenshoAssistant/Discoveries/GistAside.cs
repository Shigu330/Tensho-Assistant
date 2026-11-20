using System.Text.Json.Nodes;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-621 (review of 0.1560, I1): the copies, on your own gist, of your facts whose rows were set aside as damaged (DiscoveryDatabase.SetAside),
/// read back before a send replaces the gist (GitHubExchange.KeepAside). The gist's listing (GitHub API) gives each file's text, or its raw
/// address when the text is cut (over 1 MB): only gist.githubusercontent.com is read then. Only the index and the kind files of those keys are
/// read, each checked as the common files are (ExchangeV3.ReadKind: size, hash, lines). A kind file that cannot be read keeps nothing of its
/// kind; an index this version does not read (another format) keeps nothing (InvalidDataException).
/// </summary>
public static class GistAside
{
    /// <summary>The listing of a gist holds each file's text up to 1 MB: at most this much is read.</summary>
    public const int MaxListingBytes=64*1024*1024;
    /// <summary>A kind file in base64, within the compressed bound of the exchange.</summary>
    public const int MaxRawBytes=(ExchangeV3.MaxCompressedBytes+2)/3*4+64;
    public const string RawHost="https://gist.githubusercontent.com/";

    /// <summary>The kind a fact key names (its first field, DiscoveryFact.Key), or null.</summary>
    public static DiscoveryKind? KindOf(string key)
    {
        var end=key.IndexOf(':');
        return end>0 && int.TryParse(key.AsSpan(0,end),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var kind)
            && Enum.IsDefined((DiscoveryKind)kind) ? (DiscoveryKind)kind:null;
    }

    /// <summary>The facts of <paramref name="keys"/> your gist holds: <paramref name="listing"/> gives the gist (GET gists/ID), <paramref name="raw"/> reads a cut file.</summary>
    public static async Task<List<DiscoveryFact>> Read(Func<Task<JsonNode?>> listing,Func<Uri,Task<string>> raw,IReadOnlyCollection<string> keys)
    {
        var wanted=new HashSet<string>(keys,StringComparer.Ordinal);
        var kinds=wanted.Select(KindOf).OfType<DiscoveryKind>().ToHashSet();
        var kept=new List<DiscoveryFact>();
        if(kinds.Count==0)return kept;
        if((await listing().ConfigureAwait(false))?["files"] is not JsonObject files)throw new InvalidDataException("A gist without files.");
        var index=await Text(files,ExchangeV3.IndexName,raw).ConfigureAwait(false) is { } json ? ExchangeV3.ParseIndex(json):null;
        if(index is null)throw new InvalidDataException("A gist without an index this version reads.");
        foreach(var entry in index.Files.Where(entry=>kinds.Contains(entry.Kind)))
        {
            ExchangeV3Kind read;
            try
            {
                var text=await Text(files,entry.Name+ExchangeV3.GistSuffix,raw).ConfigureAwait(false) ?? throw new InvalidDataException("A kind file missing from the gist.");
                read=ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(text.Trim())),entry);
            }
            catch(Exception error) when(error is InvalidDataException or FormatException){continue;}
            kept.AddRange(read.Facts.Where(fact=>wanted.Contains(fact.Key)));
        }
        return kept;
    }

    private static async Task<string?> Text(JsonObject files,string name,Func<Uri,Task<string>> raw)
    {
        if(files[name] is not JsonObject file)return null;
        var cut=file["truncated"] is JsonValue truncated && truncated.TryGetValue<bool>(out var value) && value;
        if(!cut && file["content"] is JsonValue content && content.TryGetValue<string>(out var text))return text;
        if(file["raw_url"] is not JsonValue address || !address.TryGetValue<string>(out var url) || !url.StartsWith(RawHost,StringComparison.Ordinal))
            throw new InvalidDataException("A gist file without an expected address.");
        return await raw(new Uri(url)).ConfigureAwait(false);
    }
}
