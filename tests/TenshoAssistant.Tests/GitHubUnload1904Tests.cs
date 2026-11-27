using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1904 (X-883, review of 0.1903 M1): the GitHub exchange when the plugin unloads. GitHubToken1171 N8 failed once in a full run: its cleanup
// met github-merged.json's temporary file moved under it, written by the exchange after Dispose had returned (the upload the unload stopped
// notes the rotation's state). Dispose now waits a little for the work running, which ends at once when stopped; a wait for another client's
// lock ends at the stop too; a renewed pair the files refuse at the unload is said in the log. Behaviour tests (R28.14): a fake GitHub, real files.
internal static class GitHubUnload1904Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    // 0.1905: Dispose's wait made long in these tests, so that the result is read from the work, not the clock (under the whole suite's load,
    // with its pool of threads saturated, the stopped work ended past the 5 s of the plugin twice in ten runs).
    private static readonly TimeSpan Patient = TimeSpan.FromSeconds(60);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";
    private const string Renewed = "{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600,\"token_type\":\"bearer\",\"scope\":\"\"}";

    // A fake GitHub: the gist's upload can be held until the request is cancelled, then answered late (a network slow to let go).
    private sealed class GitHub : HttpMessageHandler
    {
        public bool HoldUpload;
        public readonly TaskCompletionSource UploadAsked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int refreshes;
        public int Refreshes => Volatile.Read(ref refreshes);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            if (address == RefreshAddress && body.Contains("grant_type=refresh_token"))
                return Answer(Interlocked.Increment(ref refreshes) == 1 && body.Contains("refresh_token=ghr_old") ? (HttpStatusCode.OK, Renewed) : (HttpStatusCode.OK, "{\"error\":\"bad_refresh_token\"}"));
            if (request.Method == HttpMethod.Patch && address.StartsWith("https://api.github.com/gists/", StringComparison.Ordinal))
            {
                if (HoldUpload)
                {
                    UploadAsked.TrySetResult();
                    try { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { await Task.Delay(400, CancellationToken.None).ConfigureAwait(false); throw; }
                }
                return Answer((HttpStatusCode.OK, "{\"history\":[{\"version\":\"r1\"}]}"));
            }
            if (address == "https://api.github.com/user") return Answer((HttpStatusCode.OK, "{\"login\":\"tester\"}"));
            return Answer((HttpStatusCode.NotFound, "{}"));
        }
        private static HttpResponseMessage Answer((HttpStatusCode Status, string Json) answer) => new(answer.Status) { Content = new StringContent(answer.Json) };
    }

    private static GitHubExchangeMeta Meta(DateTimeOffset expires) => new() { Login = "tester", ClientId = TestAppId, GistId = "g1", Issue = 7, GistLogin = "tester", GistFiles = ["index.json"], TokenExpires = expires, RefreshExpires = At.AddDays(100) };
    private static void Connected(string folder, GitHubExchangeMeta meta)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(meta));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghu_old")));
        File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes("ghr_old")));
    }
    private static string? Secret(string path) => File.Exists(path) ? Encoding.UTF8.GetString(UserSecret.Unprotect(File.ReadAllBytes(path))) : null;
    private static DiscoveryStore Sightings() { var store = new DiscoveryStore(); store.SeeSource(DiscoveryKind.Venture, 5, 7, 2, At); return store; }
    private static string Folder(string root) => string.Join("\n", Directory.EnumerateFiles(root).Select(file => new FileInfo(file)).OrderBy(file => file.Name, StringComparer.Ordinal)
        .Select(file => $"{file.Name} {file.Length} {file.LastWriteTimeUtc.Ticks}"));
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-unload-1904-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GitHub unload 1904: behaviour: an upload the unload stops has ended, and noted the rotation's state, when Dispose returns; nothing is written in the folder after (X-883)", () => InTemp(root =>
        {
            var github = new GitHub { HoldUpload = true };
            Connected(root, Meta(At.AddHours(8)));
            var exchange = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(exchange.Exchange(Sightings(), true) && github.UploadAsked.Task.Wait(TimeSpan.FromSeconds(20)), "the upload asked");
            exchange.Dispose();
            Check(!exchange.Busy && exchange.Message is { Key: "ExchangeStopped" }, "the work ended before Dispose returned: " + (exchange.Busy ? "still busy" : exchange.Message?.Key));
            Check(File.Exists(Path.Combine(root, GistRotation.FileName)), "the rotation's state noted before Dispose returned");
            var after = Folder(root);
            Thread.Sleep(600);
            Check(Folder(root) == after, "nothing written after Dispose returned:\n" + after + "\n--\n" + Folder(root));
            Check(!Directory.EnumerateFiles(root, "*.tmp").Any(), "no temporary file left");
        })),
        ("GitHub unload 1904: behaviour: a renewed pair the files still refuse when the plugin unloads is said in the log once; the files keep the old pair (X-883)", () => InTemp(root =>
        {
            var github = new GitHub();
            Connected(root, Meta(At.AddMinutes(-1)));
            var refreshFile = Path.Combine(root, "github.refresh");
            File.SetAttributes(refreshFile, FileAttributes.ReadOnly);
            var said = new List<string>();
            var exchange = new GitHubExchange(root, github, App, () => At) { Log = line => { lock (said) said.Add(line); } };
            Check(exchange.Exchange(Sightings(), true), "started");
            for (var wait = 0; exchange.Busy && wait < 3000; wait++) Thread.Sleep(10);
            Check(!exchange.Busy && exchange.Message is { Key: "ExchangeDone" } && github.Refreshes == 1 && exchange.Connected, "renewed and sent with the pair in memory: " + exchange.Message?.Key);
            exchange.Dispose();
            string[] lines; lock (said) lines = [.. said.Where(line => line.Contains("GitHub sign-in could not be written", StringComparison.Ordinal))];
            Check(lines.Length == 1, "said once in the log: " + string.Join(" | ", said));
            Check(!lines[0].Contains("ghu_", StringComparison.Ordinal) && !lines[0].Contains("ghr_", StringComparison.Ordinal), "no token in the log");
            Check(Secret(refreshFile) == "ghr_old", "the refused file left as it was");
        })),
        ("GitHub unload 1904: behaviour: a work waiting for another client's renewal lock stops at the unload; Dispose returns well before its wait, no renewal asked (X-883)", () => InTemp(root =>
        {
            var github = new GitHub();
            Connected(root, Meta(At.AddMinutes(-1)));
            using var other = new FileStream(Path.Combine(root, "github.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var exchange = new GitHubExchange(root, github, App, () => At) { UnloadWait = Patient };
            Check(exchange.Exchange(Sightings(), true), "started");
            Thread.Sleep(500);
            Check(exchange.Busy, "waiting for the other client's lock");
            var timer = Stopwatch.StartNew();
            exchange.Dispose();
            Check(timer.Elapsed < TimeSpan.FromSeconds(20), "Dispose not held by the lock's wait (45 s): " + timer.Elapsed);
            Check(!exchange.Busy && exchange.Message is { Key: "ExchangeStopped" } && github.Refreshes == 0, "stopped, no renewal asked: " + exchange.Message?.Key);
        })),
    ];
}
