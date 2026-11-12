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
/// Report a problem (ONBOARD-001, lot 3, G5): the address of the project's GitHub form, filled in. The plugin sends nothing and holds no token: the
/// player's browser opens, and the player reads and submits it. Form fields by id (.github/ISSUE_TEMPLATE); GitHub answers 414 on a too long address and
/// gives no figure, so a safe length is kept. Labels cannot be set this way without write access to the repository: the report says where it comes from.
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

    public static string SearchAddress(string words)
        => $"{Repository}/issues?q={Uri.EscapeDataString("is:issue " + Clean(words).Trim())}";

    public static ProblemLink Link(ProblemDraft draft)
    {
        var send = Prepare(draft);
        return new(send.Url, send.ReportLeftOut);
    }

    /// <summary>The address, the fields as they are in it and the clipboard text, all cleaned the same way (one place: what is previewed is what leaves).</summary>
    public static ProblemSend Prepare(ProblemDraft draft, IEnumerable<string>? names = null)
    {
        var known = (names ?? LocalNames.Value).ToArray();
        var (template, fields) = draft.Kind switch
        {
            ProblemKind.Idea => ("feature_request.yml", new (string, string)[] { ("need", draft.What), ("example", draft.Expected) }),
            ProblemKind.Translation => ("translation.yml", [("language", draft.Client), ("texts", draft.What)]),
            _ => ("bug_report.yml", [("version", draft.Version), ("client", draft.Client), ("steps", draft.What), ("expected", draft.Expected), ("report", draft.Report)]),
        };
        var cleaned = fields.Select(field => (Key: field.Item1, Value: Clean(field.Item2 ?? string.Empty, known))).Where(field => field.Value.Length > 0).ToArray();
        var title = ("title", Clean(draft.Title ?? string.Empty, known).Trim());
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
