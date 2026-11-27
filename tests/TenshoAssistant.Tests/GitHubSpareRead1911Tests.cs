using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1911 (X-889 M1, review of 0.1909): github.spare may hold the only copy of a renewed pair (GitHub ended the old refresh token). It was
// removed at the start as soon as it could not be read, a read refused for a moment (a scanner, OneDrive, another client replacing it)
// included, and a refusal of GitHub then signed the player out and removed it too. It is now removed only when it was read and found older
// than the two files; a disk not readable whole now removes nothing. Behaviour tests (R28.14): the spare held by a handle that refuses reads
// but allows a delete (as a scanner may), a fake GitHub that rotates the refresh token.
internal static class GitHubSpareRead1911Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private static readonly TimeSpan Patient = TimeSpan.FromSeconds(60);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";

    // ghr_old gives ghu_new/ghr_new once, ghr_new gives ghu_third/ghr_third once; any other (or a second use) is refused. ghu_old, the
    // ended pair's access token, is refused.
    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<string> Used = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (address == RefreshAddress && body.Contains("grant_type=refresh_token"))
            {
                string? next;
                lock (Used)
                {
                    var used = body.Contains("refresh_token=ghr_old") ? "ghr_old" : body.Contains("refresh_token=ghr_new") ? "ghr_new" : "other";
                    next = used switch { "ghr_old" when !Used.Contains("ghr_old") => "new", "ghr_new" when !Used.Contains("ghr_new") => "third", _ => null };
                    Used.Add(used);
                }
                return next is null ? Answer(HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}")
                    : Answer(HttpStatusCode.OK, $"{{\"access_token\":\"ghu_{next}\",\"expires_in\":28800,\"refresh_token\":\"ghr_{next}\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}}");
            }
            // The ended pair's access token is refused (401): the exchange renews with its refresh token, which GitHub refuses too.
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
        var root = Path.Combine(Path.GetTempPath(), "tensho-spare-read-1911-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // A renewal GitHub accepted (ghr_old ended) while github.refresh refuses any write, then the unload: ghu_new/ghr_new is only in the
    // spare; the files still hold the ended ghr_old. The files are writable again afterwards.
    private static GitHub KeptAside(string root)
    {
        var github = new GitHub();
        Connected(root);
        var refresh = Path.Combine(root, "github.refresh");
        File.SetAttributes(refresh, FileAttributes.ReadOnly);
        var exchange = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
        Check(exchange.Exchange(Sightings(7), true), "started");
        Ended(exchange);
        Check(exchange.Message is { Key: "ExchangeDone" }, "renewed and sent: " + exchange.Message?.Key);
        exchange.Dispose();
        File.SetAttributes(refresh, FileAttributes.Normal);
        Check(File.Exists(Path.Combine(root, GitHubExchange.SpareFile)) && Secret(refresh) == "ghr_old", "ghr_new kept aside only");
        return github;
    }
    // A handle on the spare that refuses any read but lets a delete through, as a scanner or a sync tool may hold it for a moment.
    private static FileStream Held(string root)
    {
        var spare = Path.Combine(root, GitHubExchange.SpareFile);
        var held = new FileStream(spare, FileMode.Open, FileAccess.Read, FileShare.Delete);
        var refused = false;
        try { File.ReadAllBytes(spare); } catch (IOException) { refused = true; }
        Check(refused, "the spare cannot be read while held");
        return held;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub spare read 1911: behaviour: a spare that cannot be read at the start is kept, then taken once readable; the ended ghr_old is never sent (X-889 M1)", () => InTemp(root =>
        {
            var github = KeptAside(root);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            GitHubExchange later;
            using (Held(root)) later = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(File.Exists(spare), "the spare not removed while it could not be read");
            Check(later.Exchange(Sightings(8), true), "started");
            Ended(later);
            Check(later.Message is { Key: "ExchangeDone" } && later.Connected, "the spare's pair taken and used: " + later.Message?.Key);
            Check(github.Renewals().SequenceEqual(["ghr_old"]), "no renewal with the ended ghr_old: " + string.Join(",", github.Renewals()));
            later.Dispose();
            var third = new GitHubExchange(root, github, App, () => At);
            Check(third.Connected && Secret(Path.Combine(root, "github.refresh")) == "ghr_new" && !File.Exists(spare), "written to its files at the next start, the spare removed");
            third.Dispose();
        })),
        ("GitHub spare read 1911: behaviour: GitHub refusing the files' ended pair while the spare cannot be read signs nobody out and removes nothing (X-889 M1)", () => InTemp(root =>
        {
            var github = KeptAside(root);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            GitHubExchange later;
            using (Held(root))
            {
                // A client that meets the spare held reads the files' pair; GitHub refuses its ended ghr_old.
                later = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
                Check(later.Exchange(Sightings(8), true), "started");
                Ended(later);
                Check(later.Connected && File.Exists(spare), "still connected, the spare kept: " + later.Message?.Key);
            }
            Check(later.Exchange(Sightings(9), true), "started again");
            Ended(later);
            Check(later.Message is { Key: "ExchangeDone" } && later.Connected, "the spare's pair taken once readable: " + later.Message?.Key);
            later.Dispose();
        })),
        ("GitHub spare read 1911: behaviour: a spare read and found older than the two files is still removed at the start (X-889 M1)", () => InTemp(root =>
        {
            var github = KeptAside(root);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            File.WriteAllBytes(Path.Combine(root, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_other")));
            File.WriteAllBytes(Path.Combine(root, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghr_other")));
            var newer = File.GetLastWriteTimeUtc(spare).AddMinutes(5);
            File.SetLastWriteTimeUtc(Path.Combine(root, "github.token"), newer);
            File.SetLastWriteTimeUtc(Path.Combine(root, "github.refresh"), newer);
            var later = new GitHubExchange(root, github, App, () => At);
            Check(later.Connected && !File.Exists(spare) && Secret(Path.Combine(root, "github.refresh")) == "ghr_other", "the older spare removed, the files' pair kept");
            later.Dispose();
        })),
    ];
}
