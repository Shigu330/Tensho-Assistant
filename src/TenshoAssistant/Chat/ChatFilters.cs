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
