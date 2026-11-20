using System.Net;
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

    /// <summary>
    /// X-640 (bench X-631, review of 0.1564 M1; user decision D-190 a confirmed on 20/11): a cut file's raw address refused durably (any 4xx but
    /// 408 and 429: the gist's revision gone, a file removed, access refused) at this many exchanges in a row, the send no longer waits for that
    /// copy (GitHubExchange.KeepAside): the refused files are skipped at the last try, their facts not kept, and the player is told. Before, any
    /// refusal there was « not reached » and the send stayed blocked (« ShareAsideWaiting ») with no limit and no cause given.
    /// </summary>
    public const int RefusalLimit=3;
    /// <summary>X-640: a status of the raw address that says the file will not come back by asking again (not a timeout, not a rate limit).</summary>
    public static bool Durable(HttpStatusCode status)=>(int)status is >=400 and <500 && status is not (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests);
    /// <summary>X-640: a cut file refused durably at its raw address (<see cref="Durable"/>).</summary>
    public sealed class RawRefusedException(HttpStatusCode status,Exception? inner=null):Exception($"A cut gist file refused by GitHub ({(int)status}).",inner)
    {
        public HttpStatusCode Status{get;}=status;
    }
    /// <summary>
    /// X-640: <paramref name="raw"/>, with a durable refusal turned into an unreadable file (InvalidDataException): a kind file refused keeps
    /// nothing of its kind, an index refused keeps nothing (<see cref="Read"/>); <paramref name="skipped"/> is called for each.
    /// </summary>
    public static Func<Uri,Task<string>> SkipRefused(Func<Uri,Task<string>> raw,Action skipped)=>async address=>
    {
        try{return await raw(address).ConfigureAwait(false);}
        catch(RawRefusedException error){skipped();throw new InvalidDataException(error.Message,error);}
    };

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
