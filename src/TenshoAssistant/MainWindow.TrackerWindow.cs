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

    internal void DrawTrackerWindow()
    {
        if(windowDisposed || !configuration.TrackerWindow || gameGui.GameUiHidden)return;
        var now=DateTimeOffset.UtcNow;
        if(now>=trackerRefresh){trackerRows=TrackedAlarms.Rows(configuration.Alarms,now);trackerRefresh=now.AddSeconds(1);}
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(40,240),ImGuiCond.FirstUseEver);
        var open=true;
        var visible=ImGui.Begin(localization.Get("Tracker.Title")+"###TenshoTracker",ref open,
            ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize);
        try
        {
            if(!open){configuration.TrackerWindow=false;try{save();}catch(Exception error){log.Warning(error,"Tracker window setting could not be saved.");}return;}
            if(!visible)return;
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
        finally{ImGui.End();}
    }
}
