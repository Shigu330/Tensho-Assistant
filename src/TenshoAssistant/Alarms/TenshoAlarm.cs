namespace TenshoAssistant.Alarms;

// 0.577 (T0): in real time: from a date (At), each day or week; or at each game reset.
public enum AlarmRepeat { Once, EorzeaDaily, RealDaily, RealWeekly, DailyReset, WeeklyReset }
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
    // 0.832 (X-62): the quantity a tracking list needed when the alarm was added from it; 0 for every other alarm.
    public long Needed { get; set; }
    public List<uint> Weathers { get; set; } = [];
    public List<uint> PreviousWeathers { get; set; } = [];
    public bool Enabled { get; set; } = true;
    // Occurrence already handled (fired or skipped for its weather), so it is not handled twice.
    public DateTimeOffset LastOccurrence { get; set; }
    // 0.556 (T20): the occurrence that really rang (LastOccurrence also marks one skipped for its weather), and the one that rang again on time.
    public DateTimeOffset LastRung { get; set; }
    public DateTimeOffset LastOnTime { get; set; }
    // R-12 (0.319): tool that created the alarm (Discord categories; older alarms: Other, the gathering job then decides), whether it
    // goes to Discord when the webhook is on, and its own Discord message (empty = the template of the settings).
    public AlarmSource Source { get; set; }
    // 0.371 (g, M-01): key of the deadline an automatic alarm follows (AutoAlarms); empty for the player's own alarms.
    public string AutoKey { get; set; } = "";
    // 0.578 (user request): the row of the Reminders page this alarm comes from, for its Discord box.
    public string ReminderRow { get; set; } = "";
    /// <summary>0.778 (X-23, user request): off for a new alarm; one joining a group takes the group's (MainWindow.AddAlarms). A saved alarm keeps its own.</summary>
    public bool Discord { get; set; }
    public string DiscordMessage { get; set; } = "";
}

/// <summary>Settings of the alarm system: default reminder lead, game alarm sound (AlarmSoundEffect 0–5), chat line, silence in duties.</summary>
public sealed class AlarmSettings
{
    public int LeadMinutes { get; set; } = 5;
    public int Sound { get; set; }
    public bool PlaySound { get; set; } = true;
    public bool Chat { get; set; } = true;
    public bool MuteInDuties { get; set; } = true;
    // 0.556 (T20): ring a second time at the exact time; Discord only gets the first.
    public bool RingOnTime { get; set; }
    // 0.557 (T19): how long « remind me later » waits.
    public int SnoozeMinutes { get; set; } = 5;
    // Queue step e (audit 2026-11-02): with MuteInDuties, no [Tensho] chat line in a duty either. Off by default (the line stays, as
    // before); Discord and the alarms copied into the game are not affected.
    public bool MuteChatInDuties { get; set; }
    // Also create daily ET alarms in the game's alarm list through « /alarm » (0.205); weather-conditioned ones only on request,
    // the game not checking the weather.
    public bool UseGameAlarms { get; set; }
    public bool GameIncludeWeather { get; set; }
    // Delay between two « /alarm » commands sent to the game (Settings › Advanced, 250–5000 ms).
    public int GameCommandDelayMs { get; set; } = 500;
    // 0.366 (g, C-10): a chat line and the alarm sound when a duty is ready (Duty Finder pop); off by default.
    public bool DutyPopAlert { get; set; }
    // 0.371 (g, M-01): automatic alarms at the ventures' end and the vessels' return (user's choice: on by default).
    public bool AutoAlarms { get; set; } = true;

    /// <summary>Whether a ringing Tensho alarm plays its sound: not in a duty while MuteInDuties is on.</summary>
    public bool SoundAllowed(bool inDuty) => PlaySound && !(MuteInDuties && inDuty);
    /// <summary>Whether it writes its [Tensho] chat line: not in a duty when MuteInDuties and MuteChatInDuties are both on.</summary>
    public bool ChatAllowed(bool inDuty) => Chat && !(MuteInDuties && MuteChatInDuties && inDuty);
}

