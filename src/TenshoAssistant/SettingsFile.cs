using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace TenshoAssistant;

/// <summary>
/// X-685 part 2 (0.1745, D-206 a, the user's choice of 21/11): how Tensho's settings are turned into text and back, with System.Text.Json and
/// options of Tensho's own (TenshoJson), no longer by Dalamud (Newtonsoft with type names: Newtonsoft and TypeDescriptor, shared by the whole
/// game process, kept the types of every copy of Tensho for good, and so each unloaded copy; docs/evidence/0.1741-unload-json.md). The shape
/// stays Newtonsoft's: public fields written too (Vector4), objects and collections already made filled in place as Newtonsoft's « Auto »
/// does (IconSizes relies on it: a default kept for an icon absent from the file), names read whatever their case, NaN and infinities
/// written as text. Pure (tests; the desktop app reads the file through it, by reflection).
/// </summary>
public static class SettingsJson
{
    /// <summary>The depth of JSON read (Configuration is far less deep).</summary>
    public const int MaxDepth=64;

    public static readonly JsonSerializerOptions Options=TenshoJson.Own(new JsonSerializerOptions
    {
        IncludeFields=true,
        PreferredObjectCreationHandling=JsonObjectCreationHandling.Populate,
        PropertyNameCaseInsensitive=true,
        NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals,
        ReadCommentHandling=JsonCommentHandling.Skip,
        AllowTrailingCommas=true,
        MaxDepth=MaxDepth,
        WriteIndented=true,
        Encoder=JavaScriptEncoder.Create(UnicodeRanges.All),
    });

    private static readonly JsonDocumentOptions Document=new(){CommentHandling=JsonCommentHandling.Skip,AllowTrailingCommas=true,MaxDepth=MaxDepth};

    public static string Write<T>(T value)=>JsonSerializer.Serialize(value,Options);
    public static T? Read<T>(string text)=>JsonSerializer.Deserialize<T>(text,Options);
    /// <summary>For the desktop app, which has the plugin's type by reflection only.</summary>
    public static object? Read(string text,Type type)=>JsonSerializer.Deserialize(text,type,Options);

    /// <summary>The text as a JSON object (JsonException when it is not one).</summary>
    public static JsonObject Node(string text)=>JsonNode.Parse(text,documentOptions:Document) as JsonObject ?? throw new JsonException("the settings are not a JSON object");
    public static string Text(JsonObject node)=>node.ToJsonString(Options);

    /// <summary>
    /// Dalamud's file (pluginConfigs\TenshoAssistant.json, Newtonsoft with TypeNameHandling.Objects): the « $type » names Newtonsoft writes at
    /// the head of every object, dictionaries included, are taken out at any depth, then the rest is read as Tensho's own file.
    /// </summary>
    public static T? ReadLegacy<T>(string text)=>WithoutTypeNames(text).Deserialize<T>(Options);
    /// <summary>For the desktop app.</summary>
    public static object? ReadLegacy(string text,Type type)=>WithoutTypeNames(text).Deserialize(type,Options);

    public static JsonObject WithoutTypeNames(string text)
    {
        var root=Node(text);
        var stack=new Stack<JsonNode>();
        stack.Push(root);
        while(stack.Count>0)
        {
            switch(stack.Pop())
            {
                case JsonObject node:
                    node.Remove("$type");
                    foreach(var (_,child) in node)if(child is not null)stack.Push(child);
                    break;
                case JsonArray list:
                    foreach(var child in list)if(child is not null)stack.Push(child);
                    break;
            }
        }
        return root;
    }

