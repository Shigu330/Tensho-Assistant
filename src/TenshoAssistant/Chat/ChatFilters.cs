namespace TenshoAssistant.Chat;

/// <summary>X-484 (0.1482): one of the game's log filters (LogFilter sheet: the filters of the game's log window, read in the client's language):
/// a message kind, who does it (Caster) and to whom (Target), as masks of Dalamud's XivChatRelationKind (bit 1 &lt;&lt; kind; 65535: anyone),
/// its group (1 Discussion, 2 Announcements, 4 to 14 the combat lines by subject), its place and the game's default tabs (1 General, 2 Battle,
/// 4 Event). docs/evidence/0.1482-chat-log-filters.md.</summary>
/// <remarks>X-545 (0.1517): <paramref name="Example"/> is the sheet's example line, in the client's language, shown in the filter's tooltip.</remarks>
public sealed record ChatFilterRow(uint Id,int Kind,ushort Caster,ushort Target,byte Category,byte Order,byte Preset,string Name,string Example="");

/// <summary>X-484 (0.1482): the game's log filters, as the game files them; pure (tests). A message is shown by a tab when one of the filters it
/// chose matches it (kind, subject, target), as in the game's log window; a message no filter matches (a kind the game does not list) is shown
/// only when the tab keeps « the others ».</summary>
public sealed class ChatFilterTable
{
    public const ushort Anyone=ushort.MaxValue;
    public const byte Discussion=1,Announcements=2,FirstCombat=4,LastCombat=14;
    public const byte GeneralTab=1,BattleTab=2,EventTab=4;
    public IReadOnlyList<ChatFilterRow> Rows{get;}

    public ChatFilterTable(IEnumerable<ChatFilterRow> rows)
        =>Rows=rows.Where(row=>row.Kind>0 && row.Category>0 && row.Name.Length>0).OrderBy(row=>row.Category).ThenBy(row=>row.Order).ThenBy(row=>row.Id).ToList();

    /// <summary>The game files one « Tell » filter (kind 12; its example shows both directions) and one « Party » filter (kind 14): the
    /// incoming tells (13) and the cross-world party (32) go with them.</summary>
    public static int FilterKind(int kind)=>kind switch{13=>12,32=>14,_=>kind};

    public static bool Matches(ChatFilterRow row,int kind,byte source,byte target)
        =>row.Kind==FilterKind(kind) && Covers(row.Caster,source) && Covers(row.Target,target);

    private static bool Covers(ushort mask,byte relation)=>mask==Anyone || relation<16 && (mask&(1<<relation))!=0;

    public static bool IsCombat(byte category)=>category is >=FirstCombat and <=LastCombat;

    /// <summary>X-484 (0.1518, the user's request of 17/11: the combat filters by kind of line): a combat filter's kind of line, as the game's
    /// log window files it under each subject: the message kind (41 damage, 42 failed attacks, 43 actions, 44 items, 45 healing, 46 beneficial
    /// effects, 47 detrimental effects, 48 and 49 their end) and its side: done by the subject (« Subject » in the game, the target anyone) or
    /// done to the subject (« Object », the caster anyone). Null for a filter outside the combat lines.</summary>
    public static (int Kind,bool ToSubject)? Genre(ChatFilterRow row)
        =>IsCombat(row.Category) ? (row.Kind,row.Caster==Anyone && row.Target!=Anyone):null;

    /// <summary>X-484 (0.1518): the combat filters grouped by kind of line across every subject (« damage dealt »: by you, by your party
    /// members… by others' pets), in the game's order (the side done by the subject first, then the side done to it, each by kind).</summary>
    public IReadOnlyList<((int Kind,bool ToSubject) Genre,IReadOnlyList<ChatFilterRow> Rows)> Genres()
        =>Rows.Where(row=>IsCombat(row.Category)).GroupBy(row=>Genre(row)!.Value).OrderBy(group=>group.Key.ToSubject).ThenBy(group=>group.Min(row=>row.Order))
            .ThenBy(group=>group.Key.Kind).Select(group=>(group.Key,(IReadOnlyList<ChatFilterRow>)group.ToList())).ToList();

    /// <summary>X-484 (0.1518): the localisation key of a kind of line (« ChatWindow.Genre41By », « ChatWindow.Genre41On »).</summary>
    public static string GenreKey((int Kind,bool ToSubject) genre)=>$"ChatWindow.Genre{genre.Kind}{(genre.ToSubject ? "On":"By")}";

    /// <summary>The game's default filters of one of its tabs (General, Battle, Event).</summary>
    public IEnumerable<uint> Preset(byte gameTab)=>Rows.Where(row=>(row.Preset&gameTab)!=0).Select(row=>row.Id);
    public IEnumerable<uint> AllIds=>Rows.Select(row=>row.Id);
    /// <summary>The filters of the messages a tab of channels shows: the first step from channels to the game's filters.</summary>
    public IEnumerable<uint> FromChannels(IEnumerable<ChatChannel> channels)
    {
        var chosen=channels.ToHashSet();
        return Rows.Where(row=>chosen.Contains(ChatChannels.From(row.Kind))).Select(row=>row.Id);
    }
}

