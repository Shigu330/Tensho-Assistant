using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Diagnostics;

namespace TenshoAssistant;

// X-165 (0.1244, slice 1, user request of 2026-11-12): Tensho's dashboard, first in developer mode: the measures Tensho already takes (memory,
// the slowest page, readers unavailable, cache, bank, discoveries, saves, self-check, requests per host), read once a second while it is
// shown, nothing when it is closed. Its rules: Diagnostics/HealthBoard.cs. 0.1245 (slice 2): the server bar entry. 0.1246 (slice 3): the
// floating panel and the desktop application (the game's state through the local link: status bar and this dashboard there).
internal sealed partial class MainWindow
{
    // 0.828 rule (Timers828Tests): a « once a second » clock never starts from long.MinValue.
    private long dashboardAt=-100_000;
    private (HealthLevel Overall,IReadOnlyList<HealthLine> Lines)? dashboard;
    private HealthInputs? dashboardInputs;
    // X-264 (0.1269): the last hour, a point every ten seconds (Diagnostics/HealthHistory.cs), kept whether the dashboard is shown or not
    // (the cost of a point is measured: dashboardSampleMs); nothing is drawn while the dashboard is closed.
    private readonly HealthHistory dashboardHistory=new();
    private double dashboardSampleMs,dashboardSampleMaxMs;
    private string? dashboardCurve;
    /// <summary>X-165 (0.1245): what the server bar entry shows (framework thread): the overall state, the heap in MB (null: unknown), the lines
    /// as texts; 0.1246: and the lines that are not fine (the panel, the desktop application).</summary>
    internal sealed record DashboardSnapshot(HealthLevel Overall,double? HeapMegabytes,IReadOnlyList<string> Lines,IReadOnlyList<string> Issues);
    private DashboardSnapshot? dashboardSnapshot;
    internal DashboardSnapshot? DashboardState=>Volatile.Read(ref dashboardSnapshot);

    /// <summary>X-165 (0.1246): the snapshot as the local link sends it (LocalLinkFeed, framework thread), bounded by the protocol.</summary>
    internal Link.LinkState? DashboardLinkState()
        => DashboardState is { } state ? Link.LocalLinkProtocol.State(state.Overall.ToString(),state.HeapMegabytes,state.Issues) : null;

    // Once a second at most, on the draw thread (the page times belong to it): the dashboard and the snapshot the server bar reads.
    private void RefreshDashboard()
    {
        var now=Environment.TickCount64;
        if(dashboard is not null && now-dashboardAt<1000)return;
        var inputs=DashboardInputs();
        var evaluated=HealthBoard.Evaluate(inputs);
        dashboard=evaluated;dashboardAt=now;dashboardInputs=inputs;
        Volatile.Write(ref dashboardSnapshot,new DashboardSnapshot(evaluated.Overall,inputs.HeapBytes is { } heap ? heap/1048576.0:null,
            [.. evaluated.Lines.Select(DashboardText)],[.. evaluated.Lines.Where(line=>line.Level!=HealthLevel.Ok).Select(DashboardText)]));
    }

    /// <summary>X-165 (0.1245): called by the plugin's draw in every frame; reads the measures (once a second) only while one of their readers
    /// is on: the server bar entry, the panel (0.1246) or the link with the desktop application (0.1246).</summary>
    internal void TickDashboard(){if(DashboardWanted && DashboardStarted)RefreshDashboard();SampleDashboardHistory();}