    /// <summary>
    /// The text to write when the file changed since this instance last read or wrote it (another game client's Tensho, a newer Tensho's
    /// values): three-way, property by property at any depth. <paramref name="before"/> is this instance's own settings as it last read or
    /// wrote them, <paramref name="mine"/> as they are now, <paramref name="theirs"/> the file as it is now. A value this instance did not
    /// change is taken from the file (another's change kept, a name it does not know kept, a key another removed stays removed); a value it
    /// changed is its own, two objects both changed are merged the same way inside; a name it removed (it had it, it no longer has it) stays
    /// removed. Arrays and plain values are taken whole. Mine's order first, then the file's other names.
    /// </summary>
    public static JsonObject Merge(JsonObject? before,JsonObject mine,JsonObject theirs)=>Merge(before,mine,theirs,0);

    private static JsonObject Merge(JsonObject? before,JsonObject mine,JsonObject theirs,int depth)
    {
        var result=new JsonObject();
        foreach(var (name,value) in mine)
        {
            JsonNode? old=null;
            var had=before is not null && before.TryGetPropertyValue(name,out old);
            var there=theirs.TryGetPropertyValue(name,out var other);
            if(had && JsonNode.DeepEquals(old,value))
            {
                if(there)result[name]=other?.DeepClone();
                continue;
            }
            result[name]=value is JsonObject own && other is JsonObject file && depth<MaxDepth ? Merge(old as JsonObject,own,file,depth+1) : value?.DeepClone();
        }
        foreach(var (name,other) in theirs)
        {
            if(mine.ContainsKey(name))continue;
            if(before is not null && before.ContainsKey(name))continue;
            result[name]=other?.DeepClone();
        }
        return result;
    }
}

/// <summary>
/// Where the settings came from when Tensho loaded. Held and HeldBackup (X-761): the file was there but another program kept it from being
/// opened; new settings, or its backup's, for this session, which writes nothing to the file (SettingsFile.Unseen). Neither is a new install.
/// LegacyAfterUnreadable and NewAfterUnreadable (X-763 M1): Settings.json was there but could not be read (kept aside: SettingsFile.KeptAside),
/// nor its backup; Dalamud's older file, or new settings. Neither is the first move nor a new install.
/// </summary>
public enum SettingsOrigin { New, Own, Backup, Legacy, Held, HeldBackup, LegacyAfterUnreadable, NewAfterUnreadable }

/// <summary>
/// X-685 part 2 (0.1745, D-206 a): Tensho's settings file of its own, pluginConfigs\TenshoAssistant\Settings.json (before: Dalamud's
/// pluginConfigs\TenshoAssistant.json, read once at the first load and then left as it was, so an older Tensho still finds its settings).
/// Written whole (AtomicFile: a crash or a full disk never leaves half a file), the version before kept as « .bak », under a fair lock shared
/// by the game clients of the PC (FairFileLock), merged with the file as it is (SettingsJson.Merge): two game clients saving their own
/// changes keep each other's, and names this version does not know are kept. A write that fails changes nothing on disk and is said once;
/// the settings stay in memory and are written at the next save. A file that cannot be read is kept aside (« Settings.unreadable-… »),
/// never written over unseen; then the backup, then Dalamud's file, then new settings. X-761 (review of 0.1746, I1): a file there but held
/// by another program past the attempts at load is never written by that session (Unseen), which shows its backup's settings, else new ones.
/// </summary>
public sealed class SettingsFile<T>(string path,string? legacyPath,Action<Exception?,string> warn) where T:class,new()
{
    public const string FileName="Settings.json";
    /// <summary>The wait for another game client's write (a write takes milliseconds).</summary>
    public const int LockWaitMs=10_000;
    /// <summary>
    /// X-761 (review of 0.1746, I1): the attempts to open the file at load, about 1.8 s in all (0.75 s at a save): a scanner or a backup tool
    /// holding it a moment does not cost the session its settings.
    /// </summary>
    public const int LoadAttempts=9;
    /// <summary>
    /// X-763 M2 (review of 0.1746): a save on the game's thread (the saver's fallback) waits this long at most for another game client's lock
    /// (Dalamud's save cost 64 to 71 ms there), and reads the file with one retry; refused past it, said once, written at the next save.
    /// </summary>
    public const int GameThreadLockWaitMs=100;
    public const int GameThreadReadAttempts=2;
    private const int ReadAttempts=6;

