using System.Globalization;
using System.Text;

namespace TenshoAssistant.Chat;

/// <remarks>X-505 (0.1488): LinkMark, the game's arrow before a link (part of the link: a click on it opens it).</remarks>
public enum ChatRole { Timestamp,Channel,Sender,Body,Link,LinkMark }
/// <param name="Link">X-403 phase 2 (0.1441): what the run's text links to (role Link); null otherwise.</param>
/// <param name="Underlined">X-510 (review of 0.1490): with the game's look, a link that has no arrow of the game (a web address found by Tensho,
/// a link named by Tensho, a link of Dalamud without arrow): drawn underlined in the accent colour, as every link before 0.1488, so that it is
/// told apart from the text. Without the game's look every link is drawn so already.</param>
public readonly record struct ChatRun(string Text,ChatRole Role,ChatLink? Link=null,bool Underlined=false);

/// <summary>X-403 phase 1 (D-166; back from 0.471): how one message reads, and its lines for a given width. Pure: the width of a text is given by the caller.</summary>
public static class ChatFormat
{
    /// <summary>X-565 (0.1565): whether the next control, this wide, still fits on the current line (the width left on it); a width left not
    /// known (NaN, infinite) counts as not fitting, so the control goes to the next line rather than past the edge.</summary>
    public static bool FitsOnLine(float left,float needed)=>float.IsFinite(left) && float.IsFinite(needed) && needed<=left;

    /// <summary>X-560 (0.1528, the user's report of 17/11: « chat settings still not framed »): how many columns of boxes the width holds, each
    /// as wide as the widest box (its name never cut): at least one, at most one per box; a width not known (0, NaN) gives one.</summary>
    public static int GridColumns(float available,float cell,int count)
    {
        if(count<=1 || !float.IsFinite(available) || !float.IsFinite(cell) || cell<=0)return 1;
        return Math.Clamp((int)MathF.Floor(available/cell),1,count);
    }

    /// <summary>The text without control characters; X-472 (0.1471): each glyph of the game's font (private use area U+E000 to U+F8FF) written
    /// with its readable equivalent (ChatGlyphs: boxed letters as letters, the world icon « @ »…), the link glyph alone left out.</summary>
    public static string Clean(string? text)
    {
        if(string.IsNullOrEmpty(text))return "";
        var clean=new StringBuilder(text.Length);
        foreach(var letter in text)
            if(letter=='\n')clean.Append(letter);
            else if(ChatGlyphs.IsGlyph(letter))clean.Append(ChatGlyphs.Readable(letter));
            else if(!char.IsControl(letter))clean.Append(letter);
        return clean.ToString();
    }

    /// <summary>When a message was said: the game's time stamp (seconds since 1970) when it is within a day of now, else now.</summary>
    public static DateTimeOffset When(long timestamp,DateTimeOffset now)
    {
        if(timestamp<=0)return now;
        var said=DateTimeOffset.FromUnixTimeSeconds(Math.Min(timestamp,253402300799));
        return (said-now).Duration()<=TimeSpan.FromDays(1) ? said:now;
    }

    public static string Time(DateTimeOffset time,ChatSettings settings)
        // X-505 (0.1488): the game writes the hour without a leading zero ([9:21]); with one on request.
        =>time.ToLocalTime().ToString((settings.Clock24 ? (settings.HourLeadingZero ? "HH:mm":"H:mm"):"h:mm")+(settings.ShowSeconds ? ":ss":"")+(settings.Clock24 ? "":" tt"),CultureInfo.InvariantCulture);

    /// <summary>What changes how the lines are written: a change lays them out again. X-482 M1 (0.1472): <paramref name="names"/> says which of the
    /// lists that name a link without text of its own (items, quests, maps) are loaded: a message received before them (at login) kept « [link] »
    /// for good; it is laid out again once they are.</summary>
    public static string LayoutKey(ChatSettings settings,string names="")
        =>$"{settings.ShowTimestamps}{settings.Clock24}{settings.ShowSeconds}{settings.ShowChannel}{settings.BoundedFontScale}{settings.HourLeadingZero}{settings.GameLinks}{settings.WrapIndent}{settings.SenderBrackets}{settings.CaptureNames}|{ChannelNamesKey(settings)}|{names}";

