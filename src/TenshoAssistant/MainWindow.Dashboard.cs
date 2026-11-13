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
        dashboard=evaluated;dashboardAt=now;
        Volatile.Write(ref dashboardSnapshot,new DashboardSnapshot(evaluated.Overall,inputs.HeapBytes is { } heap ? heap/1048576.0:null,
            [.. evaluated.Lines.Select(DashboardText)],[.. evaluated.Lines.Where(line=>line.Level!=HealthLevel.Ok).Select(DashboardText)]));
    }

    /// <summary>X-165 (0.1245): called by the plugin's draw in every frame; reads the measures (once a second) only while one of their readers
    /// is on: the server bar entry, the panel (0.1246) or the link with the desktop application (0.1246).</summary>
    internal void TickDashboard(){if(DashboardWanted)RefreshDashboard();}
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
            self.Count(line=>line.Status==SelfCheckStatus.Ok),self.Count(line=>line.Status==SelfCheckStatus.Suspect),self.Count(line=>line.Status==SelfCheckStatus.Failed),self.Count>0);
    }

    private void DrawDashboard()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Dashboard.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
        // X-165 (0.1246): in the desktop application the measures would be the application's own: the game's state, brought by the link, instead.
        if(inDesktopApp){DrawDesktopGameState();return;}
        RefreshDashboard();
        var (overall,lines)=dashboard!.Value;
        ImGui.TextColored(DashboardColour(overall),localization.Get("Dashboard.Overall"+overall));
        foreach(var line in lines)
        {
            DrawStateMark(line.Level switch { HealthLevel.Problem=>false,HealthLevel.Watch=>null,_=>true });
            ImGui.SameLine();ImGui.TextWrapped(DashboardText(line));
        }
        ImGui.TextDisabled(localization.Get("Dashboard.Help"));
    }

    private System.Numerics.Vector4 DashboardColour(HealthLevel level)=>level switch { HealthLevel.Problem=>Error,HealthLevel.Watch=>Warning,_=>Success };

    // X-165 (0.1246, slice 3): the floating panel, for every player, off by default (Settings › General): the overall state and the memory,
    // the lines that are not fine, and « Details » opening the settings where the whole dashboard is. « Only when something is wrong » hides it
    // while all is fine, as it does the server bar entry. Read on the draw thread (its own measures), cut with the other panels.
    private PanelDefinition StatePanelDefinition=>new("StatePanel","###TenshoStatePanel",new Vector2(40,560),
        ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar,
        ()=>configuration.StatePanel && !inDesktopApp,StatePanelVisible,DrawStatePanelContents,Alpha:0.7f);

    private bool StatePanelVisible()
    {
        if(windowDisposed)return false;
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
