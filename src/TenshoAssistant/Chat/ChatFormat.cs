using System.Globalization;
using System.Text;

namespace TenshoAssistant.Chat;

public enum ChatRole { Timestamp,Channel,Sender,Body }
public readonly record struct ChatRun(string Text,ChatRole Role);

/// <summary>CHAT-001: how one message reads, and its lines for a given width. Pure: the width of a text is given by the caller.</summary>
public static class ChatFormat
{
    /// <summary>The text without control characters and the game's own glyphs (private use area), which the plugin's font does not draw.</summary>
    public static string Clean(string? text)
    {
        if(string.IsNullOrEmpty(text))return "";
        var clean=new StringBuilder(text.Length);
        foreach(var letter in text)
            if(letter=='\n')clean.Append(letter);
            else if(!char.IsControl(letter) && letter is not (>='' and <=''))clean.Append(letter);
        return clean.ToString();
    }
    public static string Time(DateTimeOffset time,ChatSettings settings)
        =>time.ToLocalTime().ToString((settings.Clock24 ? "HH:mm":"h:mm")+(settings.ShowSeconds ? ":ss":"")+(settings.Clock24 ? "":" tt"),CultureInfo.InvariantCulture);
    /// <param name="shortName">The channel's short name; empty for none.</param>
    public static IReadOnlyList<ChatRun> Runs(ChatRecord record,ChatSettings settings,Func<ChatChannel,string> shortName)
    {
        var runs=new List<ChatRun>(5);
        if(settings.ShowTimestamps)runs.Add(new($"{Time(record.Time,settings)} ",ChatRole.Timestamp));
        var spoken=ChatChannels.Spoken(record.Channel);
        if(settings.ShowChannel && spoken && shortName(record.Channel) is {Length:>0} channel)runs.Add(new($"[{channel}] ",ChatRole.Channel));
        var sender=Clean(record.Sender).Trim();
        // A custom emote (kind 28) has its author apart from its text; a standard emote (29) already names them.
        if(record.Kind==28 && sender.Length>0)runs.Add(new($"{sender} ",ChatRole.Sender));
        else if(sender.Length>0 && (spoken || record.Channel is ChatChannel.Npc))
            runs.Add(new(record.Channel switch{ChatChannel.TellOut=>$">> {sender}: ",ChatChannel.TellIn=>$"{sender} >> ",_=>$"{sender}: "},ChatRole.Sender));
        foreach(var segment in record.Body)if(Clean(segment.Text) is {Length:>0} text)runs.Add(new(text,ChatRole.Body));
        return runs;
    }
    /// <summary>The lines of a message for a width. Breaks between words; a word wider than the line is cut. Runs of one role are joined on a line, to draw as little as possible.</summary>
    public static IReadOnlyList<IReadOnlyList<ChatRun>> Wrap(IEnumerable<ChatRun> runs,float width,Func<string,float> measure)
    {
        var lines=new List<IReadOnlyList<ChatRun>>();
        var line=new List<ChatRun>();
        var used=0f;
        void Flush(){lines.Add(line);line=[];used=0;}
        void Add(string text,ChatRole role,float size)
        {
            if(line.Count>0 && line[^1].Role==role)line[^1]=new(line[^1].Text+text,role);
            else line.Add(new(text,role));
            used+=size;
        }
        foreach(var run in runs)
            foreach(var (token,newLine) in Tokens(run.Text))
            {
                if(newLine){Flush();continue;}
                var size=measure(token);
                if(used>0 && used+measure(token.TrimEnd())>width)Flush();
                if(size<=width || width<=0){Add(token,run.Role,size);continue;}
                // A word wider than the line: as many letters as fit, then the next line.
                var part=new StringBuilder();
                foreach(var letter in token)
                {
                    if(part.Length>0 && used+measure(part.ToString()+letter)>width){Add(part.ToString(),run.Role,measure(part.ToString()));part.Clear();Flush();}
                    part.Append(letter);
                }
                if(part.Length>0)Add(part.ToString(),run.Role,measure(part.ToString()));
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