    // X-264 (0.1269): one point of history every ten seconds, from the same reading as the dashboard (never a second way to measure);
    // after the same 20 s as the first reading. Its own cost is timed and shown in developer mode.
    private void SampleDashboardHistory()
    {
        var now=Environment.TickCount64;
        if(!DashboardStarted || !dashboardHistory.Due(now))return;
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        RefreshDashboard();
        if(dashboard is not { } read || dashboardInputs is not { } inputs)return;
        var frame=SlowFrames.Steady.LastFrame;
        dashboardHistory.Add(new HealthSample(now,read.Overall,inputs.ManagedBytes is { } managed ? managed/1048576.0:null,inputs.SlowestMedianMs,
            frame?.Average,frame?.P95,SlowFrames.Reported,inputs.Requests.Values.Sum(),inputs.WritesFailed,inputs.PanelsOff?.Count ?? 0,inputs.Unavailable.Count));
        dashboardSampleMs=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        dashboardSampleMaxMs=Math.Max(dashboardSampleMaxMs,dashboardSampleMs);
    }
    // X-165 (0.1251): the first reading by the server bar, the panel or the link waits 20 s after the window is built: done on the first
    // frame after a load it cost 16 to 17 ms there (« StateBar » in the log's slow-frame line of 0.1245 to 0.1250: its code compiled on the
    // game's thread); the JIT warm-up (from 5 s) compiles it before. The dashboard opened by the player reads at once.
    private const long DashboardStartDelayMs=20_000;
    private readonly long dashboardFrom=Environment.TickCount64+DashboardStartDelayMs;
    private bool DashboardStarted=>Environment.TickCount64-dashboardFrom>=0;
    private bool DashboardWanted=>configuration.StateServerBar || configuration.StatePanel || configuration.DesktopLink;

    private string DashboardText(HealthLine line)=>localization.Format(line.Key,[.. line.Values.Select(value=>value is double number ? number.ToString("0.0#",Numbers):value)]);

    private HealthInputs DashboardInputs()
    {
        var memory=GC.GetGCMemoryInfo();
        string? slowest=null;(double Median,double Max)? worst=null;
        foreach(var (shown,ring) in pageDrawMs)
            if(HealthBoard.MedianMax(ring.Samples,ring.Count) is { } times && (worst is null || times.Median>worst.Value.Median)){worst=times;slowest=shown.ToString();}
        var readers=new (string Name,Func<bool>? Down)[]
        {
            ("Chocobo",ChocoboUnavailable),("RaceChocobo",RaceChocoboUnavailable),("EquippedGear",EquippedGearUnavailable),
            ("ExtraCollections",ExtraCollectionsUnavailable),("Hunt",HuntUnavailable),("LeveProgress",LeveProgressUnavailable),
            ("DutyProgress",DutyProgressUnavailable),("Voyages",VoyageReadUnavailable),("GearCatalog",GearCatalogFailed),
            ("SubmarineCatalog",SubmarineCatalogFailed),("AirshipCatalog",AirshipCatalogFailed),("VesselParts",VesselPartsFailed),
        };
        var unavailable=readers.Where(reader=>reader.Down?.Invoke()==true).Select(reader=>reader.Name).Concat(FailedReadings()).Distinct().ToArray();
        var self=SelfCheck?.LastLines ?? [];
        return new HealthInputs(memory.HeapSizeBytes>0 ? memory.HeapSizeBytes:null,GC.GetTotalMemory(false),slowest,worst?.Median,worst?.Max,unavailable,
            dataCacheStore?.Error,bank.State.Error,DiscoveriesWriteBlocked,DiscoveryStore?.Dirty==true,saveError,Community.BoundedHttp.Sent,
            self.Count(line=>line.Status==SelfCheckStatus.Ok),self.Count(line=>line.Status==SelfCheckStatus.Suspect),self.Count(line=>line.Status==SelfCheckStatus.Failed),self.Count>0,
            [.. panelsOff],Volatile.Read(ref writesFailed),Volatile.Read(ref lastWriteFailure),LoadMilliseconds);
    }

    // X-165 (0.1247, slice 4): measures Tensho took without showing them. The panels and overlays turned off after an error (draw thread:
    // PanelWindow, AttachedPanels, the plugin's overlays), the saves in the background that failed (a writer's thread), the load's time.
    private readonly SortedSet<string> panelsOff=new(StringComparer.Ordinal);
    internal void NotePanelOff(string name)=>panelsOff.Add(name);
    private int writesFailed;
    private string? lastWriteFailure;
    /// <summary>X-165 (0.1247): a save in the background failed (any thread): counted, its message kept for the dashboard.</summary>
    internal void NoteWriteFailed(string message){Interlocked.Increment(ref writesFailed);Volatile.Write(ref lastWriteFailure,message);}
    /// <summary>X-165 (0.1247): the plugin's load, from its start to its window ready (the « Load created » line of the log).</summary>
    internal long? LoadMilliseconds { get; set; }

