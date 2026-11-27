using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1910 (X-890, review of 0.1909 M2; X-889 M3): two clients of one profile alive at the same time on the same folder. A renews P1 to P2 and
// its files refuse P2, which goes to github.spare; B takes P2 from the spare and renews it to P3. A, still holding P2 in memory as « not
// written », used to write P2 back over P3 (before its next renewal, outside the profile's lock, or at its unload); GitHub then refused P2
// and A was signed out, its files removed. The pair is now settled under the profile's lock: a pair on disk that is neither the one this
// client last saw there nor the one in memory is newer, and is taken. Behaviour tests (R28.14): a fake GitHub that rotates the refresh token,
// real files refused by a read-only attribute, both clients kept alive.
internal static class GitHubTwoClients1910Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private static readonly TimeSpan Patient = TimeSpan.FromSeconds(60);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";
    private static readonly string[] Chain = ["old", "new", "third", "fourth", "fifth"];

    // A fake GitHub whose refresh tokens follow Chain: each one gives the next pair once, then is refused (GitHub ends it at the renewal).
    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<string> Used = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (address == RefreshAddress && body.Contains("grant_type=refresh_token"))
            {
                string? next = null;
                lock (Used)
                {
                    var index = Array.FindIndex(Chain, name => body.Contains("refresh_token=ghr_" + name + "&") || body.EndsWith("refresh_token=ghr_" + name, StringComparison.Ordinal));
                    var used = index < 0 ? "other" : "ghr_" + Chain[index];
                    if (index >= 0 && index + 1 < Chain.Length && !Used.Contains(used)) next = Chain[index + 1];
                    Used.Add(used);
                }
                return next is null ? Answer(HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}")
                    : Answer(HttpStatusCode.OK, $"{{\"access_token\":\"ghu_{next}\",\"expires_in\":28800,\"refresh_token\":\"ghr_{next}\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}}");
            }
            if (request.Method == HttpMethod.Patch && address.StartsWith("https://api.github.com/gists/", StringComparison.Ordinal))
                return Answer(HttpStatusCode.OK, "{\"history\":[{\"version\":\"r1\"}]}");
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
    private static void Done(GitHubExchange exchange, string what)
    {
        Ended(exchange);
        Check(exchange.Message is { Key: "ExchangeDone" } && exchange.Connected, what + ": " + exchange.Message?.Key + " " + string.Join(",", exchange.Message?.Args ?? []));
    }
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-two-1910-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // Both clients alive on one folder; A renews P1 (old) to P2 (new) while github.refresh refuses any write: P2 lives in A's memory and in
    // the spare. Then the file is writable again. The clocks are the callers' to move.
    private sealed class Pair : IDisposable
    {
        public readonly GitHub GitHub = new();
        public DateTimeOffset ANow = At, BNow = At;
        public readonly GitHubExchange A, B;
        public readonly List<string> Said = [];
        public readonly string Root;
        public Pair(string root)
        {
            Root = root;
            Connected(root);
            A = new GitHubExchange(root, GitHub, App, () => ANow) { UnloadWait = Patient, Log = line => { lock (Said) Said.Add(line); } };
            B = new GitHubExchange(root, GitHub, App, () => BNow) { UnloadWait = Patient };
            File.SetAttributes(Path(Refresh), FileAttributes.ReadOnly);
            Check(A.Exchange(Sightings(7), true), "A started");
            Done(A, "A renewed and sent with P2 in memory");
            Check(File.Exists(Path(GitHubExchange.SpareFile)) && Secret(Path(Refresh)) == "ghr_old", "P2 kept aside, the refused file unchanged");
            File.SetAttributes(Path(Refresh), FileAttributes.Normal);
        }
        public const string Refresh = "github.refresh", Token = "github.token";
        public string Path(string name) => System.IO.Path.Combine(Root, name);
        // B, nine hours later (P2 is due), takes P2 from the spare and renews it to P3, written to the files.
        public void BRenews()
        {
            BNow = At.AddHours(9);
            Check(B.Exchange(Sightings(8), true), "B started");
            Done(B, "B took P2 and renewed it");
            Check(Secret(Path(Refresh)) == "ghr_third" && Secret(Path(Token)) == "ghu_third" && !File.Exists(Path(GitHubExchange.SpareFile)), "P3 on disk, the spare removed");
        }
        public void OnDiskStill(string name, string what) =>
            Check(Secret(Path(Refresh)) == "ghr_" + name && Secret(Path(Token)) == "ghu_" + name, what + ": " + Secret(Path(Refresh)));
        public void Dispose() { A.Dispose(); B.Dispose(); }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub two clients 1910: behaviour: A's older pair P2 is never written back over the P3 that B renewed from it; A takes P3 and its next renewal sends ghr_third (X-890, X-889 M3)", () => InTemp(root =>
        {
            using var two = new Pair(root);
            two.BRenews();
            // A, an hour later: P2 not due for it, a new sighting to send. It used to write P2 over P3 here, before Fresh and its lock.
            two.ANow = At.AddHours(1);
            Check(two.A.Exchange(Sightings(9), true), "A started again");
            Done(two.A, "A sent again");
            two.OnDiskStill("third", "P3 kept on disk, P2 never written back");
            // A, at P3's end: its renewal sends B's ghr_third, never the ended ghr_new, and A stays connected.
            two.ANow = At.AddHours(18);
            Check(two.A.Exchange(Sightings(10), true), "A started a third time");
            Done(two.A, "A renewed P3");
            Check(two.GitHub.Renewals().SequenceEqual(["ghr_old", "ghr_new", "ghr_third"]), "renewals: " + string.Join(",", two.GitHub.Renewals()));
            two.OnDiskStill("fourth", "P4 on disk");
            Check(two.B.Connected, "B still connected");
        })),
        ("GitHub two clients 1910: behaviour: A unloading after B renewed does not write its older pair back, nor keep it aside again (X-890, X-889 M3)", () => InTemp(root =>
        {
            var two = new Pair(root);
            two.BRenews();
            two.A.Dispose();
            two.OnDiskStill("third", "P3 kept on disk at A's unload");
            Check(!File.Exists(two.Path(GitHubExchange.SpareFile)), "no spare written back");
            lock (two.Said) Check(!two.Said.Any(line => line.Contains("could not be written", StringComparison.Ordinal)), "nothing said lost or kept aside: " + string.Join(" | ", two.Said));
            two.B.Dispose();
            var later = new GitHubExchange(root, two.GitHub, App, () => At.AddHours(9));
            Check(later.Connected, "connected at the next start");
            later.Dispose();
            two.OnDiskStill("third", "P3 still on disk");
        })),
        ("GitHub two clients 1910: behaviour: A (P2 not written) and B (P1) renewing at the same moment: one renewal of ghr_new, both end with P3 (X-890, X-889 M3)", () => InTemp(root =>
        {
            using var two = new Pair(root);
            two.ANow = two.BNow = At.AddHours(9);
            using var go = new Barrier(2);
            var a = Task.Run(() => { go.SignalAndWait(); return two.A.Exchange(Sightings(9), true); });
            var b = Task.Run(() => { go.SignalAndWait(); return two.B.Exchange(Sightings(8), true); });
            Check(a.Wait(Patient) && a.Result && b.Wait(Patient) && b.Result, "both started");
            Done(two.A, "A sent");
            Done(two.B, "B sent");
            Check(two.GitHub.Renewals().SequenceEqual(["ghr_old", "ghr_new"]), "one renewal of ghr_new: " + string.Join(",", two.GitHub.Renewals()));
            two.OnDiskStill("third", "P3 on disk");
            Check(!File.Exists(two.Path(GitHubExchange.SpareFile)), "the spare removed");
        })),
    ];
}
