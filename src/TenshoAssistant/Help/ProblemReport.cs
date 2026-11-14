using System.Text;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Help;

public enum ProblemKind { Bug, Idea, Translation }
public sealed record ProblemDraft(ProblemKind Kind, string Title, string What, string Expected, string Report, string Version, string Client);
/// <summary>ReportLeftOut: the address would be too long with the report, which then goes to the clipboard.</summary>
public sealed record ProblemLink(string Url, bool ReportLeftOut);

/// <summary>
/// 0.1125 (X-143 part 3, audit A08): what leaves, exactly. Fields are the form's fields as they are in the address (cleaned, in the address's
/// order), Clipboard the text put in the clipboard when the address would be too long (null when nothing is). The preview shown before the
/// browser opens is made of these same strings, so the player reads what is sent, not an earlier draft.
/// </summary>
public sealed record ProblemSend(string Url, IReadOnlyList<(string Key, string Value)> Fields, string? Clipboard)
{
    public bool ReportLeftOut => Clipboard is not null;
    public string Preview()
    {
        var text = new StringBuilder();
        foreach (var (key, value) in Fields) text.Append('[').Append(key).Append("]\n").Append(value).Append("\n\n");
        if (Clipboard is not null) text.Append("[clipboard]\n").Append(Clipboard).Append('\n');
        return text.ToString().TrimEnd('\n');
    }
}

/// <summary>
/// 0.1324 (X-340, user's decision of 2026-11-14, R04): the issue sent directly through the player's GitHub connection (the discoveries' one), on
/// the player's click only. The preview shown before the click is <see cref="Preview"/>, made of the very strings that leave (title and body).
/// </summary>
public sealed record ProblemIssue(string Title, string Body, IReadOnlyList<string> Labels)
{
    public string Preview() => Title + "\n\n" + Body;
}

/// <summary>
/// Report a problem (ONBOARD-001, lot 3, G5): the address of the project's GitHub form, filled in: the player's browser opens, and the player reads
/// and submits it. Since 0.1324 (X-340), a player connected to GitHub may send <see cref="Issue"/> directly instead, on a click under its preview. Form fields by id (.github/ISSUE_TEMPLATE); GitHub answers 414 on a too long address and
/// gives no figure, so a safe length is kept. Labels come from the chosen form (template=…: bug, enhancement or translation, and player-report), which
/// GitHub applies without write access; the title starts with the kind's prefix (0.1323, X-342: players' reports apart from the project's plan).
/// 0.1125 (X-143 part 3, audit A08): the diagnosis is built from a closed list of fields; paths are masked whole, folders with spaces and network
/// paths included; the Windows user and computer names are masked wherever they appear; the browser opens only after a preview of the exact text.
/// </summary>
public static partial class ProblemReport
{
    public const string Repository = "https://github.com/Shigu330/Tensho-Assistant";
    public const string SecurityAddress = Repository + "/security/advisories/new";
    private const int MaximumAddress = 6000;
    private const int MaximumField = 240;

    /// <summary>The only fields the diagnosis may hold (closed list): counts, switches and states, never a free path or a name.</summary>
    public static readonly IReadOnlyList<string> DiagnosticFields =
    [
        "Tensho", "Items", "Recipes", "Gathering", "Maps", "Weather", "Vendors", "Duties", "Lumina", "Community", "Quests", "Alarms", "Discord",
        "Context item",
    ];

    /// <summary>What must never leave: folders that name the user, network shares, a Discord address, the Windows user and computer names.</summary>
    public static string Clean(string text) => Clean(text, LocalNames.Value);