    private readonly object gate=new();
    private JsonObject? before;
    private bool failing;
    // X-767 M1: set by Close without the gate (volatile), read under it.
    private volatile bool closing;
    private bool? closingSave;
    private bool unseen;
    private bool unseenSaid;
    private bool freeSaid;

    /// <summary>
    /// X-761: true when the file was there at load but held by another program past the attempts. This session never read it, so it never
    /// writes it: a save merged over a file it never read would put this session's lists (alarms, bookmarks, tasks: taken whole) over the
    /// saved ones, and the next save the backup over the one before. Its changes stay in memory; Tensho reads the file when it loads again.
    /// </summary>
    public bool Unseen{get{lock(gate)return unseen;}}

    /// <summary>X-761: called once, off the lock, when a session that never read the file finds it readable again at a save.</summary>
    public Action? FreeAgain{get;set;}

    /// <summary>X-763 M1: the name of the copy of an unreadable settings file kept aside (« Settings.unreadable-….json »); null when none.</summary>
    public string? KeptAside{get;private set;}

    /// <summary>
    /// X-765 M4 (review of 0.1751): the name of the copy of an unreadable backup (Settings.json.bak) kept aside at load
    /// (« Settings.backup-unreadable-….json »), before the next save writes the backup over it; null when none. A backup cut short is often
    /// still mended by hand: it was lost at the next save before.
    /// </summary>
    public string? BackupKeptAside{get;private set;}

    /// <summary>
    /// X-764 M1 (review of 0.1751): the unload begins. Every save from now on is the last one: it waits for another client's lock and reads
    /// the file as long as a save off the game's thread does (LockWaitMs, not the game's tenth of a second: no later save will try again),
    /// and its failure is said every time, as the loss it is (not « written at the next save »), even when a failure was said before.
    /// X-767 M1 (review of 0.1756): without the gate, which a write under way holds while it waits for another client's lock (up to
    /// LockWaitMs): the game's thread is never held here, so the saver's Flush keeps its bound of 5 s and its log line at unload. A write
    /// already under way ends as it began (not the last one: the saver's Flush hands the live settings to the next).
    /// </summary>
    public void Close()=>closing=true;

    /// <summary>X-764 M1: the last save asked after <see cref="Close"/>: true written, false failed (said), null none asked.</summary>
    public bool? ClosingSave{get{lock(gate)return closingSave;}}

    /// <summary>X-764 M1: true while the last write failed and none succeeded since: the file is older than the settings in memory.</summary>
    public bool Failing{get{lock(gate)return failing;}}

    /// <summary>The settings file in the plugin's folder (pluginConfigs\TenshoAssistant).</summary>
    public static string In(string folder)=>Path.Combine(folder,FileName);

    public string FilePath=>path;
    public string BackupPath=>path+".bak";
    public string LockPath=>path+".lock";

