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
/// (its pieces: <c>text</c>, and <c>link</c> for a link: <c>type</c> MapLink, ItemLink, QuestLink, WebLink or DalamudLink, then territory, map,
/// x, y, place, item, hq, quest, url, plugin, command, data in base64). A value that is empty or zero is left out.</para>
/// <para>A line that cannot be read (a write cut short) is skipped, the others kept. A file of another format, version or character is never
/// read nor written over. The file is trimmed to the bounds (messages and days) when it is read and when it has grown past twice the
/// messages. Pure (tests); the game's side is ChatHistory.Dalamud.cs.</para>
/// </summary>
public static class ChatHistory
{
    public const string FolderName="ChatHistory";
    public const string FormatName="tensho-chat-history";
    public const int FormatVersion=1;
    public const int MinimumMessages=100,MaximumMessages=10000,DefaultMessages=2000;
    public const int MinimumDays=1,MaximumDays=90,DefaultDays=7;
    /// <summary>The most of a file read at once: past it, only its end (a file grows between two trims).</summary>
    public const long MaximumReadBytes=64L*1024*1024;
    // A hand-edited line is bounded too.
    private const int MaximumText=4096,MaximumPieces=256,MaximumSender=256;

    public static int BoundedMessages(int messages)=>Math.Clamp(messages,MinimumMessages,MaximumMessages);
    public static int BoundedDays(int days)=>Math.Clamp(days,MinimumDays,MaximumDays);
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

