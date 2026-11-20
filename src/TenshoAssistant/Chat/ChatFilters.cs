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
    private IReadOnlyList<((int Kind,bool ToSubject) Genre,IReadOnlyList<ChatFilterRow> Rows)>? genres;

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
    /// <remarks>X-634 (bench X-631, review of 0.1520, M2): made once per table (the table does not change), not at each frame the tree is open.</remarks>
    public IReadOnlyList<((int Kind,bool ToSubject) Genre,IReadOnlyList<ChatFilterRow> Rows)> Genres()
        =>genres??=Rows.Where(row=>IsCombat(row.Category)).GroupBy(row=>Genre(row)!.Value).OrderBy(group=>group.Key.ToSubject).ThenBy(group=>group.Min(row=>row.Order))
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

    /// <summary>X-508 (0.1566): the player's name of a sender, without its world (« Ann Doe@World » gives « Ann Doe »); empty for none.</summary>
    public static string PlayerName(string? sender)
    {
        if(string.IsNullOrWhiteSpace(sender))return "";
        var at=sender.IndexOf('@');
        return (at>0 ? sender[..at]:sender).Trim();
    }

    /// <summary>X-508 (0.1566): the list with one more line, still saved with commas (the values saved before are read line by line). The
    /// entry may hold commas: each piece is a line. Null when nothing is added: empty, already there (capitals ignored), or the list would
    /// go past its maximum length. X-626 M3 (review of 0.1568): with <paramref name="matchCase"/> (a tab's « Match case »), « Foo » is not
    /// the double of « foo ».</summary>
    public static string? Add(string? list,string? entry,int maximum,bool matchCase=false)
    {
        var lines=Words(list).ToList();
        var added=false;
        var comparison=matchCase ? StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;
        foreach(var word in Words(entry))
        {
            if(lines.Any(line=>string.Equals(line,word,comparison)))continue;
            lines.Add(word);added=true;
        }
        if(!added)return null;
        var joined=string.Join(", ",lines);
        return joined.Length<=maximum ? joined:null;
    }

    /// <summary>X-508 (0.1566): the list without its line at this index (an index out of the list leaves it as it is, written again).</summary>
    public static string Remove(string? list,int index)
    {
        var lines=Words(list).ToList();
        if(index>=0 && index<lines.Count)lines.RemoveAt(index);
        return string.Join(", ",lines);
    }

    /// <summary>X-508 (0.1566): the list without this line (capitals ignored).</summary>
    public static string Remove(string? list,string word)
        =>string.Join(", ",Words(list).Where(line=>!string.Equals(line,word.Trim(),StringComparison.OrdinalIgnoreCase)));

    /// <summary>X-626 M3 (review of 0.1568): the list without every line that names this sender, the way <see cref="IsPlayer"/> finds them
    /// (« Ann Doe » and « Ann Doe@World », capitals ignored): what the menu offers to take away is really taken away.</summary>
    public static string RemovePlayer(string? list,string sender)
        =>string.Join(", ",Words(list).Where(line=>!IsPlayer(sender,[line])));
}