    /// <summary>The settings: this file, else its backup, else Dalamud's (once: then written here), else new ones.</summary>
    public T Load(out SettingsOrigin origin)
    {
        lock(gate)
        {
            string? own;
            try{own=ReadText(path,LoadAttempts);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException)
            {
                // X-761 (review of 0.1746, I1): held by another program past the attempts. The backup's settings, else new ones, for this
                // session, which writes nothing to the file until Tensho loads again (Unseen). Not a new install: no guide.
                unseen=true;
                T? copy=null;
                try{if(ReadText(BackupPath) is { } copyText)copy=Parse(copyText,legacy:false);}
                catch(Exception other) when(other is IOException or UnauthorizedAccessException){}
                origin=copy is null ? SettingsOrigin.Held : SettingsOrigin.HeldBackup;
                warn(error,copy is null
                    ? "The settings file could not be opened (held by another program); new settings are used for this session, and nothing is written to the file until Tensho loads again."
                    : "The settings file could not be opened (held by another program); its backup is used for this session, and nothing is written to the file until Tensho loads again.");
                var held=copy ?? new T();
                Remember(held);
                return held;
            }
            if(own is not null)
            {
                if(Parse(own,legacy:false) is { } value){origin=SettingsOrigin.Own;Remember(value);return value;}
                KeepAside(path,own);
            }
            // X-763 M1 (review of 0.1746): Settings.json was there and could not be read: what is read below is a step back, said as such.
            var unreadable=own is not null;
            var backup=ReadText(BackupPath);
            var saved=backup is null ? null:Parse(backup,legacy:false);
            // X-765 M4 (review of 0.1751): an unreadable backup is kept aside under a name of its own (the next save copies the file over it).
            if(backup is not null && saved is null)KeepAside(BackupPath,backup,isBackup:true);
            if(saved is not null)
            {
                warn(null,"The settings file could not be read; its backup was read (the file kept aside, the backup written in its place).");
                origin=SettingsOrigin.Backup;
                Remember(saved);
                Write(SettingsJson.Write(saved));
                return saved;
            }
            if(legacyPath is not null && ReadText(legacyPath) is { } old)
            {
                if(Parse(old,legacy:true) is { } moved)
                {
                    // X-763 M1: after Tensho's own file and its backup could not be read, Dalamud's file is a step back (it may be weeks
                    // old): said so, never as the first move.
                    origin=unreadable ? SettingsOrigin.LegacyAfterUnreadable : SettingsOrigin.Legacy;
                    if(unreadable)warn(null,"The settings file and its backup could not be read; Dalamud's older settings file of Tensho was read instead (the settings may be older than the last ones saved).");
                    Remember(moved);
                    Write(SettingsJson.Write(moved));
                    return moved;
                }
                warn(null,"Dalamud's settings file of Tensho could not be read; new settings are used (that file is left as it is).");
            }
            origin=unreadable ? SettingsOrigin.NewAfterUnreadable : SettingsOrigin.New;
            if(unreadable)warn(null,"The settings file could not be read, nor any copy of it; new settings are used.");
            var fresh=new T();
            Remember(fresh);
            return fresh;
        }
    }

    /// <summary>
    /// Writes the settings (merged with the file as it is now); false when it failed (said once, the next save tries again). X-763 M2:
    /// <paramref name="onGameThread"/>: a tenth of a second at most for another client's lock (GameThreadLockWaitMs), never 10 s.
    /// </summary>
    public bool Save(T live,bool onGameThread=false)
    {
        // X-761: a session that never read the file writes nothing to it; said once, and once more when the file can be read again.
        bool unread,freeAgain=false;
        lock(gate)
        {
            unread=unseen;
            if(unread)
            {
                if(!unseenSaid){unseenSaid=true;warn(null,"The settings are not written: the settings file was held by another program when Tensho loaded and this session never read it; its changes are kept in memory only.");}
                if(!freeSaid && Readable(path)){freeSaid=freeAgain=true;warn(null,"The settings file can be read again; Tensho reads it when it loads again (nothing of this session is written to it).");}
            }
        }
        if(unread)
        {
            if(freeAgain)FreeAgain?.Invoke();
            return false;
        }
        string text;
        try{text=SettingsJson.Write(live);}
        catch(Exception error) when(error is JsonException or NotSupportedException or InvalidOperationException)
        {
            lock(gate)
            {
                if(closing){closingSave=false;warn(error,ClosingLost);}
                else Fail(error,"The settings could not be turned into text; nothing was written.");
            }
            return false;
        }
        lock(gate)
        {
            // X-764 M1: the unload's save waits as long as the worker's, whatever thread asks it, and says its failure.
            if(!closing)return onGameThread ? Write(text,GameThreadLockWaitMs,GameThreadReadAttempts) : Write(text);
            var written=Write(text,LockWaitMs,ReadAttempts,atClose:true);
            closingSave=written;
            return written;
        }
    }

    /// <summary>X-764 M1: said at each failed save after <see cref="Close"/>.</summary>
    public const string ClosingLost="The last settings could not be written at unload (the settings file was held by another program or could not be written); the changes since the last save written are lost.";

