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
    /// <summary>X-177 C4.2 (0.1200): the early part is compiled (or there is none, or the warm-up ended or was stopped): what waits for the
    /// interface's code (the readers' classes) may go on.</summary>
    public bool EarlyDone=>earlyDone;
    private volatile bool earlyDone;
    /// <summary>X-177 C4.2 (0.1200): how long the rest waits at most for what is compiled before it (the readers' classes).</summary>
    public static readonly TimeSpan RestHold=TimeSpan.FromSeconds(30);

    /// <summary>X-01 (0.1279): the first part (the free panels' and the bars' code, with what it calls in the plugin) is compiled, or there is
    /// none, or the warm-up ended or was stopped: the panels may be drawn (<see cref="PanelGate"/>).</summary>
    public bool FirstPartDone=>firstPartDone;
    private volatile bool firstPartDone;
    /// <summary>X-01 (0.1279): methods of the first part (its roots and what they call), how many of them were compiled, and how long after
    /// the warm-up was made they were all compiled (ms; -1 before).</summary>
    public int FirstPartMethods{get;private set;}
    public int FirstPrepared{get;private set;}
    public double FirstReadyAfter{get;private set;}=-1;
    /// <summary>X-01 (0.1279): at most this many methods in the first part (roots and callees, breadth first: the nearest callees first).
    /// 0.1280: 2000, measured: at 8000 (0.1279) the closure reached the cap and was not compiled when the gate's bound came (3 s); the
    /// panels' 391 methods with their direct callees are 1604, with the next level 3688, then up to 12 762 (the window's code calls itself
    /// from one method to the next); in the game a method takes about 1.1 ms to compile (1894 in 2205 ms), so 2000 fit in the bound.</summary>
    public const int FirstCap=2000;
    private readonly long created=System.Diagnostics.Stopwatch.GetTimestamp();

    private JitWarmup(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished,Func<MethodBase,bool>? early,Func<bool>? holdRest,Func<MethodBase,bool>? firstPart)
    {
        var token=cancellation.Token;
        thread=new Thread(()=>Run(roots,delay,finished,early,holdRest,firstPart,token)){IsBackground=true,Priority=ThreadPriority.BelowNormal,Name="Tensho JIT warm-up"};
        thread.Start();
    }

    /// <summary>Starts the warm-up of these types and their nested types (lambdas, closures, local functions), listed on the worker, in this
    /// order; finished is told once, on the worker.</summary>
    /// <remarks>X-01 (0.1104): with <paramref name="early"/>, those methods are compiled first, <see cref="EarlyDelay"/> after loading, and the rest
    /// once <paramref name="delay"/> has passed.</remarks>
    /// <param name="holdRest">X-177 C4.2 (0.1200): while it says true, the rest (everything after the early part) waits, <see cref="RestHold"/>
    /// at most: the runtime compiles one method at a time for the plugin, so what the game's thread needs sooner is compiled first.</param>
    /// <param name="firstPart">X-01 (0.1279): with <paramref name="early"/>, the methods compiled before the rest of the early part, together
    /// with what they call in the plugin (<see cref="CallGraph.Closure"/>); <see cref="FirstPartDone"/> tells when.</param>
    public static JitWarmup Start(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished=null,Func<MethodBase,bool>? early=null,Func<bool>? holdRest=null,Func<MethodBase,bool>? firstPart=null)
        =>new(roots,delay,finished,early,holdRest,firstPart);

    /// <summary>X-01 (0.1104): a method of the main window (or of a type nested in it: lambdas, local functions) whose name says Map.
    /// X-01 (0.1111): and every method of the map's own types (namespace <see cref="MapNamespace"/>, nested types included).
    /// X-01 C3.3 (0.1188): and what the map's canvas calls on its first frame without saying Map: see <see cref="MapPathMethods"/>.</summary>
    public static bool MapFirst(MethodBase method)
    {
        if(method.DeclaringType?.Namespace is MapNamespace or VendorNamespace)return true;
        if(method.DeclaringType?.Name=="Configuration" && MapPathSettings.Contains(method.Name))return true;
        var name=method.Name;
        if(!name.Contains("Map",StringComparison.Ordinal) && !OnMapPath(name))return false;
        for(var type=method.DeclaringType;type is not null;type=type.DeclaringType)
            if(type.Name=="MainWindow")return true;
        return false;
    }

    /// <summary>
    /// X-01 C3.3 (0.1188, analysis of 12/11: « canvas vendors » took 32 to 40 ms for 9 vendors on the map's first frame, so the cost was
    /// the compiling, not the data): the main window's methods on the canvas's path whose name does not say Map (vendors, event and FATE
    /// labels, edge markers, glyphs, the player's arrow, the beasts' places, the quest index), compiled early with the map's; their lambdas
    /// and local functions carry the outer name in angle brackets. A test checks that each name is still declared in the sources.
    /// </summary>
    public static readonly IReadOnlyList<string> MapPathMethods=["VendorLayout","VendorLabel","VendorIconOf","VendorCandidates","VendorMarker","EventLabel","FateLabel",
        "DrawEdgeMarkers","DrawGlyph","DrawPlayerArrow","BeastPlacesOf","RefreshQuestIndex"];
    /// <summary>X-01 C3.3 (0.1188): the settings' methods the canvas calls for each icon (size and priority, per map).</summary>
    public static readonly IReadOnlyList<string> MapPathSettings=["SizeOf","PriorityOf","CleanSize"];
    /// <summary>X-01 C3.3 (0.1188): the namespace of the vendors' catalogue, read by the canvas for its vendor icons.</summary>
    public const string VendorNamespace="TenshoAssistant.Npcs";

    private static bool OnMapPath(string name)
    {
        foreach(var path in MapPathMethods)
            if(name==path || name.Contains("<"+path+">",StringComparison.Ordinal))return true;
        return false;
    }

    /// <summary>
    /// X-01 C2.1 (0.1185, analysis of 12/11: the overlays' first frame came about 1.9 s after the warm-up started, before the « everything
    /// else » part, and took 145 to 564 ms): the methods each frame calls outside the window, compiled early with the map's. The methods named
    /// in Plugin's overlay table (and their lambdas, which carry the outer name in angle brackets), the free panels (PanelWindow, the tracker,
    /// the bars) and the bank overlay.
    /// </summary>
    public static readonly IReadOnlyList<string> OverlayMethods=["DrawCraftLogHelper","DrawMonsterOverheadIcons","CollectDiscoveries","DrawStorageHighlight","DrawItemHover",
        "DrawCactpotOverlay","DrawCraftLivePanel","DrawInventorySortButton","ObserveRegenTick","DrawTriadOverlay","DrawTriadRequestPanel","DrawTriadDeckReminder","DrawCharacterPanel",
        "DrawDutyFinderPanel","DrawFashionCheckPanel","DrawGatheringPanel","DrawCollectablePanel","DrawRetainerSalesPanel","DrawSpearfishingPanel","DrawOceanVoyagePanel",
        "ProcessGameLogRequest","ObserveHuntTrain","ObserveOccultAlerts","ObserveGarden","WatchWishlistSales","WatchPlots","DrawFishingPanel","RefreshNearbyMonsters"];
    /// <summary>Words of the main window's methods that belong to the free panels and bars drawn every frame (tracker, wear bar, alarm and reminder bars).</summary>
    public static readonly IReadOnlyList<string> OverlayWords=["Tracker","Panel","Bar","Overlay"];

    /// <summary>X-01 C2.1 (0.1185): an overlay method (see <see cref="OverlayMethods"/>), a free panel's or a bar's.</summary>
    public static bool OverlayFirst(MethodBase method)
    {
        if(method.DeclaringType is not { } declaring)return false;
        for(var type=declaring;type is not null;type=type.DeclaringType)
            if(type.Name is "PanelWindow" or "BankImportOverlay")return true;
        var inWindow=false;
        for(var type=declaring;type is not null;type=type.DeclaringType)
            if(type.Name=="MainWindow"){inWindow=true;break;}
        if(!inWindow)return false;
        var name=method.Name;
        foreach(var overlay in OverlayMethods)
            if(name==overlay || name.Contains("<"+overlay+">",StringComparison.Ordinal))return true;
        foreach(var word in OverlayWords)
            if(name.Contains(word,StringComparison.Ordinal))return true;
        return false;
    }

    /// <summary>X-01 C2.1 (0.1185): what the early phase compiles: the map's methods and the overlays'.</summary>
    public static bool EarlyMethod(MethodBase method)=>MapFirst(method) || OverlayFirst(method);

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

    /// <summary>X-177 C4.2 (0.1198): the class a handler belongs to: the outermost type around its method (a lambda's closure is nested in it).</summary>
    public static Type? Owner(MethodBase handler)
    {
        var type=handler.DeclaringType;
        while(type?.DeclaringType is { } outer)type=outer;
        return type;
    }

    /// <summary>X-177 C4.2 (0.1198): a class with more methods than this is not compiled whole when one of its handlers subscribes (the window, the plugin).</summary>
    public const int OwnerCap=600;

    /// <summary>
    /// X-177 C4.2 (0.1198): compiles now, on the calling thread, the code a handler's class runs: the methods of its outermost class and of
    /// the types nested in it; when that is more than <paramref name="cap"/> methods, only those of the handler's own type (its closure).
    /// Returns how many were compiled; a method that cannot be prepared is skipped, never an error for the subscription.
    /// 0.1199: <paramref name="stop"/> is asked before each method (an unload does not wait for a whole class) and
    /// <paramref name="prepare"/> compiles one method (the runtime's by default; the tests' own).
    /// </summary>
    public static int PrepareOwner(MethodBase handler,int cap=OwnerCap,Func<bool>? stop=null,Action<MethodBase>? prepare=null)
    {
        IReadOnlyList<MethodBase> methods;
        try
        {
            methods=Owner(handler) is { } owner ? Methods([owner]):[];
            if(methods.Count>cap)methods=handler.DeclaringType is { } own ? Methods([own]):[];
            if(methods.Count>cap)return 0;
        }
        catch(Exception){return 0;}
        var prepared=0;
        foreach(var method in methods)
        {
            if(stop?.Invoke()==true)break;
            try
            {
                if(prepare is null)RuntimeHelpers.PrepareMethod(method.MethodHandle);else prepare(method);
                prepared++;
            }
            catch(Exception){}
        }
        return prepared;
    }

    /// <summary>X-177 C4.2 (0.1198): the part of a slow pass spent compiling, for its line of the log; nothing when nothing was compiled.</summary>
    public static string CompiledText(double milliseconds,long methods)=>
        methods<=0 ? "":string.Create(System.Globalization.CultureInfo.InvariantCulture,$" ({milliseconds:0} ms of it compiling {methods} methods)");

    internal static bool Compilable(MethodBase method)
    {
        if(method.IsAbstract || method.ContainsGenericParameters)return false;
        var implementation=method.MethodImplementationFlags;
        if((implementation&(MethodImplAttributes.InternalCall|MethodImplAttributes.Runtime|MethodImplAttributes.Native))!=0)return false;
        return (method.Attributes&MethodAttributes.PinvokeImpl)==0;
    }

    private void Run(Func<IReadOnlyList<Type>> roots,TimeSpan delay,Action<JitWarmup>? finished,Func<MethodBase,bool>? early,Func<bool>? holdRest,Func<MethodBase,bool>? firstPart,CancellationToken token)
    {
        try
        {
            if(early is null){earlyDone=true;firstPartDone=true;}
            var first=early is null ? TimeSpan.Zero:EarlyDelay<delay ? EarlyDelay:delay;
            if(token.WaitHandle.WaitOne(early is null ? delay:first))return;
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            var types=roots();
            var methods=Methods(types);
            // X-01 (0.1279): the panels' code first, with what it calls in the plugin and the generic code made for its types.
            if(early is not null && firstPart is not null && types.Count>0)
            {
                var part=CallGraph.Closure(methods.Where(firstPart),types[0].Assembly,FirstCap,()=>token.IsCancellationRequested);
                FirstPartMethods=part.Count;
                foreach(var method in part)
                {
                    if(token.IsCancellationRequested)return;
                    if(CallGraph.Prepare(method))Prepared++;else Skipped++;
                }
                FirstPrepared=Prepared;
                FirstReadyAfter=System.Diagnostics.Stopwatch.GetElapsedTime(created).TotalMilliseconds;
                var compiled=new HashSet<MethodBase>(part);
                methods=[..methods.Where(method=>!compiled.Contains(method))];
            }
            firstPartDone=true;
            var earlyCount=early is null ? 0:methods.Count(early);
            var index=0;
            foreach(var method in early is null ? methods:EarlyFirst(methods,early))
            {
                if(token.IsCancellationRequested)return;
                // 0.1104: the early part done, the rest waits until the usual delay after loading.
                if(index++==earlyCount && early is not null)
                {
                    EarlyPrepared=Prepared;
                    earlyDone=true;
                    var left=delay-first-System.Diagnostics.Stopwatch.GetElapsedTime(started);
                    if(left>TimeSpan.Zero && token.WaitHandle.WaitOne(left))return;
                    // 0.1200: then whatever is compiled before the rest (the readers' classes), bounded.
                    var held=System.Diagnostics.Stopwatch.GetTimestamp();
                    while(holdRest is not null && Held(holdRest) && System.Diagnostics.Stopwatch.GetElapsedTime(held)<RestHold)
                        if(token.WaitHandle.WaitOne(50))return;
                }
                try{RuntimeHelpers.PrepareMethod(method.MethodHandle);Prepared++;}
                catch(Exception){Skipped++;}
            }
            Elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(started);
            Completed=true;
            if(!token.IsCancellationRequested)finished?.Invoke(this);
        }
        catch(Exception){Skipped++;}
        finally{firstPartDone=true;earlyDone=true;done.Set();}
    }
    private static bool Held(Func<bool> ask)
    {
        try{return ask();}
        catch(Exception){return false;}
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
