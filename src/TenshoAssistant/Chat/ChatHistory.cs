using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-475 (the user's remark of 15/11, asked again on 16/11: the chat has no memory between two loads or game restarts; X-447 B10: kept on disk,
/// off by default, only with the player's consent): the chat history of one character, on this PC only, never sent anywhere.
/// <para>Format 1 (R02.3, settled before the announcement): a text file per character, <c>ChatHistory/&lt;the character's local
/// number&gt;.jsonl</c> in the plugin's folder, UTF-8, one JSON object per line. The first line says what the file is:
/// <c>{"format":"tensho-chat-history","version":1,"character":N}</c>. Each next line is one message: <c>time</c> (milliseconds since 1970,
/// UTC), <c>kind</c> (the game's chat kind), <c>sender</c>, <c>source</c> and <c>target</c> (who does it and to whom, 0 none), <c>body</c>
/// (its pieces: <c>text</c>, and <c>link</c> for a link: <c>type</c> MapLink, ItemLink, QuestLink or WebLink, then territory, map, x, y, place,
/// item, hq, quest, url). A value that is empty or zero is left out.</para>
/// <para>X-514 (review of 0.1492, I1): a link of Dalamud or of a plugin is kept as its text only, never as a link. Its command number is the
/// plugin's for that session only: after a reload or a restart the same (plugin, number) may be another action of that plugin, which a click
/// on an old link would run (the game's chat keeps nothing from one session to the next either). A line written by 0.1492 with such a link
/// (type DalamudLink, plugin, command, data) is read back as its text.</para>
/// <para>A line that cannot be read (a write cut short) is skipped, the others kept. A file of another format, version or character is never
/// read nor written over. The file is trimmed to the bounds (messages and days) when it is read and when it has grown past twice the
/// messages. Pure (tests); the game's side is ChatHistory.Dalamud.cs.</para>
/// <para>X-513 (the user's request of 16/11, choice (a)): each bound may be « No limit » (<see cref="NoLimit"/>): nothing is removed for it.
/// The window's messages are read from the file's end only (a big history is read back at once); a trim goes through the whole file line by
/// line (<see cref="Trim"/>), never held whole, so nothing is lost past what is read back.</para>
/// </summary>
public static class ChatHistory
{
    public const string FolderName="ChatHistory";
    public const string FormatName="tensho-chat-history";
    public const int FormatVersion=1;
    public const int MinimumMessages=100,MaximumMessages=10000,DefaultMessages=2000;
    /// <summary>X-513 (the user's request of 16/11, choice (a)): 30 days by default (7 before; a saved setting keeps its value).</summary>
    public const int MinimumDays=1,MaximumDays=90,DefaultDays=30;
    /// <summary>X-513: « No limit » for the messages or the days (the player's box): nothing is removed for that bound. The window still shows
    /// its own number of lines; what is read back for it is the last <see cref="MaximumMessages"/> of each kind.</summary>
    public const int NoLimit=int.MaxValue;
    /// <summary>The most of a file's end read back for the window at once (X-513: a big history is never read whole at load; the trim goes
    /// through the whole file line by line, so nothing past this is lost).</summary>
    public const long MaximumReadBytes=64L*1024*1024;
    // A hand-edited line is bounded too.
    private const int MaximumText=4096,MaximumPieces=256,MaximumSender=256;
    /// <summary>X-723 (0.1724): a job read back above this is no ClassJob row (the sheet has about 45): not kept.</summary>
    private const uint MaximumJob=255;

    public static int BoundedMessages(int messages)=>Math.Clamp(messages,MinimumMessages,MaximumMessages);
    public static int BoundedDays(int days)=>Math.Clamp(days,MinimumDays,MaximumDays);
    // X-513: a bound as the history uses it: NoLimit kept, any other number bounded.
    private static int Limit(int messages)=>messages==NoLimit ? NoLimit:BoundedMessages(messages);
    private static DateTimeOffset Oldest(DateTimeOffset now,int days)=>days==NoLimit ? DateTimeOffset.MinValue:now-TimeSpan.FromDays(BoundedDays(days));

    /// <summary>X-513: the place a history file takes, as shown to the player: the amount and its unit (KB under 1 MB, MB under 1 GB, GB).</summary>
    public static (double Amount,string Unit) Size(long bytes)
        =>bytes<1024*1024 ? (bytes<=0 ? 0:Math.Max(1,Math.Ceiling(bytes/1024.0)),"KB")
            : bytes<1024L*1024*1024 ? (bytes/1048576.0,"MB"):(bytes/1073741824.0,"GB");
    public static string FileName(ulong character)=>character.ToString(CultureInfo.InvariantCulture)+".jsonl";
    public static string PathOf(string folder,ulong character)=>Path.Combine(folder,FileName(character));

    private sealed class HeaderLine
    {
        [JsonPropertyName("format")] public string? Format{get;set;}
        [JsonPropertyName("version")] public int Version{get;set;}
        [JsonPropertyName("character")] public ulong Character{get;set;}
    }

    private sealed class MessageLine
    {
        [JsonPropertyName("time")] public long Time{get;set;}
        [JsonPropertyName("kind")] public int Kind{get;set;}
        [JsonPropertyName("sender")] public string? Sender{get;set;}
        [JsonPropertyName("source")] public byte Source{get;set;}
        [JsonPropertyName("target")] public byte Target{get;set;}
        // X-723 (0.1724): the sender's job when the line arrived (0, not written: not known).
        [JsonPropertyName("job")] public uint Job{get;set;}
        [JsonPropertyName("body")] public List<PieceLine>? Body{get;set;}
    }

    private sealed class PieceLine
    {
        [JsonPropertyName("text")] public string? Text{get;set;}
        [JsonPropertyName("link")] public LinkLine? Link{get;set;}
    }

    private sealed class LinkLine
    {
        [JsonPropertyName("type")] public string? Type{get;set;}
        [JsonPropertyName("territory")] public uint Territory{get;set;}
        [JsonPropertyName("map")] public uint Map{get;set;}
        [JsonPropertyName("x")] public float X{get;set;}
        [JsonPropertyName("y")] public float Y{get;set;}
        [JsonPropertyName("place")] public string? Place{get;set;}
        [JsonPropertyName("item")] public uint Item{get;set;}
        [JsonPropertyName("hq")] public bool Hq{get;set;}
        [JsonPropertyName("quest")] public uint Quest{get;set;}
        [JsonPropertyName("url")] public string? Url{get;set;}
        [JsonPropertyName("plugin")] public string? Plugin{get;set;}
        [JsonPropertyName("command")] public uint Command{get;set;}
        [JsonPropertyName("data")] public byte[]? Data{get;set;}
    }

    private static readonly JsonSerializerOptions Json=TenshoJson.Own(new()
    {
        DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingDefault,MaxDepth=8,
        // Letters of every language written as they are (the file stays readable); the JSON escapes stay (quotes, line breaks).
        Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
    private static readonly UTF8Encoding Utf8=new(false);

    /// <summary>The file's first line.</summary>
    public static string Header(ulong character)=>JsonSerializer.Serialize(new HeaderLine{Format=FormatName,Version=FormatVersion,Character=character},Json);

    /// <summary>One message as one line (no line break).</summary>
    public static string Encode(ChatRecord record)=>JsonSerializer.Serialize(new MessageLine
    {
        Time=record.Time.ToUnixTimeMilliseconds(),Kind=record.Kind,Sender=Empty(record.Sender),Source=record.Source,Target=record.Target,Job=record.Job,
        // X-514: a link of Dalamud or of a plugin is kept as its text (its command is valid in this session only). X-473 (0.1715): a status
        // link too (its row is not in the format; its name stays).
        // X-473 (0.1720): an action's name too (it is no link of the game).
        Body=record.Body.Select(segment=>new PieceLine{Text=Empty(segment.Text),Link=segment.Link is { Kind:not (ChatSegmentKind.DalamudLink or ChatSegmentKind.StatusLink or ChatSegmentKind.ActionName or ChatSegmentKind.FontIcon) } link ? Link(link):null}).ToList(),
    },Json);

    private static string? Empty(string? text)=>string.IsNullOrEmpty(text) ? null:text;

    private static LinkLine Link(ChatLink link)=>new()
    {
        Type=link.Kind.ToString(),Territory=link.TerritoryId,Map=link.MapId,X=link.X,Y=link.Y,Place=Empty(link.Place),Item=link.ItemId,Hq=link.Hq,
        Quest=link.QuestId,Url=Empty(link.Url),Plugin=Empty(link.Plugin),Command=link.CommandId,Data=link.Data is { Length:>0 } data ? data:null,
    };

    /// <summary>One line read back as a message numbered <paramref name="id"/>; null when it cannot be read (cut short, not a message).</summary>
    public static ChatRecord? Decode(string line,long id)
    {
        if(string.IsNullOrWhiteSpace(line))return null;
        try
        {
            var read=JsonSerializer.Deserialize<MessageLine>(line,Json);
            if(read?.Body is not { } body || read.Time<=0)return null;
            var time=DateTimeOffset.FromUnixTimeMilliseconds(read.Time);
            var pieces=new List<ChatSegment>(Math.Min(body.Count,MaximumPieces));
            foreach(var piece in body.Take(MaximumPieces))
            {
                var text=Cut(piece?.Text,MaximumText);
                var link=piece?.Link is { } stored ? Link(stored):null;
                pieces.Add(link is null ? new ChatSegment(text):new ChatSegment(text,link.Kind,link));
            }
            return new ChatRecord(id,time,read.Kind,ChatChannels.From(read.Kind),Cut(read.Sender,MaximumSender),pieces,read.Source,read.Target,read.Job is >0 and <=MaximumJob ? read.Job:0);
        }
        catch(Exception error) when(error is JsonException or ArgumentOutOfRangeException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string Cut(string? text,int maximum)=>text is null ? "":text.Length<=maximum ? text:text[..maximum];

    // A link of a type this version does not know stays plain text; X-514: so does a link of Dalamud or of a plugin (0.1492 wrote them),
    // so that a click never runs a command number of an older session.
    private static ChatLink? Link(LinkLine link)
        =>Enum.TryParse<ChatSegmentKind>(link.Type,false,out var kind) && kind is not (ChatSegmentKind.Text or ChatSegmentKind.DalamudLink or ChatSegmentKind.StatusLink or ChatSegmentKind.ActionName) && Enum.IsDefined(kind)
            ? new ChatLink(kind,link.Territory,link.Map,link.X,link.Y,Cut(link.Place,MaximumSender),link.Item,link.Hq,link.Quest,Cut(link.Url,MaximumText),
                Cut(link.Plugin,MaximumSender),link.Command,link.Data is { Length:>0 and <=MaximumText } data ? data:null)
            : null;

    /// <summary>What a file holds: its messages within the bounds, the lines read, the lines that could not be read, whether the file is
    /// Tensho's chat history of that character (false: another format, version or character; left as it is), and whether it should be
    /// written again trimmed (messages past the bounds, unreadable lines, its end only read).</summary>
    public sealed record Loaded(IReadOnlyList<ChatRecord> Records,int Lines,int Bad,bool Ours,bool Trim);

    /// <summary>What the first line says: Tensho's history of that character (true), another file (false), or a line that cannot be read
    /// (null: a header cut short; the file is still taken as Tensho's, its next lines are read).</summary>
    public static bool? IsOurs(string? firstLine,ulong character)
    {
        if(string.IsNullOrWhiteSpace(firstLine))return null;
        try
        {
            var header=JsonSerializer.Deserialize<HeaderLine>(firstLine,Json);
            if(header?.Format is null)return null;
            return header.Format==FormatName && header.Version==FormatVersion && header.Character==character;
        }
        catch(JsonException){return null;}
    }

    /// <summary>X-518 M4: this many messages older than the days in a row end the reading from the end (the rest is older still).</summary>
    public const int OldRunEnd=256;
    /// <summary>X-644 M1 (review of 0.1586): the time and the kind of a line as <see cref="Encode"/> writes it (« {"time":…,"kind":…, »),
    /// read without decoding the rest; null when the line does not start so (it is then decoded whole). Once one kind is full, its next
    /// lines are only peeked: with « No limit » on the messages and the days, a kind that rarely comes (few combat lines) no longer makes
    /// every opening decode the whole file back to <see cref="MaximumReadBytes"/>; what the window gets is the same.</summary>
    public static (DateTimeOffset Time,int Kind)? Peek(string line)
    {
        const string timeKey="{\"time\":",kindKey=",\"kind\":";
        if(!line.StartsWith(timeKey,StringComparison.Ordinal))return null;
        var timeEnd=line.IndexOf(',',timeKey.Length);
        if(timeEnd<0 || !long.TryParse(line.AsSpan(timeKey.Length,timeEnd-timeKey.Length),NumberStyles.None,CultureInfo.InvariantCulture,out var milliseconds)
           || milliseconds<=0 || string.CompareOrdinal(line,timeEnd,kindKey,0,kindKey.Length)!=0)return null;
        var kindAt=timeEnd+kindKey.Length;
        var kindEnd=kindAt;
        while(kindEnd<line.Length && char.IsAsciiDigit(line[kindEnd]))kindEnd++;
        if(kindEnd==kindAt || kindEnd>=line.Length || line[kindEnd] is not (',' or '}')
           || !int.TryParse(line.AsSpan(kindAt,kindEnd-kindAt),NumberStyles.None,CultureInfo.InvariantCulture,out var kind))return null;
        try{return (DateTimeOffset.FromUnixTimeMilliseconds(milliseconds),kind);}
        catch(ArgumentOutOfRangeException){return null;}
    }

    /// <summary>Reads a character's file back for the window: the messages within the bounds, numbered from 1, in their order: none older
    /// than <paramref name="days"/> days, and the last <paramref name="messages"/> of the combat lines and the last of the others (as the
    /// window keeps them: a raid's lines never push a conversation out). X-513: read from the file's end only (at most
    /// <see cref="MaximumReadBytes"/>, and no further back than both kinds need), so a big history is read back at once; with
    /// <see cref="NoLimit"/>, the last <see cref="MaximumMessages"/> of each kind. <c>Trim</c> says the file holds more than its bounds (or
    /// lines that cannot be read, or a damaged first line): <see cref="Trim"/> then goes through it whole. No file: nothing, ours.
    /// X-670 M2 (review of 0.1638): this summary stood above OldRunEnd, which has its own.</summary>
    public static Loaded Read(string path,ulong character,DateTimeOffset now,int messages,int days)=>Read(path,character,now,messages,days,MaximumReadBytes);

    /// <summary><see cref="Read(string,ulong,DateTimeOffset,int,int)"/> with the most of the end read (a test makes it small).</summary>
    public static Loaded Read(string path,ulong character,DateTimeOffset now,int messages,int days,long budget)
    {
        if(!File.Exists(path))return new([],0,0,true,false);
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        var first=FirstLine(stream,out var body);
        var ours=IsOurs(first,character);
        if(ours==false)return new([],0,0,false,false);
        var limit=Limit(messages);
        var shown=Math.Min(limit,MaximumMessages);
        var oldest=Oldest(now,days);
        var newest=new List<ChatRecord>();
        int lines=0,bad=0,combat=0,others=0,oldRun=0;
        bool over=false,old=false,full=false;
        var walk=new BackwardLines(stream,body,budget);
        while(walk.Next() is { } line)
        {
            if(line.Length==0)continue;
            lines++;
            // X-644 M1: a line of a kind already full is only peeked (the same steps as below, without decoding its text).
            if((combat>=shown || others>=shown) && Peek(line) is { } peek && (ChatChannels.From(peek.Kind)==ChatChannel.Combat ? combat:others)>=shown)
            {
                if(peek.Time<oldest){old=true;if(++oldRun>=OldRunEnd)break;continue;}
                oldRun=0;over=true;continue;
            }
            if(Decode(line,0) is not { } record){bad++;continue;}
            // X-518 M4 (review of 0.1494): the file is in time order, read from its end: the lines before a run of old ones are older too.
            // A run, not the first one: a single line out of order (a clock set back) never hides the rest.
            if(record.Time<oldest){old=true;if(++oldRun>=OldRunEnd)break;continue;}
            oldRun=0;
            ref var count=ref record.Channel==ChatChannel.Combat ? ref combat:ref others;
            if(count>=shown){over=true;continue;}
            count++;
            newest.Add(record);
            if(combat>=shown && others>=shown){full=true;break;}
        }
        // Lines left before the point reached (both kinds full, or past the budget): past the bound when it is a number; with « No limit » on
        // the messages, kept, unless the days remove the oldest one (the file is in time order: its first message says).
        var before=walk.Cut;
        if(full)
        {
            string? more;
            do more=walk.Next();while(more is { Length:0 });
            before|=more is not null;
        }
        if(before)
        {
            if(limit!=NoLimit)over=true;
            else if(!old && oldest!=DateTimeOffset.MinValue && FirstMessageTime(stream,body) is { } time && time<oldest)old=true;
        }
        newest.Reverse();
        var kept=new List<ChatRecord>(newest.Count);
        foreach(var record in newest)kept.Add(record with{Id=kept.Count+1});
        return new(kept,lines,bad,true,bad>0 || ours is null || old || over && limit!=NoLimit);
    }

    // The first line of a file (at most 64 KB; a header never is that long) and where the next one begins.
    private static string? FirstLine(FileStream stream,out long next)
    {
        stream.Seek(0,SeekOrigin.Begin);
        var buffer=new byte[(int)Math.Min(stream.Length,64*1024)];
        var read=0;
        while(read<buffer.Length && stream.Read(buffer,read,buffer.Length-read) is var got and >0)read+=got;
        var end=Array.IndexOf(buffer,(byte)'\n',0,read);
        next=end<0 ? stream.Length:end+1;
        return read==0 ? null:Utf8.GetString(buffer,0,end<0 ? read:end).TrimEnd('\r');
    }

    // X-513: the time of the file's first message (the line after the first line), or null when it cannot be read.
    private static DateTimeOffset? FirstMessageTime(FileStream stream,long body)
    {
        if(body>=stream.Length)return null;
        stream.Seek(body,SeekOrigin.Begin);
        using var reader=new StreamReader(stream,Utf8,false,4096,leaveOpen:true);
        return reader.ReadLine() is { } line && Decode(line,0) is { } record ? record.Time:null;
    }

    /// <summary>X-513: a file's lines from its end back to <c>start</c> (the line after the first), newest first, in blocks; at most
    /// <c>budget</c> bytes. <see cref="Cut"/>: the budget stopped it before the first line's end (lines are left before).</summary>
    private sealed class BackwardLines(FileStream stream,long start,long budget)
    {
        private readonly byte[] block=new byte[64*1024];
        private readonly long floor=Math.Max(start,stream.Length-budget);
        private long position=stream.Length;
        private int index=-1,end;
        private List<byte> carry=[];
        private bool done;

        /// <summary>The budget is reached before the first line's end: lines are left before what can be given.</summary>
        public bool Cut=>floor>start;

        public string? Next()
        {
            while(true)
            {
                for(;index>=0;index--)
                {
                    if(block[index]!=(byte)'\n')continue;
                    var line=Text(index+1,end);
                    end=index;index--;
                    return line;
                }
                if(index==-1 && end>=0 && position<stream.Length)
                {
                    // The block's start belongs to the line that goes on in the block before.
                    var rest=new List<byte>(end+carry.Count);
                    for(var at=0;at<end;at++)rest.Add(block[at]);
                    rest.AddRange(carry);carry=rest;end=-1;
                }
                if(position<=floor)
                {
                    if(done)return null;
                    done=true;
                    // At the first line's end, what is carried is a whole line; past the budget, a line cut by it (not given).
                    if(floor>start || carry.Count==0)return null;
                    var last=Utf8.GetString(carry.ToArray()).TrimEnd('\r');
                    carry.Clear();
                    return last;
                }
                var size=(int)Math.Min(block.Length,position-floor);
                position-=size;
                stream.Seek(position,SeekOrigin.Begin);
                stream.ReadExactly(block,0,size);
                index=size-1;end=size;
            }
        }

        private string Text(int from,int to)
        {
            string line;
            if(carry.Count==0)line=Utf8.GetString(block,from,to-from);
            else
            {
                var bytes=new byte[to-from+carry.Count];
                Array.Copy(block,from,bytes,0,to-from);
                carry.CopyTo(bytes,to-from);
                carry.Clear();
                line=Utf8.GetString(bytes);
            }
            return line.TrimEnd('\r');
        }
    }

    /// <summary>X-513: writes a character's file again within its bounds, line by line (a file of any size is never held whole): the lines
    /// older than the days, the oldest past the messages of their kind (none with <see cref="NoLimit"/>) and the lines that cannot be read
    /// are left out, the first line made again; through a temporary file (a failure leaves the old file whole). Not this character's
    /// history: left as it is.</summary>
    public static void Trim(string path,ulong character,DateTimeOffset now,int messages,int days)
    {
        TrimCore(path,character,now,messages,days);
    }

    /// <summary>X-836 M3 (0.1840, security review before the alpha): the other characters' files are trimmed when they have not been written for
    /// this long: no client plays them now (a file another client still writes is never written over).</summary>
    public static readonly TimeSpan SweepIdle=TimeSpan.FromDays(1);

    /// <summary>X-836 M3 (0.1840): the files of the characters other than <paramref name="open"/>, idle for <see cref="SweepIdle"/>, brought within
    /// the days kept: a file left with no message is deleted, one with older messages trimmed. Before, a character no longer played kept the other
    /// players' messages past the days said. Nothing with « No limit » days; files of another format or character left as they are.
    /// Returns the files deleted or trimmed.</summary>
    public static int Sweep(string folder,ulong open,DateTimeOffset now,int messages,int days)
    {
        if(days==NoLimit || !Directory.Exists(folder))return 0;
        var done=0;
        foreach(var path in Directory.EnumerateFiles(folder,"*.jsonl"))
        {
            if(!ulong.TryParse(Path.GetFileNameWithoutExtension(path),NumberStyles.None,CultureInfo.InvariantCulture,out var who) || who==0 || who==open)continue;
            try
            {
                if(now.UtcDateTime-File.GetLastWriteTimeUtc(path)<SweepIdle)continue;
                var read=Read(path,who,now,messages,days);
                if(!read.Ours)continue;
                if(read.Records.Count==0){File.Delete(path);done++;}
                else if(read.Trim){Trim(path,who,now,messages,days);done++;}
            }
            catch(Exception error) when(error is IOException or UnauthorizedAccessException or InvalidDataException){}
        }
        return done;
    }

    private static void TrimCore(string path,ulong character,DateTimeOffset now,int messages,int days)
    {
        if(!File.Exists(path) || IsOurs(FirstLineOf(path),character)==false)return;
        var limit=Limit(messages);
        var oldest=Oldest(now,days);
        int combat=0,others=0;
        foreach(var record in Records(path,character))
            if(record is { } kept && kept.Time>=oldest){if(kept.Channel==ChatChannel.Combat)combat++;else others++;}
        int dropCombat=limit==NoLimit ? 0:Math.Max(0,combat-limit),dropOthers=limit==NoLimit ? 0:Math.Max(0,others-limit);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var output=new StreamWriter(new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None),Utf8,65536))
            {
                output.Write(Header(character));output.Write('\n');
                foreach(var record in Records(path,character))
                {
                    if(record is null || record.Time<oldest)continue;
                    if(record.Channel==ChatChannel.Combat){if(dropCombat>0){dropCombat--;continue;}}
                    else if(dropOthers>0){dropOthers--;continue;}
                    output.Write(Encode(record));output.Write('\n');
                }
            }
            AtomicFile.OverwriteWhenFree(temporary,path);
        }
        finally{AtomicFile.DeleteWhenFree(temporary);}
    }

    private static string? FirstLineOf(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        return FirstLine(stream,out _);
    }

    // X-513: a file's messages from its first, one line at a time (null for a line that cannot be read); none when it is another history.
    private static IEnumerable<ChatRecord?> Records(string path,ulong character)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        if(IsOurs(FirstLine(stream,out var body),character)==false)yield break;
        stream.Seek(body,SeekOrigin.Begin);
        using var reader=new StreamReader(stream,Utf8,false,65536,leaveOpen:true);
        while(reader.ReadLine() is { } line)
            if(line.Length>0)yield return Decode(line,0);
    }

    /// <summary>Writes the file again with these messages only, through a temporary file (a failure leaves the old file whole).</summary>
    public static void Rewrite(string path,ulong character,IReadOnlyList<ChatRecord> records)
    {
        var text=new StringBuilder(Header(character)).Append('\n');
        foreach(var record in records)text.Append(Encode(record)).Append('\n');
        AtomicFile.Write(path,Utf8.GetBytes(text.ToString()));
    }

    /// <summary>Adds lines at the end of a character's file (made with its first line when there is none). A line cut short by an earlier
    /// failed write is closed first, so it stays alone (skipped when read) and never spoils the next one. A file that is not this
    /// character's history throws InvalidDataException and is not touched. Returns the file's length before (0: made by this write), for
    /// <see cref="Undo"/>.</summary>
    public static long Append(string path,ulong character,IReadOnlyList<string> lines)
    {
        if(lines.Count==0)return File.Exists(path) ? new FileInfo(path).Length:0;
        if(Path.GetDirectoryName(path) is { Length:>0 } folder)Directory.CreateDirectory(folder);
        using var stream=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read);
        var before=stream.Length;
        var text=new StringBuilder();
        if(stream.Length==0)text.Append(Header(character)).Append('\n');
        else
        {
            string? first;
            using(var head=new StreamReader(stream,Utf8,false,4096,leaveOpen:true))first=head.ReadLine();
            if(IsOurs(first,character)==false)throw new InvalidDataException("The chat history file holds another format or character.");
            stream.Seek(-1,SeekOrigin.End);
            if(stream.ReadByte()!='\n')text.Append('\n');
        }
        foreach(var line in lines)text.Append(line).Append('\n');
        stream.Seek(0,SeekOrigin.End);
        stream.Write(Utf8.GetBytes(text.ToString()));
        stream.Flush();
        return before;
    }

    /// <summary>X-515 M3: takes back a write that the player's « off » or « Erase » overtook: the file cut back to its length before
    /// (<paramref name="before"/>), or deleted when that write made it.</summary>
    public static void Undo(string path,long before)
    {
        if(!File.Exists(path))return;
        if(before<=0){AtomicFile.DeleteWhenFree(path);return;}
        using var stream=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.Read);
        if(stream.Length>before)stream.SetLength(before);
    }

    // X-515 M5: a history file is named by the character's number; a message that names it (an IO error's message holds the whole path)
    // reaches dalamud.log, which players join to their bug reports (R04, R25: a player's data). The number is replaced there.
    private static readonly System.Text.RegularExpressions.Regex FileNumber=new(@"\d+(?=\.jsonl)",System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>X-515 M5: a text with every history file's character number replaced by « &lt;character&gt; ».</summary>
    public static string Scrub(string? text)=>string.IsNullOrEmpty(text) ? "":FileNumber.Replace(text,"<character>");

    /// <summary>X-515 M5: the line written to the log for a failure of the history: the message, the error's kind and its scrubbed message
    /// (never the error itself, whose message and stack name the file).</summary>
    /// X-518 M3 (review of 0.1494): the scrubbed call stack follows (a real bug, a NullReferenceException, says where it is); the stack holds
    /// method and source names, never the character's number, scrubbed all the same.
    public static string Report(Exception error,string message)
        =>$"{message} ({error.GetType().Name}: {Scrub(error.Message)})"+(string.IsNullOrEmpty(error.StackTrace) ? "":"\n"+Scrub(error.StackTrace));

    /// <summary>X-518 M1 (review of 0.1494): the history removes its older messages only while one bound (messages or days) is set.</summary>
    public static bool RemovesOlder(bool messagesUnlimited,bool daysUnlimited)=>!(messagesUnlimited && daysUnlimited);

    /// <summary>Deletes every history file of the folder (every character), and the folder when it is left empty. False when a file is still
    /// there (held by another program).</summary>
    public static bool Erase(string folder)
    {
        if(!Directory.Exists(folder))return true;
        var all=true;
        foreach(var file in Directory.EnumerateFiles(folder,"*.jsonl").Concat(Directory.EnumerateFiles(folder,"*.tmp")).ToList())
            all&=AtomicFile.DeleteWhenFree(file);
        try{if(!Directory.EnumerateFileSystemEntries(folder).Any())Directory.Delete(folder);}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        return all;
    }
}