    /// <summary>X-447 B4 (0.1621): the channels' short names chosen, in the layout's key (a name changed lays the lines out again).</summary>
    public static string ChannelNamesKey(ChatSettings settings)
        =>settings.ChannelNames is { Count:>0 } names ? string.Join(",",names.OrderBy(pair=>pair.Key).Select(pair=>$"{(int)pair.Key}={pair.Value}")):"";

    /// <summary>X-447 B5 (0.1621): a sender as written before the text: in brackets when chosen.</summary>
    public static string Sender(string sender,ChatSettings settings)=>settings.SenderBrackets ? $"[{sender}]":sender;

    /// <summary>X-669 M1 (review of 0.1627, R25): the secret the stand-ins are made with: 32 random bytes drawn once per load of the plugin, kept in
    /// memory only (never saved, logged nor shown).</summary>
    private static readonly byte[] CaptureKey=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    // X-669 M1: the stand-ins made so far (the names' colours ask for them each frame); emptied past 2000 names.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string,string> standIns=new(StringComparer.Ordinal);

    /// <summary>X-447 B15 (0.1623): a player's stand-in in capture mode: « # » and five letters or digits taken from the name (its world, case and
    /// spaces aside), the same for the same player; nothing of the name can be read in it. X-669 M1 (review of 0.1627, R25): made with
    /// <see cref="CaptureKey"/>. It was a hash without a key, the same on every PC: whoever saw a picture could check a guessed name by working
    /// out its code, and match a player from one picture to the next, even between two players' pictures. Without the key neither can be done;
    /// the codes change at each start of the plugin.</summary>
    public static string StandIn(string? name)
    {
        var text=name ?? "";
        if(standIns.TryGetValue(text,out var known))return known;
        if(standIns.Count>=2000)standIns.Clear();
        return standIns[text]=StandIn(text,CaptureKey);
    }

