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
}

/// <summary>
/// X-77.1 (0.883, user choice: a GitHub sign-in, and the automatic exchange uploads too: yes, opt-in). GitHub's device flow (OAuth App of the repository,
/// client id below, not a secret; no client secret in the plugin): Tensho shows a code, the player approves it on github.com; the token is kept on this PC only,
/// protected by Windows for this user (DPAPI), and can be revoked on GitHub at any time. An exchange: the common file of the repository joined as imported
/// (once per version of the file), then, when connected, the player's own sightings (never imported ones) written to one secret gist of theirs, updated each
/// time, and one issue per player in the repository pointing to it. Started by a click, or once a day by the opt-in setting « automatic exchange ».
/// A discovery never holds a player's name, position or id; the issue's title holds the GitHub login of the account the player connected.
/// </summary>
public sealed class GitHubExchange : IDisposable
{
    public const string ClientId="Ov23liBmk4RCUYw6pU6Z",Scopes="gist public_repo",Repository="Shigu330/Tensho-Assistant",GistFile="tensho-discoveries.b64";
    public static readonly TimeSpan Every=TimeSpan.FromHours(20),Retry=TimeSpan.FromMinutes(15);
    private readonly string folder;
    private readonly HttpClient client=new(){Timeout=TimeSpan.FromSeconds(30)};
    private readonly CancellationTokenSource stop=new();
    private readonly object gate=new();
    private string? token;private GitHubExchangeMeta meta=new();private DateTimeOffset tried;private bool busy;
    private string? userCode,verifyAddress;private ExchangeMessage? message;

    public bool Busy{get{lock(gate)return busy;}}
    public string? UserCode{get{lock(gate)return userCode;}}
    public string? VerifyAddress{get{lock(gate)return verifyAddress;}}
    public ExchangeMessage? Message{get{lock(gate)return message;}}
    public bool Connected{get{lock(gate)return token is not null;}}
    public string Login{get{lock(gate)return meta.Login;}}
    public DateTimeOffset? Exchanged{get{lock(gate)return meta.Exchanged;}}
    private string TokenPath=>Path.Combine(folder,"github.token");
    private string MetaPath=>Path.Combine(folder,"github.json");
    private string CommonMarker=>Path.Combine(folder,"community.sha256");

