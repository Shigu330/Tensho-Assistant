using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Competitive;

/// <summary>
/// DEC-038: the one switch of the competitive cut. Every framework subscription of the plugin goes through <see cref="Add"/>: while <see cref="Cut"/>
/// is true, none runs, so nothing is read from the game. A test refuses a subscription that goes around it. <see cref="AddAlways"/> is only for
/// what must undo something when the cut starts: the server bar entries and the game map.
/// </summary>
internal static partial class CompetitiveGate
{
    private static readonly object Gate=new();
    private static readonly Dictionary<IFramework.OnUpdateDelegate,IFramework.OnUpdateDelegate> Wrappers=[];
    private static volatile bool cut;
    public static bool Cut=>cut;
    // X-109 (0.981, DEC-047): in the Beastmaster labyrinth, the board window is open and the player is not in combat (docs/evidence/0.979-labyrinth-reading.md).
    private static volatile bool labyrinthBoard;
    public static bool LabyrinthBoard=>labyrinthBoard;
    internal static void SetLabyrinthBoard(bool value)=>labyrinthBoard=value;
    /// <summary>X-109 (0.981, DEC-047): a subscription that runs out of a cut, and during one only while the labyrinth's board is open (the team panel).</summary>
    public static void AddBoard(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        var ticket=Compiler.Add(handler.Method);
        void Guarded(IFramework running){if((!cut || labyrinthBoard) && ticket.Ready)Timed(handler,running);}
        lock(Gate)Wrappers[handler]=Guarded;
        framework.Update+=Guarded;
    }
    /// <summary>
    /// 0.992 (the log shows « Long CompetitiveGate…Guarded » hitches at each load, 90 to 144 ms, without saying which reader): in developer mode, a
    /// subscription that takes more than 50 ms is named in the log, once per reader. X-177 (0.1162): at most every 5 minutes per reader, with its
    /// worst time meanwhile (SlowNotes), so a reader slow at each zone change is seen again.
    /// </summary>
    private static readonly Diagnostics.SlowNotes slowNotes=new(TimeSpan.FromMinutes(5));
    private static void Timed(IFramework.OnUpdateDelegate handler,IFramework running)
    {
        if(Developer?.Invoke()!=true){handler(running);return;}
        // X-177 C4.4 (0.1193): the reader's own time: what it spends inside a function of the game (GameCallStarted / GameCallEnded) is left out.
        // X-177 C4.2 (0.1198): what this thread compiled during the pass (the runtime's own count for the calling thread): a slow pass that
        // is mostly compiling is code met for the first time, not work to move.
        var compiledBefore=System.Runtime.JitInfo.GetCompilationTime(true);
        var methodsBefore=System.Runtime.JitInfo.GetCompiledMethodCount(true);
        var pausedBefore=GC.GetTotalPauseDuration();
        var started=MeasureStart();
        handler(running);
        var spent=MeasureOwn(started);
        if(spent<=50)return;
        var name=$"{handler.Method.DeclaringType?.Name}.{handler.Method.Name}";
        if(slowNotes.Note(name,spent,Environment.TickCount64) is not { } worst)return;
        SlowSubscription?.Invoke(name+Diagnostics.JitWarmup.CompiledText((System.Runtime.JitInfo.GetCompilationTime(true)-compiledBefore).TotalMilliseconds,
            System.Runtime.JitInfo.GetCompiledMethodCount(true)-methodsBefore)+Diagnostics.JitWarmup.PauseText((GC.GetTotalPauseDuration()-pausedBefore).TotalMilliseconds),spent,worst);
    }

    /// <summary>
    /// X-177 C4.2 (0.1198, 0.1199; at a reload, the first pass of a reader took 125 to 276 ms of one frame: its code compiled the first
    /// time it ran, on the game's thread): a reader's own code is compiled before the game's thread first calls it: the methods of its
    /// class and of the types nested in it (lambdas, closures, local functions); a class too large for that (the window, the plugin
    /// itself) gives only the handler's own type. 0.1199: on a thread of its own, not on the one that builds the plugin (the load had
    /// grown from 2.4 s to 5.6 s): each way to subscribe takes the ticket of the reader's class from <see cref="Compiler"/> and calls
    /// the reader only once the ticket is ready (class compiled, or guard delay over). What a reader calls in other classes is still
    /// compiled on first use (the warm-up reaches it later).
    /// </summary>
    public static void Add(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        var ticket=Compiler.Add(handler.Method);
        void Guarded(IFramework running){if(!cut && ticket.Ready)Timed(handler,running);}
        lock(Gate)Wrappers[handler]=Guarded;
        framework.Update+=Guarded;
    }
    public static void Remove(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        IFramework.OnUpdateDelegate? guarded;
        lock(Gate){if(!Wrappers.Remove(handler,out guarded))return;}
        framework.Update-=guarded;
    }
    // 0.1199: these wait for their class too, through a wrapper kept with the others (Remove and ReleaseAll take it off the framework);
    // and, the wrapper hiding their name from Dalamud's own slow-call line, a slow one is named like the others (developer mode).
    public static void AddAlways(IFramework framework,IFramework.OnUpdateDelegate handler)
    {
        var ticket=Compiler.Add(handler.Method);
        void Deferred(IFramework running){if(ticket.Ready)Timed(handler,running);}
        lock(Gate)Wrappers[handler]=Deferred;
        framework.Update+=Deferred;
    }
    public static void RemoveAlways(IFramework framework,IFramework.OnUpdateDelegate handler)=>Remove(framework,handler);

