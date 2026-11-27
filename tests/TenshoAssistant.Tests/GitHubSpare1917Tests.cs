using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1917 (X-895, review of 0.1915 M1, M2). A client holds in memory the pair kept aside in the spare (taken back at its start, not written:
// another client held the profile's lock). M1: another client then took the same spare back, wrote it to its files without removing the
// spare, and renewed it into a newer pair in the files. While the spare cannot be read, the first client kept its spare's pair, whose refresh
// token GitHub had ended, and the exchange said the sign-in expired; now the files' newer pair is taken (files written after the spare).
// M2: at the unload, the spare's pair not written because the spare could not be read was logged as lost (« the old one has ended »); it is
// kept aside in the spare, and the log says so. Behaviour tests (R28.14).
internal static class GitHubSpare1917Tests
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
                var used = body.Contains("refresh_token=ghr_old") ? "ghr_old" : body.Contains("refresh_token=ghr_new") ? "ghr_new" : "other";
                lock (Used) { first = used == "ghr_old" && !Used.Contains("ghr_old"); Used.Add(used); }
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

    private static GitHubExchangeMeta Meta(DateTimeOffset expires) => new() { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, GistLogin = "tester", GistFiles = ["index.json"], TokenExpires = expires, RefreshExpires = At.AddDays(100) };
    private static void Pair(string folder, string access, string renewal, DateTimeOffset expires)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(Meta(expires)));
        File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes(renewal)));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes(access)));
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
        var root = Path.Combine(Path.GetTempPath(), "tensho-spare-1917-" + Guid.NewGuid().ToString("N"));
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
    // ghu_new/ghr_new renewed while github.refresh refuses any write: kept aside in the spare only (ghr_old ended). Then a client that takes
    // the spare's pair back at its start but cannot write it (another client holds the profile's lock).
    private static GitHubExchange Spared(string root, GitHub github, List<string> log)
    {
        Pair(root, "ghu_old", "ghr_old", At.AddMinutes(-1));
        var refresh = Path.Combine(root, "github.refresh");
        File.SetAttributes(refresh, FileAttributes.ReadOnly);
        var first = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
        Check(first.Exchange(Sightings(7), true), "first started");
        Ended(first);
        Check(first.Message is { Key: "ExchangeDone" }, "renewed and sent: " + first.Message?.Key);
        first.Dispose();
        File.SetAttributes(refresh, FileAttributes.Normal);
        Check(File.Exists(Path.Combine(root, GitHubExchange.SpareFile)) && Secret(refresh) == "ghr_old", "ghr_new kept aside only");
        GitHubExchange second;
        using (new FileStream(Path.Combine(root, "github.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            second = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient, Log = line => { lock (log) log.Add(line); } };
        Check(second.Connected && Secret(refresh) == "ghr_old", "the spare's pair taken back, not written yet");
        return second;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub spare 1917: behaviour: a newer pair another client renewed in the files after the spare is taken while the spare cannot be read (X-895 M1)", () => InTemp(root =>
        {
            var github = new GitHub();
            var log = new List<string>();
            var second = Spared(root, github, log);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            // Another client took the spare's pair back, wrote it to its files (the spare not removed), then renewed it: ghr_new is ended
            // at GitHub, ghu_newer/ghr_newer are in the files, written after the spare.
            Thread.Sleep(30);
            Pair(root, "ghu_newer", "ghr_newer", At.AddHours(8));
            Check(File.GetLastWriteTimeUtc(Path.Combine(root, "github.token")) > File.GetLastWriteTimeUtc(spare), "the files written after the spare");
            using (Held(spare))
            {
                Check(second.Exchange(Sightings(8), true), "second started");
                Ended(second);
                // The old code kept ghu_new/ghr_new (the spare's, whose refresh token is ended) and sent ghu_new.
                Check(second.Message is { Key: "ExchangeDone" }, "sent with the files' newer pair: " + second.Message?.Key + " " + string.Join(",", github.Renewals()));
                Check(github.Tokens().LastOrDefault() == "ghu_newer", "the newer token sent: " + string.Join(",", github.Tokens()));
                Check(github.Renewals().SequenceEqual(["ghr_old"]), "no renewal with the ended refresh token: " + string.Join(",", github.Renewals()));
                Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_newer", "the newer pair left in its files");
            }
            second.Dispose();
            Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_newer" && Secret(Path.Combine(root, "github.token")) == "ghu_newer", "the newer pair never written over");
            lock (log) Check(!log.Any(line => line.Contains("has ended", StringComparison.Ordinal)), "no sign-in said lost: " + string.Join(" | ", log));
        })),
        ("GitHub spare 1917: behaviour: the spare's pair not written at the unload because the spare cannot be read is said kept aside, not lost; taken back at the next start (X-895 M2)", () => InTemp(root =>
        {
            var github = new GitHub();
            var log = new List<string>();
            var second = Spared(root, github, log);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            var refresh = Path.Combine(root, "github.refresh");
            using (Held(spare)) second.Dispose();
            lock (log)
            {
                // The old code logged « the old one has ended, so the Discoveries page will ask to connect again ».
                Check(!log.Any(line => line.Contains("has ended", StringComparison.Ordinal)), "the sign-in not said lost: " + string.Join(" | ", log));
                Check(log.Any(line => line.Contains("kept aside in " + GitHubExchange.SpareFile, StringComparison.Ordinal)), "said kept aside: " + string.Join(" | ", log));
            }
            Check(File.Exists(spare) && Secret(refresh) == "ghr_old", "the files untouched, the spare kept");
            var third = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(third.Connected && Secret(refresh) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "the spare's pair written to its files at the next start");
            third.Dispose();
            Check(github.Renewals().SequenceEqual(["ghr_old"]), "one renewal only: " + string.Join(",", github.Renewals()));
        })),
    ];
}
