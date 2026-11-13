using Dalamud.Bindings.ImGui;
using TenshoAssistant.Diagnostics;

namespace TenshoAssistant;

// X-165 (0.1244, slice 1, user request of 2026-11-12): Tensho's dashboard, first in developer mode: the measures Tensho already takes (memory,
// the slowest page, readers unavailable, cache, bank, discoveries, saves, self-check, requests per host), read once a second while it is
// shown, nothing when it is closed. Its rules: Diagnostics/HealthBoard.cs. The players' version (server bar, panel, desktop page) comes next.
internal sealed partial class MainWindow
{
    // 0.828 rule (Timers828Tests): a « once a second » clock never starts from long.MinValue.
    private long dashboardAt=-100_000;
    private (HealthLevel Overall,IReadOnlyList<HealthLine> Lines)? dashboard;

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
        var now=Environment.TickCount64;
        if(dashboard is null || now-dashboardAt>=1000){dashboard=HealthBoard.Evaluate(DashboardInputs());dashboardAt=now;}
        var (overall,lines)=dashboard.Value;
        ImGui.TextColored(DashboardColour(overall),localization.Get("Dashboard.Overall"+overall));
        foreach(var line in lines)
        {
            DrawStateMark(line.Level switch { HealthLevel.Problem=>false,HealthLevel.Watch=>null,_=>true });
            ImGui.SameLine();ImGui.TextWrapped(localization.Format(line.Key,[.. line.Values.Select(value=>value is double number ? number.ToString("0.0#",Numbers):value)]));
        }
        ImGui.TextDisabled(localization.Get("Dashboard.Help"));
    }

    private System.Numerics.Vector4 DashboardColour(HealthLevel level)=>level switch { HealthLevel.Problem=>Error,HealthLevel.Watch=>Warning,_=>Success };
}
