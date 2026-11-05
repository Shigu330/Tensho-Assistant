using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>
/// Tensho map window (MAP-VIEW-016, docs/goals/MAP-016-PLAN.md; drawn by MapViewWindow): the game map texture with its fixed icons,
/// the player arrow (optional vision cone), the flag, quest / FATE / gathering / vendor / vista markers and edge markers for points
/// off screen. Layout like the game map: region / zone / floor menus on top, tool column on the left (zoom, follow, settings), or
/// both drawn over the map (0.303 option). Icons have their own right-click menu and a list tooltip of what is under the mouse.
/// When the game map is replaced, M and the game's map requests (quest, aetheryte, FATE links) open this window instead; it hides
/// in combat or duties when the map settings say so.
/// </summary>
internal sealed partial class MainWindow
{
    // Game map icon of the red flag marker (to confirm in game; a drawn flag is the fallback).
    private const uint FlagIcon=60561;
    private MapLiveReader? mapLive;
    private MapMarkerSource? mapMarkers;
    private bool mapViewOpen;
    private bool mapViewFollow=true;
    private uint mapViewMapId;
    private float mapZoom=0.5f;
    private Vector2 mapPan;
    private Vector2 mapContextPixel;
    // Vista under the mouse this frame and the one right-clicked (0.201: alarm for its ET window and weather).
    private MapLocalPosition? mapHoveredVista,mapContextVista;
    private MapLocalIndexService? mapLocal;
    internal void ConfigureMapView(MapLiveReader live,MapMarkerSource markers,MapLocalIndexService local){mapLive=live;mapMarkers=markers;mapLocal=local;}
    // 0.328 (map audit #13): a chosen map opens whole, like NavigateMap; the old zoom and pan belonged to another map.
    // 0.499 (user report): a link to the map brings it above the Tensho window instead of opening behind it.
    private bool mapViewToFront;
    private void OpenMapView(uint mapId){mapViewMapId=mapId;mapViewFollow=mapId==0;mapFitRequested=mapId!=0;mapViewOpen=true;mapViewToFront=true;}
    // Called when the game map is replaced: the same key (M) that opens the game map toggles the Tensho map.
    // Hosted game map (0.151): the replacer asks whether the Tensho map is still open, and closes it with the game map.
    internal bool MapViewOpen=>Volatile.Read(ref mapViewOpen);
    internal void CloseMapFromGame()=>Volatile.Write(ref mapViewOpen,false);
    internal void ShowMapFromGame(GameMapRequest request)
    {
        if(DateTimeOffset.UtcNow<mapOwnFlagUntil){mapOwnFlagUntil=default;mapViewOpen=true;mapViewToFront=true;return;}
        var mapId=maps.State.Catalog is { } catalog ? request.Resolve(catalog):request.Selected==request.Current ? 0:request.Selected;
        // A quest, aetheryte or FATE was asked (journal, link): never the M toggle, the map opens focused on it.
        var pointed=request.Pointed;
        if(pointed && mapId==0)mapId=request.Current;
        if(!pointed && mapViewOpen && mapId==0 && mapViewFollow)mapViewOpen=false;
        // A pointed request keeps its own zoom: the focus centres it, or fits the map once it is not found.
        else{OpenMapView(mapId);if(pointed){mapFitRequested=false;mapGameTarget=null;mapFocusRequest=request;mapFocusUntil=DateTimeOffset.UtcNow.AddSeconds(1.5);}}
    }
    // Texture pixel of the asked target on the shown map: its event marker (ObjectiveId), the quest giver, the aetheryte, the FATE.
    private Vector2? FocusPixel(GameMapRequest focus,MapDefinition map,MapLiveState? live)
    {
        // The game's own target (temporary marker) first, like Mappy: it is on the map the game opened.
        if(focus.Target is { } target)return MapProjection.PixelFromWorld(new Vector3(target.X,0,target.Y),map);
        if(focus.QuestId!=0)
        {
            var key=focus.QuestId & 0xFFFF;
            var questId=focus.QuestId<0x10000 ? focus.QuestId+0x10000:focus.QuestId;
            if(live?.Events?.FirstOrDefault(marker=>marker.MapId==map.MapId && marker.ObjectiveId!=0 && (marker.ObjectiveId & 0xFFFF)==key) is { } objective)
                return MapProjection.PixelFromWorld(objective.Position,map);
            // The game names quest markers after the quest: same name as the journal entry.
            if(QuestOf(questId) is { Name.Length:>0 } quest
               && live?.Events?.FirstOrDefault(marker=>marker.MapId==map.MapId && marker.Tooltip.Contains(quest.Name,StringComparison.CurrentCultureIgnoreCase)) is { } named)
                return MapProjection.PixelFromWorld(named.Position,map);
            if(mapLocal?.State.Index?.Quest(questId) is { } giver && giver.MapId==map.MapId)return MapProjection.PixelFromWorld(new Vector3(giver.X,0,giver.Z),map);
        }
        if(focus.FateId!=0 && live?.MapId==map.MapId && live.Fates?.FirstOrDefault(fate=>fate.Id==focus.FateId) is { } running)
            return MapProjection.PixelFromWorld(running.Position,map);
        if(focus.AetheryteId!=0 && (mapMarkers?.For(map.MarkerRange) ?? []).FirstOrDefault(marker=>marker.AetheryteId==focus.AetheryteId) is { } aetheryte)
            return new Vector2(aetheryte.X,aetheryte.Y);
        return null;
    }
    private (uint Id,string Name)? mapTeleportTarget;
    private string mapContextBookmarkName="";
    private Action? openGameMap;
    internal void ConfigureGameMapOpener(Action opener)=>openGameMap=opener;

    // Drawn by MapViewWindow (WindowSystem, PUBLISH-018): visibility, then size/flags before Begin, then the content.
    internal bool MapViewVisible()
    {
        if(windowDisposed || maps.State.Catalog is null)return false;
        var settings=configuration.MapView;var live=mapLive?.State;
        // Hidden, not closed: the window comes back when combat or the duty ends.
        return !(live is not null && (settings.HideInCombat && live.InCombat || settings.HideInDuty && live.InDuty));
    }
    internal void PrepareMapView(Dalamud.Interface.Windowing.Window window)
    {
        UI.FfxivTheme.Push();
        if(mapViewToFront){mapViewToFront=false;window.BringToFront();}
        var settings=configuration.MapView;
        window.WindowName=localization.Get("MapView.Title")+"###TenshoMapView";
        // Sizes in screen pixels, set here rather than through Window.Size (which the window system scales).
        if(mapResize is { } resize)
        {
            // Enlarge: 90 % of the screen, centred; a second click restores the previous size.
            ImGui.SetNextWindowSize(resize.Size,ImGuiCond.Always);ImGui.SetNextWindowPos(resize.Position,ImGuiCond.Always);mapResize=null;
        }
        else ImGui.SetNextWindowSize(new Vector2(720,760),ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(320,280),new Vector2(float.MaxValue));
        ImGui.SetNextWindowBgAlpha(MapViewSettings.ClampOpacity(settings.Opacity));
        window.Flags=ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | (settings.Locked ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize:0) | (settings.HideTitleBar ? ImGuiWindowFlags.NoTitleBar:0);
    }
    internal void FinishMapView()=>UI.FfxivTheme.Pop();
    internal void DrawMapViewContents()
    {
        if(maps.State.Catalog is not { } catalog)return;
        var live=mapLive?.State;var viewport=ImGui.GetMainViewport();
        // Read here, outside the tool column: inside a child these calls return the child's size (the « Réduire » bug).
        var window=(ImGui.GetWindowSize(),ImGui.GetWindowPos());
        if(mapViewFollow && live is { MapId:>0 })mapViewMapId=live.MapId;
        var map=catalog.Find(mapViewMapId) ?? mapMarkers?.Overview(mapViewMapId);
        var settings=configuration.MapView;
        mapOverlayHovered=false;
        if(settings.OverlayControls && map is not null && map.TexturePath.Length>0)
        {
            DrawMapOverlayLayout(catalog,map,live,viewport,window);
            return;
        }
        // Layout of the game map: region and zone menus on top, tool column on the left, the map on the right.
        DrawMapMenus(catalog,map,live);
        DrawMapTools(map,viewport,window);
        ImGui.SameLine();
        if(map is null || map.TexturePath.Length==0)
        {
            // 0.328: no map because the character's position is unknown (not logged in, or the live read failed): said as such.
            ImGui.TextWrapped(localization.Get(map is null && live is null && mapViewFollow ? "MapView.PositionUnknown":"MapView.Unavailable"));
            return;
        }
        DrawMapCanvas(map,live);
    }

    // 0.303 (user request): the map takes the whole window; the region / zone menus (on a dark-to-clear band with the menu
    // arrows, like the game map) and the tool column are drawn over it, only while the mouse is over the map window (or one
    // of its menus is open).
    private void DrawMapOverlayLayout(MapCatalog catalog,MapDefinition map,MapLiveState? live,ImGuiViewportPtr viewport,(Vector2 Size,Vector2 Position) window)
    {
        var start=ImGui.GetCursorPos();var area=ImGui.GetContentRegionAvail();
        var settings=configuration.MapView;
        // 0.306: « blocked by active item » counts as hovered — a pressed button is the active item, and without it the overlays
        // vanished between the press and the release, so no click ever reached them.
        var shown=mapMarkers is not null && (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows|ImGuiHoveredFlags.AllowWhenBlockedByPopup|ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
            || ImGui.IsPopupOpen("",ImGuiPopupFlags.AnyPopupId|ImGuiPopupFlags.AnyPopupLevel));
        var line=ImGui.GetTextLineHeightWithSpacing()+2;const float pad=6,indent=16;var fade=line*1.2f;
        var grey=new Vector4(0.85f,0.85f,0.85f,1);
        // 0.305 (user feedback: no button answered): the overlays are submitted BEFORE the map, so their buttons take the click
        // first; the map then ignores the mouse while it is over one of them. They are still drawn above it (child windows).
        mapOverlayHovered=false;
        if(shown)DrawMapOverlayControls(catalog,map,live,viewport,window,start,area,line,pad,indent,fade,grey);
        else
        {
            mapHeaderList=0;
            // 0.305 (user request): the shown map's name stays written while the mouse is away, like the game map (setting).
            if(settings.OverlayZoneName)
            {
                var name=MapName(map);var sub=map.TerritoryId!=0 && map.Subzone.Length>0 ? "> "+map.Subzone:null;
                var size=new Vector2(Math.Max(ImGui.CalcTextSize(name).X,sub is null ? 0:ImGui.CalcTextSize(sub).X+indent)+pad*2,(sub is null ? 1:2)*line+pad*2);
                var at=start+new Vector2(38,0);
                HeaderBand("MapOverlayNameBand",at,size,fade,area-(at-start));
                ImGui.SetCursorPos(at);
                if(ImGui.BeginChild("MapOverlayName",Vector2.Min(size,area-(at-start)),false,ImGuiWindowFlags.NoBackground|ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoScrollbar))
                {
                    ImGui.SetCursorPos(new Vector2(pad,pad));
                    HeaderEntry("Name",name,UI.FfxivTheme.Gold,0,null,false);
                    if(sub is not null)HeaderEntry("Sub",sub,grey,indent,null,false);
                }
                ImGui.EndChild();
            }
        }
        ImGui.SetCursorPos(start);
        DrawMapCanvas(map,live);
    }

    // Region, zone and floor names (the game map's header: stacked, outlined, each unrolling its list downward) to the right of the
    // tool column, on a dark band sized to the names that fades out to the right and below.
    private void DrawMapOverlayControls(MapCatalog catalog,MapDefinition map,MapLiveState? live,ImGuiViewportPtr viewport,(Vector2 Size,Vector2 Position) window,
        Vector2 start,Vector2 area,float line,float pad,float indent,float fade,Vector4 grey)
    {
        var markers=mapMarkers!;
        // Tools first, from the top; their height leaves room for the cursor coordinates written at the bottom of the map.
        ImGui.SetCursorPos(start);
        var toolsWidth=DrawMapTools(map,viewport,window,true,area.Y-ImGui.GetTextLineHeight()-16);
        var regionId=markers.RegionOf(map);var region=ResolveMap(catalog,regionId);
        var regions=markers.Regions.Select(id=>ResolveMap(catalog,id)).OfType<MapDefinition>().ToArray();
        var zones=MapMenus.Zones(catalog.Entries,region);
        var floors=zones.FirstOrDefault(entry=>entry.Any(floor=>floor.MapId==map.MapId)) ?? [];
        var isZone=map.TerritoryId!=0;var world=localization.Get("MapView.World");
        static string FloorName(MapDefinition floor)=>floor.Subzone.Length>0 ? floor.Subzone:floor.MapIndex.ToString();
        var regionName=region is not null ? MapName(region):world;var zoneName=isZone ? MapName(map):"--";
        var arrow=ImGui.GetTextLineHeight();
        var widths=new List<float>{ImGui.CalcTextSize(regionName).X+arrow,ImGui.CalcTextSize(zoneName).X+arrow+(isZone ? 28:0)};
        if(floors.Count>1)widths.Add(ImGui.CalcTextSize(FloorName(map)).X+arrow);
        else if(isZone && map.Subzone.Length>0)widths.Add(indent+ImGui.CalcTextSize("> "+map.Subzone).X);
        var headerHeight=widths.Count*line+pad*2;
        var listCount=mapHeaderList switch{1=>regions.Length+1,2=>zones.Count+1,3=>floors.Count,_=>0};
        var at=start+new Vector2(toolsWidth+4,0);var room=area-(at-start);
        HeaderBand("MapOverlayBand",at,new Vector2(Math.Min(widths.Max()+pad*2+4,room.X),headerHeight),fade,room);
        var listWidth=regions.Select(MapName).Append(world).Concat(zones.Select(entry=>MapName(entry[0]))).Concat(floors.Select(FloorName))
            .Select(name=>ImGui.CalcTextSize("> "+name).X).DefaultIfEmpty(80).Max()+indent+28;
        var width=Math.Min(Math.Max(widths.Max(),mapHeaderList==0 ? 0:listWidth)+pad*2+4,room.X);
        var height=Math.Min(headerHeight+listCount*line,room.Y);
        ImGui.SetCursorPos(at);
        var headerHovered=false;
        if(ImGui.BeginChild("MapOverlayHeader",new Vector2(width,height),false,ImGuiWindowFlags.NoBackground))
        {
            headerHovered=ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows|ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            ImGui.SetCursorPos(new Vector2(pad,pad));
            if(HeaderEntry("Region",regionName,UI.FfxivTheme.Gold,0,mapHeaderList==1))mapHeaderList=mapHeaderList==1 ? 0:1;
            if(mapHeaderList==1)
            {
                ListBackdrop(regions.Length+1);
                if(HeaderEntry("World","> "+world,ListColour(regionId==0),indent,null)){NavigateMap(markers.World);mapHeaderList=0;}
                foreach(var choice in regions)
                    if(HeaderEntry($"Region{choice.MapId}","> "+MapName(choice),ListColour(choice.MapId==regionId),indent,null)){NavigateMap(choice.MapId);mapHeaderList=0;}
            }
            if(HeaderEntry("Zone",zoneName,UI.FfxivTheme.Gold,0,mapHeaderList==2))mapHeaderList=mapHeaderList==2 ? 0:2;
            if(isZone)DrawFlightWing(map,live,false);
            if(mapHeaderList==2)
            {
                ListBackdrop(zones.Count+1);
                if(HeaderEntry("ZoneRegion","> --",ListColour(!isZone),indent,null)){if(regionId!=0)NavigateMap(regionId);mapHeaderList=0;}
                foreach(var entry in zones)
                {
                    var zone=entry[0];
                    if(HeaderEntry($"Zone{zone.MapId}","> "+MapName(zone),ListColour(entry.Contains(map)),indent,null)){NavigateMap(zone.MapId);mapHeaderList=0;}
                    DrawFlightWing(zone,live,false);
                }
            }
            if(floors.Count>1)
            {
                if(HeaderEntry("Floor",FloorName(map),UI.FfxivTheme.Gold,0,mapHeaderList==3))mapHeaderList=mapHeaderList==3 ? 0:3;
                if(mapHeaderList==3)
                {
                    ListBackdrop(floors.Count);
                    foreach(var floor in floors)
                        if(HeaderEntry($"Floor{floor.MapId}","> "+FloorName(floor),ListColour(floor.MapId==map.MapId),indent,null)){NavigateMap(floor.MapId);mapHeaderList=0;}
                }
            }
            else if(isZone && map.Subzone.Length>0)HeaderEntry("Subzone","> "+map.Subzone,grey,indent,null,false);
        }
        ImGui.EndChild();
        mapOverlayHovered|=headerHovered;
        // A click elsewhere on the map rolls the list up.
        if(mapHeaderList!=0 && !headerHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))mapHeaderList=0;

