using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Link;

// X-119 step 2, part 1 (0.1081): the local link between the plugin and the desktop application. One file, compiled into the plugin, the
// desktop application (linked by its csproj) and the tests, so both ends speak the same protocol. Evidence and limits:
// docs/evidence/x119-desktop-link.md.
//  - A named pipe, no network port. Off by default: the plugin opens it only while Settings › Advanced « Link with the desktop app » is on.
//  - One way: the plugin writes, the application reads (PipeDirection.Out on the plugin's side): nothing the application sends is read.
//  - Current user only: the pipe's access list allows the user who runs the game and denies network logons; the application connects with
//    PipeOptions.CurrentUserOnly, so a pipe of the same name made by another user is refused.
//  - One line of JSON per message, at most MaxLine bytes, protocol number checked; the application drops anything else.
//  - Protocol 2 (0.1114): small live states (LinkLive: job, level, GP, bags' free slots) and notices of the plugin's saved files that
//    changed (LinkChange), never a file's content: the application reads those files itself, read-only.
//  - Protocol 3 (0.1119, X-149): a notice carries a category of a closed list (LocalLinkProtocol.Categories: « settings », « bank »…),
//    never a file name (the bank's and the characters' files are named by the character's id). Another protocol number is refused; the
//    application, finding no pipe of its own protocol but one of another for the same user, says so (OtherProtocol).

/// <summary>What the plugin tells the application, read on the framework thread and copied (R07). Protocol 1: who and where. Protocol 2
/// (0.1114): also the small live states and the saved files that changed. Protocol 3 (0.1119): those as categories only.</summary>
public sealed record LinkSnapshot(int Protocol,string Plugin,bool LoggedIn,bool Paused,uint Territory,string Zone,long Sequence,
    LinkLive? Live=null,IReadOnlyList<LinkChange>? Changes=null);

/// <summary>The link's wire format: one JSON line per snapshot, bounded and checked.</summary>
public static class LocalLinkProtocol
{
    public const int Version=3;
    /// <summary>Longest line accepted, in bytes (a snapshot is about 150, 800 with every category).</summary>
    public const int MaxLine=8192;
    public const int MaxJob=16;
    /// <summary>Change notices in one snapshot, at most (one per category).</summary>
    public const int MaxChanges=16;
    /// <summary>The category of the plugin's settings file (the only one the application reads again so far).</summary>
    public const string SettingsCategory="settings";

    /// <summary>
    /// X-149 (0.1119): the closed list of what a change notice may say, by the plugin's saved file or folder it comes from (pluginConfigs\
    /// TenshoAssistant\…; the file's name up to its first dot): never a file name, never a character's id. Anything else is not sent.
    /// </summary>
    public static readonly IReadOnlyDictionary<string,string> Categories=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
    {
        ["InventoryBank"]="bank",["Characters"]="characters",["GilJournal"]="gil-journal",["CraftLists"]="craft-lists",["MyTools"]="my-tools",
        ["VoyageHistory"]="voyages",["AchievementProgress"]="achievements",["BeastTeams"]="beast-teams",["DutyProgress"]="duties",["Discoveries"]="discoveries",
        // X-119 with X-173 (0.1147): the player's data file TenshoData.db (gil journal, achievement progress since 0.1139 and 0.1140).
        ["TenshoData"]="data",
    };
    /// <summary>X-119 with X-173 (0.1147): the category of the player's data file, copied by SQLite's backup (TenshoDataDatabase.CopyForDesktop).</summary>
    public const string DataCategory="data";
    /// <summary>X-119 step 2, part 2 (0.1131): the categories the desktop application copies from the plugin's folder and reads
    /// (DesktopMirror, MainWindow.UseDesktopData); the others stay in the game for now. 0.1147: « data » too. 0.1151: « characters » and « duties ». 0.1152: « discoveries »
    /// (its database only, copied by SQLite's backup, TenshoDataDatabase.CopyDiscoveriesForDesktop).</summary>
    public static readonly IReadOnlyList<string> DesktopCategories=["bank","craft-lists","my-tools","voyages","beast-teams",DataCategory,"characters","duties",DiscoveriesCategory];
    /// <summary>X-119 (0.1152): the category of the discoveries (Discoveries/), whose database alone the desktop application copies, at most every 5 minutes.</summary>
    public const string DiscoveriesCategory="discoveries";
    public const int MaxPlugin=32;
    public const int MaxZone=128;
    private static readonly JsonSerializerOptions Options=new(){MaxDepth=4};

