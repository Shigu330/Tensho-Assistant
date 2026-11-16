using System.Globalization;
using System.Text;

namespace TenshoAssistant.Chat;

/// <remarks>X-505 (0.1488): LinkMark, the game's arrow before a link (part of the link: a click on it opens it).</remarks>
public enum ChatRole { Timestamp,Channel,Sender,Body,Link,LinkMark }
/// <param name="Link">X-403 phase 2 (0.1441): what the run's text links to (role Link); null otherwise.</param>
public readonly record struct ChatRun(string Text,ChatRole Role,ChatLink? Link=null);

/// <summary>X-403 phase 1 (D-166; back from 0.471): how one message reads, and its lines for a given width. Pure: the width of a text is given by the caller.</summary>
public static class ChatFormat
{
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
        =>$"{settings.ShowTimestamps}{settings.Clock24}{settings.ShowSeconds}{settings.ShowChannel}{settings.BoundedFontScale}{settings.HourLeadingZero}{settings.GameLinks}{settings.WrapIndent}|{names}";

    /// <summary>X-482 M1 (0.1472): the part of the layout key that says which lists of names are loaded.</summary>
    public static string NamesKey(bool items,bool quests,bool maps)=>$"{(items ? 'i':'-')}{(quests ? 'q':'-')}{(maps ? 'm':'-')}";

    /// <summary>X-501 (review of 0.1482): the lines' area of one tab. Drawn outside the tab item since 0.1481 (no tab ID pushed), it is named
    /// after its tab: each tab keeps its own scroll, as when it was drawn inside its tab item.</summary>
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
        if(settings.ShowChannel && spoken && shortName(record.Channel) is { Length:>0 } channel)runs.Add(new($"[{channel}] ",ChatRole.Channel));
        var sender=Clean(record.Sender).Trim();
        // A custom emote (kind 28) has its author apart from its text; a standard emote (29) already names them.
        if(record.Kind==28 && sender.Length>0)runs.Add(new($"{sender} ",ChatRole.Sender));
        else if(sender.Length>0 && (spoken || record.Channel is ChatChannel.Npc))
            runs.Add(new(record.Channel switch{ChatChannel.TellOut=>$">> {sender}: ",ChatChannel.TellIn=>$"{sender} >> ",_=>$"{sender}: "},ChatRole.Sender));
        foreach(var segment in record.Body)
        {
            // X-461 (0.1463): a link with no text of its own (ChatPieces keeps its place empty): the name of what it points to.
            if(segment.Text.Length==0 && segment.Link is { } named)
            {
                runs.Add(new(Clean(linkName?.Invoke(named)) is { Length:>0 } name ? name:"…",ChatRole.Link,named));
                continue;
            }
            // X-505 (0.1488): with the game's look, a link's glyph (where the game draws its arrow) becomes the arrow, apart, then its text.
            if(settings.GameLinks && segment.Link is { } marked && ChatLook.Marked(segment.Text))
            {
                runs.Add(new(ChatGlyphs.LinkArrow,ChatRole.LinkMark,marked));
                if(Clean(ChatLook.Unmarked(segment.Text)) is { Length:>0 } rest)runs.Add(new(rest,ChatRole.Link,marked));
                continue;
            }
            if(Clean(segment.Text) is { Length:>0 } text)runs.Add(segment.Link is { } link ? new(text,ChatRole.Link,link):new(text,ChatRole.Body));
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
        void Add(string text,ChatRole role,ChatLink? link,float size)
        {
            if(line.Count>0 && line[^1].Role==role && Equals(line[^1].Link,link))line[^1]=new(line[^1].Text+text,role,link);
            else line.Add(new(text,role,link));
            used+=size;
        }
        foreach(var run in runs)
            foreach(var (token,newLine) in Tokens(run.Text))
            {
                if(newLine){Flush();continue;}
                var size=measure(token);
                if(used>0 && used+measure(token.TrimEnd())>width)Flush();
                if(size<=width || width<=0){Add(token,run.Role,run.Link,size);continue;}
                // A word wider than the line: as many letters as fit, then the next line.
                var part=new StringBuilder();
                foreach(var letter in token)
                {
                    if(part.Length>0 && used+measure(part.ToString()+letter)>width){Add(part.ToString(),run.Role,run.Link,measure(part.ToString()));part.Clear();Flush();}
                    part.Append(letter);
                }
                if(part.Length>0)Add(part.ToString(),run.Role,run.Link,measure(part.ToString()));
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
