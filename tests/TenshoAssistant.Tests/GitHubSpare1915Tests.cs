using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1915 (X-894, review of 0.1913 M1). A pair renewed while github.refresh refused it is kept aside in the spare; the next start takes it
// back from the spare but cannot write it (another client holds the profile's lock). At the next exchange the spare cannot be read for a
// moment (a scanner): the files' pair, the older one whose refresh token GitHub ended at the renewal, was taken for a newer pair another
// client had renewed, and the valid pair was left in memory (kept by the spare alone). Behaviour test (R28.14): the old code sends the
// ended token and gets « unauthorised »; now the pair in memory is used, stays unsaved, and is written to its files once the spare reads.
internal static class GitHubSpare1915Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private static readonly TimeSpan Patient = TimeSpan.FromSeconds(60);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";

    // ghr_old gives ghu_new/ghr_new once; any other refresh token (or a second use) is refused. ghu_old, the ended pair's token, is refused.
    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<string> Used = [];
        public readonly List<string> Sent = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (address == RefreshAddress && body.Contains("grant_type=refresh_token"))
            {
                bool first;
                lock (Used) { first = body.Contains("refresh_token=ghr_old") && !Used.Contains("ghr_old"); Used.Add(body.Contains("refresh_token=ghr_old") ? "ghr_old" : "other"); }
                return !first ? Answer(HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}")
                    : Answer(HttpStatusCode.OK, "{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}");
            }
            if (request.Method == HttpMethod.Patch && address.StartsWith("https://api.github.com/gists/", StringComparison.Ordinal))
            {
                var used = request.Headers.Authorization?.Parameter ?? "";
                lock (Sent) Sent.Add(used);
                return used == "ghu_old" ? Answer(HttpStatusCode.Unauthorized, "{}") : Answer(HttpStatusCode.OK, "{\"history\":[{\"version\":\"r1\"}]}");
            }
            return Answer(HttpStatusCode.NotFound, "{}");
        }
        private static HttpResponseMessage Answer(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json) };
        public string[] Renewals() { lock (Used) return [.. Used]; }
        public string[] Tokens() { lock (Sent) return [.. Sent]; }
    }

    private static void Connected(string folder)
    {
        Directory.CreateDirectory(folder);
        var meta = new GitHubExchangeMeta { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, GistLogin = "tester", GistFiles = ["index.json"], TokenExpires = At.AddMinutes(-1), RefreshExpires = At.AddDays(100) };
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(meta));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_old")));
        File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghr_old")));
    }
    private static string? Secret(string path) => File.Exists(path) ? Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(path))) : null;
    private static DiscoveryStore Sightings(uint item) { var store = new DiscoveryStore(); store.SeeSource(DiscoveryKind.Venture, 5, item, 2, At); return store; }
    private static void Ended(GitHubExchange exchange)
    {
        for (var wait = 0; exchange.Busy && wait < 6000; wait++) Thread.Sleep(10);
        Check(!exchange.Busy, "the exchange ended");
    }
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-spare-1915-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    // A handle on the spare that refuses any read but lets a delete through, as a scanner or a sync tool may hold it for a moment.
    private static FileStream Held(string spare)
    {
        var held = new FileStream(spare, FileMode.Open, FileAccess.Read, FileShare.Delete);
        var refused = false;
        try { File.ReadAllBytes(spare); } catch (IOException) { refused = true; }
        Check(refused, "the spare cannot be read while held");
        return held;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub spare 1915: behaviour: the pair taken back from a spare that cannot be read at the next exchange is used, not the files' ended pair; written once the spare reads (X-894)", () => InTemp(root =>
        {
            var github = new GitHub();
            Connected(root);
            var refresh = Path.Combine(root, "github.refresh");
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            // ghu_new/ghr_new renewed while github.refresh refuses any write: kept aside in the spare only; ghr_old is ended.
            File.SetAttributes(refresh, FileAttributes.ReadOnly);
            var first = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(first.Exchange(Sightings(7), true), "first started");
            Ended(first);
            Check(first.Message is { Key: "ExchangeDone" }, "renewed and sent: " + first.Message?.Key);
            first.Dispose();
            File.SetAttributes(refresh, FileAttributes.Normal);
            Check(File.Exists(spare) && Secret(refresh) == "ghr_old", "ghr_new kept aside only");
            // The next start takes the spare's pair back but cannot write it: another client holds the profile's lock.
            GitHubExchange second;
            using (new FileStream(Path.Combine(root, "github.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                second = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(second.Connected && Secret(refresh) == "ghr_old", "the spare's pair taken back, not written yet");
            using (Held(spare))
            {
                Check(second.Exchange(Sightings(8), true), "second started");
                Ended(second);
                // The old code took ghu_old/ghr_old from the files here and sent ghu_old: « ExchangeUnauthorized ».
                Check(second.Message is { Key: "ExchangeDone" }, "sent with the pair in memory: " + second.Message?.Key + " " + string.Join(",", github.Tokens()));
                Check(!github.Tokens().Contains("ghu_old"), "the ended token never sent: " + string.Join(",", github.Tokens()));
                Check(Secret(refresh) == "ghr_old" && Secret(Path.Combine(root, "github.token")) == "ghu_old", "nothing written while the spare cannot be read");
            }
            // A crash of the game (no Dispose): the next start reads the spare and writes its pair to the files.
            var third = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(third.Connected && Secret(refresh) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "the renewed pair written to its files at the next start");
            third.Dispose();
            second.Dispose();
            Check(Secret(refresh) == "ghr_new" && !File.Exists(spare), "the renewed pair stays, the spare removed");
            Check(github.Renewals().SequenceEqual(["ghr_old"]), "one renewal only: " + string.Join(",", github.Renewals()));
        })),
    ];
}
