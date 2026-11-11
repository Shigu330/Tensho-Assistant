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

/// <summary>What the plugin tells the application, read on the framework thread and copied (R07). Protocol 1: who and where, nothing else.</summary>
public sealed record LinkSnapshot(int Protocol,string Plugin,bool LoggedIn,bool Paused,uint Territory,string Zone,long Sequence);

/// <summary>The link's wire format: one JSON line per snapshot, bounded and checked.</summary>
public static class LocalLinkProtocol
{
    public const int Version=1;
    /// <summary>Longest line accepted, in bytes (a snapshot is about 150).</summary>
    public const int MaxLine=4096;
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
        snapshot=read;return true;
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

    private async Task Run(CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            try
            {
                using var pipe=new NamedPipeClientStream(".",name,PipeDirection.In,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                await pipe.ConnectAsync(1000,token).ConfigureAwait(false);
                problem=null;
                await Read(pipe,token).ConfigureAwait(false);
            }
            catch(OperationCanceledException){return;}
            catch(TimeoutException){}// no plugin offers the link
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
