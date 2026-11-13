using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TenshoAssistant.Discoveries;

public sealed record ExchangeMessage(string Key,params string[] Args);
/// <summary>What the exchange keeps beside the base (github.json): the account's login, its gist, its issue, the last upload's hash and the last exchange.</summary>
public sealed class GitHubExchangeMeta
{
    public string Login { get; set; }="";
    public string GistId { get; set; }="";
    public int Issue { get; set; }
    public string UploadHash { get; set; }="";
    public DateTimeOffset? Exchanged { get; set; }
    /// <summary>X-174 (0.1137): the files the last upload put in the gist (empty: a gist of before 0.1137, holding the v2 file only).</summary>
    public List<string> GistFiles { get; set; }=[];
    /// <summary>X-175 (1138): the client id that gave the token (empty: before 1138, the OAuth app); a token of another sign-in is forgotten.</summary>
    public string ClientId { get; set; }="";
    /// <summary>X-175: when the token and its refresh token expire (null: never, the OAuth app's tokens).</summary>
    public DateTimeOffset? TokenExpires { get; set; }
    public DateTimeOffset? RefreshExpires { get; set; }
    /// <summary>X-181 (0.1171, night review N30): the login that owns the gist and the issue (empty: not known, before 0.1171).</summary>
    public string GistLogin { get; set; }="";
}

/// <summary>X-175 (1138): an exchange stopped with a message of its own (App not installed, sign-in expired), the token forgotten when <paramref name="Forget"/>.</summary>
internal sealed class ExchangeRefused(string key,bool forget):Exception(key)
{
    public string Key{get;}=key;
    public bool Forget{get;}=forget;
}

