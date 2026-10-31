using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>MAP-VIEW-016: the game map texture with fixed icons, the player and a flag on right click (docs/MAP-016-PLAN.md).</summary>
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
    private MapLocalIndexService? mapLocal;
    internal void ConfigureMapView(MapLiveReader live,MapMarkerSource markers,MapLocalIndexService local){mapLive=live;mapMarkers=markers;mapLocal=local;}
    private void OpenMapView(uint mapId){mapViewMapId=mapId;mapViewFollow=mapId==0;mapViewOpen=true;}
    // Called when the game map is replaced: the same key (M) that opens the game map toggles the Tensho map.
    internal void ShowMapFromGame(GameMapRequest request)
    {
        if(DateTimeOffset.UtcNow<mapOwnFlagUntil){mapOwnFlagUntil=default;mapViewOpen=true;return;}
        var mapId=maps.State.Catalog is { } catalog ? request.Resolve(catalog):request.Selected==request.Current ? 0:request.Selected;
        if(mapViewOpen && mapId==0 && mapViewFollow)mapViewOpen=false;
        else OpenMapView(mapId);
    }
    private (uint Id,string Name)? mapTeleportTarget;
    private Action? openGameMap;
    internal void ConfigureGameMapOpener(Action opener)=>openGameMap=opener;

    public void DrawMapView()
    {
        if(windowDisposed || !mapViewOpen || maps.State.Catalog is not { } catalog)return;
        var settings=configuration.MapView;var live=mapLive?.State;
        // Hidden, not closed: the window comes back when combat or the duty ends.
        if(live is not null && (settings.HideInCombat && live.InCombat || settings.HideInDuty && live.InDuty))return;
        using var theme=UI.FfxivTheme.Apply();
        var viewport=ImGui.GetMainViewport();
        if(mapResize is { } resize)
        {
            // Enlarge: 90 % of the screen, centred; a second click restores the previous size.
            ImGui.SetNextWindowSize(resize.Size,ImGuiCond.Always);ImGui.SetNextWindowPos(resize.Position,ImGuiCond.Always);mapResize=null;
        }
        else ImGui.SetNextWindowSize(new Vector2(720,760),ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(320,280),new Vector2(float.MaxValue));
        ImGui.SetNextWindowBgAlpha(MapViewSettings.ClampOpacity(settings.Opacity));
        var flags=ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | (settings.Locked ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize:0);
        var visible=ImGui.Begin(localization.Get("MapView.Title")+"###TenshoMapView",ref mapViewOpen,flags);
        try
        {
            if(!visible)return;
            // Read here, outside the tool column: inside a child these calls return the child's size (the « Réduire » bug).
            var window=(ImGui.GetWindowSize(),ImGui.GetWindowPos());
            if(mapViewFollow && live is { MapId:>0 })mapViewMapId=live.MapId;
            var map=catalog.Find(mapViewMapId) ?? mapMarkers?.Overview(mapViewMapId);
            // Layout of the game map: region and zone menus on top, tool column on the left, the map on the right.
            DrawMapMenus(catalog,map,live);
            DrawMapTools(map,viewport,window);
            ImGui.SameLine();
            if(map is null || map.TexturePath.Length==0){ImGui.TextWrapped(localization.Get("MapView.Unavailable"));return;}
            DrawMapCanvas(map,live);
        }
        finally{ImGui.End();}
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
        ImGui.SameLine();
        var isZone=map.TerritoryId!=0;
        var zones=MapMenus.Zones(catalog.Entries,ResolveMap(catalog,regionId));
        var floors=zones.FirstOrDefault(entry=>entry.Any(floor=>floor.MapId==map.MapId)) ?? [];
        ImGui.SetNextItemWidth(MenuWidth(zones.Select(entry=>MapName(entry[0])).Append(isZone ? MapName(map):"--"),true));
        if(ImGui.BeginCombo("##MapZone",isZone ? MapName(map):"--"))
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
        if(ImGui.IsItemHovered())ImGui.SetTooltip(flight.Complete ? localization.Get("MapView.FlightYes"):localization.Format("MapView.FlightPartial",flight.Unlocked,flight.Total));
    }

    // Vertical tool column like the game map buttons.
    private void DrawMapTools(MapDefinition? map,ImGuiViewportPtr viewport,(Vector2 Size,Vector2 Position) window)
    {
        if(!ImGui.BeginChild("MapTools",new Vector2(34,0),false,ImGuiWindowFlags.NoScrollbar))return;
        try
        {
            var parent=map is null || mapMarkers is null ? 0:mapMarkers.Parent(map);
            if(ToolButton(FontAwesomeIcon.ArrowUp,"MapView.Up",parent!=0))NavigateMap(parent);
            if(ToolButton(FontAwesomeIcon.Crosshairs,"MapView.Follow",true,mapViewFollow))mapViewFollow=!mapViewFollow;
            if(ToolButton(FontAwesomeIcon.ExpandArrowsAlt,"MapView.Fit",true)){mapFitRequested=true;mapViewFollow=false;}
            if(ToolButton(mapRestore is null ? FontAwesomeIcon.WindowMaximize:FontAwesomeIcon.WindowRestore,mapRestore is null ? "MapView.Enlarge":"MapView.Restore",true))
            {
                if(mapRestore is { } previous){mapResize=previous;mapRestore=null;}
                else{mapRestore=window;var size=viewport.WorkSize*0.9f;mapResize=(size,viewport.WorkPos+(viewport.WorkSize-size)/2);}
                mapFitRequested=true;
            }
            if(ToolButton(FontAwesomeIcon.Map,"MapView.OpenGameMap",openGameMap is not null))openGameMap?.Invoke();
            if(ToolButton(FontAwesomeIcon.Cog,"MapView.Settings",true,ImGui.IsPopupOpen("MapViewSettings")))ImGui.OpenPopup("MapViewSettings");
            DrawMapSettings(map);
            ImGui.Spacing();
            if(ToolButton(FontAwesomeIcon.Plus,"MapView.ZoomIn",true))mapZoom=MapProjection.ClampZoom(mapZoom*1.25f);
            var percent=mapZoom*100;
            // Short slider centred in the column, with the zoom percentage written under the − button.
            var column=ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX()+Math.Max(0,(column-16)/2));
            if(ImGui.VSliderFloat("##MapZoom",new Vector2(16,80),ref percent,15,400,""))mapZoom=MapProjection.ClampZoom(percent/100);
            if(ToolButton(FontAwesomeIcon.Minus,"MapView.ZoomOut",true))mapZoom=MapProjection.ClampZoom(mapZoom/1.25f);
            var label=$"{mapZoom*100:0}%";
            ImGui.SetCursorPosX(ImGui.GetCursorPosX()+Math.Max(0,(column-ImGui.CalcTextSize(label).X)/2));
            ImGui.TextDisabled(label);
        }
        finally{ImGui.EndChild();}
    }
    // Mappy-like display settings, saved at each change; every layer can be turned off.
    private void DrawMapSettings(MapDefinition? map)
    {
        if(!ImGui.BeginPopup("MapViewSettings"))return;
        try
        {
            var settings=configuration.MapView;var changed=false;
            if(ImGui.CollapsingHeader(localization.Get("MapView.SectionGame"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowFixedIcons",settings.ShowFixedIcons,value=>settings.ShowFixedIcons=value);
                changed|=SettingBox("MapView.TeleportOnClick",settings.TeleportOnClick,value=>settings.TeleportOnClick=value);
                changed|=SettingBox("MapView.ShowPlaceNames",settings.ShowPlaceNames,value=>settings.ShowPlaceNames=value);
                var names=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.PlaceNameScale"),ref names,50,150,"%.0f %%")){settings.PlaceNameScale=names/100;changed=true;}
                changed|=SettingBox("MapView.ShowFlag",settings.ShowFlag,value=>settings.ShowFlag=value);
            }
            if(ImGui.CollapsingHeader(localization.Get("MapView.SectionQuests"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowQuests",settings.ShowQuests,value=>settings.ShowQuests=value);
                changed|=SettingBox("MapView.ShowQuestAreas",settings.ShowQuestAreas,value=>settings.ShowQuestAreas=value);
                changed|=SettingBox("MapView.QuestTooltips",settings.QuestTooltips,value=>settings.QuestTooltips=value);
                changed|=SettingBox("MapView.ShowFates",settings.ShowFates,value=>settings.ShowFates=value);
            }
            if(ImGui.CollapsingHeader(localization.Get("MapView.SectionNearby"),ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed|=SettingBox("MapView.ShowTreasure",settings.ShowTreasure,value=>settings.ShowTreasure=value);
                changed|=SettingBox("MapView.ShowGathering",settings.ShowGathering,value=>settings.ShowGathering=value);
                changed|=SettingBox("MapView.GatheringLabels",settings.GatheringLabels,value=>settings.GatheringLabels=value);
            }
            if(ImGui.CollapsingHeader(localization.Get("MapView.SectionPlayer")))
            {
                changed|=SettingBox("MapView.ShowPlayer",settings.ShowPlayer,value=>settings.ShowPlayer=value);
                changed|=SettingBox("MapView.ShowViewCone",settings.ShowViewCone,value=>settings.ShowViewCone=value);
            }
            if(ImGui.CollapsingHeader(localization.Get("MapView.Display")))
            {
                var scale=MapViewSettings.ClampScale(settings.IconScale)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.IconScale"),ref scale,50,200,"%.0f %%")){settings.IconScale=scale/100;changed=true;}
                var opacity=MapViewSettings.ClampOpacity(settings.Opacity)*100;ImGui.SetNextItemWidth(160);
                if(ImGui.SliderFloat(localization.Get("MapView.Opacity"),ref opacity,20,100,"%.0f %%")){settings.Opacity=opacity/100;changed=true;}
            }
            if(map is not null && ImGui.CollapsingHeader(localization.Get("MapView.SectionIcons")))changed|=DrawIconSizes(map,settings);
            if(ImGui.CollapsingHeader(localization.Get("MapView.Window")))
            {
                changed|=SettingBox("MapView.ReplaceGameMap",settings.ReplaceGameMap,value=>settings.ReplaceGameMap=value);
                changed|=SettingBox("MapView.Locked",settings.Locked,value=>settings.Locked=value);
                changed|=SettingBox("MapView.HideInCombat",settings.HideInCombat,value=>settings.HideInCombat=value);
                changed|=SettingBox("MapView.HideInDuty",settings.HideInDuty,value=>settings.HideInDuty=value);
            }
            ImGui.TextDisabled(localization.Get("MapView.SettingsHelp"));
            if(changed)
            {
                try{save();}
                catch(Exception error){log.Warning(error,"Could not save map settings.");}
            }
        }
        finally{ImGui.EndPopup();}
    }
    // One row per kind of game icon on the shown map (fixed icons, quest/event markers, flag): visible box and own size.
    // Settings are kept per icon id, so they apply to every map that uses the same icon.
    private bool DrawIconSizes(MapDefinition map,MapViewSettings settings)
    {
        var icons=new SortedSet<uint>((mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.Icon!=0).Select(marker=>marker.Icon));
        if(mapLive?.State?.Events is { } events)foreach(var marker in events)if(marker.MapId==map.MapId)icons.Add(marker.Icon);
        icons.Add(FlagIcon);settings.IconSizes ??= [];
        var changed=false;
        if(ImGui.SmallButton(localization.Get("MapView.IconsReset"))){settings.IconSizes.Clear();changed=true;}
        foreach(var icon in icons)
        {
            ImGui.PushID((int)icon);
            try
            {
                var own=settings.SizeOf(icon);var visible=own>0;
                if(ImGui.Checkbox("##Visible",ref visible)){settings.IconSizes[icon]=visible ? 1:0;changed=true;}
                ImGui.SameLine();
                if(textures.TryGetFromGameIcon(new GameIconLookup(icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)
                    ImGui.Image(image.Handle,new Vector2(ImGui.GetFrameHeight()));
                else ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                ImGui.SameLine();
                var percent=(visible ? own:1)*100;
                ImGui.BeginDisabled(!visible);
                ImGui.SetNextItemWidth(120);
                if(ImGui.SliderFloat("##Size",ref percent,50,200,"%.0f %%")){settings.IconSizes[icon]=percent/100;changed=true;}
                ImGui.EndDisabled();
                ImGui.SameLine();
                var name=icon==FlagIcon ? localization.Get("MapView.ShowFlag"):mapMarkers?.SymbolName(icon) is { Length:>0 } symbol ? symbol:localization.Format("MapView.IconUnnamed",icon);
                ImGui.TextUnformatted(name);
            }
            finally{ImGui.PopID();}
        }
        return changed;
    }
    private bool SettingBox(string key,bool value,Action<bool> apply)
    {
        if(!ImGui.Checkbox(localization.Get(key),ref value))return false;
        apply(value);return true;
    }

    private bool ToolButton(FontAwesomeIcon icon,string tooltipKey,bool enabled,bool active=false)
    {
        ImGui.BeginDisabled(!enabled);
        if(active)ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.Button($"{icon.ToIconString()}##{tooltipKey}",new Vector2(26));}
        finally{ImGui.PopFont();if(active)ImGui.PopStyleColor();ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))ImGui.SetTooltip(localization.Get(tooltipKey));
        return clicked && enabled;
    }

    private (Vector2 Size,Vector2 Position)? mapResize;
    private (Vector2 Size,Vector2 Position)? mapRestore;
    private bool mapFitRequested;
    // Texture pixel to bring to the centre of the canvas on the next frame (flag placed from the Tensho map, 0.116).
    private Vector2? mapCenterRequest;
    private bool mapDragged;
    private void NavigateMap(uint mapId){if(mapId==0)return;mapViewMapId=mapId;mapViewFollow=false;mapFitRequested=true;}

    private void DrawMapCanvas(MapDefinition map,MapLiveState? live)
    {
        var origin=ImGui.GetCursorScreenPos();var size=Vector2.Max(ImGui.GetContentRegionAvail(),new Vector2(64));
        ImGui.InvisibleButton("MapCanvas",size,ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
        var hovered=ImGui.IsItemHovered();var io=ImGui.GetIO();
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
            if(textures.GetFromGame(map.TexturePath).TryGetWrap(out var texture,out _) && texture is not null)
                draw.AddImage(texture.Handle,origin+mapPan,origin+mapPan+new Vector2(MapProjection.TextureSize*mapZoom),Vector2.Zero,Vector2.One,ImGui.GetColorU32(new Vector4(1,1,1,opacity)));
            else draw.AddText(origin+new Vector2(8),ImGui.GetColorU32(UI.FfxivTheme.TextMuted),localization.Get("MapView.Loading"));
            string? tooltip=null;
            // Icons follow the map zoom exactly (32 px at 100 %, 0.113 at the user's request); 4 px floor only so they never vanish.
            // Each game icon can then be resized or hidden on its own (settings, « Icônes de cette carte »).
            var baseIcon=Math.Max(4,32*mapZoom*MapViewSettings.ClampScale(settings.IconScale));
            var markers=settings.ShowFixedIcons || settings.ShowPlaceNames ? mapMarkers?.For(map.MarkerRange) ?? []:[];
            // Place names first, under the icons, in large outlined text like the game map; they also follow the zoom.
            var font=ImGui.GetFont();var nameScale=Math.Clamp(float.IsFinite(settings.PlaceNameScale) ? settings.PlaceNameScale:1,0.5f,1.5f);
            var nameSize=Math.Clamp(ImGui.GetFontSize()*2.4f*mapZoom*nameScale,ImGui.GetFontSize()*0.75f,ImGui.GetFontSize()*2.6f*nameScale);
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
            foreach(var marker in settings.ShowFixedIcons ? markers.Where(marker=>marker.Icon!=0):[])
            {
                var own=settings.SizeOf(marker.Icon);if(own<=0)continue;
                var iconSize=Math.Max(4,baseIcon*own);
                var centre=origin+mapPan+new Vector2(marker.X,marker.Y)*mapZoom;
                if(hovered && marker.LinkMapId!=0 && Vector2.Distance(io.MousePos,centre)<=iconSize/2)linkUnderMouse=marker.LinkMapId;
                if(textures.TryGetFromGameIcon(new GameIconLookup(marker.Icon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                    draw.AddImage(icon.Handle,centre-new Vector2(iconSize/2),centre+new Vector2(iconSize/2));
                var underMouse=hovered && Vector2.Distance(io.MousePos,centre)<=iconSize/2;
                if(underMouse && marker.Label.Length>0)tooltip=marker.Label;
                // Aetherytes: a click asks for the teleport, like the game map (normal teleport cost).
                if(underMouse && marker.AetheryteId!=0 && settings.TeleportOnClick)
                {
                    tooltip=(tooltip ?? "")+"\n"+localization.Get("MapView.TeleportHint");
                    if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged){mapTeleportTarget=(marker.AetheryteId,marker.Label);mapLive?.RefreshTeleportCosts();ImGui.OpenPopup("MapTeleport");}
                }
            }
            // Nearby coffers and gathering points streamed by the game, drawn with icon-font symbols.
            if(live?.Objects is { } nearby && live.MapId==map.MapId)
                foreach(var item in nearby)
                {
                    if(item.Kind==MapObjectKind.Treasure ? !settings.ShowTreasure:!settings.ShowGathering)continue;
                    var at=origin+mapPan+MapProjection.PixelFromWorld(item.Position,map)*mapZoom;
                    var symbol=(item.Kind==MapObjectKind.Treasure ? FontAwesomeIcon.BoxOpen:FontAwesomeIcon.Leaf).ToIconString();
                    var colour=item.Kind==MapObjectKind.Treasure ? new Vector4(0.98f,0.82f,0.30f,1):new Vector4(0.55f,0.90f,0.55f,1);
                    var glyph=Math.Max(4,baseIcon*0.6f);var corner=at-new Vector2(glyph/2);
                    draw.AddCircleFilled(at,glyph*0.7f,ImGui.GetColorU32(new Vector4(0,0,0,0.55f)),20);
                    draw.AddText(UiBuilder.IconFont,glyph,corner,ImGui.GetColorU32(colour),symbol);
                    var label=item.Kind==MapObjectKind.Gathering && item.Level>0 ? localization.Format("MapView.GatheringLabel",item.Name,item.Level):item.Name;
                    if(item.Kind==MapObjectKind.Gathering && settings.GatheringLabels)
                        draw.AddText(at+new Vector2(glyph*0.8f,-ImGui.GetFontSize()/2),ImGui.GetColorU32(UI.FfxivTheme.Text),label);
                    if(hovered && Vector2.Distance(io.MousePos,at)<=glyph)tooltip=label;
                }
            // FATEs running in the zone: area, icon, then name/level/progress in the tooltip (the FATE table covers the current zone only).
            if(settings.ShowFates && live?.Fates is { } fates && live.MapId==map.MapId)
                foreach(var fate in fates)
                {
                    var own=settings.SizeOf(fate.Icon);if(own<=0)continue;
                    var iconSize=Math.Max(4,baseIcon*own);
                    var at=origin+mapPan+MapProjection.PixelFromWorld(fate.Position,map)*mapZoom;
                    if(fate.Radius>0)
                    {
                        var radius=fate.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom;
                        draw.AddCircleFilled(at,radius,ImGui.GetColorU32(new Vector4(0.75f,0.45f,0.95f,0.16f)),48);
                        draw.AddCircle(at,radius,ImGui.GetColorU32(new Vector4(0.75f,0.45f,0.95f,0.6f)),48,1.5f);
                    }
                    if(fate.Icon!=0 && textures.TryGetFromGameIcon(new GameIconLookup(fate.Icon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                        draw.AddImage(icon.Handle,at-new Vector2(iconSize/2),at+new Vector2(iconSize/2));
                    if(hovered && Vector2.Distance(io.MousePos,at)<=iconSize/2)tooltip=FateLabel(fate);
                }
            // Quest and event markers of the game: area circle (when the game gives a radius), then the icon.
            if(settings.ShowQuests && live?.Events is { } events)
                foreach(var marker in events.Where(marker=>marker.MapId==map.MapId))
                {
                    var own=settings.SizeOf(marker.Icon);if(own<=0)continue;
                    var iconSize=Math.Max(4,baseIcon*own);
                    var at=origin+mapPan+MapProjection.PixelFromWorld(marker.Position,map)*mapZoom;
                    if(settings.ShowQuestAreas && marker.Radius>0)
                    {
                        var radius=marker.Radius*Math.Max(1,map.SizeFactor)/100f*mapZoom;
                        draw.AddCircleFilled(at,radius,ImGui.GetColorU32(new Vector4(0.95f,0.70f,0.25f,0.18f)),48);
                        draw.AddCircle(at,radius,ImGui.GetColorU32(new Vector4(0.95f,0.70f,0.25f,0.6f)),48,1.5f);
                    }
                    if(textures.TryGetFromGameIcon(new GameIconLookup(marker.Icon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                        draw.AddImage(icon.Handle,at-new Vector2(iconSize/2),at+new Vector2(iconSize/2));
                    if(settings.QuestTooltips && hovered && marker.Tooltip.Length>0 && Vector2.Distance(io.MousePos,at)<=iconSize/2)tooltip=marker.Tooltip;
                }
            if(settings.ShowFlag && settings.SizeOf(FlagIcon)>0 && live is { Flag:{ } flag } && live.FlagMapId==map.MapId)
            {
                var flagPixel=MapProjection.PixelFromWorld(new Vector3(flag.X,0,flag.Y),map);var at=origin+mapPan+flagPixel*mapZoom;var iconSize=Math.Max(4,baseIcon*settings.SizeOf(FlagIcon));
                if(textures.TryGetFromGameIcon(new GameIconLookup(FlagIcon),out var shared) && shared.TryGetWrap(out var icon,out _) && icon is not null)
                    draw.AddImage(icon.Handle,at-new Vector2(iconSize/2),at+new Vector2(iconSize/2));
                else draw.AddTriangleFilled(at,at+new Vector2(0,-14),at+new Vector2(10,-10),ImGui.GetColorU32(UI.FfxivTheme.Error));
            }
            if(settings.ShowPlayer && player is { } here)DrawPlayerArrow(draw,origin+mapPan+here*mapZoom,live!.Rotation,settings.ShowViewCone);
            if(hovered)
            {
                var coordinate=MapProjection.CoordinateFromPixel((io.MousePos-origin-mapPan)/mapZoom,map);
                var label=localization.Format("MapView.Cursor",coordinate.X.ToString("0.0"),coordinate.Y.ToString("0.0"));
                var box=ImGui.CalcTextSize(label)+new Vector2(10,6);
                draw.AddRectFilled(origin+new Vector2(6,size.Y-box.Y-6),origin+new Vector2(6+box.X,size.Y-6),ImGui.GetColorU32(UI.FfxivTheme.Panel),3);
                draw.AddText(origin+new Vector2(11,size.Y-box.Y-3),ImGui.GetColorU32(UI.FfxivTheme.Text),label);
            }
            if(tooltip is not null)ImGui.SetTooltip(linkUnderMouse is null ? tooltip:tooltip+"\n"+localization.Get("MapView.OpenLink"));
            // Region and world maps: a click (not a drag) on a linked icon opens that map, like the game.
            if(linkUnderMouse is { } link && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)NavigateMap(link);
        }
        finally{draw.PopClipRect();}
        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right)){mapContextPixel=(io.MousePos-origin-mapPan)/mapZoom;ImGui.OpenPopup("MapViewContext");}
        if(ImGui.BeginPopup("MapTeleport"))
        {
            try
            {
                if(mapTeleportTarget is { } target)
                {
                    ImGui.TextUnformatted(localization.Format("MapView.TeleportAsk",target.Name));
                    // Price from the game's teleport list; unknown (not attuned or not read yet) is said, never shown as 0.
                    if(mapLive?.TeleportCosts is not { } costs)ImGui.TextDisabled(localization.Get("MapView.TeleportCostReading"));
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
                try{if(ImGui.Selectable(localization.Get("MapView.Flag")))PlaceFlagFromMapView(map,coordinate.X,coordinate.Y);}
                finally{ImGui.EndDisabled();}
                ImGui.BeginDisabled(live?.Flag is null);
                try{if(ImGui.Selectable(localization.Get("MapView.RemoveFlag")))mapLive?.RemoveFlag();}
                finally{ImGui.EndDisabled();}
                ImGui.BeginDisabled(map.TerritoryId==0);
                try{if(ImGui.Selectable(localization.Get("MapView.UseInTool"))){selectedMapId=map.MapId;mapX=coordinate.X;mapY=coordinate.Y;bookmarkSaved=false;page=Page.Maps;IsOpen=true;}}
                finally{ImGui.EndDisabled();}
                if(map.TerritoryId!=0){ImGui.Separator();DrawMapLocalLists(map,live);}
            }
            finally{ImGui.EndPopup();}
        }
        // Aetheryte chosen in the local lists: the teleport question opens once the context menu is closed.
        if(mapTeleportRequested){mapTeleportRequested=false;ImGui.OpenPopup("MapTeleport");}
        if(mapOpenError is { } error)ImGui.SetTooltip(error);
    }

    // Game rotation 0 faces +Z, which is "down" on the map texture.
    private static void DrawPlayerArrow(ImDrawListPtr draw,Vector2 centre,float rotation,bool cone)
    {
        var forward=new Vector2(MathF.Sin(rotation),MathF.Cos(rotation));var side=new Vector2(-forward.Y,forward.X);
        var colour=ImGui.GetColorU32(UI.FfxivTheme.Gold);
        if(cone)
        {
            // View cone like the minimap: 90° wide, 60 px long.
            var left=forward*MathF.Cos(MathF.PI/4)+side*MathF.Sin(MathF.PI/4);var right=forward*MathF.Cos(MathF.PI/4)-side*MathF.Sin(MathF.PI/4);
            draw.AddTriangleFilled(centre,centre+left*60,centre+right*60,ImGui.GetColorU32(UI.FfxivTheme.Gold with{W=0.18f}));
        }
        draw.AddTriangleFilled(centre+forward*12,centre-forward*7+side*7,centre-forward*7-side*7,colour);
        draw.AddCircle(centre,4,ImGui.GetColorU32(UI.FfxivTheme.Text),12,1.5f);
    }
}
