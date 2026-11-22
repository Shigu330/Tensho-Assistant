using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-804 (user decision of 22/11 around 14 h 35): what the plugin knows of its gist's merges (github-merged.json beside github.json, shared by the
/// clients of this profile under github.rotate.lock): the issue it is about, the last upload (its gist, its revision, each fact's hash), the facts
/// known in the common base (merged: their hash when merged), a merged gist still to delete, and whether the gist was deleted after a merge
/// (the next gist then keeps the issue and the issue is edited to link it).
/// </summary>
public sealed class GistRotationState
{
    public int Issue { get; set; }
    public string SentGist { get; set; }="";
    public string SentRevision { get; set; }="";
    public Dictionary<string,string> Sent { get; set; }=[];
    public Dictionary<string,string> Merged { get; set; }=[];
    public string OldGist { get; set; }="";
    /// <summary>X-820 M1 (review of 0.1819): the GitHub account whose gist OldGist is: only that account can delete it (another one is answered 404).</summary>
    public string OldLogin { get; set; }="";
    public bool Rotated { get; set; }
    /// <summary>The account, gist, its files and last upload's hash when this state was written (Known): the clients of the profile take them
    /// from here, never from an older github.json another client wrote outside the rotation lock.</summary>
    public bool Known { get; set; }
    public string Login { get; set; }="";
    public string Gist { get; set; }="";
    public List<string> GistFiles { get; set; }=[];
    public string UploadHash { get; set; }="";
}

/// <summary>
/// X-804 (user decision of 22/11 around 14 h 35, after the risks were explained): the gist a player's issue links keeps their sightings with
/// the exact place and day; once the merge tool has put them in the common base, it is deleted. The merge tool notes in the player file of the
/// repository (players/&lt;issue&gt;.jsonl.gz, header « Gist » and « Revision ») the gist revision it merged; the plugin reads that header from
/// main without any account, compares it with its last upload, and when they match: the facts of that upload are known merged, the gist is
/// deleted whole (not only emptied: GitHub keeps a gist's old revisions), and a new gist holds only what is new (a new fact, or one whose
/// levels, values, HQ sightings or days changed since). Pure: the requests are GitHubExchange's.
/// </summary>
public static class GistRotation
{
    public const string FileName="github-merged.json";
    /// <summary>The player files of the repository, read from main like the common set (CommunityDiscoveries.AddressV3).</summary>
    public const string NotesAddress="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-discoveries/players/";
    /// <summary>A player file read within this size (the merge tool's MAX_COMPRESSED); its header line within this many bytes.</summary>
    public const int MaxNoteFileBytes=32*1024*1024,MaxHeaderBytes=64*1024;
    private const string NoteFormat="tensho-discoveries-player";

    /// <summary>A fact's hash as the gist would carry it (any change of what is sent changes it).</summary>
    public static string Hash(DiscoveryFact fact)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fact,TenshoJson.Plain))))[..32];
    public static Dictionary<string,string> Hashes(IEnumerable<DiscoveryFact> facts)
    {
        var hashes=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var fact in facts)hashes[fact.Key]=Hash(fact);
        return hashes;
    }

    /// <summary>The gist and revision a player file notes as merged (its header line), or null: no note, or a file this version cannot read.</summary>
    public static (string Gist,string Revision)? Note(byte[] compressed)
    {
        try
        {
            using var stream=new GZipStream(new MemoryStream(compressed),CompressionMode.Decompress);
            var line=new List<byte>();
            for(var read=stream.ReadByte();read>=0 && read!='\n';read=stream.ReadByte())
            {
                if(line.Count>=MaxHeaderBytes)return null;
                line.Add((byte)read);
            }
            using var document=JsonDocument.Parse(line.ToArray());
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("Format",out var format) || format.ValueKind!=JsonValueKind.String || format.GetString()!=NoteFormat)return null;
            if(root.TryGetProperty("Gist",out var gist) && gist.ValueKind==JsonValueKind.String && gist.GetString() is { Length:>0 } id
                && root.TryGetProperty("Revision",out var revision) && revision.ValueKind==JsonValueKind.String && revision.GetString() is { Length:>0 } version)
                return (id,version);
            return null;
        }
        catch(Exception error) when(error is InvalidDataException or JsonException or IOException){return null;}
    }

    /// <summary>Whether the player file is worth asking: a gist and an issue, and an upload of that gist whose revision is known.</summary>
    public static bool Asks(GistRotationState state,string gist,int issue)=>gist.Length>0 && issue>0 && state.Issue==issue && state.SentGist==gist && state.SentRevision.Length>0;

    /// <summary>The merge tool merged the last upload: the same gist and the same revision.</summary>
    public static bool Confirmed((string Gist,string Revision)? note,GistRotationState state,string gist)
        =>note is { } merged && gist.Length>0 && state.SentGist==gist && state.SentRevision.Length>0 && merged.Gist==gist && merged.Revision==state.SentRevision;

    /// <summary>After a confirmed merge: what was sent is known merged; that gist is to delete (by <paramref name="login"/>, its account); the next gist keeps the issue.</summary>
    public static GistRotationState AfterMerge(GistRotationState state,string login="")
    {
        var merged=new Dictionary<string,string>(state.Merged,StringComparer.Ordinal);
        foreach(var (key,hash) in state.Sent)merged[key]=hash;
        return new(){Issue=state.Issue,Merged=merged,OldGist=state.SentGist,OldLogin=login,Rotated=true};
    }

    /// <summary>X-820 M1: whether the account connected now may delete the merged gist still to delete (its own; an older state that does not name the account: tried).</summary>
    public static bool Deletes(GistRotationState state,string login)=>state.OldLogin.Length==0 || string.Equals(state.OldLogin,login,StringComparison.OrdinalIgnoreCase);

    /// <summary>The facts to send: those the common base does not hold as they are (new, or changed since they were merged).</summary>
    public static List<DiscoveryFact> News(IEnumerable<DiscoveryFact> mine,IReadOnlyDictionary<string,string> merged)
        =>[..mine.Where(fact=>!merged.TryGetValue(fact.Key,out var hash) || hash!=Hash(fact))];

    /// <summary>The state as written (null: none, or unreadable).</summary>
    public static GistRotationState? Load(string path)
    {
        GistRotationState? state;
        try{state=File.Exists(path) ? JsonSerializer.Deserialize<GistRotationState>(File.ReadAllText(path),TenshoJson.Plain):null;}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException){return null;}
        if(state is null)return null;
        state.Sent??=[];state.Merged??=[];state.SentGist??="";state.SentRevision??="";state.OldGist??="";state.OldLogin??="";state.Login??="";state.Gist??="";state.GistFiles??=[];state.UploadHash??="";
        return state;
    }
    /// <summary>The state of this issue: another issue (a new one, another account) starts with nothing known merged, a gist still to delete kept.</summary>
    public static GistRotationState Read(string path,int issue)
    {
        var state=Load(path) ?? new(){Issue=issue};
        return state.Issue==issue ? state:new(){Issue=issue,OldGist=state.OldGist,OldLogin=state.OldLogin};
    }
    public static void Write(string path,GistRotationState state)=>AtomicFile.Write(path,JsonSerializer.Serialize(state,TenshoJson.Plain));
}
