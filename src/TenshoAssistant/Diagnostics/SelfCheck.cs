using System.Text.Json;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// Self-check (0.494, user request): one read-only pass over what each part of the plugin has read, written to the log. Numbers only: no
/// character name, no identifier. OK = read and plausible; NOT_READ = the game has not given it yet, not a fault; SUSPECT = read, but
/// improbable; FAILED = the read failed; OK_SEEN = not readable now, but read well earlier with this version of the game's data (0.496). Plausible
/// is not proven right: only the game's own window can confirm a value.
/// </summary>
public enum SelfCheckStatus { Ok, NotRead, Suspect, Failed, Seen }

public sealed record SelfCheckLine(string Domain, SelfCheckStatus Status, string Detail)
{
    public string Format() => $"Selftest {Domain,-18} {Label(Status),-8} {Detail}";
    public static string Label(SelfCheckStatus status) => status switch
    {
        SelfCheckStatus.Ok => "OK",
        SelfCheckStatus.NotRead => "NOT_READ",
        SelfCheckStatus.Suspect => "SUSPECT",
        SelfCheckStatus.Seen => "OK_SEEN",
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

    /// <summary>A window of the game: OK only when it was really read. Waiting, closed or not ready says nothing about the reader.</summary>
    public static SelfCheckLine Window(string domain, bool enabled, string status, string closed)
        => !enabled ? NotRead(domain, "observation off")
            : status == "Observed" ? Ok(domain, status)
            : status == "Error" ? Failed(domain, "status Error")
            : status == "AddonAbsent" ? NotRead(domain, closed) : NotRead(domain, status);

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
        return $"Selftest done ({reason}): {Count(SelfCheckStatus.Ok)} ok, {Count(SelfCheckStatus.Seen)} seen earlier, {Count(SelfCheckStatus.NotRead)} not read, {Count(SelfCheckStatus.Suspect)} suspect, {Count(SelfCheckStatus.Failed)} failed.";
    }

    [GeneratedRegex(@"[A-Za-z]:[\\/][^\s""']+")]
    private static partial Regex PathPattern();
}

/// <summary>
/// 0.496 (user report « it does not record the OKs »): the windows of the game are open one at a time, and the Crafting log closes during a
/// synthesis, so no single pass can show them all. What was read well is kept per domain, with its time, for this version of the game's data
/// only: after a game update an old OK says nothing.
/// </summary>
public sealed class SelfCheckMemory
{
    public sealed record Entry(string Detail, DateTimeOffset At);
    public sealed record Document(string GameVersion, Dictionary<string, Entry> Seen);
    private static readonly TimeSpan Refresh = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, Entry> seen;
    private readonly string gameVersion;

    private SelfCheckMemory(string gameVersion, Dictionary<string, Entry> seen) { this.gameVersion = gameVersion; this.seen = seen; }
    public static SelfCheckMemory Empty(string gameVersion) => new(gameVersion, []);
    /// <summary>A saved file that is damaged, or of another version of the game, gives an empty memory.</summary>
    public static SelfCheckMemory Load(string? json, string gameVersion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return Empty(gameVersion);
            var document = JsonSerializer.Deserialize<Document>(json);
            return document is { Seen: not null } && document.GameVersion == gameVersion ? new(gameVersion, new(document.Seen)) : Empty(gameVersion);
        }
        catch (JsonException) { return Empty(gameVersion); }
    }
    public string Save() => JsonSerializer.Serialize(new Document(gameVersion, seen));
    public int Count => seen.Count;

    /// <summary>Keeps an OK line. True when this should reach the disk: a new domain or detail, or the time of a known one, at most every five minutes.</summary>
    public bool Record(SelfCheckLine line, DateTimeOffset now)
    {
        if (line.Status != SelfCheckStatus.Ok) return false;
        var known = seen.GetValueOrDefault(line.Domain);
        if (known is not null && known.Detail == line.Detail && now - known.At < Refresh) return false;
        seen[line.Domain] = new(line.Detail, now);
        return true;
    }

    /// <summary>Only a line with nothing to say takes the memory: a live OK, SUSPECT or FAILED always wins.</summary>
    public SelfCheckLine Apply(SelfCheckLine line)
        => line.Status == SelfCheckStatus.NotRead && seen.GetValueOrDefault(line.Domain) is { } known
            ? new(line.Domain, SelfCheckStatus.Seen, $"{known.Detail} at {known.At.ToLocalTime():MM-dd HH:mm} ({line.Detail} now)") : line;
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
