using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant.Discoveries;

// 1138 (X-175, user choice of 12/11, option B): the players' GitHub exchange moves to a GitHub App with least permissions once its client id is
// given; until then the OAuth app as before. One switch (GitHubSignIn): the device flow of the App takes no scope; its user token may expire and is
// renewed before an upload with its refresh token, kept protected like the token; a refused refresh disconnects; a refused issue (App not installed
// on the repository) says so and sends nothing else; the gist is always created secret. Behaviour tests (R28): a fake GitHub, a fake clock.
internal static class GitHubApp1138Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,12,12,0,0,TimeSpan.Zero);
    private const string TestAppId="Iv23liTestOnly000000";
    private static readonly GitHubSignIn App=GitHubSignIn.For(TestAppId);

    /// <summary>A fake GitHub: every request recorded (method, address, body, bearer); the answer chosen by <see cref="Answer"/> (404 « {} » otherwise).</summary>
    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<(string Method,string Address,string Body,string? Bearer)> Calls=[];
        public Func<string,string,string,(HttpStatusCode Status,string Json)?> Answer=(_,_,_)=>null;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            lock(Calls)Calls.Add((request.Method.Method,address,body,request.Headers.Authorization?.Parameter));
            var (status,json)=Answer(request.Method.Method,address,body) ?? (HttpStatusCode.NotFound,"{}");
            return new(status){Content=new StringContent(json)};
        }
        public List<(string Method,string Address,string Body,string? Bearer)> Sent{get{lock(Calls)return Calls.Where(call=>call.Method!="GET").ToList();}}
    }

    /// <summary>GitHub's usual answers: the device code, a token (given by <paramref name="token"/>), the user, a new gist g1, an issue 5.</summary>
    private static (HttpStatusCode,string)? Usual(string method,string address,string body,string token,HttpStatusCode issue=HttpStatusCode.Created)
    {
        if(address=="https://github.com/login/device/code")
            return (HttpStatusCode.OK,"{\"device_code\":\"dev\",\"user_code\":\"ABCD-1234\",\"verification_uri\":\"https://github.com/login/device\",\"interval\":0,\"expires_in\":900}");
        if(address=="https://github.com/login/oauth/access_token")return (HttpStatusCode.OK,token);
        if(address=="https://api.github.com/user")return (HttpStatusCode.OK,"{\"login\":\"tester\"}");
        if(method=="POST" && address=="https://api.github.com/gists")return (HttpStatusCode.Created,"{\"id\":\"g1\"}");
        if(method=="PATCH" && address.StartsWith("https://api.github.com/gists/",StringComparison.Ordinal))return (HttpStatusCode.OK,"{}");
        if(method=="POST" && address==$"https://api.github.com/repos/{GitHubExchange.Repository}/issues")
            return issue==HttpStatusCode.Created ? (issue,"{\"number\":5}"):(issue,"{\"message\":\"Resource not accessible by integration\"}");
        return null;
    }

    private static ExchangeMessage Wait(GitHubExchange exchange)
    {
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null,"the work ends");
        return exchange.Message!;
    }
    private static string Secret(string path)=>Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(path)));
    private static bool Clear(string path,string secret)=>Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains(secret,StringComparison.Ordinal)
        || Encoding.Unicode.GetString(File.ReadAllBytes(path)).Contains(secret,StringComparison.Ordinal);
    private static GitHubExchangeMeta Meta(string folder)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(folder,"github.json")))!;
    /// <summary>A folder as a connection leaves it: github.json, and the token (and refresh token) protected.</summary>
    private static void Connected(string folder,GitHubExchangeMeta meta,string token,string? refresh)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"github.json"),JsonSerializer.Serialize(meta));
        File.WriteAllBytes(Path.Combine(folder,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes(token)));
        if(refresh is not null)File.WriteAllBytes(Path.Combine(folder,"github.refresh"),UserSecret.Protect(Encoding.UTF8.GetBytes(refresh)));
    }
    private static DiscoveryStore Sightings(){var store=new DiscoveryStore();store.SeeSource(DiscoveryKind.Venture,5,7,2,At);return store;}
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-app-1138-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GitHub App 1138: the switch: no App id, the OAuth app with its scopes as before; an App id, that id and no scope; a token answer read with its ends",() =>
        {
            Check(GitHubSignIn.For("") == GitHubSignIn.OAuth && GitHubSignIn.For("  ") == GitHubSignIn.OAuth && !GitHubSignIn.OAuth.App,"an empty App id: the OAuth app");
            Check(GitHubSignIn.OAuth.DeviceCodeFields() is { Count:2 } oauth && oauth["client_id"]==GitHubSignIn.OAuthClientId && oauth["scope"]=="gist public_repo","OAuth: client id and scopes");
            Check(App.App && App.DeviceCodeFields() is { Count:1 } app && app["client_id"]==TestAppId,"App: its id, no scope");
            Check(App.RefreshFields("ghr_x") is { Count:3 } fields && fields["grant_type"]=="refresh_token" && fields["refresh_token"]=="ghr_x" && fields["client_id"]==TestAppId,"refresh: no client secret");
            Check(GitHubSignIn.OAuth.Owns("") && GitHubSignIn.OAuth.Owns(GitHubSignIn.OAuthClientId) && !App.Owns("") && App.Owns(TestAppId),"a token of before 1138 is the OAuth app's");
            var token=GitHubSignIn.ReadToken(JsonNode.Parse("{\"access_token\":\"ghu_a\",\"expires_in\":28800,\"refresh_token\":\"ghr_a\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}")!.AsObject(),At)!;
            Check(token.Access=="ghu_a" && token.Refresh=="ghr_a" && token.Expires==At.AddHours(8) && token.RefreshExpires==At.AddSeconds(15897600),"expires_in and refresh_token read");
            Check(GitHubSignIn.ReadToken(JsonNode.Parse("{\"access_token\":\"gho_b\",\"token_type\":\"bearer\",\"scope\":\"gist,public_repo\"}")!.AsObject(),At) is { Refresh:null, Expires:null },"an OAuth token never expires");
            Check(GitHubSignIn.ReadToken(JsonNode.Parse("{\"error\":\"bad_refresh_token\"}")!.AsObject(),At) is null,"an error: no token");
            Check(!GitHubSignIn.Due(null,At) && !GitHubSignIn.Due(At.AddMinutes(6),At) && GitHubSignIn.Due(At.AddMinutes(4),At) && GitHubSignIn.Due(At.AddHours(-1),At),"renewed 5 minutes before its end");
        }),
        ("GitHub App 1138: connecting: the OAuth app sends its client id and scopes, no refresh token kept; the App sends its id without scope, both tokens kept protected",() => InTemp(root =>
        {
            const string oauthToken="gho_oauth_secret_value";
            using var first=new GitHub();
            first.Answer=(method,address,body)=>Usual(method,address,body,$"{{\"access_token\":\"{oauthToken}\",\"token_type\":\"bearer\",\"scope\":\"gist,public_repo\"}}");
            var a=Path.Combine(root,"a");
            using(var exchange=new GitHubExchange(a,first,GitHubSignIn.For(""),()=>At){PollFloor=TimeSpan.Zero})
            {
                Check(exchange.Connect(),"connect starts");
                Check(Wait(exchange) is { Key:"ConnectDone" } && exchange.Connected && exchange.Login=="tester","connected as tester");
            }
            var device=first.Sent.First();
            Check(device.Address=="https://github.com/login/device/code" && device.Body.Contains("client_id="+GitHubSignIn.OAuthClientId) && device.Body.Contains("scope=gist+public_repo"),
                $"the device code asked with the OAuth client id and scopes as before ({device.Body})");
            Check(first.Sent.All(call=>!call.Body.Contains("client_secret")),"no client secret sent");
            Check(Secret(Path.Combine(a,"github.token"))==oauthToken && !Clear(Path.Combine(a,"github.token"),oauthToken),"the token kept protected, never in clear");
            Check(!File.Exists(Path.Combine(a,"github.refresh")) && Meta(a) is { TokenExpires:null, RefreshExpires:null, ClientId:GitHubSignIn.OAuthClientId },"no refresh token, no end");

            const string access="ghu_app_access_value",renewal="ghr_app_refresh_value";
            using var second=new GitHub();
            second.Answer=(method,address,body)=>Usual(method,address,body,
                $"{{\"access_token\":\"{access}\",\"expires_in\":28800,\"refresh_token\":\"{renewal}\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}}");
            var b=Path.Combine(root,"b");
            using(var exchange=new GitHubExchange(b,second,App,()=>At){PollFloor=TimeSpan.Zero})
            {
                Check(exchange.Connect() && Wait(exchange) is { Key:"ConnectDone" } && exchange.Connected,"connected through the App");
            }
            device=second.Sent.First();
            Check(device.Body=="client_id="+TestAppId,$"the device code asked with the App id and no scope ({device.Body})");
            Check(Secret(Path.Combine(b,"github.token"))==access && Secret(Path.Combine(b,"github.refresh"))==renewal,"both tokens kept");
            Check(!Clear(Path.Combine(b,"github.token"),access) && !Clear(Path.Combine(b,"github.refresh"),renewal),"neither in clear");
            var meta=Meta(b);
            Check(meta.ClientId==TestAppId && meta.TokenExpires==At.AddHours(8) && meta.RefreshExpires==At.AddSeconds(15897600),"the ends noted beside the App's id");
            Check(!File.ReadAllText(Path.Combine(b,"github.json")).Contains(renewal) && !File.ReadAllText(Path.Combine(b,"github.json")).Contains(access),"no token in github.json");
            Check(GitHubExchange.SecretFiles.Contains("github.token") && GitHubExchange.SecretFiles.Contains("github.refresh"),"both kept out of the backup copies");
        })),
        ("GitHub App 1138: an expired App token is renewed before the upload, the new pair kept protected; a fresh one is not; the gist is created secret",() => InTemp(root =>
        {
            using var github=new GitHub();
            github.Answer=(method,address,body)=>Usual(method,address,body,
                "{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}");
            Connected(root,new(){Login="tester",ClientId=TestAppId,TokenExpires=At.AddMinutes(-1),RefreshExpires=At.AddDays(100)},"ghu_old","ghr_old");
            using(var exchange=new GitHubExchange(root,github,App,()=>At))
            {
                Check(exchange.Connected,"connected");
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeDone" } done && done.Args[3]=="Sent","sent");
            }
            var sent=github.Sent;
            Check(sent[0].Address=="https://github.com/login/oauth/access_token" && sent[0].Body.Contains("grant_type=refresh_token") && sent[0].Body.Contains("refresh_token=ghr_old")
                && sent[0].Body.Contains("client_id="+TestAppId) && !sent[0].Body.Contains("client_secret"),"the refresh first, with the refresh token and the App id, no secret");
            Check(sent.Skip(1).All(call=>call.Bearer=="ghu_new"),"the upload sent with the renewed token");
            var gist=sent.Single(call=>call.Address=="https://api.github.com/gists");
            Check(JsonNode.Parse(gist.Body)!["public"]!.GetValue<bool>()==false,"the gist created secret (public false)");
            Check(sent.Any(call=>call.Address.EndsWith("/issues",StringComparison.Ordinal)),"the issue opened");
            Check(Secret(Path.Combine(root,"github.token"))=="ghu_new" && Secret(Path.Combine(root,"github.refresh"))=="ghr_new","the new pair kept (GitHub ends the old refresh token)");
            Check(Meta(root) is { TokenExpires:{ } ends, Issue:5, GistId:"g1" } && ends==At.AddHours(8),"its new end noted");

            // Still valid (more than 5 minutes left): no refresh.
            github.Calls.Clear();
            using(var exchange=new GitHubExchange(root,github,App,()=>At.AddHours(1)))
            {
                var store=Sightings();store.SeeSource(DiscoveryKind.Venture,6,7,2,At);
                Check(exchange.Exchange(store,true) && Wait(exchange) is { Key:"ExchangeDone" },"sent again");
            }
            Check(github.Sent.Count>0 && github.Sent.All(call=>call.Address!="https://github.com/login/oauth/access_token" && call.Bearer=="ghu_new"),"a token still valid is used as is");
        })),
        ("GitHub App 1138: a refused refresh (error answer or HTTP 400) or a refresh token past its end disconnects cleanly and asks to connect again; nothing is uploaded",() => InTemp(root =>
        {
            var answers=new (string Name,HttpStatusCode Status,string Json)[]{("error",HttpStatusCode.OK,"{\"error\":\"bad_refresh_token\"}"),("http 400",HttpStatusCode.BadRequest,"{}")};
            foreach(var (name,status,json) in answers)
            {
                var folder=Path.Combine(root,name.Replace(' ','-'));
                using var github=new GitHub();
                github.Answer=(method,address,body)=>address=="https://github.com/login/oauth/access_token" ? (status,json):Usual(method,address,body,"{}");
                Connected(folder,new(){Login="tester",ClientId=TestAppId,GistId="g1",Issue=7,TokenExpires=At.AddHours(-2),RefreshExpires=At.AddDays(30)},"ghu_old","ghr_old");
                using var exchange=new GitHubExchange(folder,github,App,()=>At);
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeExpired" },$"{name}: « connect again »");
                Check(!exchange.Connected && exchange.Login.Length==0,$"{name}: disconnected");
                Check(!File.Exists(Path.Combine(folder,"github.token")) && !File.Exists(Path.Combine(folder,"github.refresh")),$"{name}: both tokens removed from this PC");
                Check(github.Sent.Count==1 && github.Sent[0].Address=="https://github.com/login/oauth/access_token","only the refresh asked: nothing uploaded");
                Check(Meta(folder) is { ClientId:"", TokenExpires:null, RefreshExpires:null, GistId:"g1", Issue:7 },$"{name}: the sign-in forgotten, the gist and issue kept");
            }
            var ended=Path.Combine(root,"ended");
            using var none=new GitHub();none.Answer=(method,address,body)=>Usual(method,address,body,"{}");
            Connected(ended,new(){Login="tester",ClientId=TestAppId,TokenExpires=At.AddDays(-200),RefreshExpires=At.AddDays(-1)},"ghu_old","ghr_old");
            using(var exchange=new GitHubExchange(ended,none,App,()=>At))
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeExpired" } && !exchange.Connected && none.Sent.Count==0,"a refresh token past its end: disconnected without asking GitHub");
            var lost=Path.Combine(root,"lost");
            using var lostServer=new GitHub();lostServer.Answer=(method,address,body)=>Usual(method,address,body,"{}");
            Connected(lost,new(){Login="tester",ClientId=TestAppId,TokenExpires=At.AddHours(-1)},"ghu_old",null);
            using(var exchange=new GitHubExchange(lost,lostServer,App,()=>At))
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeExpired" } && !exchange.Connected && lostServer.Sent.Count==0,"an expired token without refresh token: disconnected");
        })),
        ("GitHub App 1138: an issue refused (403 or 404) with the App says it is not installed on the repository, keeps the gist and the token, and sends nothing else",() => InTemp(root =>
        {
            foreach(var status in new[]{HttpStatusCode.Forbidden,HttpStatusCode.NotFound})
            {
                var folder=Path.Combine(root,((int)status).ToString());
                using var github=new GitHub();
                github.Answer=(method,address,body)=>Usual(method,address,body,"{}",status);
                Connected(folder,new(){Login="tester",ClientId=TestAppId,TokenExpires=At.AddHours(8),RefreshExpires=At.AddDays(30)},"ghu_live","ghr_live");
                using var exchange=new GitHubExchange(folder,github,App,()=>At);
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeAppMissing" },$"{(int)status}: « the App is not installed »");
                var sent=github.Sent;
                Check(sent.Count==2 && sent[0].Address=="https://api.github.com/gists" && sent[1].Address.EndsWith("/issues",StringComparison.Ordinal),$"{(int)status}: the gist, the issue, nothing after");
                Check(exchange.Connected && File.Exists(Path.Combine(folder,"github.token")) && File.Exists(Path.Combine(folder,"github.refresh")),$"{(int)status}: still connected");
                Check(Meta(folder) is { GistId:"g1", Issue:0, UploadHash:"" },$"{(int)status}: the gist kept (no second gist next time), the upload not marked done");
            }
            // The OAuth app as before: a refused issue is a failed exchange, not « App missing ».
            var oauth=Path.Combine(root,"oauth");
            using var before=new GitHub();
            before.Answer=(method,address,body)=>Usual(method,address,body,"{}",HttpStatusCode.Forbidden);
            Connected(oauth,new(){Login="tester"},"gho_live",null);
            using(var exchange=new GitHubExchange(oauth,before,GitHubSignIn.For(""),()=>At))
                Check(exchange.Exchange(Sightings(),true) && Wait(exchange) is { Key:"ExchangeFailed" } && exchange.Connected,"OAuth: failed as before, still connected");
        })),
        ("GitHub App 1138: once the App is in place, a token of the OAuth app is forgotten and the player asked to connect again; with the OAuth app it stays",() => InTemp(root =>
        {
            Connected(root,new(){Login="tester",GistId="g1",Issue=7},"gho_old",null);
            using(var same=new GitHubExchange(root,new GitHub(),GitHubSignIn.For(""),()=>At))
                Check(same.Connected && same.Message is null,"the OAuth app keeps its token (behaviour unchanged)");
            using(var app=new GitHubExchange(root,new GitHub(),App,()=>At))
                Check(!app.Connected && app.Message is { Key:"ExchangeSignInChanged" } && !File.Exists(Path.Combine(root,"github.token")),"the App: the OAuth token forgotten, « connect again »");
            Check(Meta(root) is { Login:"", GistId:"g1", Issue:7 },"the gist and the issue kept for the next upload");
        })),
        ("GitHub App 1138: the wiring: one switch, texts in both languages, secrets out of the backup copy",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
            var exchange=Read("src/TenshoAssistant/Discoveries/GitHubExchange.cs");
            Check(exchange.Contains("signIn.DeviceCodeFields()") && exchange.Contains("signIn.PollFields(device)") && !exchange.Contains(GitHubSignIn.OAuthClientId) && !exchange.Contains("\"scope\""),
                "the client id and scopes come from GitHubSignIn only");
            Check(exchange.Contains("[\"public\"]=false") && !exchange.Contains("[\"public\"]=true"),"a gist is never made public");
            Check(exchange.Contains("await Fresh().ConfigureAwait(false);await Upload(files)"),"renewed before the upload");
            Check(!Read("src/TenshoAssistant/Discoveries/GitHubSignIn.cs").Contains("client_secret"),"no client secret");
            Check(Read("src/TenshoAssistant/MainWindow.Discoveries.cs").Contains("\"discoveries\",DateTimeOffset.UtcNow,GitHubExchange.SecretFiles);"),"the copy before emptying skips both tokens");
            foreach(var language in new[]{"en","fr"})
            {
                var ui=JsonNode.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"))!["DiscoveriesUi"]!.AsObject();
                foreach(var key in new[]{"ExchangeAppMissing","ExchangeExpired","ExchangeSignInChanged"})
                    Check(ui[key]?.GetValue<string>() is { Length:>20 },$"{language}: DiscoveriesUi.{key}");
            }
        }),
    ];
}