    /// <summary>The pipe's name for a Windows user: the same in the game and in the application, apart between users of one PC.</summary>
    public static string PipeName(string user)
        => "TenshoAssistant.Link."+Version+"."+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.ToUpperInvariant())))[..16];

    /// <summary>The pipe's name for the user running this process.</summary>
    public static string PipeName()=>PipeName(Environment.UserDomainName+"\\"+Environment.UserName);

    public static string Serialize(LinkSnapshot snapshot)=>JsonSerializer.Serialize(snapshot,Options);

    /// <summary>A line read from the pipe: the snapshot, or null and why it was refused (English, for the log).</summary>
    public static bool TryParse(string? line,out LinkSnapshot? snapshot,out string? error)
    {
        snapshot=null;error=null;
        if(string.IsNullOrWhiteSpace(line)){error="empty line";return false;}
        if(Encoding.UTF8.GetByteCount(line)>MaxLine){error="line too long";return false;}
        LinkSnapshot? read;
        try{read=JsonSerializer.Deserialize<LinkSnapshot>(line,Options);}
        catch(JsonException e){error="not a snapshot: "+e.Message;return false;}
        if(read is null || read.Plugin is null || read.Zone is null){error="not a snapshot";return false;}
        if(read.Protocol!=Version){error=$"protocol {read.Protocol}, this end speaks {Version}";return false;}
        if(read.Plugin.Length>MaxPlugin || read.Zone.Length>MaxZone){error="field too long";return false;}
        if(read.Live is { } live && (live.Job is null || live.Job.Length>MaxJob || live.Level is < -1 or > 1000 || live.Gp is < -1 or > 100_000
            || live.MaxGp is < -1 or > 100_000 || live.BagSize is < -1 or > 1000 || live.BagFree<-1 || live.BagFree>Math.Max(live.BagSize,0)))
            {error="live state out of bounds";return false;}
        if(read.Changes is { } changes && (changes.Count>MaxChanges || changes.Any(change=>change is null || !ValidCategory(change.Category) || change.Generation<1)))
            {error="change list refused";return false;}
        snapshot=read;return true;
    }

    /// <summary>
    /// X-149 (0.1119): the category a saved file's change is sent as: « settings » for the settings file itself (pluginConfigs\
    /// TenshoAssistant.json), else the category of the plugin folder's file or sub-folder it is in (Categories); null for anything else,
    /// a temporary or lock file, the data cache and the play state.
    /// </summary>
    public static string? ChangeCategory(string settingsFile,string folder,string path)
    {
        var full=Path.GetFullPath(path);
        if(string.Equals(full,Path.GetFullPath(settingsFile),StringComparison.OrdinalIgnoreCase))return SettingsCategory;
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return null;
        if(full.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase) || full.EndsWith(".lock",StringComparison.OrdinalIgnoreCase))return null;
        // 0.1147: a SQLite file's shared-memory index (-shm) changes when a reader (the desktop application's copy) reads: not a change of data.
        if(full.EndsWith("-shm",StringComparison.OrdinalIgnoreCase))return null;
        var first=full[(root.Length+1)..].Split(Path.DirectorySeparatorChar)[0];
        var stem=first.Split('.')[0];
        return Categories.TryGetValue(stem,out var category) ? category : null;
    }

    /// <summary>A category the link may carry: « settings » or one of Categories, nothing else.</summary>
    public static bool ValidCategory(string? category)
        => category==SettingsCategory || category is not null && Categories.Values.Contains(category,StringComparer.Ordinal);

    /// <summary>
    /// X-149 (0.1119): the protocol number of another Tensho link offered to this user (pipe names « TenshoAssistant.Link.&lt;n&gt;.&lt;user&gt; »
    /// of another number), or null: the plugin and the app speak different protocols (one of them is older).
    /// </summary>
    public static int? OtherProtocol(IEnumerable<string> pipes,string user)
    {
        var own=PipeName(user);
        var tail="."+own[(own.LastIndexOf('.')+1)..];
        foreach(var pipe in pipes)
        {
            var name=Path.GetFileName(pipe);
            if(!name.StartsWith("TenshoAssistant.Link.",StringComparison.Ordinal) || !name.EndsWith(tail,StringComparison.Ordinal) || name==own)continue;
            var middle=name["TenshoAssistant.Link.".Length..^tail.Length];
            if(int.TryParse(middle,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var number) && number!=Version)return number;
        }
        return null;
    }
}