    private void DrawDashboard()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Dashboard.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
        // X-165 (0.1246): in the desktop application the measures would be the application's own: the game's state, brought by the link, instead.
        if(inDesktopApp){DrawDesktopGameState();return;}
        RefreshDashboard();
        var (overall,lines)=dashboard!.Value;
        ImGui.TextColored(DashboardColour(overall),localization.Get("Dashboard.Overall"+overall));
        DrawDashboardGauges(dashboardInputs!,dashboardHistory);
        foreach(var line in lines)
        {
            DrawStateMark(line.Level switch { HealthLevel.Problem=>false,HealthLevel.Watch=>null,_=>true });
            ImGui.SameLine();ImGui.TextWrapped(DashboardText(line));
        }
        ImGui.TextDisabled(localization.Get("Dashboard.Help"));
    }

    private System.Numerics.Vector4 DashboardColour(HealthLevel level)=>level switch { HealthLevel.Problem=>Error,HealthLevel.Watch=>Warning,_=>Success };

    /// <summary>
    /// X-264 (0.1269): the dashboard as gauges (the state of the moment) and counters, from the reading the lines below come from. A gauge
    /// with a rule of Tensho (HealthBoard) carries its zones and the colour of its level; one without a rule is drawn in the accent.
    /// The frame's own times exist only in developer mode (they are only measured there): no gauge otherwise, never a zero.
    /// </summary>
    private void DrawDashboardGauges(HealthInputs inputs,HealthHistory history)
    {
        var started=DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        var perLine=GaugesPerLine();var onLine=0;
        void Next(){if(onLine>0 && onLine<perLine)ImGui.SameLine();else onLine=0;onLine++;}
        string Number(double value,string format)=>value.ToString(format,Numbers);
        var memory=inputs.ManagedBytes is { } managed ? managed/1048576.0:(double?)null;
        var memoryTop=UI.GaugeMath.NiceMax(history.Series(sample=>sample.HeapMb).Max() ?? memory ?? 0,500);
        Next();
        if(Gauge("Memory",localization.Get("Dashboard.GaugeMemory"),memory is { } mb ? localization.Format("Dashboard.UnitMb",Number(mb,"N0")):"—",memory,memoryTop,null,null,
            localization.Format("Dashboard.GaugeMemoryTip",Number(memoryTop,"N0")),dashboardCurve=="Memory"))dashboardCurve=dashboardCurve=="Memory" ? null:"Memory";
        Next();
        if(Gauge("Page",localization.Get("Dashboard.GaugePage"),inputs.SlowestMedianMs is { } page ? localization.Format("Dashboard.UnitMs",Number(page,"0.0#")):"—",inputs.SlowestMedianMs,HealthHistoryScales.FrameMs,HealthBoard.SlowPageMs,null,
            localization.Format("Dashboard.GaugePageTip",inputs.SlowestPage ?? "—"),dashboardCurve=="Page"))dashboardCurve=dashboardCurve=="Page" ? null:"Page";
        if(SlowFrames.Steady.LastFrame is { } frame)
        {
            Next();
            if(Gauge("Frame",localization.Get("Dashboard.GaugeFrame"),localization.Format("Dashboard.UnitMs",Number(frame.Average,"0.0#")),frame.Average,HealthHistoryScales.FrameMs,null,null,
                localization.Format("Dashboard.GaugeFrameTip",frame.P95 is { } bound ? "≤ "+Number(bound,"0.##"):"> 100",Number(frame.Max,"0.#"),frame.Count),dashboardCurve=="Frame"))dashboardCurve=dashboardCurve=="Frame" ? null:"Frame";
        }
        var rate=history.LastPerMinute(sample=>sample.Requests);
        var rateTop=UI.GaugeMath.NiceMax(history.PerMinute(sample=>sample.Requests).Max() ?? rate ?? 0,10);
        Next();
        if(Gauge("Requests",localization.Get("Dashboard.GaugeRequests"),rate is { } perMinute ? Number(perMinute,"0.#"):"—",rate,rateTop,null,null,
            localization.Format("Dashboard.GaugeRequestsTip",inputs.Requests.Values.Sum()),dashboardCurve=="Requests"))dashboardCurve=dashboardCurve=="Requests" ? null:"Requests";
        var failed=inputs.WritesFailed+(inputs.SaveError is null ? 0:1);var panels=inputs.PanelsOff?.Count ?? 0;
        Next();StatTile(localization.Get("Dashboard.TileWrites"),failed.ToString("N0",Numbers),failed>0 ? HealthLevel.Problem:HealthLevel.Ok,localization.Get("Dashboard.TileWritesTip"));
        Next();StatTile(localization.Get("Dashboard.TilePanels"),panels.ToString("N0",Numbers),panels>0 ? HealthLevel.Watch:HealthLevel.Ok,localization.Get("Dashboard.TilePanelsTip"));
        Next();StatTile(localization.Get("Dashboard.TileReaders"),inputs.Unavailable.Count.ToString("N0",Numbers),inputs.Unavailable.Count>0 ? HealthLevel.Watch:HealthLevel.Ok,localization.Get("Dashboard.TileReadersTip"));
        if(DeveloperMode)
        {
            Next();StatTile(localization.Get("Dashboard.TileSlow"),SlowFrames.Reported.ToString("N0",Numbers),HealthLevel.Ok,localization.Get("Dashboard.TileSlowTip"));
            ImGui.TextDisabled(localization.Format("Dashboard.HistoryCost",history.Count,Number(dashboardSampleMs,"0.00"),Number(dashboardSampleMaxMs,"0.00")));
            SlowFrames.Add(ImGui.GetFrameCount(),"dashboard gauges",System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    // X-165 (0.1246, slice 3): the floating panel, for every player, off by default (Settings › General): the overall state and the memory,
    // the lines that are not fine, and « Details » opening the settings where the whole dashboard is. « Only when something is wrong » hides it
    // while all is fine, as it does the server bar entry. Read on the draw thread (its own measures), cut with the other panels.
    private PanelDefinition StatePanelDefinition=>new("StatePanel","###TenshoStatePanel",new Vector2(40,560),
        ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar,
        ()=>configuration.StatePanel && !inDesktopApp,StatePanelVisible,DrawStatePanelContents,Alpha:0.7f);

    private bool StatePanelVisible()
    {
        if(windowDisposed || !DashboardStarted)return false;
        RefreshDashboard();
        return StateBar.Shown(DashboardState?.Overall,configuration.StateServerBarOnlyProblems);
    }

    private void DrawStatePanelContents()
    {
        if(DashboardState is not { } state)return;
        ImGui.TextColored(DashboardColour(state.Overall),StateHeadline(state.Overall.ToString(),state.HeapMegabytes));
        foreach(var issue in state.Issues)ImGui.TextUnformatted(issue);
        if(ImGui.SmallButton(localization.Get("Dashboard.Details")+"##TenshoStateDetails"))OpenSettings();
        Tip("Dashboard.DetailsTip");
    }

    /// <summary>« Tensho: ok · 210 MB » (the server bar's own words), or without the memory when it is not known.</summary>
    private string StateHeadline(string overall,double? megabytes)
        => megabytes is { } mb
            ? localization.Format("Dashboard.Bar",[localization.Get("Dashboard.Bar"+overall),Math.Round(mb).ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current)])
            : localization.Format("Dashboard.BarNoMemory",[localization.Get("Dashboard.Bar"+overall)]);

    // X-165 (0.1246): the game's state the desktop application received by the link (DesktopSurface.GameState, each frame); null: none.
    private (string Overall,double? HeapMegabytes,string[] Issues)? desktopGameState;
    internal void DesktopGameState(string? overall,double? heapMegabytes,string[] issues)
        => desktopGameState=overall is not null && Link.LocalLinkProtocol.StateLevels.Contains(overall) ? (overall,heapMegabytes,issues ?? []) : null;

    private void DrawDesktopGameState()
    {
        if(desktopGameState is not var (overall,heap,issues)){ImGui.TextDisabled(localization.Get("Dashboard.DesktopNoState"));return;}
        var level=Enum.TryParse<HealthLevel>(overall,out var parsed) ? parsed : HealthLevel.Watch;
        ImGui.TextColored(DashboardColour(level),localization.Get("Dashboard.DesktopGame")+" "+StateHeadline(overall,heap));
        if(issues.Length==0)ImGui.TextUnformatted(localization.Get("Dashboard.DesktopAllFine"));
        foreach(var issue in issues){DrawStateMark(level==HealthLevel.Problem ? false : null);ImGui.SameLine();ImGui.TextWrapped(issue);}
    }
}