    public GitHubExchange(string folder)
    {
        this.folder=folder;
        // X-76 H10 (0.923): an unreadable file is kept aside as « .broken » before a new one is started (it was written over later).
        try{if(File.Exists(MetaPath))meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(MetaPath)) ?? new();}
        catch(Exception){meta=new();try{File.Move(MetaPath,MetaPath+".broken",true);}catch(Exception){}}
        try{if(File.Exists(TokenPath))token=Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(TokenPath)));}catch(Exception){token=null;}
    }

    /// <summary>The automatic exchange is due: none for <see cref="Every"/>, and no attempt for <see cref="Retry"/> (a failure is not retried every second).</summary>
    public bool Due(DateTimeOffset now){lock(gate)return !busy && now-tried>=Retry && (meta.Exchanged is not { } last || now-last>=Every);}

    private bool Start(Func<Task<ExchangeMessage>> work)
    {
        lock(gate){if(busy)return false;busy=true;message=null;tried=DateTimeOffset.UtcNow;}
        _=Task.Run(async()=>
        {
            ExchangeMessage result;
            try{result=await work().ConfigureAwait(false);}
            catch(OperationCanceledException){result=new("ExchangeStopped");}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.Unauthorized){Disconnect();result=new("ExchangeUnauthorized");}
            catch(Exception error){result=new("ExchangeFailed",error.GetType().Name);}
            lock(gate){message=result;busy=false;userCode=null;}
        });
        return true;
    }

    public bool Connect()=>Start(ConnectAsync);
    public bool Exchange(DiscoveryStore store,bool upload)=>Start(()=>ExchangeAsync(store,upload));

    /// <summary>Forgets the token on this PC (the authorization itself is revoked on github.com › Settings › Applications).</summary>
    public void Disconnect()
    {
        lock(gate){token=null;meta.Login="";}
        try{File.Delete(TokenPath);}catch(Exception){}
        SaveMeta();
    }

    private async Task<ExchangeMessage> ConnectAsync()
    {
        var code=await Form("https://github.com/login/device/code",new(){["client_id"]=ClientId,["scope"]=Scopes}).ConfigureAwait(false);
        var device=code["device_code"]?.GetValue<string>() ?? throw new InvalidDataException("No device code.");
        var interval=Math.Max(5,code["interval"]?.GetValue<int>() ?? 5);
        var expires=DateTimeOffset.UtcNow.AddSeconds(Math.Min(900,code["expires_in"]?.GetValue<int>() ?? 900));
        lock(gate){userCode=code["user_code"]?.GetValue<string>();verifyAddress=code["verification_uri"]?.GetValue<string>() ?? "https://github.com/login/device";}
        while(DateTimeOffset.UtcNow<expires)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval),stop.Token).ConfigureAwait(false);
            var answer=await Form("https://github.com/login/oauth/access_token",
                new(){["client_id"]=ClientId,["device_code"]=device,["grant_type"]="urn:ietf:params:oauth:grant-type:device_code"}).ConfigureAwait(false);
            if(answer["access_token"]?.GetValue<string>() is { Length:>0 } granted)
            {
                var user=await Api(HttpMethod.Get,"https://api.github.com/user",null,granted).ConfigureAwait(false);
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(TokenPath,UserSecret.Protect(Encoding.UTF8.GetBytes(granted)));
                lock(gate){token=granted;meta.Login=user?["login"]?.GetValue<string>() ?? "";}
                SaveMeta();
                return new("ConnectDone",Login);
            }
            switch(answer["error"]?.GetValue<string>())
            {
                case "authorization_pending":continue;
                case "slow_down":interval+=5;continue;
                case "access_denied":return new("ConnectDenied");
                default:return new("ConnectExpired");
            }
        }
        return new("ConnectExpired");
    }

    private async Task<ExchangeMessage> ExchangeAsync(DiscoveryStore store,bool upload)
    {
        var added=0;var joined=0;string common;
        try
        {
            var json=await Community.BoundedHttp.ReadAsync(client,new Uri(CommunityDiscoveries.Address),CommunityDiscoveries.MaxBytes,stop.Token).ConfigureAwait(false);
            var hash=CommunityDiscoveries.Hash(json);
            if(File.Exists(CommonMarker) && File.ReadAllText(CommonMarker).Trim()==hash)common="CommonSame";
            else if(CommunityDiscoveries.Parse(json) is { } file)
            {
                var result=store.Merge(file.Facts);added=result.Added;joined=result.Joined;
                Directory.CreateDirectory(folder);File.WriteAllText(CommonMarker,hash);common="CommonTaken";
            }
            else common="CommonUnreadable";
        }
        catch(HttpRequestException){common="CommonMissing";}
        var sent="";
        if(upload && Connected)
        {
            var mine=CommunityDiscoveries.Mine(store.Snapshot(DateTimeOffset.UtcNow,false));
            var payload=CommunityDiscoveries.Encode(mine);var hash=CommunityDiscoveries.Hash(payload);
            string known;lock(gate)known=meta.UploadHash;
            if(mine.Facts.Count==0)sent="ShareEmpty";
            else if(hash==known)sent="SentSame";
            else{await Upload(payload).ConfigureAwait(false);lock(gate)meta.UploadHash=hash;sent="Sent";}
        }
        lock(gate)meta.Exchanged=DateTimeOffset.UtcNow;
        SaveMeta();
        return new("ExchangeDone",added.ToString("N0"),joined.ToString("N0"),common,sent);
    }

    private async Task Upload(string payload)
    {
        var files=new JsonObject{[GistFile]=new JsonObject{["content"]=payload}};
        string gist;lock(gate)gist=meta.GistId;
        if(gist.Length>0)
            try{await Api(HttpMethod.Patch,$"https://api.github.com/gists/{gist}",new JsonObject{["files"]=files}).ConfigureAwait(false);}
            catch(HttpRequestException error) when(error.StatusCode==HttpStatusCode.NotFound){gist="";}
        if(gist.Length==0)
        {
            var created=await Api(HttpMethod.Post,"https://api.github.com/gists",
                new JsonObject{["description"]="Tensho Assistant discoveries",["public"]=false,["files"]=files.DeepClone()}).ConfigureAwait(false);
            gist=created?["id"]?.GetValue<string>() ?? throw new InvalidDataException("No gist id.");
            lock(gate){meta.GistId=gist;meta.Issue=0;}
        }
        int issue;lock(gate)issue=meta.Issue;
        if(issue==0)
        {
            var version=typeof(GitHubExchange).Assembly.GetName().Version?.ToString(3) ?? "?";
            var opened=await Api(HttpMethod.Post,$"https://api.github.com/repos/{Repository}/issues",new JsonObject
            {
                ["title"]=$"Discoveries: {Login}",
                ["body"]=$"Shared from Tensho Assistant {version} (World › Discoveries › automatic exchange).\n\nGist: https://gist.github.com/{gist}\n\nThe gist is updated at each exchange; this issue stays the same.",
                ["labels"]=new JsonArray("discoveries"),
            }).ConfigureAwait(false);
            lock(gate)meta.Issue=opened?["number"]?.GetValue<int>() ?? 0;
        }
        SaveMeta();
    }

    private async Task<JsonObject> Form(string address,Dictionary<string,string> fields)
    {
        var request=new HttpRequestMessage(HttpMethod.Post,address){Content=new FormUrlEncodedContent(fields)};
        request.Headers.Accept.ParseAdd("application/json");
        var text=await Community.BoundedHttp.SendAsync(client,request,64*1024,stop.Token).ConfigureAwait(false);
        return JsonNode.Parse(text)?.AsObject() ?? throw new InvalidDataException("Empty answer.");
    }

    private async Task<JsonNode?> Api(HttpMethod method,string address,JsonNode? body,string? with=null)
    {
        var request=new HttpRequestMessage(method,address);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version","2022-11-28");
        string? bearer;lock(gate)bearer=with ?? token;
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

    public void Dispose(){stop.Cancel();client.Dispose();}
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
