using System.Text.RegularExpressions;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// Self-check (0.494, user request): one read-only pass over what each part of the plugin has read, written to the log. Numbers only: no
/// character name, no identifier. OK = read and plausible; NOT_READ = the game has not given it yet, not a fault; SUSPECT = read, but
/// improbable; FAILED = the read failed. Plausible is not proven right: only the game's own window can confirm a value.
/// </summary>
public enum SelfCheckStatus { Ok, NotRead, Suspect, Failed }

public sealed record SelfCheckLine(string Domain, SelfCheckStatus Status, string Detail)
{
    public string Format() => $"Selftest {Domain,-18} {Label(Status),-8} {Detail}";
    public static string Label(SelfCheckStatus status) => status switch
    {
        SelfCheckStatus.Ok => "OK",
        SelfCheckStatus.NotRead => "NOT_READ",
        SelfCheckStatus.Suspect => "SUSPECT",
        _ => "FAILED",
    };
}

public static partial class SelfCheck
{
    private const int MaximumError = 160;

    public static SelfCheckLine Ok(string domain, string detail) => new(domain, SelfCheckStatus.Ok, detail);
    public static SelfCheckLine NotRead(string domain, string detail) => new(domain, SelfCheckStatus.NotRead, detail);
    public static SelfCheckLine Suspect(string domain, string detail) => new(domain, SelfCheckStatus.Suspect, detail);
    public static SelfCheckLine Failed(string domain, string? detail) => new(domain, SelfCheckStatus.Failed, Clean(detail));

    /// <summary>A catalogue of the game's data: an error fails, not there yet is not read, an empty one is suspect.</summary>
    public static SelfCheckLine Catalog(string domain, int? count, int copied, int total, string? error)
    {
        if (error is not null) return Failed(domain, error);
        if (count is null) return NotRead(domain, $"loading {copied}/{total}");
        return count > 0 ? Ok(domain, $"{count} rows") : Suspect(domain, "0 rows");
    }

    /// <summary>A catalogue that only says whether it was built.</summary>
    public static SelfCheckLine Built(string domain, bool built, bool failed)
        => failed ? Failed(domain, "could not be built") : built ? Ok(domain, "built") : NotRead(domain, "loading");

    /// <summary>A probe never stops the pass: its exception is its line.</summary>
    public static SelfCheckLine Run(string domain, Func<SelfCheckLine> probe)
    {
        try { return probe(); }
        catch (Exception error) { return Failed(domain, $"{error.GetType().Name}: {error.Message}"); }
    }

    /// <summary>One line, bounded, without the paths that would name the user's folders.</summary>
    public static string Clean(string? text)
    {
        var line = PathPattern().Replace((text ?? "unknown error").ReplaceLineEndings(" "), "<path>").Trim();
        return line.Length <= MaximumError ? line : line[..MaximumError] + "…";
    }

    public static string Summary(string reason, IReadOnlyCollection<SelfCheckLine> lines)
    {
        int Count(SelfCheckStatus status) => lines.Count(line => line.Status == status);
        return $"Selftest done ({reason}): {Count(SelfCheckStatus.Ok)} ok, {Count(SelfCheckStatus.NotRead)} not read, {Count(SelfCheckStatus.Suspect)} suspect, {Count(SelfCheckStatus.Failed)} failed.";
    }

    [GeneratedRegex(@"[A-Za-z]:[\\/][^\s""']+")]
    private static partial Regex PathPattern();
}

/// <summary>A pass, one probe per step, so that a pass never holds a whole frame.</summary>
public sealed class SelfCheckRun
{
    private readonly IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)> probes;
    private readonly List<SelfCheckLine> lines = [];
    private int position;

    public SelfCheckRun(IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)> probes) => this.probes = probes;
    public bool Done => position >= probes.Count;
    public IReadOnlyList<SelfCheckLine> Lines => lines;

    public SelfCheckLine? Step()
    {
        if (Done) return null;
        var (domain, probe) = probes[position++];
        var line = SelfCheck.Run(domain, probe);
        lines.Add(line);
        return line;
    }
}
