using System.Globalization;
using System.Text;

namespace TenshoAssistant.Inventory;

/// <summary>
/// X-557 (0.1774, the user's request of 17/11: « check the parsers to see whether more categories can come out of « Other » »): what surrounds
/// a gil change classified « Other », written once in the log 3 seconds after it, so the agent can propose categories taken from the game (R06):
/// the game's messages (kind and text) and the log message rows heard, and the game windows seen open, within 3 seconds before and after.
/// Privacy (R25, the rule of 0.888 in ReadingCapture): a message is kept only when the game speaks (no sender), when it names no player (no
/// player payload) and when it holds none of the character's own names (the character, its first or last name, its worlds, its retainers):
/// such a message is left out whole, never masked (a masked name was once followed by the same name in plain text). Windows are addon names
/// and rows are numbers: no player data. Pure, so the decisions are tested; the watcher feeds it on the framework thread.
/// X-773 M3 (review of 0.1775): without a sender nor a player payload, another player's name in plain text still passed (another plugin's
/// Print, /echo), over every kind and up to 80 messages. Now only the game's own lines that may go with a gil change are kept (<see
/// cref="KindKept"/>: system, error, items obtained, progress, retainer sales), never the kinds plugins print with by default (Dalamud's
/// Print and PrintError: 1 and 2), the echo, the players' channels, combat (never read, DEC-053), nor the kinds that name other players
/// (rolls, the company's announcements, others' crafting and gathering); at most 20 messages.
/// </summary>
public sealed class GilOtherContext
{
    public static readonly TimeSpan Around=TimeSpan.FromSeconds(3);
    /// <summary>What is kept at most: messages and rows heard, window readings (one a second), changes waiting for their 3 seconds after.</summary>
    public const int MaxMessages=20,MaxRows=80,MaxReadings=30,MaxPending=20,MaxText=200;
    private readonly List<(DateTimeOffset At,int Kind,string Text)> messages=[];
    private readonly List<(DateTimeOffset At,uint Row)> rows=[];
    private readonly List<(DateTimeOffset At,string[] Windows)> readings=[];
    private readonly List<(DateTimeOffset At,long Change)> pending=[];

    /// <summary>Changes waiting to be written (for the tests and the self-check).</summary>
    public int Pending=>pending.Count;

    /// <summary>
    /// The text when it may be written, else null: a sender (a player or an NPC speaking), a player payload, or any of the character's own
    /// names (<paramref name="personal"/>, compared without case; empty ones ignored) leave the message out whole.
    /// </summary>
    public static string? Safe(bool hasSender,bool namesPlayer,string text,IEnumerable<string> personal)
    {
        if(hasSender || namesPlayer)return null;
        var trimmed=text.Trim();
        if(trimmed.Length==0)return null;
        // Compared without case nor accents, apostrophes folded (the shared helper): « Dorée » is found in « DOREE » too.
        foreach(var name in personal)
            if(name.Trim() is { Length:>=2 } part && SearchText.Contains(trimmed,part,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace,CultureInfo.InvariantCulture))return null;
        return trimmed.Length>MaxText ? trimmed[..MaxText]+"…":trimmed;
    }

    /// <summary>
    /// The character's own names to keep out of the log (its name, worlds, company tag, retainers' names): each one whole and each of its
    /// words (« First Last » gives « First » and « Last » too); fewer than two letters left out. A message holding one is left out whole.
    /// X-773 M2 (review of 0.1775): <paramref name="whole"/> are kept whole only (the company's name: its words are often common ones, « of »,
    /// « de », which would leave out nearly every message).
    /// </summary>
    public static IReadOnlyList<string> PersonalNames(IEnumerable<string?> owned,IEnumerable<string?>? whole=null)
    {
        var names=new List<string>();
        foreach(var name in owned)
        {
            if(string.IsNullOrWhiteSpace(name))continue;
            names.Add(name.Trim());
            names.AddRange(name.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries));
        }
        foreach(var name in whole ?? [])
            if(!string.IsNullOrWhiteSpace(name))names.Add(name.Trim());
        return names.Where(name=>name.Length>=2).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>X-773 M3: the kinds whose messages may be kept: 57 system, 60 error, 62 items obtained, 64 progress (experience, quests), 71
    /// retainer sales (docs/evidence/0.1434-chat-logfilter.md for the kinds). Any other kind is left out whole.</summary>
    public static readonly IReadOnlySet<int> KeptKinds=new HashSet<int>{57,60,62,64,71};