    internal static void Set(bool value,bool notify)
    {
        if(cut==value)return;
        cut=value;
        if(notify)Changed?.Invoke(value);
    }
    /// <summary>
    /// X-01 C1.3 (0.1187): at unload, what is still subscribed through the gate is taken off the framework and forgotten; the count left says
    /// which unload forgot a reader (0 when every reader released its own).
    /// </summary>
    internal static int ReleaseAll(IFramework framework)
    {
        IFramework.OnUpdateDelegate[] left;
        lock(Gate){left=[..Wrappers.Values];Wrappers.Clear();}
        foreach(var guarded in left)framework.Update-=guarded;
        return left.Length;
    }
}

/// <summary>Reads the zones once, decides before any other service starts, then checks at each tick. Reads only what Dalamud's client state already holds.</summary>
internal sealed class CompetitiveWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IReadOnlySet<uint>? zones;
    private readonly IPluginLog log;
    private readonly IGameGui gui;
    private readonly ICondition condition;
    private string arenaSeen="";

    // X-109 (0.979, DEC-047): developer reading inside the labyrinth only, to prove when a fight starts (nothing else runs there).
    private string labyrinthSeen="";
    private DateTimeOffset labyrinthNext;
    // X-135 (0.1116, R06): the numbers of the labyrinth's own windows last written, and how many lines each got this visit (20 at most).
    private readonly Dictionary<string,string> labyrinthValues=[];
    private readonly Dictionary<string,int> labyrinthValueLines=[];
    private const int LabyrinthValueLines=20,LabyrinthValueCount=400;
    public CompetitiveWatcher(IFramework framework,IClientState client,IDataManager data,IPluginLog log,IGameGui gui,ICondition condition)
    {
        this.framework=framework;this.client=client;this.log=log;this.gui=gui;this.condition=condition;
        try
        {
            // X-92 (0.1001): the labyrinth's boards from the sheets (XBMContent column 0 = ContentFinderCondition); a failed read keeps the written list only.
            var labyrinth=new List<uint>();
            try
            {
                var finder=data.GetExcelSheet<ContentFinderCondition>();
                foreach(var row in data.Excel.GetSheet<Lumina.Excel.RawRow>(null,"XBMContent"))
                    if(row.RowId!=0 && Convert.ToUInt32(row.ReadColumn(0))is var content and not 0 && finder.GetRowOrDefault(content) is { } duty)labyrinth.Add(duty.TerritoryType.RowId);
            }
            catch(Exception error){log.Warning(error,"Labyrinth boards unreadable from the sheets: the written list stays.");}
            zones=CompetitiveZones.Build(data.GetExcelSheet<TerritoryType>().Select(row=>(row.RowId,row.IsPvpZone)),
                data.GetExcelSheet<ContentFinderCondition>().Select(row=>(row.TerritoryType.RowId,row.PvP,row.ContentType.RowId)),labyrinth);
        }
        // In doubt, stopped: after a game update that breaks these sheets Tensho stays cut rather than running in PvP.
        catch(Exception error){zones=null;log.Warning(error,"Competitive zones unavailable: Tensho stays stopped while logged in.");}
        CompetitiveGate.Set(Decide(),false);
        // What the in-game checks read in the log: the zones found, and each change.
        log.Information("Competitive cut: {0} zones known, cut now: {1}, territory {2}.",zones?.Count ?? -1,CompetitiveGate.Cut,client.TerritoryType);
        framework.Update+=Tick;
    }
    // Only looked at in the places that need it. The values are logged when they change: they tell the public hall from the arena's instance in game.
    // X-169 (0.1141, GPT audit R06): called only in the arena's place (IsCut asks it there); the game's PvP instance is the one read that decides
    // (a failure there is caught by IsCut: stopped); the others are for the log only, and their failure never reaches the decision.
    private unsafe bool InArenaInstance()
    {
        // Measured in the public hall (0.612 log): bound by duty and content row are true there, so only the game's PvP instance can tell.
        var pvpInstance=GameMain.IsInPvPInstance();
        try
        {
            var bound=condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
            var main=GameMain.Instance();
            var seen=$"instance area {(main!=null && main->IsInInstanceArea())}, PvP instance {pvpInstance}, bound by duty {bound}, content row {(main!=null && main->CurrentContentFinderConditionRow!=null)}, match window {gui.GetAddonByName("TripleTriad") is {IsNull:false,IsVisible:true}}";
            if(seen!=arenaSeen){arenaSeen=seen;log.Information("Competitive arena check, territory {0}: {1}.",client.TerritoryType,seen);}
        }
        catch(Exception error)
        {
            if(arenaSeen!="failed"){arenaSeen="failed";log.Warning(error,"Competitive arena check: the reading for the log failed; the decision does not use it.");}
        }
        return pvpInstance;
    }
    private bool Decide()=>CompetitiveZones.IsCut(client.IsLoggedIn,(uint)client.TerritoryType,client.IsPvP,zones,CompetitiveGate.Cut,InArenaInstance);
    private void Tick(IFramework _)
    {
        // X-109 (0.981, DEC-047): the board is open (its window visible) and the player not in combat; anything else, or a failed read, is no board.
        var board=false;
        if(CompetitiveZones.IsLabyrinth((uint)client.TerritoryType))
            try{var window=gui.GetAddonByName("XBMStageDetailList");board=!window.IsNull && window.IsVisible && !condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];}catch(Exception){board=false;}
        if(board!=CompetitiveGate.LabyrinthBoard){CompetitiveGate.SetLabyrinthBoard(board);log.Information("Labyrinth board panel {0}: territory {1}.",board ? "on":"off",client.TerritoryType);}
        if(CompetitiveGate.Developer?.Invoke()==true && CompetitiveZones.IsLabyrinth((uint)client.TerritoryType))
            try{ReadLabyrinth();}catch(Exception error){if(labyrinthSeen!="failed"){labyrinthSeen="failed";log.Warning(error,"Labyrinth reading failed.");}}
        else if(labyrinthValueLines.Count>0){labyrinthValueLines.Clear();labyrinthValues.Clear();}
        var now=Decide();
        if(now==CompetitiveGate.Cut)return;
        log.Information("Competitive cut {0}: territory {1}, PvP {2}.",now ? "on":"off",client.TerritoryType,client.IsPvP);
        CompetitiveGate.Set(now,true);
    }
    /// <summary>
    /// X-109 (0.979, DEC-047): read only, twice a second, written on change: the game's « in combat » and « bound by duty » states and the names of the
    /// visible windows, so the moment a fight starts (and the board's windows) can be told from the log. Developer mode only, labyrinth only.
    /// </summary>
    private unsafe void ReadLabyrinth()
    {
        var at=DateTimeOffset.UtcNow;
        if(at<labyrinthNext)return;
        labyrinthNext=at.AddMilliseconds(500);
        var names=new List<string>();
        var manager=FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkUnitManager.Instance();
        if(manager!=null)
        {
            var layers=manager->AtkUnitManager.DepthLayers;
            for(var layer=0;layer<layers.Length;layer++)
            {
                ref var list=ref layers[layer];
                var count=Math.Min((int)list.Count,list.Entries.Length);
                for(var index=0;index<count && names.Count<64;index++)
                {
                    FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase* unit=list.Entries[index].Value;
                    if(unit==null || !unit->IsVisible)continue;
                    var name=unit->NameString;
                    if(name.Length>0)names.Add(name);
                    if(name.StartsWith("XBM",StringComparison.Ordinal))LabyrinthValues(unit,name);
                }
            }
        }
        var seen=$"territory {client.TerritoryType}, in combat {condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]}, bound by duty {condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty]}, "
            +$"cut {CompetitiveGate.Cut}, windows: {string.Join(", ",names.Distinct().Order(StringComparer.Ordinal))}";
        if(seen==labyrinthSeen)return;
        labyrinthSeen=seen;
        log.Information("Labyrinth reading: {0}",seen);
    }
    /// <summary>
    /// X-135 (0.1116, R06): one of the labyrinth's own windows (« XBM… »): its numbers only (Int, UInt, Bool, by index; never a text, which may
    /// name the character), written on change, <see cref="LabyrinthValueLines"/> lines per window and visit at most. Developer mode, labyrinth only.
    /// </summary>
    private unsafe void LabyrinthValues(FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase* unit,string name)
    {
        if(unit->AtkValues==null)return;
        var values=new List<string>();
        for(var index=0;index<Math.Min((int)unit->AtkValuesCount,LabyrinthValueCount);index++)
        {
            var value=unit->AtkValues[index];
            var text=value.Type switch
            {
                FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int=>value.Int.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.UInt=>value.UInt.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Bool=>value.Byte!=0 ? "true" : "false",
                _=>null,
            };
            if(text is not null)values.Add($"{index}:{text}");
        }
        var line=string.Join(";",values);
        if(labyrinthValues.GetValueOrDefault(name)==line)return;
        labyrinthValues[name]=line;
        var written=labyrinthValueLines.GetValueOrDefault(name);
        if(written>=LabyrinthValueLines)return;
        labyrinthValueLines[name]=written+1;
        log.Information("Labyrinth window {0}: values={1}",name,line);
    }

    public void Dispose()
    {
        framework.Update-=Tick;
        CompetitiveGate.ClearSubscribers();
        CompetitiveGate.Set(false,false);
    }
}
