using System.Text;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Help;

public enum ProblemKind { Bug, Idea, Translation }
public sealed record ProblemDraft(ProblemKind Kind, string Title, string What, string Expected, string Report, string Version, string Client);
/// <summary>ReportLeftOut: the address would be too long with the report, which then goes to the clipboard.</summary>
public sealed record ProblemLink(string Url, bool ReportLeftOut);

/// <summary>
/// Report a problem (ONBOARD-001, lot 3, G5): the address of the project's GitHub form, filled in. The plugin sends nothing and holds no token: the
/// player's browser opens, and the player reads and submits it. Form fields by id (.github/ISSUE_TEMPLATE); GitHub answers 414 on a too long address and
/// gives no figure, so a safe length is kept. Labels cannot be set this way without write access to the repository: the report says where it comes from.
/// </summary>
public static partial class ProblemReport
{
    public const string Repository = "https://github.com/Shigu330/Tensho-Assistant";
    public const string SecurityAddress = Repository + "/security/advisories/new";
    private const int MaximumAddress = 6000;

    /// <summary>What must never leave: folders that name the user, a Discord address.</summary>
    public static string Clean(string text)
        => PathPattern().Replace(WebhookPattern().Replace(text, "<webhook>"), "<path>");

    public static string SearchAddress(string words)
        => $"{Repository}/issues?q={Uri.EscapeDataString("is:issue " + Clean(words).Trim())}";

    public static ProblemLink Link(ProblemDraft draft)
    {
        var (template, fields) = draft.Kind switch
        {
            ProblemKind.Idea => ("feature_request.yml", new (string, string)[] { ("need", draft.What), ("example", draft.Expected) }),
            ProblemKind.Translation => ("translation.yml", [("language", draft.Client), ("texts", draft.What)]),
            _ => ("bug_report.yml", [("version", draft.Version), ("client", draft.Client), ("steps", draft.What), ("expected", draft.Expected), ("report", draft.Report)]),
        };
        string Address(IEnumerable<(string Key, string Value)> pairs)
        {
            var text = new StringBuilder($"{Repository}/issues/new?template={template}");
            text.Append("&title=").Append(Uri.EscapeDataString(Clean(draft.Title).Trim()));
            foreach (var (key, value) in pairs)
                if (value.Length > 0) text.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(Clean(value)));
            return text.ToString();
        }
        var full = Address(fields);
        if (full.Length <= MaximumAddress) return new(full, false);
        var shorter = Address(fields.Where(field => field.Item1 != "report"));
        if (shorter.Length <= MaximumAddress) return new(shorter, draft.Report.Length > 0);
        // The player's own text is too long as well: the form opens with the title and the version, the rest goes to the clipboard.
        return new(Address(fields.Where(field => field.Item1 is "version" or "client" or "language")), true);
    }

    [GeneratedRegex(@"https?://\S*discord(app)?\.com/api/webhooks/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex WebhookPattern();
    [GeneratedRegex(@"[A-Za-z]:[\\/][^\s""']+")]
    private static partial Regex PathPattern();
}
