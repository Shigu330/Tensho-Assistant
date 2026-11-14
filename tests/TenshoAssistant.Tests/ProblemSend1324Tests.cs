using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Help;

// 0.1324 (X-340, user's decision of 2026-11-14, R04): a problem report sent directly through the player's GitHub connection. Behaviour tests
// (R28.14, privacy): a fake GitHub and real files. Nothing leaves before Report is called (the « Send now » click); what leaves is exactly the
// title and body of the preview; not connected, nothing; a refusal or a renewal is handled like the exchange's.
internal static class ProblemSend1324Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    private const string TestAppId = "Iv23liTestOnly000000";
    private static readonly GitHubSignIn App = GitHubSignIn.For(TestAppId);
    private const string RefreshAddress = "https://github.com/login/oauth/access_token";
    private static readonly string IssuesAddress = $"https://api.github.com/repos/{GitHubExchange.Repository}/issues";

    private sealed class GitHub : HttpMessageHandler
    {
        public readonly List<(string Method, string Address, string Body, string? Bearer)> Calls = [];
        public Func<string, string, string, string?, (HttpStatusCode Status, string Json)?> Answer = (_, _, _, _) => null;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            lock (Calls) Calls.Add((request.Method.Method, address, body, request.Headers.Authorization?.Parameter));
            var (status, json) = Answer(request.Method.Method, address, body, request.Headers.Authorization?.Parameter) ?? (HttpStatusCode.NotFound, "{}");
            return new(status) { Content = new StringContent(json) };
        }
        public List<(string Method, string Address, string Body, string? Bearer)> All { get { lock (Calls) return [.. Calls]; } }
    }
    private static (HttpStatusCode, string)? Created(string method, string address)
        => method == "POST" && address == IssuesAddress
            ? (HttpStatusCode.Created, $"{{\"number\":12,\"html_url\":\"https://github.com/{GitHubExchange.Repository}/issues/12\"}}") : null;
    private static ExchangeMessage WaitReport(GitHubExchange exchange)
    {
        for (var wait = 0; (exchange.Busy || exchange.ReportMessage is null) && wait < 3000; wait++) Thread.Sleep(10);
        Check(!exchange.Busy && exchange.ReportMessage is not null, "the report ends");
        return exchange.ReportMessage!;
    }
    private static void Connected(string folder, string token, string? refresh, DateTimeOffset expires)
    {
        Directory.CreateDirectory(folder);
        var meta = new GitHubExchangeMeta { Login = "tester", ClientId = TestAppId, TokenExpires = expires, RefreshExpires = At.AddDays(100) };
        File.WriteAllText(Path.Combine(folder, "github.json"), JsonSerializer.Serialize(meta));
        File.WriteAllBytes(Path.Combine(folder, "github.token"), UserSecret.Protect(Encoding.UTF8.GetBytes(token)));
        if (refresh is not null) File.WriteAllBytes(Path.Combine(folder, "github.refresh"), UserSecret.Protect(Encoding.UTF8.GetBytes(refresh)));
    }
    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-report-1324-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    private static ProblemIssue Bug(string report = "Tensho: 0.1324.0\nItems: 12\nSent from the in-game form.")
        => ProblemReport.Issue(new(ProblemKind.Bug, "Map & flag", "Open the map\nPress F", "A flag", report, "0.1324.0", "Plugin language fr"), ["Shigu"]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Problem send 1324: the issue holds the form's fields under their headings, cleaned, and its preview is its title then its body", () =>
        {
            var issue = ProblemReport.Issue(new(ProblemKind.Bug, "Crash in C:\\Users\\Shigu\\x", "Step one", "", "Items: 3\n```inner```", "0.1324.0", "fr"), ["Shigu"]);
            Check(issue.Title == "[Bug] Crash in <path>", "title prefixed and cleaned: " + issue.Title);
            Check(issue.Body.Contains("### Tensho version\n\n0.1324.0\n\n", StringComparison.Ordinal) && issue.Body.Contains("### Steps to reproduce\n\nStep one\n\n", StringComparison.Ordinal), "headings of the form");
            Check(!issue.Body.Contains("### Expected and actual result", StringComparison.Ordinal), "an empty field is left out");
            Check(issue.Body.Contains("### Diagnostic report\n\n````text\nItems: 3\n```inner```\n````\n", StringComparison.Ordinal), "the report fenced longer than its own backticks: " + issue.Body);
            Check(!issue.Body.Contains("Shigu", StringComparison.Ordinal), "the Windows user name masked");
            Check(issue.Preview() == issue.Title + "\n\n" + issue.Body, "the preview is the title then the body");
            Check(issue.Labels.SequenceEqual(["bug", "player-report"]), "bug labels");
            Check(ProblemReport.Issue(new(ProblemKind.Idea, "More", "Want", "", "", "v", "c"), []) is { Title: "[Idea] More" } idea && idea.Labels.SequenceEqual(["enhancement", "player-report"]) && idea.Body.StartsWith("### What you need\n\nWant", StringComparison.Ordinal), "idea");
            Check(ProblemReport.Issue(new(ProblemKind.Translation, "de", "Text", "", "", "v", "de"), []) is { Title: "[Translation] de" } words && words.Labels.SequenceEqual(["translation", "player-report"]) && words.Body.StartsWith("### Language\n\nde", StringComparison.Ordinal), "translation");
        }),
        ("Problem send 1324: nothing leaves before the click; on Report, the request holds exactly the preview's title and body", () => InTemp(root =>
        {
            using var github = new GitHub(); github.Answer = (method, address, _, _) => Created(method, address);
            Connected(root, "ghu_live", "ghr_live", At.AddHours(4));
            using var exchange = new GitHubExchange(root, github, App, () => At);
            var issue = Bug();
            Check(github.All.Count == 0, "no request before the click");
            Check(exchange.Report(issue), "sent on the click");
            Check(WaitReport(exchange) is { Key: "ReportSent" } sent && sent.Args[0] == "12" && sent.Args[1] == $"https://github.com/{GitHubExchange.Repository}/issues/12", "the issue's number and address: " + exchange.ReportMessage);
            var calls = github.All;
            Check(calls.Count == 1 && calls[0].Method == "POST" && calls[0].Address == IssuesAddress && calls[0].Bearer == "ghu_live", "one POST to the repository's issues, with the player's token");
            var json = JsonNode.Parse(calls[0].Body)!.AsObject();
            Check(json["title"]!.GetValue<string>() == issue.Title && json["body"]!.GetValue<string>() == issue.Body, "the title and body sent are the ones shown");
            Check(issue.Preview() == json["title"]!.GetValue<string>() + "\n\n" + json["body"]!.GetValue<string>(), "sent text = shown text");
            Check(json["labels"]!.AsArray().Select(label => label!.GetValue<string>()).SequenceEqual(["bug", "player-report"]), "the form's labels asked");
            Check(json.Count == 3, "nothing else in the request");
            Check(exchange.Message is null, "the exchange's own message untouched");
            Check(!calls[0].Body.Contains("ghr_live", StringComparison.Ordinal) && !exchange.ReportMessage!.Args.Any(arg => arg.Contains("ghu_", StringComparison.Ordinal)), "no token in the body or the message");
        })),
        ("Problem send 1324: not connected, Report sends nothing; the App not installed is said, the sign-in kept", () => InTemp(root =>
        {
            using var github = new GitHub(); github.Answer = (method, address, _, _) => Created(method, address);
            using (var none = new GitHubExchange(Path.Combine(root, "none"), github, App, () => At))
                Check(!none.Report(Bug()) && none.ReportMessage is null && github.All.Count == 0, "not connected: nothing leaves");
            var folder = Path.Combine(root, "refused");
            using var refused = new GitHub(); refused.Answer = (method, address, _, _) => method == "POST" && address == IssuesAddress ? (HttpStatusCode.Forbidden, "{}") : null;
            Connected(folder, "ghu_live", "ghr_live", At.AddHours(4));
            using var exchange = new GitHubExchange(folder, refused, App, () => At);
            Check(exchange.Report(Bug()) && WaitReport(exchange).Key == "ReportAppMissing", "App missing: " + exchange.ReportMessage?.Key);
            Check(exchange.Connected, "the sign-in kept");
        })),
        ("Problem send 1324: a 401 with a refresh token in hand renews once and sends once more", () => InTemp(root =>
        {
            using var github = new GitHub();
            github.Answer = (method, address, body, bearer) =>
                address == RefreshAddress && body.Contains("refresh_token=ghr_old")
                    ? (HttpStatusCode.OK, "{\"access_token\":\"ghu_new\",\"expires_in\":28800,\"refresh_token\":\"ghr_new\",\"refresh_token_expires_in\":15897600}")
                : method == "POST" && address == IssuesAddress && bearer == "ghu_old" ? (HttpStatusCode.Unauthorized, "{}")
                : Created(method, address);
            Connected(root, "ghu_old", "ghr_old", At.AddHours(4));
            using var exchange = new GitHubExchange(root, github, App, () => At);
            Check(exchange.Report(Bug()) && WaitReport(exchange).Key == "ReportSent", "sent after the renewal: " + exchange.ReportMessage?.Key);
            var posts = github.All.Where(call => call.Address == IssuesAddress).ToList();
            Check(posts.Count == 2 && posts[1].Bearer == "ghu_new" && github.All.Count(call => call.Address == RefreshAddress) == 1, "renewed once, sent twice");
        })),
        ("Problem send 1324: the labelling workflow gives each prefix its form's labels, without running the title", () =>
        {
            var flow = File.ReadAllText(Path.GetFullPath(".github/workflows/player-report-labels.yml")).Replace("\r\n", "\n");
            Check(flow.Contains("'bug,player-report'", StringComparison.Ordinal) && flow.Contains("'enhancement,player-report'", StringComparison.Ordinal) && flow.Contains("'translation,player-report'", StringComparison.Ordinal), "labels by prefix");
            Check(flow.Contains("types: [ opened ]", StringComparison.Ordinal) && flow.Contains("issues: write", StringComparison.Ordinal), "on opened issues, issues write only");
            Check(!flow.Split('\n').Any(line => line.TrimStart().StartsWith("run:", StringComparison.Ordinal) && line.Contains("${{", StringComparison.Ordinal)), "no expression inside a run line");
            foreach (var kind in new[] { ProblemKind.Bug, ProblemKind.Idea, ProblemKind.Translation })
                Check(flow.Contains($"'{string.Join(',', ProblemReport.Labels(kind))}'", StringComparison.Ordinal) && flow.Contains($"'{ProblemReport.Prefix(kind)}'", StringComparison.Ordinal), "workflow and plugin agree: " + kind);
        }),
    ];
}