/// <summary>X-484 (0.1520, beyond the game): the alert words: a message holding one stands out and marks its tab until seen. Pure (tests).</summary>
public static class ChatAlert
{
    /// <summary>A message (its text, not its sender's name) holds one of the words, capitals ignored. X-554 and X-555 (review of 0.1520):
    /// what you send yourself never alerts, on any channel (<see cref="Sent"/>), and the combat lines are never read for it (DEC-053: shown as
    /// the game shows them, never read nor counted).</summary>
    public static bool Holds(ChatRecord record,IReadOnlyList<string> words)
    {
        if(words.Count==0 || record.Channel==ChatChannel.Combat || Sent(record))return false;
        var text=string.Concat(record.Body.Select(segment=>segment.Text));
        return words.Any(word=>SearchText.Contains(text,word,StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>X-447 B7 (0.1646, after Prat): <see cref="Holds(ChatRecord,IReadOnlyList{string})"/>, or your character's first name said by a
    /// player (<see cref="NameIn"/>; <paramref name="ownName"/> null: not looked for).</summary>
    public static bool Holds(ChatRecord record,IReadOnlyList<string> words,string? ownName)=>Holds(record,words) || NameIn(record,ownName);

    /// <summary>X-447 B7 (0.1646): your first name (<see cref="FirstName"/>) as a whole word, capitals ignored, in a message of a spoken channel
    /// (a player's words: never a loot or system line naming you), never one you sent.</summary>
    public static bool NameIn(ChatRecord record,string? ownName)
    {
        if(FirstName(ownName) is not { } first || !ChatChannels.Spoken(record.Channel) || Sent(record))return false;
        return WholeWord(string.Concat(record.Body.Select(segment=>segment.Text)),first);
    }

    /// <summary>X-447 B7 (0.1646): the first name of a character's name (« Ann » of « Ann Doe »); null for none or a single letter.</summary>
    public static string? FirstName(string? name)
    {
        var trimmed=name?.Trim();
        if(string.IsNullOrEmpty(trimmed))return null;
        var space=trimmed.IndexOf(' ');
        var first=space>0 ? trimmed[..space]:trimmed;
        return first.Length>=2 ? first:null;
    }

    /// <summary>X-447 B7 (0.1646): <paramref name="word"/> in <paramref name="text"/>, capitals ignored, not inside a longer word (« Ann » in
    /// « Ann? » and « hi ann », not in « Annie » nor « Joann »).</summary>
    public static bool WholeWord(string text,string word)
    {
        // Apostrophes folded on both sides, as every search (SearchText): « Y’shtola » said finds « Y'shtola ».
        var folded=SearchText.Fold(text);var wanted=SearchText.Fold(word);
        if(wanted.Length==0)return false;
        for(var at=folded.IndexOf(wanted,0,StringComparison.OrdinalIgnoreCase);at>=0;at=folded.IndexOf(wanted,at+1,StringComparison.OrdinalIgnoreCase))
        {
            var end=at+wanted.Length;
            if((at==0 || !char.IsLetterOrDigit(folded[at-1])) && (end>=folded.Length || !char.IsLetterOrDigit(folded[end])))return true;
        }
        return false;
    }

    /// <summary>Dalamud's XivChatRelationKind.LocalPlayer: the message's subject is you.</summary>
    public const byte You=1;

    /// <summary>X-554: you sent this message: a tell you send, or a message whose subject is you (<see cref="Source"/>).</summary>
    public static bool Sent(ChatRecord record)=>record.Channel==ChatChannel.TellOut || record.Source==You;

    /// <summary>X-554 (review of 0.1520): the subject kept for a message. The game gives none (0) for what is said (the chat history of
    /// 17/11: say, shout and party lines all with 0), so a message spoken (or an emote) whose sender is your own character's name, without
    /// a world (the game writes none for you), is marked as yours.</summary>
    public static byte Source(ChatChannel channel,byte source,string sender,string? ownName)
        =>source==0 && channel<=ChatChannel.Emote && IsOwnName(sender,ownName) ? You:source;

    /// <summary>The sender is your character: its name, once the game's glyphs (party number, icons) are left out, capitals ignored; a
    /// sender with a world (« Name@World ») is someone of another world.</summary>
    public static bool IsOwnName(string sender,string? ownName)
    {
        if(string.IsNullOrWhiteSpace(ownName) || sender.Contains('@'))return false;
        var name=new string(sender.Where(letter=>!ChatGlyphs.IsGlyph(letter)).ToArray()).Trim();
        return string.Equals(name,ownName.Trim(),StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>X-447 B7 (0.1647): the messages with an alert not seen in any of <paramref name="views"/>, a message shown by two tabs counted
    /// once (<paramref name="scratch"/> reused from frame to frame).</summary>
    public static int Unseen(IEnumerable<ChatTabView> views,HashSet<long> scratch)
    {
        scratch.Clear();
        foreach(var view in views)scratch.UnionWith(view.AlertIds);
        return scratch.Count;
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
        if(!ChatCopy.AfterClear(record,tab.ClearedAt))return false;
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