    /// <summary>X-669 M1: the stand-in made with a given key: HMAC-SHA256 of the name (world, case and spaces aside), its first 20 bits as five
    /// letters or digits. The tests give their own keys.</summary>
    public static string StandIn(string? name,byte[] key)
    {
        var text=(name ?? "").Trim().ToLowerInvariant();
        var at=text.IndexOf('@');
        if(at>0)text=text[..at].Trim();
        var hash=System.Security.Cryptography.HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(text));
        return "#"+((hash[0]<<12)|(hash[1]<<4)|(hash[2]>>4)).ToString("X5",System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>X-670 M3 (review of 0.1638): a message's sender as the window writes it: cleaned, and in capture mode a player's stand-in (a
    /// character of the story keeps its name). The lines and the search both take it, so the search finds what is shown.</summary>
    public static string ShownSender(ChatRecord record,bool capture)
    {
        var sender=Clean(record.Sender).Trim();
        return capture && sender.Length>0 && record.Channel is not ChatChannel.Npc ? StandIn(sender):sender;
    }

    /// <summary>X-482 M1 (0.1472): the part of the layout key that says which lists of names are loaded.</summary>
    public static string NamesKey(bool items,bool quests,bool maps)=>$"{(items ? 'i':'-')}{(quests ? 'q':'-')}{(maps ? 'm':'-')}";

    /// <summary>X-501 (review of 0.1482): the lines' area of one tab. Drawn outside the tab item since 0.1481 (no tab ID pushed), it is named
    /// after its tab: each tab keeps its own scroll, as when it was drawn inside its tab item. X-504 M2 (review of 0.1486): the number is the
    /// tab's view's own (ChatTabView.Serial), not its place: removing a tab no longer gives the next one the removed tab's scroll.</summary>
    public static string LinesId(int tab)=>"###TenshoChatLines"+tab.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A tab's name, with its unread count when it has one and shows it.</summary>
    public static string TabLabel(string? name,bool showUnread,int unread)
        =>(name is { Length:>0 } ? name:"-")+(showUnread && unread>0 ? $" ({UnreadText(unread)})":"");

    /// <summary>X-466 (the user's request of 15/11): the most unread messages a tab writes as a number; past it, « 100+ » (the count itself
    /// goes on underneath, and the tab's width no longer changes with each message).</summary>
    public const int UnreadShown=100;

    /// <summary>X-466: the unread count as the tab writes it: the number up to <see cref="UnreadShown"/>, then « 100+ ».</summary>
    public static string UnreadText(int unread)=>unread>UnreadShown ? $"{UnreadShown}+":unread.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <param name="shortName">The channel's short name; empty for none.</param>
    /// <param name="linkName">X-461 (0.1463): the name of what a link without visible text points to (an item, a quest, a place); null or empty: « … ».</param>
    public static IReadOnlyList<ChatRun> Runs(ChatRecord record,ChatSettings settings,Func<ChatChannel,string> shortName,Func<ChatLink,string?>? linkName=null)
    {
        var runs=new List<ChatRun>(5);
        // X-476 (0.1481): in brackets, as the game's chat ([22:47]).
        if(settings.ShowTimestamps)runs.Add(new($"[{Time(record.Time,settings)}] ",ChatRole.Timestamp));
        var spoken=ChatChannels.Spoken(record.Channel);
        // X-447 B4 (0.1621): the short name chosen for the channel, else the game's.
        if(settings.ShowChannel && spoken && (settings.ShortNameOf(record.Channel) ?? shortName(record.Channel)) is { Length:>0 } channel)runs.Add(new($"[{channel}] ",ChatRole.Channel));
        // X-510 (review of 0.1490): with the game's look, the links that have the game's arrow (its glyph, or the arrow written for it); the
        // others get the underline (ChatRun.Underlined). Made once per layout, not per frame.
        HashSet<ChatLink>? arrowed=null;
        if(settings.GameLinks)
            foreach(var segment in record.Body)
                if(segment.Link is { } link && ChatLook.Marked(segment.Text))(arrowed??=[]).Add(link);
        bool Underlined(ChatLink target)=>settings.GameLinks && arrowed?.Contains(target)!=true;
        // X-447 B15 (0.1623): capture mode, a player's stand-in (a character of the story keeps its name). X-670 M3: ShownSender, the search's too.
        var sender=ShownSender(record,settings.CaptureNames);
        // A custom emote (kind 28) has its author apart from its text; a standard emote (29) already names them.
        if(record.Kind==28 && sender.Length>0)runs.Add(new($"{sender} ",ChatRole.Sender));
        else if(sender.Length>0 && (spoken || record.Channel is ChatChannel.Npc))
        {
            // X-447 B5 (0.1621): a player's name in brackets when chosen (a character of the game's story keeps its name as it is).
            var named=spoken ? Sender(sender,settings):sender;
            runs.Add(new(record.Channel switch{ChatChannel.TellOut=>$">> {named}: ",ChatChannel.TellIn=>$"{named} >> ",_=>$"{named}: "},ChatRole.Sender));
        }
        foreach(var segment in record.Body)
        {
            // X-461 (0.1463): a link with no text of its own (ChatPieces keeps its place empty): the name of what it points to.
            if(segment.Text.Length==0 && segment.Link is { } named)
            {
                runs.Add(new(Clean(linkName?.Invoke(named)) is { Length:>0 } name ? name:"…",ChatRole.Link,named,Underlined(named)));
                continue;
            }
            // X-505 (0.1488): with the game's look, a link's glyph (where the game draws its arrow) becomes the arrow, apart, then its text.
            if(settings.GameLinks && segment.Link is { } marked && ChatLook.Marked(segment.Text))
            {
                runs.Add(new(ChatGlyphs.LinkArrow,ChatRole.LinkMark,marked));
                if(Clean(ChatLook.Unmarked(segment.Text)) is { Length:>0 } rest)runs.Add(new(rest,ChatRole.Link,marked));
                continue;
            }
            if(Clean(segment.Text) is { Length:>0 } text)runs.Add(segment.Link is { } link ? new(text,ChatRole.Link,link,Underlined(link)):new(text,ChatRole.Body));
        }
        return runs;
    }

    /// <summary>X-403 phase 2 (0.1442): a message as one line of text, as the window writes it (time, channel, sender, text), for the clipboard.</summary>
    public static string Plain(ChatRecord record,ChatSettings settings,Func<ChatChannel,string> shortName,Func<ChatLink,string?>? linkName=null)
        =>string.Concat(Runs(record,settings,shortName,linkName).Select(run=>run.Text)).TrimEnd();

    /// <summary>X-403 phase 2 (0.1442): messages one per line, for the clipboard.</summary>
    public static string Plain(IEnumerable<ChatRecord> records,ChatSettings settings,Func<ChatChannel,string> shortName,Func<ChatLink,string?>? linkName=null)
        =>string.Join("\n",records.Select(record=>Plain(record,settings,shortName,linkName)));

    /// <summary>The lines of a message for a width. Breaks between words; a word wider than the line is cut. Runs of one role (and one link) are joined on a line, to draw as little as possible.</summary>
    /// <param name="indent">X-505 (0.1488): how far the lines after the first are indented (they are that much narrower), as the game's chat.</param>
    public static IReadOnlyList<IReadOnlyList<ChatRun>> Wrap(IEnumerable<ChatRun> runs,float width,Func<string,float> measure,float indent=0)
    {
        var lines=new List<IReadOnlyList<ChatRun>>();
        var line=new List<ChatRun>();
        var used=0f;
        var full=width;
        // The narrower lines after the first keep at least half the width.
        void Flush(){lines.Add(line);line=[];used=0;width=indent>0 && full>0 ? Math.Max(full/2,full-indent):full;}
        void Add(string text,ChatRun run,float size)
        {
            if(line.Count>0 && line[^1].Role==run.Role && Equals(line[^1].Link,run.Link) && line[^1].Underlined==run.Underlined)line[^1]=line[^1] with{Text=line[^1].Text+text};
            else line.Add(run with{Text=text});
            used+=size;
        }
        foreach(var run in runs)
            foreach(var (token,newLine) in Tokens(run.Text))
            {
                if(newLine){Flush();continue;}
                var size=measure(token);
                if(used>0 && used+measure(token.TrimEnd())>width)Flush();
                if(size<=width || width<=0){Add(token,run,size);continue;}
                // A word wider than the line: as many letters as fit, then the next line.
                var part=new StringBuilder();
                foreach(var letter in token)
                {
                    if(part.Length>0 && used+measure(part.ToString()+letter)>width){Add(part.ToString(),run,measure(part.ToString()));part.Clear();Flush();}
                    part.Append(letter);
                }
                if(part.Length>0)Add(part.ToString(),run,measure(part.ToString()));
            }
        if(line.Count>0 || lines.Count==0)lines.Add(line);
        return lines;
    }

    /// <summary>Each word with the spaces that follow it; a line break is its own token.</summary>
    private static IEnumerable<(string Token,bool NewLine)> Tokens(string text)
    {
        var start=0;
        for(var index=0;index<text.Length;index++)
        {
            if(text[index]=='\n')
            {
                if(index>start)yield return (text[start..index],false);
                yield return ("",true);
                start=index+1;
            }
            else if(text[index]==' ' && (index+1==text.Length || text[index+1]!=' '))
            {
                yield return (text[start..(index+1)],false);
                start=index+1;
            }
        }
        if(start<text.Length)yield return (text[start..],false);
    }
}