/// <summary>Where the plugin's end of the link stands.</summary>
public enum LocalLinkState { Off, Waiting, Linked, Busy, Unavailable }

/// <summary>
/// The plugin's end: one pipe instance, written to on a worker (never on the framework thread). Publish keeps the latest snapshot; the
/// worker sends it when it changes and again every Heartbeat, so a closed application is seen and a new pipe offered. Dispose cancels and
/// waits one second at most.
/// </summary>
public sealed class LocalLinkServer:IDisposable
{
    public static readonly TimeSpan Heartbeat=TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BusyRetry=TimeSpan.FromSeconds(3);
    private readonly string name;
    private readonly Action<string>? log;
    private readonly CancellationTokenSource cancel=new();
    private readonly SemaphoreSlim changed=new(0,1);
    private readonly Task loop;
    private volatile string? latest;
    private volatile LocalLinkState state=LocalLinkState.Waiting;
    private volatile string? problem;
    private int disposed;

    public LocalLinkServer(string name,Action<string>? log=null)
    {
        this.name=name;this.log=log;
        loop=Task.Run(()=>Run(cancel.Token));
    }

    public LocalLinkState State=>state;
    /// <summary>Why the link is Busy or Unavailable (English), else null.</summary>
    public string? Problem=>problem;

    /// <summary>Any thread: the snapshot to send (the previous one, not sent yet, is replaced).</summary>
    public void Publish(LinkSnapshot snapshot)
    {
        latest=LocalLinkProtocol.Serialize(snapshot);
        try{changed.Release();}
        catch(SemaphoreFullException){}
    }

    private void Set(LocalLinkState value,string? why)
    {
        var before=state;
        problem=why;state=value;
        if(before!=value)log?.Invoke($"Local link: {value}{(why is null ? "" : " ("+why+")")}.");
    }

    private async Task Run(CancellationToken token)
    {
        try
        {
            if(!OperatingSystem.IsWindows()){Set(LocalLinkState.Unavailable,"the link needs Windows named pipes");return;}
            while(!token.IsCancellationRequested)
            {
                NamedPipeServerStream pipe;
                try{pipe=Create(name);}
                catch(IOException e)
                {
                    // Another game client of this user holds the pipe, or the last application has not let go yet.
                    Set(LocalLinkState.Busy,e.Message);
                    await Task.Delay(BusyRetry,token).ConfigureAwait(false);
                    continue;
                }
                catch(Exception e) when(e is UnauthorizedAccessException or PlatformNotSupportedException or NotSupportedException or IdentityNotMappedException)
                {
                    Set(LocalLinkState.Unavailable,e.GetType().Name+": "+e.Message);
                    return;
                }
                await using(pipe.ConfigureAwait(false))
                {
                    Set(LocalLinkState.Waiting,null);
                    await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                    Set(LocalLinkState.Linked,null);
                    try
                    {
                        while(!token.IsCancellationRequested && pipe.IsConnected)
                        {
                            if(latest is { } line)
                            {
                                await pipe.WriteAsync(Encoding.UTF8.GetBytes(line+"\n"),token).ConfigureAwait(false);
                                await pipe.FlushAsync(token).ConfigureAwait(false);
                            }
                            await changed.WaitAsync(Heartbeat,token).ConfigureAwait(false);
                        }
                    }
                    catch(IOException){}// the application closed: a new pipe instance is offered
                }
            }
        }
        catch(OperationCanceledException){}
        catch(ObjectDisposedException){}
        catch(Exception e){Set(LocalLinkState.Unavailable,e.GetType().Name+": "+e.Message);}
    }

