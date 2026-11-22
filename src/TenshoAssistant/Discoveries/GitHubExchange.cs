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
    /// <summary>
    /// X-640 (D-190 a confirmed on 20/11): the exchanges in a row where GitHub refused durably the raw address of a cut file of your gist while
    /// rows of yours set aside wait for their copy (GistAside.RefusalLimit); back to 0 once they are settled.
    /// </summary>
    public int AsideRefused { get; set; }
    /// <summary>
    /// X-656 M2 (review of 0.1598): the gist and the rows set aside the count above is about (GitHubExchange.AsideRows); other rows, or another
    /// gist, start again from 0. Empty: none.
    /// </summary>
    public string AsideRefusedFor { get; set; }="";
    /// <summary>X-666 M4 (0.1614, review of 0.1612): when the last exchange was started, and how many exchanges in a row failed since the last
    /// one that ended (GitHubExchange.Wait): kept here so a restart of the game does not try again at once.</summary>
    public DateTimeOffset? Tried { get; set; }
    public int Failures { get; set; }
    /// <summary>
    /// X-656 M1 (review of 0.1598): copies refused GistAside.RefusalLimit times were given up and their rows settled, but no send has replaced
    /// the gist since: « ShareAsideGivenUp » is said at the first send that does (« Sent »), even sessions later, then this goes back to false.
    /// </summary>
    public bool AsideGivenUp { get; set; }
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
/// time, and one issue per player in the repository pointing to it. Started by a click, or at most once every 20 hours (<see cref="Every"/>) by the opt-in setting « automatic exchange »;
/// after a failure, tried again 15 minutes later, then twice as long after each new failure, 20 hours at most (<see cref="Wait"/>, X-666).
/// A discovery never holds a player's name, position or id; the issue's title holds the GitHub login of the account the player connected.
/// X-175 (1138): the sign-in (OAuth app today, GitHub App once its client id is given) comes from GitHubSignIn, the one switch; an expiring token
/// is renewed before an upload with its refresh token, kept protected the same way (github.refresh).
/// </summary>
public sealed class GitHubExchange : IDisposable
{
    public const string Repository="Shigu330/Tensho-Assistant",GistFile="tensho-discoveries.b64";
    /// <summary>X-803 (user decision of 22/11): said in each player's issue, in English then French (GitHub in two languages, R20.1): what is shared
    /// stays in the common base without the player's name, even after they stop sharing. The French half is listed in Languages406Tests.AllowedLiterals.</summary>
    internal const string IssueStays="What you share stays in the common base even if you stop sharing. Close this issue (or ask in it) and the maintainer removes your player file, which links your GitHub name to your discoveries; this issue's title and the repository's history still show that name.";
    internal const string IssueStaysFrench="Français : ce que vous partagez reste dans la base commune même si vous arrêtez de partager. Fermez ce ticket (ou demandez-le dedans) et le mainteneur retire votre fichier de joueur, qui relie votre nom GitHub à vos découvertes ; le titre de ce ticket et l'historique du dépôt montrent encore ce nom.";
    /// <summary>X-175: the files that hold a secret, never put in a backup copy.</summary>
    public static readonly IReadOnlySet<string> SecretFiles=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"github.token","github.refresh"};
    public static readonly TimeSpan Every=TimeSpan.FromHours(20),Retry=TimeSpan.FromMinutes(15);
    /// <summary>
    /// X-666 M4 (0.1614, review of 0.1612, R04: fewer automatic requests): the wait before the automatic exchange tries again after
    /// <paramref name="failures"/> exchanges in a row that failed (expired token, refusal, network): 15 minutes, then twice as long after each
    /// new failure, 20 hours at most (<see cref="Every"/>). It was 15 minutes whatever the number of failures.
    /// </summary>
    public static TimeSpan Wait(int failures)=>failures<=1 ? Retry:TimeSpan.FromTicks(Math.Min(Every.Ticks,Retry.Ticks<<Math.Min(failures-1,20)));
    private readonly string folder;
    private readonly HttpClient client;
    private readonly GitHubSignIn signIn;
    private readonly Func<DateTimeOffset> clock;
    private readonly CancellationTokenSource stop=new();
    private readonly object gate=new(),marks=new();
    private string? token,refresh;private GitHubExchangeMeta meta=new();private DateTimeOffset tried;private bool busy;
    // X-822: github.json could not be written at the last try (said once in the log until a write succeeds).
    private bool metaUnsaved;
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
    // X-340 (0.1324): the result of the last problem report sent directly, apart from the exchange's message (the Help page shows it).
    private ExchangeMessage? reportMessage;

    public bool Busy{get{lock(gate)return busy;}}
    public string? UserCode{get{lock(gate)return userCode;}}
    public string? VerifyAddress{get{lock(gate)return verifyAddress;}}
    public ExchangeMessage? Message{get{lock(gate)return message;}}
    /// <summary>X-340: the last report's result: « ReportSent » (issue number, address), or the cause of a failure; null while none or one is on its way.</summary>
    public ExchangeMessage? ReportMessage{get{lock(gate)return reportMessage;}}
    public bool Connected{get{lock(gate)return token is not null;}}
    public string Login{get{lock(gate)return meta.Login;}}
    public DateTimeOffset? Exchanged{get{lock(gate)return meta.Exchanged;}}
    private string TokenPath=>Path.Combine(folder,"github.token");
    private string RefreshPath=>Path.Combine(folder,"github.refresh");
    private string MetaPath=>Path.Combine(folder,"github.json");
    private string LockPath=>Path.Combine(folder,"github.lock");
    /// <summary>X-804: what is known of the gist's merges (GistRotation), and the lock around its rotation (separate from the renewal's lock).</summary>
    private string RotationPath=>Path.Combine(folder,GistRotation.FileName);
    private string RotationLockPath=>Path.Combine(folder,"github.rotate.lock");
    /// <summary>X-174 (0.1137): the hash of each v3 kind file taken, by kind number ({"10":"HEX",…}), noted once a save holds its facts.</summary>
    private string CommonV3Marker=>Path.Combine(folder,"community-v3.json");

    public GitHubExchange(string folder):this(folder,null){}
    /// <summary>X-468 (0.1805): the log's line of each exchange that read the common set's index (the download and the merge timed), in English.</summary>
    public Action<string>? Log{get;init;}
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
        try{if(File.Exists(MetaPath))meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(MetaPath),TenshoJson.Plain) ?? new();}
        catch(Exception){meta=new();try{File.Move(MetaPath,MetaPath+".broken",true);}catch(Exception){}}
        try{if(File.Exists(TokenPath))token=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(TokenPath)));}catch(Exception){token=null;}
        try{if(token is not null && File.Exists(RefreshPath))refresh=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(RefreshPath)));}catch(Exception){refresh=null;}
        // X-175: a token of the other sign-in (the OAuth app's, once the GitHub App is in place) is forgotten: the player connects again, to the App.
        if(token is not null && !this.signIn.Owns(meta.ClientId)){Disconnect();message=new("ExchangeSignInChanged");}
        tried=meta.Tried ?? default;
    }

    /// <summary>The automatic exchange is due: none for <see cref="Every"/>, and no attempt for <see cref="Wait"/> (X-666 M4: a failure is
    /// tried again later and later, never every 15 minutes); a last attempt dated after now (the clock set back) does not hold it.</summary>
    public bool Due(DateTimeOffset now)
    {
        lock(gate)
        {
            var since=now-tried;
            return !busy && (since<TimeSpan.Zero || since>=Wait(meta.Failures)) && (meta.Exchanged is not { } last || now-last>=Every);
        }
    }

    private bool Start(Func<Task<ExchangeMessage>> work,bool report=false,bool exchange=false)
    {
        lock(gate)
        {
            if(busy)return false;
            busy=true;workGeneration=generation;
            if(report)reportMessage=null;else{message=null;tried=clock();if(exchange)meta.Tried=tried;}
        }
        _=Task.Run(async()=>
        {
            ExchangeMessage result;
            try{result=await work().ConfigureAwait(false);}
            catch(OperationCanceledException){result=new("ExchangeStopped");}
            catch(ExchangeRefused refused){if(refused.Forget)Disconnect();result=new(refused.Key);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized){ForgetIfStill(null);result=new("ExchangeUnauthorized");}
            catch(Exception error){result=new("ExchangeFailed",error.GetType().Name);}
            // X-666 M4: an exchange that ended clears the count of failures; one that failed adds one (a stop of the plugin is not one).
            // 0.1618: nothing written once the plugin unloads (stop asked): the file was written while its folder went away (a test's
            // cleanup met the temporary file moved under it, GitHubToken1171 N8 under load).
            if(exchange && result.Key!="ExchangeStopped" && !stop.IsCancellationRequested)
            {
                lock(gate)meta.Failures=result.Key=="ExchangeDone" ? 0:Math.Min(meta.Failures+1,30);
                SaveMeta();
            }
            lock(gate){if(report)reportMessage=result;else message=result;busy=false;userCode=null;}
        });
        return true;
    }

    public bool Connect()=>Start(ConnectAsync);
    public bool Exchange(DiscoveryStore store,bool upload)=>Start(()=>ExchangeAsync(store,upload),exchange:true);
    /// <summary>
    /// X-340 (0.1324, user's decision of 2026-11-14, R04): the player's problem report opened as an issue of the repository, through this connection,
    /// on the player's click only (the Help page calls it from its « Send » button, under the preview of these very strings). False when not
    /// connected or while another work runs. The token is never written in a message or the log.
    /// </summary>
    public bool Report(TenshoAssistant.Help.ProblemIssue issue)=>Connected && Start(()=>ReportAsync(issue),true);

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
            var saved=File.Exists(MetaPath) ? JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(MetaPath),TenshoJson.Plain):null;
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
        // X-279 (0.1292, test GitHubToken1171 failing alone): the renewal is registered before its request leaves, so that a Dispose
        // coming while GitHub answers always finds it to wait for (the request used to start, and could be answered, before this line).
        var go=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool?> Registered(){await go.Task.ConfigureAwait(false);return await RenewAndKeep(renewal,started).ConfigureAwait(false);}
        var work=Registered();
        lock(gate)refreshing=work;
        go.SetResult();
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
    /// <summary>
    /// X-367 M1 (relecture 0.1349, D-159): of those, the ones where the file could not be fetched, not a file that is damaged: GitHub not reached, a
    /// refusal or a missing file (any HttpRequestException: no answer, 404 before the set is published, 403, 429, 5xx), a request past its delay
    /// (TaskCanceledException outside a stop) and a body cut midway (IOException). « Unreachable or not published yet, try again later », never
    /// « unreadable ». Bad gzip, a size or hash that does not match, text or JSON that cannot be read stay « unreadable ».
    /// </summary>
    internal static bool CommonUnreachable(Exception error)=>error is HttpRequestException or IOException or TaskCanceledException;

    private async Task<ExchangeMessage> ExchangeAsync(DiscoveryStore store,bool upload)
    {
        (string Common,int Added,int Joined) taken;
        // X-357 (0.1349, DEC-052): the v3 set only; the v2 file is gone from the repository. Without a v3 index this version reads: nothing taken,
        // « unreadable », the local base untouched.
        try{taken=await TakeCommonV3(store).ConfigureAwait(false) ?? ("CommonUnreadable",0,0);}
        catch(Exception error) when(CommonUnreadable(error)){taken=(CommonUnreachable(error) ? "CommonUnreachable":"CommonUnreadable",0,0);}
        var (common,added,joined)=taken;
        var sent="";(bool Go,string Said) aside=(true,"");var replaced=false;
        // X-587 (external audit A16 of 18/11): your own base not read whole at start (DiscoveryStore.MineIncomplete): the gist would be
        // replaced by this session's facts only, and its kinds missing here removed; nothing is sent, the common data above are still taken.
        if(upload && Connected && store.MineIncomplete)sent="ShareBlocked";
        // X-621 (review of 0.1560, I1): rows of yours set aside as damaged: their copy on your gist read back first, else nothing sent this time.
        // X-640 (D-190 a confirmed on 20/11): « not reached » (ShareAsideWaiting) or refused durably (ShareAsideRefused), said with its cause; refused
        // GistAside.RefusalLimit exchanges in a row, the send goes on without that copy (ShareAsideGivenUp, said once it is sent).
        else if(upload && Connected && store.Aside is not null && !(aside=await KeepAside(store).ConfigureAwait(false)).Go)sent=aside.Said;
        else if(upload && Connected)
        {
            var mine=CommunityDiscoveries.Mine(store.Snapshot(DateTimeOffset.UtcNow,false));
            if(mine.Facts.Count==0)sent="ShareEmpty";
            // X-822: after a write of github.json that failed, written again first, or nothing made (a send whose gist and issue cannot be
            // noted would make new ones at each exchange). Not written when the last write held: another client of this profile may have
            // renewed the sign-in since, and this client's older copy would hide it (GitHubToken1171Tests, N2).
            else{if(MetaUnsaved)KeepMeta();(sent,replaced)=await Rotate(mine).ConfigureAwait(false);}
        }
        // X-656 M2 (review of 0.1598): an exchange « blocked » or « not reached » breaks the row of refusals (« 3 exchanges in a row »).
        if(sent is "ShareBlocked" or "ShareAsideWaiting")ClearRefused();
        // X-656 M1 (review of 0.1598): the give-up is said by the send that replaces the gist without those copies, whenever it comes (an
        // upload that failed, « SentSame » or « ShareEmpty » leave them on the gist: still to say). X-824 M1 (review of 0.1823): told by the
        // upload itself (replaced), not by the word « Sent », which « ShareOldGistKept » takes the place of: the give-up was then never said
        // while another account's gist waited. Said first: « ShareOldGistKept » is said again at every exchange until that gist goes.
        if(replaced)lock(gate)if(meta.AsideGivenUp){meta.AsideGivenUp=false;sent="ShareAsideGivenUp";}
        lock(gate)meta.Exchanged=DateTimeOffset.UtcNow;
        SaveMeta();
        return new("ExchangeDone",added.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),joined.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),common,sent);
    }

    /// <summary>
    /// X-174 (0.1137, DEC-052): the v3 set of the repository: its index, then only the kind files whose hash differs from the one noted for that kind, each
    /// downloaded within the size its index gives, checked (ExchangeV3.ReadKind) and merged; its hash is noted only once a save holds its facts (X-166:
    /// WhenImportSaved, ImportWaiting against a double merge meanwhile). Null when there is no index this version reads (X-357: nothing else is read).
    /// </summary>
    private async Task<(string Common,int Added,int Joined)?> TakeCommonV3(DiscoveryStore store)
    {
        ExchangeV3Index? index;
        try{index=ExchangeV3.ParseIndex(await Community.BoundedHttp.ReadAsync(client,new Uri(CommunityDiscoveries.AddressV3+ExchangeV3.IndexName),ExchangeV3.MaxIndexBytes,stop.Token).ConfigureAwait(false));}
        catch(Exception error) when(CommonUnreadable(error)){return CommonUnreachable(error) ? ("CommonUnreachable",0,0):null;}
        if(index is null)return null;
        var noted=CommonV3Marks();int added=0,joined=0,taken=0,unreadable=0,unreachable=0,facts=0;long size=0;
        // X-468 (0.1805, D-216 a): the download (with the check of each file against the index) and the merge timed apart, said in the log.
        var fetching=new System.Diagnostics.Stopwatch();var merging=new System.Diagnostics.Stopwatch();
        foreach(var entry in index.Files)
        {
            var kind=(int)entry.Kind;var hash=entry.Sha256.ToUpperInvariant();var key=$"common-v3:{kind}:{hash}";
            if(noted.GetValueOrDefault(kind)==hash || store.ImportWaiting(key))continue;
            try
            {
                fetching.Start();
                ExchangeV3Kind read;
                try
                {
                    var bytes=await Community.BoundedHttp.ReadBytesAsync(client,new Uri(CommunityDiscoveries.AddressV3+entry.Name),(int)entry.Bytes,stop.Token).ConfigureAwait(false);
                    read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);size+=bytes.Length;
                }
                finally{fetching.Stop();}
                merging.Start();
                try{var result=store.Merge(read.Facts,common:true);added+=result.Added;joined+=result.Joined;taken++;facts+=read.Facts.Count;}
                finally{merging.Stop();}
                store.WhenImportSaved(key,()=>NoteCommonV3(kind,hash));
            }
            catch(Exception error) when(CommonUnreadable(error)){if(CommonUnreachable(error))unreachable++;else unreadable++;}
        }
        Log?.Invoke(CommonTimes(index.Files.Count,taken,facts,size,fetching.ElapsedMilliseconds,merging.ElapsedMilliseconds,added,joined,unreadable,unreachable));
        return (taken>0 ? "CommonTaken":unreadable>0 ? "CommonUnreadable":unreachable>0 ? "CommonUnreachable":"CommonSame",added,joined);
    }
    /// <summary>X-468 (0.1805): the log's line of a common set read: kind files taken of those listed, facts and bytes, the download and the merge.</summary>
    internal static string CommonTimes(int listed,int taken,int facts,long bytes,long fetchMs,long mergeMs,int added,int joined,int unreadable,int unreachable)=>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"Discoveries: common set: {taken} of {listed} kind file(s) taken ({facts} facts, {bytes} bytes): download {fetchMs} ms, merge {mergeMs} ms; {added} added, {joined} joined")
        +(unreadable+unreachable>0 ? string.Create(System.Globalization.CultureInfo.InvariantCulture,$"; {unreadable} unreadable, {unreachable} not reached"):"")+".";
    private Dictionary<int,string> CommonV3Marks()
    {
        lock(marks)
            try{return File.Exists(CommonV3Marker) ? JsonSerializer.Deserialize<Dictionary<int,string>>(File.ReadAllText(CommonV3Marker),TenshoJson.Plain) ?? []:[];}
            catch(Exception){return [];}
    }
    private void NoteCommonV3(int kind,string hash)
    {
        lock(marks)
        {
            var noted=CommonV3Marks();noted[kind]=hash;
            Directory.CreateDirectory(folder);AtomicFile.Write(CommonV3Marker,JsonSerializer.Serialize(noted,TenshoJson.Plain));
        }
    }

    /// <summary>
    /// X-621 (review of 0.1560, I1): rows of yours set aside at a start (damaged, DiscoveryDatabase.SetAside) are missing from your facts, and the
    /// send replaces the whole gist: before, a fact whose row was damaged vanished from your gist at the next send even when the gist held a valid
    /// copy. That copy is now read back first (GistAside) and joined to your base, database and memory (DiscoveryStore.RestoreAside), so it is
    /// sent again. The gist not reached: false, nothing is sent this time, tried again at the next exchange. No gist yet, a gist gone (404), or a
    /// gist this version cannot read: nothing on it to keep; the rows are settled (their text stays in facts_aside).
    /// </summary>
    /// <remarks>X-623 (review of 0.1562, I1): the gist is read with the token renewed first, as the send does (Fresh): a GitHub App token past
    /// its 8 h answered 401, taken for « GitHub not reached », and the send stayed blocked at every exchange. A 401 with a refresh token in hand
    /// renews once and reads again; a 401 without one ends as the send does (ExchangeUnauthorized), never « not reached ». X-624 M1: only the
    /// listing's 404 means « gist gone »; a cut file not read from its raw address (5xx, 408, 429, network) is « not reached », tried again later.
    /// X-640 (D-190 a confirmed on 20/11): a cut file refused durably there (GistAside.Durable) is counted (meta.AsideRefused) and said
    /// (« ShareAsideRefused »); at the GistAside.RefusalLimit-th exchange in a row, a file refused again is skipped (its facts not kept, their
    /// damaged text still in facts_aside), the rows settled, the send goes on and says it (« ShareAsideGivenUp »). Go is false when nothing may
    /// be sent this time, Said the message's key.</remarks>
    private async Task<(bool Go,string Said)> KeepAside(DiscoveryStore store)
    {
        if(store.Aside is not { } aside)return (true,"");
        string gist,about;int refused;
        // X-656 M2: the count is about this gist and these rows only (rows set aside at a later start begin at 0).
        lock(gate){gist=meta.GistId;about=AsideRows(gist,aside.Keys);refused=meta.AsideRefusedFor==about ? meta.AsideRefused:0;}
        var last=refused+1>=GistAside.RefusalLimit;var skipped=false;
        IReadOnlyCollection<DiscoveryFact> kept;
        try
        {
            if(gist.Length==0)kept=[];
            else
            {
                WriteSecrets();
                await Fresh().ConfigureAwait(false);
                kept=await GistAside.Read(()=>AsideListing(gist),last ? GistAside.SkipRefused(AsideRaw,()=>skipped=true):AsideRaw,aside.Keys).ConfigureAwait(false);
            }
        }
        catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.NotFound){kept=[];}
        catch(GistAside.RawRefusedException){lock(gate){meta.AsideRefused=refused+1;meta.AsideRefusedFor=about;}return (false,"ShareAsideRefused");}
        catch(Exception error) when(CommonUnreachable(error) && error is not HttpRequestException{StatusCode:HttpStatusCode.Unauthorized}){return (false,"ShareAsideWaiting");}
        catch(Exception error) when(error is InvalidDataException or System.Text.Json.JsonException or DecoderFallbackException){kept=[];}
        store.RestoreAside(kept);
        lock(gate){meta.AsideRefused=0;meta.AsideRefusedFor="";if(skipped)meta.AsideGivenUp=true;}
        // X-656 M1: kept at once, the rows being settled: an upload that fails next must not lose what is still to say.
        if(skipped)SaveMeta();
        return (true,"");
    }
    private void ClearRefused(){lock(gate){meta.AsideRefused=0;meta.AsideRefusedFor="";}}
    /// <summary>X-656 M2: the gist and the rows set aside, as one short text (the keys' hash), for <see cref="GitHubExchangeMeta.AsideRefusedFor"/>.</summary>
    internal static string AsideRows(string gist,IReadOnlyCollection<string> keys)
        =>gist+"|"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',keys.Order(StringComparer.Ordinal)))))[..16];
    /// <summary>X-623: your gist's listing; a 401 with a refresh token in hand (a PC clock late, a token ended early) renews once and asks again.</summary>
    private async Task<JsonNode?> AsideListing(string gist)
    {
        var address=$"https://api.github.com/gists/{gist}";
        try{return await Api(HttpMethod.Get,address,null,maximumBytes:GistAside.MaxListingBytes).ConfigureAwait(false);}
        catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized && HasRefresh)
        {await Fresh(force:true).ConfigureAwait(false);return await Api(HttpMethod.Get,address,null,maximumBytes:GistAside.MaxListingBytes).ConfigureAwait(false);}
    }
    /// <summary>
    /// X-624 M1 (review of 0.1562): a cut file read from its raw address (no token sent). A refusal there (404, 401, 5xx), while the listing
    /// answered, says nothing of the gist: never « gist gone » nor a refused sign-in. X-640 (D-190 a confirmed on 20/11): a durable one (4xx but
    /// 408 and 429) is GistAside.RawRefusedException, counted by KeepAside; the others stay « not reached » (IOException).
    /// </summary>
    private async Task<string> AsideRaw(Uri address)
    {
        try{return await Community.BoundedHttp.ReadAsync(client,address,GistAside.MaxRawBytes,stop.Token).ConfigureAwait(false);}
        catch(HttpRequestException error) when(error.StatusCode is { } status)
        {
            if(GistAside.Durable(status))throw new GistAside.RawRefusedException(status,error);
            throw new IOException($"A cut gist file not read ({(int)status}).",error);
        }
    }

    /// <summary>
    /// X-804 (user decision of 22/11 around 14 h 35): the upload, under the rotation lock of this profile. The gist fields are first taken as
    /// another client left them on disk; when the merge tool notes in the player file of main the revision of the last upload (GistRotation), its
    /// facts are known merged and that gist is to delete; only the facts the common base does not hold as they are are sent, to a new gist then,
    /// which keeps the issue (edited to link it); the merged gist is deleted once no issue links it any more. A fact not merged yet is never
    /// left out: it is sent again until a merge notes it. Nothing new: « ShareMerged », the gist deleted, the issue kept for the next one.
    /// </summary>
    private async Task<(string Said,bool Replaced)> Rotate(DiscoveryFile mine)
    {
        using var held=await HoldRotation().ConfigureAwait(false);
        AdoptGist();
        string gist;int issue;lock(gate){gist=meta.GistId;issue=meta.Issue;}
        var state=GistRotation.Read(RotationPath,issue);
        if(GistRotation.Asks(state,gist,issue) && GistRotation.Confirmed(await MergedNote(issue).ConfigureAwait(false),state,gist))
        {
            string login;lock(gate)login=meta.Login;
            state=GistRotation.AfterMerge(state,login);
            lock(gate){meta.GistId="";meta.GistFiles=[];meta.UploadHash="";}
            Save(state);KeepMeta();
        }
        var news=GistRotation.News(mine.Facts,state.Merged);
        var sent="ShareMerged";var replaced=false;
        if(news.Count>0)
        {
            // X-174 (0.1137, DEC-052): sent as a v3 set: index.json and one kind-NN.jsonl.gz.b64 per kind (base64: a gist holds text). The index holds each
            // file's hash, so the same facts give the same index and nothing is sent again.
            var files=ExchangeV3.GistFiles(ExchangeV3.Build(new DiscoveryFile{Version=mine.Version,Producer=mine.Producer,Written=mine.Written,Facts=news}));
            var hash=CommunityDiscoveries.Hash(files[ExchangeV3.IndexName]);
            string known;lock(gate)known=meta.UploadHash;
            if(hash==known)sent="SentSame";
            else
            {
                // X-181 (0.1171): a pair whose write failed before is written again first (night review N8).
                WriteSecrets();
                await Fresh().ConfigureAwait(false);
                // N29: a 401 with a refresh token in hand (a PC clock late, a token ended early): renewed once, sent once more, before forgetting.
                string? revision;
                try
                {
                    try{revision=await Upload(files,state.Rotated).ConfigureAwait(false);}
                    catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized && HasRefresh)
                    {await Fresh(force:true).ConfigureAwait(false);revision=await Upload(files,state.Rotated).ConfigureAwait(false);}
                }
                // X-819 (review of 0.1819, I1): a gist made, then the issue step failing: the rotation's state takes it at once, so the
                // next exchange writes that gist again instead of making another (X-175), and no gist is left behind unlinked.
                catch{KeepGist(state);throw;}
                lock(gate){meta.UploadHash=hash;gist=meta.GistId;issue=meta.Issue;}
                // A new issue is a new player file: nothing of it merged yet.
                if(state.Issue!=issue)state.Merged=[];
                state.Issue=issue;state.SentGist=gist;state.SentRevision=revision ?? "";state.Sent=GistRotation.Hashes(news);state.Rotated=false;
                Save(state);KeepMeta();
                sent="Sent";replaced=true;
            }
        }
        string current,account;lock(gate){current=meta.GistId;account=meta.Login;}
        if(state.OldGist.Length>0 && state.OldGist!=current)
        {
            // X-820 M1 (review of 0.1819): another account's gist is answered 404 (« already gone ») though it stays: kept noted, said, deleted
            // once that account is connected again.
            if(!GistRotation.Deletes(state,account))
            {
                Log?.Invoke("Discoveries: the merged gist of the GitHub account used before on this PC is kept: only that account can delete it.");
                sent="ShareOldGistKept";
            }
            else
            {
                WriteSecrets();
                await Fresh().ConfigureAwait(false);
                if(await DeleteGist(state.OldGist).ConfigureAwait(false)){state.OldGist="";state.OldLogin="";Save(state);}
            }
        }
        return (sent,replaced);
    }

    /// <summary>X-804: the gist and revision the merge tool notes in this issue's player file on main (no account), or null (none, not reached).</summary>
    private async Task<(string Gist,string Revision)?> MergedNote(int issue)
    {
        try{return GistRotation.Note(await Community.BoundedHttp.ReadBytesAsync(client,new Uri($"{GistRotation.NotesAddress}{issue}.jsonl.gz"),GistRotation.MaxNoteFileBytes,stop.Token).ConfigureAwait(false));}
        catch(Exception error) when(CommonUnreadable(error)){return null;}
    }

    /// <summary>X-804: a merged gist deleted whole (true, or already gone); false when GitHub refused or was not reached: tried again at the next exchange.</summary>
    private async Task<bool> DeleteGist(string gist)
    {
        try{await Api(HttpMethod.Delete,$"https://api.github.com/gists/{gist}",null).ConfigureAwait(false);return true;}
        catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.NotFound){return true;}
        catch(Exception error) when(error is HttpRequestException or IOException or TaskCanceledException)
        {Log?.Invoke($"Discoveries: the merged gist could not be deleted ({error.GetType().Name}); tried again at the next exchange.");return false;}
    }

    /// <summary>X-804: the gist, its issue, files and last upload's hash as the last rotation of this profile left them (another client may have
    /// rotated the gist meanwhile). Read from the rotation's state, written under the rotation lock only: github.json is written by every client
    /// outside it too, and an older copy of it there would bring back a deleted gist. The same account only.</summary>
    private void AdoptGist()
    {
        if(GistRotation.Load(RotationPath) is not { Known:true } held)return;
        lock(gate)
            if(meta.Login.Length>0 && held.Login==meta.Login){meta.GistId=held.Gist;meta.Issue=held.Issue;meta.GistFiles=[..held.GistFiles];meta.UploadHash=held.UploadHash;}
    }
    /// <summary>
    /// X-819 (review of 0.1819, I1): an upload that failed after its gist was made (or after the issue was found gone): the rotation's state
    /// written with the gist and issue as they are now, so AdoptGist does not bring back the gist before. An issue changed (0: to open again)
    /// is a new player file: nothing of it merged yet, as after an upload that ends.
    /// </summary>
    private void KeepGist(GistRotationState state)
    {
        int issue;lock(gate)issue=meta.Issue;
        if(state.Issue!=issue){state.Issue=issue;state.Merged=[];}
        try{Save(state);}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        {Log?.Invoke($"Discoveries: the gist in use could not be noted ({error.GetType().Name}).");}
    }
    /// <summary>X-804: the rotation's state written, with the gist fields as they are now (read back by <see cref="AdoptGist"/>).</summary>
    private void Save(GistRotationState state)
    {
        lock(gate){state.Login=meta.Login;state.Gist=meta.GistId;state.GistFiles=[..meta.GistFiles];state.UploadHash=meta.UploadHash;state.Known=true;}
        GistRotation.Write(RotationPath,state);
    }

    /// <summary>X-804: the lock file around a gist's rotation, shared by every client of this profile; IOException past <see cref="LockWait"/>.</summary>
    private async Task<FileStream> HoldRotation()
    {
        Directory.CreateDirectory(folder);
        var until=DateTime.UtcNow+LockWait;
        while(true)
        {
            try{return new FileStream(RotationLockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(DateTime.UtcNow<until){await Task.Delay(50).ConfigureAwait(false);}
        }
    }

    /// <summary>X-804: the text of a player's issue: its gist, the CC0 sharing, what becomes of the gist and of what is shared (English then French).</summary>
    internal static string IssueBody(string version,string gist)=>
        $"Shared from Tensho Assistant {version} (World › Discoveries › automatic exchange).\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain, https://creativecommons.org/publicdomain/zero/1.0/).\n\n{IssueGist}\n{IssueGistFrench}\n\n{IssueStays}\n{IssueStaysFrench}";
    internal const string IssueGist="Once its discoveries are in the common base, the gist is deleted and a new one holds only what is new; this issue is then edited to link it.";
    internal const string IssueGistFrench="Français : une fois ses découvertes dans la base commune, le gist est supprimé et un nouveau ne garde que les nouveautés ; ce ticket est alors modifié pour y renvoyer.";

    /// <summary>
    /// The gist written: the one in use patched, else a new one; its revision (history[0].version: what the merge tool notes once merged), or
    /// null when GitHub gives none. X-804: <paramref name="rotated"/>: the gist was deleted after a merge: a new gist keeps the issue, whose text is
    /// edited to link it (a new issue only when that one is gone).
    /// </summary>
    private async Task<string?> Upload(IReadOnlyDictionary<string,string> contents,bool rotated=false)
    {
        var files=new JsonObject();
        foreach(var (name,content) in contents)files[name]=new JsonObject{["content"]=content};
        string gist;List<string> before;lock(gate){gist=meta.GistId;before=meta.GistFiles.Count>0 ? [..meta.GistFiles]:[GistFile];}
        JsonNode? written=null;
        if(gist.Length>0)
        {
            // X-174 (0.1137): the files of before that this upload does not hold (the v2 file GistFile, a kind gone) are removed from the gist: GitHub
            // removes a file set to null. Refused (a file already gone by hand): sent again without the removals.
            var patch=(JsonObject)files.DeepClone();var removed=0;
            foreach(var name in before.Where(name=>!contents.ContainsKey(name))){patch[name]=null;removed++;}
            try{written=await Api(HttpMethod.Patch,$"https://api.github.com/gists/{gist}",new JsonObject{["files"]=patch}).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.UnprocessableEntity && removed>0)
            {
                try{written=await Api(HttpMethod.Patch,$"https://api.github.com/gists/{gist}",new JsonObject{["files"]=files.DeepClone()}).ConfigureAwait(false);}
                catch(HttpRequestException missing) when(missing.StatusCode==HttpStatusCode.NotFound){gist="";}
            }
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.NotFound){gist="";}
        }
        if(gist.Length==0)
        {
            written=await Api(HttpMethod.Post,"https://api.github.com/gists",
                new JsonObject{["description"]="Tensho Assistant discoveries",["public"]=false,["files"]=files.DeepClone()}).ConfigureAwait(false);
            gist=written?["id"]?.GetValue<string>() ?? throw new InvalidDataException("No gist id.");
            lock(gate){meta.GistId=gist;if(!rotated)meta.Issue=0;}
        }
        lock(gate){meta.GistFiles=[..contents.Keys];if(meta.Login.Length>0)meta.GistLogin=meta.Login;}
        // X-175: the gist kept before the issue, so a refused issue does not make a second gist at the next exchange. X-822: not kept (github.json
        // not written): stopped here, no issue opened (the rotation's state notes the gist, Rotate's catch).
        KeepMeta();
        int issue;lock(gate)issue=meta.Issue;
        var version=typeof(GitHubExchange).Assembly.GetName().Version?.ToString(3) ?? "?";
        // X-804: a new gist after a merge: the issue edited to link it; an issue gone (404, 410) is opened again below. X-819: at every
        // upload until the rotation ends (a gist made at a try whose issue edit failed is written again, not made again).
        if(issue>0 && rotated)
        {
            try{await Api(HttpMethod.Patch,$"https://api.github.com/repos/{Repository}/issues/{issue}",new JsonObject{["body"]=IssueBody(version,gist)}).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone){lock(gate)meta.Issue=issue=0;}
        }
        if(issue==0)
        {
            JsonNode? opened;
            try
            {
                opened=await Api(HttpMethod.Post,$"https://api.github.com/repos/{Repository}/issues",new JsonObject
                {
                    ["title"]=$"Discoveries: {Login}",
                    ["body"]=IssueBody(version,gist),
                    ["labels"]=new JsonArray("discoveries"),
                }).ConfigureAwait(false);
            }
            // X-175: with the GitHub App, a refused issue (403, or 404 for a repository the App cannot see) means the App is not installed on the
            // repository: the maintainer has to install it; nothing else is sent, the token is kept.
            catch(HttpRequestException error) when(signIn.App && error.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {throw new ExchangeRefused("ExchangeAppMissing",false);}
            lock(gate)meta.Issue=opened?["number"]?.GetValue<int>() ?? 0;
        }
        KeepMeta();
        return written?["history"]?[0]?["version"]?.GetValue<string>();
    }

    private bool HasRefresh{get{lock(gate)return refresh is not null;}}

    private async Task<ExchangeMessage> ReportAsync(TenshoAssistant.Help.ProblemIssue issue)
    {
        WriteSecrets();
        await Fresh().ConfigureAwait(false);
        var body=new JsonObject{["title"]=issue.Title,["body"]=issue.Body,["labels"]=new JsonArray([..issue.Labels.Select(label=>(JsonNode?)JsonValue.Create(label))])};
        JsonNode? opened;
        try
        {
            try{opened=await Api(HttpMethod.Post,$"https://api.github.com/repos/{Repository}/issues",body.DeepClone()).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized && HasRefresh)
            {await Fresh(force:true).ConfigureAwait(false);opened=await Api(HttpMethod.Post,$"https://api.github.com/repos/{Repository}/issues",body.DeepClone()).ConfigureAwait(false);}
        }
        catch(HttpRequestException error) when(signIn.App && error.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {throw new ExchangeRefused("ReportAppMissing",false);}
        var number=opened?["number"] is JsonValue value && value.TryGetValue<int>(out var given) ? given:0;
        if(number<=0)throw new InvalidDataException("No issue number.");
        var address=opened?["html_url"] is JsonValue link && link.TryGetValue<string>(out var text) && text.StartsWith($"https://github.com/{Repository}/issues/",StringComparison.Ordinal)
            ? text:$"https://github.com/{Repository}/issues/{number}";
        return new("ReportSent",number.ToString(System.Globalization.CultureInfo.InvariantCulture),address);
    }

    private async Task<JsonObject> Form(string address,Dictionary<string,string> fields,CancellationToken? cancel=null)
    {
        var request=new HttpRequestMessage(HttpMethod.Post,address){Content=new FormUrlEncodedContent(fields)};
        request.Headers.Accept.ParseAdd("application/json");
        var text=await Community.BoundedHttp.SendAsync(client,request,64*1024,cancel ?? stop.Token).ConfigureAwait(false);
        return JsonNode.Parse(text)?.AsObject() ?? throw new InvalidDataException("Empty answer.");
    }

    private async Task<JsonNode?> Api(HttpMethod method,string address,JsonNode? body,string? with=null,int maximumBytes=4*1024*1024)
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
        var text=await Community.BoundedHttp.SendAsync(client,request,maximumBytes,stop.Token).ConfigureAwait(false);
        return text.Length==0 ? null:JsonNode.Parse(text);
    }

    /// <summary>
    /// github.json written. X-822 (audit of 22/11): a write that fails (disk full, file locked, no rights) was swallowed without a word; since
    /// X-804 this file holds the gist in use and the issue, so the next exchange could make a new gist and issue and leave the old gist, with its
    /// places and days, never deleted. Now false, said once in the log (until a write succeeds again); <see cref="KeepMeta"/> stops the exchange.
    /// </summary>
    private bool SaveMeta()
    {
        try
        {
            string json;lock(gate)json=JsonSerializer.Serialize(meta,TenshoJson.Plain);
            AtomicFile.Write(MetaPath,json);
        }
        catch(Exception error)
        {
            bool first;lock(gate){first=!metaUnsaved;metaUnsaved=true;}
            if(first)Log?.Invoke($"Discoveries: {Path.GetFileName(MetaPath)} could not be written ({error.GetType().Name}); no gist nor issue is made until it is.");
            return false;
        }
        bool again;lock(gate){again=metaUnsaved;metaUnsaved=false;}
        if(again)Log?.Invoke($"Discoveries: {Path.GetFileName(MetaPath)} written again.");
        return true;
    }
    private bool MetaUnsaved{get{lock(gate)return metaUnsaved;}}
    /// <summary>X-822: github.json written, or the exchange stopped (« ExchangeMetaUnsaved », said on the Discoveries page) before any gist or issue is made.</summary>
    private void KeepMeta()
    {
        if(!SaveMeta())throw new ExchangeRefused("ExchangeMetaUnsaved",false);
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