    /// <summary>Same, with the names to mask given (tests); a name shorter than 3 letters would mask ordinary words and is left.</summary>
    public static string Clean(string text, IEnumerable<string> names)
    {
        var line = MaskPaths(WebhookPattern().Replace(text ?? string.Empty, "<webhook>"));
        foreach (var name in names.Where(name => name.Trim().Length >= 3).Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(name => name.Length))
            line = Regex.Replace(line, $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}_])", "<user>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return line;
    }

    /// <summary>
    /// Paths masked whole: a drive path (C:\ or C:/) or a network path (\\server\share, \\?\UNC\…); folders may hold spaces (a segment followed by
    /// a separator), the last name stops at a space; no Windows name may hold &lt; &gt; : " | ? *, so these end a path. Shared with the self-check.
    /// </summary>
    public static string MaskPaths(string text) => PathPattern().Replace(text ?? string.Empty, "<path>");

    /// <summary>The diagnosis: only the listed fields, each one line, cleaned and bounded; an unknown field is dropped, not printed.</summary>
    public static string Diagnostic(IEnumerable<(string Field, string Value)> fields, IEnumerable<string>? names = null)
    {
        var text = new StringBuilder();
        foreach (var (field, value) in fields)
        {
            if (!DiagnosticFields.Contains(field, StringComparer.Ordinal)) continue;
            var line = Clean((value ?? string.Empty).ReplaceLineEndings(" "), names ?? LocalNames.Value).Trim();
            if (line.Length > MaximumField) line = line[..MaximumField] + "…";
            text.Append(field).Append(": ").Append(line).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>0.1323 (X-342): the title prefix of each kind, the same as the forms' « title: » in .github/ISSUE_TEMPLATE.</summary>
    public static string Prefix(ProblemKind kind) => kind switch { ProblemKind.Idea => "[Idea]", ProblemKind.Translation => "[Translation]", _ => "[Bug]" };

    /// <summary>The title with its prefix, once: a title the player already started with the prefix (any case) keeps it as typed.</summary>
    public static string PrefixedTitle(ProblemKind kind, string title)
    {
        var prefix = Prefix(kind);
        var text = (title ?? string.Empty).Trim();
        if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return text;
        return text.Length == 0 ? prefix : prefix + " " + text;
    }

    public static string SearchAddress(string words)
        => $"{Repository}/issues?q={Uri.EscapeDataString("is:issue " + Clean(words).Trim())}";

    public static ProblemLink Link(ProblemDraft draft)
    {
        var send = Prepare(draft);
        return new(send.Url, send.ReportLeftOut);
    }

    /// <summary>The address, the fields as they are in it and the clipboard text, all cleaned the same way (one place: what is previewed is what leaves).</summary>
    /// <summary>The form of each kind, its fields by id (cleaned, empty ones left out, in the form's order) and the title with its prefix.</summary>
    private static (string Template, (string Key, string Value)[] Fields, string Title) Fields(ProblemDraft draft, string[] known)
    {
        var (template, fields) = draft.Kind switch
        {
            ProblemKind.Idea => ("feature_request.yml", new (string, string)[] { ("need", draft.What), ("example", draft.Expected) }),
            ProblemKind.Translation => ("translation.yml", [("language", draft.Client), ("texts", draft.What)]),
            _ => ("bug_report.yml", [("version", draft.Version), ("client", draft.Client), ("steps", draft.What), ("expected", draft.Expected), ("report", draft.Report)]),
        };
        var cleaned = fields.Select(field => (Key: field.Item1, Value: Clean(field.Item2 ?? string.Empty, known))).Where(field => field.Value.Length > 0).ToArray();
        return (template, cleaned, PrefixedTitle(draft.Kind, Clean(draft.Title ?? string.Empty, known)));
    }

    /// <summary>The labels of each kind's form (.github/ISSUE_TEMPLATE); GitHub keeps them only for a sender with write access, a workflow of the
    /// repository (player-report-labels.yml) sets them from the title's prefix for everyone else.</summary>
    public static IReadOnlyList<string> Labels(ProblemKind kind) => kind switch
    {
        ProblemKind.Idea => ["enhancement", "player-report"],
        ProblemKind.Translation => ["translation", "player-report"],
        _ => ["bug", "player-report"],
    };

    /// <summary>The headings of the form's fields, as GitHub writes them in an issue made from the form (« ### label »).</summary>
    private static string Heading(string key) => key switch
    {
        "version" => "Tensho version",
        "client" => "Game client language and Dalamud version",
        "steps" => "Steps to reproduce",
        "expected" => "Expected and actual result",
        "report" => "Diagnostic report",
        "need" => "What you need",
        "example" => "Existing plugin or example (optional)",
        "language" => "Language",
        "texts" => "Texts",
        _ => key,
    };

    /// <summary>
    /// 0.1324 (X-340): the issue as it leaves through the GitHub connection: the same cleaned fields as the browser's form, each under its form
    /// heading, the diagnostic report in a code block whose fence is longer than any run of backticks in it (the report cannot close it).
    /// </summary>
    public static ProblemIssue Issue(ProblemDraft draft, IEnumerable<string>? names = null)
    {
        var (_, fields, title) = Fields(draft, (names ?? LocalNames.Value).ToArray());
        var body = new StringBuilder();
        foreach (var (key, value) in fields)
        {
            body.Append("### ").Append(Heading(key)).Append("\n\n");
            if (key == "report")
            {
                var fence = new string('`', Math.Max(3, LongestRun(value, '`') + 1));
                body.Append(fence).Append("text\n").Append(value.TrimEnd('\n', '\r')).Append('\n').Append(fence).Append("\n\n");
            }
            else body.Append(value.Trim()).Append("\n\n");
        }
        body.Append("Sent from the in-game form of Tensho Assistant, on the player's click, through their GitHub connection.");
        return new(title, body.ToString(), Labels(draft.Kind));
    }

    private static int LongestRun(string text, char mark)
    {
        int longest = 0, run = 0;
        foreach (var letter in text) { run = letter == mark ? run + 1 : 0; longest = Math.Max(longest, run); }
        return longest;
    }

    public static ProblemSend Prepare(ProblemDraft draft, IEnumerable<string>? names = null)
    {
        var (template, cleaned, prefixed) = Fields(draft, (names ?? LocalNames.Value).ToArray());
        var title = ("title", prefixed);
        (string Url, IReadOnlyList<(string, string)> Fields) Address(IEnumerable<(string Key, string Value)> pairs)
        {
            var list = new List<(string, string)> { title };
            var text = new StringBuilder($"{Repository}/issues/new?template={template}");
            text.Append("&title=").Append(Uri.EscapeDataString(title.Item2));
            foreach (var (key, value) in pairs)
            {
                text.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(value));
                list.Add((key, value));
            }
            return (text.ToString(), list);
        }
        var full = Address(cleaned);
        if (full.Url.Length <= MaximumAddress) return new(full.Url, full.Fields, null);
        var clipboard = string.Join("\n\n", cleaned.Where(field => field.Key is not ("version" or "client" or "language")).Select(field => field.Value));
        var shorter = Address(cleaned.Where(field => field.Key != "report"));
        if (shorter.Url.Length <= MaximumAddress)
            return new(shorter.Url, shorter.Fields, cleaned.Any(field => field.Key == "report") ? clipboard : null);
        // The player's own text is too long as well: the form opens with the title and the version, the rest goes to the clipboard.
        var least = Address(cleaned.Where(field => field.Key is "version" or "client" or "language"));
        return new(least.Url, least.Fields, clipboard);
    }

    /// <summary>The Windows user name, the profile folder's name (it can differ) and the computer name.</summary>
    private static readonly Lazy<string[]> LocalNames = new(() =>
    {
        var names = new List<string>();
        void Add(Func<string?> read) { try { if (read() is { Length: > 0 } name) names.Add(name); } catch (Exception) { } }
        Add(() => Environment.UserName);
        Add(() => Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\', '/')));
        Add(() => Environment.MachineName);
        return [.. names];
    });

    [GeneratedRegex(@"https?://\S*discord(app)?\.com/api/webhooks/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex WebhookPattern();
    [GeneratedRegex(@"(?:(?<![A-Za-z0-9])[A-Za-z]:[\\/]|\\\\[?.]\\UNC\\|\\\\(?=[^\\/\s""<>|:*?]))(?:[^\\/\r\n""<>|:*?]*[\\/])*[^\s\\/""<>|:*?]*")]
    private static partial Regex PathPattern();
}
