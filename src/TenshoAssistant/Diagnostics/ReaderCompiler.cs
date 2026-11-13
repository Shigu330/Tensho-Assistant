using System.Reflection;
using System.Runtime.CompilerServices;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-177 C4.2 (0.1199). The readers' first passes compiled their code in a frame of the game (19 of them over 50 ms at a reload, 2.1 s
/// of frames in all); 0.1198 compiled each reader's class when it subscribed, on the thread that builds the plugin: the frames were
/// spared, and the load took 5.6 s instead of 2.4 s. Here the classes are compiled on a thread of their own, in the order the readers
/// subscribed, and each reader is first called once its class is compiled: neither the frames nor the load pay for it.
/// A reader waits through its <see cref="Ticket"/>: ready once its class is compiled, or once the guard delay is over (the reader then
/// compiles what it needs itself, as before), or once the compiler is stopped. One class at a time, each class once; the thread ends when
/// nothing waits and starts again for a later subscription. Stopped at unload: the thread is told and joined (R07), what still waited is
/// let go, and nothing of the plugin's instance stays behind (types and plain flags only).
/// 0.1200 (read at the load of 0.1199: the load back to 3.0 s, but 12 slow first passes and a first frame of 596 ms): on this machine's
/// 32 processors, three threads compiling at once took as long as one after the other (the classes in 8.0 s instead of 4.4 s, the
/// warm-up in 54 s instead of 32 s): the runtime compiles one method at a time for the plugin, and each thread compiling in the
/// background makes the game's thread wait longer for its own. So one background thread compiles at a time, in the order of the
/// need: the interface's code first (the warm-up's early part), then the readers' classes, then the rest. <see cref="Hold"/> is that
/// order: the classes wait while it says so, <see cref="MaxHold"/> at most.
/// </summary>
public sealed class ReaderCompiler : IDisposable
{
    /// <summary>How long a reader waits at most for its class: past it, the reader starts anyway.</summary>
    public static readonly TimeSpan Guard=TimeSpan.FromSeconds(20);
    /// <summary>0.1200: how long the classes wait at most for what is compiled before them, counted from the first subscription.</summary>
    public static readonly TimeSpan MaxHold=TimeSpan.FromSeconds(12);
    /// <summary>0.1200: while this says true the next class is not compiled yet (the interface's code is compiled first); asked on the
    /// worker. Given by the plugin, dropped at the stop: it is the one thing here that can hold an instance.</summary>
    public Func<bool>? Hold{get=>hold;set=>hold=value;}
    private volatile Func<bool>? hold;

    /// <summary>What a subscription asks before each call: whether its reader may run yet.</summary>
    public sealed class Ticket
    {
        private readonly long deadline;
        private readonly Func<long> now;
        private volatile bool ready;
        internal Ticket(long deadline,Func<long> now){this.deadline=deadline;this.now=now;}
        /// <summary>The class is compiled, the guard delay is over, or the compiler was stopped. Once true, it stays true.</summary>
        public bool Ready
        {
            get
            {
                if(ready)return true;
                if(now()<deadline)return false;
                ready=true;
                return true;
            }
        }
        internal void Set()=>ready=true;
    }

    private readonly object gate=new();
    private readonly Queue<(Ticket Ticket,MethodBase Handler)> waiting=new();
    private readonly Dictionary<Type,Ticket> tickets=[];
    private readonly Func<long> now;
    private readonly Action<MethodBase> prepare;
    private readonly long guard;
    private readonly long maxHold;
    private long firstAdded=-1;
    private readonly long created=System.Diagnostics.Stopwatch.GetTimestamp();
    private Thread? worker;
    private volatile bool stopped;
    private int classes,methods;
    private double compiling,doneAfter;