public static class AlarmSchedule
{
    /// <summary>0.832 (X-62, user report): an alarm added from a tracking list stays silent once the item is held in the quantity the list needed.</summary>
    public static bool Gathered(TenshoAlarm alarm, long held) => alarm.ItemId != 0 && alarm.Needed > 0 && held >= alarm.Needed;

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
        if (alarm.Repeat == AlarmRepeat.DailyReset) return TenshoAssistant.Reminders.ResetSchedule.NextDaily(after, TenshoAssistant.Reminders.ResetSchedule.DailyHour);
        if (alarm.Repeat == AlarmRepeat.WeeklyReset) return TenshoAssistant.Reminders.ResetSchedule.NextWeekly(after, TenshoAssistant.Reminders.ResetSchedule.WeeklyDay, TenshoAssistant.Reminders.ResetSchedule.WeeklyHour);
        if (alarm.Repeat is AlarmRepeat.RealDaily or AlarmRepeat.RealWeekly)
        {
            if (alarm.At > after) return alarm.At;
            var days = alarm.Repeat == AlarmRepeat.RealDaily ? 1 : 7;
            var periods = (long)Math.Floor((after - alarm.At).TotalDays / days) + 1;
            return alarm.At.AddDays(periods * days);
        }
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
        // 0.331: an occurrence up to 30 s past is still due (with a lead of 0 it rang only within one second, which a long frame or a
        // loading screen could skip); LastOccurrence keeps it from ringing twice.
        if (!alarm.Enabled || Next(alarm, now.AddSeconds(-30)) is not { } occurrence || occurrence == alarm.LastOccurrence) return (AlarmCheck.None, default);
        if (occurrence.AddMinutes(-ClampLead(alarm.Repeat, alarm.LeadMinutes)) > now) return (AlarmCheck.None, occurrence);
        if (alarm.Weathers.Count > 0 && (weatherAt?.Invoke(occurrence) is not { } weather || !alarm.Weathers.Contains(weather))) return (AlarmCheck.SkipWeather, occurrence);
        if (alarm.PreviousWeathers.Count > 0 && (weatherAt?.Invoke(occurrence.AddSeconds(-1400)) is not { } previous || !alarm.PreviousWeathers.Contains(previous)))
            return (AlarmCheck.SkipWeather, occurrence);
        return (AlarmCheck.Fire, occurrence);
    }

    /// <summary>0.557 (T19): the same alarm once more in a few minutes; the original is left alone.</summary>
    public static TenshoAlarm Snooze(TenshoAlarm alarm, DateTimeOffset now, int minutes)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        return new TenshoAlarm { Label = alarm.Label, Group = alarm.Group, IconId = alarm.IconId, JobId = alarm.JobId, ItemId = alarm.ItemId, TerritoryId = alarm.TerritoryId,
            Source = alarm.Source, Discord = alarm.Discord, DiscordMessage = alarm.DiscordMessage, Repeat = AlarmRepeat.Once, At = now.AddMinutes(Math.Clamp(minutes, 1, 1440)), LeadMinutes = 0 };
    }

    /// <summary>0.556 (T20): the occurrence to ring again at its exact time: it rang ahead, its time has come, and it has not rung twice. Without a lead the first ring was already on time.</summary>
    public static DateTimeOffset? OnTimeDue(TenshoAlarm alarm, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (!alarm.Enabled || ClampLead(alarm.Repeat, alarm.LeadMinutes) == 0 || Next(alarm, now.AddSeconds(-30)) is not { } occurrence) return null;
        return occurrence <= now && occurrence == alarm.LastRung && occurrence != alarm.LastOnTime ? occurrence : null;
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

    /// <summary>Job of an alarm: the one stored, else the only job among the item's gathering sources (alarms made before 0.203, or
    /// by a tool that does not store it); 0 when none or several.</summary>
    public static uint JobOfAlarm(TenshoAlarm alarm, Func<uint, IReadOnlyList<TenshoAssistant.Gathering.GatheringSource>?> sourcesOf)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.JobId != 0 || alarm.ItemId == 0 || sourcesOf(alarm.ItemId) is not { Count: > 0 } sources) return alarm.JobId;
        var jobs = sources.Select(source => JobOf(source.TypeId, source.IsFishing)).Where(job => job != 0).Distinct().ToArray();
        return jobs.Length == 1 ? jobs[0] : 0;
    }

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
