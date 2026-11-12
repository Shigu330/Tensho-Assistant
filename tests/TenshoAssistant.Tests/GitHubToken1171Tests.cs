using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1171 (X-181, night review N2, N7, N8, N29, N30): the GitHub App's token. Two clients of one profile renewing at once (the second takes the
// first's pair, one renewal asked); a Disconnect during a renewal (nothing kept, nothing sent); a renewal GitHub accepted while the plugin unloads
// (kept); the pair's write refused (kept in memory, the upload goes on); a 401 with a refresh token in hand (renewed once, sent again); another
// account after reconnecting (its own gist and issue). Behaviour tests (R28: concurrency, privacy): a fake GitHub, a fake clock, real files.
internal static class GitHubToken1171Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private const string Renewed = "{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}";
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";

    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<(string Method, string Address, string Body, string? Bearer)> Calls = [];
        public Func<string, string, string, string?, (HttpStatusCode Status, string Json)?> Answer = (_, _, _, _) => null;
        public TaskCompletionSource? Hold;
        public readonly TaskCompletionSource Asked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            lock (Calls) Calls.Add((request.Method.Method, address, body, request.Headers.Authorization?.Parameter));
            if (address == RefreshAddress && body.Contains("grant_type=refresh_token") && Hold is { } hold) { Asked.TrySetResult(); await hold.Task.ConfigureAwait(false); }
            var (status, json) = Answer(request.Method.Method, address, body, request.Headers.Authorization?.Parameter) ?? (HttpStatusCode.NotFound, "{}");
            return new(status) { Content = new StringContent(json) };
        }
        public List<(string Method, string Address, string Body, string? Bearer)> Sent { get { lock (Calls) return Calls.Where(call => call.Method != "GET").ToList(); } }
        public int Refreshes { get { lock (Calls) return Calls.Count(call => call.Address == RefreshAddress && call.Body.Contains("grant_type=refresh_token")); } }
    }
    private static (HttpStatusCode, string)? Usual(string method, string address, string login = "tester")
    {
        if (address == "https://github.com/login/device/code")
            return (HttpStatusCode.OK, "{\"device_code\":\"dev\",\"user_code\":\"ABCD-1234\",\"verification_uri\":\"https://github.com/login/device\",\"interval\":0,\"expires_in\":900}");
        if (address == "https://api.github.com/user") return (HttpStatusCode.OK, $"{{\"login\":\"{login}\"}}");
        if (method == "POST" && address == "https://api.github.com/gists") return (HttpStatusCode.Created, "{\"id\":\"g2\"}");
        if (method == "PATCH" && address.StartsWith("https://api.github.com/gists/", StringComparison.Ordinal)) return (HttpStatusCode.OK, "{}");
        if (method == "POST" && address == $"https://api.github.com/repos/{GitHubExchange.Repository}/issues") return (HttpStatusCode.Created, "{\"number\":9}");
        return null;
    }
    private static ExchangeMessage Wait(GitHubExchange exchange)
    {
        for (var wait = 0; exchange.Busy && wait < 3000; wait++) Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null, "the work ends");
        return exchange.Message!;
    }
    private static string? Secret(string path) => File.Exists(path) ? Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(path))) : null;
    private static GitHubExchangeMeta Meta(string folder) => JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(folder, "github.json")))!;
    private static void Connected(string folder, GitHubExchangeMeta meta, string token, string? refresh)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(meta));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes(token)));
        if (refresh is not null) File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes(refresh)));
    }
    private static DiscoveryStore Sightings(uint subject = 5) { var store = new DiscoveryStore(); store.SeeSource(DiscoveryKind.Venture, subject, 7, 2, At); return store; }
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-token-1171-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    // GitHub ends a refresh token once used: ghr_old renews once, then is refused.
    private static Func<string, string, string, string?, (HttpStatusCode, string)?> OneRenewal()
    {
        var used = 0;
        return (method, address, body, _) => address == RefreshAddress && body.Contains("refresh_token=ghr_old")
            ? (Interlocked.Increment(ref used) == 1 ? (HttpStatusCode.OK, Renewed) : (HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}"))
            : Usual(method, address);
    }
    private static GitHubExchangeMeta Due() => new() { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, GistLogin = "tester", TokenExpires = At.AddMinutes(-1), RefreshExpires = At.AddDays(100) };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub token 1171: two clients renewing at once: one renewal asked, the second takes the first's pair; both stay connected (N2)", () => InTemp(root =>
        {
            using var github = new GitHub(); github.Answer = OneRenewal();
            Connected(root, Due(), "ghu_old", "ghr_old");
            using var a = new GitHubExchange(root, github, App, () => At); using var b = new GitHubExchange(root, github, App, () => At);
            Check(a.Exchange(Sightings(), true) && Wait(a) is { Key: "ExchangeDone" } first && first.Args[3] == "Sent", "the first sent");
            Check(b.Exchange(Sightings(6), true) && Wait(b) is { Key: "ExchangeDone" } second && second.Args[3] == "Sent", "the second sent too, not disconnected: " + b.Message?.Key);
            Check(github.Refreshes == 1, "one renewal asked: " + github.Refreshes);
            Check(a.Connected && b.Connected && Secret(Path.Combine(root, "github.token")) == "ghu_new" && Secret(Path.Combine(root, "github.refresh")) == "ghr_new", "the new pair kept on disk");
            Check(github.Sent.Where(call => call.Address != RefreshAddress).All(call => call.Bearer == "ghu_new"), "every upload with the renewed token");
        })),
        ("GitHub token 1171: a refusal never removes a pair another client renewed meanwhile (N2)", () => InTemp(root =>
        {
            using var github = new GitHub();
            github.Answer = (method, address, body, bearer) => address == RefreshAddress ? (HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}") : Usual(method, address);
            Connected(root, Due(), "ghu_old", "ghr_old");
            using var b = new GitHubExchange(root, github, App, () => At);
            Connected(root, Due(), "ghu_other", "ghr_other");            // the other client renewed after b read the files
            File.WriteAllText(Path.Combine(root, "github.json"), JsonSerializer.Serialize(new GitHubExchangeMeta { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, TokenExpires = At.AddHours(8), RefreshExpires = At.AddDays(100) }));
            Check(b.Exchange(Sightings(), true) && Wait(b) is { Key: "ExchangeDone" }, "the other's pair taken: " + b.Message?.Key);
            Check(github.Refreshes == 0 && b.Connected && Secret(Path.Combine(root, "github.refresh")) == "ghr_other", "no renewal asked, nothing removed");
        })),
        ("GitHub token 1171: Disconnect during a renewal: nothing kept, no token file, nothing sent after (N7)", () => InTemp(root =>
        {
            using var github = new GitHub { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) }; github.Answer = OneRenewal();
            Connected(root, Due(), "ghu_old", "ghr_old");
            using var exchange = new GitHubExchange(root, github, App, () => At);
            Check(exchange.Exchange(Sightings(), true), "started");
            Check(github.Asked.Task.Wait(TimeSpan.FromSeconds(10)), "the renewal asked");
            exchange.Disconnect();
            github.Hold!.SetResult();
            Wait(exchange);
            Check(!exchange.Connected && exchange.Login.Length == 0, "disconnected");
            Check(!File.Exists(Path.Combine(root, "github.token")) && !File.Exists(Path.Combine(root, "github.refresh")), "no token file came back");
            Check(github.Sent.All(call => !call.Address.Contains("/gists") && !call.Address.Contains("/issues")), "no gist nor issue request after the Disconnect");
        })),
        ("GitHub token 1171: a renewal GitHub accepted while the plugin unloads is kept (N8)", () => InTemp(root =>
        {
            using var github = new GitHub { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) }; github.Answer = OneRenewal();
            Connected(root, Due(), "ghu_old", "ghr_old");
            var exchange = new GitHubExchange(root, github, App, () => At);
            Check(exchange.Exchange(Sightings(), true) && github.Asked.Task.Wait(TimeSpan.FromSeconds(10)), "the renewal asked");
            _ = Task.Delay(300).ContinueWith(_ => github.Hold!.SetResult());
            exchange.Dispose();
            Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "the new pair on disk after the unload");
        })),
        ("GitHub token 1171: the pair's write refused: kept in memory, the upload goes on with it, written once possible (N8)", () => InTemp(root =>
        {
            using var github = new GitHub(); github.Answer = OneRenewal();
            Connected(root, Due(), "ghu_old", "ghr_old");
            var refreshFile = Path.Combine(root, "github.refresh");
            File.SetAttributes(refreshFile, FileAttributes.ReadOnly);
            using (var exchange = new GitHubExchange(root, github, App, () => At))
            {
                Check(exchange.Exchange(Sightings(), true) && Wait(exchange) is { Key: "ExchangeDone" } done && done.Args[3] == "Sent", "sent with the pair in memory: " + exchange.Message?.Key);
                Check(github.Sent.Where(call => call.Address != RefreshAddress).All(call => call.Bearer == "ghu_new") && exchange.Connected, "the renewed token used, still connected");
                File.SetAttributes(refreshFile, FileAttributes.Normal);
                Check(exchange.Exchange(Sightings(7), true) && Wait(exchange) is { Key: "ExchangeDone" }, "the next exchange");
                Check(github.Refreshes == 1, "no second renewal (the old refresh token is gone at GitHub)");
            }
            Check(Secret(refreshFile) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "written once the file could be written");
        })),
        ("GitHub token 1171: a 401 with a refresh token in hand: renewed once, sent again, still connected (N29)", () => InTemp(root =>
        {
            using var github = new GitHub(); var renewal = OneRenewal();
            github.Answer = (method, address, body, bearer) => method == "PATCH" && bearer == "ghu_old" ? (HttpStatusCode.Unauthorized, "{}") : renewal(method, address, body, bearer);
            var later = Due(); later.TokenExpires = At.AddHours(2); Connected(root, later, "ghu_old", "ghr_old");
            using var exchange = new GitHubExchange(root, github, App, () => At);
            Check(exchange.Exchange(Sightings(), true) && Wait(exchange) is { Key: "ExchangeDone" } done && done.Args[3] == "Sent", "sent after the renewal: " + exchange.Message?.Key);
            Check(exchange.Connected && github.Refreshes == 1 && Secret(Path.Combine(root, "github.refresh")) == "ghr_new", "connected, renewed once");
        })),
        ("GitHub token 1171: another account after reconnecting gets its own gist and issue; the same account keeps them (N30)", () => InTemp(root =>
        {
            using var github = new GitHub();
            github.Answer = (method, address, body, _) => address == RefreshAddress ? (HttpStatusCode.OK, "{\"access_token\":\"ghu_b\",\"token_type\":\"bearer\",\"scope\":\"\"}") : Usual(method, address, "b");
            File.WriteAllText(Path.Combine(root, "github.json"), JsonSerializer.Serialize(new GitHubExchangeMeta { GistId = "g1", Issue = 7, GistLogin = "a", UploadHash = "X", GistFiles = ["index.json"] }));
            using var exchange = new GitHubExchange(root, github, App, () => At) { PollFloor = TimeSpan.Zero };
            Check(exchange.Connect() && Wait(exchange) is { Key: "ConnectDone" } && exchange.Login == "b", "connected as b");
            Check(Meta(root) is { GistId: "", Issue: 0, UploadHash: "" }, "a's gist and issue not used for b");
            Check(exchange.Exchange(Sightings(), true) && Wait(exchange) is { Key: "ExchangeDone" }, "exchanged");
            Check(github.Sent.Any(call => call.Method == "POST" && call.Address == "https://api.github.com/gists") && github.Sent.Any(call => call.Address.EndsWith("/issues", StringComparison.Ordinal))
                && !github.Sent.Any(call => call.Address.EndsWith("/gists/g1", StringComparison.Ordinal)), "a new gist and a new issue, never a's gist");
            Check(Meta(root) is { GistId: "g2", Issue: 9, GistLogin: "b" }, "b's gist noted with its login");
        })),
    ];
}
