using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenshoAssistant.Gathering;

public enum GatheringScheduleKind { Unknown, Unrestricted, Limited, NoWindow, Invalid }
public readonly record struct GatheringWindow(int StartMinute, int EndMinute);

/// <summary>Copied static ET restrictions only; does not assert weather, unlocks or node presence.</summary>
[JsonConverter(typeof(GatheringScheduleJsonConverter))]
public sealed class GatheringSchedule
{
    public static GatheringSchedule Unknown { get; } = new(GatheringScheduleKind.Unknown, []);
    private static readonly GatheringSchedule Unrestricted = new(GatheringScheduleKind.Unrestricted, []);
    private static readonly GatheringSchedule Invalid = new(GatheringScheduleKind.Invalid, []);
    public GatheringScheduleKind Kind { get; }
    public IReadOnlyList<GatheringWindow> Windows { get; }
    public string Key { get; }
    public string Label { get; }

    private GatheringSchedule(GatheringScheduleKind kind, IEnumerable<GatheringWindow> windows)
    {
        Kind = kind;
        Windows = Array.AsReadOnly(windows.ToArray());
        Key = $"{kind}:{string.Join(';', Windows.Select(w => $"{w.StartMinute}-{w.EndMinute}"))}";
        Label = kind switch
        {
            GatheringScheduleKind.Unrestricted => "Sans restriction horaire ET",
            GatheringScheduleKind.Limited => string.Join(" · ", Windows.Select(w => $"{Time(w.StartMinute)}–{Time(w.EndMinute)}")) + " ET",
            GatheringScheduleKind.NoWindow => "Aucune fenêtre ET renseignée",
            GatheringScheduleKind.Invalid => "Horaire ET non interprétable",
            _ => "Horaire ET inconnu / non couvert",
        };
    }

    private static string Time(int minute) => $"{minute / 60:00}:{minute % 60:00}";
    private static bool TryMinutes(ushort value, out int minutes)
    {
        minutes = value / 100 * 60 + value % 100;
        return value <= 2400 && value % 100 < 60;
    }

    public static GatheringSchedule FromEphemeral(ushort start, ushort end)
    {
        if (start == ushort.MaxValue && end == ushort.MaxValue) return Unrestricted;
        if (!TryMinutes(start, out var first) || !TryMinutes(end, out var last)) return Invalid;
        first %= 1440;
        if (first == last % 1440) return Unrestricted;
        var duration = (last - first + 1440) % 1440;
        return Normalize([new(first, first + duration)]);
    }

    public static GatheringSchedule FromRare(IEnumerable<(ushort Start, ushort Duration)> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        var windows = new List<GatheringWindow>();
        foreach (var (start, rawDuration) in slots)
        {
            if (rawDuration == 0) continue; // Unused slots commonly have start=65535.
            if (!TryMinutes(start, out var first)
                || !TryMinutes(rawDuration == 160 ? (ushort)200 : rawDuration, out var duration)) return Invalid;
            windows.Add(new(first % 1440, first % 1440 + duration));
        }
        return Normalize(windows);
    }

    private static GatheringSchedule Normalize(IEnumerable<GatheringWindow> windows)
    {
        var pieces = new List<GatheringWindow>();
        foreach (var window in windows)
        {
            if (window.EndMinute <= 1440) pieces.Add(window);
            else
            {
                pieces.Add(new(window.StartMinute, 1440));
                pieces.Add(new(0, window.EndMinute - 1440));
            }
        }
        var merged = new List<GatheringWindow>();
        foreach (var piece in pieces.OrderBy(w => w.StartMinute).ThenBy(w => w.EndMinute))
        {
            if (merged.Count > 0 && piece.StartMinute <= merged[^1].EndMinute)
                merged[^1] = new(merged[^1].StartMinute, Math.Max(merged[^1].EndMinute, piece.EndMinute));
            else merged.Add(piece);
        }
        if (merged.Count == 1 && merged[0] == new GatheringWindow(0, 1440)) return Unrestricted;
        return new(merged.Count == 0 ? GatheringScheduleKind.NoWindow : GatheringScheduleKind.Limited, merged);
    }

    /// <summary>Restores a persisted schedule only if it is exactly what the copy rules would have produced; otherwise null.</summary>
    public static GatheringSchedule? FromStored(GatheringScheduleKind kind, IReadOnlyList<GatheringWindow>? windows)
    {
        windows ??= [];
        switch (kind)
        {
            case GatheringScheduleKind.Unknown: return windows.Count == 0 ? Unknown : null;
            case GatheringScheduleKind.Unrestricted: return windows.Count == 0 ? Unrestricted : null;
            case GatheringScheduleKind.Invalid: return windows.Count == 0 ? Invalid : null;
            case GatheringScheduleKind.NoWindow: return windows.Count == 0 ? new(GatheringScheduleKind.NoWindow, []) : null;
            case GatheringScheduleKind.Limited:
                if (windows.Count == 0 || windows.Any(w => w.StartMinute < 0 || w.EndMinute > 1440 || w.StartMinute >= w.EndMinute)) return null;
                var normalized = Normalize(windows);
                return normalized.Kind == GatheringScheduleKind.Limited && normalized.Windows.SequenceEqual(windows) ? normalized : null;
            default: return null;
        }
    }
}

internal sealed class GatheringScheduleJsonConverter : JsonConverter<GatheringSchedule>
{
    private sealed record Stored(GatheringScheduleKind Kind, GatheringWindow[] Windows);
    public override GatheringSchedule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stored = JsonSerializer.Deserialize<Stored>(ref reader, options) ?? throw new JsonException("Missing gathering schedule.");
        return GatheringSchedule.FromStored(stored.Kind, stored.Windows) ?? throw new JsonException("Invalid stored gathering schedule.");
    }
    public override void Write(Utf8JsonWriter writer, GatheringSchedule value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, new Stored(value.Kind, value.Windows.ToArray()), options);
}