    /// <summary>X-773 M3: whether a message of this kind may be kept (<see cref="KeptKinds"/>).</summary>
    public static bool KindKept(int kind)=>KeptKinds.Contains(kind);

    /// <summary>A game message heard (see <see cref="KindKept"/> and <see cref="Safe"/>); whether it was kept.</summary>
    public bool Message(DateTimeOffset at,int kind,bool hasSender,bool namesPlayer,string text,IEnumerable<string> personal)
    {
        if(!KindKept(kind) || Safe(hasSender,namesPlayer,text,personal) is not { } kept)return false;
        messages.Add((at,kind,kept));
        if(messages.Count>MaxMessages)messages.RemoveAt(0);
        return true;
    }

    /// <summary>A log message row heard (a number of the LogMessage sheet, language-independent).</summary>
    public void Row(DateTimeOffset at,uint row)
    {
        rows.Add((at,row));
        if(rows.Count>MaxRows)rows.RemoveAt(0);
    }

    /// <summary>The game windows seen open at <paramref name="at"/> (one reading a second).</summary>
    public void Windows(DateTimeOffset at,IEnumerable<string> open)
    {
        readings.Add((at,open.Where(name=>name.Length>0).Distinct(StringComparer.Ordinal).ToArray()));
        if(readings.Count>MaxReadings)readings.RemoveAt(0);
    }

    /// <summary>A change classified « Other » at <paramref name="at"/>: written by <see cref="Due"/> once its 3 seconds after have passed.</summary>
    public void Other(DateTimeOffset at,long change)
    {
        if(pending.Count>=MaxPending)return;
        pending.Add((at,change));
    }

    /// <summary>The lines of the changes whose 3 seconds after have passed at <paramref name="now"/>, each written once; old readings dropped.</summary>
    public IReadOnlyList<string> Due(DateTimeOffset now)
    {
        var lines=new List<string>();
        for(var index=0;index<pending.Count;)
        {
            var (at,change)=pending[index];
            if(now-at<Around){index++;continue;}
            lines.Add(Line(at,change));
            pending.RemoveAt(index);
        }
        // Kept: what a change still waiting may need (its 3 seconds before), and the last 3 seconds for a change to come.
        var keep=(pending.Count>0 ? pending.Min(item=>item.At):now)-Around;
        messages.RemoveAll(item=>item.At<keep);rows.RemoveAll(item=>item.At<keep);readings.RemoveAll(item=>item.At<keep);
        return lines;
    }

    private string Line(DateTimeOffset at,long change)
    {
        bool Near(DateTimeOffset when)=>when>=at-Around && when<=at+Around;
        string Offset(DateTimeOffset when)=>((when-at).TotalSeconds).ToString("+0.0;-0.0;0.0",CultureInfo.InvariantCulture)+" s";
        var text=new StringBuilder();
        text.Append(CultureInfo.InvariantCulture,$"Gil other context: {change.ToString("+#,0;-#,0",CultureInfo.InvariantCulture)}; game messages within 3 s: ");
        var near=messages.Where(item=>Near(item.At)).ToArray();
        text.Append(near.Length==0 ? "none":string.Join(" | ",near.Select(item=>$"[{Offset(item.At)}] kind {item.Kind}: {item.Text}")));
        var heard=rows.Where(item=>Near(item.At)).ToArray();
        text.Append("; log message rows: ").Append(heard.Length==0 ? "none":string.Join(", ",heard.Select(item=>$"{item.Row} ({Offset(item.At)})")));
        var open=readings.Where(item=>Near(item.At)).SelectMany(item=>item.Windows).Distinct(StringComparer.Ordinal).ToArray();
        text.Append("; windows open within 3 s: ").Append(open.Length==0 ? "none":string.Join(", ",open)).Append('.');
        return text.ToString();
    }
}
