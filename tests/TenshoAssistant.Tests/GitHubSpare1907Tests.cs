using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1907 (X-888, review of 0.1906 M2): a renewed GitHub pair the files refuse when the plugin unloads is no longer lost. GitHub ends the old
// refresh token at each renewal, so a new pair that only lived in memory left the player signed out at the next start. It is now kept aside,
// protected the same way, in github.spare, and taken back at the next start (or by another client) when newer than the two files; a pair written
// to its files, or a Disconnect, removes it. Behaviour tests (R28.14): a fake GitHub, real files refused by a read-only attribute.
internal static class GitHubSpare1907Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private static readonly TimeSpan Patient = TimeSpan.FromSeconds(60);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";

    // A fake GitHub that rotates the refresh token: ghr_old gives ghu_new/ghr_new once, ghr_new gives ghu_third/ghr_third; any other is refused.
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
                return next is null ? Answer((HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}"))
                    : Answer((HttpStatusCode.OK, $"{{\"access_token\":\"ghu_{next}\",\"expires_in\":28800,\"refresh_token\":\"ghr_{next}\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}}"));
            }
            if (request.Method == HttpMethod.Patch && address.StartsWith("https://api.github.com/gists/", StringComparison.Ordinal))
                return Answer((HttpStatusCode.OK, "{\"history\":[{\"version\":\"r1\"}]}"));
            if (address == "https://api.github.com/user") return Answer((HttpStatusCode.OK, "{\"login\":\"tester\"}"));
            return Answer((HttpStatusCode.NotFound, "{}"));
        }
        private static HttpResponseMessage Answer((HttpStatusCode Status, string Json) answer) => new(answer.Status) { Content = new StringContent(answer.Json) };
    }

    private static GitHubExchangeMeta Meta(DateTimeOffset expires) => new() { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, GistLogin = "tester", GistFiles = ["index.json"], TokenExpires = expires, RefreshExpires = At.AddDays(100) };
    private static void Connected(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(Meta(At.AddMinutes(-1))));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_old")));
        File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghr_old")));
    }
    private static string? Secret(string path) => File.Exists(path) ? Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(path))) : null;
    private static DiscoveryStore Sightings(uint item = 7) { var store = new DiscoveryStore(); store.SeeSource(DiscoveryKind.Venture, 5, item, 2, At); return store; }
    private static void Ended(GitHubExchange exchange)
    {
        for (var wait = 0; exchange.Busy && wait < 6000; wait++) Thread.Sleep(10);
        Check(!exchange.Busy, "the exchange ended");
    }
    private static void Writable(string root) { foreach (var file in Directory.EnumerateFiles(root)) File.SetAttributes(file, FileAttributes.Normal); }
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-spare-1907-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // A renewal GitHub accepted (ghr_old ended) while github.refresh refuses any write, then the plugin unloads: the pair ghu_new/ghr_new
    // lives only in memory and in the spare file. Returns the log's lines.
    private static List<string> RenewedThenUnloaded(string root, GitHub github)
    {
        Connected(root);
        File.SetAttributes(Path.Combine(root, "github.refresh"), FileAttributes.ReadOnly);
        var said = new List<string>();
        var exchange = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient, Log = line => { lock (said) said.Add(line); } };
        Check(exchange.Exchange(Sightings(), true), "started");
        Ended(exchange);
        Check(exchange.Message is { Key: "ExchangeDone" } && exchange.Connected, "renewed and sent with the pair in memory: " + exchange.Message?.Key);
        exchange.Dispose();
        Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_old", "the refused file left as it was");
        return said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub spare 1907: behaviour: a renewed pair the files refuse at the unload is found again at the next start and written to its files; the spare is removed (X-888)", () => InTemp(root =>
        {
            var github = new GitHub();
            var said = RenewedThenUnloaded(root, github);
            Check(File.Exists(Path.Combine(root, GitHubExchange.SpareFile)), "kept aside");
            Check(said.Count(line => line.Contains("kept aside in github.spare", StringComparison.Ordinal)) == 1 && !said.Any(line => line.Contains("ask to connect again", StringComparison.Ordinal)), "said once, no loss said: " + string.Join(" | ", said));
            Writable(root);
            var later = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(later.Connected && later.Message is null, "still connected at the next start");
            Check(Secret(Path.Combine(root, "github.refresh")) == "ghr_new" && Secret(Path.Combine(root, "github.token")) == "ghu_new", "the renewed pair written to its files");
            Check(!File.Exists(Path.Combine(root, GitHubExchange.SpareFile)), "the spare removed once the files hold the pair");
            later.Dispose();
        })),
        ("GitHub spare 1907: behaviour: files still refused at the next start: the spare's pair is used, the next renewal sends ghr_new, never the ended ghr_old (X-888)", () => InTemp(root =>
        {
            var github = new GitHub();
            RenewedThenUnloaded(root, github);
            // Nine hours later: the spare's access token (8 h) is due, so the next exchange renews it.
            var later = new GitHubExchange(root, github, App, () => At.AddHours(9)) { UnloadWait = Patient };
            Check(later.Connected, "connected with the spare's pair");
            Check(later.Exchange(Sightings(8), true), "started (a new sighting: something to upload)");
            Ended(later);
            later.Dispose();
            string[] used; lock (github.Used) used = [.. github.Used];
            Check(later.Message is { Key: "ExchangeDone" } && used.SequenceEqual(["ghr_old", "ghr_new"]), "renewed with the spare's refresh token: " + later.Message?.Key + " " + string.Join(",", used));
            Writable(root);
            var third = new GitHubExchange(root, github, App, () => At);
            Check(third.Connected && Secret(Path.Combine(root, "github.refresh")) == "ghr_third", "the third pair kept through the spare again");
            third.Dispose();
        })),
        ("GitHub spare 1907: behaviour: a spare older than the two files is ignored and removed; a Disconnect removes the spare too (X-888)", () => InTemp(root =>
        {
            var github = new GitHub();
            RenewedThenUnloaded(root, github);
            Writable(root);
            var spare = Path.Combine(root, GitHubExchange.SpareFile);
            File.WriteAllBytes(Path.Combine(root, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_other")));
            File.WriteAllBytes(Path.Combine(root, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghr_other")));
            var newer = File.GetLastWriteTimeUtc(spare).AddMinutes(5);
            File.SetLastWriteTimeUtc(Path.Combine(root, "github.token"), newer);
            File.SetLastWriteTimeUtc(Path.Combine(root, "github.refresh"), newer);
            var later = new GitHubExchange(root, github, App, () => At);
            Check(later.Connected && Secret(Path.Combine(root, "github.refresh")) == "ghr_other" && !File.Exists(spare), "the files' newer pair kept, the old spare removed");
            later.Dispose();
            RenewedThenUnloaded(Path.Combine(root, "b"), new GitHub());
            var b = Path.Combine(root, "b");
            Check(File.Exists(Path.Combine(b, GitHubExchange.SpareFile)), "kept aside in b");
            var gone = new GitHubExchange(b, new GitHub(), App, () => At);   // github.refresh still refuses: the spare stays
            File.SetAttributes(Path.Combine(b, "github.refresh"), FileAttributes.Normal);
            gone.Disconnect();
            gone.Dispose();
            Check(!File.Exists(Path.Combine(b, GitHubExchange.SpareFile)) && !File.Exists(Path.Combine(b, "github.token")) && !File.Exists(Path.Combine(b, "github.refresh")), "a Disconnect leaves no secret");
        })),
        ("GitHub spare 1907: behaviour: the spare is never in clear and is kept out of the backup copies (X-888)", () => InTemp(root =>
        {
            RenewedThenUnloaded(root, new GitHub());
            var bytes = File.ReadAllBytes(Path.Combine(root, GitHubExchange.SpareFile));
            foreach (var secret in new[] { "ghu_new", "ghr_new" })
                Check(!Encoding.UTF8.GetString(bytes).Contains(secret, StringComparison.Ordinal) && !Encoding.Unicode.GetString(bytes).Contains(secret, StringComparison.Ordinal), secret + " not in clear");
            Check(GitHubExchange.SecretFiles.Contains(GitHubExchange.SpareFile), "kept out of the backup copies");
        })),
    ];
}