    /// <summary>The pipe: out only, one instance, the current user allowed and network logons denied.</summary>
    [SupportedOSPlatform("windows")]
    private static NamedPipeServerStream Create(string name)
    {
        var user=WindowsIdentity.GetCurrent().User ?? throw new UnauthorizedAccessException("no user for this process");
        var security=new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(user,PipeAccessRights.FullControl,AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid,null),PipeAccessRights.FullControl,AccessControlType.Deny));
        return NamedPipeServerStreamAcl.Create(name,PipeDirection.Out,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.FirstPipeInstance,0,0,security);
    }

    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        cancel.Cancel();
        try{loop.Wait(TimeSpan.FromSeconds(1));}
        catch(AggregateException){}
        state=LocalLinkState.Off;
    }
}

/// <summary>
/// The application's end: connects when the plugin offers the pipe, reads bounded lines, keeps the last snapshot and when it came. Tries
/// again every two seconds; nothing is ever written to the pipe.
/// </summary>
public sealed class LocalLinkClient:IDisposable
{
    /// <summary>A snapshot older than this (three heartbeats) means the link is gone.</summary>
    public static readonly TimeSpan Fresh=TimeSpan.FromSeconds(15);
    private readonly string name;
    private readonly CancellationTokenSource cancel=new();
    private readonly Task loop;
    private volatile Received? last;
    private volatile string? problem;
    private int disposed;

    private sealed record Received(LinkSnapshot Snapshot,long At);

    public LocalLinkClient(string name)
    {
        this.name=name;
        loop=Task.Run(()=>Run(cancel.Token));
    }

    /// <summary>The last snapshot if it came less than <see cref="Fresh"/> ago, else null (not linked).</summary>
    public LinkSnapshot? Current=>last is { } received && Environment.TickCount64-received.At<Fresh.TotalMilliseconds ? received.Snapshot : null;
    /// <summary>The last line refused or connection error (English), else null.</summary>
    public string? Problem=>problem;
    /// <summary>X-149: the protocol of a Tensho link of another number offered to this user while none of this one is, else null.</summary>
    public int? OtherProtocol{get;private set;}

