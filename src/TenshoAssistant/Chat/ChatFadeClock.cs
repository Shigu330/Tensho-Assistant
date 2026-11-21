namespace TenshoAssistant.Chat;

/// <summary>
/// X-732 (0.1736, the user's report of 21/11 around 15:20, « S'effacer après » at 10 s: the chat window, once faded out, did not come back when a
/// message came): ImGui draws nothing of a window whose alpha is 0 at its Begin, and the tabs' lines are refreshed inside the window's content,
/// so a faded-out window no longer saw its tab's new messages and stayed faded. The window's fade (called before its content) now takes the
/// newest message of the tab shown from the chat's data (<see cref="Newest"/>), and, with the setting on, a tell received or an alert in any
/// tab brings it back too (<see cref="Wakes"/>). When the last message that keeps it up came. Pure (tests).
/// </summary>
public sealed class ChatFadeClock(long now)
{
    private long seen=-1;
    /// <summary>When the last message that keeps the window up came (Environment.TickCount64, milliseconds).</summary>
    public long LastAt{get;private set;}=now;

    /// <summary>The tab shown's newest message (its id; -1: none: another tab chosen counts as new) and whether another message brings the
    /// window back (<see cref="Wakes"/>, the setting on), at <paramref name="at"/>.</summary>
    public void Note(long newest,bool wake,long at)
    {
        if(newest==seen && !wake)return;
        seen=newest;LastAt=at;
    }

    /// <summary>The window's alpha at <paramref name="at"/> (ChatLook.Idle: <paramref name="after"/> seconds without such a message, then it fades out).</summary>
    public float? Alpha(float? alpha,int after,long at,bool kept)=>ChatLook.Idle(alpha,after,(at-LastAt)/1000d,kept);

    /// <summary>The newest message of a tab, its view refreshed from the store first (the lines it shows, as the window's content does); -1 when none.</summary>
    public static long Newest(ChatStore store,ChatTabSettings tab,ChatSettings settings,ChatFilterTable? table)
    {
        var view=store.ViewOf(tab);
        view.Refresh(store,tab,settings.BoundedMaxLines,table,settings);
        return view.Count>0 ? view.RecordAt(view.Count-1).Id:-1;
    }

    /// <summary>Messages that bring a faded window back besides the tab shown's (the setting on): a tell received, or a message holding an
    /// alert (ChatAlert: an alert word, your first name said by a player); never what you send, never a combat line (ChatAlert).</summary>
    public static bool Wakes(IEnumerable<ChatRecord> records,IReadOnlyList<string> words,string? ownName)
        =>records.Any(record=>(record.Channel==ChatChannel.TellIn && !ChatAlert.Sent(record)) || ChatAlert.Holds(record,words,ownName));
}