    private void Remember(T value)
    {
        try{before=SettingsJson.Node(SettingsJson.Write(value));}
        catch(Exception error) when(error is JsonException or NotSupportedException or InvalidOperationException){before=null;}
    }

    // Under gate.
    private bool Write(string text,int lockWaitMs=LockWaitMs,int readAttempts=ReadAttempts,bool atClose=false)
    {
        try
        {
            var mine=SettingsJson.Node(text);
            using(FairFileLock.Hold(LockPath,lockWaitMs,error=>new IOException("The settings file's lock is held by another program.",error)))
            {
                JsonObject? theirs=null;
                if(ReadText(path,readAttempts) is { } now)
                {
                    try{theirs=SettingsJson.Node(now);}
                    catch(JsonException){KeepAside(path,now);}
                }
                var merged=theirs is null ? mine : SettingsJson.Merge(before,mine,theirs);
                if(theirs is not null)
                {
                    try{File.Copy(path,BackupPath,true);}
                    catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
                }
                AtomicFile.Write(path,SettingsJson.Text(merged));
            }
            before=mine;
            if(failing){failing=false;warn(null,"The settings are written again.");}
            return true;
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException)
        {
            if(atClose){failing=true;warn(error,ClosingLost);}
            else Fail(error,"The settings could not be written; they stay in memory and are written at the next save.");
            return false;
        }
    }

    private void Fail(Exception error,string message)
    {
        if(failing)return;
        failing=true;
        warn(error,message);
    }

    private T? Parse(string text,bool legacy)
    {
        try{return legacy ? SettingsJson.ReadLegacy<T>(text) : SettingsJson.Read<T>(text);}
        catch(Exception error) when(error is JsonException or NotSupportedException or InvalidOperationException or FormatException or OverflowException)
        {
            warn(error,legacy ? "Dalamud's settings file of Tensho could not be read." : "A settings file could not be read.");
            return null;
        }
    }

    // X-761: one attempt, so that a save is never held up by a file still held; false while it cannot be opened, or is absent.
    private static bool Readable(string file)
    {
        try{return ReadText(file,1) is not null;}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){return false;}
    }

    // The whole text, read while another program may write it (a write moves a whole new file in place); null when absent. Tried again a
    // few times while a scanner holds it an instant; still held: an IOException (never taken for an absent file).
    private static string? ReadText(string file,int attempts=ReadAttempts)
    {
        for(var attempt=1;;attempt++)
        {
            try
            {
                if(!File.Exists(file))return null;
                using var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                using var reader=new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch(FileNotFoundException){return null;}
            catch(DirectoryNotFoundException){return null;}
            catch(Exception error) when(attempt<attempts && error is IOException or UnauthorizedAccessException){Thread.Sleep(50*attempt);}
        }
    }

    // A file that cannot be read is copied aside once (each text once), never written over unseen.
    private readonly HashSet<string> keptAside=new(StringComparer.Ordinal);
    private void KeepAside(string file,string text,bool isBackup=false)
    {
        if(!keptAside.Add(text))return;
        try
        {
            // X-765 M4: the backup's copy has a name of its own (the file's copy of the same second is not taken for it).
            var aside=Path.Combine(Path.GetDirectoryName(file)!,Path.GetFileNameWithoutExtension(FileName)+(isBackup ? ".backup-unreadable-":".unreadable-")+DateTime.Now.ToString("yyyyMMdd-HHmmss",System.Globalization.CultureInfo.InvariantCulture)+".json");
            if(!File.Exists(aside))File.Copy(file,aside);
            if(isBackup)BackupKeptAside=Path.GetFileName(aside);else KeptAside=Path.GetFileName(aside);
            warn(null,"A settings file could not be read; it is kept aside as "+Path.GetFileName(aside)+".");
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){warn(error,"A settings file could not be read, nor kept aside.");}
    }
}
