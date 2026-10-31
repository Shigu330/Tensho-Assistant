namespace TenshoAssistant.Alarms;

public enum AlarmRepeat { Once, EorzeaDaily }
public enum AlarmCheck { None, Fire, SkipWeather }

/// <summary>
/// A Tensho alarm (0.201): once at a real date, or every Eorzea day at an ET time (one ET day = 70 real minutes), with its own reminder lead
/// in real minutes and, where the weather matters (fish, weather search), the weathers the period must have (empty = no condition).
/// </summary>
public sealed class TenshoAlarm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = "";
    // Display group when the alarm has no item (weather, vista, own name) and its game icon (0.202).
    public string Group { get; set; } = "";
    public uint IconId { get; set; }
    // Gathering job of the item (ClassJob 16 miner, 17 botanist, 18 fisher; 0 = none or unknown), shown with the alarm (0.203).
    public uint JobId { get; set; }
    // Name of the copy in the game's alarms (0.205), empty when not sent; Tensho then leaves the ringing to the game.
    public string GameName { get; set; } = "";
    public AlarmRepeat Repeat { get; set; }
    public DateTimeOffset At { get; set; }
    // ET minute of the day (0–1439) for EorzeaDaily.
    public int EtMinute { get; set; }
    public int LeadMinutes { get; set; } = 5;
    public uint TerritoryId { get; set; }
    public uint ItemId { get; set; }
    public List<uint> Weathers { get; set; } = [];
    public List<uint> PreviousWeathers { get; set; } = [];
    public bool Enabled { get; set; } = true;
    // Occurrence already handled (fired or skipped for its weather), so it is not handled twice.
    public DateTimeOffset LastOccurrence { get; set; }
}

/// <summary>Settings of the alarm system: default reminder lead, game alarm sound (AlarmSoundEffect 0–5), chat line, silence in duties.</summary>
public sealed class AlarmSettings
{
    public int LeadMinutes { get; set; } = 5;
    public int Sound { get; set; }
    public bool PlaySound { get; set; } = true;
    public bool Chat { get; set; } = true;
    public bool MuteInDuties { get; set; } = true;
    // Also create daily ET alarms in the game's alarm list through « /alarm » (0.205); weather-conditioned ones only on request,
    // the game not checking the weather.
    public bool UseGameAlarms { get; set; }
    public bool GameIncludeWeather { get; set; }
    // Delay between two « /alarm » commands sent to the game (Settings › Advanced, 250–5000 ms).
    public int GameCommandDelayMs { get; set; } = 500;
}

public static class AlarmSchedule
{
    public const int Maximum = 60;
    // Real seconds of one ET day (24 ET hours × 175 s) and of one ET minute (175 / 60 s).
    public const double EtDaySeconds = 4200, EtMinuteSeconds = 175.0 / 60;

    /// <summary>Longest lead allowed: up to one day for a single alarm, under one ET day for a daily one.</summary>
    public static int ClampLead(AlarmRepeat repeat, int minutes) => Math.Clamp(minutes, 0, repeat == AlarmRepeat.EorzeaDaily ? 60 : 1440);