/// <summary>X-484 (0.1482, beyond the game): words kept or hidden in a tab, players hidden or put forward. A list is written with commas.</summary>
public static class ChatTextFilter
{
    public static IReadOnlyList<string> Words(string? list)
        =>string.IsNullOrWhiteSpace(list) ? []:list.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Where(word=>word.Length>0).ToArray();

    /// <summary>Kept: no word to keep, or one of them is in the text; and none of the words to hide.</summary>
    public static bool Passes(string text,IReadOnlyList<string> keep,IReadOnlyList<string> hide,bool matchCase)
    {
        var comparison=matchCase ? StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;
        if(keep.Count>0 && !keep.Any(word=>text.Contains(word,comparison)))return false;
        return !hide.Any(word=>text.Contains(word,comparison));
    }

    /// <summary>The sender is one of these players: its name before the world (« Ann Doe@World »), whatever the case.</summary>
    public static bool IsPlayer(string sender,IReadOnlyList<string> names)
    {
        if(names.Count==0 || sender.Length==0)return false;
        var at=sender.IndexOf('@');
        var name=(at>0 ? sender[..at]:sender).Trim();
        return names.Any(chosen=>string.Equals(chosen,name,StringComparison.OrdinalIgnoreCase) || string.Equals(chosen,sender.Trim(),StringComparison.OrdinalIgnoreCase));
    }

    public static string Text(ChatRecord record)=>record.Sender+" "+string.Concat(record.Body.Select(segment=>segment.Text));
}

/// <summary>X-484 (0.1520, beyond the game): the alert words: a message holding one stands out and marks its tab until seen. Pure (tests).</summary>
public static class ChatAlert
{
    /// <summary>A message (its text, not its sender's name) holds one of the words, capitals ignored; what you send yourself never alerts.</summary>
    public static bool Holds(ChatRecord record,IReadOnlyList<string> words)
    {
        if(words.Count==0 || !ChatChannels.CountsAsUnread(record.Channel))return false;
        var text=string.Concat(record.Body.Select(segment=>segment.Text));
        return words.Any(word=>SearchText.Contains(text,word,StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The tab's label with « ! » in front while an alert in it is not seen.</summary>
    public static string Mark(string label,int alerts)=>alerts>0 ? "! "+label:label;
}

/// <summary>X-484 (0.1519, beyond the game): a message that repeats the one shown just before it in a tab: same kind, channel, sender and text,
/// at most <see cref="Window"/> after the last time it came. Pure (tests).</summary>
public static class ChatRepeat
{
    public static readonly TimeSpan Window=TimeSpan.FromSeconds(60);

    public static bool Repeats(ChatRecord shown,DateTimeOffset lastSeen,ChatRecord next)
        =>next.Kind==shown.Kind && next.Channel==shown.Channel && string.Equals(next.Sender,shown.Sender,StringComparison.Ordinal)
          && next.Time>=lastSeen && next.Time-lastSeen<=Window
          && string.Equals(ChatTextFilter.Text(next),ChatTextFilter.Text(shown),StringComparison.Ordinal);
}

/// <summary>X-484 (0.1482): what one tab shows, made once per refresh: by its channels, or by the game's filters it chose (when the game's
/// filters are read), then the players hidden and its words.</summary>
public sealed class ChatTabFilter
{
    private readonly ChatTabSettings tab;
    private readonly ChatFilterTable? table;
    private readonly HashSet<uint>? chosen;
    private readonly IReadOnlyList<string> keep,hide,hidden;

    public ChatTabFilter(ChatTabSettings tab,ChatFilterTable? table,ChatSettings? settings)
    {
        this.tab=tab;
        if(tab.Filters is { } filters && table is { Rows.Count:>0 }){this.table=table;chosen=filters.ToHashSet();}
        keep=ChatTextFilter.Words(tab.KeepWords);hide=ChatTextFilter.Words(tab.HideWords);hidden=ChatTextFilter.Words(settings?.HiddenPlayers);
    }

    public bool Shows(ChatRecord record)
    {
        if(!ByKind(record))return false;
        if(ChatTextFilter.IsPlayer(record.Sender,hidden))return false;
        return keep.Count==0 && hide.Count==0 || ChatTextFilter.Passes(ChatTextFilter.Text(record),keep,hide,tab.WordsMatchCase);
    }

    private bool ByKind(ChatRecord record)
    {
        if(table is null || chosen is null)return tab.Shows(record.Channel);
        var matched=false;
        foreach(var row in table.Rows)
        {
            if(!ChatFilterTable.Matches(row,record.Kind,record.Source,record.Target))continue;
            if(chosen.Contains(row.Id))return true;
            matched=true;
        }
        return !matched && tab.Unlisted;
    }
}