/// <summary>
/// X-475: the history's writes, in the order asked, where <c>run</c> puts them (off the game's thread: a BackgroundWriter; the tests: at once).
/// The game's thread opens a character's history (read back, trimmed, handed to <c>loaded</c>), keeps each new message (a line made at
/// once, queued), and flushes the queue once a second. A write that fails loses nothing: its lines go back to the head of the queue
/// (bounded) and are written at the next flush, and the failure is said once until a write succeeds. A file that is not this character's
/// history is never written to. Turning the history off (<see cref="Drop"/>) or erasing it (<see cref="Erase"/>) voids what was queued
/// before: work handed to <c>run</c> and not begun does nothing; work under way stops before the next file, and the write it was doing
/// when the player's « off » or « Erase » came is taken back (X-515 M3: the file cut back to its length before), so nothing it was asked
/// to keep is left once off. The messages read back wait in <see cref="Take"/> for the game's thread; « off », « Erase » and a change of
/// character drop them (X-515 M1: an erase is never undone by a read that ended just before it).
/// </summary>
/// <param name="append">The write of a file's lines (<see cref="ChatHistory.Append"/>; a test puts its own in, to act while it writes).</param>
public sealed class ChatHistoryWriter(string folder,Action<string,Action> run,Action<Exception,string> warn,Func<string,ulong,IReadOnlyList<string>,long>? append=null)
{
    private bool swept;
    /// <summary>The most lines waiting (a disk that keeps failing): past it, the oldest go.</summary>
    public const int MaximumPending=2*ChatHistory.MaximumMessages;
    private readonly object gate=new();
    private readonly List<(ulong Character,string Line)> pending=[];
    private readonly Func<string,ulong,IReadOnlyList<string>,long> write=append ?? ChatHistory.Append;
    private ulong character;
    private int generation;
    private int appended;
    private bool failing;
    private int failures;
    private ChatHistory.Loaded? ready;

