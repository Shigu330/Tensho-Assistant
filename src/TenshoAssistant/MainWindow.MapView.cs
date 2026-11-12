using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>
/// Tensho map window (MAP-VIEW-016, CHANGELOG 0.101.0 onwards; drawn by MapViewWindow): the game map texture with its fixed icons,
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
    internal void ConfigureMapView(MapLiveReader live,MapMarkerSource markers,MapLocalIndexService local){mapLive=live;mapMarkers=markers;mapLocal=local;mapZoom=MapProjection.ClampZoom(configuration.MapView.OpenZoom==MapOpenZoom.Fixed ? configuration.MapView.OpenZoomValue:configuration.MapView.LastZoom);}
    // 0.542 (P1): the zoom chosen for the opening; a chosen map still opens whole.
    private readonly MapZoomMemory mapZoomMemory=new(TimeSpan.FromSeconds(2));
    private void ApplyOpenZoom()
    {
        var settings=configuration.MapView;
        if(settings.OpenZoom==MapOpenZoom.Fixed)mapZoom=MapProjection.ClampZoom(settings.OpenZoomValue);
        else if(settings.OpenZoom==MapOpenZoom.Whole){mapViewFollow=false;mapFitRequested=true;}
    }
    private float MapFocusZoom=>MapProjection.ClampZoom(configuration.MapView.FocusZoom);
    /// <summary>Each frame, also when the map closes just after a zoom. Only the last zoom is kept, and only when it has rested.</summary>
    internal void KeepMapZoom()
    {
        var settings=configuration.MapView;
        if(settings.OpenZoom!=MapOpenZoom.Last || mapZoomMemory.Tick(mapZoom,settings.LastZoom,TimeSpan.FromTicks(DateTimeOffset.UtcNow.UtcTicks)) is not { } rested)return;
        settings.LastZoom=rested;
        // X-76 H3 (0.914): the rested zoom stays in memory when saving fails (it is the zoom in use); the next save writes it.
        TrySave("Map zoom");
    }
    // 0.328 (map audit #13): a chosen map opens whole, like NavigateMap; the old zoom and pan belonged to another map.
    // 0.499 (user report): a link to the map brings it above the Tensho window instead of opening behind it.
    private bool mapViewToFront;
    // 0.850 (X-46, user report: the zoom does not always stay the one we set): another map opened whole whatever the opening setting (the game keeps the map last
    // viewed, so M often asked another map); it now takes the setting too (fixed, last, whole) and is centred, its old pan belonging to another map.
    private void OpenMapView(uint mapId){mapViewMapId=mapId;mapViewFollow=mapId==0;mapFitRequested=false;ApplyOpenZoom();CentreOtherMap(mapId);mapViewOpen=true;mapViewToFront=true;}
    private void CentreOtherMap(uint mapId){if(mapId!=0 && configuration.MapView.OpenZoom!=MapOpenZoom.Whole)mapCenterRequest=new Vector2(MapProjection.TextureSize/2f);}
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
        if(pointed && request.Territory!=0 && maps.State.Catalog?.MapOf(request.Territory) is { } questMap and not 0)mapQuestMap=questMap;
        if(pointed && mapId==0)mapId=request.Current;
        if(!pointed && mapViewOpen && mapId==0 && mapViewFollow)mapViewOpen=false;
        // A pointed request keeps its own zoom: the focus centres it, or fits the map once it is not found.
        else{OpenMapView(mapId);if(pointed){mapFitRequested=false;mapGameTarget=null;mapFocusRequest=request;mapFocusUntil=DateTimeOffset.UtcNow.AddSeconds(1.5);}}
    }
    // Texture pixel of the asked target on the shown map: its event marker (ObjectiveId), the quest giver, the aetheryte, the FATE.
    private Vector2? FocusPixel(GameMapRequest focus,MapDefinition map,MapLiveState? live)
    {
        // The game's own target (temporary marker) first, like Mappy: it is on the map the game opened.
        if(focus.UsesRoute(map) && focus.Route is { } route)return MapProjection.PixelFromWorld(new Vector3(route.X,0,route.Y),map);
        if(maps.State.Catalog is { } entranceCatalog && focus.EntranceTarget(entranceCatalog) is { } entrance && entrance.MapId==map.MapId)return new Vector2(entrance.X,entrance.Y);
        // 0.628 (user capture, icon in the sea): the target's coordinates are those of its territory: they mean nothing on a world or region map.
        if(focus.Target is { } target && map.TerritoryId!=0)return MapProjection.PixelFromWorld(new Vector3(target.X,0,target.Y),map);
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
    // 0.833 (X-63): the way into a zone with no aetheryte, shown in the teleport question for that aetheryte (Warn: its town has no aethernet).
    private (uint Id,string Text,bool Warn)? mapTeleportNote;
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
        // Read here, outside the tool column: inside a child these calls return the child's size (the « Restore » button bug, MapView.Restore).
        var window=(ImGui.GetWindowSize(),ImGui.GetWindowPos());
        if(mapViewFollow && live is { MapId:>0 })mapViewMapId=live.MapId;
        var map=catalog.Find(mapViewMapId) ?? mapMarkers?.Overview(mapViewMapId);
        var settings=configuration.MapView;
        mapOverlayHovered=false;
        if(settings.OverlayControls && map is not null && map.TexturePath.Length>0)
        {
            MeasureMap("map overlay layout",()=>DrawMapOverlayLayout(catalog,map,live,viewport,window));
            return;
        }
        // Layout of the game map: region and zone menus on top, tool column on the left, the map on the right.
        MeasureMap("map menus",()=>DrawMapMenus(catalog,map,live));
        MeasureMap("map tools",()=>DrawMapTools(map,viewport,window));
        ImGui.SameLine();
        if(map is null || map.TexturePath.Length==0)
        {
            // 0.328: no map because the character's position is unknown (not logged in, or the live read failed): said as such.
            ImGui.TextWrapped(localization.Get(map is null && live is null && mapViewFollow ? "MapView.PositionUnknown":"MapView.Unavailable"));
            return;
        }
        MeasureMap("map canvas",()=>DrawMapCanvas(map,live));
    }

    // 0.749 (X-01): the map's frame was 195 ms when it opened; its parts in the slow-frame log, developer mode only.
    // 0.791 (X-01: « map overlay layout » 110 to 160 ms several times an hour on 2026-11-07, not only at opening): checkpoints inside the canvas, developer mode.
    private long mapMarkAt;
    private void MapMarkStart(){if(DeveloperMode)mapMarkAt=System.Diagnostics.Stopwatch.GetTimestamp();}
    private uint leveLogged;
    private void MapMark(string part)
    {
        if(!DeveloperMode)return;
        var now=System.Diagnostics.Stopwatch.GetTimestamp();
        SlowFrames.Add(ImGui.GetFrameCount(),part,System.Diagnostics.Stopwatch.GetElapsedTime(mapMarkAt,now).TotalMilliseconds);
        mapMarkAt=now;
    }
    private static bool MonsterNoteShown(MonsterNote note,MapViewSettings settings)=>note.Kind switch
    {
        MonsterNoteKind.List=>settings.ShowListMonsters,
        MonsterNoteKind.HuntLog=>settings.ShowHuntLogMonsters,
        MonsterNoteKind.Fate=>settings.ShowFateMonsters,
        MonsterNoteKind.BlueSpell=>settings.ShowBlueMageMonsters,
        MonsterNoteKind.Collection=>settings.ShowCollectionMonsters,
        MonsterNoteKind.Beast=>settings.ShowBeastMonsters,
        MonsterNoteKind.Relic=>settings.ShowRelicMonsters,
        MonsterNoteKind.Wish=>settings.ShowWishMonsters,
        _=>true,
    };
    private void MeasureMap(string part,Action draw)
    {
        if(!DeveloperMode){draw();return;}
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{draw();}
        finally{SlowFrames.Add(ImGui.GetFrameCount(),part,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);}
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
        if(shown)MeasureMap("map overlay controls",()=>DrawMapOverlayControls(catalog,map,live,viewport,window,start,area,line,pad,indent,fade,grey));
        else
        {
            mapHeaderList=0;
            // 0.305 (user request): the shown map's name stays written while the mouse is away, like the game map (setting).
            if(settings.OverlayZoneName)
            {
                var name=MapName(map);var sub=map.TerritoryId!=0 && map.Subzone.Length>0 ? "> "+map.Subzone:null;
                var places=MapMenus.PlaceLines(live?.MapId ?? 0,map.MapId,live?.Area,live?.SubArea);
                var size=new Vector2(places.Select(place=>ImGui.CalcTextSize(place).X+indent).Append(ImGui.CalcTextSize(name).X).Append(sub is null ? 0:ImGui.CalcTextSize(sub).X+indent).Max()+pad*2,((sub is null ? 1:2)+places.Count)*line+pad*2);
                var at=start+new Vector2(38,0);
                HeaderBand("MapOverlayNameBand",at,size,fade,area-(at-start));
                ImGui.SetCursorPos(at);
                if(ImGui.BeginChild("MapOverlayName",Vector2.Min(size,area-(at-start)),false,ImGuiWindowFlags.NoBackground|ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoScrollbar))
                {
                    ImGui.SetCursorPos(new Vector2(pad,pad));
                    HeaderEntry("Name",name,UI.FfxivTheme.Gold,0,null,false);
                    if(sub is not null)HeaderEntry("Sub",sub,grey,indent,null,false);
                    for(var place=0;place<places.Count;place++)HeaderEntry($"Place{place}",places[place],grey,indent,null,false);
                }
                ImGui.EndChild();
            }
        }
        ImGui.SetCursorPos(start);
        MeasureMap("map canvas",()=>DrawMapCanvas(map,live));
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
        mapZoneMenu.Update(catalog,region,map,mapQuestMap);
        var zones=mapZoneMenu.Zones;
        var floors=mapZoneMenu.Floors;
        var isZone=map.TerritoryId!=0;var world=localization.Get("MapView.World");
        static string FloorName(MapDefinition floor)=>MapMenus.FloorLabel(floor);
        var regionName=region is not null ? MapName(region):world;var zoneName=isZone ? MapName(map):"--";
        // 0.632 (user captures: only « -- » under the world): the world's own floors, Hydaelyn and The First.
        var worldChoices=markers.WorldChoices(map.MapId);
        if(worldChoices.Count>0){regionName=world;zoneName=MapName(map);zones=[];}
        var arrow=ImGui.GetTextLineHeight();
        var widths=new List<float>{ImGui.CalcTextSize(regionName).X+arrow,ImGui.CalcTextSize(zoneName).X+arrow+(isZone ? 28:0)};
        var placeLines=MapMenus.PlaceLines(live?.MapId ?? 0,map.MapId,live?.Area,live?.SubArea);
        if(floors.Count>1)widths.Add(ImGui.CalcTextSize(FloorName(map)).X+arrow);
        else if(isZone && map.Subzone.Length>0)widths.Add(indent+ImGui.CalcTextSize("> "+map.Subzone).X);
        widths.AddRange(placeLines.Select(place=>indent+ImGui.CalcTextSize(place).X));
        var headerHeight=widths.Count*line+pad*2;
        var listCount=mapHeaderList switch{1=>regions.Length+1,2=>worldChoices.Count>0 ? worldChoices.Count:zones.Count+1,3=>floors.Count,_=>0};
        var at=start+new Vector2(toolsWidth+4,0);var room=area-(at-start);
        HeaderBand("MapOverlayBand",at,new Vector2(Math.Min(widths.Max()+pad*2+4,room.X),headerHeight),fade,room);
        // X-76 (0.956): the widest name of the lists, measured again only when the lists, the language or the font size change.
        var listKey=new MapMenuWidthKey(catalog,mapZoneMenu.Builds,localization.Revision,ImGui.GetFontSize());
        if(mapOverlayListKey!=listKey)
        {
            mapOverlayListKey=listKey;
            mapOverlayListText=regions.Select(MapName).Append(world).Concat(zones.Select(entry=>MapName(entry[0]))).Concat(floors.Select(FloorName)).Concat(worldChoices.Select(MapName))
                .Select(name=>ImGui.CalcTextSize("> "+name).X).DefaultIfEmpty(80).Max();
        }
        var listWidth=mapOverlayListText+indent+28;
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
                ListBackdrop(worldChoices.Count>0 ? worldChoices.Count:zones.Count+1);
                foreach(var choice in worldChoices)
                    if(HeaderEntry($"ZoneWorld{choice.MapId}","> "+MapName(choice),ListColour(choice.MapId==map.MapId),indent,null)){NavigateMap(choice.MapId);mapHeaderList=0;}
                if(worldChoices.Count==0 && HeaderEntry("ZoneRegion","> --",ListColour(!isZone),indent,null)){if(regionId!=0)NavigateMap(regionId);mapHeaderList=0;}
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
            for(var place=0;place<placeLines.Count;place++)HeaderEntry($"Place{place}",placeLines[place],grey,indent,null,false);
        }
        ImGui.EndChild();
        mapOverlayHovered|=headerHovered;
        // A click elsewhere on the map rolls the list up.
        if(mapHeaderList!=0 && !headerHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))mapHeaderList=0;

        static Vector4 ListColour(bool current)=>current ? UI.FfxivTheme.Gold:MapOverlayText;
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
    // X-76 (0.956): the zone list and floors kept until the shown map changes (MapZoneMenu), and the widest name of each menu kept per list, language and font size.
    private readonly MapZoneMenu mapZoneMenu=new();
    private readonly record struct MapMenuWidthKey(MapCatalog Catalog,int Lists,int LanguageRevision,float FontSize);
    private MapMenuWidthKey? mapMenuWidthKey,mapOverlayListKey;
    private float mapRegionNamesWidth,mapZoneNamesWidth,mapFloorNamesWidth,mapOverlayListText;
    private static string MapName(MapDefinition map)=>map.Zone.Length>0 ? map.Zone:map.Region;

    private void DrawMapMenus(MapCatalog catalog,MapDefinition? map,MapLiveState? live)
    {
        if(mapMarkers is null || map is null){ImGui.TextColored(Accent,localization.Get("MapView.NoMap"));return;}
        var regionId=mapMarkers.RegionOf(map);
        var regionMap=ResolveMap(catalog,regionId);
        mapZoneMenu.Update(catalog,regionMap,map,mapQuestMap);
        var zones=mapZoneMenu.Zones;
        var floors=mapZoneMenu.Floors;
        var worldChoices=mapMarkers.WorldChoices(map.MapId);
        var isZone=map.TerritoryId!=0;
        var zoneShown=isZone || worldChoices.Count>0 ? MapName(map):"--";
        var widthKey=new MapMenuWidthKey(catalog,mapZoneMenu.Builds,localization.Revision,ImGui.GetFontSize());
        if(mapMenuWidthKey!=widthKey)
        {
            mapMenuWidthKey=widthKey;
            mapRegionNamesWidth=MenuTextWidth(mapMarkers.Regions.Select(id=>ResolveMap(catalog,id)).OfType<MapDefinition>().Select(MapName).Append(localization.Get("MapView.World")));
            mapZoneNamesWidth=MenuTextWidth(zones.Select(entry=>MapName(entry[0])).Concat(worldChoices.Select(MapName)).Append(zoneShown));
            mapFloorNamesWidth=MenuTextWidth(floors.Select(MapMenus.FloorLabel));
        }
        ImGui.SetNextItemWidth(MenuWidth(mapRegionNamesWidth,false));
        if(ImGui.BeginCombo("##MapRegion",regionMap is { } region ? MapName(region):localization.Get("MapView.World")))
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
        ImGui.SetNextItemWidth(MenuWidth(mapZoneNamesWidth,true));
        var zoneMenu=ImGui.BeginCombo("##MapZone",zoneShown);Tip("MapView.ZoneTip");
        if(zoneMenu)
        {
            try
            {
                foreach(var choice in worldChoices)
                    if(ImGui.Selectable($"{MapName(choice)}##ZoneWorld{choice.MapId}",choice.MapId==map.MapId))NavigateMap(choice.MapId);
                if(worldChoices.Count==0 && ImGui.Selectable("--##ZoneRegion",!isZone) && regionId!=0)NavigateMap(regionId);
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
            ImGui.SameLine();ImGui.SetNextItemWidth(MenuWidth(mapFloorNamesWidth,false));
            if(ImGui.BeginCombo("##MapFloor",MapMenus.FloorLabel(map)))
            {
                try{foreach(var floor in floors)if(ImGui.Selectable($"{MapMenus.FloorLabel(floor)}##Floor{floor.MapId}",floor.MapId==map.MapId))NavigateMap(floor.MapId);}
                finally{ImGui.EndCombo();}
            }
            Tip("MapView.FloorTip");
        }
        else if(isZone && map.Subzone.Length>0){ImGui.SameLine();ImGui.TextDisabled(map.Subzone);}
    }

    // Menu as wide as its longest entry plus the arrow (and the wing column for zones), so names are never cut or padded.
    // X-76 (0.956): the longest entry is measured when its list changes (MenuTextWidth); the frame's padding is added on every frame.
    private static float MenuTextWidth(IEnumerable<string> names)=>names.Select(name=>ImGui.CalcTextSize(name).X).DefaultIfEmpty(40).Max();
    private static float MenuWidth(float text,bool wing)
    {
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
        // X-164 (0.1130): under the buttons, the typed percentage and the « 100% » button (two frame rows).
        var style=ImGui.GetStyle();var step=26+style.ItemSpacing.Y;var label=2*ImGui.GetFrameHeightWithSpacing();
        var room=maxHeight>0 ? maxHeight:ImGui.GetContentRegionAvail().Y;
        var slider=Math.Clamp(room-9*step-label-2*style.ItemSpacing.Y-style.WindowPadding.Y*2,0,80);
        if(slider<30)slider=0;
        const int buttons=9;
        var perColumn=slider>0 ? buttons:Math.Clamp((int)((room-label-style.WindowPadding.Y*2)/step),1,buttons);
        var columns=(buttons+perColumn-1)/perColumn;
        var width=Math.Max(34,ImGui.CalcTextSize("100%").X+style.FramePadding.X+style.WindowPadding.X)+(columns-1)*30;
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
            // 0.547 (P20): the layers in one click, the same settings as the cog's.
            Place();if(ToolButton(FontAwesomeIcon.LayerGroup,"MapView.Layers",true,ImGui.IsPopupOpen("MapViewLayers")))ImGui.OpenPopup("MapViewLayers");
            DrawMapLayers();
            // 0.587 (user request): too many settings for a popup: the cog opens the Map tab of Settings.
            Place();if(ToolButton(FontAwesomeIcon.Cog,"MapView.Settings",true,false)){page=Page.Settings;requestedSettingsTab="Map";IsOpen=true;BringToFront();}
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
            // X-164 (0.1130, user request): the percentage is typed with the keyboard (Enter applies it, clamped to 15–400 %), and
            // « 100% » under it, a button like the others, puts the zoom back to 100 %.
            var text=$"{mapZoom*100:0}%";
            var lastColumn=slider>0 ? 0:(buttons-1)/perColumn;
            if(slider<=0)ImGui.SetCursorPosY(top+((buttons-1)%perColumn+1)*step);
            var columnX=left+lastColumn*30;
            var fieldWidth=Math.Max(26,ImGui.CalcTextSize("100%").X+style.FramePadding.X);
            ImGui.SetCursorPosX(Math.Max(0,columnX));
            if(overlay)ImGui.PushStyleColor(ImGuiCol.Text,MapOverlayText);
            try
            {
                ImGui.SetNextItemWidth(fieldWidth);
                var typed=text;
                if(ImGui.InputText("##MapZoomTyped",ref typed,8,ImGuiInputTextFlags.CharsDecimal|ImGuiInputTextFlags.AutoSelectAll|ImGuiInputTextFlags.EnterReturnsTrue)
                   && MapProjection.ZoomFromPercent(typed) is { } typedZoom)ZoomTo(typedZoom);
            }
            finally{if(overlay)ImGui.PopStyleColor();}
            Tip("MapView.ZoomLevel");
            ImGui.SetCursorPosX(Math.Max(0,columnX));
            if(TextToolButton("100%","MapView.Zoom100"))ZoomTo(1);
        }
        finally{ImGui.EndChild();}
        return width;
    }
    private void DrawMapLayers()
    {
        if(!ImGui.BeginPopup("MapViewLayers"))return;
        try
        {
            var settings=configuration.MapView;var changed=false;
            void All(bool shown)
            {
                settings.ShowMiningPoints=settings.ShowBotanyPoints=settings.ShowFishingPoints=shown;
                settings.ShowQuests=settings.ShowFates=settings.ShowGathering=settings.ShowVendors=settings.ShowSightseeing=settings.ShowAetherCurrents=shown;
                settings.ShowParty=settings.ShowBookmarks=settings.ShowHuntMarks=settings.ShowTreasure=settings.ShowFixedIcons=settings.ShowPlaceNames=settings.ShowIconNames=shown;
                settings.ShowIslandPlants=settings.ShowIslandWood=settings.ShowIslandRocks=settings.ShowIslandEarth=settings.ShowIslandFishing=settings.ShowIslandAnimals=shown;
                // 0.902 (user report: the monster layers were left out of All and None).
                settings.ShowListMonsters=settings.ShowHuntLogMonsters=settings.ShowFateMonsters=settings.ShowBlueMageMonsters=settings.ShowCollectionMonsters=settings.ShowBeastMonsters=settings.ShowRelicMonsters=settings.ShowWishMonsters=shown;
                changed=true;
            }
            if(ImGui.SmallButton(localization.Get("MapView.LayersAll")))All(true);
            Tip("MapView.LayersAllTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("MapView.LayersNone")))All(false);
            Tip("MapView.LayersNoneTip");
            ImGui.Separator();
            changed|=SettingBox("MapView.ShowFixedIcons",settings.ShowFixedIcons,value=>settings.ShowFixedIcons=value);
            changed|=SettingBox("MapView.ShowPlaceNames",settings.ShowPlaceNames,value=>settings.ShowPlaceNames=value);
            changed|=SettingBox("MapView.ShowIconNames",settings.ShowIconNames,value=>settings.ShowIconNames=value);
            changed|=SettingBox("MapView.ShowQuests",settings.ShowQuests,value=>settings.ShowQuests=value);
            changed|=SettingBox("MapView.ShowFates",settings.ShowFates,value=>settings.ShowFates=value);
            changed|=SettingBox("MapView.ShowGathering",settings.ShowGathering,value=>settings.ShowGathering=value);
            changed|=GathererBoxes(settings,false);
            // 0.684 (user request): the game's five kinds, in this menu only.
            changed|=SettingBox("MapView.ShowIslandPlants",settings.ShowIslandPlants,value=>settings.ShowIslandPlants=value);
            changed|=SettingBox("MapView.ShowIslandWood",settings.ShowIslandWood,value=>settings.ShowIslandWood=value);
            changed|=SettingBox("MapView.ShowIslandRocks",settings.ShowIslandRocks,value=>settings.ShowIslandRocks=value);
            changed|=SettingBox("MapView.ShowIslandEarth",settings.ShowIslandEarth,value=>settings.ShowIslandEarth=value);
            changed|=SettingBox("MapView.ShowIslandFishing",settings.ShowIslandFishing,value=>settings.ShowIslandFishing=value);
            changed|=SettingBox("MapView.ShowIslandAnimals",settings.ShowIslandAnimals,value=>settings.ShowIslandAnimals=value);
            changed|=SettingBox("MapView.ShowVendors",settings.ShowVendors,value=>settings.ShowVendors=value);
            changed|=SettingBox("MapView.ShowSightseeing",settings.ShowSightseeing,value=>settings.ShowSightseeing=value);
            changed|=SettingBox("MapView.ShowAetherCurrents",settings.ShowAetherCurrents,value=>settings.ShowAetherCurrents=value);
            changed|=SettingBox("MapView.ShowParty",settings.ShowParty,value=>settings.ShowParty=value);
            changed|=SettingBox("MapView.ShowBookmarks",settings.ShowBookmarks,value=>settings.ShowBookmarks=value);
            changed|=SettingBox("MapView.ShowHuntMarks",settings.ShowHuntMarks,value=>settings.ShowHuntMarks=value);
            changed|=SettingBox("MapView.ShowListMonsters",settings.ShowListMonsters,value=>settings.ShowListMonsters=value);
            changed|=SettingBox("MapView.ShowHuntLogMonsters",settings.ShowHuntLogMonsters,value=>settings.ShowHuntLogMonsters=value);
            changed|=SettingBox("MapView.ShowFateMonsters",settings.ShowFateMonsters,value=>settings.ShowFateMonsters=value);
            changed|=SettingBox("MapView.ShowBlueMageMonsters",settings.ShowBlueMageMonsters,value=>settings.ShowBlueMageMonsters=value);
            changed|=SettingBox("MapView.ShowCollectionMonsters",settings.ShowCollectionMonsters,value=>settings.ShowCollectionMonsters=value);
            changed|=SettingBox("MapView.ShowBeastMonsters",settings.ShowBeastMonsters,value=>settings.ShowBeastMonsters=value);
            changed|=SettingBox("MapView.ShowRelicMonsters",settings.ShowRelicMonsters,value=>settings.ShowRelicMonsters=value);
            changed|=SettingBox("MapView.ShowWishMonsters",settings.ShowWishMonsters,value=>settings.ShowWishMonsters=value);
            changed|=SettingBox("MapView.ShowTreasure",settings.ShowTreasure,value=>settings.ShowTreasure=value);
            // X-76 H3 (0.914): the layers shown stay as ticked when saving fails (the next save writes them). This menu is in the map
            // window, where the main window's footer is not seen, so it keeps its own error line.
            if(changed)TrySave("Map layers");
            if(saveError is { } failed)ImGui.TextColored(Error,failed);
        }
        finally{ImGui.EndPopup();}
    }
    // Mappy-like display settings, saved at each change; every layer can be turned off.
    // Settings › Map, opened by the map's cog since 0.587.
    private void DrawMapSettingsBody(MapDefinition? map)
    {
        {
            var settings=configuration.MapView;var changed=false;
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionGame"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowFixedIcons",settings.ShowFixedIcons,value=>settings.ShowFixedIcons=value,MapDefaults.ShowFixedIcons);
                changed|=SettingBox("MapView.HideUndiscovered",settings.HideUndiscovered,value=>settings.HideUndiscovered=value,MapDefaults.HideUndiscovered);
                changed|=SettingBox("MapView.TeleportOnClick",settings.TeleportOnClick,value=>settings.TeleportOnClick=value,MapDefaults.TeleportOnClick);
                changed|=SettingBox("MapView.ChatLinks",settings.ChatLinks,value=>settings.ChatLinks=value,MapDefaults.ChatLinks);
                changed|=SettingBox("MapView.ShowPlaceNames",settings.ShowPlaceNames,value=>settings.ShowPlaceNames=value,MapDefaults.ShowPlaceNames);
                // X-76 step 3 (0.953): icon names are the names of the game map icons (item.Fixed is set only by the fixed-icon loops):
                // greyed while those icons are off.
                ImGui.BeginDisabled(!settings.ShowFixedIcons);
                try{changed|=SettingBox("MapView.ShowIconNames",settings.ShowIconNames,value=>settings.ShowIconNames=value,MapDefaults.ShowIconNames);}
                finally{ImGui.EndDisabled();}
                // X-76 step 3 (0.953): the name size, zoom follow and minimum zoom are read only for place names and icon names: indented
                // under them, greyed while neither is drawn.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowPlaceNames && !(settings.ShowIconNames && settings.ShowFixedIcons));
                try
                {
                    var names=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.PlaceNameScale"),ref names,50,150,"%.0f %%")){settings.PlaceNameScale=names/100;changed=true;}
                    Tip("SettingTips.PlaceNameScale");
                    if(DefaultButton("PlaceNameScale",settings.PlaceNameScale!=MapDefaults.PlaceNameScale)){settings.PlaceNameScale=MapDefaults.PlaceNameScale;changed=true;}
                    var nameFollow=MapViewSettings.ClampFollow(settings.NameZoomFollow ?? settings.ZoomFollow)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.NameZoomFollow"),ref nameFollow,0,100,"%.0f %%")){settings.NameZoomFollow=nameFollow/100;changed=true;}
                    Tip("MapView.NameZoomFollowHelp");
                    if(DefaultButton("NameZoomFollow",settings.NameZoomFollow is not null)){settings.NameZoomFollow=null;changed=true;}
                    var nameMinimum=Math.Clamp(settings.NameMinZoom,0,4)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.NameMinZoom"),ref nameMinimum,0,200,"%.0f %%")){settings.NameMinZoom=nameMinimum/100;changed=true;}
                    Tip("MapView.NameMinZoomHelp");
                    if(DefaultButton("NameMinZoom",settings.NameMinZoom!=MapDefaults.NameMinZoom)){settings.NameMinZoom=MapDefaults.NameMinZoom;changed=true;}
                }
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                changed|=SettingBox("MapView.FlagOnClick",settings.FlagOnClick,value=>settings.FlagOnClick=value,MapDefaults.FlagOnClick);
                changed|=SettingBox("MapView.ShowFlag",settings.ShowFlag,value=>settings.ShowFlag=value,MapDefaults.ShowFlag);
                changed|=SettingBox("MapView.ShowPlayerCoordinates",settings.ShowPlayerCoordinates,value=>settings.ShowPlayerCoordinates=value,MapDefaults.ShowPlayerCoordinates);
                changed|=SettingBox("MapView.ShowBookmarks",settings.ShowBookmarks,value=>settings.ShowBookmarks=value,MapDefaults.ShowBookmarks);
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionQuests"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowQuests",settings.ShowQuests,value=>settings.ShowQuests=value,MapDefaults.ShowQuests);
                // X-76 step 3 (0.953): read only for the quest markers, drawn only with « Quests and objectives »: indented and greyed under it.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowQuests);
                try
                {
                    changed|=SettingBox("MapView.QuestTooltips",settings.QuestTooltips,value=>settings.QuestTooltips=value,MapDefaults.QuestTooltips);
                    changed|=SettingBox("MapView.ShowLockedQuests",settings.ShowLockedQuests,value=>settings.ShowLockedQuests=value,MapDefaults.ShowLockedQuests);
                }
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                changed|=SettingBox("MapView.ShowFates",settings.ShowFates,value=>settings.ShowFates=value,MapDefaults.ShowFates);
                changed|=SettingBox("MapView.ShowSightseeing",settings.ShowSightseeing,value=>settings.ShowSightseeing=value,MapDefaults.ShowSightseeing);
                changed|=SettingBox("MapView.ShowAetherCurrents",settings.ShowAetherCurrents,value=>settings.ShowAetherCurrents=value,MapDefaults.ShowAetherCurrents);
                // X-76 step 3 (0.953): read only inside the vista and aether current loops: indented under them, greyed while both are off.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowSightseeing && !settings.ShowAetherCurrents);
                try{changed|=SettingBox("MapView.HideCollected",settings.HideCollected,value=>settings.HideCollected=value,MapDefaults.HideCollected);}
                finally{ImGui.EndDisabled();ImGui.Unindent();}
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionNearby"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowTreasure",settings.ShowTreasure,value=>settings.ShowTreasure=value,MapDefaults.ShowTreasure);
                changed|=SettingBox("MapView.ShowVendors",settings.ShowVendors,value=>settings.ShowVendors=value,MapDefaults.ShowVendors);
                changed|=SettingBox("MapView.ShowGathering",settings.ShowGathering,value=>settings.ShowGathering=value,MapDefaults.ShowGathering);
                changed|=GathererBoxes(settings,true);
                // X-76 step 3 (0.953): the level labels are drawn only for gathering points (item.Point), and their zoom and size only with the
                // labels: moved here from the end of this section, each indented and greyed under its switch.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowGathering);
                try
                {
                    changed|=SettingBox("MapView.GatheringLabels",settings.GatheringLabels,value=>settings.GatheringLabels=value,MapDefaults.GatheringLabels);
                    ImGui.Indent();
                    ImGui.BeginDisabled(!settings.GatheringLabels);
                    try
                    {
                        var labelMinimum=Math.Clamp(settings.GatheringLabelMinZoom,0,4)*100;ImGui.SetNextItemWidth(160);
                        if(ImGui.SliderFloat(localization.Get("MapView.GatheringLabelMinZoom"),ref labelMinimum,0,200,"%.0f %%")){settings.GatheringLabelMinZoom=labelMinimum/100;changed=true;}
                        Tip("MapView.GatheringLabelMinZoomHelp");
                        if(DefaultButton("GatheringLabelMinZoom",settings.GatheringLabelMinZoom!=MapDefaults.GatheringLabelMinZoom)){settings.GatheringLabelMinZoom=MapDefaults.GatheringLabelMinZoom;changed=true;}
                        var labelScale=Math.Clamp(settings.GatheringLabelScale,0.5f,2f)*100;ImGui.SetNextItemWidth(160);
                        if(ImGui.SliderFloat(localization.Get("MapView.GatheringLabelScale"),ref labelScale,50,200,"%.0f %%")){settings.GatheringLabelScale=labelScale/100;changed=true;}
                        Tip("MapView.GatheringLabelScaleHelp");
                        if(DefaultButton("GatheringLabelScale",settings.GatheringLabelScale!=MapDefaults.GatheringLabelScale)){settings.GatheringLabelScale=MapDefaults.GatheringLabelScale;changed=true;}
                    }
                    finally{ImGui.EndDisabled();ImGui.Unindent();}
                }
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                changed|=SettingBox("MapView.ShowHuntMarks",settings.ShowHuntMarks,value=>settings.ShowHuntMarks=value,MapDefaults.ShowHuntMarks);
                changed|=SettingBox("MapView.ShowListMonsters",settings.ShowListMonsters,value=>settings.ShowListMonsters=value,MapDefaults.ShowListMonsters);
                // X-40 (0.1002): every crafting list, not only the one in a panel; greyed while the list layer is off.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowListMonsters && !configuration.ListPanelsNearbyMonsters);
                try{changed|=SettingBox("MapView.ListMonstersAllLists",settings.ListMonstersAllLists,value=>settings.ListMonstersAllLists=value,MapDefaults.ListMonstersAllLists);}
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                changed|=SettingBox("MapView.ShowHuntLogMonsters",settings.ShowHuntLogMonsters,value=>settings.ShowHuntLogMonsters=value,MapDefaults.ShowHuntLogMonsters);
                changed|=SettingBox("MapView.ShowFateMonsters",settings.ShowFateMonsters,value=>settings.ShowFateMonsters=value,MapDefaults.ShowFateMonsters);
                changed|=SettingBox("MapView.ShowBlueMageMonsters",settings.ShowBlueMageMonsters,value=>settings.ShowBlueMageMonsters=value,MapDefaults.ShowBlueMageMonsters);
                // X-76 step 3 (0.953): « even when not Blue Mage » is read only with its switch (MainWindow.NearbyMonsters.cs): right under it,
                // indented and greyed while it is off.
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowBlueMageMonsters);
                try{changed|=SettingBox("MapView.BlueMageMonstersAlways",settings.BlueMageMonstersAlways,value=>settings.BlueMageMonstersAlways=value,MapDefaults.BlueMageMonstersAlways);}
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                changed|=SettingBox("MapView.ShowCollectionMonsters",settings.ShowCollectionMonsters,value=>settings.ShowCollectionMonsters=value,MapDefaults.ShowCollectionMonsters);
                changed|=SettingBox("MapView.ShowBeastMonsters",settings.ShowBeastMonsters,value=>settings.ShowBeastMonsters=value,MapDefaults.ShowBeastMonsters);
                // X-76 step 3 (0.953): same for « even when not Beastmaster ».
                ImGui.Indent();
                ImGui.BeginDisabled(!settings.ShowBeastMonsters);
                try{changed|=SettingBox("MapView.BeastMonstersAlways",settings.BeastMonstersAlways,value=>settings.BeastMonstersAlways=value,MapDefaults.BeastMonstersAlways);}
                finally{ImGui.EndDisabled();ImGui.Unindent();}
                // X-40 (0.1002, DEC-049): relic book targets and wish drops.
                changed|=SettingBox("MapView.ShowRelicMonsters",settings.ShowRelicMonsters,value=>settings.ShowRelicMonsters=value,MapDefaults.ShowRelicMonsters);
                changed|=SettingBox("MapView.ShowWishMonsters",settings.ShowWishMonsters,value=>settings.ShowWishMonsters=value,MapDefaults.ShowWishMonsters);
                // 0.799 (X-38): icons above the monsters' heads, in the game's view.
                changed|=SettingBox("MapView.MonsterIconsAbove",settings.MonsterIconsAbove,value=>settings.MonsterIconsAbove=value,MapDefaults.MonsterIconsAbove);
                if(settings.MonsterIconsAbove)
                {
                    ImGui.Indent();
                    // X-76 step 3 (0.953): an overhead icon shows only what was noted, and each kind is noted only while its switch above is on
                    // (MainWindow.NearbyMonsters.cs; the list drops also with the list panels' « nearby monsters »): each row greyed while it is off.
                    ImGui.BeginDisabled(!settings.ShowBlueMageMonsters);
                    try{changed|=SettingBox("MapView.AboveBlueSpell",settings.AboveBlueSpell,value=>settings.AboveBlueSpell=value,MapDefaults.AboveBlueSpell);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowListMonsters && !configuration.ListPanelsNearbyMonsters);
                    try{changed|=SettingBox("MapView.AboveList",settings.AboveList,value=>settings.AboveList=value,MapDefaults.AboveList);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowHuntLogMonsters);
                    try{changed|=SettingBox("MapView.AboveHuntLog",settings.AboveHuntLog,value=>settings.AboveHuntLog=value,MapDefaults.AboveHuntLog);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowFateMonsters);
                    try{changed|=SettingBox("MapView.AboveFate",settings.AboveFate,value=>settings.AboveFate=value,MapDefaults.AboveFate);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowCollectionMonsters);
                    try{changed|=SettingBox("MapView.AboveCollection",settings.AboveCollection,value=>settings.AboveCollection=value,MapDefaults.AboveCollection);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowBeastMonsters);
                    try{changed|=SettingBox("MapView.AboveBeast",settings.AboveBeast,value=>settings.AboveBeast=value,MapDefaults.AboveBeast);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowRelicMonsters);
                    try{changed|=SettingBox("MapView.AboveRelic",settings.AboveRelic,value=>settings.AboveRelic=value,MapDefaults.AboveRelic);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowWishMonsters);
                    try{changed|=SettingBox("MapView.AboveWish",settings.AboveWish,value=>settings.AboveWish=value,MapDefaults.AboveWish);}
                    finally{ImGui.EndDisabled();}
                    changed|=SettingBox("MapView.MonsterIconsHideInCombat",settings.MonsterIconsHideInCombat,value=>settings.MonsterIconsHideInCombat=value,MapDefaults.MonsterIconsHideInCombat);
                    var iconSize=Math.Clamp(settings.MonsterIconSize,16,96);ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderInt(localization.Get("MapView.MonsterIconSize"),ref iconSize,16,96)){settings.MonsterIconSize=iconSize;changed=true;}
                    Tip("MapView.MonsterIconSizeTip");
                    var iconDistance=Math.Clamp(settings.MonsterIconDistance,5,150);ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.MonsterIconDistance"),ref iconDistance,5,150,"%.0f")){settings.MonsterIconDistance=iconDistance;changed=true;}
                    Tip("MapView.MonsterIconDistanceTip");
                    ImGui.Unindent();
                }
                // 0.844 (X-67): the arrows above or below, and their threshold.
                changed|=SettingBox("MapView.HeightArrows",settings.HeightArrows,value=>settings.HeightArrows=value,MapDefaults.HeightArrows);
                if(settings.HeightArrows)
                {
                    var heightYalms=Math.Clamp(settings.HeightArrowYalms,2,50);ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.HeightArrowYalms"),ref heightYalms,2,50,"%.0f")){settings.HeightArrowYalms=heightYalms;changed=true;}
                    Tip("MapView.HeightArrowYalmsTip");
                    if(DefaultButton("HeightArrowYalms",settings.HeightArrowYalms!=MapDefaults.HeightArrowYalms)){settings.HeightArrowYalms=MapDefaults.HeightArrowYalms;changed=true;}
                }
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
                    // X-76 step 3 (0.953): DrawEdgeMarkers brings back only what the map draws: each kind also greyed while its map layer is off
                    // (character, flag, party; quests, leves and events come from « Quests and objectives », FATEs and gathering points from
                    // their layers). The flag's coordinates go with the flag's marker. The journal target has no layer of its own.
                    ImGui.BeginDisabled(!settings.ShowPlayer);
                    try{changed|=SettingBox("MapView.EdgePlayerOn",settings.EdgePlayer,value=>settings.EdgePlayer=value,MapDefaults.EdgePlayer);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowFlag);
                    try
                    {
                        changed|=SettingBox("MapView.EdgeFlagOn",settings.EdgeFlag,value=>settings.EdgeFlag=value,MapDefaults.EdgeFlag);
                        ImGui.Indent();
                        ImGui.BeginDisabled(!settings.EdgeFlag);
                        try{changed|=SettingBox("MapView.EdgeFlagCoordinatesOn",settings.EdgeFlagCoordinates,value=>settings.EdgeFlagCoordinates=value,MapDefaults.EdgeFlagCoordinates);}
                        finally{ImGui.EndDisabled();ImGui.Unindent();}
                    }
                    finally{ImGui.EndDisabled();}
                    changed|=SettingBox("MapView.EdgeTargetOn",settings.EdgeTarget,value=>settings.EdgeTarget=value,MapDefaults.EdgeTarget);
                    ImGui.BeginDisabled(!settings.ShowQuests);
                    try
                    {
                        changed|=SettingBox("MapView.EdgeQuestsOn",settings.EdgeQuests,value=>settings.EdgeQuests=value,MapDefaults.EdgeQuests);
                        changed|=SettingBox("MapView.EdgeLevesOn",settings.EdgeLeves,value=>settings.EdgeLeves=value,MapDefaults.EdgeLeves);
                        changed|=SettingBox("MapView.EdgeEventsOn",settings.EdgeEvents,value=>settings.EdgeEvents=value,MapDefaults.EdgeEvents);
                    }
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowFates);
                    try{changed|=SettingBox("MapView.EdgeFatesOn",settings.EdgeFates,value=>settings.EdgeFates=value,MapDefaults.EdgeFates);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowGathering);
                    try{changed|=SettingBox("MapView.EdgeGatheringOn",settings.EdgeGathering,value=>settings.EdgeGathering=value,MapDefaults.EdgeGathering);}
                    finally{ImGui.EndDisabled();}
                    ImGui.BeginDisabled(!settings.ShowParty);
                    try{changed|=SettingBox("MapView.EdgePartyOn",settings.EdgeParty,value=>settings.EdgeParty=value,MapDefaults.EdgeParty);}
                    finally{ImGui.EndDisabled();}
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
                // 0.542 (P1).
                ImGui.SetNextItemWidth(160);
                if(ImGui.BeginCombo(localization.Get("MapView.OpenZoom")+"###MapOpenZoom",localization.Get("MapView.OpenZoom"+settings.OpenZoom)))
                {
                    foreach(var mode in Enum.GetValues<MapOpenZoom>())
                        if(ImGui.Selectable(localization.Get("MapView.OpenZoom"+mode),settings.OpenZoom==mode)){settings.OpenZoom=mode;changed=true;}
                    ImGui.EndCombo();
                }
                Tip("MapView.OpenZoomHelp");
                if(DefaultButton("OpenZoom",settings.OpenZoom!=MapDefaults.OpenZoom)){settings.OpenZoom=MapDefaults.OpenZoom;changed=true;}
                if(settings.OpenZoom==MapOpenZoom.Fixed)
                {
                    var opening=MapProjection.ClampZoom(settings.OpenZoomValue)*100;ImGui.SetNextItemWidth(160);
                    if(ImGui.SliderFloat(localization.Get("MapView.OpenZoomValue"),ref opening,15,400,"%.0f %%")){settings.OpenZoomValue=opening/100;changed=true;}
                    Tip("MapView.OpenZoomValueHelp");
                    if(DefaultButton("OpenZoomValue",settings.OpenZoomValue!=MapDefaults.OpenZoomValue)){settings.OpenZoomValue=MapDefaults.OpenZoomValue;changed=true;}
                }
                var focus=MapFocusZoom*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.FocusZoom"),ref focus,15,400,"%.0f %%")){settings.FocusZoom=focus/100;changed=true;}
                Tip("MapView.FocusZoomHelp");
                if(DefaultButton("FocusZoom",settings.FocusZoom!=MapDefaults.FocusZoom)){settings.FocusZoom=MapDefaults.FocusZoom;changed=true;}
                var wheel=(Math.Clamp(float.IsFinite(settings.WheelStep) ? settings.WheelStep:1.15f,1.02f,1.5f)-1)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.WheelStep"),ref wheel,2,50,"%.0f %%")){settings.WheelStep=1+wheel/100;changed=true;}
                Tip("MapView.WheelStepHelp");
                if(DefaultButton("WheelStep",settings.WheelStep!=MapDefaults.WheelStep)){settings.WheelStep=MapDefaults.WheelStep;changed=true;}
                changed|=SettingBox("MapView.WheelInvert",settings.WheelInvert,value=>settings.WheelInvert=value,MapDefaults.WheelInvert);
                var follow=MapViewSettings.ClampFollow(settings.ZoomFollow)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.ZoomFollow"),ref follow,0,100,"%.0f %%")){settings.ZoomFollow=follow/100;changed=true;}
                Tip("MapView.ZoomFollowHelp");
                if(DefaultButton("ZoomFollow",settings.ZoomFollow!=MapDefaults.ZoomFollow)){settings.ZoomFollow=MapDefaults.ZoomFollow;changed=true;}
                var opacity=MapViewSettings.ClampOpacity(settings.Opacity)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.Opacity"),ref opacity,20,100,"%.0f %%")){settings.Opacity=opacity/100;changed=true;}
                Tip("SettingTips.Opacity");
                if(DefaultButton("Opacity",settings.Opacity!=MapDefaults.Opacity)){settings.Opacity=MapDefaults.Opacity;changed=true;}
            }
            if(TenshoAssistant.UI.UiComponents.Header(localization.Get("MapView.SectionAreas")))
            {
                changed|=AreaSetting("MapView.AreaQuests",settings.ShowQuestAreas,value=>settings.ShowQuestAreas=value,settings.QuestAreaColor,value=>settings.QuestAreaColor=value,MapDefaults.ShowQuestAreas,MapDefaults.QuestAreaColor);
                changed|=AreaSetting("MapView.AreaLeves",settings.ShowLeveAreas,value=>settings.ShowLeveAreas=value,settings.LeveAreaColor,value=>settings.LeveAreaColor=value,MapDefaults.ShowLeveAreas,MapDefaults.LeveAreaColor);
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
                changed|=SettingBox("MapView.HideBehindGame",settings.HideBehindGame,value=>settings.HideBehindGame=value,MapDefaults.HideBehindGame);
                changed|=SettingBox("MapView.HideInCombat",settings.HideInCombat,value=>settings.HideInCombat=value,MapDefaults.HideInCombat);
                changed|=SettingBox("MapView.HideInDuty",settings.HideInDuty,value=>settings.HideInDuty=value,MapDefaults.HideInDuty);
            }
            ImGui.TextDisabled(localization.Get("MapView.SettingsHelp"));
            // 0.328 (map audit #41): a failed save is said (Common.SaveError), like the other settings. X-76 H4 (0.914): every change
            // applies at once (the map follows a slider while it is dragged), but the file is written only once no control is held any
            // more: on release of a slider or a colour picker, when a typed number is left, at once for a box or a « ↺ ».
            mapSettingsPending|=changed;
            if(mapSettingsPending && !ImGui.IsAnyItemActive())SaveMapSettings();
        }
    }
    // X-76 H4 (0.914): map settings changed in memory and not written yet (a slider, colour or number still held). Any save writes the
    // whole configuration, so a change left pending when the page closes mid-drag is also written by the next save of anything.
    private bool mapSettingsPending;
    // The values stay in memory when saving fails (the map shows them); the next save writes them.
    private void SaveMapSettings()
    {
        mapSettingsPending=false;
        TrySave("Map settings");
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
            var sameLand=SameLandMaps(map);
            foreach(var marker in live.Events ?? [])if(sameLand.Contains(marker.MapId)){icons.Add(marker.Icon);eventIcons.Add(marker.Icon);}
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
        if(settings.ShowIslandAnimals && map.TerritoryId==IslandResources.Territory)icons.Add(GlyphIslandAnimal);
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
                else DrawGameIcon(icon,new Vector2(ImGui.GetFrameHeight()));
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
                else Tip("MapView.IconsPriority");
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
                if(TenshoGlyph(icon) is not null)Tip("MapView.TenshoIconHelp");
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
        Tip(key+"Help");
        ImGui.SameLine(ImGui.GetFontSize()*14);
        if(ImGui.ColorEdit4("##Colour"+key,ref colour,ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf)){applyColour(colour);changed=true;}
        Tip("SettingTips.AreaColour");
        if(DefaultButton("Area"+key,shown!=shownByDefault || colour!=colourByDefault)){applyShown(shownByDefault);applyColour(colourByDefault);changed=true;}
        return changed;
    }
    // 0.262: a map setting box with its help (key + « Help » when written) and the « ↺ » button once it differs from the default.
    /// <summary>X-80 (0.895): the three gatherers under « Gathering points », greyed while it is off.</summary>
    private bool GathererBoxes(MapViewSettings settings,bool defaults)
    {
        var changed=false;
        ImGui.Indent();
        using(ImRaii.Disabled(!settings.ShowGathering))
        {
            changed|=SettingBox("MapView.ShowMiningPoints",settings.ShowMiningPoints,value=>settings.ShowMiningPoints=value,defaults ? MapDefaults.ShowMiningPoints:null);
            changed|=SettingBox("MapView.ShowBotanyPoints",settings.ShowBotanyPoints,value=>settings.ShowBotanyPoints=value,defaults ? MapDefaults.ShowBotanyPoints:null);
            changed|=SettingBox("MapView.ShowFishingPoints",settings.ShowFishingPoints,value=>settings.ShowFishingPoints=value,defaults ? MapDefaults.ShowFishingPoints:null);
        }
        ImGui.Unindent();
        return changed;
    }

    private bool SettingBox(string key,bool value,Action<bool> apply,bool? fallback=null)
    {
        var changed=ImGui.Checkbox(localization.Get(key)+"###"+key,ref value);
        if(changed)apply(value);
        // 0.328 (map audit #39): also when greyed (edge kinds, zone name), whose help says what they depend on.
        if(localization.Has(key+"Help"))Tip(key+"Help");
        if(fallback is { } byDefault && DefaultButton(key,value!=byDefault)){apply(byDefault);changed=true;}
        return changed;
    }

    private (Vector2 Size,Vector2 Position)? mapResize;
    private (Vector2 Size,Vector2 Position)? mapRestore;
    private bool mapFitRequested;
    // Texture pixel to bring to the centre of the canvas on the next frame (flag placed from the Tensho map, 0.116).
    private Vector2? mapCenterRequest;
    // Enlarge/restore: the new window size only exists two frames later, so the fit waits for it (0.118, user report).
    private int mapFitDelay;
    // Quest, aetheryte or FATE the game asked to show (journal click), focused once the target map is drawn (0.118).
    private uint mapQuestMap;
    private GameMapRequest? mapFocusRequest;private DateTimeOffset mapFocusUntil;
    // The game's temporary marker (quest or gathering target) is not an event marker: it is kept and drawn here (0.122) until the
    // next game request, since closing the game map would otherwise lose it.
    private (uint MapId,Vector2 World,uint Icon,uint QuestId)? mapGameTarget;
    private Quests.QuestState? mapGameTargetQuests;
    internal IReadOnlyList<IslandPoint>? IslandPoints { get; set; }
    private string? mapGameTargetLeve;
    private DateTimeOffset mapGameTargetSince,mapGameTargetRefresh;
    // 0.813 (X-49): the accepted quest whose point stood where the target was set; 0 = none found yet.
    private uint mapGameTargetObjective;

    // 0.243 (R-01): a journal target belongs to its quest; once that quest is no longer active (completed or abandoned), the
    // target goes. The quest list is otherwise read every 5 min, so a refresh is asked at most once a minute while such a target
    // is shown; only a list observed after the target was set (not the disk cache) may remove it. Nothing is scanned per frame.
    private void ExpireGameTarget()
    {
        // 0.655 (user report): a leve's target has no quest; it goes when the game no longer lists the leve.
        if(mapGameTarget is not null && TenshoAssistant.Maps.MapLeves.TargetGone(mapGameTargetLeve,TenshoAssistant.Maps.MapLiveReader.AcceptedLeves)){mapGameTarget=null;mapGameTargetLeve=null;return;}
        // 0.813 (X-49): a journal target without a quest follows the quest point it stood on.
        // 0.847 (X-69, user report: the quest went on to « talk to Raulf », the journal's target stayed): a target with a quest too; the journal check below, once a
        // minute, saw only a quest ended, never a step done.
        if(mapGameTarget is { } bare && mapLive?.State?.QuestPoints is { } points)
        {
            var list=points.Select(point=>(point.Quest,point.MapId,point.X,point.Z)).ToArray();
            if(mapGameTargetObjective==0)mapGameTargetObjective=TenshoAssistant.Maps.MapQuestTarget.QuestAt(list,bare.MapId,bare.World);
            else if(TenshoAssistant.Maps.MapQuestTarget.Gone(list,mapGameTargetObjective,bare.MapId,bare.World)){mapGameTarget=null;mapGameTargetObjective=0;return;}
        }
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
    // 0.544 (P3): from any point or the flag, the question of 0.373: nearest aetheryte of this map, its real cost, teleport only if confirmed.
    private void DrawNearestAetheryteEntry(MapDefinition map)
    {
        if(!MapHasAetheryte(map))return;
        if(ImGui.Selectable(localization.Get("MapView.NearestAetheryte")+"##NearestAetheryte"))AskNearestAetheryteAt(map,mapContextPixel,map.Zone);
        Tip("MapView.NearestAetheryteTip");
    }
    private string? MapFailedParts()
    {
        var parts=mapLive?.FailedParts;
        if(parts is not { Count:>0 })return null;
        return localization.Format("MapView.PartsUnavailable",string.Join(", ",parts.Select(part=>localization.Get("MapView.Part"+part))));
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
        mapZoom=MapProjection.ClampZoom(Math.Max(mapZoom,MapFocusZoom));mapCenterRequest=pixel;
        if(!flag)return;
        var coordinate=MapProjection.CoordinateFromPixel(pixel,map);
        FlagFromMap(map,coordinate.X,coordinate.Y);
    }
    // Same, without a known position: the whole map.
    private void ShowMapOnTenshoMap(uint mapId){if(mapId==0)return;mapViewOpen=true;NavigateMap(mapId);}
    // 0.642 (user captures): the target stays when going up, as on the game's map: inside its zone's picture on the region, on the region's link on the world.
    private (Vector2 Pixel,uint Link)? GameTargetAbove(MapDefinition map,IReadOnlyList<MapIconMarker> markers,uint targetMap,Vector2 world)
    {
        if(map.TerritoryId!=0 || mapMarkers is null || maps.State.Catalog is not { } catalog || catalog.Find(targetMap) is not { } zone)return null;
        foreach(var link in markers)
            if(link.LinkMapId!=0 && (link.LinkMapId==targetMap || catalog.Find(link.LinkMapId)?.TerritoryId==zone.TerritoryId))
                return (MapLabels.ZonePixel(new Vector2(link.X,link.Y),MapProjection.PixelFromWorld(new Vector3(world.X,0,world.Y),zone)),targetMap);
        var region=mapMarkers.RegionOf(zone);
        foreach(var link in markers)
            if(region!=0 && link.LinkMapId==region)return (new Vector2(link.X,link.Y),region);
        return null;
    }
    // 0.850 (X-46): a map chosen from the menus keeps the zoom setting too, centred.
    private void NavigateMap(uint mapId){if(mapId==0)return;mapViewMapId=mapId;mapViewFollow=false;mapFitRequested=false;ApplyOpenZoom();CentreOtherMap(mapId);}

    private (uint Shown,uint Standing) mapCostsReadFor;
    private void DrawMapCanvas(MapDefinition map,MapLiveState? live)
    {
        MapMarkStart();
        // 0.645 (P7): the game's teleport list, read when the shown map or the player's map changes: costs depend on where the player stands.
        if(mapCostsReadFor!=(map.MapId,live?.MapId ?? 0)){mapCostsReadFor=(map.MapId,live?.MapId ?? 0);mapLive?.RefreshTeleportCosts();}
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
            mapZoom=MapProjection.ClampZoom(mapZoom*configuration.MapView.WheelFactor(io.MouseWheel));
            mapPan=io.MousePos-origin-before*mapZoom;
        }
        // 0.545 (P5): over the tool column, the slider or the names on the map, the wheel zooms around the centre of the view.
        else if(io.MouseWheel!=0 && ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))ZoomTo(mapZoom*configuration.MapView.WheelFactor(io.MouseWheel));
        if(mapFitDelay>0 && --mapFitDelay==0)mapFitRequested=true;
        if(mapFocusRequest is { } focus)
        {
            // Markers can arrive a moment after the map opens, so the search is retried for 1.5 s. Logged (/xllog) to check the quest links in game.
            // 0.637 (user request): from the quest journal or an active quest, the map is always adjusted and the zoom stays: centred on what was asked, or on the map until then.
            if(FocusPixel(focus,map,live) is { } pixel)
            {
                // 0.636 (user report): the icon stays on the way there, even once the game has forgotten its marker.
                mapGameTargetObjective=0;
                mapGameTarget=focus.UsesRoute(map) && focus.Route is { } way && focus.RouteIcon!=0 ? (map.MapId,way,focus.RouteIcon,focus.QuestId):focus.Target is { } world && focus.TargetIcon!=0 && map.TerritoryId!=0 && !focus.UsesRoute(map) && (maps.State.Catalog is not { } shownCatalog || focus.EntranceTarget(shownCatalog) is null) ? (map.MapId,world,focus.TargetIcon,focus.QuestId):null;
                mapGameTargetQuests=null;mapGameTargetSince=DateTimeOffset.UtcNow;mapGameTargetRefresh=default;
                mapGameTargetLeve=focus.QuestId==0 ? TenshoAssistant.Maps.MapLiveReader.LeveKeyOf(focus.Title):null;
                if(mapGameTargetLeve is { } clickedLeve && focus.Target is { } leveArea && focus.TargetScale>0 && map.TerritoryId!=0)
                    TenshoAssistant.Maps.MapLiveReader.SeedLeveArea(map.MapId,clickedLeve,focus.Title,leveArea,focus.TargetScale,focus.TargetIcon);
                // 0.846 (X-68, user correction: « do not zoom, centre on the quest, objective, resource or other »): centred, the zoom stays (0.637).
                mapFocusRequest=null;mapZoom=MapProjection.ClampZoom(mapZoom);mapCenterRequest=pixel;mapViewFollow=false;mapFitRequested=false;
                log.Information("Map focus found on map {0} for quest {1}, aetheryte {2}, FATE {3}.",map.MapId,focus.QuestId,focus.AetheryteId,focus.FateId);
            }
            else if(DateTimeOffset.UtcNow>=mapFocusUntil)
            {
                mapFocusRequest=null;mapCenterRequest=new Vector2(MapProjection.TextureSize/2f);mapFitRequested=false;
                var seen=string.Join(" | ",(live?.Events ?? []).Where(marker=>SameLandMaps(map).Contains(marker.MapId)).Take(12).Select(marker=>$"{marker.ObjectiveId}:{marker.Icon}:{marker.Tooltip}"));
                log.Information("Map focus not found on map {0} for quest {1} (journal {2}); markers: {3}",map.MapId,focus.QuestId,focus.QuestLog,seen);
            }
            else mapCenterRequest=new Vector2(MapProjection.TextureSize/2f);
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
            {
                draw.AddImage(texture.Handle,origin+mapPan,origin+mapPan+new Vector2(MapProjection.TextureSize*mapZoom),Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(1,1,1,opacity)));
                // P11 (0.803): the parts not discovered yet, blank like the game's map.
                if(MapDiscoveryImage(map,live) is { } hidden)
                    draw.AddImage(hidden.Handle,origin+mapPan,origin+mapPan+new Vector2(MapProjection.TextureSize*mapZoom),Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(1,1,1,opacity)));
            }
            else draw.AddText(origin+new Vector2(8),ImGui.GetColorU32(UI.FfxivTheme.TextMuted),localization.Get(textureFailure is not null ? "MapView.Unavailable":"MapView.Loading"));
            string? tooltip=null;MapLocalGatheringPoint? tooltipPoint=null;(MapLocalPosition Vista,bool? Done)? sightseeingTip=null;
            // Icons follow the map zoom exactly (32 px at 100 %, 0.113 at the user's request); 4 px floor only so they never vanish.
            // Each game icon can then be resized or hidden on its own (settings, « Icons on this map », MapView.SectionIcons).
            // 0.153: zoom^follow (setting « Zoom follow », MapView.ZoomFollow) so icons set for a close zoom stay readable when zoomed out.
            var zoomFactor=settings.ZoomFactor(mapZoom);
            var baseIcon=Math.Max(4,32*zoomFactor*MapViewSettings.ClampScale(settings.IconScale));
            var markers=settings.ShowFixedIcons || settings.ShowPlaceNames ? mapMarkers?.For(map.MarkerRange) ?? []:[];
            if(mapMarkers?.Failed(map.MarkerRange)==true)
                draw.AddText(origin+new Vector2(8,26),ImGui.GetColorU32(UI.FfxivTheme.TextMuted),localization.Get("MapView.MarkersUnavailable"));
            // 0.569 (P15): what no longer reads after a game update, instead of a map that just looks empty.
            if(MapFailedParts() is { } brokenParts)draw.AddText(origin+new Vector2(8,44),ImGui.GetColorU32(UI.FfxivTheme.Warning),brokenParts);
            // Place names first, under the icons, in large outlined text like the game map; they also follow the zoom.
            var font=ImGui.GetFont();var nameScale=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f);
            // 0.152 (user choice « follow the zoom »): names scale with the map like the icons; only a 6 px floor keeps them drawable.
            var nameSize=Math.Max(6,ImGui.GetFontSize()*2.4f*settings.NameFactor(mapZoom)*nameScale);
            var placeNameSize=MapLabels.PlaceNameSize(nameSize,mapZoom,nameScale);
            foreach(var marker in settings.ShowPlaceNames && settings.NamesShown(mapZoom) && MapLabels.PlaceNameShown(map.TerritoryId!=0,mapZoom) ? markers.Where(marker=>marker.Icon==0):[])
            {
                var centre=origin+mapPan+new Vector2(marker.X,marker.Y)*mapZoom;
                var text=ImGui.CalcTextSize(marker.Label)*(placeNameSize/ImGui.GetFontSize());var corner=centre-text/2;
                var outline=ImGui.GetColorU32(new Vector4(0.96f,0.93f,0.84f,0.9f));
                foreach(var shift in new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(0,-1),new Vector2(0,1)})
                    draw.AddText(font,placeNameSize,corner+shift,outline,marker.Label);
                draw.AddText(font,placeNameSize,corner,ImGui.GetColorU32(new Vector4(0.30f,0.22f,0.12f,1)),marker.Label);
            }
            // 0.631: the zones of a region map, under everything else, like the game.
            foreach(var marker in settings.ShowFixedIcons ? markers.Where(marker=>marker.Icon!=0 && marker.LinkMapId!=0 && settings.SizeOf(marker.Icon,map.MapId)>0):[])
                if(textures.TryGetFromGameIcon(new GameIconLookup(marker.Icon),out var zoneShared) && zoneShared.TryGetWrap(out var zoneImage,out _) && zoneImage is not null && MapLabels.IsZoneImage(marker.LinkMapId,zoneImage.Width))
                {
                    var zoneAt=origin+mapPan+new Vector2(marker.X,marker.Y)*mapZoom;var zoneHalf=new Vector2(zoneImage.Width,zoneImage.Height)*MapLabels.ZoneScale*mapZoom;
                    // The game darkens the parchment with this picture; ImGui only paints over it: a parchment tint comes close (by eye, on the captures).
                    draw.AddImage(zoneImage.Handle,zoneAt-zoneHalf,zoneAt+zoneHalf,Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(0.74f,0.70f,0.55f,opacity)));
                }
            uint? linkUnderMouse=null;
            // Areas first, under every icon: running FATEs, quest areas, gathering points of this map.
            var fates=settings.ShowFates && live?.Fates is { } running && live.MapId==map.MapId ? running:[];
            var eventMaps=SameLandMaps(map);
            var events=settings.ShowQuests && live?.Events is { } liveEvents ? liveEvents.Where(marker=>eventMaps.Contains(marker.MapId)).ToArray():[];
            MapMark("canvas texture, names, zones");
            // X-80 (0.895): only the gatherers ticked.
            var gathering=settings.ShowGathering ? MapGatheringPoints(map).Where(point=>settings.ShowsGatherer(point.Job)).ToArray():[];
            MapMark("canvas gathering points");
            // Areas per kind (0.151): quests (marker type 3), leves and other events, FATEs, gathering points; own switch and colour.
            if(settings.ShowFateAreas)
                foreach(var fate in fates.Where(fate=>fate.Radius>0))
                    DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(fate.Position,map)*mapZoom,fate.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,settings.FateAreaColor);
            // X-86 (0.967, user request): a quest or event area above or below the player gets the game's chevron across the whole area, faint, so the
            // icons inside stay visible (an area without an icon had no arrow at all).
            var areaPlayerHeight=settings.HeightArrows && live is { } areaLive && eventMaps.Contains(areaLive.MapId) ? areaLive.Position.Y:float.NaN;
            var areaChevron=float.IsNaN(areaPlayerHeight) || !textures.GetFromGame(MapHeight.ChevronTexture).TryGetWrap(out var areaChevronWrap,out _) ? null:areaChevronWrap;
            foreach(var marker in events.Where(marker=>marker.Radius>0))
            {
                var quest=marker.MarkerType==3;
                if(quest ? !settings.ShowQuestAreas:marker.IsLeve ? !settings.ShowLeveAreas:!settings.ShowEventAreas)continue;
                if(areaChevron is not null && MapHeight.Arrow(marker.Position.Y,areaPlayerHeight,settings.HeightArrowYalms) is var areaArrow and not 0)
                {
                    var areaCentre=origin+mapPan+MapProjection.PixelFromWorld(marker.Position,map)*mapZoom;
                    var areaSide=Math.Max(12,marker.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom*1.4f);
                    var (areaFrom,areaTo)=MapHeight.ChevronUv(areaArrow);
                    draw.AddImage(areaChevron.Handle,areaCentre-new Vector2(areaSide/2),areaCentre+new Vector2(areaSide/2),areaFrom,areaTo,ImGui.GetColorU32(new Vector4(1,1,1,0.3f)));
                }
                DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(marker.Position,map)*mapZoom,marker.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,quest ? settings.QuestAreaColor:marker.IsLeve ? settings.LeveAreaColor:settings.EventAreaColor);
            }
            if(settings.ShowGatheringAreas)
                foreach(var point in gathering)
                    if(point.Position is { Radius:>0 } area)
                        DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(new Vector3(area.X,0,area.Z),map)*mapZoom,area.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,settings.GatheringAreaColor);
            // N-07 (0.667): the Island Sanctuary's resources; world coordinates, to compare with the game's map.
            IReadOnlyList<IslandPoint> islandPoints=map.TerritoryId==IslandResources.Territory ? (IslandPoints ?? []).Where(point=>settings.ShowsIslandKind(point.Kind)).ToArray():[];
            if(settings.ShowGatheringAreas)
                foreach(var point in islandPoints.Where(point=>point.Radius>0))
                    DrawArea(draw,origin+mapPan+MapProjection.PixelFromWorld(new Vector3(point.X,0,point.Y),map)*mapZoom,point.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom,settings.GatheringAreaColor);
            // Every game icon in one list sorted by priority (0.124: quests used to be drawn above everything whatever the
            // setting). Equal priorities keep the game's layer order: fixed icons, gathering, FATEs, quests, game target.
            MapMark("canvas areas");
            var icons=new List<MapIconDraw>();
            // worldRadius: area of a quest, FATE or gathering point; hovering inside it lists the icon too (like Mappy).
            void Add(uint icon,int layer,Vector2 pixel,string? label,MapIconMarker? fixedMarker=null,MapLocalGatheringPoint? point=null,float worldRadius=0,Vector2? flag=null,float height=float.NaN)
            {
                var own=settings.SizeOf(icon,map.MapId);if(own<=0)return;
                icons.Add(new(settings.PriorityOf(icon,map.MapId),layer,icons.Count,icon,origin+mapPan+pixel*mapZoom,Math.Max(4,baseIcon*own),label,fixedMarker,point,
                    worldRadius*Math.Max(1,map.SizeFactor)/100f*mapZoom,flag,height));
            }
            // 0.844 (X-67): the player's height, when the player stands on this map's land; the arrows need it.
            var playerHeight=settings.HeightArrows && live is { } heightLive && eventMaps.Contains(heightLive.MapId) ? heightLive.Position.Y:float.NaN;
            // NPC vendors (0.140): a vendor standing by the game's icon for it (shop or service icon, MapVendorIcons, MAP-017) adds its
            // shops to that icon's tooltip instead of a second icon; others get the game icon of their kind (cached, VendorLayout).
            var vendorLayout=settings.ShowVendors ? VendorLayout(map,markers,settings.ShowFixedIcons):MapVendorLayout.None;
            // 0.805 (X-01: « canvas icons » 34 to 71 ms every 5 to 10 minutes): finer checkpoints inside it.
            MapMark("canvas vendors");
            foreach(var marker in settings.ShowFixedIcons ? markers.Where(marker=>marker.Icon!=0):[])
            {
                var label=marker.Label.Length>0 ? marker.Label:null;Vector2? vendorFlag=null;
                if(vendorLayout.Merged.TryGetValue(marker,out var vendors)){label=vendors.Label;vendorFlag=vendors.Flag;}
                Add(marker.Icon,0,new Vector2(marker.X,marker.Y),label,marker,flag:vendorFlag);
            }
            foreach(var point in islandPoints)
            {
                var islandItems=itemCatalog.State.Catalog;
                var firstItem=islandItems?.Find(point.Items[0]);
                if(firstItem is { IconId:not 0 })
                    Add(firstItem.IconId,1,MapProjection.PixelFromWorld(new Vector3(point.X,0,point.Y),map),string.Join("\n",point.Items.Select(item=>islandItems?.Find(item)?.Name ?? item.ToString())),worldRadius:settings.ShowGatheringAreas ? point.Radius:0);
            }
            // 0.632 (user captures): a region map also shows the aetherytes of its zones; a click asks for the teleport, as on the zone's map.
            if(settings.ShowFixedIcons && map.TerritoryId==0 && mapMarkers is not null && maps.State.Catalog is { } zoneCatalog)
                foreach(var zoneLink in markers.Where(marker=>marker.Icon!=0 && marker.LinkMapId!=0))
                    if(zoneCatalog.Find(zoneLink.LinkMapId) is { } linkedZone)
                        foreach(var crystal in mapMarkers.For(linkedZone.MarkerRange).Where(marker=>marker.AetheryteId!=0))
                            Add(crystal.Icon,0,MapLabels.ZonePixel(new Vector2(zoneLink.X,zoneLink.Y),new Vector2(crystal.X,crystal.Y)),crystal.Label.Length>0 ? crystal.Label:MapName(linkedZone),crystal);
            // 0.328 (map audit #7): hovering inside an area lists its icon only while that kind of area is drawn.
            foreach(var point in gathering)
                if(point.Position is { } spot && point.Icon!=0)Add(point.Icon,1,MapProjection.PixelFromWorld(new Vector3(spot.X,0,spot.Z),map),GatheringLabelOf(point),point:point,
                    worldRadius:settings.ShowGatheringAreas ? spot.Radius:0);
            // NPC vendors (0.139): the game's shop map icon (MapSymbol 60412, docs/evidence/0.139.0-symbols-probe.log), in the
            // same list, sizes and priorities as the game icons; click = flag.
            foreach(var vendor in vendorLayout.Lonely)Add(vendor.Icon,1,vendor.Pixel,vendor.Label,flag:vendor.Flag);
            foreach(var fate in fates.Where(fate=>fate.Icon!=0))Add(fate.Icon,2,MapProjection.PixelFromWorld(fate.Position,map),FateLabel(fate),worldRadius:settings.ShowFateAreas ? fate.Radius:0,height:fate.Position.Y);
            // X-86 (0.966): locked quest markers (the game's « not available yet » icons) left out when the player hides them.
            foreach(var marker in events)
                if(settings.ShowLockedQuests || mapMarkers?.QuestIcons.TryGetValue(marker.Icon,out var questIcon)!=true || !questIcon.Locked)
                Add(marker.Icon,3,MapProjection.PixelFromWorld(marker.Position,map),settings.QuestTooltips ? EventLabel(marker):null,
                worldRadius:(marker.MarkerType==3 ? settings.ShowQuestAreas:marker.IsLeve ? settings.ShowLeveAreas:settings.ShowEventAreas) ? marker.Radius:0,height:marker.Position.Y);
            ExpireGameTarget();
            if(mapGameTarget is { } gameTarget && gameTarget.MapId==map.MapId)
                Add(gameTarget.Icon,4,MapProjection.PixelFromWorld(new Vector3(gameTarget.World.X,0,gameTarget.World.Y),map),localization.Get("MapView.GameTarget"));
            // 0.643 (user capture): a click on it goes down one step, like the links.
            else if(mapGameTarget is { } upperTarget && GameTargetAbove(map,markers,upperTarget.MapId,upperTarget.World) is { } upper)
                Add(upperTarget.Icon,4,upper.Pixel,localization.Get("MapView.GameTarget"),new MapIconMarker(upper.Pixel.X,upper.Pixel.Y,upperTarget.Icon,"",upper.Link));
            MapMark("canvas icon list");
            icons.Sort((left,right)=>left.Priority!=right.Priority ? left.Priority.CompareTo(right.Priority):left.Layer!=right.Layer ? left.Layer.CompareTo(right.Layer):left.Order.CompareTo(right.Order));
            MapIconDraw? top=null;var hits=new List<MapIconDraw>();
            if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))mapIconContext=null;
            var iconNames=new List<(Vector2 At,Vector2 Size,bool MapLink,string Text,float FontSize,uint Link)>();
            foreach(var item in icons)
            {
                if(textures.TryGetFromGameIcon(new GameIconLookup(item.Icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null && !MapLabels.IsZoneImage(item.Fixed?.LinkMapId ?? 0,image.Width))
                    // 0.645 (P7): an aetheryte not attuned is greyed.
                    draw.AddImage(image.Handle,item.At-new Vector2(item.Size/2),item.At+new Vector2(item.Size/2),Vector2.Zero,Vector2.One,
                        MapLabels.NotAttuned(item.Fixed?.AetheryteId ?? 0,mapLive?.TeleportCosts) ? ImGui.GetColorU32(new Vector4(0.45f,0.45f,0.45f,0.8f)):ImGui.GetColorU32(Vector4.One));
                // 0.844 (X-67, user request): above or below the player, an arrow on the icon, as the game map does.
                // 0.849 (X-71, user request: the game's own arrows): the game's orange chevron (MapHeight.Chevron), up above, turned down below, over the icon's lower half.
                if(MapHeight.Arrow(item.Height,playerHeight,settings.HeightArrowYalms) is var arrow and not 0
                   && textures.GetFromGame(MapHeight.ChevronTexture).TryGetWrap(out var chevron,out _) && chevron is not null)
                {
                    // 0.851 (X-71, user capture: « a little smaller »): 55 % of the icon, under its middle.
                    var side=Math.Max(8,item.Size*0.55f);var centre=item.At+new Vector2(0,item.Size*0.35f);
                    var (from,to)=MapHeight.ChevronUv(arrow);
                    draw.AddImage(chevron.Handle,centre-new Vector2(side/2),centre+new Vector2(side/2),from,to);
                }
                // 0.596 (P12): the name the game writes beside this icon, on the side it uses; drawn after the icons (0.623).
                if(settings.ShowIconNames && settings.NamesShown(mapZoom) && item.Fixed is {Label.Length:>0} named && MapLabels.Shown(named.LinkMapId!=0,mapZoom))
                {
                    var iconNameSize=MapLabels.IconNameSize(nameSize);
                    var iconNameText=ImGui.CalcTextSize(named.Label)*(iconNameSize/ImGui.GetFontSize());
                    iconNames.Add((item.At+MapLabels.Offset(named.Orientation,item.Size,iconNameText),iconNameText,named.LinkMapId!=0,named.Label,iconNameSize,named.LinkMapId));
                }
                // Timed gathering points: a clock badge, green while open now, orange otherwise (0.130).
                if(item.Point is { Timed:true } timedPoint)
                {
                    var badge=Math.Max(6,item.Size*0.45f);var corner=item.At+new Vector2(item.Size*0.2f,-item.Size*0.6f);
                    draw.AddCircleFilled(corner+new Vector2(badge/2),badge*0.62f,ImGui.GetColorU32(new Vector4(0,0,0,0.75f)),16);
                    draw.AddText(UiBuilder.IconFont,badge,corner,ImGui.GetColorU32(TimedPointColor(timedPoint)),FontAwesomeIcon.Clock.ToIconString());
                }
                if(item.Point is { } labelled && settings.GatheringLabels && settings.GatheringLabelShown(mapZoom))
                {
                    var labelSize=settings.GatheringLabelSize(ImGui.GetFontSize());
                    draw.AddText(ImGui.GetFont(),labelSize,item.At+new Vector2(item.Size*0.6f,-labelSize/2),ImGui.GetColorU32(UI.FfxivTheme.Text),localization.Format("MapLocal.Level",labelled.Level));
                }
                if(hovered)
                {
                    var distance=Vector2.Distance(io.MousePos,item.At);
                    if(distance<=item.Size/2)top=item;
                    if(distance<=Math.Max(item.Size/2,item.Area))hits.Add(item);
                }
            }
            // 0.623: as on the game's map: white with a dark edge, map links in blue-green, and none on top of another.
            foreach(var index in MapLabels.Fitting(iconNames.Select(name=>(name.At,name.Size,name.MapLink)).ToArray()))
            {
                var label=iconNames[index];
                // 0.626 (user report): the name of a map link opens that map, like its icon (handled with it, below).
                var overLink=hovered && label.Link!=0 && io.MousePos.X>=label.At.X && io.MousePos.X<=label.At.X+label.Size.X && io.MousePos.Y>=label.At.Y && io.MousePos.Y<=label.At.Y+label.Size.Y;
                if(overLink)linkUnderMouse=label.Link;
                foreach(var shift in new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(0,-1),new Vector2(0,1)})
                    draw.AddText(font,label.FontSize,label.At+shift,ImGui.GetColorU32(new Vector4(0.05f,0.05f,0.05f,0.9f)),label.Text);
                draw.AddText(font,label.FontSize,label.At,ImGui.GetColorU32(overLink ? new Vector4(0.8f,1,1,1):label.MapLink ? new Vector4(0.45f,0.9f,0.95f,1):new Vector4(0.97f,0.95f,0.9f,1)),label.Text);
            }
            // X-114 (0.1049, user request): the current weather of each zone, only while the mouse is over the map; built once per map and period.
            if(hovered)
            {
                var weatherCatalog=weather.State.Catalog;var mapCatalog=maps.State.Catalog;var weatherTime=DateTimeOffset.UtcNow;
                var weatherMarks=mapWeather.For(map.MapId,weatherTime.ToUnixTimeSeconds(),weatherCatalog is not null && mapCatalog is not null,()=>MapWeatherIcons.Build(map.TerritoryId,
                    markers.Where(marker=>marker.Icon!=0 && marker.LinkMapId!=0).Select(marker=>(new Vector2(marker.X,marker.Y),marker.LinkMapId)).ToArray(),
                    linked=>mapCatalog?.Find(linked)?.TerritoryId ?? 0,
                    zone=>weatherCatalog?.Forecast(zone,weatherTime,1) is [ { Weather:{ } current } ] ? current:null));
                var weatherSize=Math.Max(16,baseIcon*0.9f);
                foreach(var mark in weatherMarks)
                {
                    if(!textures.TryGetFromGameIcon(new GameIconLookup(mark.Icon),out var weatherShared) || !weatherShared.TryGetWrap(out var weatherImage,out _) || weatherImage is null)continue;
                    var weatherCentre=mark.At is { } zonePixel ? origin+mapPan+zonePixel*mapZoom-new Vector2(0,weatherSize):origin+size-new Vector2(weatherSize/2+12,weatherSize/2+44);
                    var weatherHalf=new Vector2(weatherSize/2);
                    draw.AddCircleFilled(weatherCentre,weatherSize*0.62f,ImGui.GetColorU32(new Vector4(0,0,0,0.55f)),20);
                    draw.AddImage(weatherImage.Handle,weatherCentre-weatherHalf,weatherCentre+weatherHalf);
                    if(Vector2.Distance(io.MousePos,weatherCentre)<=weatherSize/2){tooltip=localization.Format("MapView.VistaWeatherNow",mark.Name);tooltipPoint=null;}
                }
            }
            // Every icon under the cursor (or whose area holds it) is listed in one tooltip, topmost first (0.132, as Mappy does).
            hits.Reverse();
            // Nearby coffers with the game's treasure coffer map icon (0.210), and gathering points the game streams for your current job
            // (icon-font symbol); both sized and hidden from the icon settings like the game's icons.
            MapMark("canvas icons drawn");
            if(live?.Objects is { } nearby && live.MapId==map.MapId)
                foreach(var item in nearby)
                {
                    if(item.Kind switch{MapObjectKind.Treasure=>!settings.ShowTreasure,MapObjectKind.HuntMark=>!settings.ShowHuntMarks,MapObjectKind.IslandAnimal=>!settings.ShowIslandAnimals,_=>!settings.ShowGathering})continue;
                    var kindSize=settings.SizeOf(item.Kind switch{MapObjectKind.Treasure=>TreasureIcon,MapObjectKind.IslandAnimal=>GlyphIslandAnimal,_=>GlyphNearbyGathering},map.MapId);
                    if(kindSize<=0)continue;
                    var at=origin+mapPan+MapProjection.PixelFromWorld(item.Position,map)*mapZoom;
                    var glyph=Math.Max(4,baseIcon*0.6f*kindSize);
                    if(item.Kind==MapObjectKind.Treasure && textures.TryGetFromGameIcon(new GameIconLookup(TreasureIcon),out var chestShared) && chestShared.TryGetWrap(out var chest,out _) && chest is not null)
                    {
                        var half=Math.Max(4,baseIcon*kindSize)/2;
                        draw.AddImage(chest.Handle,at-new Vector2(half),at+new Vector2(half));
                        glyph=half*2;
                    }
                    // 0.901 (X-83): an island animal with its own icon (MJIAnimals.Icon).
                    else if(item.Kind==MapObjectKind.IslandAnimal && item.Icon!=0 && textures.TryGetFromGameIcon(new GameIconLookup(item.Icon),out var animalShared) && animalShared.TryGetWrap(out var animalImage,out _) && animalImage is not null)
                    {
                        var half=Math.Max(4,baseIcon*kindSize)/2;
                        draw.AddCircleFilled(at,half*1.05f,ImGui.GetColorU32(new Vector4(0,0,0,0.55f)),20);
                        draw.AddImage(animalImage.Handle,at-new Vector2(half),at+new Vector2(half));
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
                        :item.Kind==MapObjectKind.HuntMark ? localization.Format("MapView.HuntMarkLabel",item.Name,item.Level switch{3=>"S",2=>"A",_=>"B"})
                        :item.Kind==MapObjectKind.IslandAnimal ? IslandAnimalLabel(item):item.Name;
                    if(hovered && Vector2.Distance(io.MousePos,at)<=glyph){tooltip=label;tooltipPoint=null;}
                }
            MapMark("canvas nearby objects");
            // 0.795 (X-35, user request): nearby monsters that drop an item the tracking lists still need.
            // 0.796 (X-36): one layer for every reason; one icon per monster, the first reason's, and every reason on hover.
            if((settings.ShowListMonsters || settings.ShowHuntLogMonsters || settings.ShowFateMonsters || settings.ShowBlueMageMonsters || settings.ShowCollectionMonsters || settings.ShowBeastMonsters || settings.ShowRelicMonsters || settings.ShowWishMonsters) && live?.Monsters is { Count:>0 } monsters && live.MapId==map.MapId)
            {
                RefreshNearbyMonsters();
                foreach(var monster in monsters)
                {
                    var notes=MonsterNotes(monster,live.Fates);
                    if(notes.Count==0)continue;
                    MonsterNote? first=null;
                    foreach(var note in notes)if(MonsterNoteShown(note,settings)){first=note;break;}
                    if(first is null)continue;
                    var at=origin+mapPan+MapProjection.PixelFromWorld(monster.Position,map)*mapZoom;
                    var glyph=Math.Max(4,baseIcon*0.6f);
                    var (symbol,colour)=MonsterGlyph(first.Kind);
                    draw.AddCircleFilled(at,glyph*0.7f,ImGui.GetColorU32(new Vector4(0,0,0,0.55f)),20);
                    draw.AddText(UiBuilder.IconFont,glyph,at-new Vector2(glyph/2),ImGui.GetColorU32(colour),symbol.ToIconString());
                    if(hovered && Vector2.Distance(io.MousePos,at)<=glyph)
                    {
                        tooltip=localization.Format("MapView.MonsterLabel",monster.Name,monster.Level)+"\n"+string.Join("\n",notes.Where(note=>MonsterNoteShown(note,settings)).Select(note=>note.Text));
                        tooltipPoint=null;
                    }
                }
            }
            // X-87 (0.968, user request): while a leve is under way, the monsters whose event is that leve (EventId low part = the leve's objective id) get
            // the leve marker's own game icon. Developer mode logs the nearby events once per leve, to prove the link.
            if(settings.ShowQuests && live is { ActiveLeve:{ } leve, Monsters:{ Count:>0 } leveMonsters } && live.MapId==map.MapId
               && textures.TryGetFromGameIcon(new GameIconLookup(leve.Icon),out var leveShared) && leveShared.TryGetWrap(out var leveIcon,out _) && leveIcon is not null)
            {
                if(developerMode && leveLogged!=leve.Objective)
                {
                    leveLogged=leve.Objective;
                    log.Information("Leve {0} (icon {1}): nearby monster events {2}.",leve.Objective,leve.Icon,string.Join(", ",leveMonsters.Where(entry=>entry.EventId!=0).Select(entry=>$"{entry.NameId}:{entry.EventId:X8}").Distinct().Take(20)));
                }
                var leveSize=Math.Max(4,baseIcon*0.7f);
                foreach(var monster in leveMonsters)
                {
                    if(monster.EventId==0 || (monster.EventId & 0xFFFF)!=leve.Objective)continue;
                    var at=origin+mapPan+MapProjection.PixelFromWorld(monster.Position,map)*mapZoom;
                    draw.AddImage(leveIcon.Handle,at-new Vector2(leveSize/2),at+new Vector2(leveSize/2));
                    if(hovered && Vector2.Distance(io.MousePos,at)<=leveSize/2){tooltip=localization.Format("MapView.LeveTarget",monster.Name,monster.Level);tooltipPoint=null;}
                }
            }
            MapMark("canvas monsters");
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
            MapMark("canvas icons");
            if(settings.ShowBookmarks && settings.SizeOf(GlyphBookmark,map.MapId) is var bookmarkSize and >0)
                foreach(var mark in configuration.MapBookmarks ?? [])
                {
                    if(mark is null || mark.MapId!=map.MapId)continue;
                    var at=origin+mapPan+MapProjection.PixelFromCoordinate(new Vector2(mark.X,mark.Y),map)*mapZoom;
                    if(!DrawGlyph(draw,at,FontAwesomeIcon.Star,new Vector4(1f,0.85f,0.35f,1),baseIcon*bookmarkSize,hovered,io.MousePos))continue;
                    hoveredBookmark=mark;sightseeingTip=null;tooltipPoint=null;
                    tooltip=(mark.Group is { } group ? $"[{group}] ":"")+mark.Name+$" ({MapProjection.CoordinateNumber(mark.X)}, {MapProjection.CoordinateNumber(mark.Y)})"+(mark.Note is { } note ? "\n"+note:"")+"\n"+localization.Get("MapView.BookmarkClick");
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
            // X-110 (0.985, user request): where to tame the beasts not tamed yet, seen in game (BeastPlacesOf), on this map, with the beast layer
            // (as Beastmaster, or always when chosen); the beast's icon, a click places the flag.
            if(settings.ShowBeastMonsters && (settings.BeastMonstersAlways || characterStats.State.JobId==BeastmasterJob)
               && Beasts?.State is { Catalog:{ } tameCatalog, Obtained:{ } tamedAlready })
            {
                var tameSize=Math.Max(6,baseIcon*0.6f);
                foreach(var wild in tameCatalog.Beasts)
                {
                    if(tamedAlready.Contains(wild.Id))continue;
                    foreach(var place in BeastPlacesOf(wild.Id))
                    {
                        if(place.MapId!=map.MapId)continue;
                        var pixel=MapProjection.PixelFromCoordinate(new Vector2(place.X,place.Y),map);var at=origin+mapPan+pixel*mapZoom;
                        if(!textures.TryGetFromGameIcon(new GameIconLookup(wild.Icon),out var wildShared) || !wildShared.TryGetWrap(out var wildIcon,out _) || wildIcon is null)continue;
                        draw.AddImage(wildIcon.Handle,at-new Vector2(tameSize/2),at+new Vector2(tameSize/2),Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(1,1,1,0.85f)));
                        if(!hovered || Vector2.Distance(io.MousePos,at)>tameSize/2)continue;
                        sightseeingTip=null;tooltipPoint=null;
                        tooltip=localization.Format("MapView.BeastToTame",wild.Name)+"\n"+localization.Get("MapView.BookmarkClick");
                        if(top is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)FlagFromMap(map,place.X,place.Y);
                    }
                }
            }
            if(settings.ShowPlayer && player is { } here)DrawPlayerArrow(draw,origin+mapPan+here*mapZoom,live!.Rotation,settings.ShowViewCone,MapViewSettings.ClampMarker(settings.PlayerIconScale),
                MapViewSettings.ClampMarker(settings.ViewConeScale),MapViewSettings.ClampConeOpacity(settings.ViewConeOpacity),settings.ViewConeColor);
            // 0.297 (user request): targets outside the view keep their icon on the map's edge, compass style.
            MapMark("canvas bookmarks, treasure, player");
            // 0.814 (X-50, user report: edge icons hidden under the zone names' band): they are drawn in a layer of their own, a child window created after the
            // band and the tools (children too), so above them; it takes no input, the canvas keeps every click.
            string? edgeLabel=null;
            if(settings.ShowEdgeMarkers)
            {
                var cursor=ImGui.GetCursorScreenPos();
                ImGui.SetCursorScreenPos(origin);
                if(ImGui.BeginChild("MapEdgeLayer",size,false,ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoBackground|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoScrollWithMouse|ImGuiWindowFlags.NoSavedSettings))
                    edgeLabel=DrawEdgeMarkers(ImGui.GetWindowDrawList(),origin,size,map,live,settings,player,events,fates,gathering,hovered,io.MousePos);
                ImGui.EndChild();
                ImGui.SetCursorScreenPos(cursor);
            }
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
                        if(released){mapTeleportNote=null;mapTeleportTarget=(marker.AetheryteId,marker.Label);mapLive?.RefreshTeleportCosts();ImGui.OpenPopup("MapTeleport");}
                    }
                }
                if(chosen.Flag is { } vendorSpot && released)FlagFromMap(map,vendorSpot.X,vendorSpot.Y);
                if(chosen.Point is { Position:{ } spot })
                {
                    if(released){var coordinate=MapProjection.CoordinateFromWorld(new Vector3(spot.X,0,spot.Z),map);FlagFromMap(map,coordinate.X,coordinate.Y);}
                }
                // R-04 (0.245): right click on an icon opens that icon's menu, unless the icon is in the « general menu » list.
                if(ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !settings.RightClickIgnored(chosen.Icon) && sightseeingTip is null)
                    mapIconContext=(chosen,(chosen.At-origin-mapPan)/mapZoom);
            }
            // 0.644 (P6): where the player stands, always; above the cursor's line.
            if(settings.ShowPlayerCoordinates && player is { } standing)
            {
                var own=MapProjection.CoordinateFromPixel(standing,map);
                var ownLabel=localization.Format("MapView.PlayerCoordinates",MapProjection.CoordinateNumber(own.X),MapProjection.CoordinateNumber(own.Y));
                var ownBox=ImGui.CalcTextSize(ownLabel)+new Vector2(10,6);
                var ownTop=size.Y-ownBox.Y*2-10;
                draw.AddRectFilled(origin+new Vector2(6,ownTop),origin+new Vector2(6+ownBox.X,ownTop+ownBox.Y),ImGui.GetColorU32(UI.FfxivTheme.Panel),3);
                draw.AddText(origin+new Vector2(11,ownTop+3),ImGui.GetColorU32(UI.FfxivTheme.Text),ownLabel);
            }
            if(hovered)
            {
                var coordinate=MapProjection.CoordinateFromPixel((io.MousePos-origin-mapPan)/mapZoom,map);
                var label=localization.Format("MapView.Cursor",MapProjection.CoordinateNumber(coordinate.X),MapProjection.CoordinateNumber(coordinate.Y));
                if(player is { } measured)label=localization.Format("MapView.CursorDistance",label,MapProjection.Yalms(measured,(io.MousePos-origin-mapPan)/mapZoom,map).ToString("0"));
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
            // 0.640 (user capture): a quest icon on top of a door hid its link.
            if(linkUnderMouse is null && edgeLabel is null && top is not { Flag:not null } && top is not { Point:not null } && top is not { Fixed:{ AetheryteId:not 0 } })
                foreach(var hit in hits)
                    if(hit.Fixed is { LinkMapId:not 0 } door)linkUnderMouse=door.LinkMapId;
            // Region and world maps: a click (not a drag) on a linked icon opens that map, like the game.
            if(linkUnderMouse is { } link && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)NavigateMap(link);
            if(settings.FlagOnClick && hovered && top is null && linkUnderMouse is null && hoveredBookmark is null && tooltipPoint is null && sightseeingTip is null && map.TerritoryId!=0 && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)
            {var spotClicked=MapProjection.CoordinateFromPixel((io.MousePos-origin-mapPan)/mapZoom,map);FlagFromMap(map,spotClicked.X,spotClicked.Y);}
        }
        finally{MapMark("canvas edges and tooltips");draw.PopClipRect();}
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
                    if(mapTeleportNote is { } note && note.Id==target.Id)
                    {
                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX()+360);
                        try{if(note.Warn)ImGui.TextColored(UI.FfxivTheme.Error,note.Text);else ImGui.TextUnformatted(note.Text);}
                        finally{ImGui.PopTextWrapPos();}
                    }
                    // Price from the game's teleport list; unknown (not attuned or not read yet) is said, never shown as 0.
                    if(mapLive?.TeleportCostsFailed==true)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MapView.TeleportCostFailed"));
                    else if(mapLive?.TeleportCosts is not { } costs)ImGui.TextDisabled(localization.Get("MapView.TeleportCostReading"));
                    else if(costs.TryGetValue(target.Id,out var cost)){ImGui.TextUnformatted(localization.Format("MapView.TeleportCost",cost.ToString("N0")));DrawGilIcon();}
                    else ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MapView.TeleportCostUnknown"));
                    if(ImGui.Button(localization.Get("MapView.TeleportYes"))){mapLive?.Teleport(target.Id);mapTeleportTarget=null;mapTeleportNote=null;ImGui.CloseCurrentPopup();}
                    ImGui.SameLine();
                    if(ImGui.Button(localization.Get("MapView.TeleportNo"))){mapTeleportTarget=null;mapTeleportNote=null;ImGui.CloseCurrentPopup();}
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
                ImGui.TextDisabled(localization.Format("MapView.Cursor",MapProjection.CoordinateNumber(coordinate.X),MapProjection.CoordinateNumber(coordinate.Y)));
                // X-128 (0.1107, user capture): the box of the quests not available yet moved from here to the Quests tab below.
                ImGui.Separator();
                ImGui.BeginDisabled(map.TerritoryId==0);
                try{if(ImGui.Selectable(localization.Get("MapView.Flag")))FlagFromMap(map,coordinate.X,coordinate.Y);}
                finally{ImGui.EndDisabled();}
                ImGui.BeginDisabled(live?.Flag is null);
                try{if(ImGui.Selectable(localization.Get("MapView.RemoveFlag")))mapLive?.RemoveFlag();}
                finally{ImGui.EndDisabled();}
                DrawNearestAetheryteEntry(map);
                // 0.242 (R-01, first part): the journal target icon could not be removed; right click › remove it.
                if(mapGameTarget is not null && ImGui.Selectable(localization.Get("MapView.RemoveGameTarget")))mapGameTarget=null;
                ImGui.BeginDisabled(map.TerritoryId==0);
                try{if(ImGui.Selectable(localization.Get("MapView.UseInTool"))){selectedMapId=map.MapId;mapsTabRequest=0;mapX=coordinate.X;mapY=coordinate.Y;bookmarkSaved=false;page=Page.Maps;IsOpen=true;}}
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
                        var name=string.IsNullOrWhiteSpace(mapContextBookmarkName) ? MapBookmarks.DefaultName(map.Zone,coordinate.X,coordinate.Y):mapContextBookmarkName;
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
        MapMark("canvas menus");
        ResolveNearestAetheryte(map);
        MapMark("canvas nearest aetheryte");
        // Aetheryte chosen in the local lists: the teleport question opens once the context menu is closed.
        if(mapTeleportRequested){mapTeleportRequested=false;ImGui.OpenPopup("MapTeleport");}
        if(!ReferenceEquals(mapOpenError,mapErrorSeen)){mapErrorSeen=mapOpenError;if(mapOpenError is not null)mapErrorUntil=DateTimeOffset.UtcNow.AddSeconds(5);}
        if(hovered && mapOpenError is { } error && DateTimeOffset.UtcNow<mapErrorUntil)WrappedTooltip(error);
        MapMark("canvas end");
    }

    // One game icon to draw on the canvas, sorted by priority then layer then insertion.
    private readonly record struct MapIconDraw(int Priority,int Layer,int Order,uint Icon,Vector2 At,float Size,string? Label,MapIconMarker? Fixed,MapLocalGatheringPoint? Point,float Area=0,Vector2? Flag=null,
        float Height=float.NaN);
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
            DrawNearestAetheryteEntry(map);
            if(item.Fixed is { AetheryteId:not 0 } aetheryte && ImGui.Selectable(localization.Get("MapView.IconTeleport")))
                {mapTeleportNote=null;mapTeleportTarget=(aetheryte.AetheryteId,aetheryte.Label);mapLive?.RefreshTeleportCosts();mapTeleportRequested=true;}
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
    // Game shop icons (MapSymbol, docs/evidence/0.139.0-symbols-probe.log): 60412 shop, 60935 exchange, 63851 merchant.
    private static MapIconMarker[] VendorCandidates(IReadOnlyList<MapIconMarker> markers)=>
        markers.Where(marker=>MapVendorIcons.Shops.Contains(marker.Icon) || MapVendorIcons.Services.Contains(marker.Icon)).ToArray();
    private static MapIconMarker? VendorMarker(MapIconMarker[] candidates,Vector2 pixel,uint preferred=0)
    {
        var index=MapVendorIcons.Nearest(candidates.Select(marker=>(marker.X,marker.Y,marker.Icon,marker.Label.Length>0)).ToArray(),pixel,preferred);
        return index<0 ? null:candidates[index];
    }
    // Gil shop = the shop icon; only special exchanges = the exchange icon.
    private uint VendorIconOf(Npcs.NpcVendor vendor)
    {
        var kinds=vendor.ShopIds.Select(id=>mapVendorCache.Shops.TryGetValue(id,out var shop) ? shop.Kind:(Npcs.NpcShopKind?)null).OfType<Npcs.NpcShopKind>().ToArray();
        return kinds.Length>0 && kinds.All(kind=>kind==Npcs.NpcShopKind.Special) ? ExchangeIcon:VendorIcon;
    }
    // One tooltip for every icon under the cursor: game icon, first line as title, the rest muted; gathering points list their
    // items with icons; the clickable (topmost) icon says what a click does. Unnamed icons use the game's symbol name.
    private void DrawIconListTooltip(IReadOnlyList<MapIconDraw> hits,MapIconDraw? top,MapViewSettings settings)
    {
        using(TooltipScope.Begin())
        {
            var size=new Vector2(ImGui.GetTextLineHeight()*1.6f);var shown=0;
            foreach(var hit in hits)
            {
                if(shown++>=12){ImGui.TextDisabled(localization.Format("MapView.HoverMore",hits.Count-12));break;}
                if(shown>1)ImGui.Separator();
                DrawGameIcon(hit.Icon,size);
                ImGui.SameLine();
                ImGui.BeginGroup();
                try
                {
                    var lines=(hit.Label ?? (mapMarkers?.SymbolName(hit.Icon) is { Length:>0 } symbol ? symbol:localization.Format("MapView.IconUnnamed",hit.Icon))).Split('\n');
                    ImGui.TextColored(Accent,lines[0]);
                    foreach(var line in lines.Skip(hit.Point is null ? 1:2))ImGui.TextDisabled(line);
                    // 0.645 (P7): the cost on hover, or that it is not attuned.
                    if(hit.Fixed is { AetheryteId:not 0 } crystal && mapLive?.TeleportCostsFailed!=true && mapLive?.TeleportCosts is { Count:>0 } teleportCosts)
                    {
                        if(teleportCosts.TryGetValue(crystal.AetheryteId,out var gilCost))ImGui.TextDisabled(localization.Format("MapView.TeleportCost",gilCost.ToString("N0")));
                        else ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("MapView.AetheryteNotAttuned"));
                    }
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
    }
    // Event marker title: the game's tooltip when it could be read safely, else the name of a quest in progress with the same
    // objective id (0.134: a leve marker took a completed quest's name through a 16-bit id collision), else null → the
    // game's symbol name in the list.
    internal Func<IReadOnlyList<IslandPlace>>? IslandPlacesLoad { get; set; }
    private Task<IReadOnlyList<IslandPlace>>? islandPlaces;
    private string? IslandMarkerName(MapEventMarker marker)
    {
        if(IslandPlacesLoad is not { } load || maps.State.Catalog?.Find(marker.MapId)?.TerritoryId!=IslandResources.Territory)return null;
        islandPlaces??=Task.Run(load);
        return islandPlaces.IsCompletedSuccessfully ? IslandResources.NearestName(islandPlaces.Result,marker.Position.X,marker.Position.Z):null;
    }
    private string? EventLabel(MapEventMarker marker)
    {
        if(marker.Tooltip.Length>0)return marker.Tooltip;
        // 0.684 (user report): the island's markers come with no text, a construction board among them; the sheet says what stands there.
        if(marker.MarkerType!=3 && IslandMarkerName(marker) is { } islandName)return islandName;
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
    internal Func<uint,TenshoAssistant.Gathering.GatherPointKind>? GatherKind { get; set; }
    // Light text on the map's own dark backdrops (overlay lists, slider labels): fixed, as the backdrop does not follow the theme.
    private static readonly Vector4 MapOverlayText=new(0.93f,0.93f,0.93f,1);
    /// <summary>
    /// X-94 (0.960, user report: a fishing hole shown as a miner's legendary point asking for a tome): a fishing spot's id is a FishingSpot row (type 100),
    /// not a GatheringPointBase row; its number then named another point. Kind and tome are read for gathering points only (miner, botanist, spearfishing).
    /// </summary>
    private static bool IsGatheringPointBase(MapLocalGatheringPoint point)=>point.TypeId!=100;
    private TenshoAssistant.Gathering.GatherPointAccess? PointAccess(MapLocalGatheringPoint point)=>IsGatheringPointBase(point) ? GatherAccess?.Invoke(point.BaseId):null;
    private Vector4 TimedPointColor(MapLocalGatheringPoint point)=>PointAccess(point)?.TomeMissing is not null ? Muted:point.ActiveAt(EorzeaMinutes()) ? UI.FfxivTheme.Success:UI.FfxivTheme.Warning;
    private void DrawPointAvailability(MapLocalGatheringPoint point)
    {
        var access=PointAccess(point);
        // 0.647 (C27): the kind of the point.
        if(IsGatheringPointBase(point) && GatherKind?.Invoke(point.BaseId) is { } kind and not TenshoAssistant.Gathering.GatherPointKind.Normal)ImGui.TextDisabled(localization.Get("MapView.PointKind"+kind));
        if(point.Timed)
        {
            var open=point.ActiveAt(EorzeaMinutes());
            ImGui.TextColored(TimedPointColor(point),localization.Format(!open ? "MapView.TimedClosed":access?.TomeMissing is not null ? "MapView.TimedOpenNoTome":"MapView.TimedOpen",point.Schedule!.Label));
            // 0.754 (X-08, user report: « closed now » without a timer): how long until it opens, or closes, as on the Gathering page.
            var now=DateTimeOffset.UtcNow;
            if(TenshoAssistant.Gathering.GatheringTime.Next(point.Schedule,now,1).Openings.FirstOrDefault() is { } next)ImGui.TextDisabled(WindowCountdown(next.At(now)));
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
    // Treasure coffer map icon of the game (MapSymbol 63852, docs/evidence/0.139.0-symbols-probe.log), used for nearby coffers (0.210).
    private const uint TreasureIcon=63852;
    // Keys of Tensho's own map layers in the icon size/priority tables (outside the game's icon id range).
    private const uint GlyphVista=0xFFFF0001,GlyphCurrent=0xFFFF0002,GlyphNearbyGathering=0xFFFF0003,GlyphParty=0xFFFF0004,GlyphBookmark=0xFFFF0005,GlyphTreasureMap=0xFFFF0006,
        GlyphIslandAnimal=0xFFFF0007;
    private static (FontAwesomeIcon Symbol,Vector4 Colour)? TenshoGlyph(uint icon)=>icon switch
    {
        GlyphVista=>(FontAwesomeIcon.Binoculars,new Vector4(0.98f,0.82f,0.30f,1)),
        GlyphCurrent=>(FontAwesomeIcon.Wind,new Vector4(0.45f,0.85f,0.95f,1)),
        GlyphNearbyGathering=>(FontAwesomeIcon.Leaf,new Vector4(0.55f,0.90f,0.55f,1)),
        GlyphParty=>(FontAwesomeIcon.Users,new Vector4(0.35f,0.6f,1f,1)),
        GlyphBookmark=>(FontAwesomeIcon.Star,new Vector4(1f,0.85f,0.35f,1)),
        GlyphTreasureMap=>(FontAwesomeIcon.Gem,new Vector4(0.95f,0.55f,0.25f,1)),
        GlyphIslandAnimal=>(FontAwesomeIcon.Paw,new Vector4(0.85f,0.75f,0.55f,1)),
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
        GlyphIslandAnimal=>localization.Get("MapView.ShowIslandAnimals"),
        _ when mapMarkers?.SymbolName(icon) is { Length:>0 } known=>known,
        _ when mapMarkers?.QuestIcons.TryGetValue(icon,out var quest)==true=>localization.Format(MapQuestIcons.NamesItsVariant(quest.Locked,quest.Variant) ? "MapView.QuestIconNamed":"MapView.QuestIcon",
            localization.Get("MapView.QuestKind"+(quest.Row is 1 or 3 or 8 or 10 ? quest.Row:0u)),quest.Row,
            localization.Get(MapQuestIcons.IsStep(quest.Locked,quest.Variant) ? "MapView.QuestStep":quest.Locked ? "MapView.QuestLocked":"MapView.QuestAvailable"),quest.Variant),
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
    private readonly MapWeatherIcons mapWeather=new();
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
                ? MapProjection.CoordinateFromWorld(new Vector3(row.X,0,row.Z),map) is var c ? (row.Id,(int)(row.Id-2162688u),c.X,c.Y):default
                :default).Where(entry=>entry.Id!=0);
            wikiVistaMatch=(document,index,WikiVistaParser.Match(document.Rows,games));
        }
        return wikiVistaMatch.Rows.GetValueOrDefault(vista.Id);
    }
    private string ExpansionName(int section)=>localization.Get("MapView.Expansion"+Math.Clamp(section,0,5));

    // Eorzea minute of the day now (one Eorzea day = 70 real minutes), from the one shared formula.
    private static int EorzeaMinutes()=>Gathering.GatheringTime.MinuteOfDay(DateTimeOffset.UtcNow);
    // Sightseeing log entry: number (row order), name, state, and the game's picture once discovered.
    private void DrawSightseeingTooltip(MapLocalPosition vista,bool? done)
    {
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(Accent,localization.Format("MapView.Vista",vista.Name));
            ImGui.TextDisabled(localization.Get(done switch{true=>"MapView.VistaDone",false=>"MapView.VistaTodo",_=>"MapView.CollectedUnknown"}));
            // X-158 (0.1128): every « now » of this tooltip comes from one calculation (VistaWindow): the time line, the weather line and
            // the state line agree with the list's « possible now » and with the next window.
            var territory=maps.State.Catalog?.Find(vista.MapId)?.TerritoryId ?? 0;
            var stateNow=VistaStateAt(vista,territory,DateTimeOffset.UtcNow,false);
            // Conditions from the game data: Eorzea time window and emote; the weather is not in the data.
            if(vista.MinTime!=0 || vista.MaxTime!=0)
                ImGui.TextUnformatted(localization.Format("MapView.VistaTime",$"{vista.MinTime/100:00}:{vista.MinTime%100:00}",$"{vista.MaxTime/100:00}:{vista.MaxTime%100:00}")
                    +(stateNow.TimeOk ? " "+localization.Get("MapView.VistaNow"):""));
            if(vista.Extra.Length>0)ImGui.TextUnformatted(localization.Format("MapView.VistaEmote",vista.Extra));
            var (stateText,stateColour)=VistaStateLine(stateNow,stateNow.State==VistaOpen.Open ? stateNow:NextVistaWindow(vista,territory));
            ImGui.TextColored(stateColour,stateText);
            // Weather now in the vista's zone, compared with the weather the wiki gives (absent from the game data).
            var weatherNow=territory!=0 && weather.State.Catalog?.Forecast(territory,DateTimeOffset.UtcNow,1) is [ { Weather:{ } current } ] ? current:null;
            if(weatherNow is not null)ImGui.TextUnformatted(localization.Format("MapView.VistaWeatherNow",weatherNow.Name));
            var utcNow=DateTimeOffset.UtcNow;
            if(mapVistaRetryAt==default)mapVistaRetryAt=utcNow.AddMinutes(1);
            // X-75.5 (0.879): the weather and hour it was registered in, seen in game (yours or shared), before the wiki's weathers (seen with, not « required »).
            if(DiscoveryStore is { } vistaStore && DiscoveriesLoaded && vistaStore.Find(Discoveries.DiscoveryKind.VistaSeen,vista.Id,4) is { Count:>0 } registered)
                foreach(var fact in registered)
                    ImGui.TextColored(UI.FfxivTheme.Success,localization.Format("MapView.VistaSeen",weather.State.Catalog?.Weather(fact.Detail)?.Name ?? $"#{fact.Detail}",Gathering.GatheringTime.Clock(fact.LevelMin)));
            wikiVistas?.Request();
            if(WikiVistaFor(vista) is { } wiki)
            {
                ImGui.TextColored(Accent,VistaNumber(vista) is { Length:>0 } number ? number:localization.Format(wiki.Section==0 ? "MapView.VistaNumberArr":"MapView.VistaNumberEx",wiki.Number,ExpansionName(wiki.Section)));
                if(wiki.Weathers.Count>0)
                {
                    var names=wiki.Weathers.Select(name=>weather.State.Catalog?.WeatherBySourceName(name,territory) is { } known ? known.Name:name).ToArray();
                    ImGui.TextColored(stateNow.WeatherOk switch{true=>UI.FfxivTheme.Success,false=>UI.FfxivTheme.Error,_=>UI.FfxivTheme.Text},
                        localization.Format("MapView.VistaWeatherNeeded",string.Join(", ",names))
                        +(stateNow.WeatherOk is { } weatherOk ? " "+localization.Get(weatherOk ? "MapView.VistaWeatherOk":"MapView.VistaWeatherWait"):""));
                }
                else ImGui.TextDisabled(localization.Get("MapView.VistaNoWeather"));
                ImGui.TextDisabled(localization.Get("Alarms.VistaRightClick"));
                if(wiki.Comment.Length>0)
                {
                    ImGui.TextWrapped(localization.Format("MapView.VistaWhere",wiki.Comment));
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
            if(done==true && vista.Data!=0)DrawGameIcon(vista.Data,new Vector2(160,90),false);
        }
    }
    private void DrawGatheringTooltip(MapLocalGatheringPoint point)
    {
        using(TooltipScope.Begin())
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
    }
    // Vendors standing on a map, with the shop names of each, rebuilt only when the map or the catalogue changes.
    private (uint MapId,object? Catalog,IReadOnlyList<(Npcs.NpcVendor Vendor,Npcs.NpcLocation Spot)> Rows,IReadOnlyDictionary<uint,(string Name,Npcs.NpcShopKind Kind,int Items)> Shops,string Festivals) mapVendorCache=(0,null,[],new Dictionary<uint,(string,Npcs.NpcShopKind,int)>(),"");
    // 0.679: worked out once per map, not on every frame.
    private (uint MapId,MapCatalog? Catalog,HashSet<uint> Maps) mapSameLand=(0,null,[]);
    private HashSet<uint> SameLandMaps(MapDefinition map)
    {
        var catalog=maps.State.Catalog;
        if(mapSameLand.MapId==map.MapId && ReferenceEquals(mapSameLand.Catalog,catalog))return mapSameLand.Maps;
        var same=(catalog?.SameLand(map) ?? [map.MapId]).ToHashSet();
        mapSameLand=(map.MapId,catalog,same);
        return same;
    }
    private IReadOnlyList<(Npcs.NpcVendor Vendor,Npcs.NpcLocation Spot)> MapVendors(MapDefinition map)
    {
        if(npcVendors?.State.Catalog is not { } catalog)return [];
        // 0.810, 0.811 (X-45): a festival's NPCs only while it runs; a quest's or the opening's never (their layer says which, NpcLocation.Condition).
        var festivals=Festivals?.Invoke();festivals?.Want();
        // X-95 (0.950): not read yet (logging in, a reload): the last known list stands, instead of « none » then the list again.
        var festivalState=festivals?.State;
        var running=festivalState?.Active.Select(festival=>festival.Id).ToHashSet() ?? (mapVendorCache.Festivals is { Length:>0 } kept ? kept.Split(',').Select(uint.Parse).ToHashSet():[]);
        var festivalKey=string.Join(',',running.Order());
        if(mapVendorCache.MapId!=map.MapId || !ReferenceEquals(mapVendorCache.Catalog,catalog) || mapVendorCache.Festivals!=festivalKey)
        {
            // X-01 (0.1103, first map frame after a load: « canvas vendors » 71 to 156 ms, once 2 729 ms): the shops and the vendors of the map come
            // from the catalogue's indexes, built off the frame, instead of grouping every sale and scanning every vendor here.
            var shops=catalog.ShopSummaries;
            var same=(maps.State.Catalog?.Equivalent(map) ?? [map.MapId]).ToHashSet();
            var nearby=catalog.VendorsOnAny(same);
            var rows=nearby.SelectMany(vendor=>vendor.Locations.Where(spot=>same.Contains(spot.MapId) && Npcs.NpcShopLinks.Present(spot,running)).Take(1).Select(spot=>(vendor,spot))).ToArray();
            mapVendorCache=(map.MapId,catalog,rows,shops,festivalKey);
            // 0.823 (X-45): developer mode, the vendors drawn on this map with their place's condition, and those left out.
            if(developerMode)
            {
                var hidden=nearby.SelectMany(vendor=>vendor.Locations.Where(spot=>same.Contains(spot.MapId) && !Npcs.NpcShopLinks.Present(spot,running)).Take(1)).Count();
                log.Information("Map vendors on map {0}: {1} shown, {2} left out (festivals running: {3}); shown: {4}.",map.MapId,rows.Length,hidden,festivalKey,
                    string.Join(", ",rows.Take(30).Select(row=>$"{row.vendor.NpcId} {row.vendor.Name} c{row.spot.Condition}")));
            }
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
        // 0.679: an NPC of a service whose shop no sheet gives: its name and title say what it does.
        if(vendor.ShopIds.Count==0)return name;
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
            else if(marker.IsLeve ? settings.EdgeLeves:settings.EdgeEvents)targets.Add((MapProjection.PixelFromWorld(marker.Position,map),marker.Icon,EventLabel(marker) ?? localization.Get("MapView.EdgeEvent"),false,0));
        }
        if(settings.EdgeFates)
            foreach(var fate in fates.Where(fate=>fate.Icon!=0 && settings.SizeOf(fate.Icon,map.MapId)>0))targets.Add((MapProjection.PixelFromWorld(fate.Position,map),fate.Icon,FateLabel(fate),false,0));
        if(settings.EdgeGathering)
        {
            var minutes=EorzeaMinutes();
            foreach(var point in gathering.Where(point=>point.Timed && point.Icon!=0 && settings.SizeOf(point.Icon,map.MapId)>0 && point.Position is not null && point.ActiveAt(minutes) && PointAccess(point)?.TomeMissing is null))
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
            // 0.644 (P6, user's precision): the flag's coordinates under its edge icon, not in a corner; above it near the bottom of the map.
            if(icon==FlagIcon && settings.EdgeFlagCoordinates)
            {
                var spot=MapProjection.CoordinateFromPixel(pixel,map);
                var text=localization.Format("MapView.Cursor",MapProjection.CoordinateNumber(spot.X),MapProjection.CoordinateNumber(spot.Y));
                if(player is { } standing)text=localization.Format("MapView.CursorDistance",text,MapProjection.Yalms(standing,pixel,map).ToString("0"));
                var measure=ImGui.CalcTextSize(text);
                var left=Math.Clamp(edge.X-measure.X/2,origin.X+2,Math.Max(origin.X+2,origin.X+size.X-measure.X-2));
                var below=edge.Y+badge*0.75f;
                var top=below+measure.Y>origin.Y+size.Y ? edge.Y-badge*0.75f-measure.Y:below;
                draw.AddRectFilled(new Vector2(left-3,top-1),new Vector2(left+measure.X+3,top+measure.Y+1),ImGui.GetColorU32(new Vector4(0,0,0,0.6f)),3);
                draw.AddText(new Vector2(left,top),ImGui.GetColorU32(UI.FfxivTheme.Text),text);
            }
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