/// <summary>
/// X-77.1 (0.883, user choice: a GitHub sign-in, and the automatic exchange uploads too: yes, opt-in). GitHub's device flow (OAuth App of the repository,
/// client id below, not a secret; no client secret in the plugin): Tensho shows a code, the player approves it on github.com; the token is kept on this PC only,
/// protected by Windows for this user (DPAPI), and can be revoked on GitHub at any time. An exchange: the common file of the repository joined as imported
/// (once per version of the file), then, when connected, the player's own sightings (never imported ones) written to one secret gist of theirs, updated each
/// time, and one issue per player in the repository pointing to it. Started by a click, or once a day by the opt-in setting « automatic exchange ».
/// A discovery never holds a player's name, position or id; the issue's title holds the GitHub login of the account the player connected.
/// X-175 (1138): the sign-in (OAuth app today, GitHub App once its client id is given) comes from GitHubSignIn, the one switch; an expiring token
/// is renewed before an upload with its refresh token, kept protected the same way (github.refresh).
/// </summary>
public sealed class GitHubExchange : IDisposable
{
    public const string Repository="Shigu330/Tensho-Assistant",GistFile="tensho-discoveries.b64";
    /// <summary>X-175: the files that hold a secret, never put in a backup copy.</summary>
    public static readonly IReadOnlySet<string> SecretFiles=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"github.token","github.refresh"};
    public static readonly TimeSpan Every=TimeSpan.FromHours(20),Retry=TimeSpan.FromMinutes(15);
    private readonly string folder;
    private readonly HttpClient client;
    private readonly GitHubSignIn signIn;
    private readonly Func<DateTimeOffset> clock;
    private readonly CancellationTokenSource stop=new();
    private readonly object gate=new(),marks=new();
    private string? token,refresh;private GitHubExchangeMeta meta=new();private DateTimeOffset tried;private bool busy;
    // X-181 (0.1171, night review N2, N7, N8): Disconnect changes the generation: a work started before keeps nothing and sends nothing after it.
    // The secrets are written under their own gate (never after a Disconnect removed them); a write that failed is tried again (the pair stays in
    // memory). A renewal runs under a lock file shared by every client of this profile, not tied to the plugin's stop.
    private int generation,workGeneration;private bool secretsUnsaved;private Task? refreshing;
    private readonly object secretsGate=new();
    /// <summary>X-181: the longest wait for the other client's renewal (beyond the renewal's own deadline).</summary>
    internal TimeSpan LockWait { get; init; }=TimeSpan.FromSeconds(45);
    internal static readonly TimeSpan RefreshDeadline=TimeSpan.FromSeconds(30);
    /// <summary>X-175: the shortest wait between two polls of the device flow (GitHub asks 5 seconds; the behaviour tests wait less).</summary>
    internal TimeSpan PollFloor { get; init; }=TimeSpan.FromSeconds(5);
    private string? userCode,verifyAddress;private ExchangeMessage? message;

    public bool Busy{get{lock(gate)return busy;}}
    public string? UserCode{get{lock(gate)return userCode;}}
    public string? VerifyAddress{get{lock(gate)return verifyAddress;}}
    public ExchangeMessage? Message{get{lock(gate)return message;}}
    public bool Connected{get{lock(gate)return token is not null;}}
    public string Login{get{lock(gate)return meta.Login;}}
    public DateTimeOffset? Exchanged{get{lock(gate)return meta.Exchanged;}}
    private string TokenPath=>Path.Combine(folder,"github.token");
    private string RefreshPath=>Path.Combine(folder,"github.refresh");
    private string MetaPath=>Path.Combine(folder,"github.json");
    private string LockPath=>Path.Combine(folder,"github.lock");
    private string CommonMarker=>Path.Combine(folder,"community.sha256");
    /// <summary>X-174 (0.1137): the hash of each v3 kind file taken, by kind number ({"10":"HEX",…}), noted once a save holds its facts.</summary>
    private string CommonV3Marker=>Path.Combine(folder,"community-v3.json");

    public GitHubExchange(string folder):this(folder,null){}
    /// <summary>
    /// X-174: <paramref name="handler"/> answers the requests instead of the network (behaviour tests). X-175: <paramref name="signIn"/> (default
    /// GitHubSignIn.Current) and <paramref name="clock"/> (default the UTC time) given by the behaviour tests.
    /// </summary>
    internal GitHubExchange(string folder,HttpMessageHandler? handler,GitHubSignIn? signIn=null,Func<DateTimeOffset>? clock=null)
    {
        this.folder=folder;
        this.signIn=signIn ?? GitHubSignIn.Current;
        this.clock=clock ?? (()=>DateTimeOffset.UtcNow);
        client=handler is null ? new(){Timeout=TimeSpan.FromSeconds(30)}:new(handler){Timeout=TimeSpan.FromSeconds(30)};
        // X-76 H10 (0.923): an unreadable file is kept aside as « .broken » before a new one is started (it was written over later).
        try{if(File.Exists(MetaPath))meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(MetaPath)) ?? new();}
        catch(Exception){meta=new();try{File.Move(MetaPath,MetaPath+".broken",true);}catch(Exception){}}
        try{if(File.Exists(TokenPath))token=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(TokenPath)));}catch(Exception){token=null;}
        try{if(token is not null && File.Exists(RefreshPath))refresh=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(RefreshPath)));}catch(Exception){refresh=null;}
        // X-175: a token of the other sign-in (the OAuth app's, once the GitHub App is in place) is forgotten: the player connects again, to the App.
        if(token is not null && !this.signIn.Owns(meta.ClientId)){Disconnect();message=new("ExchangeSignInChanged");}
    }

    /// <summary>The automatic exchange is due: none for <see cref="Every"/>, and no attempt for <see cref="Retry"/> (a failure is not retried every second).</summary>
    public bool Due(DateTimeOffset now){lock(gate)return !busy && now-tried>=Retry && (meta.Exchanged is not { } last || now-last>=Every);}

    private bool Start(Func<Task<ExchangeMessage>> work)
    {
        lock(gate){if(busy)return false;busy=true;message=null;tried=DateTimeOffset.UtcNow;workGeneration=generation;}
        _=Task.Run(async()=>
        {
            ExchangeMessage result;
            try{result=await work().ConfigureAwait(false);}
            catch(OperationCanceledException){result=new("ExchangeStopped");}
            catch(ExchangeRefused refused){if(refused.Forget)Disconnect();result=new(refused.Key);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized){ForgetIfStill(null);result=new("ExchangeUnauthorized");}
            catch(Exception error){result=new("ExchangeFailed",error.GetType().Name);}
            lock(gate){message=result;busy=false;userCode=null;}
        });
        return true;
    }

    public bool Connect()=>Start(ConnectAsync);
    public bool Exchange(DiscoveryStore store,bool upload)=>Start(()=>ExchangeAsync(store,upload));

    /// <summary>Forgets the token on this PC, and its refresh token (the authorization itself is revoked on github.com › Settings › Applications).</summary>
    public void Disconnect()
    {
        lock(secretsGate)
        {
            lock(gate){generation++;token=null;refresh=null;secretsUnsaved=false;meta.Login="";meta.ClientId="";meta.TokenExpires=null;meta.RefreshExpires=null;}
            try{File.Delete(TokenPath);}catch(Exception){}
            try{File.Delete(RefreshPath);}catch(Exception){}
        }
        SaveMeta();
    }

    /// <summary>
    /// X-181 (0.1171, night review N2): a refusal forgets the sign-in only when the files still hold what this client used (the refresh token
    /// refused, or with none given, the token); another client that renewed meanwhile left a new pair: it is taken, never removed.
    /// </summary>
    private void ForgetIfStill(string? usedRefresh)
    {
        string? used;lock(gate)used=usedRefresh is null ? token:usedRefresh;
        var disk=ReadSecrets();
        if(disk is { } found && (usedRefresh is null ? found.Token!=used:found.Refresh!=used)){Adopt(found);return;}
        Disconnect();
    }

    private sealed record Secrets(string Token,string? Refresh,DateTimeOffset? Expires,DateTimeOffset? RefreshExpires,string ClientId);
    /// <summary>X-181: the pair as this profile's files hold it now (another client may have renewed it), or null without a token file.</summary>
    private Secrets? ReadSecrets()
    {
        try
        {
            if(!File.Exists(TokenPath))return null;
            var access=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(TokenPath)));
            var renewal=File.Exists(RefreshPath) ? Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(RefreshPath))):null;
            var saved=File.Exists(MetaPath) ? JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(MetaPath)):null;
            return new(access,renewal,saved?.TokenExpires,saved?.RefreshExpires,saved?.ClientId ?? "");
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException or System.ComponentModel.Win32Exception or System.Security.Cryptography.CryptographicException){return null;}
    }
    private void Adopt(Secrets found)
    {
        lock(gate){token=found.Token;refresh=found.Refresh;meta.TokenExpires=found.Expires;meta.RefreshExpires=found.RefreshExpires;if(found.ClientId.Length>0)meta.ClientId=found.ClientId;}
    }

    /// <summary>X-181: the lock file shared by every client of this profile around a renewal (one renewal at a time); IOException past <see cref="LockWait"/>.</summary>
    private async Task<FileStream> HoldLock()
    {
        Directory.CreateDirectory(folder);
        var until=DateTime.UtcNow+LockWait;
        while(true)
        {
            try{return new FileStream(LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(DateTime.UtcNow<until){await Task.Delay(50).ConfigureAwait(false);}
        }
    }

    /// <summary>
    /// X-175: keeps a token on this PC, protected for this Windows user (DPAPI). The refresh token first: GitHub ends the old one at each refresh, so a
    /// stop between the two writes leaves the new refresh token beside an old access token, renewed again at the next use (never the reverse).
    /// </summary>
    /// <remarks>X-181 (0.1171, night review N7, N8, N30): nothing kept when the player disconnected since the work began (false); the pair in memory
    /// first, then written (a write that fails is tried again before the next use, the pair is never lost); another login than the gist's owner
    /// starts a gist and an issue of its own.</remarks>
    private bool Keep(GitHubToken granted,int started,string? login=null)
    {
        lock(gate)
        {
            if(started!=generation)return false;
            token=granted.Access;refresh=granted.Refresh;secretsUnsaved=true;
            meta.ClientId=signIn.ClientId;meta.TokenExpires=granted.Expires;meta.RefreshExpires=granted.RefreshExpires;
            if(login is not null)
            {
                meta.Login=login;
                if(meta.GistLogin.Length>0 && login.Length>0 && !string.Equals(meta.GistLogin,login,StringComparison.OrdinalIgnoreCase))
                {meta.GistId="";meta.Issue=0;meta.GistFiles=[];meta.UploadHash="";meta.GistLogin="";}
            }
        }
        WriteSecrets();
        SaveMeta();
        return true;
    }
    /// <summary>X-181: the pair in memory written to the files (refresh token first), unless a Disconnect came since; false when the write failed.</summary>
    private bool WriteSecrets()
    {
        lock(secretsGate)
        {
            string? access,renewal;
            lock(gate){if(!secretsUnsaved || token is null)return true;access=token;renewal=refresh;}
            try
            {
                Directory.CreateDirectory(folder);
                if(renewal is not null)AtomicFile.Write(RefreshPath,UserSecret.Protect(Encoding.UTF8.GetBytes(renewal)));
                else if(File.Exists(RefreshPath))File.Delete(RefreshPath);
                AtomicFile.Write(TokenPath,UserSecret.Protect(Encoding.UTF8.GetBytes(access)));
                lock(gate)if(token==access && refresh==renewal)secretsUnsaved=false;
                return true;
            }
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){return false;}
        }
    }

    /// <summary>
    /// X-175: an expiring token (GitHub App) is renewed before use with its refresh token. No refresh token, one past its end, or a refresh refused
    /// by GitHub: the sign-in has ended, the token is forgotten and the player is asked to connect again. GitHub not reached: the exchange fails, the
    /// token is kept for the next try.
    /// </summary>
    /// <remarks>X-181 (0.1171, night review N2, N8, N29): under the profile's lock file, the files read again first: a pair renewed by another client
    /// is taken without asking GitHub; a refusal forgets the sign-in only when the files still hold the refresh token refused. The request has its
    /// own deadline, not the plugin's stop: a renewal GitHub accepted is kept even when the plugin unloads meanwhile (Dispose waits for it a little).
    /// <paramref name="force"/>: renewed even when not due (a 401 with a refresh token in hand).</remarks>
    private async Task Fresh(bool force=false)
    {
        string? renewal;DateTimeOffset? expires,ends;int started;
        lock(gate){renewal=refresh;expires=meta.TokenExpires;ends=meta.RefreshExpires;started=workGeneration;}
        if(!force && !GitHubSignIn.Due(expires,clock()))return;
        using var held=await HoldLock().ConfigureAwait(false);
        if(ReadSecrets() is { } disk && disk.Refresh is not null && disk.Refresh!=renewal)
        {
            Adopt(disk);
            if(!GitHubSignIn.Due(disk.Expires,clock()))return;
            (renewal,ends)=(disk.Refresh,disk.RefreshExpires);
        }
        lock(gate)if(started!=generation)throw new OperationCanceledException("Disconnected.");
        if(renewal is null || GitHubSignIn.Ended(ends,clock())){ForgetIfStill(renewal);throw new ExchangeRefused("ExchangeExpired",false);}
        var work=RenewAndKeep(renewal,started);
        lock(gate)refreshing=work;
        bool? kept;
        try{kept=await work.ConfigureAwait(false);}
        finally{lock(gate)refreshing=null;}
        if(kept is null){ForgetIfStill(renewal);throw new ExchangeRefused("ExchangeExpired",false);}
        if(kept==false)throw new OperationCanceledException("Disconnected.");
    }
    // The renewal asked and its pair kept, as one task Dispose can wait for: true kept, false disconnected meanwhile, null refused by GitHub.
    private async Task<bool?> RenewAndKeep(string renewal,int started)
    {
        JsonObject answer;
        using(var deadline=new CancellationTokenSource(RefreshDeadline))
            try{answer=await Form("https://github.com/login/oauth/access_token",signIn.RefreshFields(renewal),deadline.Token).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden){return null;}
        return GitHubSignIn.ReadToken(answer,clock()) is { } granted ? Keep(granted,started):null;
    }

    private async Task<ExchangeMessage> ConnectAsync()
    {
        var code=await Form("https://github.com/login/device/code",signIn.DeviceCodeFields()).ConfigureAwait(false);
        var device=code["device_code"]?.GetValue<string>() ?? throw new InvalidDataException("No device code.");
        var interval=TimeSpan.FromSeconds(Math.Max(PollFloor.TotalSeconds,code["interval"]?.GetValue<int>() ?? 5));
        var expires=DateTimeOffset.UtcNow.AddSeconds(Math.Min(900,code["expires_in"]?.GetValue<int>() ?? 900));
        lock(gate){userCode=code["user_code"]?.GetValue<string>();verifyAddress=code["verification_uri"]?.GetValue<string>() ?? "https://github.com/login/device";}
        while(DateTimeOffset.UtcNow<expires)
        {
            await Task.Delay(interval,stop.Token).ConfigureAwait(false);
            var answer=await Form("https://github.com/login/oauth/access_token",signIn.PollFields(device)).ConfigureAwait(false);
            if(GitHubSignIn.ReadToken(answer,clock()) is { } granted)
            {
                var user=await Api(HttpMethod.Get,"https://api.github.com/user",null,granted.Access).ConfigureAwait(false);
                int started;lock(gate)started=workGeneration;
                if(!Keep(granted,started,user?["login"]?.GetValue<string>() ?? ""))return new("ExchangeStopped");
                return new("ConnectDone",Login);
            }
            switch(answer["error"]?.GetValue<string>())
            {
                case "authorization_pending":continue;
                case "slow_down":interval+=TimeSpan.FromSeconds(5);continue;
                case "access_denied":return new("ConnectDenied");
                default:return new("ConnectExpired");
            }
        }
        return new("ConnectExpired");
    }

    /// <summary>
    /// X-184 N15 (0.1208, night review of 12/11): what a failed read of the common files may throw besides a refused request: a body cut
    /// midway (IOException, HttpIOException), a request past its 30 s (TaskCanceledException while the exchange is not being stopped), text or
    /// JSON that cannot be read. Such a file counts as unreadable; it no longer ends the exchange before the player's own facts are sent.
    /// </summary>
    private bool CommonUnreadable(Exception error)
        =>error is HttpRequestException or IOException or InvalidDataException or DecoderFallbackException or System.Text.Json.JsonException
            || error is TaskCanceledException && !stop.IsCancellationRequested;

    private async Task<ExchangeMessage> ExchangeAsync(DiscoveryStore store,bool upload)
    {
        (string Common,int Added,int Joined) taken;
        try{taken=await TakeCommonV3(store).ConfigureAwait(false) ?? await TakeCommonV2(store).ConfigureAwait(false);}
        catch(Exception error) when(CommonUnreadable(error)){taken=("CommonUnreadable",0,0);}
        var (common,added,joined)=taken;
        var sent="";
        if(upload && Connected)
        {
            var mine=CommunityDiscoveries.Mine(store.Snapshot(DateTimeOffset.UtcNow,false));
            // X-174 (0.1137, DEC-052): sent as a v3 set: index.json and one kind-NN.jsonl.gz.b64 per kind (base64: a gist holds text). The index holds each
            // file's hash, so the same facts give the same index and nothing is sent again.
            var files=ExchangeV3.GistFiles(ExchangeV3.Build(mine));var hash=CommunityDiscoveries.Hash(files[ExchangeV3.IndexName]);
            string known;lock(gate)known=meta.UploadHash;
            if(mine.Facts.Count==0)sent="ShareEmpty";
            else if(hash==known)sent="SentSame";
            else
            {
                // X-181 (0.1171): a pair whose write failed before is written again first (night review N8).
                WriteSecrets();
                await Fresh().ConfigureAwait(false);
                // N29: a 401 with a refresh token in hand (a PC clock late, a token ended early): renewed once, sent once more, before forgetting.
                try{await Upload(files).ConfigureAwait(false);}
                catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized && HasRefresh)
                {await Fresh(force:true).ConfigureAwait(false);await Upload(files).ConfigureAwait(false);}
                lock(gate)meta.UploadHash=hash;sent="Sent";
            }
        }
        lock(gate)meta.Exchanged=DateTimeOffset.UtcNow;
        SaveMeta();
        return new("ExchangeDone",added.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),joined.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),common,sent);
    }

    /// <summary>
    /// X-174 (0.1137, DEC-052): the v3 set of the repository: its index, then only the kind files whose hash differs from the one noted for that kind, each
    /// downloaded within the size its index gives, checked (ExchangeV3.ReadKind) and merged; its hash is noted only once a save holds its facts (X-166:
    /// WhenImportSaved, ImportWaiting against a double merge meanwhile). Null when there is no index this version reads: the v2 file is read instead.
    /// </summary>
    private async Task<(string Common,int Added,int Joined)?> TakeCommonV3(DiscoveryStore store)
    {
        ExchangeV3Index? index;
        try{index=ExchangeV3.ParseIndex(await Community.BoundedHttp.ReadAsync(client,new Uri(CommunityDiscoveries.AddressV3+ExchangeV3.IndexName),ExchangeV3.MaxIndexBytes,stop.Token).ConfigureAwait(false));}
        catch(Exception error) when(CommonUnreadable(error)){return null;}
        if(index is null)return null;
        var noted=CommonV3Marks();int added=0,joined=0,taken=0,unreadable=0;
        foreach(var entry in index.Files)
        {
            var kind=(int)entry.Kind;var hash=entry.Sha256.ToUpperInvariant();var key=$"common-v3:{kind}:{hash}";
            if(noted.GetValueOrDefault(kind)==hash || store.ImportWaiting(key))continue;
            try
            {
                var bytes=await Community.BoundedHttp.ReadBytesAsync(client,new Uri(CommunityDiscoveries.AddressV3+entry.Name),(int)entry.Bytes,stop.Token).ConfigureAwait(false);
                var read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);
                var result=store.Merge(read.Facts,common:true);added+=result.Added;joined+=result.Joined;taken++;
                store.WhenImportSaved(key,()=>NoteCommonV3(kind,hash));
            }
            catch(Exception error) when(CommonUnreadable(error)){unreadable++;}
        }
        return (taken>0 ? "CommonTaken":unreadable>0 ? "CommonUnreadable":"CommonSame",added,joined);
    }
    private Dictionary<int,string> CommonV3Marks()
    {
        lock(marks)
            try{return File.Exists(CommonV3Marker) ? JsonSerializer.Deserialize<Dictionary<int,string>>(File.ReadAllText(CommonV3Marker)) ?? []:[];}
            catch(Exception){return [];}
    }
    private void NoteCommonV3(int kind,string hash)
    {
        lock(marks)
        {
            var noted=CommonV3Marks();noted[kind]=hash;
            Directory.CreateDirectory(folder);AtomicFile.Write(CommonV3Marker,JsonSerializer.Serialize(noted));
        }
    }

    /// <summary>X-174 (0.1137): the v2 common file, read while the v3 set is missing (« v2 still read for a while »).</summary>
    private async Task<(string Common,int Added,int Joined)> TakeCommonV2(DiscoveryStore store)
    {
        var added=0;var joined=0;string common;
        try
        {
            var json=await Community.BoundedHttp.ReadAsync(client,new Uri(CommunityDiscoveries.Address),CommunityDiscoveries.MaxBytes,stop.Token).ConfigureAwait(false);
            var hash=CommunityDiscoveries.Hash(json);
            if(File.Exists(CommonMarker) && File.ReadAllText(CommonMarker).Trim()==hash || store.ImportWaiting("common:"+hash))common="CommonSame";
            else if(CommunityDiscoveries.Parse(json) is { } file)
            {
                var result=store.Merge(file.Facts,common:true);added=result.Added;joined=result.Joined;
                // X-166 (0.1133, audit R01): the hash is noted only once a save holds the merged facts; a stop before leaves the file to take again.
                store.WhenImportSaved("common:"+hash,()=>{Directory.CreateDirectory(folder);AtomicFile.Write(CommonMarker,hash);});
                common="CommonTaken";
            }
            else common="CommonUnreadable";
        }
        catch(HttpRequestException){common="CommonMissing";}
        catch(Exception error) when(CommonUnreadable(error)){common="CommonUnreadable";}
        return (common,added,joined);
    }

    private async Task Upload(IReadOnlyDictionary<string,string> contents)
    {
        var files=new JsonObject();
        foreach(var (name,content) in contents)files[name]=new JsonObject{["content"]=content};
        string gist;List<string> before;lock(gate){gist=meta.GistId;before=meta.GistFiles.Count>0 ? [..meta.GistFiles]:[GistFile];}
        if(gist.Length>0)
        {
            // X-174 (0.1137): the files of before that this upload does not hold (the v2 file GistFile, a kind gone) are removed from the gist: GitHub
            // removes a file set to null. Refused (a file already gone by hand): sent again without the removals.
            var patch=(JsonObject)files.DeepClone();var removed=0;
            foreach(var name in before.Where(name=>!contents.ContainsKey(name))){patch[name]=null;removed++;}
            try{await Api(HttpMethod.Patch,$"https://api.github.com/gists/{gist}",new JsonObject{["files"]=patch}).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.UnprocessableEntity && removed>0)
            {
                try{await Api(HttpMethod.Patch,$"https://api.github.com/gists/{gist}",new JsonObject{["files"]=files.DeepClone()}).ConfigureAwait(false);}
                catch(HttpRequestException missing) when(missing.StatusCode==HttpStatusCode.NotFound){gist="";}
            }
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.NotFound){gist="";}
        }
        if(gist.Length==0)
        {
            var created=await Api(HttpMethod.Post,"https://api.github.com/gists",
                new JsonObject{["description"]="Tensho Assistant discoveries",["public"]=false,["files"]=files.DeepClone()}).ConfigureAwait(false);
            gist=created?["id"]?.GetValue<string>() ?? throw new InvalidDataException("No gist id.");
            lock(gate){meta.GistId=gist;meta.Issue=0;}
        }
        lock(gate){meta.GistFiles=[..contents.Keys];if(meta.Login.Length>0)meta.GistLogin=meta.Login;}
        // X-175: the gist kept before the issue, so a refused issue does not make a second gist at the next exchange.
        SaveMeta();
        int issue;lock(gate)issue=meta.Issue;
        if(issue==0)
        {
            var version=typeof(GitHubExchange).Assembly.GetName().Version?.ToString(3) ?? "?";
            JsonNode? opened;
            try
            {
                opened=await Api(HttpMethod.Post,$"https://api.github.com/repos/{Repository}/issues",new JsonObject
                {
                    ["title"]=$"Discoveries: {Login}",
                    ["body"]=$"Shared from Tensho Assistant {version} (World › Discoveries › automatic exchange).\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain, https://creativecommons.org/publicdomain/zero/1.0/).\n\nThe gist is updated at each exchange; this issue stays the same.",
                    ["labels"]=new JsonArray("discoveries"),
                }).ConfigureAwait(false);
            }
            // X-175: with the GitHub App, a refused issue (403, or 404 for a repository the App cannot see) means the App is not installed on the
            // repository: the maintainer has to install it; nothing else is sent, the token is kept.
            catch(HttpRequestException error) when(signIn.App && error.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {throw new ExchangeRefused("ExchangeAppMissing",false);}
            lock(gate)meta.Issue=opened?["number"]?.GetValue<int>() ?? 0;
        }
        SaveMeta();
    }

    private bool HasRefresh{get{lock(gate)return refresh is not null;}}

    private async Task<JsonObject> Form(string address,Dictionary<string,string> fields,CancellationToken? cancel=null)
    {
        var request=new HttpRequestMessage(HttpMethod.Post,address){Content=new FormUrlEncodedContent(fields)};
        request.Headers.Accept.ParseAdd("application/json");
        var text=await Community.BoundedHttp.SendAsync(client,request,64*1024,cancel ?? stop.Token).ConfigureAwait(false);
        return JsonNode.Parse(text)?.AsObject() ?? throw new InvalidDataException("Empty answer.");
    }

    private async Task<JsonNode?> Api(HttpMethod method,string address,JsonNode? body,string? with=null)
    {
        var request=new HttpRequestMessage(method,address);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version","2022-11-28");
        // X-181 (0.1171, night review N7): after a Disconnect nothing is sent with the player's token (no gist, no issue).
        string? bearer;
        lock(gate)
        {
            if(with is null && (token is null || workGeneration!=generation))throw new OperationCanceledException("Disconnected.");
            bearer=with ?? token;
        }
        if(bearer is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
        if(body is not null)request.Content=new StringContent(body.ToJsonString(),Encoding.UTF8,"application/json");
        var text=await Community.BoundedHttp.SendAsync(client,request,4*1024*1024,stop.Token).ConfigureAwait(false);
        return text.Length==0 ? null:JsonNode.Parse(text);
    }

    private void SaveMeta()
    {
        try
        {
            string json;lock(gate)json=JsonSerializer.Serialize(meta);
            AtomicFile.Write(MetaPath,json);
        }
        catch(Exception){}
    }

    /// <summary>X-181 (0.1171, night review N8): a renewal in flight is given a few seconds to finish and be kept before the client closes.</summary>
    public void Dispose()
    {
        stop.Cancel();
        Task? pending;lock(gate)pending=refreshing;
        try{pending?.Wait(TimeSpan.FromSeconds(5));}catch(AggregateException){}
        WriteSecrets();
        client.Dispose();
    }
}

/// <summary>X-77.1 (0.883): a secret kept on this PC for this Windows user only (DPAPI, CryptProtectData), so the token is never written in clear.</summary>
internal static class UserSecret
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Blob{public int Size;public IntPtr Data;}
    [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)]
    private static extern bool CryptProtectData(ref Blob input,string? description,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,out Blob output);
    [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,out Blob output);
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
    private const int NoInterface=1;

    public static byte[] Protect(byte[] data)=>Run(data,true);
    public static byte[] Unprotect(byte[] data)=>Run(data,false);
    private static byte[] Run(byte[] data,bool protect)
    {
        var handle=GCHandle.Alloc(data,GCHandleType.Pinned);
        try
        {
            var input=new Blob{Size=data.Length,Data=handle.AddrOfPinnedObject()};
            Blob output;
            var done=protect ? CryptProtectData(ref input,"Tensho Assistant",IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,NoInterface,out output)
                :CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,NoInterface,out output);
            if(!done)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            try{var result=new byte[output.Size];Marshal.Copy(output.Data,result,0,output.Size);return result;}
            finally{LocalFree(output.Data);}
        }
        finally{handle.Free();}
    }
}
