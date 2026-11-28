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
        // X-76 H3 (0.914): a closed window stays closed when saving fails (the player asked for it); the next save writes it.
        TrySave("Tracker window");
    }

    private void DrawTrackerContents()
    {
        var now=DateTimeOffset.UtcNow;
        // X-727 (0.1733): an alarm made without an icon shows its item's (Item sheet).
        if(now>=trackerRefresh){trackerRows=TrackedAlarms.Rows(configuration.Alarms,now,itemIcon:id=>itemCatalog.State.Catalog?.Find(id)?.IconId ?? 0);trackerRefresh=now.AddSeconds(1);}
        // X-908 (0.1951): one line, the window has no width of its own (AlwaysAutoResize): a wrapped text would shrink it to a column.
        if(trackerRows.Count==0){EmptyStateLine("Tracker.Empty");return;}
        foreach(var row in trackerRows)
        {
            ImGui.PushID(row.Id.ToString());
            try
            {
                // X-727 (0.1733): the icon and the name answer as every item of Tensho (R10): the common tooltip, with the alarm's place and
                // next time under its header; the common menu, with the alarm's gestures first.
                var alarm=configuration.Alarms?.FirstOrDefault(entry=>entry.Id==row.Id);
                var place=alarm is null ? null:AlarmPlace(alarm);
                var map=place is { } point ? maps.State.Catalog?.Find(point.MapId):null;
                var item=itemCatalog.State.Catalog?.Find(row.ItemId);
                DrawGameIcon(row.IconId,new Vector2(20));
                TrackerRowHelp("TrackerIcon",row,item,alarm,map,place,now);
                ImGui.SameLine();
                if(ImGui.Selectable(row.Label,false,ImGuiSelectableFlags.None,new Vector2(220,0))){OpenAlarmItem(row.ItemId);IsOpen=true;}
                TrackerRowHelp("TrackerName",row,item,alarm,map,place,now);
                ImGui.SameLine();
                var left=row.At-now;
                ImGui.TextColored(left<=TimeSpan.FromMinutes(5) ? Accent:Muted,AlarmDtrEntry.Remaining(left,localization.Get)+(row.Weather ? " *":""));
                if(row.Weather)Tip("Alarms.WeatherCondition");
                if(map is not null && place is { } shown)
                {
                    var (_,_,x,y)=shown;
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Tracker.Map")))OpenMapPointFromPage(map,x,y);
                    PlaceMenu("TrackerPlace",map,new(x,y),map.Zone);
                }
            }
            finally{ImGui.PopID();}
        }
    }

    /// <summary>X-727 (0.1733): hover and right click of a tracker row's icon or name: the common item tooltip with the alarm's place and next
    /// time; the common item menu with the alarm's gestures first (the Alarm module, turn it off, the map on its point).</summary>
    private void TrackerRowHelp(string id,TrackedAlarm row,Items.ItemDefinition? item,TenshoAlarm? alarm,Maps.MapDefinition? map,(uint TerritoryId,uint MapId,float X,float Y)? place,DateTimeOffset now)
    {
        if(ImGui.IsItemHovered())
        {
            var lines=new List<string>{row.Label};
            if(map is not null && place is { } at)lines.Add(localization.Format("Tracker.Place",map.Zone,Maps.MapProjection.CoordinateText(new(at.X,at.Y))));
            lines.Add(localization.Format("Tracker.Next",row.At.ToLocalTime().ToString("t"),AlarmDtrEntry.Remaining(row.At-now,localization.Get))
                +(row.Weather ? " · "+localization.Get("Alarms.WeatherCondition"):""));
            DrawItemInfoTooltip(row.ItemId,lines:lines);
        }
        // X-735 M1 (review of 0.1733): an item the item list does not know (or not loaded yet) keeps the alarm's gestures, which do not depend
        // on the item: a menu of its own, under the alarm's name.
        if(item is null)
        {
            if(alarm is not null && ImGui.BeginPopupContextItem(UniqueMenuId($"{id}{row.Id}")))
            {
                try{ImGui.TextColored(Accent,row.Label);ImGui.Separator();TrackerAlarmMenu(alarm,map,place);}
                finally{ImGui.EndPopup();}
            }
            return;
        }
        ItemLinksMenu(UniqueMenuId($"{id}{row.Id}"),item,first:alarm is null ? null:()=>TrackerAlarmMenu(alarm,map,place));
    }

    private void TrackerAlarmMenu(TenshoAlarm alarm,Maps.MapDefinition? map,(uint TerritoryId,uint MapId,float X,float Y)? place)
    {
        if(ImGui.Selectable(localization.Get("Alarms.Open")+"##TrackerOpenAlarm")){page=Page.Alarms;IsOpen=true;}
        if(ImGui.Selectable(localization.Get("Tracker.TurnOff")+"##TrackerTurnOff"))
        {
            alarm.Enabled=false;NoteGameCopy([alarm]);trackerRefresh=default;
            TrySave("Alarm turned off from the tracker",()=>alarm.Enabled=true);
        }
        if(map is not null && place is { } at && ImGui.Selectable(localization.Get("Tracker.Map")+"##TrackerMapMenu"))OpenMapPointFromPage(map,at.X,at.Y);
    }

    /// <summary>0.514: the free-standing panels, as Dalamud windows; Plugin adds them to its window system.</summary>
    internal PanelWindow[] CreatePanelWindows()=>[new(this,TrackerPanel),new(this,TasksPanel),new(this,WearPanel),new(this,RegenTickPanel),new(this,WorkshopListPanel),new(this,PreparationListPanel),new(this,CraftListListPanel),new(this,ChatPanelDefinition),
        // X-447 B11 (0.1714): the chat's detached windows, drawn after the chat window.
        ..ChatDetachedPanels().Select(panel=>new PanelWindow(this,panel)),new(this,OccultPanelDefinition),new(this,StatePanelDefinition)];
    /// <summary>0.537: the game's windows, for the interfaces that step aside when one comes in front.</summary>
    internal Func<IReadOnlyList<UI.GameWindow>>? GameWindowSource { get; set; }
    internal IReadOnlyList<UI.GameWindow> GameWindowList=>GameWindowSource?.Invoke() ?? [];
    internal bool HidePanelsBehindGame=>configuration.HidePanelsBehindGame;
    private bool BehindGame(Vector2 at,Vector2 size,bool chat=false)
    {
        var screen=ImGui.GetMainViewport().Size;
        return UI.GameWindows.Covers(GameWindowList,at.X,at.Y,size.X,size.Y,screen.X,screen.Y,chat:chat);
    }
    internal bool BarBehindGame(Vector2 at,Vector2 size,string? name=null)
    {
        var behind=configuration.HideBarsBehindGame && BehindGame(at,size);
        // 0.692: what hides a panel, on each change, to read a « panel never shows » report in the log.
        if(name is not null && panelsBehind.GetValueOrDefault(name)!=behind && developerMode)
        {
            panelsBehind[name]=behind;
            log.Information("Panel {0} {1}: at {2}, size {3}; windows: {4}.",name,behind ? "steps aside":"comes back",at,size,string.Join(", ",GameWindowList.Where(window=>!UI.GameWindows.ChatOnlyWindow(window.Name) && UI.GameWindows.Touch(at.X,at.Y,size.X,size.Y,window.X,window.Y,window.Width,window.Height)).Select(window=>window.Name)));
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
            if(developerMode)log.Information("Map {0}: game window {1}, panel {2}, at {3}, size {4}; windows: {5}.",covered ? "steps aside":"comes back",byWindow,byPanel,at,size,string.Join(", ",GameWindowList.Where(window=>UI.GameWindows.Touch(at.X,at.Y,size.X,size.Y,window.X,window.Y,window.Width,window.Height)).Select(window=>window.Name)));
        }
        return covered;
    }
    private bool mapWasCovered;
    /// <summary>0.539: where a game window is now against where its reader saw it, for the panels that follow it.</summary>
    internal Func<string,float,float,Vector2>? GameWindowFollow { get; set; }
    internal Vector2 WindowMoved(string addon,float x,float y)=>GameWindowFollow is { } follow ? follow(addon,x,y)-new Vector2(x,y):Vector2.Zero;
    /// <summary>0.516: the panels attached to a game window.</summary>
    internal AttachedPanels? Panels { get; set; }
    internal void PanelFailed(string name,Exception error){log.Error(error,"Overlay {0} turned off until the plugin reloads (perhaps after a game update).",name);NotePanelOff(name);}
}