    public string Folder=>folder;
    /// <summary>The character whose history is open (0: none).</summary>
    public ulong Character{get{lock(gate)return character;}}
    /// <summary>Lines waiting to be written.</summary>
    public int Pending{get{lock(gate)return pending.Count;}}
    /// <summary>Writes that failed since the start.</summary>
    public int Failures=>Volatile.Read(ref failures);
    private long bytes=-1;
    /// <summary>X-513: the place the open character's file takes on disk, measured off the game's thread after each read, write, trim and
    /// erase (-1: no history open).</summary>
    public long Bytes=>Interlocked.Read(ref bytes);

    // X-513: measured where `run` runs, for the open character only.
    private void Measure(ulong who)
    {
        if(who==0 || who!=Character)return;
        var path=ChatHistory.PathOf(folder,who);
        long size;
        try{size=File.Exists(path) ? new FileInfo(path).Length:0;}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return;}
        lock(gate)if(who==character)Interlocked.Exchange(ref bytes,size);
    }

    // X-513: the bounds trimmed on disk at most every twice the messages kept (or the most read back, with « No limit »); never when both
    // bounds are « No limit » (nothing is ever removed then).
    private static int TrimEvery(int messages,int days)
        =>messages==ChatHistory.NoLimit && days==ChatHistory.NoLimit ? int.MaxValue:2*(messages==ChatHistory.NoLimit ? ChatHistory.MaximumMessages:ChatHistory.BoundedMessages(messages));

    /// <summary>Opens a character's history: its file read back within the bounds (trimmed on disk when needed), handed to <paramref name="loaded"/>
    /// where <c>run</c> runs (an empty list when it cannot be read).</summary>
    public void Open(ulong who,DateTimeOffset now,int messages,int days,Action<ChatHistory.Loaded> loaded)
    {
        int at;
        lock(gate){character=who;at=generation;}
        run("The chat history could not be read.",()=>
        {
            var path=ChatHistory.PathOf(folder,who);
            ChatHistory.Loaded read;
            try{read=ChatHistory.Read(path,who,now,messages,days);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException)
            {
                Failed(error,"The chat history could not be read; it is left as it is.");
                loaded(new([],0,0,true,false));
                return;
            }
            bool stale;
            lock(gate)stale=at!=generation;
            if(stale){loaded(new([],0,0,true,false));return;}
            if(!read.Ours)warn(new InvalidDataException("another format or character"),"The chat history file of this character is not one this version reads; it is left as it is and nothing is added to it.");
            else if(read.Trim)
                try{ChatHistory.Trim(path,who,now,messages,days);}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The chat history could not be trimmed; it is kept as it was.");}
            appended=0;
            Measure(who);
            loaded(read);
            // X-836 M3 (0.1840): once per load, the files of the characters no longer played brought within the days kept.
            if(!swept)
            {
                swept=true;
                try{ChatHistory.Sweep(folder,who,now,messages,days);}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The other characters' chat histories could not be trimmed; they are kept as they were.");}
            }
        });
    }

    /// <summary>X-515 M1: opens a character's history; the messages read back wait for <see cref="Take"/>, kept only while nothing voided
    /// them (« off », « Erase », another character opened, the history closed).</summary>
    public void Open(ulong who,DateTimeOffset now,int messages,int days)
    {
        int at;
        lock(gate){ready=null;at=generation;}
        Open(who,now,messages,days,read=>{lock(gate)if(at==generation && character==who)ready=read;});
    }

    /// <summary>X-515 M1: the messages read back, once (game's thread); null when none wait.</summary>
    public ChatHistory.Loaded? Take()
    {
        lock(gate){var read=ready;ready=null;return read;}
    }

    /// <summary>A new message of the open character, queued (game's thread).</summary>
    public void Keep(ChatRecord record)
    {
        var line=ChatHistory.Encode(record);
        lock(gate)
        {
            if(character==0)return;
            pending.Add((character,line));
            if(pending.Count>MaximumPending)pending.RemoveRange(0,pending.Count-MaximumPending);
        }
    }

    /// <summary>Writes what is queued, in order; the file trimmed when it has grown past twice <paramref name="messages"/> since it was read.</summary>
    public void Flush(DateTimeOffset now,int messages,int days)
    {
        List<(ulong Character,string Line)> batch;int at;
        lock(gate)
        {
            if(pending.Count==0)return;
            batch=[..pending];pending.Clear();at=generation;
        }
        run("The chat history could not be written.",()=>
        {
            for(var start=0;start<batch.Count;)
            {
                // X-515 M3: « off » or « Erase » since this work began: nothing more is written.
                lock(gate)if(at!=generation)return;
                var who=batch[start].Character;
                var end=start;
                while(end<batch.Count && batch[end].Character==who)end++;
                var lines=batch.GetRange(start,end-start).Select(entry=>entry.Line).ToList();
                var path=ChatHistory.PathOf(folder,who);
                try
                {
                    var before=write(path,who,lines);
                    bool overtaken;
                    lock(gate)overtaken=at!=generation;
                    if(overtaken)
                    {
                        // X-515 M3: the player's « off » or « Erase » came while these lines were written: taken back.
                        try{ChatHistory.Undo(path,before);}
                        catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The chat history's last lines could not be taken back after it was turned off.");}
                        return;
                    }
                }
                catch(InvalidDataException error)
                {
                    // Not this character's history: never written to; these lines are not kept.
                    Failed(error,"The chat history file of this character is not one this version reads; nothing is added to it.");
                    start=end;continue;
                }
                catch(Exception error) when(error is IOException or UnauthorizedAccessException)
                {
                    Failed(error,"The chat history could not be written; its lines are kept for the next try.");
                    lock(gate)
                    {
                        if(at==generation)
                        {
                            pending.InsertRange(0,batch.Skip(start));
                            if(pending.Count>MaximumPending)pending.RemoveRange(0,pending.Count-MaximumPending);
                        }
                    }
                    return;
                }
                failing=false;
                appended+=lines.Count;
                Measure(who);
                start=end;
            }
            var open=Character;
            if(open!=0 && appended>TrimEvery(messages,days))
            {
                appended=0;
                lock(gate)if(at!=generation)return;
                var path=ChatHistory.PathOf(folder,open);
                try
                {
                    var read=ChatHistory.Read(path,open,now,messages,days);
                    if(read.Ours && read.Trim)ChatHistory.Trim(path,open,now,messages,days);
                }
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The chat history could not be trimmed; it is kept as it was.");}
                Measure(open);
            }
        });
    }

    /// <summary>The history is closed (logout, the window turned off): what is queued is still written, to its character's file.</summary>
    public void Close(DateTimeOffset now,int messages,int days)
    {
        Flush(now,messages,days);
        lock(gate){character=0;ready=null;Interlocked.Exchange(ref bytes,-1);}
    }

    /// <summary>The history turned off: nothing more is written, what was queued is dropped (the files stay until « Erase »).</summary>
    public void Drop()
    {
        lock(gate){pending.Clear();generation++;character=0;ready=null;Interlocked.Exchange(ref bytes,-1);}
    }

    /// <summary>« Erase the history »: what is queued is dropped and every character's file deleted; <paramref name="done"/> says whether
    /// every file went. The history stays open: the next messages are kept again.</summary>
    public void Erase(Action<bool>? done=null)
    {
        lock(gate){pending.Clear();generation++;ready=null;}
        run("The chat history could not be erased.",()=>
        {
            var all=ChatHistory.Erase(folder);
            appended=0;
            Measure(Character);
            done?.Invoke(all);
        });
    }

    private void Failed(Exception error,string message)
    {
        Interlocked.Increment(ref failures);
        if(failing)return;
        failing=true;
        warn(error,message);
    }
}
