namespace TenshoAssistant.Alarms;

/// <summary>
/// The game's alarm text command (0.205), from the TextCommand sheet row 229 (docs/evidence/0.205-alarm-command.md): « /alarm » in every
/// client language (French alias « /alerte »), with the English sub-commands every language accepts: clock « et », « rp » (repeat; daily for
/// Eorzea time per FFXIVClientStructs AlarmFlag.Repeat), time HHmm, reminder 0–60 minutes, sound « se00 »–« se05 », « mute » in duties.
/// The game keeps at most 30 alarms; names are 20 characters at most.
/// </summary>
public static class GameAlarmCommand
{
    public const int GameMaximum = 30, NameLength = 20;

    /// <summary>Name in the game: the label without quotes or control characters, shortened, ending with the ET time to stay distinct.</summary>
    public static string Name(string label, int etMinute)
    {
        var time = $" {etMinute / 60 % 24:00}{etMinute % 60:00}";
        var clean = new string((label ?? "").Where(character => character != '"' && !char.IsControl(character)).ToArray()).Trim();
        var room = NameLength - time.Length;
        if (clean.Length > room) clean = clean[..room].TrimEnd();
        return clean.Length == 0 ? time.Trim() : clean + time;
    }

    /// <summary>The command for a daily Eorzea-time alarm, or null when the alarm is not daily ET.</summary>
    public static string? Build(TenshoAlarm alarm, int sound, bool mute)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.Repeat != AlarmRepeat.EorzeaDaily) return null;
        var minute = Math.Clamp(alarm.EtMinute, 0, 1439);
        return $"/alarm \"{Name(alarm.Label, minute)}\" et rp {minute / 60:00}{minute % 60:00} {Math.Clamp(alarm.LeadMinutes, 0, 60)} se{Math.Clamp(sound, 0, 5):00}"
            + (mute ? " mute" : "");
    }

    /// <summary>Only an alarm command built here may be sent to the game.</summary>
    public static bool IsAlarmCommand(string command) =>
        command.StartsWith("/alarm \"", StringComparison.Ordinal) && command.Length <= 100 && !command.Contains('\n') && !command.Contains('\r');
}
