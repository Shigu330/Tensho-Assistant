using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant.Discoveries;

// 0.1563 (X-623, review of 0.1562, I1; X-624 M1): behaviour tests (R28.14). A row of yours set aside, then an exchange with a GitHub App token
// past its 8 h: the gist is read with the token renewed first (before, a 401 taken for « GitHub not reached » blocked the send at every
// exchange); a 401 with a refresh token in hand renews once; without one, or a renewal refused: the sign-in ends, nothing sent, nothing lost.
// A cut file's raw address refused while the listing answered: « not reached », the rows kept pending, never settled with nothing kept.
internal static class AsideToken1563Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow;
    private const string TestAppId="Iv23liTestOnly000000";
    private const string RefreshAddress="https://github.com/login/oauth/access_token";
    private const string Renewed="{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}";
    private const string RawPrefix=GistAside.RawHost+"tester/g1/raw/abc/";

    /// <summary>A fake GitHub that checks the token: the gist answers 401 to any token but <see cref="Valid"/>; ghr_old renews to ghu_new.</summary>
    private sealed class GitHub : HttpMessageHandler
    {
        public IReadOnlyDictionary<string,string> Files=new Dictionary<string,string>();
        public string Valid="ghu_new";
        public bool RenewalRefused;
        public HttpStatusCode Raw=HttpStatusCode.OK;
        public readonly List<(string Method,string Address,string? Bearer)> Calls=[];
        public readonly List<string> Patches=[];
        public int Refreshes{get{lock(Calls)return Calls.Count(call=>call.Address==RefreshAddress);}}
        public List<string?> GistBearers{get{lock(Calls)return Calls.Where(call=>call.Address=="https://api.github.com/gists/g1").Select(call=>call.Bearer).ToList();}}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var bearer=request.Headers.Authorization?.Parameter;
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            lock(Calls)Calls.Add((request.Method.Method,address,bearer));
            if(address==RefreshAddress)
                return new(HttpStatusCode.OK){Content=new StringContent(!RenewalRefused && body.Contains("refresh_token=ghr_old") ? Renewed:"{\"error\":\"bad_refresh_token\"}")};
            if(address=="https://api.github.com/gists/g1")
            {
                if(bearer!=Valid)return new(HttpStatusCode.Unauthorized){Content=new StringContent("{\"message\":\"Bad credentials\"}")};
                if(request.Method==HttpMethod.Get)
                {
                    var files=new JsonObject();
                    foreach(var (name,content) in Files)
                        files[name]=name==ExchangeV3.IndexName ? new JsonObject{["content"]=content,["truncated"]=false}
                            :new JsonObject{["content"]=content[..Math.Min(8,content.Length)],["truncated"]=true,["raw_url"]=RawPrefix+name};
                    return new(HttpStatusCode.OK){Content=new StringContent(new JsonObject{["id"]="g1",["files"]=files}.ToJsonString())};
                }
                lock(Patches)Patches.Add(body);
                return new(HttpStatusCode.OK){Content=new StringContent("{}")};
            }
            if(address.StartsWith(RawPrefix,StringComparison.Ordinal))
                return Raw!=HttpStatusCode.OK || bearer is not null ? new(Raw==HttpStatusCode.OK ? HttpStatusCode.BadRequest:Raw)
                    :new(HttpStatusCode.OK){Content=new StringContent(Files[address[RawPrefix.Length..]])};
            return new(HttpStatusCode.NotFound);
        }
    }

    private static GitHubExchange Connected(string root,GitHub github,DateTimeOffset? expires,string? refresh)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",GistLogin="tester",Issue=7,
            ClientId=TestAppId,TokenExpires=expires,RefreshExpires=refresh is null ? null:At.AddDays(100)}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_old")));
        if(refresh is not null)File.WriteAllBytes(Path.Combine(root,"github.refresh"),UserSecret.Protect(Encoding.UTF8.GetBytes(refresh)));
        var exchange=new GitHubExchange(root,github,GitHubSignIn.For(TestAppId),()=>At);
        Check(exchange.Connected,"connected");
        return exchange;
    }

    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null,"the exchange ends");
        return exchange.Message!;
    }

    private static Dictionary<string,DiscoveryFact> Sent(GitHub github)
    {
        var files=JsonNode.Parse(github.Patches[^1])!["files"]!.AsObject();
        var index=ExchangeV3.ParseIndex(files[ExchangeV3.IndexName]!["content"]!.GetValue<string>())!;
        var facts=new Dictionary<string,DiscoveryFact>();
        foreach(var entry in index.Files)
            foreach(var fact in ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(files[entry.Name+ExchangeV3.GistSuffix]!["content"]!.GetValue<string>())),entry).Facts)
                facts[fact.Key]=fact;
        return facts;
    }

    private static string Said(ExchangeMessage message)=>message.Key+" "+string.Join(", ",message.Args);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Aside token 1563: I1, a token past its 8 h: renewed before the gist is read, B read back and sent with the new token",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=AsideGist1561Tests.Damaged(root);
            var github=new GitHub{Files=gist};
            using var exchange=Connected(root,github,At.AddMinutes(-1),"ghr_old");
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="Sent","sent, not « ShareAsideWaiting »: "+Said(said));
            Check(github.Refreshes==1 && github.GistBearers.Count>=2 && github.GistBearers.All(bearer=>bearer=="ghu_new"),"one renewal, the gist read and sent with the new token only: "+string.Join(", ",github.GistBearers));
            Check(Sent(github).TryGetValue(b,out var kept) && kept.Seen==bSeen && Sent(github).ContainsKey(a),"the gist keeps A and B");
            Check(AsideGist1561Tests.Row(database,b)?.Seen==bSeen && database.AsidePending().Count==0 && store.Aside is null,"B back in your base, settled");
            Check(exchange.Connected,"still connected");
        })),
        ("Aside token 1563: I1, a 401 while the PC clock says the token is good (refresh token in hand): renewed once, read again, sent",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,bSeen)=AsideGist1561Tests.Damaged(root);
            var github=new GitHub{Files=gist};
            using var exchange=Connected(root,github,At.AddHours(6),"ghr_old");
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="Sent","sent: "+Said(said));
            Check(github.Refreshes==1 && github.GistBearers.First()=="ghu_old" && github.GistBearers.Skip(1).All(bearer=>bearer=="ghu_new"),"refused once, renewed once, then the new token: "+string.Join(", ",github.GistBearers));
            Check(Sent(github)[b].Seen==bSeen && database.AsidePending().Count==0,"B sent and settled");
        })),
        ("Aside token 1563: I1, a 401 and no refresh token: the sign-in refused as the send says, nothing sent, B still pending (nothing lost)",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_)=AsideGist1561Tests.Damaged(root);
            var github=new GitHub{Files=gist};
            using var exchange=Connected(root,github,null,null);
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(said.Key=="ExchangeUnauthorized","« connect again », never « GitHub not reached »: "+Said(said));
            Check(github.Patches.Count==0 && github.Refreshes==0,"nothing sent, no renewal asked");
            Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null && AsideGist1561Tests.Row(database,b) is null,"B still pending, its copy kept on the gist");
        })),
        ("Aside token 1563: I1, the renewal refused (sign-in ended): « expired », nothing read nor sent with the old token, B still pending",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_)=AsideGist1561Tests.Damaged(root);
            var github=new GitHub{Files=gist,RenewalRefused=true};
            using var exchange=Connected(root,github,At.AddMinutes(-1),"ghr_old");
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(said.Key=="ExchangeExpired","« expired, connect again »: "+Said(said));
            Check(github.GistBearers.Count==0 && github.Patches.Count==0,"the gist never asked with the expired token, nothing sent");
            Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null,"B still pending");
        })),
        ("Aside token 1563: M1, a cut file's raw address refused (404, 401) while the listing answered: « not reached », nothing settled, still connected",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,bSeen)=AsideGist1561Tests.Damaged(root);
            var github=new GitHub{Files=gist,Valid="ghu_old",Raw=HttpStatusCode.NotFound};
            using var exchange=Connected(root,github,null,null);
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="ShareAsideWaiting" && github.Patches.Count==0,"a raw 404: nothing sent: "+Said(said));
            Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null,"B still pending, never settled with nothing kept");
            github.Raw=HttpStatusCode.Unauthorized;
            said=Run(exchange,store);
            Check(said.Args.Length>3 && said.Args[3]=="ShareAsideWaiting" && exchange.Connected && github.Patches.Count==0,"a raw 401: not a refused sign-in: "+Said(said));
            github.Raw=HttpStatusCode.OK;
            said=Run(exchange,store);
            Check(said.Args[3]=="Sent" && Sent(github)[b].Seen==bSeen && database.AsidePending().Count==0,"the raw file read: B sent and settled");
        })),
        ("Aside token 1563: I1 wiring: the token renewed before the gist is read; the listing's 401 renewed once; the raw read never sends the token",() =>
        {
            var exchange=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).Replace("\r\n","\n");
            var keep=exchange.IndexOf("private async Task<bool> KeepAside(",StringComparison.Ordinal);
            var fresh=exchange.IndexOf("await Fresh().ConfigureAwait(false);",keep,StringComparison.Ordinal);
            Check(keep>0 && fresh>keep && fresh<exchange.IndexOf("GistAside.Read(",keep,StringComparison.Ordinal),"Fresh before the read");
            Check(exchange.Contains("error is not HttpRequestException{StatusCode:HttpStatusCode.Unauthorized}){return false;}",StringComparison.Ordinal),"a 401 is never « not reached »");
            var raw=exchange[exchange.IndexOf("private async Task<string> AsideRaw(",StringComparison.Ordinal)..];
            raw=raw[..raw.IndexOf("\n    }\n",StringComparison.Ordinal)];
            Check(raw.Contains("BoundedHttp.ReadAsync(client,",StringComparison.Ordinal) && !raw.Contains("Api(",StringComparison.Ordinal),"the raw read goes without the token");
        }),
    ];
}