    private static int? Other()
    {
        try{return LocalLinkProtocol.OtherProtocol(Directory.GetFiles(@"\\.\pipe\"),Environment.UserDomainName+"\\"+Environment.UserName);}
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
        catch(ArgumentException){return null;}
    }

    private async Task Run(CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            try
            {
                using var pipe=new NamedPipeClientStream(".",name,PipeDirection.In,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                await pipe.ConnectAsync(1000,token).ConfigureAwait(false);
                problem=null;OtherProtocol=null;
                await Read(pipe,token).ConfigureAwait(false);
            }
            catch(OperationCanceledException){return;}
            catch(TimeoutException){OtherProtocol=Other();}// no plugin offers the link (or one of another protocol: said)
            catch(IOException e){problem=e.Message;}
            catch(UnauthorizedAccessException e){problem="refused: "+e.Message;}
            try{await Task.Delay(2000,token).ConfigureAwait(false);}
            catch(OperationCanceledException){return;}
        }
    }

    private async Task Read(Stream pipe,CancellationToken token)
    {
        var buffer=new byte[LocalLinkProtocol.MaxLine+1];
        var filled=0;
        while(true)
        {
            var count=await pipe.ReadAsync(buffer.AsMemory(filled),token).ConfigureAwait(false);
            if(count==0)return;
            filled+=count;
            var start=0;int end;
            while((end=Array.IndexOf(buffer,(byte)'\n',start,filled-start))>=0)
            {
                var line=Encoding.UTF8.GetString(buffer,start,end-start);
                start=end+1;
                if(LocalLinkProtocol.TryParse(line,out var snapshot,out var error))last=new(snapshot!,Environment.TickCount64);
                else problem=error;
            }
            if(start>0){Buffer.BlockCopy(buffer,start,buffer,0,filled-start);filled-=start;}
            if(filled>=buffer.Length){problem="line too long";return;}
        }
    }

    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        cancel.Cancel();
        try{loop.Wait(TimeSpan.FromSeconds(1));}
        catch(AggregateException){}
    }
}

/// <summary>X-119 step 2 (0.1114, protocol 2): the small live states sent whole, read on the framework thread and copied: job, level, gathering
/// points (GP), the four bags' free slots. -1 when not read (no character, a reading that failed).</summary>
public sealed record LinkLive(string Job,int Level,int Gp,int MaxGp,int BagFree,int BagSize);

/// <summary>X-119 step 2 (0.1114, protocol 2): a kind of saved file of the plugin that changed and the generation it changed in; X-149
/// (0.1119, protocol 3): its category (LocalLinkProtocol.Categories), never its name; the application reads the file again itself.</summary>
public sealed record LinkChange(string Category,long Generation);

/// <summary>
/// X-119 step 2 (0.1114): the saved files of the plugin that changed lately, filled from a file watcher's threads, read by the feed once a
/// second. At most <see cref="LocalLinkProtocol.MaxChanges"/> names, the newest generation of each; the oldest names leave first.
/// </summary>
public sealed class LinkChangeLog
{
    private readonly object gate=new();
    private readonly List<LinkChange> changes=[];
    private long generation;

    /// <summary>Any thread: a saved file changed (its category as <see cref="LocalLinkProtocol.ChangeCategory"/> gives it).</summary>
    public void Add(string category)
    {
        lock(gate)
        {
            changes.RemoveAll(change=>change.Category==category);
            changes.Add(new(category,++generation));
            if(changes.Count>LocalLinkProtocol.MaxChanges)changes.RemoveRange(0,changes.Count-LocalLinkProtocol.MaxChanges);
        }
    }

    /// <summary>The names to send now (oldest first), or null when nothing changed since the log began.</summary>
    public IReadOnlyList<LinkChange>? Snapshot(){lock(gate)return changes.Count==0 ? null : changes.ToArray();}
}

/// <summary>X-119 step 2 (0.1114): the application's side of the change notices: which files to read again, each once.</summary>
public sealed class LinkChangeReader
{
    private long seen,lastSequence;

    /// <summary>The categories changed since the last call (a new plugin start counts again from zero); empty when nothing new.</summary>
    public IReadOnlyList<string> Take(LinkSnapshot? snapshot)
    {
        // X-170 (0.1142, GPT audit): the plugin may start again while the link is down, its generations counting from 1 again up to the one seen
        // (then « newest < seen » does not tell it and its first notices were dropped): a lost link, or snapshots counting again from the start
        // (Sequence), makes everything it sends next new. Reading a file again is harmless; missing a change is not.
        if(snapshot is null){seen=0;lastSequence=0;return [];}
        if(snapshot.Sequence<lastSequence)seen=0;
        lastSequence=snapshot.Sequence;
        if(snapshot.Changes is not { Count: >0 } changes)return [];
        var newest=changes.Max(change=>change.Generation);
        // The plugin started again (its generations count from 1 again): every name it sends is new.
        if(newest<seen)seen=0;
        var categories=changes.Where(change=>change.Generation>seen).Select(change=>change.Category).Distinct().ToArray();
        seen=Math.Max(seen,newest);
        return categories;
    }
}