    private static readonly JsonSerializerOptions Json=new()
    {
        DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingDefault,MaxDepth=8,
        // Letters of every language written as they are (the file stays readable); the JSON escapes stay (quotes, line breaks).
        Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    private static readonly UTF8Encoding Utf8=new(false);

    /// <summary>The file's first line.</summary>
    public static string Header(ulong character)=>JsonSerializer.Serialize(new HeaderLine{Format=FormatName,Version=FormatVersion,Character=character},Json);

    /// <summary>One message as one line (no line break).</summary>
    public static string Encode(ChatRecord record)=>JsonSerializer.Serialize(new MessageLine
    {
        Time=record.Time.ToUnixTimeMilliseconds(),Kind=record.Kind,Sender=Empty(record.Sender),Source=record.Source,Target=record.Target,
        Body=record.Body.Select(segment=>new PieceLine{Text=Empty(segment.Text),Link=segment.Link is { } link ? Link(link):null}).ToList(),
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
            return new ChatRecord(id,time,read.Kind,ChatChannels.From(read.Kind),Cut(read.Sender,MaximumSender),pieces,read.Source,read.Target);
        }
        catch(Exception error) when(error is JsonException or ArgumentOutOfRangeException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string Cut(string? text,int maximum)=>text is null ? "":text.Length<=maximum ? text:text[..maximum];

    // A link of a type this version does not know stays plain text.
    private static ChatLink? Link(LinkLine link)
        =>Enum.TryParse<ChatSegmentKind>(link.Type,false,out var kind) && kind!=ChatSegmentKind.Text && Enum.IsDefined(kind)
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

    /// <summary>Reads a character's file: the messages within the bounds (<see cref="Bound"/>), numbered from 1. No file: nothing, ours.</summary>
    public static Loaded Read(string path,ulong character,DateTimeOffset now,int messages,int days)
    {
        if(!File.Exists(path))return new([],0,0,true,false);
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        string? first;
        using(var head=new StreamReader(stream,Utf8,false,4096,leaveOpen:true))first=head.ReadLine();
        var ours=IsOurs(first,character);
        if(ours==false)return new([],0,0,false,false);
        var cut=stream.Length>MaximumReadBytes;
        stream.Seek(cut ? stream.Length-MaximumReadBytes:0,SeekOrigin.Begin);
        var all=new List<ChatRecord>();
        int lines=0,bad=0;
        using(var reader=new StreamReader(stream,Utf8,false,65536,leaveOpen:true))
        {
            // From the start, the header (or a header cut short) is not a message; from within the file, the line cut by the seek is not one.
            reader.ReadLine();
            while(reader.ReadLine() is { } line)
            {
                if(line.Length==0)continue;
                lines++;
                if(Decode(line,all.Count+1) is { } record)all.Add(record);else bad++;
            }
        }
        var kept=Bound(all,now,messages,days);
        return new(kept,lines,bad,true,cut || bad>0 || kept.Count<all.Count || ours is null);
    }

    /// <summary>The messages within the bounds, in their order, numbered from 1: none older than <paramref name="days"/> days, and the last
    /// <paramref name="messages"/> of the combat lines and the last <paramref name="messages"/> of the others (as the window keeps them: a
    /// raid's lines never push a conversation out).</summary>
    public static IReadOnlyList<ChatRecord> Bound(IReadOnlyList<ChatRecord> records,DateTimeOffset now,int messages,int days)
    {
        var limit=BoundedMessages(messages);
        var oldest=now-TimeSpan.FromDays(BoundedDays(days));
        var recent=records.Where(record=>record.Time>=oldest).ToList();
        int combat=recent.Count(record=>record.Channel==ChatChannel.Combat),others=recent.Count-combat;
        int dropCombat=Math.Max(0,combat-limit),dropOthers=Math.Max(0,others-limit);
        var kept=new List<ChatRecord>(recent.Count-dropCombat-dropOthers);
        foreach(var record in recent)
        {
            if(record.Channel==ChatChannel.Combat){if(dropCombat>0){dropCombat--;continue;}}
            else if(dropOthers>0){dropOthers--;continue;}
            kept.Add(record with{Id=kept.Count+1});
        }
        return kept;
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
    /// character's history throws InvalidDataException and is not touched.</summary>
    public static void Append(string path,ulong character,IReadOnlyList<string> lines)
    {
        if(lines.Count==0)return;
        if(Path.GetDirectoryName(path) is { Length:>0 } folder)Directory.CreateDirectory(folder);
        using var stream=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read);
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
    }

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
/// before, even work already handed to <c>run</c>.
/// </summary>
public sealed class ChatHistoryWriter(string folder,Action<string,Action> run,Action<Exception,string> warn)
{
    /// <summary>The most lines waiting (a disk that keeps failing): past it, the oldest go.</summary>
    public const int MaximumPending=2*ChatHistory.MaximumMessages;
    private readonly object gate=new();
    private readonly List<(ulong Character,string Line)> pending=[];
    private ulong character;
    private int generation;
    private int appended;
    private bool failing;
    private int failures;

    public string Folder=>folder;
    /// <summary>The character whose history is open (0: none).</summary>
    public ulong Character{get{lock(gate)return character;}}
    /// <summary>Lines waiting to be written.</summary>
    public int Pending{get{lock(gate)return pending.Count;}}
    /// <summary>Writes that failed since the start.</summary>
    public int Failures=>Volatile.Read(ref failures);

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
                try{ChatHistory.Rewrite(path,who,read.Records);}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The chat history could not be trimmed; it is kept as it was.");}
            appended=0;
            loaded(read);
        });
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
            lock(gate)if(at!=generation)return;
            for(var start=0;start<batch.Count;)
            {
                var who=batch[start].Character;
                var end=start;
                while(end<batch.Count && batch[end].Character==who)end++;
                var lines=batch.GetRange(start,end-start).Select(entry=>entry.Line).ToList();
                try{ChatHistory.Append(ChatHistory.PathOf(folder,who),who,lines);}
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
                start=end;
            }
            var open=Character;
            if(open!=0 && appended>2*ChatHistory.BoundedMessages(messages))
            {
                appended=0;
                var path=ChatHistory.PathOf(folder,open);
                try
                {
                    var read=ChatHistory.Read(path,open,now,messages,days);
                    if(read.Ours && read.Trim)ChatHistory.Rewrite(path,open,read.Records);
                }
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){Failed(error,"The chat history could not be trimmed; it is kept as it was.");}
            }
        });
    }

    /// <summary>The history is closed (logout, the window turned off): what is queued is still written, to its character's file.</summary>
    public void Close(DateTimeOffset now,int messages,int days)
    {
        Flush(now,messages,days);
        lock(gate)character=0;
    }

    /// <summary>The history turned off: nothing more is written, what was queued is dropped (the files stay until « Erase »).</summary>
    public void Drop()
    {
        lock(gate){pending.Clear();generation++;character=0;}
    }

    /// <summary>« Erase the history »: what is queued is dropped and every character's file deleted; <paramref name="done"/> says whether
    /// every file went. The history stays open: the next messages are kept again.</summary>
    public void Erase(Action<bool>? done=null)
    {
        lock(gate){pending.Clear();generation++;}
        run("The chat history could not be erased.",()=>
        {
            var all=ChatHistory.Erase(folder);
            appended=0;
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
