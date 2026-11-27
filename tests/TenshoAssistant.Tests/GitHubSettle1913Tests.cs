using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1913 (X-892, review of 0.1912). M1: a pair GitHub had just renewed (the old refresh token ended) was written neither to its files nor
// to the spare while the spare alone could not be read (a scanner holding it): it stayed in memory only, lost at a crash of the game. The
// files readable, it is written to them at once again. M2: the start and the unload (the thread that loads the plugin) waited up to 2 s for
// the profile's lock, twice at the unload, and each read retried up to 200 ms; they no longer wait more than before 0.1910 (the lock only
// when free at once, each read tried once). Behaviour tests (R28.14): a spare held by a handle that refuses reads, another client holding
// the profile's lock, a fake GitHub that rotates the refresh token.
internal static class GitHubSettle1913Tests
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
                return request.Headers.Authorization?.Parameter == "ghu_old" ? Answer(HttpStatusCode.Unauthorized, "{}")
                    : Answer(HttpStatusCode.OK, "{\"history\":[{\"version\":\"r1\"}]}");
            return Answer(HttpStatusCode.NotFound, "{}");
        }
        private static HttpResponseMessage Answer(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json) };
        public string[] Renewals() { lock (Used) return [.. Used]; }
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
        var root = Path.Combine(Path.GetTempPath(), "tensho-settle-1913-" + Guid.NewGuid().ToString("N"));
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
        ("GitHub settle 1913: behaviour: a pair GitHub renewed while only the spare cannot be read is written to its files at once, kept through a crash (X-892 M1)", () => InTemp(root =>
        {
            var github = new GitHub();
            Connected(root);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            File.WriteAllBytes(spare, [1, 2, 3]);
            File.SetLastWriteTimeUtc(spare, File.GetLastWriteTimeUtc(Path.Combine(root, "github.token")).AddMinutes(-5));
            var exchange = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            using (Held(spare))
            {
                Check(exchange.Exchange(Sightings(7), true), "started");
                Ended(exchange);
                Check(exchange.Message is { Key: "ExchangeDone" } && github.Renewals().SequenceEqual(["ghr_old"]), "renewed and sent: " + exchange.Message?.Key);
                // The old 0.1910 code kept ghu_new/ghr_new in memory only here (the spare not readable: « Unknown »).
                Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "written to its files while the spare is held");
            }
            // A crash of the game: no Dispose. Another start reads the renewed pair from its files.
            var after = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(after.Connected && Secret(Path.Combine(root, "github.refresh")) == "ghr_new", "the renewed pair kept after the crash");
            after.Dispose();
        })),
        ("GitHub settle 1913: behaviour: the start and the unload wait no longer for another client's lock; the pair kept aside stays, taken back at the next start (X-892 M2)", () => InTemp(root =>
        {
            var github = new GitHub();
            Connected(root);
            var refresh = Path.Combine(root, "github.refresh");
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            // ghu_new/ghr_new renewed while github.refresh refuses any write: kept aside in the spare only.
            File.SetAttributes(refresh, FileAttributes.ReadOnly);
            var first = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(first.Exchange(Sightings(7), true), "started");
            Ended(first);
            Check(first.Message is { Key: "ExchangeDone" }, "renewed and sent: " + first.Message?.Key);
            first.Dispose();
            File.SetAttributes(refresh, FileAttributes.Normal);
            Check(File.Exists(spare) && Secret(refresh) == "ghr_old", "ghr_new kept aside only");
            var said = new List<string>();
            var timer = Stopwatch.StartNew();
            using (new FileStream(Path.Combine(root, "github.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var held = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient, Log = line => { lock (said) said.Add(line); } };
                Check(held.Connected, "the spare's pair taken back");
                held.Dispose();
            }
            // Before: 2 s at the start, then 2 s, 250 ms and 2 s at the unload (at least 6.25 s); now two tries 250 ms apart.
            Check(timer.Elapsed < TimeSpan.FromSeconds(4), "the load and unload thread not held by the lock: " + timer.Elapsed);
            Check(File.Exists(spare) && Secret(refresh) == "ghr_old", "the spare kept, the files untouched without the lock");
            lock (said) Check(said.Count(line => line.Contains("kept aside in " + GitHubExchange.SpareFile, StringComparison.Ordinal)) == 1
                && !said.Any(line => line.Contains("ask to connect again", StringComparison.Ordinal)), "the log says the pair is kept aside: " + string.Join(" | ", said));
            var later = new GitHubExchange(root, github, App, () => At);
            Check(later.Connected && Secret(refresh) == "ghr_new" && !File.Exists(spare), "written to its files at the next start, the spare removed");
            later.Dispose();
            Check(github.Renewals().SequenceEqual(["ghr_old"]), "one renewal only: " + string.Join(",", github.Renewals()));
        })),
    ];
}
