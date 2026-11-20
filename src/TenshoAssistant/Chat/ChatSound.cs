namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B7 (0.1646, the user's choice of 15/11, after Prat and WIM): the sound a message copied for the Tensho chat window plays: the alert's
/// sound when it alerts (an alert word, or your first name), else its channel's; none for what you send, a combat line (DEC-053: shown as the
/// game shows them, never read) or a player you hid. The game's own sounds (the macros' &lt;se.1&gt; to &lt;se.16&gt;), played by the reader on
/// the game's thread while logged in; nothing is sent to the game (R03.3). Pure (tests).
/// </summary>
public static class ChatSound
{
    /// <summary>At most one sound in this time (a busy channel never rings at each line).</summary>
    public static readonly TimeSpan Gap=TimeSpan.FromSeconds(2);

    /// <summary>The sound <paramref name="record"/> plays (1 to 16), 0 for none.</summary>
    public static int For(ChatRecord record,ChatSettings settings,bool alert,IReadOnlyList<string>? hidden=null)
    {
        if(record.Channel==ChatChannel.Combat || ChatAlert.Sent(record))return 0;
        if(hidden is { Count:>0 } && ChatTextFilter.IsPlayer(record.Sender,hidden))return 0;
        if(alert && Alarms.MacroSound.Valid(settings.AlertSound))return settings.AlertSound;
        return settings.SoundOf(record.Channel);
    }
}

/// <summary>X-447 B7 (0.1646): one sound at most every <see cref="ChatSound.Gap"/> (<see cref="Environment.TickCount64"/>; the first one plays).</summary>
public sealed class ChatSoundGate
{
    private long last=-100_000;

    public bool Take(long now)
    {
        if(now-last<(long)ChatSound.Gap.TotalMilliseconds)return false;
        last=now;
        return true;
    }
}