    /// <param name="now">The clock of the guard delay, in milliseconds (the tests' own; the machine's by default).</param>
    /// <param name="prepare">What compiles one method (the tests' own; the runtime's by default).</param>
    /// <param name="guard">The guard delay (<see cref="Guard"/> by default).</param>
    /// <param name="maxHold">How long <see cref="Hold"/> is listened to (<see cref="MaxHold"/> by default).</param>
    public ReaderCompiler(Func<long>? now=null,Action<MethodBase>? prepare=null,TimeSpan? guard=null,TimeSpan? maxHold=null)
    {
        this.now=now ?? (static ()=>Environment.TickCount64);
        this.prepare=prepare ?? (static method=>RuntimeHelpers.PrepareMethod(method.MethodHandle));
        this.guard=(long)(guard ?? Guard).TotalMilliseconds;
        this.maxHold=(long)(maxHold ?? MaxHold).TotalMilliseconds;
    }

    /// <summary>0.1200: whether the classes still wait for what is compiled before them (never past <see cref="MaxHold"/>, never once stopped).</summary>
    public bool Held
    {
        get
        {
            if(stopped || hold is not { } ask)return false;
            long first;
            lock(gate)first=firstAdded;
            if(first>=0 && now()-first>=maxHold)return false;
            try{return ask();}
            catch(Exception){return false;}
        }
    }

    /// <summary>Classes compiled so far, their methods, the time spent compiling them on the worker, how long after this compiler was made
    /// the queue last ran empty (all in milliseconds), and how many classes still wait.</summary>
    public (int Classes,int Methods,double Compiling,double DoneAfter,int Waiting) State
    {
        get{lock(gate)return (classes,methods,compiling,doneAfter,waiting.Count+(busy ? 1:0));}
    }
    private bool busy;

    /// <summary>Whether nothing waits and no thread runs (the tests' wait; the plugin never waits for it).</summary>
    public bool Idle{get{lock(gate)return worker is null && waiting.Count==0;}}
    public bool Stopped=>stopped;

    /// <summary>
    /// The ticket of a handler's class (see <see cref="JitWarmup.Owner"/>): the same for every handler of that class, compiled once.
    /// A handler without a class, or one given after the stop, is ready at once.
    /// </summary>
    public Ticket Add(MethodBase handler)
    {
        var owner=JitWarmup.Owner(handler);
        lock(gate)
        {
            if(owner is not null && tickets.TryGetValue(owner,out var known))return known;
            var ticket=new Ticket(now()+guard,now);
            if(stopped || owner is null){ticket.Set();return ticket;}
            tickets[owner]=ticket;
            if(firstAdded<0)firstAdded=now();
            waiting.Enqueue((ticket,handler));
            if(worker is null)
            {
                worker=new Thread(Run){IsBackground=true,Name="Tensho reader compiler"};
                worker.Start();
            }
            return ticket;
        }
    }

    private void Run()
    {
        while(true)
        {
            // 0.1200: the interface's code first: nothing is taken while the plugin says to wait (a short sleep, the stop seen at once).
            while(Held)Thread.Sleep(20);
            (Ticket Ticket,MethodBase Handler) next;
            lock(gate)
            {
                if(stopped || waiting.Count==0)
                {
                    // Nothing waits: the thread ends here, under the lock, so a subscription that comes now starts another one.
                    worker=null;
                    if(!stopped)doneAfter=System.Diagnostics.Stopwatch.GetElapsedTime(created).TotalMilliseconds;
                    return;
                }
                next=waiting.Dequeue();
                busy=true;
            }
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            var compiled=0;
            try{compiled=JitWarmup.PrepareOwner(next.Handler,JitWarmup.OwnerCap,()=>stopped,prepare);}
            catch(Exception){}
            // Whatever happened to the compiling, the reader is never left waiting.
            finally{next.Ticket.Set();}
            lock(gate)
            {
                busy=false;
                classes++;methods+=compiled;
                compiling+=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
        }
    }

    /// <summary>At unload: the thread is told to stop and joined (one method at most is being compiled); every reader still waiting is let
    /// go; a later subscription is ready at once.</summary>
    public void Dispose()
    {
        Thread? running;
        lock(gate)
        {
            if(stopped)return;
            stopped=true;
            hold=null;
            foreach(var (ticket,_) in waiting)ticket.Set();
            waiting.Clear();
            foreach(var ticket in tickets.Values)ticket.Set();
            tickets.Clear();
            running=worker;
        }
        if(running is not null && running!=Thread.CurrentThread)running.Join(TimeSpan.FromSeconds(2));
    }
}