        static Vector4 ListColour(bool current)=>current ? UI.FfxivTheme.Gold:new Vector4(0.93f,0.93f,0.93f,1);
        void ListBackdrop(int count)
        {
            var from=ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(from-new Vector2(pad/2,1),from+new Vector2(width-pad*1.5f,count*line),ImGui.GetColorU32(new Vector4(0,0,0,0.6f)),4);
        }
    }

    // Dark band behind the overlay names: solid under them, fading out to the right and below; no mouse input (the map stays
    // draggable under it).
    private static void HeaderBand(string id,Vector2 at,Vector2 size,float fade,Vector2 room)
    {
        ImGui.SetCursorPos(at);
        if(ImGui.BeginChild(id,Vector2.Max(Vector2.Min(size+new Vector2(fade),room),Vector2.One),false,ImGuiWindowFlags.NoBackground|ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoScrollbar))
        {
            var draw=ImGui.GetWindowDrawList();var min=ImGui.GetWindowPos();
            var dark=ImGui.GetColorU32(new Vector4(0,0,0,0.72f));var clear=ImGui.GetColorU32(new Vector4(0,0,0,0));
            draw.AddRectFilled(min,min+size,dark);
            draw.AddRectFilledMultiColor(min+new Vector2(size.X,0),min+new Vector2(size.X+fade,size.Y),dark,clear,clear,dark);
            draw.AddRectFilledMultiColor(min+new Vector2(0,size.Y),min+new Vector2(size.X,size.Y+fade),dark,dark,clear,clear);
            draw.AddRectFilledMultiColor(min+size,min+size+new Vector2(fade),dark,clear,clear,clear);
        }
        ImGui.EndChild();
    }

    // True while the mouse is over an overlay (tools, names): the map below then ignores it.
    private bool mapOverlayHovered;

    // Unrolled list of the overlay header (1 regions, 2 zones, 3 floors; 0 none).
    private int mapHeaderList;

    // One name of the overlay header: outlined text like the game map's (readable on any map), a hover glow, and for the
    // header lines (open is not null) the arrow that unrolls their list.
    private static bool HeaderEntry(string id,string text,Vector4 colour,float indent,bool? open,bool clickable=true)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()+indent);
        var at=ImGui.GetCursorScreenPos();var textSize=ImGui.CalcTextSize(text);var height=ImGui.GetTextLineHeight();
        var size=new Vector2(textSize.X+(open is null ? 0:height),height+2);
        var clicked=clickable && ImGui.InvisibleButton("##Header"+id,size);
        if(!clickable)ImGui.Dummy(size);
        var hovered=clickable && ImGui.IsItemHovered();
        var draw=ImGui.GetWindowDrawList();
        if(hovered)draw.AddRectFilled(at-new Vector2(3,1),at+size+new Vector2(3,0),ImGui.GetColorU32(new Vector4(1,1,1,0.12f)),3);
        var outline=ImGui.GetColorU32(new Vector4(0,0,0,0.9f));
        foreach(var offset in new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(0,-1),new Vector2(0,1)})draw.AddText(at+offset,outline,text);
        draw.AddText(at,ImGui.GetColorU32(hovered ? UI.FfxivTheme.Gold:colour),text);
        if(open is { } unrolled)
        {
            // ▼ closed, ▲ open, like the game's menu arrows.
            var centre=at+new Vector2(textSize.X+height*0.6f,height/2+1);var half=height*0.25f;
            var gold=ImGui.GetColorU32(UI.FfxivTheme.Gold);
            if(unrolled)draw.AddTriangleFilled(centre+new Vector2(-half,half*0.6f),centre+new Vector2(half,half*0.6f),centre+new Vector2(0,-half*0.6f),gold);
            else draw.AddTriangleFilled(centre+new Vector2(-half,-half*0.6f),centre+new Vector2(half,-half*0.6f),centre+new Vector2(0,half*0.6f),gold);
        }
        return clicked;
    }

    private MapDefinition? ResolveMap(MapCatalog catalog,uint id)=>id==0 ? null:catalog.Find(id) ?? mapMarkers?.Overview(id);
    private static string MapName(MapDefinition map)=>map.Zone.Length>0 ? map.Zone:map.Region;

    private void DrawMapMenus(MapCatalog catalog,MapDefinition? map,MapLiveState? live)
    {
        if(mapMarkers is null || map is null){ImGui.TextColored(Accent,localization.Get("MapView.NoMap"));return;}
        var regionId=mapMarkers.RegionOf(map);
        var regionNames=mapMarkers.Regions.Select(id=>ResolveMap(catalog,id)).OfType<MapDefinition>().Select(MapName).Append(localization.Get("MapView.World"));
        ImGui.SetNextItemWidth(MenuWidth(regionNames,false));
        if(ImGui.BeginCombo("##MapRegion",ResolveMap(catalog,regionId) is { } region ? MapName(region):localization.Get("MapView.World")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("MapView.World"),regionId==0))NavigateMap(mapMarkers.World);
                foreach(var id in mapMarkers.Regions)
                    if(ResolveMap(catalog,id) is { } choice && ImGui.Selectable($"{MapName(choice)}##Region{id}",id==regionId))NavigateMap(id);
            }
            finally{ImGui.EndCombo();}
        }
        Tip("MapView.RegionTip");
        ImGui.SameLine();
        var isZone=map.TerritoryId!=0;
        var zones=MapMenus.Zones(catalog.Entries,ResolveMap(catalog,regionId));
        var floors=zones.FirstOrDefault(entry=>entry.Any(floor=>floor.MapId==map.MapId)) ?? [];
        ImGui.SetNextItemWidth(MenuWidth(zones.Select(entry=>MapName(entry[0])).Append(isZone ? MapName(map):"--"),true));
        var zoneMenu=ImGui.BeginCombo("##MapZone",isZone ? MapName(map):"--");Tip("MapView.ZoneTip");
        if(zoneMenu)
        {
            try
            {
                if(ImGui.Selectable("--##ZoneRegion",!isZone) && regionId!=0)NavigateMap(regionId);
                foreach(var entry in zones)
                {
                    var zone=entry[0];
                    if(ImGui.Selectable($"{MapName(zone)}##Zone{zone.MapId}",entry.Contains(map)))NavigateMap(zone.MapId);
                    DrawFlightWing(zone,live,true);
                }
            }
            finally{ImGui.EndCombo();}
        }
        if(isZone)DrawFlightWing(map,live,false);
        // Floors of a multi-level map (dungeons), like the game's sub-map menu; a single map shows its subzone name instead.
        if(floors.Count>1)
        {
            ImGui.SameLine();ImGui.SetNextItemWidth(MenuWidth(floors.Select(floor=>floor.Subzone.Length>0 ? floor.Subzone:floor.MapIndex.ToString()),false));
            if(ImGui.BeginCombo("##MapFloor",map.Subzone.Length>0 ? map.Subzone:$"{map.MapIndex}"))
            {
                try{foreach(var floor in floors)if(ImGui.Selectable($"{(floor.Subzone.Length>0 ? floor.Subzone:floor.MapIndex.ToString())}##Floor{floor.MapId}",floor.MapId==map.MapId))NavigateMap(floor.MapId);}
                finally{ImGui.EndCombo();}
            }
            Tip("MapView.FloorTip");
        }
        else if(isZone && map.Subzone.Length>0){ImGui.SameLine();ImGui.TextDisabled(map.Subzone);}
    }

    // Menu as wide as its longest entry plus the arrow (and the wing column for zones), so names are never cut or padded.
    private static float MenuWidth(IEnumerable<string> names,bool wing)
    {
        var text=names.Select(name=>ImGui.CalcTextSize(name).X).DefaultIfEmpty(40).Max();
        var style=ImGui.GetStyle();
        return text+style.FramePadding.X*2+ImGui.GetFrameHeight()+(wing ? 26:0)+style.ItemInnerSpacing.X;
    }

    // A wing after the zone name: coloured when flying is unlocked, greyed with the current count otherwise; nothing when the
    // zone has no aether currents (cities, instances).
    private void DrawFlightWing(MapDefinition zone,MapLiveState? live,bool inMenu)
    {
        if(live?.Flights?.GetValueOrDefault(zone.TerritoryId) is not { Total:>0 } flight)return;
        ImGui.SameLine(inMenu ? ImGui.GetWindowContentRegionMax().X-22:0,inMenu ? -1:6);
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(flight.Complete ? Accent:Muted,FontAwesomeIcon.Feather.ToIconString());}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(flight.Complete ? localization.Get("MapView.FlightYes"):localization.Format("MapView.FlightPartial",flight.Unlocked,flight.Total));
    }

    // Vertical tool column like the game map buttons.
    private float DrawMapTools(MapDefinition? map,ImGuiViewportPtr viewport,(Vector2 Size,Vector2 Position) window,bool overlay=false,float maxHeight=0)
    {
        // 0.305 (user feedback: the column was cut in small windows, the zoom % lost): the column fits the room it has — the zoom
        // slider shrinks, then goes, then the buttons wrap into more columns. Over the map (0.303) it is only as tall as its
        // buttons, so the rest of the map stays draggable.
        var style=ImGui.GetStyle();var step=26+style.ItemSpacing.Y;var label=ImGui.GetTextLineHeightWithSpacing();
        var room=maxHeight>0 ? maxHeight:ImGui.GetContentRegionAvail().Y;
        var slider=Math.Clamp(room-8*step-label-2*style.ItemSpacing.Y-style.WindowPadding.Y*2,0,80);
        if(slider<30)slider=0;
        const int buttons=8;
        var perColumn=slider>0 ? buttons:Math.Clamp((int)((room-label-style.WindowPadding.Y*2)/step),1,buttons);
        var columns=(buttons+perColumn-1)/perColumn;
        var width=34+(columns-1)*30;
        var height=!overlay ? 0:slider>0 ? buttons*step+slider+label+2*style.ItemSpacing.Y+style.WindowPadding.Y*2
            :Math.Min(perColumn,buttons)*step+label+style.WindowPadding.Y*2;
        if(!ImGui.BeginChild("MapTools",new Vector2(width,height),false,ImGuiWindowFlags.NoScrollbar|(overlay ? ImGuiWindowFlags.NoBackground:ImGuiWindowFlags.None))){ImGui.EndChild();return width;}
        try
        {
            if(overlay)mapOverlayHovered|=ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows|ImGuiHoveredFlags.AllowWhenBlockedByPopup|ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            var left=ImGui.GetCursorPosX();var top=ImGui.GetCursorPosY();var index=0;
            void Place()
            {
                if(slider<=0)ImGui.SetCursorPos(new Vector2(left+index/perColumn*30,top+index%perColumn*step));
                index++;
            }
            var parent=map is null || mapMarkers is null ? 0:mapMarkers.Parent(map);
            Place();if(ToolButton(FontAwesomeIcon.ArrowUp,"MapView.Up",parent!=0))NavigateMap(parent);
            Place();if(ToolButton(FontAwesomeIcon.Crosshairs,"MapView.Follow",true,mapViewFollow))mapViewFollow=!mapViewFollow;
            Place();if(ToolButton(FontAwesomeIcon.ExpandArrowsAlt,"MapView.Fit",true)){mapFitRequested=true;mapViewFollow=false;}
            Place();
            if(ToolButton(mapRestore is null ? FontAwesomeIcon.WindowMaximize:FontAwesomeIcon.WindowRestore,mapRestore is null ? "MapView.Enlarge":"MapView.Restore",true))
            {
                if(mapRestore is { } previous){mapResize=previous;mapRestore=null;}
                else{mapRestore=window;var size=viewport.WorkSize*0.9f;mapResize=(size,viewport.WorkPos+(viewport.WorkSize-size)/2);}
                mapFitDelay=2;
            }
            Place();if(ToolButton(FontAwesomeIcon.Map,"MapView.OpenGameMap",openGameMap is not null))openGameMap?.Invoke();
            Place();if(ToolButton(FontAwesomeIcon.Cog,"MapView.Settings",true,ImGui.IsPopupOpen("MapViewSettings")))ImGui.OpenPopup("MapViewSettings");
            DrawMapSettings(map);
            if(slider>0)ImGui.Spacing();
            Place();if(ToolButton(FontAwesomeIcon.Plus,"MapView.ZoomIn",true))ZoomTo(mapZoom*1.25f);
            // Slider centred in the column (when there is room for it), the zoom percentage written under the − button.
            if(slider>0)
            {
                var percent=mapZoom*100;
                ImGui.SetCursorPosX(left+Math.Max(0,(26-16)/2f));
                if(ImGui.VSliderFloat("##MapZoom",new Vector2(16,slider),ref percent,15,400,""))ZoomTo(percent/100);
                Tip("MapView.ZoomTip");
            }
            Place();if(ToolButton(FontAwesomeIcon.Minus,"MapView.ZoomOut",true))ZoomTo(mapZoom/1.25f);
            var text=$"{mapZoom*100:0}%";
            var lastColumn=slider>0 ? 0:(buttons-1)/perColumn;
            if(slider<=0)ImGui.SetCursorPosY(top+((buttons-1)%perColumn+1)*step);
            ImGui.SetCursorPosX(Math.Max(0,left+lastColumn*30+(26-ImGui.CalcTextSize(text).X)/2));
            if(overlay)
            {
                // 0.308 (user feedback: nearly invisible on the map): dark backdrop like the names' band, light text.
                var at=ImGui.GetCursorScreenPos();var size=ImGui.CalcTextSize(text);
                ImGui.GetWindowDrawList().AddRectFilled(at-new Vector2(3,1),at+size+new Vector2(3,1),ImGui.GetColorU32(new Vector4(0,0,0,0.72f)),3);
                ImGui.TextColored(new Vector4(0.93f,0.93f,0.93f,1),text);
            }
            else ImGui.TextDisabled(text);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MapView.ZoomLevel"));
        }
        finally{ImGui.EndChild();}
        return width;
    }
    // Mappy-like display settings, saved at each change; every layer can be turned off.
    private void DrawMapSettings(MapDefinition? map)
    {
        if(!ImGui.BeginPopup("MapViewSettings"))return;
        // The save error under the help line; Settings › Map writes it under its tabs already.
        try{DrawMapSettingsBody(map);if(saveError is { } failed)ImGui.TextColored(UI.FfxivTheme.Error,failed);}
        finally{ImGui.EndPopup();}
    }
    // Same body in the map's cog popup and in Settings › Map (0.153: every setting reachable from the Settings page).
    private void DrawMapSettingsBody(MapDefinition? map)
    {
        {
            var settings=configuration.MapView;var changed=false;
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionGame"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowFixedIcons",settings.ShowFixedIcons,value=>settings.ShowFixedIcons=value,MapDefaults.ShowFixedIcons);
                changed|=SettingBox("MapView.TeleportOnClick",settings.TeleportOnClick,value=>settings.TeleportOnClick=value,MapDefaults.TeleportOnClick);
                changed|=SettingBox("MapView.ChatLinks",settings.ChatLinks,value=>settings.ChatLinks=value,MapDefaults.ChatLinks);
                changed|=SettingBox("MapView.ShowPlaceNames",settings.ShowPlaceNames,value=>settings.ShowPlaceNames=value,MapDefaults.ShowPlaceNames);
                var names=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.PlaceNameScale"),ref names,50,150,"%.0f %%")){settings.PlaceNameScale=names/100;changed=true;}
                Tip("SettingTips.PlaceNameScale");
                if(DefaultButton("PlaceNameScale",settings.PlaceNameScale!=MapDefaults.PlaceNameScale)){settings.PlaceNameScale=MapDefaults.PlaceNameScale;changed=true;}
                changed|=SettingBox("MapView.ShowFlag",settings.ShowFlag,value=>settings.ShowFlag=value,MapDefaults.ShowFlag);
                changed|=SettingBox("MapView.ShowBookmarks",settings.ShowBookmarks,value=>settings.ShowBookmarks=value,MapDefaults.ShowBookmarks);
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionQuests"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowQuests",settings.ShowQuests,value=>settings.ShowQuests=value,MapDefaults.ShowQuests);
                changed|=SettingBox("MapView.ShowQuestAreas",settings.ShowQuestAreas,value=>settings.ShowQuestAreas=value,MapDefaults.ShowQuestAreas);
                changed|=SettingBox("MapView.QuestTooltips",settings.QuestTooltips,value=>settings.QuestTooltips=value,MapDefaults.QuestTooltips);
                changed|=SettingBox("MapView.ShowFates",settings.ShowFates,value=>settings.ShowFates=value,MapDefaults.ShowFates);
                changed|=SettingBox("MapView.ShowSightseeing",settings.ShowSightseeing,value=>settings.ShowSightseeing=value,MapDefaults.ShowSightseeing);
                changed|=SettingBox("MapView.ShowAetherCurrents",settings.ShowAetherCurrents,value=>settings.ShowAetherCurrents=value,MapDefaults.ShowAetherCurrents);
                changed|=SettingBox("MapView.HideCollected",settings.HideCollected,value=>settings.HideCollected=value,MapDefaults.HideCollected);
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionNearby"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowTreasure",settings.ShowTreasure,value=>settings.ShowTreasure=value,MapDefaults.ShowTreasure);
                changed|=SettingBox("MapView.ShowVendors",settings.ShowVendors,value=>settings.ShowVendors=value,MapDefaults.ShowVendors);
                changed|=SettingBox("MapView.ShowGathering",settings.ShowGathering,value=>settings.ShowGathering=value,MapDefaults.ShowGathering);
                changed|=SettingBox("MapView.ShowHuntMarks",settings.ShowHuntMarks,value=>settings.ShowHuntMarks=value,MapDefaults.ShowHuntMarks);
                changed|=SettingBox("MapView.GatheringLabels",settings.GatheringLabels,value=>settings.GatheringLabels=value,MapDefaults.GatheringLabels);
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionPlayer")))
            {
                changed|=SettingBox("MapView.ShowPlayer",settings.ShowPlayer,value=>settings.ShowPlayer=value,MapDefaults.ShowPlayer);
                changed|=SettingBox("MapView.ShowParty",settings.ShowParty,value=>settings.ShowParty=value,MapDefaults.ShowParty);
                changed|=SettingBox("MapView.ShowViewCone",settings.ShowViewCone,value=>settings.ShowViewCone=value,MapDefaults.ShowViewCone);
                changed|=SettingBox("MapView.ShowEdgeMarkers",settings.ShowEdgeMarkers,value=>settings.ShowEdgeMarkers=value,MapDefaults.ShowEdgeMarkers);
                ImGui.BeginDisabled(!settings.ShowEdgeMarkers);
                try
                {
                    ImGui.Indent();
                    changed|=SettingBox("MapView.EdgePlayerOn",settings.EdgePlayer,value=>settings.EdgePlayer=value,MapDefaults.EdgePlayer);
                    changed|=SettingBox("MapView.EdgeFlagOn",settings.EdgeFlag,value=>settings.EdgeFlag=value,MapDefaults.EdgeFlag);
                    changed|=SettingBox("MapView.EdgeTargetOn",settings.EdgeTarget,value=>settings.EdgeTarget=value,MapDefaults.EdgeTarget);
                    changed|=SettingBox("MapView.EdgeQuestsOn",settings.EdgeQuests,value=>settings.EdgeQuests=value,MapDefaults.EdgeQuests);
                    changed|=SettingBox("MapView.EdgeEventsOn",settings.EdgeEvents,value=>settings.EdgeEvents=value,MapDefaults.EdgeEvents);
                    changed|=SettingBox("MapView.EdgeFatesOn",settings.EdgeFates,value=>settings.EdgeFates=value,MapDefaults.EdgeFates);
                    changed|=SettingBox("MapView.EdgeGatheringOn",settings.EdgeGathering,value=>settings.EdgeGathering=value,MapDefaults.EdgeGathering);
                    changed|=SettingBox("MapView.EdgePartyOn",settings.EdgeParty,value=>settings.EdgeParty=value,MapDefaults.EdgeParty);
                    ImGui.Unindent();
                }
                finally{ImGui.EndDisabled();}
                // 0.288 (user request): marker size, cone length and cone opacity, each with its tooltip and « ↺ ».
                var marker=MapViewSettings.ClampMarker(settings.PlayerIconScale)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.PlayerIconScale"),ref marker,50,300,"%.0f %%")){settings.PlayerIconScale=marker/100;changed=true;}
                Tip("SettingTips.PlayerIconScale");
                if(DefaultButton("PlayerIconScale",settings.PlayerIconScale!=MapDefaults.PlayerIconScale)){settings.PlayerIconScale=MapDefaults.PlayerIconScale;changed=true;}
                ImGui.BeginDisabled(!settings.ShowViewCone);
                try
                {
                    var cone=MapViewSettings.ClampMarker(settings.ViewConeScale)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.ViewConeScale"),ref cone,50,300,"%.0f %%")){settings.ViewConeScale=cone/100;changed=true;}
                    Tip("SettingTips.ViewConeScale");
                    if(DefaultButton("ViewConeScale",settings.ViewConeScale!=MapDefaults.ViewConeScale)){settings.ViewConeScale=MapDefaults.ViewConeScale;changed=true;}
                    var coneOpacity=MapViewSettings.ClampConeOpacity(settings.ViewConeOpacity)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.ViewConeOpacity"),ref coneOpacity,0,100,"%.0f %%")){settings.ViewConeOpacity=coneOpacity/100;changed=true;}
                    Tip("SettingTips.ViewConeOpacity");
                    if(DefaultButton("ViewConeOpacity",settings.ViewConeOpacity!=MapDefaults.ViewConeOpacity)){settings.ViewConeOpacity=MapDefaults.ViewConeOpacity;changed=true;}
                    var coneColour=settings.ViewConeColor ?? new Vector3(UI.FfxivTheme.Gold.X,UI.FfxivTheme.Gold.Y,UI.FfxivTheme.Gold.Z);
                    if(ImGui.ColorEdit3(localization.Get("MapView.ViewConeColor")+"##ViewConeColor",ref coneColour,ImGuiColorEditFlags.NoInputs)){settings.ViewConeColor=coneColour;changed=true;}
                    Tip("SettingTips.ViewConeColor");
                    if(DefaultButton("ViewConeColor",settings.ViewConeColor is not null)){settings.ViewConeColor=null;changed=true;}
                }
                finally{ImGui.EndDisabled();}
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.Display")))
            {
                changed|=SettingBox("MapView.OverlayControls",settings.OverlayControls,value=>settings.OverlayControls=value,MapDefaults.OverlayControls);
                ImGui.BeginDisabled(!settings.OverlayControls);
                try{ImGui.Indent();changed|=SettingBox("MapView.OverlayZoneName",settings.OverlayZoneName,value=>settings.OverlayZoneName=value,MapDefaults.OverlayZoneName);ImGui.Unindent();}
                finally{ImGui.EndDisabled();}
                var scale=MapViewSettings.ClampScale(settings.IconScale)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.IconScale"),ref scale,50,200,"%.0f %%")){settings.IconScale=scale/100;changed=true;}
                Tip("SettingTips.IconScale");
                if(DefaultButton("IconScale",settings.IconScale!=MapDefaults.IconScale)){settings.IconScale=MapDefaults.IconScale;changed=true;}
                var follow=MapViewSettings.ClampFollow(settings.ZoomFollow)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.ZoomFollow"),ref follow,0,100,"%.0f %%")){settings.ZoomFollow=follow/100;changed=true;}
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MapView.ZoomFollowHelp"));
                if(DefaultButton("ZoomFollow",settings.ZoomFollow!=MapDefaults.ZoomFollow)){settings.ZoomFollow=MapDefaults.ZoomFollow;changed=true;}
                var opacity=MapViewSettings.ClampOpacity(settings.Opacity)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.Opacity"),ref opacity,20,100,"%.0f %%")){settings.Opacity=opacity/100;changed=true;}
                Tip("SettingTips.Opacity");
                if(DefaultButton("Opacity",settings.Opacity!=MapDefaults.Opacity)){settings.Opacity=MapDefaults.Opacity;changed=true;}
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionAreas")))
            {
                changed|=AreaSetting("MapView.AreaQuests",settings.ShowQuestAreas,value=>settings.ShowQuestAreas=value,settings.QuestAreaColor,value=>settings.QuestAreaColor=value,MapDefaults.ShowQuestAreas,MapDefaults.QuestAreaColor);
                changed|=AreaSetting("MapView.AreaEvents",settings.ShowEventAreas,value=>settings.ShowEventAreas=value,settings.EventAreaColor,value=>settings.EventAreaColor=value,MapDefaults.ShowEventAreas,MapDefaults.EventAreaColor);
                changed|=AreaSetting("MapView.AreaFates",settings.ShowFateAreas,value=>settings.ShowFateAreas=value,settings.FateAreaColor,value=>settings.FateAreaColor=value,MapDefaults.ShowFateAreas,MapDefaults.FateAreaColor);
                changed|=AreaSetting("MapView.AreaGathering",settings.ShowGatheringAreas,value=>settings.ShowGatheringAreas=value,settings.GatheringAreaColor,value=>settings.GatheringAreaColor=value,MapDefaults.ShowGatheringAreas,MapDefaults.GatheringAreaColor);
            }
            if(map is not null && TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionIcons")))changed|=DrawIconSizes(map,settings);
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.Window")))
            {
                changed|=SettingBox("MapView.ReplaceGameMap",settings.ReplaceGameMap,value=>settings.ReplaceGameMap=value,MapDefaults.ReplaceGameMap);
                changed|=SettingBox("MapView.Locked",settings.Locked,value=>settings.Locked=value,MapDefaults.Locked);
                changed|=SettingBox("MapView.HideTitleBar",settings.HideTitleBar,value=>settings.HideTitleBar=value,MapDefaults.HideTitleBar);
                changed|=SettingBox("MapView.HideInCombat",settings.HideInCombat,value=>settings.HideInCombat=value,MapDefaults.HideInCombat);
                changed|=SettingBox("MapView.HideInDuty",settings.HideInDuty,value=>settings.HideInDuty=value,MapDefaults.HideInDuty);
            }
            ImGui.TextDisabled(localization.Get("MapView.SettingsHelp"));
            if(changed)
            {
                // 0.328 (map audit #41): a failed save is said (Common.SaveError), like the other settings.
                try{save();saveError=null;}
                catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save map settings.");}
            }
        }
    }
    // One row per kind of game icon on the shown map (fixed icons, quest/event markers, FATEs, flag): visible box, size, priority.
    // Scope (0.119): « All maps » edits the global table; « This map » writes overrides kept for this map only, cleared by
    // « Back to global ». Rows show the effective value of the chosen scope; rows are listed by priority, highest first.
    private bool mapIconScopeThisMap;private string mapIconFilter="";private uint[]? questIconsWithImage;
    private bool DrawIconSizes(MapDefinition map,MapViewSettings settings)
    {
        var icons=new HashSet<uint>((mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.Icon!=0).Select(marker=>marker.Icon));
        // Where each icon comes from, for the categories (0.212).
        var eventIcons=new HashSet<uint>();var fateIcons=new HashSet<uint>();var gatheringIcons=new HashSet<uint>();var targetIcons=new HashSet<uint>();
        if(mapLive?.State is { } live)
        {
            foreach(var marker in live.Events ?? [])if(marker.MapId==map.MapId){icons.Add(marker.Icon);eventIcons.Add(marker.Icon);}
            if(live.MapId==map.MapId)foreach(var fate in live.Fates ?? [])if(fate.Icon!=0){icons.Add(fate.Icon);fateIcons.Add(fate.Icon);}
        }
        if(settings.ShowGathering)foreach(var point in MapGatheringPoints(map))if(point.Icon!=0){icons.Add(point.Icon);gatheringIcons.Add(point.Icon);}
        if(mapGameTarget is { } target && target.MapId==map.MapId){icons.Add(target.Icon);targetIcons.Add(target.Icon);}
        if(settings.ShowVendors){icons.Add(VendorIcon);icons.Add(ExchangeIcon);}
        icons.Add(FlagIcon);
        // Tensho's own layers (0.210): treasure coffers (game icon), vistas, aether currents, nearby gathering points, party members.
        if(settings.ShowTreasure)icons.Add(TreasureIcon);
        if(settings.ShowSightseeing)icons.Add(GlyphVista);
        if(settings.ShowAetherCurrents)icons.Add(GlyphCurrent);
        if(settings.ShowGathering)icons.Add(GlyphNearbyGathering);
        if(settings.ShowParty)icons.Add(GlyphParty);
        if(settings.ShowBookmarks)icons.Add(GlyphBookmark);
        if(settings.ShowTreasure)icons.Add(GlyphTreasureMap);
        // Every quest marker icon of the game (main scenario, side, feature, repeatable… available or locked), when quests are shown;
        // only icons whose image exists in the game files.
        if(settings.ShowQuests && mapMarkers is not null)
        {
            questIconsWithImage??=mapMarkers.QuestIcons.Keys.Where(quest=>textures.TryGetIconPath(new GameIconLookup(quest),out _)).ToArray();
            foreach(var quest in questIconsWithImage)icons.Add(quest);
        }
        // « All maps »: every map icon kind the game names (MapSymbol), not only those of the shown map (0.128).
        if(!mapIconScopeThisMap && mapMarkers is not null)foreach(var symbol in mapMarkers.SymbolIcons)icons.Add(symbol);
        var changed=false;
        if(ImGui.RadioButton(localization.Get("MapView.IconsAllMaps"),!mapIconScopeThisMap))mapIconScopeThisMap=false;
        Tip("MapView.IconsAllMapsTip");
        ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("MapView.IconsThisMap"),mapIconScopeThisMap))mapIconScopeThisMap=true;
        Tip("MapView.IconsThisMapTip");
        var scope=mapIconScopeThisMap ? map.MapId:0;
        if(mapIconScopeThisMap)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(localization.Get(settings.HasMapOverrides(map.MapId) ? "MapView.IconsOwn":"MapView.IconsInherited"));
        }
        if(ImGui.SmallButton(localization.Get(mapIconScopeThisMap ? "MapView.IconsBackToGlobal":"MapView.IconsReset"))){settings.Reset(scope);changed=true;}
        if(mapIconScopeThisMap)Tip("MapView.IconsBackToGlobalTip");else Tip("MapView.IconsResetTip");
        ImGui.TextDisabled(localization.Get("MapView.IconsPriorityHelp"));
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##IconFilter",localization.Get("MapView.IconsFilter"),ref mapIconFilter,64);
        Tip("MapView.IconsFilterTip");
        // Categories (0.212): Tensho layers, quests, objectives and events, FATEs, gathering, shops, flag and target, places of the game, other.
        int Category(uint icon)=>TenshoGlyph(icon) is not null || icon==TreasureIcon ? 0
            :mapMarkers?.QuestIcons.ContainsKey(icon)==true ? 1
            :eventIcons.Contains(icon) ? 2
            :fateIcons.Contains(icon) ? 3
            :gatheringIcons.Contains(icon) ? 4
            :icon is VendorIcon or ExchangeIcon or 63851 ? 5
            :icon==FlagIcon || targetIcons.Contains(icon) ? 6
            :mapMarkers?.SymbolName(icon) is { Length:>0 } ? 7:8;
        var rows=icons.Select(icon=>(Icon:icon,Name:IconRowName(icon)))
            .Where(row=>mapIconFilter.Length==0 || row.Name.Contains(mapIconFilter,StringComparison.CurrentCultureIgnoreCase))
            .GroupBy(row=>Category(row.Icon)).OrderBy(group=>group.Key);
        foreach(var category in rows)
        {
            var open=ImGui.TreeNodeEx(localization.Format("MapView.IconCategory"+category.Key,category.Count())+$"###IconCategory{category.Key}",
                mapIconFilter.Length>0 || category.Key is 0 or 6 ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None);
            if(!open)continue;
            try
            {
                if(ImGui.SmallButton(localization.Get("MapView.IconCategoryShow")+$"##Show{category.Key}")){foreach(var row in category)settings.SetSize(row.Icon,scope,1);changed=true;}
                Tip("MapView.IconCategoryShowTip");
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("MapView.IconCategoryHide")+$"##Hide{category.Key}")){foreach(var row in category)settings.SetSize(row.Icon,scope,0);changed=true;}
                Tip("MapView.IconCategoryHideTip");
                foreach(var (icon,iconName) in category.OrderByDescending(row=>settings.PriorityOf(row.Icon,map.MapId)).ThenBy(row=>row.Icon))
                    changed|=DrawIconSizeRow(icon,iconName,map,settings,scope);
            }
            finally{ImGui.TreePop();}
        }
        return changed;
    }

    // One icon row: visible box, image, size, priority, name.
    private bool DrawIconSizeRow(uint icon,string iconName,MapDefinition map,MapViewSettings settings,uint scope)
    {
        var changed=false;
        {
            ImGui.PushID((int)icon);
            try
            {
                var own=scope==0 ? settings.SizeOf(icon):settings.SizeOf(icon,map.MapId);var visible=own>0;
                if(ImGui.Checkbox("##Visible",ref visible)){settings.SetSize(icon,scope,visible ? 1:0);changed=true;}
                Tip("MapView.IconVisibleHelp");
                ImGui.SameLine();
                if(TenshoGlyph(icon) is { } glyph)
                {
                    // Same square as the game icons (0.212: the narrower glyph shifted the slider of these rows).
                    var square=ImGui.GetFrameHeight();
                    ImGui.Dummy(new Vector2(square));
                    var corner=ImGui.GetItemRectMin();var glyphSize=square*0.8f;
                    ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont,glyphSize,corner+new Vector2((square-glyphSize)/2),ImGui.GetColorU32(glyph.Colour),glyph.Symbol.ToIconString());
                }
                else if(textures.TryGetFromGameIcon(new GameIconLookup(icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                    ImGui.Image(image.Handle,new Vector2(ImGui.GetFrameHeight()));
                else ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                ImGui.SameLine();
                var percent=(visible ? own:1)*100;
                ImGui.BeginDisabled(!visible);
                ImGui.SetNextItemWidth(110);
                if(ImGui.SliderFloat("##Size",ref percent,50,200,"%.0f %%")){settings.SetSize(icon,scope,percent/100);changed=true;}
                ImGui.EndDisabled();
                Tip("MapView.IconSizeTip");
                ImGui.SameLine();
                // 0.328 (map audit #4): Tensho layers, coffers and the flag are drawn above the game icons in a fixed order, outside the
                // sorted list; priority and the own menu do not apply to them, so both are greyed and say why.
                var layer=TenshoGlyph(icon) is not null || icon is TreasureIcon or FlagIcon;
                var priority=scope==0 ? settings.PriorityOf(icon,0):settings.PriorityOf(icon,map.MapId);
                ImGui.BeginDisabled(layer);
                try{ImGui.SetNextItemWidth(70);if(ImGui.InputInt("##Priority",ref priority,1)){settings.SetPriority(icon,scope,priority);changed=true;}}
                finally{ImGui.EndDisabled();}
                if(layer)Tip("MapView.IconsLayerFixed");
                else if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MapView.IconsPriority"));
                ImGui.SameLine();
                // R-04: right click on this icon = its own menu (on) or the general map menu; one list for all maps.
                // 0.298 (user request): a mouse button instead of a second checkbox, so the two settings of the row are not confused.
                var ownMenu=!settings.RightClickIgnored(icon);
                ImGui.BeginDisabled(layer);
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushStyleColor(ImGuiCol.Text,ownMenu ? UI.FfxivTheme.Gold:UI.FfxivTheme.TextMuted);
                bool toggled;
                try{toggled=ImGui.SmallButton(FontAwesomeIcon.MousePointer.ToIconString()+"##OwnMenu");}
                finally{ImGui.PopStyleColor();ImGui.PopFont();ImGui.EndDisabled();}
                if(toggled && !layer)
                {
                    if(ownMenu)(settings.RightClickIgnore??=[]).Add(icon);else settings.RightClickIgnore?.Remove(icon);
                    changed=true;
                }
                if(layer)Tip("MapView.IconsLayerFixed");
                else if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(ownMenu ? "MapView.IconsOwnMenuOn":"MapView.IconsOwnMenuOff")+"\n"+localization.Get("MapView.IconsOwnMenu"));
                ImGui.SameLine();
                ImGui.TextUnformatted(iconName);
                if(TenshoGlyph(icon) is not null && ImGui.IsItemHovered())WrappedTooltip(localization.Get("MapView.TenshoIconHelp"));
                // 0.328 (map audit #14/#40): the row's « ↺ ». All maps: this icon's default size, priority and right-click mode. This map:
                // its overrides go, so it follows the global settings again (the right-click mode has one value for all maps).
                var differs=scope==0
                    ? settings.SizeOf(icon)!=MapViewSettings.DefaultIconSizes.GetValueOrDefault(icon,1f) || settings.PriorityOf(icon,0)!=MapViewSettings.DefaultIconPriorities.GetValueOrDefault(icon)
                      || settings.RightClickIgnored(icon)
                    :settings.MapIconSizes?.GetValueOrDefault(map.MapId)?.ContainsKey(icon)==true || settings.MapIconPriorities?.GetValueOrDefault(map.MapId)?.ContainsKey(icon)==true;
                if(DefaultButton("IconRow",differs))
                {
                    if(scope==0)
                    {
                        if(MapViewSettings.DefaultIconSizes.TryGetValue(icon,out var size))settings.SetSize(icon,0,size);else settings.IconSizes?.Remove(icon);
                        if(MapViewSettings.DefaultIconPriorities.TryGetValue(icon,out var rank))settings.SetPriority(icon,0,rank);else settings.IconPriorities?.Remove(icon);
                        settings.RightClickIgnore?.Remove(icon);
                    }
                    else
                    {
                        if(settings.MapIconSizes?.GetValueOrDefault(map.MapId) is { } sizes && sizes.Remove(icon) && sizes.Count==0)settings.MapIconSizes.Remove(map.MapId);
                        if(settings.MapIconPriorities?.GetValueOrDefault(map.MapId) is { } ranks && ranks.Remove(icon) && ranks.Count==0)settings.MapIconPriorities.Remove(map.MapId);
                    }
                    changed=true;
                }
            }
            finally{ImGui.PopID();}
        }
        return changed;
    }
    // 0.324: the colour has its tooltip and the row a « ↺ » that puts both the switch and the colour back to their defaults.
    private bool AreaSetting(string key,bool shown,Action<bool> applyShown,Vector4 colour,Action<Vector4> applyColour,bool shownByDefault,Vector4 colourByDefault)
    {
        var changed=false;
        if(ImGui.Checkbox(localization.Get(key)+"##Area"+key,ref shown)){applyShown(shown);changed=true;}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(key+"Help"));
        ImGui.SameLine(ImGui.GetFontSize()*14);
        if(ImGui.ColorEdit4("##Colour"+key,ref colour,ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf)){applyColour(colour);changed=true;}
        Tip("SettingTips.AreaColour");
        if(DefaultButton("Area"+key,shown!=shownByDefault || colour!=colourByDefault)){applyShown(shownByDefault);applyColour(colourByDefault);changed=true;}
        return changed;
    }
    // 0.262: a map setting box with its help (key + « Help » when written) and the « ↺ » button once it differs from the default.
    private bool SettingBox(string key,bool value,Action<bool> apply,bool? fallback=null)
    {
        var changed=ImGui.Checkbox(localization.Get(key)+"###"+key,ref value);
        if(changed)apply(value);
        // 0.328 (map audit #39): also when greyed (edge kinds, zone name), whose help says what they depend on.
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && localization.Get(key+"Help") is { } help && !help.StartsWith('['))WrappedTooltip(help);
        if(fallback is { } byDefault && DefaultButton(key,value!=byDefault)){apply(byDefault);changed=true;}
        return changed;
    }

    private bool ToolButton(FontAwesomeIcon icon,string tooltipKey,bool enabled,bool active=false)
    {
        ImGui.BeginDisabled(!enabled);
        if(active)ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.Button($"{icon.ToIconString()}##{tooltipKey}",new Vector2(26));}
        finally{ImGui.PopFont();if(active)ImGui.PopStyleColor();ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(tooltipKey));
        return clicked && enabled;
    }

    private (Vector2 Size,Vector2 Position)? mapResize;
    private (Vector2 Size,Vector2 Position)? mapRestore;
    private bool mapFitRequested;
    // Texture pixel to bring to the centre of the canvas on the next frame (flag placed from the Tensho map, 0.116).
    private Vector2? mapCenterRequest;
    // Enlarge/restore: the new window size only exists two frames later, so the fit waits for it (0.118, user report).
    private int mapFitDelay;
    // Quest, aetheryte or FATE the game asked to show (journal click), focused once the target map is drawn (0.118).
    private GameMapRequest? mapFocusRequest;private DateTimeOffset mapFocusUntil;
    // The game's temporary marker (quest or gathering target) is not an event marker: it is kept and drawn here (0.122) until the
    // next game request, since closing the game map would otherwise lose it.
    private (uint MapId,Vector2 World,uint Icon,uint QuestId)? mapGameTarget;
    private Quests.QuestState? mapGameTargetQuests;
    private DateTimeOffset mapGameTargetSince,mapGameTargetRefresh;

    // 0.243 (R-01): a journal target belongs to its quest; once that quest is no longer active (completed or abandoned), the
    // target goes. The quest list is otherwise read every 5 min, so a refresh is asked at most once a minute while such a target
    // is shown; only a list observed after the target was set (not the disk cache) may remove it. Nothing is scanned per frame.
    private void ExpireGameTarget()
    {
        if(mapGameTarget is not { QuestId: not 0 } target)return;
        var now=DateTimeOffset.UtcNow;
        if(now>=mapGameTargetRefresh){mapGameTargetRefresh=now.AddMinutes(1);quests.RequestRefresh();}
        var state=quests.State;
        if(ReferenceEquals(state,mapGameTargetQuests) || state.Loading || state.FromCache || state.Error is not null
           || state.ObservedAt is not { } observed || observed<=mapGameTargetSince)return;
        mapGameTargetQuests=state;
        var key=target.QuestId & 0xFFFF;
        if(!state.Entries.Any(entry=>entry.Progress==Quests.QuestProgress.Active && (entry.Id & 0xFFFF)==key))mapGameTarget=null;
    }
    private bool mapDragged;
    // 0.328 (map audit #3): the zoom buttons and slider keep the texture pixel at the centre of the view where it is (canvas size of
    // the last frame; the tool column is drawn before the canvas). Only the mouse wheel zoomed around a point before.
    private Vector2 mapCanvasSize;
    private void ZoomTo(float zoom){var centre=(mapCanvasSize/2-mapPan)/mapZoom;mapZoom=MapProjection.ClampZoom(zoom);mapPan=mapCanvasSize/2-centre*mapZoom;}
    // 0.328 (map audit #2): an error of the flag placement or of a map link is shown over the canvas only, never glued to the cursor,
    // and only for a few seconds after it happened (a new error text, or a failed flag placement from the Tensho map).
    private string? mapErrorSeen;private DateTimeOffset mapErrorUntil;
    private void FlagFromMap(MapDefinition map,float x,float y)=>PlaceFlagFromMapView(map,x,y);
    // Links from the tools (quests, gathering, FATEs): open the Tensho map on a world position, zoomed and centred, with the flag.
    private void ShowOnTenshoMap(MapDefinition map,Vector3 world,bool flag=true)
    {
        var pixel=MapProjection.PixelFromWorld(world,map);
        mapViewOpen=true;mapViewMapId=map.MapId;mapViewFollow=false;mapFitRequested=false;
        mapZoom=MapProjection.ClampZoom(Math.Max(mapZoom,1f));mapCenterRequest=pixel;
        if(!flag)return;
        var coordinate=MapProjection.CoordinateFromPixel(pixel,map);
        FlagFromMap(map,coordinate.X,coordinate.Y);
    }
    // Same, without a known position: the whole map.
    private void ShowMapOnTenshoMap(uint mapId){if(mapId==0)return;mapViewOpen=true;NavigateMap(mapId);}
    private void NavigateMap(uint mapId){if(mapId==0)return;mapViewMapId=mapId;mapViewFollow=false;mapFitRequested=true;}

    private void DrawMapCanvas(MapDefinition map,MapLiveState? live)
    {
        var origin=ImGui.GetCursorScreenPos();var size=Vector2.Max(ImGui.GetContentRegionAvail(),new Vector2(64));mapCanvasSize=size;
        ImGui.InvisibleButton("MapCanvas",size,ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
        var hovered=ImGui.IsItemHovered() && !mapOverlayHovered;var io=ImGui.GetIO();
        var player=live is not null && live.MapId==map.MapId ? MapProjection.PixelFromWorld(live.Position,map):(Vector2?)null;
        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))mapDragged=false;
        if(ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left)){mapPan+=io.MouseDelta;mapViewFollow=false;mapDragged=true;}
        if(hovered && io.MouseWheel!=0)
        {
            // Zoom around the cursor: the pixel under the mouse stays under the mouse.
            var before=(io.MousePos-origin-mapPan)/mapZoom;
            mapZoom=MapProjection.ClampZoom(mapZoom*(io.MouseWheel>0 ? 1.15f:1/1.15f));
            mapPan=io.MousePos-origin-before*mapZoom;
        }
        if(mapFitDelay>0 && --mapFitDelay==0)mapFitRequested=true;
        if(mapFocusRequest is { } focus)
        {
            // Like the game: the map zooms on what was asked. Markers can arrive a moment after the map opens, so the search
            // is retried for 1.5 s; nothing found on this map = whole map. Logged (/xllog) to check the quest links in game.
            if(FocusPixel(focus,map,live) is { } pixel)
            {
                mapGameTarget=focus.Target is { } world && focus.TargetIcon!=0 ? (map.MapId,world,focus.TargetIcon,focus.QuestId):null;
                mapGameTargetQuests=null;mapGameTargetSince=DateTimeOffset.UtcNow;mapGameTargetRefresh=default;
                mapFocusRequest=null;mapZoom=MapProjection.ClampZoom(Math.Max(mapZoom,1f));mapCenterRequest=pixel;mapViewFollow=false;mapFitRequested=false;
                log.Information("Map focus found on map {0} for quest {1}, aetheryte {2}, FATE {3}.",map.MapId,focus.QuestId,focus.AetheryteId,focus.FateId);
            }
            else if(DateTimeOffset.UtcNow>=mapFocusUntil)
            {
                mapFocusRequest=null;mapFitRequested=true;
                var seen=string.Join(" | ",(live?.Events ?? []).Where(marker=>marker.MapId==map.MapId).Take(12).Select(marker=>$"{marker.ObjectiveId}:{marker.Icon}:{marker.Tooltip}"));
                log.Information("Map focus not found on map {0} for quest {1} (journal {2}); markers: {3}",map.MapId,focus.QuestId,focus.QuestLog,seen);
            }
        }
        if(mapFitRequested)
        {
            // Fit: the whole 2048-pixel map inside the canvas, centred.
            mapZoom=MapProjection.ClampZoom(Math.Min(size.X,size.Y)/MapProjection.TextureSize);
            mapPan=(size-new Vector2(MapProjection.TextureSize*mapZoom))/2;mapFitRequested=false;
        }
        if(mapViewFollow && player is { } centred)mapPan=size/2-centred*mapZoom;
        if(mapCenterRequest is { } requested){mapPan=size/2-requested*mapZoom;mapCenterRequest=null;}
        var draw=ImGui.GetWindowDrawList();
        draw.PushClipRect(origin,origin+size,true);
        try
        {
            var settings=configuration.MapView;var opacity=MapViewSettings.ClampOpacity(settings.Opacity);
            draw.AddRectFilled(origin,origin+size,ImGui.GetColorU32(UI.FfxivTheme.Background with{W=opacity}));
            // 0.328 (map audit #25, R-14): a texture that failed to load says so instead of « Loading map… » forever.
            if(textures.GetFromGame(map.TexturePath).TryGetWrap(out var texture,out var textureFailure) && texture is not null)
                draw.AddImage(texture.Handle,origin+mapPan,origin+mapPan+new Vector2(MapProjection.TextureSize*mapZoom),Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(1,1,1,opacity)));
            else draw.AddText(origin+new Vector2(8),ImGui.GetColorU32(UI.FfxivTheme.TextMuted),localization.Get(textureFailure is not null ? "MapView.Unavailable":"MapView.Loading"));
            string? tooltip=null;MapLocalGatheringPoint? tooltipPoint=null;(MapLocalPosition Vista,bool? Done)? sightseeingTip=null;
            // Icons follow the map zoom exactly (32 px at 100 %, 0.113 at the user's request); 4 px floor only so they never vanish.
            // Each game icon can then be resized or hidden on its own (settings, « Icônes de cette carte »).
            // 0.153: zoom^follow (setting « Suivi du zoom ») so icons set for a close zoom stay readable when zoomed out.
            var zoomFactor=settings.ZoomFactor(mapZoom);
            var baseIcon=Math.Max(4,32*zoomFactor*MapViewSettings.ClampScale(settings.IconScale));
            var markers=settings.ShowFixedIcons || settings.ShowPlaceNames ? mapMarkers?.For(map.MarkerRange) ?? []:[];
            if(mapMarkers?.Failed(map.MarkerRange)==true)
                draw.AddText(origin+new Vector2(8,26),ImGui.GetColorU32(UI.FfxivTheme.TextMuted),localization.Get("MapView.MarkersUnavailable"));
            // Place names first, under the icons, in large outlined text like the game map; they also follow the zoom.
            var font=ImGui.GetFont();var nameScale=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f);
            // 0.152 (user choice « follow the zoom »): names scale with the map like the icons; only a 6 px floor keeps them drawable.
            var nameSize=Math.Max(6,ImGui.GetFontSize()*2.4f*zoomFactor*nameScale);
            foreach(var marker in settings.ShowPlaceNames ? markers.Where(marker=>marker.Icon==0):[])
            {
                var centre=origin+mapPan+new Vector2(marker.X,marker.Y)*mapZoom;
                var text=ImGui.CalcTextSize(marker.Label)*(nameSize/ImGui.GetFontSize());var corner=centre-text/2;
                var outline=ImGui.GetColorU32(new Vector4(0.96f,0.93f,0.84f,0.9f));
                foreach(var shift in new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(0,-1),new Vector2(0,1)})
                    draw.AddText(font,nameSize,corner+shift,outline,marker.Label);
                draw.AddText(font,nameSize,corner,ImGui.GetColorU32(new Vector4(0.30f,0.22f,0.12f,1)),marker.Label);
            }
            uint? linkUnderMouse=null;
            // Areas first, under every icon: running FATEs, quest areas, gathering points of this map.
            var fates=settings.ShowFates && live?.Fates is { } running && live.MapId==map.MapId ? running:[];
            var events=settings.ShowQuests && live?.Events is { } liveEvents ? liveEvents.Where(marker=>marker.MapId==map.MapId).ToArray():[];
            var gathering=settings.ShowGathering ? MapGatheringPoints(map):[];
            // Areas per kind (0.151): quests (marker type 3), leves and other events, FATEs, gathering points; own switch and colour.
            if(settings.ShowFateAreas)
                foreach(var fate in fates.Where(fate=>fate.Radius>0))
                    DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(fate.Position,map)*mapZoom,fate.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,settings.FateAreaColor);
            foreach(var marker in events.Where(marker=>marker.Radius>0))
            {
                var quest=marker.MarkerType==3;
                if(quest ? !settings.ShowQuestAreas:!settings.ShowEventAreas)continue;
                DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(marker.Position,map)*mapZoom,marker.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,quest ? settings.QuestAreaColor:settings.EventAreaColor);
            }
            if(settings.ShowGatheringAreas)
                foreach(var point in gathering)
                    if(point.Position is { Radius:>0 } area)
                        DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(new Vector3(area.X,0,area.Z),map)*mapZoom,area.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,settings.GatheringAreaColor);
            // Every game icon in one list sorted by priority (0.124: quests used to be drawn above everything whatever the
            // setting). Equal priorities keep the game's layer order: fixed icons, gathering, FATEs, quests, game target.
            var icons=new List<MapIconDraw>();
            // worldRadius: area of a quest, FATE or gathering point; hovering inside it lists the icon too (like Mappy).
            void Add(uint icon,int layer,Vector2 pixel,string? label,MapIconMarker? fixedMarker=null,MapLocalGatheringPoint? point=null,float worldRadius=0,Vector2? flag=null)
            {
                var own=settings.SizeOf(icon,map.MapId);if(own<=0)return;
                icons.Add(new(settings.PriorityOf(icon,map.MapId),layer,icons.Count,icon,origin+mapPan+pixel*mapZoom,Math.Max(4,baseIcon*own),label,fixedMarker,point,
                    worldRadius*Math.Max(1,map.SizeFactor)/100f*mapZoom,flag));
            }
            // NPC vendors (0.140): a vendor standing by the game's icon for it (shop or service icon, MapVendorIcons, MAP-017) adds its
            // shops to that icon's tooltip instead of a second icon; others get the game icon of their kind (cached, VendorLayout).
            var vendorLayout=settings.ShowVendors ? VendorLayout(map,markers,settings.ShowFixedIcons):MapVendorLayout.None;
            foreach(var marker in settings.ShowFixedIcons ? markers.Where(marker=>marker.Icon!=0):[])
            {
                var label=marker.Label.Length>0 ? marker.Label:null;Vector2? vendorFlag=null;
                if(vendorLayout.Merged.TryGetValue(marker,out var vendors)){label=vendors.Label;vendorFlag=vendors.Flag;}
                Add(marker.Icon,0,new Vector2(marker.X,marker.Y),label,marker,flag:vendorFlag);
            }
            // 0.328 (map audit #7): hovering inside an area lists its icon only while that kind of area is drawn.
            foreach(var point in gathering)
                if(point.Position is { } spot && point.Icon!=0)Add(point.Icon,1,MapProjection.PixelFromWorld(new Vector3(spot.X,0,spot.Z),map),GatheringLabelOf(point),point:point,
                    worldRadius:settings.ShowGatheringAreas ? spot.Radius:0);
            // NPC vendors (0.139): the game's « Échoppe » map icon (MapSymbol 60412, docs/evidence/0.139.0-symbols-probe.log), in the
            // same list, sizes and priorities as the game icons; click = flag.
            foreach(var vendor in vendorLayout.Lonely)Add(vendor.Icon,1,vendor.Pixel,vendor.Label,flag:vendor.Flag);
            foreach(var fate in fates.Where(fate=>fate.Icon!=0))Add(fate.Icon,2,MapProjection.PixelFromWorld(fate.Position,map),FateLabel(fate),worldRadius:settings.ShowFateAreas ? fate.Radius:0);
            foreach(var marker in events)Add(marker.Icon,3,MapProjection.PixelFromWorld(marker.Position,map),settings.QuestTooltips ? EventLabel(marker):null,
                worldRadius:(marker.MarkerType==3 ? settings.ShowQuestAreas:settings.ShowEventAreas) ? marker.Radius:0);
            ExpireGameTarget();
            if(mapGameTarget is { } gameTarget && gameTarget.MapId==map.MapId)
                Add(gameTarget.Icon,4,MapProjection.PixelFromWorld(new Vector3(gameTarget.World.X,0,gameTarget.World.Y),map),localization.Get("MapView.GameTarget"));
            icons.Sort((left,right)=>left.Priority!=right.Priority ? left.Priority.CompareTo(right.Priority):left.Layer!=right.Layer ? left.Layer.CompareTo(right.Layer):left.Order.CompareTo(right.Order));
            MapIconDraw? top=null;var hits=new List<MapIconDraw>();
            if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))mapIconContext=null;
            foreach(var item in icons)
            {
                if(textures.TryGetFromGameIcon(new GameIconLookup(item.Icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                    draw.AddImage(image.Handle,item.At-new Vector2(item.Size/2),item.At+new Vector2(item.Size/2));
                // Timed gathering points: a clock badge, green while open now, orange otherwise (0.130).
                if(item.Point is { Timed:true } timedPoint)
                {
                    var badge=Math.Max(6,item.Size*0.45f);var corner=item.At+new Vector2(item.Size*0.2f,-item.Size*0.6f);
                    draw.AddCircleFilled(corner+new Vector2(badge/2),badge*0.62f,ImGui.GetColorU32(new Vector4(0,0,0,0.75f)),16);
                    draw.AddText(UiBuilder.IconFont,badge,corner,ImGui.GetColorU32(TimedPointColor(timedPoint)),FontAwesomeIcon.Clock.ToIconString());
                }
                if(item.Point is { } labelled && settings.GatheringLabels && mapZoom>=0.6f)
                    draw.AddText(item.At+new Vector2(item.Size*0.6f,-ImGui.GetFontSize()/2),ImGui.GetColorU32(UI.FfxivTheme.Text),localization.Format("MapLocal.Level",labelled.Level));
                if(hovered)
                {
                    var distance=Vector2.Distance(io.MousePos,item.At);
                    if(distance<=item.Size/2)top=item;
                    if(distance<=Math.Max(item.Size/2,item.Area))hits.Add(item);
                }
            }
            // Every icon under the cursor (or whose area holds it) is listed in one tooltip, topmost first (0.132, as Mappy does).
            hits.Reverse();
            // Nearby coffers with the game's « Coffre au trésor » map icon (0.210), and gathering points the game streams for your current job
            // (icon-font symbol); both sized and hidden from the icon settings like the game's icons.
            if(live?.Objects is { } nearby && live.MapId==map.MapId)
                foreach(var item in nearby)
                {
                    if(item.Kind switch{MapObjectKind.Treasure=>!settings.ShowTreasure,MapObjectKind.HuntMark=>!settings.ShowHuntMarks,_=>!settings.ShowGathering})continue;
                    var kindSize=settings.SizeOf(item.Kind==MapObjectKind.Treasure ? TreasureIcon:GlyphNearbyGathering,map.MapId);
                    if(kindSize<=0)continue;
                    var at=origin+mapPan+MapProjection.PixelFromWorld(item.Position,map)*mapZoom;
                    var glyph=Math.Max(4,baseIcon*0.6f*kindSize);
                    if(item.Kind==MapObjectKind.Treasure && textures.TryGetFromGameIcon(new GameIconLookup(TreasureIcon),out var chestShared) && chestShared.TryGetWrap(out var chest,out _) && chest is not null)
                    {
                        var half=Math.Max(4,baseIcon*kindSize)/2;
                        draw.AddImage(chest.Handle,at-new Vector2(half),at+new Vector2(half));
                        glyph=half*2;
                    }
                    else
                    {
                        var symbol=(item.Kind switch{MapObjectKind.Treasure=>FontAwesomeIcon.BoxOpen,MapObjectKind.HuntMark=>FontAwesomeIcon.Skull,_=>FontAwesomeIcon.Leaf}).ToIconString();
                        // N-09 (0.441): marks coloured by rank (B blue, A orange, S red).
                        var colour=item.Kind switch{MapObjectKind.Treasure=>new Vector4(0.98f,0.82f,0.30f,1),
                            MapObjectKind.HuntMark=>item.Level switch{3=>new Vector4(0.95f,0.30f,0.30f,1),2=>new Vector4(0.98f,0.62f,0.25f,1),_=>new Vector4(0.45f,0.70f,0.98f,1)},
                            _=>new Vector4(0.55f,0.90f,0.55f,1)};
                        draw.AddCircleFilled(at,glyph*0.7f,ImGui.GetColorU32(new Vector4(0,0,0,0.55f)),20);
                        draw.AddText(UiBuilder.IconFont,glyph,at-new Vector2(glyph/2),ImGui.GetColorU32(colour),symbol);
                    }
                    var label=item.Kind==MapObjectKind.Gathering && item.Level>0 ? localization.Format("MapView.GatheringLabel",item.Name,item.Level)
                        :item.Kind==MapObjectKind.HuntMark ? localization.Format("MapView.HuntMarkLabel",item.Name,item.Level switch{3=>"S",2=>"A",_=>"B"}):item.Name;
                    if(hovered && Vector2.Distance(io.MousePos,at)<=glyph){tooltip=label;tooltipPoint=null;}
                }
            if(settings.ShowFlag && settings.SizeOf(FlagIcon,map.MapId)>0 && live is { Flag:{ } flag } && live.FlagMapId==map.MapId)
            {
                var flagPixel=MapProjection.PixelFromWorld(new Vector3(flag.X,0,flag.Y),map);var at=origin+mapPan+flagPixel*mapZoom;var iconSize=Math.Max(4,baseIcon*settings.SizeOf(FlagIcon,map.MapId));
                if(textures.TryGetFromGameIcon(new GameIconLookup(FlagIcon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                    draw.AddImage(icon.Handle,at-new Vector2(iconSize/2),at+new Vector2(iconSize/2));
                else draw.AddTriangleFilled(at,at+new Vector2(0,-14),at+new Vector2(10,-10),ImGui.GetColorU32(UI.FfxivTheme.Error));
            }
            // Sightseeing vistas and aether currents (cached positions, collected state read in game), then party members.
            if(mapLocal?.State.Index is { } localIndex)
            {
                if(settings.ShowSightseeing)
                    foreach(var vista in localIndex.OnMap(MapLocalKind.Sightseeing,map.MapId))
                    {
                        var done=live?.Sightseen?.Contains(vista.Id);
                        if(done==true && settings.HideCollected)continue;
                        var at=origin+mapPan+MapProjection.PixelFromWorld(new Vector3(vista.X,0,vista.Z),map)*mapZoom;
                        var vistaSize=settings.SizeOf(GlyphVista,map.MapId);if(vistaSize<=0)continue;
                        if(DrawGlyph(draw,at,FontAwesomeIcon.Binoculars,done==true ? new Vector4(0.62f,0.62f,0.62f,1):new Vector4(0.98f,0.82f,0.30f,1),baseIcon*vistaSize,hovered,io.MousePos))
                            {sightseeingTip=(vista,done);tooltipPoint=null;tooltip=null;}
                    }
                if(settings.ShowAetherCurrents)
                    foreach(var current in localIndex.OnMap(MapLocalKind.AetherCurrent,map.MapId))
                    {
                        var done=live?.UnlockedCurrents?.Contains(current.Id);
                        if(done==true && settings.HideCollected)continue;
                        var at=origin+mapPan+MapProjection.PixelFromWorld(new Vector3(current.X,0,current.Z),map)*mapZoom;
                        var currentSize=settings.SizeOf(GlyphCurrent,map.MapId);if(currentSize<=0)continue;
                        if(DrawGlyph(draw,at,FontAwesomeIcon.Wind,done==true ? new Vector4(0.62f,0.62f,0.62f,1):new Vector4(0.45f,0.85f,0.95f,1),baseIcon*currentSize,hovered,io.MousePos))
                        {
                            var state=localization.Get(done switch{true=>"MapView.CurrentUnlocked",false=>"MapView.CurrentLocked",_=>"MapView.CollectedUnknown"});
                            var quest=current.Data!=0 ? QuestOf(current.Data)?.Name:null;
                            tooltip=localization.Get(current.Data!=0 ? "MapView.CurrentQuest":"MapView.CurrentField")+" · "+state+(quest is { Length:>0 } ? "\n"+localization.Format("MapView.CurrentQuestName",quest):"");
                            sightseeingTip=null;tooltipPoint=null;
                        }
                    }
            }
            if(settings.ShowParty && live?.Party is { } members && live.MapId==map.MapId && settings.SizeOf(GlyphParty,map.MapId) is var partySize and >0)
                foreach(var member in members)
                {
                    var at=origin+mapPan+MapProjection.PixelFromWorld(mapPartySmoother.At(member.Name,member.Position,io.DeltaTime),map)*mapZoom;var badge=Math.Max(6,baseIcon*0.8f*partySize);
                    draw.AddCircleFilled(at,badge*0.6f,ImGui.GetColorU32(new Vector4(0.15f,0.45f,0.95f,0.85f)),20);
                    if(member.Job!=0 && textures.TryGetFromGameIcon(new GameIconLookup(62100+member.Job),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                        draw.AddImage(image.Handle,at-new Vector2(badge/2),at+new Vector2(badge/2));
                    if(hovered && Vector2.Distance(io.MousePos,at)<=badge/2){tooltip=localization.Format("MapView.PartyMember",member.Name,member.Level);sightseeingTip=null;tooltipPoint=null;}
                }
            // 0.378 (g, M-08): the player's bookmarks of this map (star); a click on one places the flag there.
            MapBookmark? hoveredBookmark=null;
            if(settings.ShowBookmarks && settings.SizeOf(GlyphBookmark,map.MapId) is var bookmarkSize and >0)
                foreach(var mark in configuration.MapBookmarks ?? [])
                {
                    if(mark is null || mark.MapId!=map.MapId)continue;
                    var at=origin+mapPan+MapProjection.PixelFromCoordinate(new Vector2(mark.X,mark.Y),map)*mapZoom;
                    if(!DrawGlyph(draw,at,FontAwesomeIcon.Star,new Vector4(1f,0.85f,0.35f,1),baseIcon*bookmarkSize,hovered,io.MousePos))continue;
                    hoveredBookmark=mark;sightseeingTip=null;tooltipPoint=null;
                    tooltip=(mark.Group is { } group ? $"[{group}] ":"")+mark.Name+$" ({mark.X:0.0}, {mark.Y:0.0})"+(mark.Note is { } note ? "\n"+note:"")+"\n"+localization.Get("MapView.BookmarkClick");
                }
            if(hoveredBookmark is { } clicked && top is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)FlagFromMap(map,clicked.X,clicked.Y);
            // 0.384 (g, M-14): the deciphered treasure map's spot, with the treasure layer; a click places the flag there.
            if(settings.ShowTreasure && TreasureMap?.Invoke() is { } treasure && treasure.MapId==map.MapId && settings.SizeOf(GlyphTreasureMap,map.MapId) is var treasureSize and >0)
            {
                var pixel=MapProjection.PixelFromWorld(treasure.Position,map);var at=origin+mapPan+pixel*mapZoom;
                if(DrawGlyph(draw,at,FontAwesomeIcon.Gem,new Vector4(0.95f,0.55f,0.25f,1),baseIcon*treasureSize,hovered,io.MousePos))
                {
                    sightseeingTip=null;tooltipPoint=null;
                    tooltip=localization.Format("MapView.TreasureMapTip",treasure.Name)+"\n"+localization.Get("MapView.BookmarkClick");
                    if(top is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)
                    {var coordinate=MapProjection.CoordinateFromPixel(pixel,map);FlagFromMap(map,coordinate.X,coordinate.Y);}
                }
            }
            if(settings.ShowPlayer && player is { } here)DrawPlayerArrow(draw,origin+mapPan+here*mapZoom,live!.Rotation,settings.ShowViewCone,MapViewSettings.ClampMarker(settings.PlayerIconScale),
                MapViewSettings.ClampMarker(settings.ViewConeScale),MapViewSettings.ClampConeOpacity(settings.ViewConeOpacity),settings.ViewConeColor);
            // 0.297 (user request): targets outside the view keep their icon on the map's edge, compass style.
            var edgeLabel=settings.ShowEdgeMarkers ? DrawEdgeMarkers(draw,origin,size,map,live,settings,player,events,fates,gathering,hovered,io.MousePos):null;
            if(edgeLabel is not null){tooltip=edgeLabel;sightseeingTip=null;tooltipPoint=null;}
            // Only the topmost icon under the cursor answers: tooltip, map link, teleport or flag.
            // 0.328 (map audit #5): not while an edge marker is under the mouse — that click centres the map; the icon below moved the
            // flag, asked for a teleport or opened another map on the same release.
            if(top is { } chosen && edgeLabel is null)
            {
                var released=ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged;
                if(chosen.Fixed is { } marker)
                {
                    if(marker.LinkMapId!=0)linkUnderMouse=marker.LinkMapId;
                    // Aetherytes: a click asks for the teleport, like the game map (normal teleport cost).
                    if(marker.AetheryteId!=0 && settings.TeleportOnClick)
                    {
                        if(released){mapTeleportTarget=(marker.AetheryteId,marker.Label);mapLive?.RefreshTeleportCosts();ImGui.OpenPopup("MapTeleport");}
                    }
                }
                if(chosen.Flag is { } vendorSpot && released)FlagFromMap(map,vendorSpot.X,vendorSpot.Y);
                if(chosen.Point is { Position:{ } spot })
                {
                    if(released){var coordinate=MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(new Vector3(spot.X,0,spot.Z),map),map);FlagFromMap(map,coordinate.X,coordinate.Y);}
                }
                // R-04 (0.245): right click on an icon opens that icon's menu, unless the icon is in the « general menu » list.
                if(ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !settings.RightClickIgnored(chosen.Icon) && sightseeingTip is null)
                    mapIconContext=(chosen,(chosen.At-origin-mapPan)/mapZoom);
            }
            if(hovered)
            {
                var coordinate=MapProjection.CoordinateFromPixel((io.MousePos-origin-mapPan)/mapZoom,map);
                var label=localization.Format("MapView.Cursor",coordinate.X.ToString("0.0"),coordinate.Y.ToString("0.0"));
                var box=ImGui.CalcTextSize(label)+new Vector2(10,6);
                draw.AddRectFilled(origin+new Vector2(6,size.Y-box.Y-6),origin+new Vector2(6+box.X,size.Y-6),ImGui.GetColorU32(UI.FfxivTheme.Panel),3);
                draw.AddText(origin+new Vector2(11,size.Y-box.Y-3),ImGui.GetColorU32(UI.FfxivTheme.Text),label);
            }
            // Gathering point: item icons and names, like the game's gathering tooltip (0.125, user request).
            // Glyph layers (vistas, currents, party, nearby objects) answer on their own; otherwise the list of game icons.
            mapHoveredVista=sightseeingTip?.Vista;
            if(tooltipPoint is { } gatheringTip)DrawGatheringTooltip(gatheringTip);
            else if(sightseeingTip is { } vistaTip)DrawSightseeingTooltip(vistaTip.Vista,vistaTip.Done);
            else if(tooltip is not null)WrappedTooltip(tooltip);
            else if(hits.Count>0)DrawIconListTooltip(hits,top,settings);
            // Region and world maps: a click (not a drag) on a linked icon opens that map, like the game.
            if(linkUnderMouse is { } link && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)NavigateMap(link);
        }
        finally{draw.PopClipRect();}
        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            mapContextPixel=(io.MousePos-origin-mapPan)/mapZoom;mapContextVista=mapHoveredVista;
            ImGui.OpenPopup(mapIconContext is not null ? "MapIconContext":"MapViewContext");
        }
        DrawIconContext(map,live);
        if(mapGeneralMenuRequested){mapGeneralMenuRequested=false;ImGui.OpenPopup("MapViewContext");}
        if(ImGui.BeginPopup("MapTeleport"))
        {
            try
            {
                if(mapTeleportTarget is { } target)
                {
                    ImGui.TextUnformatted(localization.Format("MapView.TeleportAsk",target.Name));
                    // Price from the game's teleport list; unknown (not attuned or not read yet) is said, never shown as 0.
                    if(mapLive?.TeleportCostsFailed==true)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MapView.TeleportCostFailed"));
                    else if(mapLive?.TeleportCosts is not { } costs)ImGui.TextDisabled(localization.Get("MapView.TeleportCostReading"));
                    else if(costs.TryGetValue(target.Id,out var cost)){ImGui.TextUnformatted(localization.Format("MapView.TeleportCost",cost.ToString("N0")));DrawGilIcon();}
                    else ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MapView.TeleportCostUnknown"));
                    if(ImGui.Button(localization.Get("MapView.TeleportYes"))){mapLive?.Teleport(target.Id);mapTeleportTarget=null;ImGui.CloseCurrentPopup();}
                    ImGui.SameLine();
                    if(ImGui.Button(localization.Get("MapView.TeleportNo"))){mapTeleportTarget=null;ImGui.CloseCurrentPopup();}
                }
                else ImGui.CloseCurrentPopup();
            }
            finally{ImGui.EndPopup();}
        }
        if(ImGui.BeginPopup("MapViewContext"))
        {
            try
            {
                var coordinate=MapProjection.CoordinateFromPixel(mapContextPixel,map);
                ImGui.TextDisabled(localization.Format("MapView.Cursor",coordinate.X.ToString("0.0"),coordinate.Y.ToString("0.0")));
                ImGui.BeginDisabled(map.TerritoryId==0);
                try{if(ImGui.Selectable(localization.Get("MapView.Flag")))FlagFromMap(map,coordinate.X,coordinate.Y);}
                finally{ImGui.EndDisabled();}
                ImGui.BeginDisabled(live?.Flag is null);
                try{if(ImGui.Selectable(localization.Get("MapView.RemoveFlag")))mapLive?.RemoveFlag();}
                finally{ImGui.EndDisabled();}
                // 0.242 (R-01, first part): the journal target icon could not be removed; right click › remove it.
                if(mapGameTarget is not null && ImGui.Selectable(localization.Get("MapView.RemoveGameTarget")))mapGameTarget=null;
                ImGui.BeginDisabled(map.TerritoryId==0);
                try{if(ImGui.Selectable(localization.Get("MapView.UseInTool"))){selectedMapId=map.MapId;mapX=coordinate.X;mapY=coordinate.Y;bookmarkSaved=false;page=Page.Maps;IsOpen=true;}}
                finally{ImGui.EndDisabled();}
                // 0.378 (g, M-08): add a bookmark at the clicked point, named here.
                if(map.TerritoryId!=0)
                {
                    ImGui.SetNextItemWidth(180);
                    ImGui.InputTextWithHint("##ContextBookmark",localization.Get("MapsUi.Name"),ref mapContextBookmarkName,MapBookmarks.NameLimit*4+1);
                    Tip("MapView.AddBookmarkTip");
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("MapView.AddBookmark")))
                    {
                        var name=string.IsNullOrWhiteSpace(mapContextBookmarkName) ? $"{map.Zone} · {coordinate.X:F1}, {coordinate.Y:F1}":mapContextBookmarkName;
                        if(SaveBookmarks(items=>MapBookmarks.Add(items,MapBookmarks.Create(map,name,MathF.Round(coordinate.X,1),MathF.Round(coordinate.Y,1))),adding:true))
                            {mapContextBookmarkName="";ImGui.CloseCurrentPopup();}
                    }
                    if(bookmarkError is { } addError)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get(addError));
                }
                if(mapContextVista is { } contextVista && map.TerritoryId!=0){ImGui.Separator();DrawVistaAlarmEntry(contextVista,map.TerritoryId);}
                if(map.TerritoryId!=0){ImGui.Separator();DrawMapLocalLists(map,live);}
                if(mapLocalCloseMenu){mapLocalCloseMenu=false;ImGui.CloseCurrentPopup();}
            }
            finally{ImGui.EndPopup();}
        }
        // 0.373 (M-03): nearest aetheryte asked from a chat map link, once this map is shown.
        ResolveNearestAetheryte(map);
        // Aetheryte chosen in the local lists: the teleport question opens once the context menu is closed.
        if(mapTeleportRequested){mapTeleportRequested=false;ImGui.OpenPopup("MapTeleport");}
        if(!ReferenceEquals(mapOpenError,mapErrorSeen)){mapErrorSeen=mapOpenError;if(mapOpenError is not null)mapErrorUntil=DateTimeOffset.UtcNow.AddSeconds(5);}
        if(hovered && mapOpenError is { } error && DateTimeOffset.UtcNow<mapErrorUntil)WrappedTooltip(error);
    }

    // One game icon to draw on the canvas, sorted by priority then layer then insertion.
    private readonly record struct MapIconDraw(int Priority,int Layer,int Order,uint Icon,Vector2 At,float Size,string? Label,MapIconMarker? Fixed,MapLocalGatheringPoint? Point,float Area=0,Vector2? Flag=null);
    private (MapIconDraw Icon,Vector2 Pixel)? mapIconContext;private bool mapGeneralMenuRequested;

    // R-04 (0.245): the menu of one map icon — its name, flag there, then what the icon offers (teleport, open the linked map,
    // the gathering point's resources, remove the journal target), and a way to the general menu.
    private void DrawIconContext(MapDefinition map,MapLiveState? live)
    {
        if(!ImGui.BeginPopup("MapIconContext"))return;
        try
        {
            if(mapIconContext is not { } context){ImGui.CloseCurrentPopup();return;}
            var item=context.Icon;
            DrawSmallGameIcon(item.Icon);
            ImGui.TextUnformatted((item.Label ?? IconRowName(item.Icon)).Split('\n')[0]);
            ImGui.Separator();
            var coordinate=MapProjection.CoordinateFromPixel(context.Pixel,map);
            if(item.Flag is { } spot)coordinate=spot;
            ImGui.BeginDisabled(map.TerritoryId==0);
            try{if(ImGui.Selectable(localization.Get("MapView.Flag")))FlagFromMap(map,coordinate.X,coordinate.Y);}
            finally{ImGui.EndDisabled();}
            // 0.299 (user request): the flag can be removed from every right-click menu that can place it.
            if(live?.Flag is not null && ImGui.Selectable(localization.Get("MapView.RemoveFlag")+"##IconRemoveFlag"))mapLive?.RemoveFlag();
            if(item.Fixed is { AetheryteId:not 0 } aetheryte && ImGui.Selectable(localization.Get("MapView.IconTeleport")))
                {mapTeleportTarget=(aetheryte.AetheryteId,aetheryte.Label);mapLive?.RefreshTeleportCosts();mapTeleportRequested=true;}
            if(item.Fixed is { LinkMapId:not 0 } linked && ImGui.Selectable(localization.Get("MapView.IconOpenMap")))NavigateMap(linked.LinkMapId);
            if(item.Point is { } point && point.Items.Count>0)
            {
                ImGui.Separator();
                ImGui.TextDisabled(localization.Get("MapLocal.OpenGatheringTool"));
                foreach(var resource in point.Items)
                {
                    var found=itemCatalog.State.Catalog?.Find(resource);
                    DrawSmallGameIcon(found?.IconId ?? 0);
                    if(ImGui.Selectable($"{found?.Name ?? $"#{resource}"}##IconMenu{resource}")){itemId=(int)resource;page=Page.Gathering;IsOpen=true;}
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(resource);
                }
            }
            if(item.Layer==4 && mapGameTarget is not null && ImGui.Selectable(localization.Get("MapView.RemoveGameTarget")))mapGameTarget=null;
            ImGui.Separator();
            if(ImGui.Selectable(localization.Get("MapView.IconGeneralMenu")))mapGeneralMenuRequested=true;
        }
        finally{ImGui.EndPopup();}
    }
    private const uint VendorIcon=60412,ExchangeIcon=60935;
    // Game shop icons (MapSymbol, docs/evidence/0.139.0-symbols-probe.log): 60412 Échoppe, 60935 Échangeur, 63851 Marchand.
    private static MapIconMarker[] VendorCandidates(IReadOnlyList<MapIconMarker> markers)=>
        markers.Where(marker=>MapVendorIcons.Shops.Contains(marker.Icon) || MapVendorIcons.Services.Contains(marker.Icon)).ToArray();
    private static MapIconMarker? VendorMarker(MapIconMarker[] candidates,Vector2 pixel,uint preferred=0)
    {
        var index=MapVendorIcons.Nearest(candidates.Select(marker=>(marker.X,marker.Y,marker.Icon,marker.Label.Length>0)).ToArray(),pixel,preferred);
        return index<0 ? null:candidates[index];
    }
    // Gil shop = Échoppe; only special exchanges = Échangeur.
    private uint VendorIconOf(Npcs.NpcVendor vendor)
    {
        var kinds=vendor.ShopIds.Select(id=>mapVendorCache.Shops.TryGetValue(id,out var shop) ? shop.Kind:(Npcs.NpcShopKind?)null).OfType<Npcs.NpcShopKind>().ToArray();
        return kinds.Length>0 && kinds.All(kind=>kind==Npcs.NpcShopKind.Special) ? ExchangeIcon:VendorIcon;
    }
    // One tooltip for every icon under the cursor: game icon, first line as title, the rest muted; gathering points list their
    // items with icons; the clickable (topmost) icon says what a click does. Unnamed icons use the game's symbol name.
    private void DrawIconListTooltip(IReadOnlyList<MapIconDraw> hits,MapIconDraw? top,MapViewSettings settings)
    {
        ImGui.BeginTooltip();
        try
        {
            var size=new Vector2(ImGui.GetTextLineHeight()*1.6f);var shown=0;
            foreach(var hit in hits)
            {
                if(shown++>=12){ImGui.TextDisabled(localization.Format("MapView.HoverMore",hits.Count-12));break;}
                if(shown>1)ImGui.Separator();
                if(textures.TryGetFromGameIcon(new GameIconLookup(hit.Icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)ImGui.Image(image.Handle,size);
                else ImGui.Dummy(size);
                ImGui.SameLine();
                ImGui.BeginGroup();
                try
                {
                    var lines=(hit.Label ?? (mapMarkers?.SymbolName(hit.Icon) is { Length:>0 } symbol ? symbol:localization.Format("MapView.IconUnnamed",hit.Icon))).Split('\n');
                    ImGui.TextColored(Accent,lines[0]);
                    foreach(var line in lines.Skip(hit.Point is null ? 1:2))ImGui.TextDisabled(line);
                    if(hit.Point is { } point)
                    {
                        DrawPointAvailability(point);
                        var items=itemCatalog.State.Catalog;
                        foreach(var id in point.Items.Take(8))
                        {
                            var item=items?.Find(id);
                            DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                            ImGui.TextUnformatted(item?.Name ?? $"#{id}");
                        }
                    }
                    if(top is { } clicked && clicked.Order==hit.Order && clicked.Layer==hit.Layer)
                    {
                        var action=hit.Fixed is { AetheryteId:not 0 } && settings.TeleportOnClick ? "MapView.TeleportHint"
                            :hit.Fixed is { LinkMapId:not 0 } ? "MapView.OpenLink":hit.Point is { Position:not null } || hit.Flag is not null ? "MapLocal.ClickFlag":null;
                        if(action is not null)ImGui.TextDisabled(localization.Get(action));
                    }
                }
                finally{ImGui.EndGroup();}
            }
        }
        finally{ImGui.EndTooltip();}
    }
    // Event marker title: the game's tooltip when it could be read safely, else the name of a quest in progress with the same
    // objective id (0.134: a leve marker took a completed quest's name through a 16-bit id collision), else null → the
    // game's symbol name in the list.
    private string? EventLabel(MapEventMarker marker)
    {
        if(marker.Tooltip.Length>0)return marker.Tooltip;
        // Quest marker (type 3): exact quest row from DataId, whatever its progress.
        if(marker.MarkerType==3 && marker.DataId!=0 && QuestOf(marker.DataId+65536u) is { Name.Length:>0 } quest)return quest.Name;
        if(marker.ObjectiveId==0)return null;
        var key=marker.ObjectiveId & 0xFFFF;
        return ActiveQuestByKey(key)?.Name;
    }
    // Fill with the chosen colour and alpha; the outline is the same colour, stronger, so faint areas stay readable.
    private static void DrawArea(ImDrawListPtr draw,Vector2 at,float radius,Vector4 colour)
    {
        draw.AddCircleFilled(at,radius,ImGui.GetColorU32(colour),48);
        draw.AddCircle(at,radius,ImGui.GetColorU32(colour with{W=Math.Min(1,colour.W*2.5f+0.2f)}),48,2f);
    }
    // Gathering points of a map (cached catalogue + cached positions), rebuilt only when the map or a source changes.
    private readonly MapPartySmoother mapPartySmoother=new();
    // 0.518 (user report): « open now » only speaks of the hours. A legendary point also asks for its tome, read by this character.
    internal Func<uint,TenshoAssistant.Gathering.GatherPointAccess?>? GatherAccess { get; set; }
    private Vector4 TimedPointColor(MapLocalGatheringPoint point)=>GatherAccess?.Invoke(point.BaseId)?.TomeMissing is not null ? Muted:point.ActiveAt(EorzeaMinutes()) ? UI.FfxivTheme.Success:new Vector4(0.98f,0.62f,0.20f,1);
    private void DrawPointAvailability(MapLocalGatheringPoint point)
    {
        var access=GatherAccess?.Invoke(point.BaseId);
        if(point.Timed)
        {
            var open=point.ActiveAt(EorzeaMinutes());
            ImGui.TextColored(TimedPointColor(point),localization.Format(!open ? "MapView.TimedClosed":access?.TomeMissing is not null ? "MapView.TimedOpenNoTome":"MapView.TimedOpen",point.Schedule!.Label));
        }
        if(access is null)return;
        if(access.TomeMissing is { } tome)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MapView.TomeMissing",tome));
        if(access.JobLevel is { } level && level<point.Level)
            ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MapView.PointLevelLow",localization.Get(access.JobId==TenshoAssistant.Gathering.GatherPointAccess.Miner ? "GatheringPanel.Miner":"GatheringPanel.Botanist"),level,point.Level));
    }

    private (uint MapId,object? Catalog,object? Index,IReadOnlyList<MapLocalGatheringPoint> Points) mapGatheringCache=(0,null,null,[]);
    private IReadOnlyList<MapLocalGatheringPoint> MapGatheringPoints(MapDefinition map)
    {
        var catalog=gatheringCatalog.State.Catalog;var index=mapLocal?.State.Index;
        if(catalog is null || index is null)return [];
        if(mapGatheringCache.MapId!=map.MapId || !ReferenceEquals(mapGatheringCache.Catalog,catalog) || !ReferenceEquals(mapGatheringCache.Index,index))
            mapGatheringCache=(map.MapId,catalog,index,MapLocalLists.Gathering(catalog.Sources,index,map.MapId).SelectMany(group=>group.Points).ToArray());
        return mapGatheringCache.Points;
    }
    // « Coffre au trésor » map icon of the game (MapSymbol 63852, docs/evidence/0.139.0-symbols-probe.log), used for nearby coffers (0.210).
    private const uint TreasureIcon=63852;
    // Keys of Tensho's own map layers in the icon size/priority tables (outside the game's icon id range).
    private const uint GlyphVista=0xFFFF0001,GlyphCurrent=0xFFFF0002,GlyphNearbyGathering=0xFFFF0003,GlyphParty=0xFFFF0004,GlyphBookmark=0xFFFF0005,GlyphTreasureMap=0xFFFF0006;
    private static (FontAwesomeIcon Symbol,Vector4 Colour)? TenshoGlyph(uint icon)=>icon switch
    {
        GlyphVista=>(FontAwesomeIcon.Binoculars,new Vector4(0.98f,0.82f,0.30f,1)),
        GlyphCurrent=>(FontAwesomeIcon.Wind,new Vector4(0.45f,0.85f,0.95f,1)),
        GlyphNearbyGathering=>(FontAwesomeIcon.Leaf,new Vector4(0.55f,0.90f,0.55f,1)),
        GlyphParty=>(FontAwesomeIcon.Users,new Vector4(0.35f,0.6f,1f,1)),
        GlyphBookmark=>(FontAwesomeIcon.Star,new Vector4(1f,0.85f,0.35f,1)),
        GlyphTreasureMap=>(FontAwesomeIcon.Gem,new Vector4(0.95f,0.55f,0.25f,1)),
        _=>null,
    };
    private string IconRowName(uint icon)=>icon switch
    {
        FlagIcon=>localization.Get("MapView.ShowFlag"),
        GlyphVista=>localization.Get("MapView.ShowSightseeing"),
        GlyphCurrent=>localization.Get("MapView.ShowAetherCurrents"),
        GlyphNearbyGathering=>localization.Get("MapView.IconNearbyGathering"),
        GlyphParty=>localization.Get("MapView.ShowParty"),
        GlyphBookmark=>localization.Get("MapView.ShowBookmarks"),
        GlyphTreasureMap=>localization.Get("MapView.TreasureMapIcon"),
        _ when mapMarkers?.SymbolName(icon) is { Length:>0 } known=>known,
        _ when mapMarkers?.QuestIcons.TryGetValue(icon,out var quest)==true=>localization.Format("MapView.QuestIcon",
            localization.Get("MapView.QuestKind"+(quest.Row is 1 or 3 or 8 or 10 ? quest.Row:0u)),quest.Row,
            localization.Get(quest.Locked ? "MapView.QuestLocked":"MapView.QuestAvailable"),quest.Variant),
        _=>localization.Format("MapView.IconUnnamed",icon),
    };

    // Icon-font marker with a dark disc, following the zoom; true when the cursor is on it.
    private static bool DrawGlyph(ImDrawListPtr draw,Vector2 at,FontAwesomeIcon symbol,Vector4 colour,float baseIcon,bool hovered,Vector2 mouse)
    {
        var glyph=Math.Max(6,baseIcon*0.6f);
        draw.AddCircleFilled(at,glyph*0.75f,ImGui.GetColorU32(new Vector4(0,0,0,0.6f)),20);
        var text=symbol.ToIconString();
        draw.AddText(UiBuilder.IconFont,glyph,at-new Vector2(glyph/2),ImGui.GetColorU32(colour),text);
        return hovered && Vector2.Distance(mouse,at)<=glyph*0.75f;
    }
    private WikiVistaSource? wikiVistas;
    // Earliest next wiki retry after a failed read (0.328: one minute after the first hover, then once a minute).
    private DateTimeOffset mapVistaRetryAt;
    internal void ConfigureWikiVistas(WikiVistaSource source)=>wikiVistas=source;
    private (object? Document,object? Index,IReadOnlyDictionary<uint,WikiVista> Rows) wikiVistaMatch=(null,null,new Dictionary<uint,WikiVista>());
    // Wiki row of a vista, matched once per wiki document and position index (coordinates on the vista's own map).
    private WikiVista? WikiVistaFor(MapLocalPosition vista)
    {
        var document=wikiVistas?.State.Document;var index=mapLocal?.State.Index;var catalog=maps.State.Catalog;
        if(document is null || index is null || catalog is null)return null;
        if(!ReferenceEquals(wikiVistaMatch.Document,document) || !ReferenceEquals(wikiVistaMatch.Index,index))
        {
            var games=index.All(MapLocalKind.Sightseeing).Select(row=>catalog.Find(row.MapId) is { } map
                ? MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(new Vector3(row.X,0,row.Z),map),map) is var c ? (row.Id,(int)(row.Id-2162688u),c.X,c.Y):default
                :default).Where(entry=>entry.Id!=0);
            wikiVistaMatch=(document,index,WikiVistaParser.Match(document.Rows,games));
        }
        return wikiVistaMatch.Rows.GetValueOrDefault(vista.Id);
    }
    private string ExpansionName(int section)=>localization.Get("MapView.Expansion"+Math.Clamp(section,0,5));

    // Eorzea time: 1 Eorzea day = 70 real minutes (3600/175 Eorzea seconds per real second).
    private static int EorzeaMinutes()=>(int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()*3600/175/1000/60%1440);
    // Sightseeing log entry: number (row order), name, state, and the game's picture once discovered.
    private void DrawSightseeingTooltip(MapLocalPosition vista,bool? done)
    {
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextColored(Accent,localization.Format("MapView.Vista",vista.Name));
            ImGui.TextDisabled(localization.Get(done switch{true=>"MapView.VistaDone",false=>"MapView.VistaTodo",_=>"MapView.CollectedUnknown"}));
            // Conditions from the game data: Eorzea time window and emote; the weather is not in the data.
            if(vista.MinTime!=0 || vista.MaxTime!=0)
            {
                var now=EorzeaMinutes();
                ImGui.TextUnformatted(localization.Format("MapView.VistaTime",$"{vista.MinTime/100:00}:{vista.MinTime%100:00}",$"{vista.MaxTime/100:00}:{vista.MaxTime%100:00}")
                    +(vista.InWindow(now) ? " "+localization.Get("MapView.VistaNow"):""));
            }
            if(vista.Extra.Length>0)ImGui.TextUnformatted(localization.Format("MapView.VistaEmote",vista.Extra));
            // Weather now in the vista's zone, compared with the weather the wiki gives (absent from the game data).
            var territory=maps.State.Catalog?.Find(vista.MapId)?.TerritoryId ?? 0;
            var weatherNow=territory!=0 && weather.State.Catalog?.Forecast(territory,DateTimeOffset.UtcNow,1) is [ { Weather:{ } current } ] ? current:null;
            if(weatherNow is not null)ImGui.TextUnformatted(localization.Format("MapView.VistaWeatherNow",weatherNow.Name));
            var utcNow=DateTimeOffset.UtcNow;
            if(mapVistaRetryAt==default)mapVistaRetryAt=utcNow.AddMinutes(1);
            wikiVistas?.Request();
            if(WikiVistaFor(vista) is { } wiki)
            {
                ImGui.TextColored(Accent,localization.Format(wiki.Section==0 ? "MapView.VistaNumberArr":"MapView.VistaNumberEx",wiki.Number,ExpansionName(wiki.Section)));
                if(wiki.Weathers.Count>0)
                {
                    var names=wiki.Weathers.Select(name=>weather.State.Catalog?.WeatherBySourceName(name,territory) is { } known ? (known.Name,known.Id):(name,0u)).ToArray();
                    var matches=weatherNow is not null && names.Any(entry=>entry.Item2==weatherNow.Id);
                    ImGui.TextColored(weatherNow is null ? UI.FfxivTheme.Text:matches ? UI.FfxivTheme.Success:UI.FfxivTheme.Error,
                        localization.Format("MapView.VistaWeatherNeeded",string.Join(", ",names.Select(entry=>entry.Item1)))
                        +(weatherNow is null ? "":" "+localization.Get(matches ? "MapView.VistaWeatherOk":"MapView.VistaWeatherWait")));
                }
                else ImGui.TextDisabled(localization.Get("MapView.VistaNoWeather"));
                if(NextVistaWindow(vista,territory) is { } nextWindow)
                    ImGui.TextUnformatted(localization.Format("Alarms.VistaNext",nextWindow.ToLocalTime().ToString("g"),Math.Max(0,(int)(nextWindow-DateTimeOffset.UtcNow).TotalMinutes)));
                ImGui.TextDisabled(localization.Get("Alarms.VistaRightClick"));
                if(wiki.Comment.Length>0)
                {
                    ImGui.PushTextWrapPos(ImGui.GetFontSize()*30);
                    ImGui.TextWrapped(localization.Format("MapView.VistaWhere",wiki.Comment));
                    ImGui.PopTextWrapPos();
                }
                ImGui.TextDisabled(localization.Format("MapView.VistaWikiSource",wikiVistas!.State.Document!.FetchedAt.ToLocalTime().ToString("g")));
            }
            else ImGui.TextDisabled(localization.Get(wikiVistas?.State is { Loading:true } ? "MapView.VistaWikiLoading":"MapView.VistaWeather"));
            // 0.328 (map audit #21): a failed wiki read is said, and asked again at most once a minute while a vista is hovered (the
            // source only retries when asked after an error).
            if(wikiVistas is { State.Error:{ } wikiError } wikiSource)
            {
                ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MapView.VistaWikiError"));
                if(developerMode){ImGui.PushTextWrapPos(ImGui.GetFontSize()*30);try{ImGui.TextDisabled(wikiError);}finally{ImGui.PopTextWrapPos();}}
                if(utcNow>=mapVistaRetryAt){mapVistaRetryAt=utcNow.AddMinutes(1);wikiSource.Request(retry:true);}
            }
            // Log tiers (ClientStructs): ARR entries 1–20 need tier 1, 21–80 tier 2; later entries depend on the Ex state.
            if(mapLive?.State is { SightseeingUnlock:>=0 } state)
            {
                var number=vista.Id-2162688u;
                bool? unlocked=number<20 ? state.SightseeingUnlock>=1:number<80 ? state.SightseeingUnlock>=2:null;
                ImGui.TextColored(unlocked==false ? UI.FfxivTheme.Error:UI.FfxivTheme.TextMuted,unlocked switch
                {
                    true=>localization.Get("MapView.VistaLogOpen"),
                    false=>localization.Format("MapView.VistaLogLocked",state.SightseeingUnlock),
                    _=>localization.Format("MapView.VistaLogEx",state.SightseeingUnlock,state.SightseeingUnlockEx),
                });
            }
            if(done==true && vista.Data!=0 && textures.TryGetFromGameIcon(new GameIconLookup(vista.Data),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                ImGui.Image(image.Handle,new Vector2(160,90));
        }
        finally{ImGui.EndTooltip();}
    }
    private void DrawGatheringTooltip(MapLocalGatheringPoint point)
    {
        ImGui.BeginTooltip();
        try
        {
            if(point.Icon!=0){DrawSmallGameIcon(point.Icon);}
            ImGui.TextColored(Accent,$"{point.Type} · {localization.Format("MapLocal.Level",point.Level)}{(point.Hidden ? " "+localization.Get("MapLocal.Hidden"):"")}");
            if(point.Place.Length>0)ImGui.TextDisabled(point.Place);
            DrawPointAvailability(point);
            var items=itemCatalog.State.Catalog;
            foreach(var id in point.Items.Take(16))
            {
                var item=items?.Find(id);
                DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                ImGui.TextUnformatted(item?.Name ?? $"#{id}");
            }
            ImGui.TextDisabled(localization.Get("MapLocal.ClickFlag"));
        }
        finally{ImGui.EndTooltip();}
    }
    // Vendors standing on a map, with the shop names of each, rebuilt only when the map or the catalogue changes.
    private (uint MapId,object? Catalog,IReadOnlyList<(Npcs.NpcVendor Vendor,Npcs.NpcLocation Spot)> Rows,IReadOnlyDictionary<uint,(string Name,Npcs.NpcShopKind Kind,int Items)> Shops) mapVendorCache=(0,null,[],new Dictionary<uint,(string,Npcs.NpcShopKind,int)>());
    private IReadOnlyList<(Npcs.NpcVendor Vendor,Npcs.NpcLocation Spot)> MapVendors(MapDefinition map)
    {
        if(npcVendors?.State.Catalog is not { } catalog)return [];
        if(mapVendorCache.MapId!=map.MapId || !ReferenceEquals(mapVendorCache.Catalog,catalog))
        {
            var shops=ReferenceEquals(mapVendorCache.Catalog,catalog) ? mapVendorCache.Shops
:catalog.Sales.GroupBy(sale=>sale.ShopId).ToDictionary(group=>group.Key,group=>(group.First().ShopName,group.First().Kind,group.Select(sale=>sale.ItemId).Distinct().Count()));
            var same=(maps.State.Catalog?.Equivalent(map) ?? [map.MapId]).ToHashSet();
            var rows=catalog.Vendors.SelectMany(vendor=>vendor.Locations.Where(spot=>same.Contains(spot.MapId)).Take(1).Select(spot=>(vendor,spot))).ToArray();
            mapVendorCache=(map.MapId,catalog,rows,shops);
        }
        return mapVendorCache.Rows;
    }
    // 0.328 (map audit #8/#34): the vendors of a map merged into the game's shop icons (each icon's full label, the first vendor's
    // flag) and those drawn with their own icon (icon, pixel, label, flag), built once per map, vendor rows (catalogue), marker list,
    // « fixed icons » switch and language instead of every frame.
    private sealed record MapVendorLayout(uint MapId,object? Rows,object? Markers,bool Fixed,int Revision,
        IReadOnlyDictionary<MapIconMarker,(string Label,Vector2 Flag)> Merged,IReadOnlyList<(uint Icon,Vector2 Pixel,string Label,Vector2 Flag)> Lonely)
    {
        public static readonly MapVendorLayout None=new(0,null,null,false,-1,new Dictionary<MapIconMarker,(string,Vector2)>(),[]);
    }
    private MapVendorLayout mapVendorLayout=MapVendorLayout.None;
    private MapVendorLayout VendorLayout(MapDefinition map,IReadOnlyList<MapIconMarker> markers,bool showFixed)
    {
        var rows=MapVendors(map);var shown=showFixed ? markers:null;var cached=mapVendorLayout;
        if(cached.MapId==map.MapId && ReferenceEquals(cached.Rows,rows) && ReferenceEquals(cached.Markers,shown) && cached.Fixed==showFixed && cached.Revision==localization.Revision)
            return cached;
        var shopMarkers=showFixed ? VendorCandidates(markers):[];
        var grouped=new Dictionary<MapIconMarker,List<(string Label,Vector2 Flag)>>(ReferenceEqualityComparer.Instance);
        var lonely=new List<(uint Icon,Vector2 Pixel,string Label,Vector2 Flag)>();
        foreach(var (vendor,spot) in rows)
        {
            var pixel=MapProjection.PixelFromWorld(new Vector3(spot.WorldX,0,spot.WorldZ),map);var icon=VendorIconOf(vendor);
            var near=VendorMarker(shopMarkers,pixel,icon);
            if(near is null){lonely.Add((icon,pixel,VendorLabel(vendor),new Vector2(spot.X,spot.Y)));continue;}
            if(!grouped.TryGetValue(near,out var list))grouped[near]=list=[];
            list.Add((VendorLabel(vendor),new Vector2(spot.X,spot.Y)));
        }
        // A shop icon with vendors: its own name (else the game's symbol name), then each vendor's shops.
        var merged=new Dictionary<MapIconMarker,(string Label,Vector2 Flag)>(ReferenceEqualityComparer.Instance);
        foreach(var (marker,vendors) in grouped)
            merged[marker]=((marker.Label.Length>0 ? marker.Label:mapMarkers?.SymbolName(marker.Icon) ?? "")+"\n"+string.Join("\n",vendors.Select(entry=>entry.Label)),vendors[0].Flag);
        return mapVendorLayout=new(map.MapId,rows,shown,showFixed,localization.Revision,merged,lonely);
    }
    // Each shop: its name (or its kind when the game gives none, or only an untranslated Japanese placeholder such as
    // « アイテムの購入 », seen 0.137) and how many items it sells; shops without sales in the catalogue are counted apart.
    private string VendorLabel(Npcs.NpcVendor vendor)
    {
        var name=vendor.Name.Length>0 ? vendor.Name:localization.Get("MapLocal.UnnamedVendor");
        var lines=new List<string>();var unknown=0;
        foreach(var id in vendor.ShopIds.Distinct())
        {
            if(!mapVendorCache.Shops.TryGetValue(id,out var shop)){unknown++;continue;}
            var shopName=string.IsNullOrWhiteSpace(shop.Name) || localization.CurrentCode!="ja" && shop.Name.Any(c=>c is >= '\u3040' and <= '\u30FF' or >= '\u4E00' and <= '\u9FFF')
                ? localization.Get("MapLocal.ShopKind"+shop.Kind):shop.Name;
            lines.Add(localization.Format("MapLocal.ShopLine",shopName,shop.Items));
        }
        var shown=lines.Distinct().Take(6).ToList();
        if(lines.Distinct().Count()>6)shown.Add(localization.Format("MapView.HoverMore",lines.Distinct().Count()-6));
        if(unknown>0)shown.Add(localization.Format("MapLocal.ShopsUnknown",unknown));
        if(shown.Count==0)shown.Add(localization.Get("MapLocal.ShopsNone"));
        return name+"\n"+string.Join("\n",shown);
    }
    private string GatheringPointLabel(MapLocalGatheringPoint point)
    {
        var items=itemCatalog.State.Catalog;
        var names=string.Join(", ",point.Items.Take(8).Select(id=>items?.Find(id)?.Name ?? $"#{id}"));
        return $"{point.Type} · {localization.Format("MapLocal.Level",point.Level)} · {point.Place}{(point.Hidden ? " "+localization.Get("MapLocal.Hidden"):"")}\n{names}";
    }
    // 0.328 (map audit #8/#34): each point's label built once per point list (map), item catalogue and language, not every frame.
    private (object? Points,object? Items,int Revision,Dictionary<MapLocalGatheringPoint,string> Labels) mapGatheringLabels=(null,null,-1,new(ReferenceEqualityComparer.Instance));
    private string GatheringLabelOf(MapLocalGatheringPoint point)
    {
        var points=mapGatheringCache.Points;var items=itemCatalog.State.Catalog;
        if(!ReferenceEquals(mapGatheringLabels.Points,points) || !ReferenceEquals(mapGatheringLabels.Items,items) || mapGatheringLabels.Revision!=localization.Revision)
        {mapGatheringLabels.Labels.Clear();mapGatheringLabels=(points,items,localization.Revision,mapGatheringLabels.Labels);}
        if(!mapGatheringLabels.Labels.TryGetValue(point,out var label))mapGatheringLabels.Labels[point]=label=GatheringPointLabel(point);
        return label;
    }

    // Game rotation 0 faces +Z, which is "down" on the map texture.
    // Edge markers (0.297, user request): targets of the shown map outside the view keep their own icon on the map's edge, on their
    // side, with a small pointer; click = centre the map on the target (the character: follow it again). Each kind has its switch:
    // character, flag, journal target, active quests, leves and other events, FATEs, timed gathering points open now, party.
    // Returns the label under the mouse, if any.
    private string? DrawEdgeMarkers(ImDrawListPtr draw,Vector2 origin,Vector2 size,MapDefinition map,MapLiveState? live,MapViewSettings settings,
        Vector2? player,IReadOnlyList<MapEventMarker> events,IReadOnlyList<MapFateMarker> fates,IReadOnlyList<MapLocalGatheringPoint> gathering,bool hovered,Vector2 mouse)
    {
        var targets=new List<(Vector2 Pixel,uint Icon,string Label,bool Player,uint Job)>();
        if(settings.EdgePlayer && settings.ShowPlayer && player is { } here)targets.Add((here,0,localization.Get("MapView.EdgePlayer"),true,0));
        // 0.328 (map audit #6/#22/#37): a kind hidden on the map (Flag off, icon hidden or at size 0) is not brought back on the edge.
        if(settings.EdgeFlag && settings.ShowFlag && settings.SizeOf(FlagIcon,map.MapId)>0 && live is { Flag:{ } flag } && live.FlagMapId==map.MapId)
            targets.Add((MapProjection.PixelFromWorld(new Vector3(flag.X,0,flag.Y),map),FlagIcon,localization.Get("MapView.ShowFlag"),false,0));
        if(settings.EdgeTarget && mapGameTarget is { } target && target.MapId==map.MapId && settings.SizeOf(target.Icon,map.MapId)>0)
            targets.Add((MapProjection.PixelFromWorld(new Vector3(target.World.X,0,target.World.Y),map),target.Icon,localization.Get("MapView.GameTarget"),false,0));
        var shownQuests=new HashSet<uint>();
        foreach(var marker in events)
        {
            if(marker.Icon==0 || settings.SizeOf(marker.Icon,map.MapId)<=0)continue;
            var quest=marker.ObjectiveId!=0 ? ActiveQuestByKey(marker.ObjectiveId & 0xFFFF):null;
            if(quest is not null){if(settings.EdgeQuests && shownQuests.Add(quest.Id))targets.Add((MapProjection.PixelFromWorld(marker.Position,map),marker.Icon,quest.Name,false,0));}
            else if(settings.EdgeEvents)targets.Add((MapProjection.PixelFromWorld(marker.Position,map),marker.Icon,EventLabel(marker) ?? localization.Get("MapView.EdgeEvent"),false,0));
        }
        if(settings.EdgeFates)
            foreach(var fate in fates.Where(fate=>fate.Icon!=0 && settings.SizeOf(fate.Icon,map.MapId)>0))targets.Add((MapProjection.PixelFromWorld(fate.Position,map),fate.Icon,FateLabel(fate),false,0));
        if(settings.EdgeGathering)
        {
            var minutes=EorzeaMinutes();
            foreach(var point in gathering.Where(point=>point.Timed && point.Icon!=0 && settings.SizeOf(point.Icon,map.MapId)>0 && point.Position is not null && point.ActiveAt(minutes) && GatherAccess?.Invoke(point.BaseId)?.TomeMissing is null))
                targets.Add((MapProjection.PixelFromWorld(new Vector3(point.Position!.X,0,point.Position.Z),map),point.Icon,GatheringLabelOf(point),false,0));
        }
        if(settings.EdgeParty && settings.ShowParty && settings.SizeOf(GlyphParty,map.MapId)>0 && live?.Party is { } members && live.MapId==map.MapId)
            foreach(var member in members)targets.Add((MapProjection.PixelFromWorld(member.Position,map),0,localization.Format("MapView.PartyMember",member.Name,member.Level),false,member.Job));
        const float margin=20,badge=26;
        var centre=origin+size/2;var low=origin+new Vector2(margin);var high=origin+size-new Vector2(margin);
        string? label=null;
        foreach(var (pixel,icon,name,isPlayer,job) in targets)
        {
            var at=origin+mapPan+pixel*mapZoom;
            if(at.X>=origin.X && at.Y>=origin.Y && at.X<=origin.X+size.X && at.Y<=origin.Y+size.Y)continue;
            var direction=at-centre;if(direction.LengthSquared()<1)continue;
            direction=Vector2.Normalize(direction);
            // Point where the ray from the centre leaves the inner rectangle.
            var tx=direction.X>0 ? (high.X-centre.X)/direction.X:direction.X<0 ? (low.X-centre.X)/direction.X:float.MaxValue;
            var ty=direction.Y>0 ? (high.Y-centre.Y)/direction.Y:direction.Y<0 ? (low.Y-centre.Y)/direction.Y:float.MaxValue;
            var edge=centre+direction*Math.Min(tx,ty);
            var side=new Vector2(-direction.Y,direction.X);
            draw.AddTriangleFilled(edge+direction*(badge*0.75f),edge+direction*(badge*0.45f)+side*6,edge+direction*(badge*0.45f)-side*6,ImGui.GetColorU32(UI.FfxivTheme.Gold));
            draw.AddCircleFilled(edge,badge*0.62f,ImGui.GetColorU32(new Vector4(0,0,0,0.6f)),20);
            var image=isPlayer ? 0u:job!=0 ? 62100+job:icon;
            if(isPlayer)DrawPlayerArrow(draw,edge,live!.Rotation,false,0.8f);
            else if(textures.TryGetFromGameIcon(new GameIconLookup(image),out var shared) && shared.TryGetWrap(out var wrap,out _) && wrap is not null)
                draw.AddImage(wrap.Handle,edge-new Vector2(badge/2),edge+new Vector2(badge/2));
            if(!hovered || Vector2.Distance(mouse,edge)>badge*0.62f)continue;
            label=$"{name}\n{localization.Get("MapView.EdgeClick")}";
            if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged){mapCenterRequest=pixel;mapViewFollow=isPlayer;}
        }
        return label;
    }

    private static void DrawPlayerArrow(ImDrawListPtr draw,Vector2 centre,float rotation,bool cone,float marker=1,float coneScale=1,float coneOpacity=0.5f,Vector3? coneColour=null)
    {
        var forward=new Vector2(MathF.Sin(rotation),MathF.Cos(rotation));var side=new Vector2(-forward.Y,forward.X);
        var colour=ImGui.GetColorU32(UI.FfxivTheme.Gold);
        if(cone && coneOpacity>0)
        {
            // View cone like the minimap: 90° wide, 60 px long at 100 % (0.288: length and opacity are settings).
            var left=forward*MathF.Cos(MathF.PI/4)+side*MathF.Sin(MathF.PI/4);var right=forward*MathF.Cos(MathF.PI/4)-side*MathF.Sin(MathF.PI/4);
            var length=60*coneScale;
            var tint=coneColour is { } chosen ? new Vector4(chosen,coneOpacity):UI.FfxivTheme.Gold with{W=coneOpacity};
            draw.AddTriangleFilled(centre,centre+left*length,centre+right*length,ImGui.GetColorU32(tint));
        }
        draw.AddTriangleFilled(centre+forward*12*marker,centre-forward*7*marker+side*7*marker,centre-forward*7*marker-side*7*marker,colour);
        draw.AddCircle(centre,4*marker,ImGui.GetColorU32(UI.FfxivTheme.Text),12,1.5f);
    }
}
