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
        ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.AlwaysAutoResize,
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
                if(row.Weather)Tip("Alarms.WeatherCondition");
                if(configuration.Alarms?.FirstOrDefault(alarm=>alarm.Id==row.Id) is { } alarm && AlarmPlace(alarm) is { } place
                   && maps.State.Catalog?.Find(place.MapId) is { } map)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Tracker.Map")))OpenMapPointFromPage(map,place.X,place.Y);
                    PlaceMenu("TrackerPlace",map,new(place.X,place.Y),map.Zone);
                }
            }
            finally{ImGui.PopID();}
        }
    }

    /// <summary>0.514: the free-standing panels, as Dalamud windows; Plugin adds them to its window system.</summary>
    internal PanelWindow[] CreatePanelWindows()=>[new(this,TrackerPanel),new(this,TasksPanel),new(this,WearPanel),new(this,RegenTickPanel),new(this,WorkshopListPanel),new(this,PreparationListPanel),new(this,CraftListListPanel)];
    /// <summary>0.537: the game's windows, for the interfaces that step aside when one comes in front.</summary>
    internal Func<IReadOnlyList<UI.GameWindow>>? GameWindowSource { get; set; }
    internal IReadOnlyList<UI.GameWindow> GameWindowList=>GameWindowSource?.Invoke() ?? [];
    internal bool HidePanelsBehindGame=>configuration.HidePanelsBehindGame;
    private bool BehindGame(Vector2 at,Vector2 size)
    {
        var screen=ImGui.GetMainViewport().Size;
        return UI.GameWindows.Covers(GameWindowList,at.X,at.Y,size.X,size.Y,screen.X,screen.Y);
    }
    internal bool BarBehindGame(Vector2 at,Vector2 size,string? name=null)
    {
        var behind=configuration.HideBarsBehindGame && BehindGame(at,size);
        // 0.692: what hides a panel, on each change, to read a « panel never shows » report in the log.
        if(name is not null && panelsBehind.GetValueOrDefault(name)!=behind)
        {
            panelsBehind[name]=behind;
            log.Information("Panel {0} {1}: at {2}, size {3}; windows: {4}.",name,behind ? "steps aside":"comes back",at,size,string.Join(", ",GameWindowList.Where(window=>UI.GameWindows.Touch(at.X,at.Y,size.X,size.Y,window.X,window.Y,window.Width,window.Height)).Select(window=>window.Name)));
        }
        return behind;
    }
    private readonly Dictionary<string,bool> panelsBehind=[];
    // 0.539 (user report): a panel attached to a game window goes with it: it hides the map as the window does.
    internal bool MapBehindGame(Vector2 at,Vector2 size)
    {
        if(!configuration.MapView.HideBehindGame)return false;
        var screen=ImGui.GetMainViewport().WorkSize;
        if(UI.MapEnlarged.Is(mapRestore is not null,size.X,size.Y,screen.X,screen.Y))return false;
        var byWindow=BehindGame(at,size);var byPanel=Panels?.Over(at,size)==true;
        var covered=byWindow || byPanel;
        // What makes the map step aside, on each change, to read a « map never comes back » report in the log.
        if(covered!=mapWasCovered)
        {
            mapWasCovered=covered;
            log.Information("Map {0}: game window {1}, panel {2}, at {3}, size {4}; windows: {5}.",covered ? "steps aside":"comes back",byWindow,byPanel,at,size,string.Join(", ",GameWindowList.Where(window=>UI.GameWindows.Touch(at.X,at.Y,size.X,size.Y,window.X,window.Y,window.Width,window.Height)).Select(window=>window.Name)));
        }
        return covered;
    }
    private bool mapWasCovered;
    /// <summary>0.539: where a game window is now against where its reader saw it, for the panels that follow it.</summary>
    internal Func<string,float,float,Vector2>? GameWindowFollow { get; set; }
    internal Vector2 WindowMoved(string addon,float x,float y)=>GameWindowFollow is { } follow ? follow(addon,x,y)-new Vector2(x,y):Vector2.Zero;
    /// <summary>0.516: the panels attached to a game window.</summary>
    internal AttachedPanels? Panels { get; set; }
    internal void PanelFailed(string name,Exception error)=>log.Error(error,$"Overlay {name} turned off until the plugin reloads (perhaps after a game update).");
}
