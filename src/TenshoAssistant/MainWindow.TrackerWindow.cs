using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// R-13 (0.415): a small pinned window (setting TrackerWindow, off by default) listing the items of the enabled Tensho alarms,
/// soonest first, with the time left; the name opens the item, « Map » the game map on its point when known (as the alarm's chat link).
/// Rows recomputed once a second; hidden with the game's interface.
/// </summary>
internal sealed partial class MainWindow
{
    private IReadOnlyList<TrackedAlarm> trackerRows=[];
    private DateTimeOffset trackerRefresh;

    private PanelDefinition TrackerPanel=>new("Tracker","###TenshoTracker",new Vector2(40,240),
        ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,
        ()=>configuration.TrackerWindow,()=>!windowDisposed && !gameGui.GameUiHidden,DrawTrackerContents,CloseTracker,()=>localization.Get("Tracker.Title"));

    private void CloseTracker()
    {
        if(!configuration.TrackerWindow)return;
        configuration.TrackerWindow=false;
        try{save();}catch(Exception error){log.Warning(error,"Tracker window setting could not be saved.");}
    }

    private void DrawTrackerContents()
    {
        var now=DateTimeOffset.UtcNow;
        if(now>=trackerRefresh){trackerRows=TrackedAlarms.Rows(configuration.Alarms,now);trackerRefresh=now.AddSeconds(1);}
        if(trackerRows.Count==0){ImGui.TextDisabled(localization.Get("Tracker.Empty"));return;}
        foreach(var row in trackerRows)
        {
            ImGui.PushID(row.Id.ToString());
            try
            {
                DrawItemIcon(new ItemDefinition(0,row.Label,false,row.IconId),new Vector2(20));ImGui.SameLine();
                if(ImGui.Selectable(row.Label,false,ImGuiSelectableFlags.None,new Vector2(220,0))){OpenAlarmItem(row.ItemId);IsOpen=true;}
                ImGui.SameLine();
                var left=row.At-now;
                ImGui.TextColored(left<=TimeSpan.FromMinutes(5) ? Accent:Muted,AlarmDtrEntry.Remaining(left)+(row.Weather ? " *":""));
                if(row.Weather && ImGui.IsItemHovered())WrappedTooltip(localization.Get("Alarms.WeatherCondition"));
                if(configuration.Alarms?.FirstOrDefault(alarm=>alarm.Id==row.Id) is { } alarm && AlarmPlace(alarm) is { } place
                   && maps.State.Catalog?.Find(place.MapId) is { } map)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Tracker.Map")))OpenMapPointFromPage(map,place.X,place.Y);
                }
            }
            finally{ImGui.PopID();}
        }
    }

    /// <summary>0.514: the free-standing panels, as Dalamud windows; Plugin adds them to its window system.</summary>
    internal PanelWindow[] CreatePanelWindows()=>[new(this,TrackerPanel),new(this,TasksPanel),new(this,WearPanel),new(this,RegenTickPanel)];
    /// <summary>0.516: the panels attached to a game window.</summary>
    internal AttachedPanels? Panels { get; set; }
    internal void PanelFailed(string name,Exception error)=>log.Error(error,$"Overlay {name} turned off until the plugin reloads (perhaps after a game update).");
}