    /// <summary>First occurrence strictly after <paramref name="after"/>, or null when a single alarm is past.</summary>
    public static DateTimeOffset? Next(TenshoAlarm alarm, DateTimeOffset after)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.Repeat == AlarmRepeat.Once) return alarm.At > after ? alarm.At : null;
        var minute = Math.Clamp(alarm.EtMinute, 0, 1439);
        var seconds = after.ToUnixTimeMilliseconds() / 1000.0;
        var occurrence = Math.Floor(seconds / EtDaySeconds) * EtDaySeconds + minute * EtMinuteSeconds;
        if (occurrence <= seconds) occurrence += EtDaySeconds;
        return DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(occurrence * 1000));
    }

    /// <summary>The next <paramref name="count"/> occurrences after the date (at most one for a single alarm).</summary>
    public static IReadOnlyList<DateTimeOffset> Occurrences(TenshoAlarm alarm, DateTimeOffset after, int count)
    {
        var list = new List<DateTimeOffset>();
        var from = after;
        while (list.Count < count && Next(alarm, from) is { } next) { list.Add(next); from = next; }
        return list;
    }

    /// <summary>
    /// Whether the alarm rings now: its next occurrence is within the lead and not handled yet. With weather conditions, an occurrence whose
    /// period (or previous period) does not have them is skipped; an unknown forecast (null) is not a match.
    /// </summary>
    public static (AlarmCheck Check, DateTimeOffset Occurrence) Check(TenshoAlarm alarm, DateTimeOffset now, Func<DateTimeOffset, uint?>? weatherAt = null)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (!alarm.Enabled || Next(alarm, now.AddSeconds(-1)) is not { } occurrence || occurrence == alarm.LastOccurrence) return (AlarmCheck.None, default);
        if (occurrence.AddMinutes(-ClampLead(alarm.Repeat, alarm.LeadMinutes)) > now) return (AlarmCheck.None, occurrence);
        if (alarm.Weathers.Count > 0 && (weatherAt?.Invoke(occurrence) is not { } weather || !alarm.Weathers.Contains(weather))) return (AlarmCheck.SkipWeather, occurrence);
        if (alarm.PreviousWeathers.Count > 0 && (weatherAt?.Invoke(occurrence.AddSeconds(-1400)) is not { } previous || !alarm.PreviousWeathers.Contains(previous)))
            return (AlarmCheck.SkipWeather, occurrence);
        return (AlarmCheck.Fire, occurrence);
    }

    /// <summary>Daily alarms for every ET window start of an item (gathering point or fish), without duplicates.</summary>
    public static IReadOnlyList<TenshoAlarm> ForWindows(string label, IEnumerable<int> startMinutes, int lead, uint territory, uint item,
        IReadOnlyList<uint>? weathers = null, IReadOnlyList<uint>? previous = null) =>
        startMinutes.Select(minute => ((minute % 1440) + 1440) % 1440).Distinct().Order().Select(minute => new TenshoAlarm
        {
            Label = $"{label} · {minute / 60:00}:{minute % 60:00} ET", Repeat = AlarmRepeat.EorzeaDaily, EtMinute = minute,
            LeadMinutes = ClampLead(AlarmRepeat.EorzeaDaily, lead), TerritoryId = territory, ItemId = item,
            Weathers = [.. weathers ?? []], PreviousWeathers = [.. previous ?? []],
        }).ToArray();

    /// <summary>
    /// Gathering job of a gathering type: GatheringType 0–1 (ore, stone) miner, 2–3 (logging, harvesting) botanist, 4–5 (spearfishing) and
    /// Tensho's fishing type 100 fisher. The GatheringType sheet has no ClassJob column; type names read by PROBE_GTYPE
    /// (docs/evidence/0.203-gathering-jobs.md).
    /// </summary>
    public static uint JobOf(uint gatheringType, bool fishing) => fishing ? 18u : gatheringType switch { 0 or 1 => 16u, 2 or 3 => 17u, 4 or 5 or 100 => 18u, _ => 0u };

    /// <summary>Adds alarms, skipping one with the same label and timing; false when the list would exceed the maximum.</summary>
    public static bool Add(List<TenshoAlarm> alarms, IEnumerable<TenshoAlarm> added)
    {
        var fresh = added.Where(alarm => !alarms.Any(known => known.Label == alarm.Label && known.Repeat == alarm.Repeat
            && (alarm.Repeat == AlarmRepeat.Once ? known.At == alarm.At : known.EtMinute == alarm.EtMinute))).ToArray();
        if (alarms.Count + fresh.Length > Maximum) return false;
        alarms.AddRange(fresh);
        return true;
    }

    /// <summary>Single alarms whose time has passed (by more than ten minutes) are removed.</summary>
    public static int RemoveExpired(List<TenshoAlarm> alarms, DateTimeOffset now) =>
        alarms.RemoveAll(alarm => alarm.Repeat == AlarmRepeat.Once && alarm.At.AddMinutes(10) < now);
}
