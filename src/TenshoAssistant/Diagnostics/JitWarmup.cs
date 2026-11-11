using System.Reflection;
using System.Runtime.CompilerServices;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 (0.1050, measured without the game): most of the cost of a page's first frame is the compilation of its code (JIT), once per
/// load of the plugin. The desktop application's headless run measured it: Reminders 40.3 ms on its first frame, 38.9 ms of it compiling
/// 79 methods; Gathering 57.4 ms, 45.3 ms compiling (docs/evidence/0.1050-first-frame-jit.md). In the game the same first frames were
/// 92 to 161 ms (page sweep of 11/11 02:14). This compiles the window's methods ahead, then those of every other type of the plugin
/// (Configuration, alarms, timers, catalogues: what the pages still compiled once the window was done), on one background thread below
/// normal priority, a few seconds after loading, so that the frame finds them compiled. Generic code made for given type arguments and
/// the libraries' code (ImGui, Lumina) stay compiled on first use. Its own thread, cancelled and joined on dispose (R07); a method
/// that cannot be prepared (generic, abstract, without a body) is skipped, never an error for the window.
/// </summary>
public sealed class JitWarmup : IDisposable
{
    /// <summary>After loading: the catalogue loaders and the first frames go first.</summary>
    public static readonly TimeSpan Delay=TimeSpan.FromSeconds(5);
    /// <summary>X-01 (0.1104): the methods compiled first (the map's), this soon after loading: the map open at a reload is drawn about 2 s after it.</summary>
    public static readonly TimeSpan EarlyDelay=TimeSpan.FromMilliseconds(500);
    private readonly CancellationTokenSource cancellation=new();
    private readonly ManualResetEventSlim done=new(false);
    private readonly Thread? thread;
    private volatile bool disposed;

    public int Prepared { get; private set; }
    /// <summary>X-01 (0.1104): how many of <see cref="Prepared"/> were compiled first.</summary>
    public int EarlyPrepared { get; private set; }
    public int Skipped { get; private set; }
    public TimeSpan Elapsed { get; private set; }
    public bool Completed { get; private set; }

    private JitWarmup(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished,Func<MethodBase,bool>? early)
    {
        var token=cancellation.Token;
        thread=new Thread(()=>Run(roots,delay,finished,early,token)){IsBackground=true,Priority=ThreadPriority.BelowNormal,Name="Tensho JIT warm-up"};
        thread.Start();
    }

    /// <summary>Starts the warm-up of these types and their nested types (lambdas, closures, local functions), listed on the worker, in this
    /// order; finished is told once, on the worker.</summary>
    /// <remarks>X-01 (0.1104): with <paramref name="early"/>, those methods are compiled first, <see cref="EarlyDelay"/> after loading, and the rest
    /// once <paramref name="delay"/> has passed.</remarks>
    public static JitWarmup Start(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished=null,Func<MethodBase,bool>? early=null)
        =>new(roots,delay,finished,early);

    /// <summary>X-01 (0.1104): a method of the main window (or of a type nested in it: lambdas, local functions) whose name says Map.
    /// X-01 (0.1111): and every method of the map's own types (namespace <see cref="MapNamespace"/>, nested types included).</summary>
    public static bool MapFirst(MethodBase method)
    {
        if(method.DeclaringType?.Namespace==MapNamespace)return true;
        if(!method.Name.Contains("Map",StringComparison.Ordinal))return false;
        for(var type=method.DeclaringType;type is not null;type=type.DeclaringType)
            if(type.Name=="MainWindow")return true;
        return false;
    }

    /// <summary>X-01 (0.1111): the namespace of the map's types (catalogues, projection, layers, live readers).</summary>
    public const string MapNamespace="TenshoAssistant.Maps";

    /// <summary>X-01 (0.1104): the early methods first, then the others, each part in its former order.</summary>
    public static IReadOnlyList<MethodBase> EarlyFirst(IReadOnlyList<MethodBase> methods,Func<MethodBase,bool> early)
        =>[..methods.Where(early),..methods.Where(method=>!early(method))];

    /// <summary>The first type, then every type of its assembly (those that load: a type whose dependency is missing is left out).</summary>
    public static IReadOnlyList<Type> FirstThenAssembly(Type first)
    {
        Type[] types;
        try{types=first.Assembly.GetTypes();}
        catch(ReflectionTypeLoadException partial){types=partial.Types.Where(type=>type is not null).ToArray()!;}
        return [first,..types.Where(type=>type!=first)];
    }

    /// <summary>The methods compiled ahead: each type and its nested types (explicit stack, bounded), declared methods and constructors with a
    /// body, none generic or of a generic type (they need type arguments to compile).</summary>
    public static IReadOnlyList<MethodBase> Methods(IEnumerable<Type> roots)
    {
        const BindingFlags all=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
        var methods=new List<MethodBase>();
        var seen=new HashSet<Type>();
        // In the given order (the window first): each root, then its nested types, before the next root.
        var queue=new Queue<Type>(roots);
        var stack=new Stack<Type>();
        while((stack.Count>0 || queue.Count>0) && seen.Count<20_000)
        {
            var type=stack.Count>0 ? stack.Pop() : queue.Dequeue();
            if(!seen.Add(type))continue;
            foreach(var nested in type.GetNestedTypes(all))stack.Push(nested);
            if(type.ContainsGenericParameters || type.IsInterface)continue;
            foreach(var method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
                if(Compilable(method))methods.Add(method);
        }
        return methods;
    }

    internal static bool Compilable(MethodBase method)
    {
        if(method.IsAbstract || method.ContainsGenericParameters)return false;
        var implementation=method.MethodImplementationFlags;
        if((implementation&(MethodImplAttributes.InternalCall|MethodImplAttributes.Runtime|MethodImplAttributes.Native))!=0)return false;
        return (method.Attributes&MethodAttributes.PinvokeImpl)==0;
    }

    private void Run(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished,Func<MethodBase,bool>? early,CancellationToken token)
    {
        try
        {
            var first=early is null ? TimeSpan.Zero:EarlyDelay<delay ? EarlyDelay:delay;
            if(token.WaitHandle.WaitOne(early is null ? delay:first))return;
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            var methods=Methods(roots());
            var earlyCount=early is null ? 0:methods.Count(early);
            var index=0;
            foreach(var method in early is null ? methods:EarlyFirst(methods,early))
            {
                if(token.IsCancellationRequested)return;
                // 0.1104: the early part done, the rest waits until the usual delay after loading.
                if(index++==earlyCount && early is not null)
                {
                    EarlyPrepared=Prepared;
                    var left=delay-first-System.Diagnostics.Stopwatch.GetElapsedTime(started);
                    if(left>TimeSpan.Zero && token.WaitHandle.WaitOne(left))return;
                }
                try{RuntimeHelpers.PrepareMethod(method.MethodHandle);Prepared++;}
                catch(Exception){Skipped++;}
            }
            Elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(started);
            Completed=true;
            if(!token.IsCancellationRequested)finished?.Invoke(this);
        }
        catch(Exception){Skipped++;}
        finally{done.Set();}
    }

    /// <summary>For the desktop application's headless run: true once finished within the time.</summary>
    public bool Wait(TimeSpan limit)=>!disposed && done.Wait(limit);

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        cancellation.Cancel();
        // One method at most is being compiled: the join is short. The event is never disposed (the worker sets it last, whatever the
        // timing) and the token source only once the worker is gone: no ObjectDisposedException on the worker after an unload.
        if(thread is null || thread.Join(TimeSpan.FromSeconds(2)))cancellation.Dispose();
    }
}
